using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MythMayhem.Board;

namespace MythMayhem.Gameplay
{
    public class OpponentController : MonoBehaviour
    {
        [SerializeField] private BoardManager board;
        [SerializeField] private CreatureFactory creatureFactory;
        [SerializeField] private CreatureDefinition opponentDefinition;
        [SerializeField] private Text statusLabel;
        [SerializeField] private TurnManager turnManager;

        private readonly GameObject[] laneOccupants = new GameObject[BoardManager.LaneCount];
        private bool warnedMissingReferences;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.oKey.wasPressedThisFrame)
                return;

            if (turnManager != null && !turnManager.IsActive(CreatureSide.Opponent))
                return;

            TrySummonOpponent();
        }

        private void TrySummonOpponent()
        {
            if (board == null || creatureFactory == null || opponentDefinition == null)
            {
                WarnMissingReferences();
                return;
            }

            int cost = opponentDefinition.Cost;
            if (turnManager != null && !turnManager.CanAfford(CreatureSide.Opponent, cost))
            {
                SetStatus("Opponent has no summon points");
                return;
            }

            for (int lane = 0; lane < laneOccupants.Length; lane++)
            {
                if (laneOccupants[lane] != null)
                    continue;

                Transform spawn = board.GetOpponentSpawn(lane);
                if (spawn == null)
                    continue;

                GameObject summoned = creatureFactory.Create(spawn, lane, opponentDefinition, CreatureSide.Opponent);
                if (summoned == null)
                    continue;

                laneOccupants[lane] = summoned;

                if (turnManager != null)
                    turnManager.Spend(CreatureSide.Opponent, cost);

                SetStatus("Opponent summoned " + opponentDefinition.DisplayName + " to red lane " + (lane + 1));
                return;
            }

            SetStatus("Opponent has no open red lanes");
        }

        private void WarnMissingReferences()
        {
            if (warnedMissingReferences)
                return;

            warnedMissingReferences = true;
            Debug.LogWarning("OpponentController is missing required references. Assign Board, Creature Factory, and Opponent Definition on the GameDirector component.");
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
