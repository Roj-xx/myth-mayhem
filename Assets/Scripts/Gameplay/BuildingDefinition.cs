using UnityEngine;

namespace MythMayhem.Gameplay
{
    public enum BuildingEffectType
    {
        LaneDefense,
        FriendlyAttackBonus
    }

    [CreateAssetMenu(fileName = "BuildingDefinition", menuName = "Myth Mayhem/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Building";
        [SerializeField] private int cost = 2;
        [SerializeField] private int maxHealth = 60;
        [SerializeField] private BuildingEffectType effectType = BuildingEffectType.LaneDefense;
        [SerializeField] private int magnitude = 0;
        [SerializeField] private string description = "";

        public string DisplayName => displayName;
        public int Cost => cost;
        public int MaxHealth => maxHealth;
        public BuildingEffectType EffectType => effectType;
        public int Magnitude => magnitude;
        public string Description => description;
    }
}