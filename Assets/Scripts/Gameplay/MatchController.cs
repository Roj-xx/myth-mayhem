using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythMayhem.Gameplay
{
    public enum MatchOutcome
    {
        None,
        PlayerWin,
        OpponentWin,
        Draw
    }

    public class MatchController : MonoBehaviour
    {
        [SerializeField] private int maxHeroHealth = 100;
        [SerializeField] private bool buildRuntimeHud = true;

        private int playerHealth;
        private int opponentHealth;
        private MatchOutcome outcome = MatchOutcome.None;
        private HeroHud hud;

        public int PlayerHealth => playerHealth;
        public int OpponentHealth => opponentHealth;
        public int MaxHeroHealth => maxHeroHealth;
        public MatchOutcome Outcome => outcome;
        public bool IsMatchOver => outcome != MatchOutcome.None;

        public event Action<int, int> HeroHealthChanged;
        public event Action<MatchOutcome> MatchEnded;

        private void Awake()
        {
            playerHealth = Mathf.Max(1, maxHeroHealth);
            opponentHealth = Mathf.Max(1, maxHeroHealth);
        }

        private void Start()
        {
            if (buildRuntimeHud && hud == null)
                hud = HeroHud.Create(this);

            RefreshHud();
            HeroHealthChanged?.Invoke(playerHealth, opponentHealth);
        }

        public int GetHealth(CreatureSide side)
        {
            return side == CreatureSide.Player ? playerHealth : opponentHealth;
        }

        // Applies hero health from a replicated snapshot (no outcome evaluation:
        // the host tells us the outcome explicitly).
        public void SetHealth(int playerHealthValue, int opponentHealthValue)
        {
            playerHealth = Mathf.Clamp(playerHealthValue, 0, maxHeroHealth);
            opponentHealth = Mathf.Clamp(opponentHealthValue, 0, maxHeroHealth);
            RaiseHealthChanged();
        }

        public void ApplyHeroDamage(CreatureSide targetSide, int amount)
        {
            if (amount <= 0 || IsMatchOver)
                return;

            if (targetSide == CreatureSide.Player)
                playerHealth = Mathf.Max(0, playerHealth - amount);
            else
                opponentHealth = Mathf.Max(0, opponentHealth - amount);

            RaiseHealthChanged();
            EvaluateOutcome();
        }

        public void ApplyHeroDamageBoth(int playerDamage, int opponentDamage)
        {
            if (IsMatchOver)
                return;

            bool changed = false;
            if (playerDamage > 0)
            {
                playerHealth = Mathf.Max(0, playerHealth - playerDamage);
                changed = true;
            }

            if (opponentDamage > 0)
            {
                opponentHealth = Mathf.Max(0, opponentHealth - opponentDamage);
                changed = true;
            }

            if (!changed)
                return;

            RaiseHealthChanged();
            EvaluateOutcome();
        }

        private void EvaluateOutcome()
        {
            if (IsMatchOver)
                return;

            MatchOutcome result = MatchRules.EvaluateOutcome(playerHealth, opponentHealth);
            if (result == MatchOutcome.None)
                return;

            EndMatch(result);
        }

        public void EndMatch(MatchOutcome result)
        {
            if (IsMatchOver || result == MatchOutcome.None)
                return;

            outcome = result;
            hud?.ShowGameOver(result);
            MatchEnded?.Invoke(result);
        }

        public void Restart()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0)
                SceneManager.LoadScene(scene.buildIndex);
            else
                SceneManager.LoadScene(scene.name);
        }

        private void RaiseHealthChanged()
        {
            RefreshHud();
            HeroHealthChanged?.Invoke(playerHealth, opponentHealth);
        }

        // The seat's own health is shown as "you". The joining client plays the
        // Opponent side, so its HUD reads from the opposite field.
        private void RefreshHud()
        {
            if (hud == null)
                return;

            int localHealth = NetworkSession.IsClient ? opponentHealth : playerHealth;
            int enemyHealth = NetworkSession.IsClient ? playerHealth : opponentHealth;
            hud.Refresh(localHealth, enemyHealth, maxHeroHealth);
        }

        // Applies the host's authoritative result. The client reports its own side
        // as the winner, so player/opponent wins are inverted for display.
        public void NetworkApplyOutcome(MatchOutcome authoritative)
        {
            if (IsMatchOver || authoritative == MatchOutcome.None)
                return;

            MatchOutcome display = authoritative;
            if (NetworkSession.IsClient)
            {
                if (authoritative == MatchOutcome.PlayerWin)
                    display = MatchOutcome.OpponentWin;
                else if (authoritative == MatchOutcome.OpponentWin)
                    display = MatchOutcome.PlayerWin;
            }

            EndMatch(display);
        }
    }
}
