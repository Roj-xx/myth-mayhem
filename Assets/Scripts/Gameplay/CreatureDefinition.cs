using UnityEngine;

namespace MythMayhem.Gameplay
{
    [CreateAssetMenu(fileName = "CreatureDefinition", menuName = "Myth Mayhem/Creature Definition")]
    public class CreatureDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Creature";
        [SerializeField] private GameObject prefab;
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int attack = 10;
        [SerializeField] private int cost = 1;

        public string DisplayName => displayName;
        public GameObject Prefab => prefab;
        public int MaxHealth => maxHealth;
        public int Attack => attack;
        public int Cost => cost;
    }
}
