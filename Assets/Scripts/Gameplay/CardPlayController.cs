using UnityEngine;
using MythMayhem.Board;

namespace MythMayhem.Gameplay
{
    public class CardPlayController : MonoBehaviour
    {
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private SummonController summonController;
        [SerializeField] private OpponentController opponentController;
        [SerializeField] private CreatureFactory creatureFactory;
        [SerializeField] private BoardManager board;

        [Header("Building Health Bars")]
        [SerializeField] private bool showBuildingHealthBars = true;
        [SerializeField] private float buildingHealthBarScale = 1f;
        [SerializeField] private Vector2 buildingHealthBarSize = new Vector2(100f, 22f);
        [SerializeField] private float buildingHealthBarHeightOffset = 1.2f;

        [Header("Building Placement")]
        [Tooltip("How far behind the lane's creature spawn a building is placed (toward the owner's side).")]
        [SerializeField] private float buildingBackOffset = 1.3f;

        private readonly GameObject[] playerBuildings = new GameObject[BoardManager.LaneCount];
        private readonly GameObject[] opponentBuildings = new GameObject[BoardManager.LaneCount];

        public bool HasBuilding(CreatureSide side, int lane)
        {
            return GetBuilding(side, lane) != null;
        }

        public BuildingInstance GetBuilding(CreatureSide side, int lane)
        {
            if (lane < 0 || lane >= BoardManager.LaneCount)
                return null;

            GameObject[] buildings = BuildingArray(side);
            GameObject building = buildings[lane];
            if (building == null)
                return null;

            BuildingInstance instance = building.GetComponent<BuildingInstance>();
            return instance != null && instance.IsAlive ? instance : null;
        }

        public CreatureInstance GetCreature(CreatureSide side, int lane)
        {
            if (lane < 0 || lane >= BoardManager.LaneCount)
                return null;

            if (side == CreatureSide.Player)
                return summonController != null ? summonController.GetLaneOccupant(lane) : null;

            return opponentController != null ? opponentController.GetLaneOccupant(lane) : null;
        }

        public bool HasLivingCreature(CreatureSide side, int lane)
        {
            CreatureInstance creature = GetCreature(side, lane);
            return creature != null && creature.IsAlive;
        }

        // A lane can hold one creature AND one building. These two rules are checked
        // independently so the two card types no longer block each other.
        public bool IsLaneOpenForCreature(CreatureSide side, int lane)
        {
            if (lane < 0 || lane >= BoardManager.LaneCount)
                return false;

            return !HasLivingCreature(side, lane);
        }

        public bool IsLaneOpenForBuilding(CreatureSide side, int lane)
        {
            if (lane < 0 || lane >= BoardManager.LaneCount)
                return false;

            return !HasBuilding(side, lane);
        }

        public int FindOpenLaneForCreature(CreatureSide side)
        {
            for (int lane = 0; lane < BoardManager.LaneCount; lane++)
            {
                if (IsLaneOpenForCreature(side, lane) && GetSpawn(side, lane) != null)
                    return lane;
            }

            return -1;
        }

        public int FindOpenLaneForBuilding(CreatureSide side)
        {
            for (int lane = 0; lane < BoardManager.LaneCount; lane++)
            {
                if (IsLaneOpenForBuilding(side, lane) && GetSpawn(side, lane) != null)
                    return lane;
            }

            return -1;
        }

        public Transform GetSpawn(CreatureSide side, int lane)
        {
            if (board == null || lane < 0 || lane >= BoardManager.LaneCount)
                return null;

            return side == CreatureSide.Player ? board.GetPlayerSpawn(lane) : board.GetOpponentSpawn(lane);
        }

        // Shared creature-summon rule: validates the lane, spawns through the factory
        // and registers the occupant on the correct side's controller.
        public CreatureInstance TrySummonCreature(CreatureSide side, CreatureDefinition definition, int lane, out string failure)
        {
            failure = null;

            if (definition == null)
            {
                failure = "No creature definition";
                return null;
            }

            if (!IsLaneOpenForCreature(side, lane))
            {
                failure = "Lane " + (lane + 1) + " already has a creature";
                return null;
            }

            Transform spawn = GetSpawn(side, lane);
            if (spawn == null || creatureFactory == null)
            {
                failure = "Cannot summon right now";
                return null;
            }

            GameObject summoned = creatureFactory.Create(spawn, lane, definition, side);
            if (summoned == null)
            {
                failure = "Cannot summon right now";
                return null;
            }

            SetLaneOccupant(side, lane, summoned);
            return summoned.GetComponent<CreatureInstance>();
        }

        public void SetLaneOccupant(CreatureSide side, int lane, GameObject occupant)
        {
            if (side == CreatureSide.Player)
            {
                if (summonController != null)
                    summonController.SetLaneOccupant(lane, occupant);
            }
            else if (opponentController != null)
            {
                opponentController.SetLaneOccupant(lane, occupant);
            }
        }

        // A building only buffs the creature standing in its own lane.
        public int GetLaneAttackBonus(CreatureSide side, int lane)
        {
            BuildingInstance building = GetBuilding(side, lane);
            if (building == null || building.Definition == null)
                return 0;

            return building.Definition.EffectType == BuildingEffectType.FriendlyAttackBonus
                ? building.Definition.Magnitude
                : 0;
        }

        // Direction pointing from a side's own spawn toward the enemy's spawn, used to
        // seat a building behind the creature in the same lane.
        public Vector3 GetForwardDirection(CreatureSide side, int lane)
        {
            Transform own = GetSpawn(side, lane);
            Transform enemy = GetSpawn(side == CreatureSide.Player ? CreatureSide.Opponent : CreatureSide.Player, lane);

            if (own == null || enemy == null)
                return side == CreatureSide.Player ? Vector3.forward : Vector3.back;

            Vector3 forward = enemy.position - own.position;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                return side == CreatureSide.Player ? Vector3.forward : Vector3.back;

            return forward.normalized;
        }

        public bool TryApplySupport(CreatureSide side, SupportCardDefinition support, CreatureInstance target)
        {
            if (support == null || target == null || !target.IsAlive)
                return false;

            switch (support.EffectType)
            {
                case SupportEffectType.FriendlyAttackBuff:
                    if (target.Side != side)
                        return false;
                    target.AddAttackModifier(support.Magnitude, support.DurationTurns);
                    return true;

                case SupportEffectType.FriendlyHeal:
                    if (target.Side != side)
                        return false;
                    target.Heal(support.Magnitude);
                    return true;

                case SupportEffectType.EnemyAttackDebuff:
                    if (target.Side == side)
                        return false;
                    target.AddAttackModifier(-support.Magnitude, support.DurationTurns);
                    return true;

                default:
                    return false;
            }
        }

        public bool TryPlaceBuilding(CreatureSide side, BuildingDefinition building, int lane)
        {
            if (building == null || lane < 0 || lane >= BoardManager.LaneCount)
                return false;

            if (!IsLaneOpenForBuilding(side, lane))
                return false;

            Transform spawn = GetSpawn(side, lane);
            if (spawn == null)
                return false;

            Transform buildingSpawn = side == CreatureSide.Player
                ? (board != null ? board.GetPlayerBuildingSpawn(lane) : null)
                : (board != null ? board.GetOpponentBuildingSpawn(lane) : null);

            Transform parent = spawn.parent != null ? spawn.parent : spawn;
            Vector3 basePosition = buildingSpawn != null
                ? buildingSpawn.position
                : spawn.position - GetForwardDirection(side, lane) * buildingBackOffset;

            Material fallback = creatureFactory != null ? creatureFactory.FallbackMaterial : null;
            BuildingInstance instance = BuildingInstance.Create(building, side, lane, parent, basePosition, fallback);
            if (instance == null)
                return false;

            if (showBuildingHealthBars)
                CreatureHealthBar.Create(instance, buildingHealthBarScale, buildingHealthBarSize, buildingHealthBarHeightOffset);

            BuildingArray(side)[lane] = instance.gameObject;
            return true;
        }

        // --- Networked view helpers -------------------------------------------------
        // These rebuild board pieces from a replicated snapshot. They deliberately
        // skip rule validation: the host has already validated and applied the play,
        // the client is only mirroring the result.

        public CreatureInstance SpawnNetworkCreature(CreatureDefinition definition, CreatureSide side, int lane, int health, int attackMod, int modTurns)
        {
            if (definition == null)
                return null;

            Transform spawn = GetSpawn(side, lane);
            if (spawn == null || creatureFactory == null)
                return null;

            GameObject spawned = creatureFactory.Create(spawn, lane, definition, side);
            if (spawned == null)
                return null;

            CreatureInstance instance = spawned.GetComponent<CreatureInstance>();
            if (instance != null)
                instance.RestoreState(definition, side, lane, health, attackMod, modTurns);

            SetLaneOccupant(side, lane, spawned);
            return instance;
        }

        public void RemoveCreature(CreatureSide side, int lane)
        {
            CreatureInstance creature = GetCreature(side, lane);
            if (creature != null)
                Destroy(creature.gameObject);

            SetLaneOccupant(side, lane, null);
        }

        public BuildingInstance SpawnNetworkBuilding(BuildingDefinition definition, CreatureSide side, int lane, int health)
        {
            if (definition == null)
                return null;

            RemoveBuilding(side, lane);
            if (!TryPlaceBuilding(side, definition, lane))
                return null;

            BuildingInstance instance = GetBuilding(side, lane);
            if (instance != null)
                instance.RestoreHealth(health);
            return instance;
        }

        public void RemoveBuilding(CreatureSide side, int lane)
        {
            GameObject[] buildings = BuildingArray(side);
            GameObject building = buildings[lane];
            if (building != null)
                Destroy(building);
            buildings[lane] = null;
        }

        private void OnEnable()
        {
            if (turnManager != null)
                turnManager.TurnBegan += OnTurnBegan;
        }

        private void OnDisable()
        {
            if (turnManager != null)
                turnManager.TurnBegan -= OnTurnBegan;
        }

        private void OnTurnBegan()
        {
            if (turnManager == null)
                return;

            // Clients receive creature state from snapshots; only the host ticks
            // attack-modifier durations as turns begin.
            if (NetworkSession.IsClient)
                return;

            TickModifiers(turnManager.ActiveSide);
        }

        private void TickModifiers(CreatureSide side)
        {
            for (int lane = 0; lane < BoardManager.LaneCount; lane++)
            {
                CreatureInstance creature = GetCreature(side, lane);
                if (creature != null)
                    creature.OnOwnerTurnBegan();
            }
        }

        private GameObject[] BuildingArray(CreatureSide side)
        {
            return side == CreatureSide.Player ? playerBuildings : opponentBuildings;
        }
    }
}