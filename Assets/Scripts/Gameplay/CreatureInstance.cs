using UnityEngine;
using MythMayhem.Audio;

namespace MythMayhem.Gameplay
{
    public enum CreatureSide
    {
        Player,
        Opponent
    }

    public class CreatureInstance : MonoBehaviour, IHealthTarget
    {
        public CreatureDefinition Definition { get; private set; }
        public CreatureSide Side { get; private set; }
        public int Lane { get; private set; }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public int Attack => BaseAttack + attackModifier;
        public int BaseAttack { get; private set; }
        public int AttackModifier => attackModifier;
        public int AttackModifierTurns => attackModifierTurns;
        public CreatureHealthBar HealthBar { get; set; }

        private int attackModifier;
        private int attackModifierTurns;
        private Animator animator;
        private CreatureVisualAnimator visualAnimator;
        private const float DeathAnimationDuration = 2f;
        private static readonly int AttackTrigger = Animator.StringToHash("Attack");
        private static readonly int DeathTrigger = Animator.StringToHash("Death");

        public bool IsAlive => CurrentHealth > 0;
        public string DisplayName => Definition != null ? Definition.DisplayName : name;

        public void Initialize(CreatureDefinition definition, CreatureSide side, int lane)
        {
            Definition = definition;
            Side = side;
            Lane = lane;
            MaxHealth = definition != null ? definition.MaxHealth : 0;
            BaseAttack = definition != null ? definition.Attack : 0;
            attackModifier = 0;
            attackModifierTurns = 0;
            CurrentHealth = MaxHealth;
            animator = GetComponentInChildren<Animator>();
            visualAnimator = GetComponent<CreatureVisualAnimator>();
        }

        // Rebuilds a creature from a replicated snapshot (used by joining clients).
        public void RestoreState(CreatureDefinition definition, CreatureSide side, int lane, int currentHealth, int modifierAmount, int modifierTurns)
        {
            Definition = definition;
            Side = side;
            Lane = lane;
            MaxHealth = definition != null ? definition.MaxHealth : Mathf.Max(1, currentHealth);
            CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
            BaseAttack = definition != null ? definition.Attack : 0;
            attackModifier = modifierAmount;
            attackModifierTurns = modifierTurns;
        }

        // Applies replicated health/modifier state to an already-spawned creature
        // (used by clients applying a BoardSnapshot).
        public void SyncState(int health, int modifierAmount, int modifierTurns)
        {
            CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
            attackModifier = modifierAmount;
            attackModifierTurns = modifierTurns;
        }

        public void AddAttackModifier(int delta, int turns)
        {
            if (turns <= 0)
                return;

            attackModifier = delta;
            attackModifierTurns = turns;
        }

        public void Heal(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        }

        public void OnOwnerTurnBegan()
        {
            if (attackModifierTurns <= 0)
                return;

            attackModifierTurns--;

            if (attackModifierTurns == 0)
                attackModifier = 0;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

            if (CurrentHealth == 0)
            {
                AudioManager.PlayDeathSound();
                PlayDeath();
            }
        }

        // Plays the creature's attack take; called by the combat resolver when it
        // attacks (whether into a creature, a building, or the enemy hero).
        public void PlayAttack()
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetTrigger(AttackTrigger);

            if (visualAnimator != null)
                visualAnimator.PlayAttack();
        }

        private void PlayDeath()
        {
            if (animator != null && animator.isActiveAndEnabled)
                animator.SetTrigger(DeathTrigger);

            if (visualAnimator != null)
                visualAnimator.PlayDeath();

            Destroy(gameObject, DeathAnimationDuration);
        }

        private void OnDestroy()
        {
            if (HealthBar != null)
                Destroy(HealthBar.gameObject);
        }
    }
}
