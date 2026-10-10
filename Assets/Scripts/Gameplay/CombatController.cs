using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using MythMayhem.Board;

namespace MythMayhem.Gameplay
{
    public class CombatController : MonoBehaviour
    {
        [SerializeField] private SummonController playerController;
        [SerializeField] private OpponentController opponentController;
        [SerializeField] private Text statusLabel;

        [Header("Automatic Combat")]
        [SerializeField] private float combatInterval = 0f;

        private float combatTimer;
        private bool warnedMissingReferences;

        private void Update()
        {
            HandleManualCombat();
            HandleAutomaticCombat();
        }

        private void HandleManualCombat()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                ResolveRound(true);
        }

        private void HandleAutomaticCombat()
        {
            if (combatInterval <= 0f)
                return;

            combatTimer += Time.deltaTime;
            if (combatTimer < combatInterval)
                return;

            combatTimer = 0f;
            ResolveRound(false);
        }

        public void ResolveCombatRound()
        {
            ResolveRound(false);
        }

        private void ResolveRound(bool announceWhenEmpty)
        {
            if (playerController == null || opponentController == null)
            {
                WarnMissingReferences();
                return;
            }

            int clashes = 0;

            for (int lane = 0; lane < BoardManager.LaneCount; lane++)
            {
                CreatureInstance player = playerController.GetLaneOccupant(lane);
                CreatureInstance opponent = opponentController.GetLaneOccupant(lane);

                if (player == null || opponent == null || !player.IsAlive || !opponent.IsAlive)
                    continue;

                int playerDamage = player.Attack;
                int opponentDamage = opponent.Attack;

                player.TakeDamage(opponentDamage);
                opponent.TakeDamage(playerDamage);
                clashes++;
            }

            if (clashes > 0)
                SetStatus("Combat resolved in " + clashes + " lane(s)");
            else if (announceWhenEmpty)
                SetStatus("No opposing creatures to fight");
        }

        private void WarnMissingReferences()
        {
            if (warnedMissingReferences)
                return;

            warnedMissingReferences = true;
            Debug.LogWarning("CombatController is missing required references. Assign the Summon Controller and Opponent Controller on the GameDirector component.");
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
            else
                Debug.Log("[Combat] " + message);
        }
    }
}
