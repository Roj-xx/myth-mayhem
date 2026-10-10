using UnityEngine;

namespace MythMayhem.Gameplay
{
    public enum CreatureSide
    {
        Player,
        Opponent
    }

    public class CreatureInstance : MonoBehaviour
    {
        public CreatureDefinition Definition { get; private set; }
        public CreatureSide Side { get; private set; }
        public int Lane { get; private set; }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public int Attack { get; private set; }
        public CreatureHealthBar HealthBar { get; private set; }

        public bool IsAlive => CurrentHealth > 0;
        public string DisplayName => Definition != null ? Definition.DisplayName : name;

        public void Initialize(CreatureDefinition definition, CreatureSide side, int lane)
        {
            Definition = definition;
            Side = side;
            Lane = lane;
            MaxHealth = definition != null ? definition.MaxHealth : 0;
            Attack = definition != null ? definition.Attack : 0;
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);

            if (CurrentHealth == 0)
                Destroy(gameObject);
        }

        public void SetHealthBar(CreatureHealthBar healthBar)
        {
            HealthBar = healthBar;
        }

        private void OnDestroy()
        {
            if (HealthBar != null)
                Destroy(HealthBar.gameObject);
        }
    }
}
