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
        [SerializeField] private SupportCardDefinition supportDefinition;
        [SerializeField] private BuildingDefinition buildingDefinition;

        public CardType Type => type;
        public CreatureDefinition CreatureDefinition => creatureDefinition;
        public SupportCardDefinition SupportDefinition => supportDefinition;
        public BuildingDefinition BuildingDefinition => buildingDefinition;

        public bool HasValidDefinition
        {
            get
            {
                switch (type)
                {
                    case CardType.Support:
                        return supportDefinition != null;
                    case CardType.Building:
                        return buildingDefinition != null;
                    default:
                        return creatureDefinition != null;
                }
            }
        }

        public string DefinitionName
        {
            get
            {
                switch (type)
                {
                    case CardType.Support:
                        return supportDefinition != null ? supportDefinition.DisplayName : "Support";
                    case CardType.Building:
                        return buildingDefinition != null ? buildingDefinition.DisplayName : "Building";
                    default:
                        return creatureDefinition != null ? creatureDefinition.DisplayName : "Creature";
                }
            }
        }

        public int Cost
        {
            get
            {
                switch (type)
                {
                    case CardType.Support:
                        return supportDefinition != null ? supportDefinition.Cost : 0;
                    case CardType.Building:
                        return buildingDefinition != null ? buildingDefinition.Cost : 0;
                    default:
                        return creatureDefinition != null ? creatureDefinition.Cost : 0;
                }
            }
        }
    }
}