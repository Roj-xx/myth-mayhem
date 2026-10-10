using UnityEngine;
using UnityEngine.UI;
using MythMayhem.UI;

namespace MythMayhem.Gameplay
{
    public class TurnManager : MonoBehaviour
    {
        [SerializeField] private int startingSummonPoints = 5;
        [SerializeField] private int summonPointsPerTurn = 2;
        [SerializeField] private float endTurnDebounceSeconds = 0.5f;
        [SerializeField] private CombatController combat;
        [SerializeField] private Text activeSideLabel;
        [SerializeField] private Text summonPointsLabel;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private CardHandController hand;
        [SerializeField] private DeckController playerDeck;
        [SerializeField] private DeckController opponentDeck;

        private CreatureSide activeSide = CreatureSide.Player;
        private int playerPoints;
        private int opponentPoints;
        private bool openingTurn;
        private float endTurnLockedUntil;
        private bool playerStartedFirstTurn;
        private bool opponentStartedFirstTurn;

        public CreatureSide ActiveSide => activeSide;

        private void Start()
        {
            activeSide = CreatureSide.Player;
            openingTurn = true;
            playerStartedFirstTurn = true;
            opponentStartedFirstTurn = false;
            SetPoints(CreatureSide.Player, startingSummonPoints);
            SetPoints(CreatureSide.Opponent, startingSummonPoints);

            if (endTurnButton != null)
                endTurnButton.onClick.AddListener(EndTurn);

            RefreshLabels();

            if (hand != null)
                hand.RefreshInteractivity();

            if (playerDeck != null)
                playerDeck.DealInitialHand();

            if (opponentDeck != null)
                opponentDeck.DealInitialHand();
        }

        private void OnDestroy()
        {
            if (endTurnButton != null)
                endTurnButton.onClick.RemoveListener(EndTurn);
        }

        public bool IsActive(CreatureSide side)
        {
            return side == activeSide;
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
            if (Time.time < endTurnLockedUntil)
                return;

            endTurnLockedUntil = Time.time + endTurnDebounceSeconds;

            bool resolvesCombat = !openingTurn && activeSide == CreatureSide.Opponent;
            if (resolvesCombat && combat != null)
                combat.ResolveCombatRound();

            openingTurn = false;

            activeSide = activeSide == CreatureSide.Player ? CreatureSide.Opponent : CreatureSide.Player;
            AwardTurnStartPoints(activeSide);
            RefreshLabels();

            if (hand != null)
                hand.RefreshInteractivity();

            DrawCardForActiveSide();
        }

        private void DrawCardForActiveSide()
        {
            DeckController deck = activeSide == CreatureSide.Player ? playerDeck : opponentDeck;
            if (deck != null)
                deck.DrawToHand();
        }

        private void SetPoints(CreatureSide side, int value)
        {
            value = Mathf.Max(0, value);
            if (side == CreatureSide.Player)
                playerPoints = value;
            else
                opponentPoints = value;
        }

        private void GainPoints(CreatureSide side, int amount)
        {
            if (side == CreatureSide.Player)
                playerPoints = Mathf.Max(0, playerPoints + amount);
            else
                opponentPoints = Mathf.Max(0, opponentPoints + amount);
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
                activeSideLabel.text = activeSide == CreatureSide.Player ? "Player's Turn" : "Opponent's Turn";

            if (summonPointsLabel != null)
                summonPointsLabel.text = "Summon Points: " + GetPoints(activeSide);
        }
    }
}
