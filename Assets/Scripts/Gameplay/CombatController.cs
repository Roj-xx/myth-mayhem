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
        [SerializeField] private CardPlayController cardPlay;
        [SerializeField] private MatchController matchController;
        [SerializeField] private Text statusLabel;

        [Header("Automatic Combat")]
        [SerializeField] private float combatInterval = 0f;

        [Header("Manual (debug) Combat")]
        [SerializeField] private bool enableManualCombat = true;
        [SerializeField] private float manualCombatCooldown = 0.5f;

        private float combatTimer;
        private float lastCombatTime = float.NegativeInfinity;
        private bool resolving;
        private bool warnedMissingReferences;

        private void Update()
        {
            // In a networked match only the host resolves combat; the debug/auto
            // paths are disabled so a client can never advance the board locally.
            if (NetworkSession.IsNetworked)
                return;

            HandleManualCombat();
            HandleAutomaticCombat();
        }

        private void HandleManualCombat()
        {
            if (!enableManualCombat)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
                return;

            if (Time.time - lastCombatTime < manualCombatCooldown)
                return;

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
            if (resolving)
                return;

            if (playerController == null || opponentController == null)
            {
                WarnMissingReferences();
                return;
            }

            resolving = true;
            try
            {
                ResolveLanes(announceWhenEmpty);
                lastCombatTime = Time.time;
            }
            finally
            {
                resolving = false;
            }
        }

        private void ResolveLanes(bool announceWhenEmpty)
        {
            int clashes = 0;
            int playerHeroDamage = 0;
            int opponentHeroDamage = 0;

            for (int lane = 0; lane < BoardManager.LaneCount; lane++)
            {
                CreatureInstance player = playerController.GetLaneOccupant(lane);
                CreatureInstance opponent = opponentController.GetLaneOccupant(lane);

                int playerBonus = cardPlay != null ? cardPlay.GetLaneAttackBonus(CreatureSide.Player, lane) : 0;
                int opponentBonus = cardPlay != null ? cardPlay.GetLaneAttackBonus(CreatureSide.Opponent, lane) : 0;

                bool playerAlive = player != null && player.IsAlive;
                bool opponentAlive = opponent != null && opponent.IsAlive;

                if (playerAlive && opponentAlive)
                {
                    int playerDamage = player.Attack + playerBonus;
                    int opponentDamage = opponent.Attack + opponentBonus;

                    player.TakeDamage(opponentDamage);
                    opponent.TakeDamage(playerDamage);
                    clashes++;
                    continue;
                }

                if (playerAlive)
                {
                    BuildingInstance enemyBuilding = cardPlay != null ? cardPlay.GetBuilding(CreatureSide.Opponent, lane) : null;
                    if (enemyBuilding != null && enemyBuilding.IsAlive)
                    {
                        enemyBuilding.TakeDamage(Mathf.Max(0, player.Attack + playerBonus));
                        clashes++;
                    }
                    else
                    {
                        opponentHeroDamage += Mathf.Max(0, player.Attack + playerBonus);
                    }

                    continue;
                }

                if (opponentAlive)
                {
                    BuildingInstance enemyBuilding = cardPlay != null ? cardPlay.GetBuilding(CreatureSide.Player, lane) : null;
                    if (enemyBuilding != null && enemyBuilding.IsAlive)
                    {
                        enemyBuilding.TakeDamage(Mathf.Max(0, opponent.Attack + opponentBonus));
                        clashes++;
                    }
                    else
                    {
                        playerHeroDamage += Mathf.Max(0, opponent.Attack + opponentBonus);
                    }
                }
            }

            if (matchController != null && (playerHeroDamage > 0 || opponentHeroDamage > 0))
                matchController.ApplyHeroDamageBoth(playerHeroDamage, opponentHeroDamage);

            if (playerHeroDamage > 0 || opponentHeroDamage > 0)
                SetStatus("Unblocked attackers deal " + opponentHeroDamage + " to the enemy and " + playerHeroDamage + " to you");
            else if (clashes > 0)
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
