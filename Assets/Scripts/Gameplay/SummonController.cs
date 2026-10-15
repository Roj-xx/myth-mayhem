using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MythMayhem.Board;
using MythMayhem.UI;

namespace MythMayhem.Gameplay
{
    public class SummonController : MonoBehaviour
    {
        private const string InstructionMessage = "Select a card, then tap one of your lanes";

        [SerializeField] private BoardManager board;
        [SerializeField] private CardHandController hand;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private CardPlayController cardPlay;
        [SerializeField] private OpponentController opponentController;
        [SerializeField] private float tapRadius = 200f;
        [SerializeField] private TurnManager turnManager;

        private readonly GameObject[] laneOccupants = new GameObject[BoardManager.LaneCount];
        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
        private PointerEventData pointerData;

        private void Awake()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            SetStatus(InstructionMessage);
        }

        private void Update()
        {
            if (!TryGetTap(out Vector2 screenPosition))
                return;

            if (IsOverUI(screenPosition))
                return;

            // A debuff ("Sumpa") must be aimed at an enemy lane; every other card
            // (creature, building, friendly support) targets one of the local
            // player's lanes. We branch on the selected card so the two spawn rows
            // can never be confused, which matters because they sit close together.
            if (CanTargetEnemyLane())
            {
                int enemyLane = FindRemoteLane(screenPosition);
                if (enemyLane >= 0)
                {
                    PlaySelectedCard(enemyLane, NetworkSession.RemoteSide);
                    return;
                }

                SetStatus("Tap an enemy lane to target its creature");
                return;
            }

            int lane = FindLocalLane(screenPosition);
            if (lane >= 0)
            {
                PlaySelectedCard(lane, NetworkSession.LocalSide);
                return;
            }

            if (IsNearRemoteSpawn(screenPosition))
                SetStatus("You can only play into your own lanes");
        }

        private bool TryGetTap(out Vector2 screenPosition)
        {
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
                return true;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                screenPosition = Mouse.current.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        private bool IsOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null)
                return false;

            if (uiRaycastResults == null)
                return false;

            if (pointerData == null)
                pointerData = new PointerEventData(EventSystem.current);

            pointerData.position = screenPosition;
            uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, uiRaycastResults);
            return uiRaycastResults.Count > 0;
        }

        private Transform[] LocalSpawns
        {
            get
            {
                if (board == null)
                    return null;

                return NetworkSession.LocalSide == CreatureSide.Player ? board.PlayerSpawns : board.OpponentSpawns;
            }
        }

        private Transform[] RemoteSpawns
        {
            get
            {
                if (board == null)
                    return null;

                return NetworkSession.LocalSide == CreatureSide.Player ? board.OpponentSpawns : board.PlayerSpawns;
            }
        }

        private int FindLocalLane(Vector2 screenPosition)
        {
            return FindNearestLane(screenPosition, LocalSpawns);
        }

        private int FindRemoteLane(Vector2 screenPosition)
        {
            return FindNearestLane(screenPosition, RemoteSpawns);
        }

        private int FindNearestLane(Vector2 screenPosition, Transform[] spawns)
        {
            if (mainCamera == null || spawns == null)
                return -1;

            int bestLane = -1;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < spawns.Length; i++)
            {
                float distance = ScreenDistance(screenPosition, spawns[i]);
                if (distance <= tapRadius && distance < bestDistance)
                {
                    bestDistance = distance;
                    bestLane = i;
                }
            }

            return bestLane;
        }

        private bool CanTargetEnemyLane()
        {
            if (hand == null || !hand.HasSelection)
                return false;

            SupportCardDefinition support = hand.SelectedSupportDefinition;
            return support != null && support.EffectType == SupportEffectType.EnemyAttackDebuff;
        }

        private bool IsNearRemoteSpawn(Vector2 screenPosition)
        {
            if (mainCamera == null)
                return false;

            Transform[] spawns = RemoteSpawns;
            if (spawns == null)
                return false;

            for (int i = 0; i < spawns.Length; i++)
            {
                if (ScreenDistance(screenPosition, spawns[i]) <= tapRadius)
                    return true;
            }

            return false;
        }

        private float ScreenDistance(Vector2 screenPosition, Transform spawnPoint)
        {
            if (spawnPoint == null)
                return float.MaxValue;

            Vector3 projected = mainCamera.WorldToScreenPoint(spawnPoint.position);
            if (projected.z <= 0f)
                return float.MaxValue;

            return Vector2.Distance(screenPosition, new Vector2(projected.x, projected.y));
        }

        private void PlaySelectedCard(int lane, CreatureSide side)
        {
            if (turnManager != null && turnManager.IsMatchOver)
            {
                SetStatus("The match is over");
                return;
            }

            if (turnManager != null && !turnManager.IsActive(NetworkSession.LocalSide))
            {
                SetStatus("Wait for your turn");
                return;
            }

            if (hand == null || !hand.HasSelection)
            {
                SetStatus("Select a card first");
                return;
            }

            DeckCard card = hand.SelectedCard;
            if (card == null)
            {
                SetStatus("Select a card first");
                return;
            }

            // In a networked match the local peer sends an intent; the host
            // validates and applies it, then replicates the resulting board.
            if (NetworkSession.IsNetworked && NetworkSession.Intents != null)
            {
                switch (card.Type)
                {
                    case CardType.Support:
                        NetworkSession.Intents.PlaySupport(card.DefinitionName, lane);
                        break;
                    case CardType.Building:
                        NetworkSession.Intents.PlayBuilding(card.DefinitionName, lane);
                        break;
                    default:
                        NetworkSession.Intents.PlayCreature(card.DefinitionName, lane);
                        break;
                }

                hand.ConsumeSelected();
                SetStatus("Played " + card.DefinitionName + " in lane " + (lane + 1));
                return;
            }

            switch (card.Type)
            {
                case CardType.Support:
                    TryPlaySupport(card, lane, side);
                    break;
                case CardType.Building:
                    TryPlaceBuilding(card, lane);
                    break;
                default:
                    TrySummon(lane);
                    break;
            }
        }

        private void TryPlaySupport(DeckCard card, int lane, CreatureSide side)
        {
            SupportCardDefinition support = card != null ? card.SupportDefinition : null;
            if (support == null || cardPlay == null)
            {
                SetStatus("Cannot play that card right now");
                return;
            }

            CreatureSide localSide = NetworkSession.LocalSide;

            if (turnManager != null && !turnManager.CanAfford(localSide, support.Cost))
            {
                SetStatus("Not enough summon points");
                return;
            }

            CreatureInstance target = GetSupportTarget(support, lane, side);
            if (target == null)
            {
                SetStatus("No valid target for that card");
                return;
            }

            if (!cardPlay.TryApplySupport(localSide, support, target))
            {
                SetStatus("No valid target for that card");
                return;
            }

            hand.ConsumeSelected();

            if (turnManager != null)
                turnManager.Spend(localSide, support.Cost);

            SetStatus(support.DisplayName + " played on " + target.DisplayName);
        }

        private CreatureInstance GetSupportTarget(SupportCardDefinition support, int lane, CreatureSide side)
        {
            if (cardPlay == null)
                return null;

            CreatureSide targetSide = support != null && support.EffectType == SupportEffectType.EnemyAttackDebuff
                ? NetworkSession.RemoteSide
                : NetworkSession.LocalSide;

            return cardPlay.GetCreature(targetSide, lane);
        }

        private void TryPlaceBuilding(DeckCard card, int lane)
        {
            BuildingDefinition building = card != null ? card.BuildingDefinition : null;
            if (building == null || cardPlay == null)
            {
                SetStatus("Cannot play that card right now");
                return;
            }

            CreatureSide localSide = NetworkSession.LocalSide;

            if (!cardPlay.IsLaneOpenForBuilding(localSide, lane))
            {
                SetStatus("Lane " + (lane + 1) + " already has a building");
                return;
            }

            if (turnManager != null && !turnManager.CanAfford(localSide, building.Cost))
            {
                SetStatus("Not enough summon points");
                return;
            }

            if (!cardPlay.TryPlaceBuilding(localSide, building, lane))
            {
                SetStatus("Cannot place that building here");
                return;
            }

            hand.ConsumeSelected();

            if (turnManager != null)
                turnManager.Spend(localSide, building.Cost);

            SetStatus("Placed " + building.DisplayName + " in lane " + (lane + 1));
        }

        private void TrySummon(int lane)
        {
            CreatureSide localSide = NetworkSession.LocalSide;

            if (turnManager != null && !turnManager.IsActive(localSide))
            {
                SetStatus("Wait for your turn");
                return;
            }

            if (hand == null || !hand.HasSelection)
            {
                SetStatus("Select a card first");
                return;
            }

            if (!cardPlay.IsLaneOpenForCreature(localSide, lane))
            {
                SetStatus("Lane " + (lane + 1) + " already has a creature");
                return;
            }

            CreatureDefinition definition = hand.SelectedDefinition;
            if (definition == null)
            {
                SetStatus("Cannot summon right now");
                return;
            }

            int cost = definition.Cost;

            if (turnManager != null && !turnManager.CanAfford(localSide, cost))
            {
                SetStatus("Not enough summon points");
                return;
            }

            string failure;
            CreatureInstance summoned = cardPlay.TrySummonCreature(localSide, definition, lane, out failure);
            if (summoned == null)
            {
                SetStatus(string.IsNullOrEmpty(failure) ? "Cannot summon right now" : failure);
                return;
            }

            hand.ConsumeSelected();

            if (turnManager != null)
                turnManager.Spend(localSide, cost);

            SetStatus("Summoned " + definition.DisplayName + " to lane " + (lane + 1));
        }

        public void SetLaneOccupant(int lane, GameObject occupant)
        {
            if (lane < 0 || lane >= laneOccupants.Length)
                return;

            laneOccupants[lane] = occupant;
        }

        public CreatureInstance GetLaneOccupant(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= laneOccupants.Length)
                return null;

            GameObject occupant = laneOccupants[laneIndex];
            return occupant != null ? occupant.GetComponent<CreatureInstance>() : null;
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
