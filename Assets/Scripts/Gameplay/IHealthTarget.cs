namespace MythMayhem.Gameplay
{
    // Implemented by any runtime entity that should show a world-space health bar
    // (creatures and buildings).
    public interface IHealthTarget
    {
        int MaxHealth { get; }
        int CurrentHealth { get; }
        CreatureHealthBar HealthBar { get; set; }
    }
}
