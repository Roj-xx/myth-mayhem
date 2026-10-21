using UnityEngine;

namespace MythMayhem.Gameplay
{
    public enum SupportEffectType
    {
        FriendlyAttackBuff,
        FriendlyHeal,
        EnemyAttackDebuff
    }

    [CreateAssetMenu(fileName = "SupportCardDefinition", menuName = "Myth Mayhem/Support Card Definition")]
    public class SupportCardDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Support";
        [SerializeField] private Sprite artwork;
        [SerializeField] private int cost = 1;
        [SerializeField] private SupportEffectType effectType = SupportEffectType.FriendlyAttackBuff;
        [SerializeField] private int magnitude = 5;
        [SerializeField] private int durationTurns = 2;
        [SerializeField] private string description = "";

        public string DisplayName => displayName;
        public Sprite Artwork => artwork;
        public int Cost => cost;
        public SupportEffectType EffectType => effectType;
        public int Magnitude => magnitude;
        public int DurationTurns => durationTurns;
        public string Description => description;
    }
}