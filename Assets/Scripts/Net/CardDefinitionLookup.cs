using System.Collections.Generic;

namespace MythMayhem.Gameplay
{
    // Resolves a replicated card name back to its definition. Both peers load the
    // same fixed decks, so scanning the deck pools yields an identical map.
    public class CardDefinitionLookup
    {
        private readonly System.Collections.Generic.Dictionary<string, CreatureDefinition> creatures = new System.Collections.Generic.Dictionary<string, CreatureDefinition>();
        private readonly System.Collections.Generic.Dictionary<string, SupportCardDefinition> supports = new System.Collections.Generic.Dictionary<string, SupportCardDefinition>();
        private readonly System.Collections.Generic.Dictionary<string, BuildingDefinition> buildings = new System.Collections.Generic.Dictionary<string, BuildingDefinition>();

        public void Build(params DeckController[] decks)
        {
            creatures.Clear();
            supports.Clear();
            buildings.Clear();

            if (decks == null)
                return;

            foreach (DeckController deck in decks)
            {
                if (deck == null || deck.CardPool == null)
                    continue;

                for (int i = 0; i < deck.CardPool.Count; i++)
                {
                    DeckCard card = deck.CardPool[i];
                    if (card == null)
                        continue;

                    switch (card.Type)
                    {
                        case CardType.Support:
                            if (card.SupportDefinition != null && !supports.ContainsKey(card.SupportDefinition.DisplayName))
                                supports.Add(card.SupportDefinition.DisplayName, card.SupportDefinition);
                            break;
                        case CardType.Building:
                            if (card.BuildingDefinition != null && !buildings.ContainsKey(card.BuildingDefinition.DisplayName))
                                buildings.Add(card.BuildingDefinition.DisplayName, card.BuildingDefinition);
                            break;
                        default:
                            if (card.CreatureDefinition != null && !creatures.ContainsKey(card.CreatureDefinition.DisplayName))
                                creatures.Add(card.CreatureDefinition.DisplayName, card.CreatureDefinition);
                            break;
                    }
                }
            }
        }

        public CreatureDefinition Creature(string name)
        {
            if (name != null && creatures.TryGetValue(name, out CreatureDefinition definition))
                return definition;
            return null;
        }

        public SupportCardDefinition Support(string name)
        {
            if (name != null && supports.TryGetValue(name, out SupportCardDefinition definition))
                return definition;
            return null;
        }

        public BuildingDefinition Building(string name)
        {
            if (name != null && buildings.TryGetValue(name, out BuildingDefinition definition))
                return definition;
            return null;
        }
    }
}
