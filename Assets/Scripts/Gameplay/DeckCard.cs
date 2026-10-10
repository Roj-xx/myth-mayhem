using UnityEngine;

namespace MythMayhem.Gameplay
{
    public enum CardType
    {
        Creature,
        Support,
        Building
    }

    [System.Serializable]
    public class DeckCard
    {
        [SerializeField] private CardType type = CardType.Creature;
        [SerializeField] private CreatureDefinition creatureDefinition;

        public CardType Type => type;
        public CreatureDefinition CreatureDefinition => creatureDefinition;
    }
}