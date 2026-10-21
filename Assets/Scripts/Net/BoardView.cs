using UnityEngine;
using MythMayhem.Audio;
using MythMayhem.Board;
using MythMayhem.Gameplay;

namespace MythMayhem.Net
{
    // Rebuilds the visible board so it matches an authoritative snapshot. Used by
    // the joining client; the host already owns the live board.
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private CardPlayController cardPlay;
        [SerializeField] private int laneCount = 3;

        private bool[] knownCreaturePresence = new bool[BoardSnapshot.SideCount * BoardManager.LaneCount];
        private bool[] knownBuildingPresence = new bool[BoardSnapshot.SideCount * BoardManager.LaneCount];

        public void Apply(BoardSnapshot snapshot, CardDefinitionLookup lookup)
        {
            if (cardPlay == null || lookup == null)
                return;

            bool hasCreatures = snapshot.Creatures != null;
            bool hasBuildings = snapshot.Buildings != null;

            for (int sideIndex = 0; sideIndex < BoardSnapshot.SideCount; sideIndex++)
            {
                CreatureSide side = (CreatureSide)sideIndex;

                for (int lane = 0; lane < laneCount; lane++)
                {
                    int index = BoardSnapshot.Index(side, lane, laneCount);

                    if (hasCreatures && index < snapshot.Creatures.Length)
                        ApplyCreature(snapshot.Creatures[index], lookup, side, lane, index);

                    if (hasBuildings && index < snapshot.Buildings.Length)
                        ApplyBuilding(snapshot.Buildings[index], lookup, side, lane, index);
                }
            }
        }

        private void ApplyCreature(CreatureNetState state, CardDefinitionLookup lookup, CreatureSide side, int lane, int index)
        {
            CreatureInstance existing = cardPlay.GetCreature(side, lane);
            bool present = state.Present && state.Health > 0;

            if (!present)
            {
                if (NetworkSession.IsClient && knownCreaturePresence[index])
                    AudioManager.PlayDeathSound();
                if (existing != null)
                    cardPlay.RemoveCreature(side, lane);
                knownCreaturePresence[index] = false;
                return;
            }

            if (existing == null)
            {
                CreatureDefinition definition = lookup.Creature(state.CardName);
                if (definition != null)
                    existing = cardPlay.SpawnNetworkCreature(definition, side, lane, state.Health, state.AttackMod, state.ModTurns);
            }
            else
            {
                existing.SyncState(state.Health, state.AttackMod, state.ModTurns);
            }

            knownCreaturePresence[index] = existing != null;
        }

        private void ApplyBuilding(BuildingNetState state, CardDefinitionLookup lookup, CreatureSide side, int lane, int index)
        {
            BuildingInstance existing = cardPlay.GetBuilding(side, lane);
            bool present = state.Present && state.Health > 0;

            if (!present)
            {
                if (NetworkSession.IsClient && knownBuildingPresence[index])
                    AudioManager.PlayDeathSound();
                if (existing != null)
                    cardPlay.RemoveBuilding(side, lane);
                knownBuildingPresence[index] = false;
                return;
            }

            if (existing == null)
            {
                BuildingDefinition definition = lookup.Building(state.CardName);
                if (definition != null)
                    existing = cardPlay.SpawnNetworkBuilding(definition, side, lane, state.Health);
            }
            else
            {
                existing.RestoreHealth(state.Health);
            }

            knownBuildingPresence[index] = existing != null;
        }
    }
}
