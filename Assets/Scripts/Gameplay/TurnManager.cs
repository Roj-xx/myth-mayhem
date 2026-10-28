using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MythMayhem.Audio;
using MythMayhem.UI;

namespace MythMayhem.Gameplay
{
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private int startingSummonPoints = 5;
        [SerializeField] private int summonPointsPerTurn = 2;
        [SerializeField] private int maxSummonPoints = 8;
        [SerializeField] private float endTurnDebounceSeconds = 0.5f;
        [SerializeField] private CombatController combat;
        [SerializeField] private MatchController matchController;
        [SerializeField] private Text activeSideLabel;
        [SerializeField] private Text summonPointsLabel;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private CardHandController hand;
        [SerializeField] private DeckController playerDeck;
        [SerializeField] private DeckController opponentDeck;

        [Header("Turn Timer")]
        [SerializeField] private float turnTimeLimitSeconds = 60f;
        [SerializeField] private float countdownWarningSeconds = 10f;

        private CreatureSide activeSide = CreatureSide.Player;
        private float turnStartTime;
        private string normalTurnLabelText = string.Empty;
        private int playerPoints;
        private int opponentPoints;
        private bool openingTurn;
        private float endTurnLockedUntil;
        private bool turnTransitioning;
        private bool playerStartedFirstTurn;
        private bool opponentStartedFirstTurn;
        private bool paused;
        private float pauseStartedAt;

        public CreatureSide ActiveSide => activeSide;
        public event Action TurnBegan;
        public DeckController OpponentDeck => opponentDeck;
        public float EndTurnDebounceSeconds => endTurnDebounceSeconds;
        public bool IsMatchOver => matchController != null && matchController.IsMatchOver;
        private void Start()
        {
            activeSide = CreatureSide.Player;
            openingTurn = true;
            playerStartedFirstTurn = true;
            opponentStartedFirstTurn = false;
            turnStartTime = Time.unscaledTime;

            SetPoints(CreatureSide.Player, startingSummonPoints);
            SetPoints(CreatureSide.Opponent, startingSummonPoints);

            if (endTurnButton != null)
            {
                AudioManager.HookButton(endTurnButton);
                endTurnButton.onClick.AddListener(RequestEndTurn);
            }

            RefreshLabels();

            if (hand != null)
                hand.RefreshInteractivity();

            // Each peer only manages its own hand. In a networked match the
            // authoritative turn/points arrive through BoardSnapshot, so the
            // local peer must not touch the AI opponent's deck here.
            if (playerDeck != null)
                playerDeck.DealInitialHand();

            if (!NetworkSession.IsNetworked)
            {
                if (opponentDeck != null)
                    opponentDeck.DealInitialHand();

                TurnBegan?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.RemoveListener(RequestEndTurn);
        }

        // Wired to the End Turn button. In a networked match the request is
        // forwarded to the host; locally we only act when it is our turn.
        public void RequestEndTurn()
        {
            if (NetworkSession.IsNetworked)
            {
                if (activeSide != NetworkSession.LocalSide)
                    return;

                if (NetworkSession.Intents != null)
                    NetworkSession.Intents.RequestEndTurn();
                return;
            }

            EndTurn();
        }

        // Applies the authoritative turn/points from a replicated snapshot.
        public void NetworkApplyState(CreatureSide active, int playerPointsValue, int opponentPointsValue)
        {
            bool sideChanged = activeSide != active;
            activeSide = active;
            if (sideChanged)
                turnStartTime = Time.unscaledTime;
            SetPoints(CreatureSide.Player, playerPointsValue);
            SetPoints(CreatureSide.Opponent, opponentPointsValue);
            RefreshLabels();

            if (hand != null)
                hand.RefreshInteractivity();
        }

        // Host-only: advances to the next side, awards points and lets listeners
        // (modifier ticks) react. Combat is resolved by the caller beforehand.
        public void NetworkAdvanceTurn(CreatureSide newActiveSide)
        {
            activeSide = newActiveSide;
            turnStartTime = Time.unscaledTime;
            AwardTurnStartPoints(newActiveSide);
            RefreshLabels();

            if (hand != null)
                hand.RefreshInteractivity();

            TurnBegan?.Invoke();
        }

        public bool IsActive(CreatureSide side)
        {
            return side == activeSide;
        }

        // The turn clock uses unscaled time so it keeps ticking while the game
        // is running at a non-standard time scale. While paused, compensate the
        // start time on resume so the remaining time is preserved.
        public void SetPaused(bool value)
        {
            if (paused == value)
                return;

            paused = value;

            if (value)
            {
                pauseStartedAt = Time.unscaledTime;
                return;
            }

            // If a networked snapshot started a new turn while paused, give it a
            // fresh clock from the resume moment instead of eating the pause time.
            if (turnStartTime <= pauseStartedAt)
                turnStartTime += Time.unscaledTime - pauseStartedAt;
            else
                turnStartTime = Time.unscaledTime;
        }

        public int GetPoints(CreatureSide side)
        {
            return side == CreatureSide.Player ? playerPoints : opponentPoints;
        }

        public bool CanAfford(CreatureSide side, int cost)
        {
            return cost >= 0 && GetPoints(side) >= cost;
        }

        public bool Spend(CreatureSide side, int cost)
        {
            if (side != activeSide || cost < 0 || GetPoints(side) < cost)
                return false;

            SetPoints(side, GetPoints(side) - cost);
            RefreshLabels();
            return true;
        }

        public void EndTurn()
        {
            if (paused || turnTransitioning)
                return;

            if (IsMatchOver)
                return;

            if (Time.time < endTurnLockedUntil)
                return;

            endTurnLockedUntil = Time.time + endTurnDebounceSeconds;
            StartCoroutine(EndTurnRoutine());
        }

        // Resolves combat (one lane at a time) before handing the turn over, so the
        // next player can't act while the lanes are still fighting.
        private IEnumerator EndTurnRoutine()
        {
            turnTransitioning = true;
            try
            {
                bool resolvesCombat = !openingTurn && activeSide == CreatureSide.Opponent;
                if (resolvesCombat && combat != null)
                    yield return combat.ResolveCombatRoundRoutine();

                if (IsMatchOver)
                {
                    RefreshLabels();
                    yield break;
                }

                openingTurn = false;

                activeSide = activeSide == CreatureSide.Player ? CreatureSide.Opponent : CreatureSide.Player;
                turnStartTime = Time.unscaledTime;
                AwardTurnStartPoints(activeSide);
                RefreshLabels();

                if (hand != null)
                    hand.RefreshInteractivity();

                DrawCardForActiveSide();

                TurnBegan?.Invoke();
            }
            finally
            {
                turnTransitioning = false;
            }
        }

        private void Update()
        {
            if (paused || IsMatchOver)
                return;

            if (turnTransitioning)
                return;

            if (turnTimeLimitSeconds <= 0f)
                return;

            float elapsed = Time.unscaledTime - turnStartTime;
            if (elapsed >= turnTimeLimitSeconds)
            {
                if (NetworkSession.IsNetworked)
                {
                    if (NetworkSession.LocalSide == activeSide && NetworkSession.Intents != null)
                        NetworkSession.Intents.RequestEndTurn();
                }
                else
                {
                    EndTurn();
                }
                return;
            }

            if (activeSideLabel != null)
            {
                float remaining = turnTimeLimitSeconds - elapsed;
                if (remaining <= countdownWarningSeconds)
                {
                    int seconds = Mathf.CeilToInt(remaining);
                    if (seconds < 0) seconds = 0;
                    activeSideLabel.text = seconds.ToString();
                }
                else if (!string.IsNullOrEmpty(normalTurnLabelText))
                {
                    activeSideLabel.text = normalTurnLabelText;
                }
            }
        }

        private void DrawCardForActiveSide()
        {
            DeckController deck = activeSide == CreatureSide.Player ? playerDeck : opponentDeck;
            if (deck != null)
                deck.DrawToHand();
        }

        private void SetPoints(CreatureSide side, int value)
        {
            value = Mathf.Clamp(value, 0, maxSummonPoints);
            if (side == CreatureSide.Player)
                playerPoints = value;
            else
                opponentPoints = value;
        }

        private void GainPoints(CreatureSide side, int amount)
        {
            if (side == CreatureSide.Player)
                playerPoints = Mathf.Clamp(playerPoints + amount, 0, maxSummonPoints);
            else
                opponentPoints = Mathf.Clamp(opponentPoints + amount, 0, maxSummonPoints);
        }

        private bool HasStartedFirstTurn(CreatureSide side)
        {
            return side == CreatureSide.Player ? playerStartedFirstTurn : opponentStartedFirstTurn;
        }

        private void MarkStartedFirstTurn(CreatureSide side)
        {
            if (side == CreatureSide.Player)
                playerStartedFirstTurn = true;
            else
                opponentStartedFirstTurn = true;
        }

        private void AwardTurnStartPoints(CreatureSide side)
        {
            if (HasStartedFirstTurn(side))
            {
                GainPoints(side, summonPointsPerTurn);
                return;
            }

            MarkStartedFirstTurn(side);
        }

        private void RefreshLabels()
        {
            if (activeSideLabel != null)
            {
                if (NetworkSession.IsNetworked)
                    normalTurnLabelText = activeSide == NetworkSession.LocalSide ? "Your Turn" : "Enemy Turn";
                else
                    normalTurnLabelText = activeSide == CreatureSide.Player ? "Player's Turn" : "Opponent's Turn";
                activeSideLabel.text = normalTurnLabelText;
            }

            if (summonPointsLabel != null)
            {
                CreatureSide shown = NetworkSession.IsNetworked ? NetworkSession.LocalSide : CreatureSide.Player;
                summonPointsLabel.text = "Summon Points: " + GetPoints(shown);
            }
        }
    }
}
