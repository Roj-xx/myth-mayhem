using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MythMayhem.Board;

namespace MythMayhem.Gameplay
{
    public class OpponentController : MonoBehaviour
    {
        private const int CreatureWeight = 2;
        private const int CardWeight = 1;

        [SerializeField] private BoardManager board;
        [SerializeField] private Text statusLabel;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private CardPlayController cardPlay;

        [Header("Automatic AI")]
        [SerializeField] private float aiDecisionDelaySeconds = 0.75f;
        [SerializeField] private float endTurnPauseSeconds = 0.6f;

        [Header("Development Only")]
        [Tooltip("When enabled, pressing O on the keyboard runs the same AI action manually. Never active in normal gameplay.")]
        [SerializeField] private bool debugOKeySummon;

        private readonly GameObject[] laneOccupants = new GameObject[BoardManager.LaneCount];
        private readonly List<PlayOption> playOptions = new List<PlayOption>();
        private Coroutine turnRoutine;
        private bool playedThisTurn;
        private bool warnedMissingReferences;

        private struct PlayOption
        {
            public int HandIndex;
            public CardType Type;
            public int Weight;

            public PlayOption(int handIndex, CardType type, int weight)
            {
                HandIndex = handIndex;
                Type = type;
                Weight = weight;
            }
        }

        private void OnEnable()
        {
            if (turnManager != null)
                turnManager.TurnBegan += OnTurnBegan;
        }

        private void OnDisable()
        {
            if (turnManager != null)
                turnManager.TurnBegan -= OnTurnBegan;
        }

        private void Update()
        {
            if (NetworkSession.IsNetworked)
                return;

            if (!debugOKeySummon)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.oKey.wasPressedThisFrame)
                return;

            if (turnManager != null && (turnManager.IsMatchOver || !turnManager.IsActive(CreatureSide.Opponent)))
                return;

            PlayOpponentCard();
        }

        private void OnTurnBegan()
        {
            // The remote player controls the opponent side in a networked match,
            // so the AI must never act there.
            if (NetworkSession.IsNetworked)
                return;

            if (turnManager == null || !turnManager.IsActive(CreatureSide.Opponent))
                return;

            if (turnRoutine != null)
            {
                StopCoroutine(turnRoutine);
                turnRoutine = null;
            }

            playedThisTurn = false;
            turnRoutine = StartCoroutine(PlayTurnRoutine());
        }

        private IEnumerator PlayTurnRoutine()
        {
            float debounce = turnManager != null ? turnManager.EndTurnDebounceSeconds : 0.5f;

            if (aiDecisionDelaySeconds > 0f)
                yield return new WaitForSeconds(aiDecisionDelaySeconds);

            if (!TurnIsStillActive())
                yield break;

            PlayOpponentCard();

            float waitBeforeEnding = Mathf.Max(endTurnPauseSeconds, debounce + 0.35f);
            if (waitBeforeEnding > 0f)
                yield return new WaitForSeconds(waitBeforeEnding);

            if (!TurnIsStillActive())
                yield break;

            EndOpponentTurn();
        }

        private bool TurnIsStillActive()
        {
            return turnManager != null
                && !turnManager.IsMatchOver
                && turnManager.IsActive(CreatureSide.Opponent);
        }

        private void EndOpponentTurn()
        {
            if (!TurnIsStillActive())
                return;

            turnManager.EndTurn();
            Debug.Log("[Opponent] Turn ended; combat will resolve this round.");
        }

        private void PlayOpponentCard()
        {
            if (playedThisTurn)
                return;

            if (board == null || cardPlay == null || turnManager == null)
            {
                WarnMissingReferences();
                return;
            }

            DeckController deck = turnManager.OpponentDeck;
            if (deck == null)
            {
                WarnMissingReferences();
                return;
            }

            playOptions.Clear();

            for (int i = 0; i < deck.HandCount; i++)
            {
                DeckCard card = deck.GetHandCard(i);
                if (card == null || !turnManager.CanAfford(CreatureSide.Opponent, card.Cost))
                    continue;

                switch (card.Type)
                {
                    case CardType.Creature:
                        if (card.CreatureDefinition != null && HasOpenCreatureLane())
                            playOptions.Add(new PlayOption(i, CardType.Creature, CreatureWeight));
                        break;
                    case CardType.Support:
                        if (card.SupportDefinition != null && HasSupportTarget(card.SupportDefinition))
                            playOptions.Add(new PlayOption(i, CardType.Support, CardWeight));
                        break;
                    case CardType.Building:
                        if (card.BuildingDefinition != null && HasOpenBuildingLane())
                            playOptions.Add(new PlayOption(i, CardType.Building, CardWeight));
                        break;
                }
            }

            if (playOptions.Count == 0)
            {
                Debug.Log("[Opponent] No valid playable card; ending turn.");
                SetStatus("Opponent has nothing playable");
                return;
            }

            PlayOption chosen = ChooseWeighted();
            if (!ExecutePlay(deck, chosen))
            {
                Debug.LogWarning("[Opponent] Selected play failed; ending turn.");
                return;
            }

            playedThisTurn = true;
        }

        private PlayOption ChooseWeighted()
        {
            int total = 0;
            foreach (PlayOption option in playOptions)
                total += option.Weight;

            int roll = Random.Range(0, total);
            PlayOption chosen = playOptions[playOptions.Count - 1];

            foreach (PlayOption option in playOptions)
            {
                roll -= option.Weight;
                if (roll < 0)
                {
                    chosen = option;
                    break;
                }
            }

            return chosen;
        }

        private bool ExecutePlay(DeckController deck, PlayOption option)
        {
            DeckCard card = deck.GetHandCard(option.HandIndex);
            if (card == null)
                return false;

            switch (option.Type)
            {
                case CardType.Creature:
                    return TrySummonCreature(deck, card, option.HandIndex);
                case CardType.Support:
                    return TryPlaySupport(deck, card, option.HandIndex);
                case CardType.Building:
                    return TryPlaceBuilding(deck, card, option.HandIndex);
                default:
                    return false;
            }
        }

        private bool TrySummonCreature(DeckController deck, DeckCard card, int handIndex)
        {
            CreatureDefinition definition = card.CreatureDefinition;
            if (definition == null || cardPlay == null)
                return false;

            int lane = cardPlay.FindOpenLaneForCreature(CreatureSide.Opponent);
            if (lane < 0)
                return false;

            string failure;
            CreatureInstance summoned = cardPlay.TrySummonCreature(CreatureSide.Opponent, definition, lane, out failure);
            if (summoned == null)
            {
                Debug.LogWarning("[Opponent] Failed to summon creature: " + failure);
                return false;
            }

            deck.RemoveFromHand(handIndex);
            turnManager.Spend(CreatureSide.Opponent, card.Cost);

            Debug.Log("[Opponent] Summoned " + definition.DisplayName + " to red lane " + (lane + 1) + " for " + card.Cost + " point(s).");
            SetStatus("Opponent summoned " + definition.DisplayName + " to red lane " + (lane + 1));
            return true;
        }

        private bool TryPlaySupport(DeckController deck, DeckCard card, int handIndex)
        {
            SupportCardDefinition support = card.SupportDefinition;
            if (support == null || cardPlay == null)
                return false;

            CreatureInstance target = FindSupportTarget(support);
            if (target == null)
                return false;

            if (!cardPlay.TryApplySupport(CreatureSide.Opponent, support, target))
                return false;

            deck.RemoveFromHand(handIndex);
            turnManager.Spend(CreatureSide.Opponent, card.Cost);

            Debug.Log("[Opponent] Played " + support.DisplayName + " on " + target.DisplayName + " for " + card.Cost + " point(s).");
            SetStatus("Opponent played " + support.DisplayName + " on " + target.DisplayName);
            return true;
        }

        private bool TryPlaceBuilding(DeckController deck, DeckCard card, int handIndex)
        {
            BuildingDefinition building = card.BuildingDefinition;
            if (building == null || cardPlay == null)
                return false;

            int lane = FindOpenLaneForBuilding();
            if (lane < 0)
                return false;

            if (!cardPlay.TryPlaceBuilding(CreatureSide.Opponent, building, lane))
                return false;

            deck.RemoveFromHand(handIndex);
            turnManager.Spend(CreatureSide.Opponent, card.Cost);

            Debug.Log("[Opponent] Placed " + building.DisplayName + " in red lane " + (lane + 1) + " for " + card.Cost + " point(s).");
            SetStatus("Opponent placed " + building.DisplayName + " in red lane " + (lane + 1));
            return true;
        }

        private bool HasOpenCreatureLane()
        {
            return FindOpenLaneForCreature() >= 0;
        }

        private bool HasOpenBuildingLane()
        {
            return FindOpenLaneForBuilding() >= 0;
        }

        private int FindOpenLaneForCreature()
        {
            if (cardPlay != null)
                return cardPlay.FindOpenLaneForCreature(CreatureSide.Opponent);

            for (int lane = 0; lane < laneOccupants.Length; lane++)
            {
                if (laneOccupants[lane] != null)
                    continue;

                if (board == null || board.GetOpponentSpawn(lane) == null)
                    continue;

                return lane;
            }

            return -1;
        }

        private int FindOpenLaneForBuilding()
        {
            if (cardPlay != null)
                return cardPlay.FindOpenLaneForBuilding(CreatureSide.Opponent);

            for (int lane = 0; lane < laneOccupants.Length; lane++)
            {
                if (board == null || board.GetOpponentSpawn(lane) == null)
                    continue;

                return lane;
            }

            return -1;
        }

        private bool HasSupportTarget(SupportCardDefinition support)
        {
            return FindSupportTarget(support) != null;
        }

        private CreatureInstance FindSupportTarget(SupportCardDefinition support)
        {
            if (support == null || cardPlay == null)
                return null;

            CreatureSide wanted = support.EffectType == SupportEffectType.EnemyAttackDebuff ? CreatureSide.Player : CreatureSide.Opponent;

            for (int lane = 0; lane < BoardManager.LaneCount; lane++)
            {
                CreatureInstance creature = cardPlay.GetCreature(wanted, lane);
                if (creature != null && creature.IsAlive)
                    return creature;
            }

            return null;
        }

        private void WarnMissingReferences()
        {
            if (warnedMissingReferences)
                return;

            warnedMissingReferences = true;
            Debug.LogWarning("OpponentController is missing required references. Assign Board, Creature Factory, Turn Manager (with its opponent deck) on the GameDirector component.");
        }

        public void SetLaneOccupant(int lane, GameObject occupant)
        {
            if (lane < 0 || lane >= laneOccupants.Length)
                return;

            laneOccupants[lane] = occupant;
        }

        public CreatureInstance GetLaneOccupant(int laneIndex)
        {
            GameObject occupant = laneOccupants[laneIndex];
            return occupant != null ? occupant.GetComponent<CreatureInstance>() : null;
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
            else
                Debug.Log("[Opponent] " + message);
        }
    }
}