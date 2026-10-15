using UnityEngine;

namespace MythMayhem.Gameplay
{
    public class BuildingInstance : MonoBehaviour, IHealthTarget
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color PlayerColor = new Color(0.25f, 0.45f, 1.00f, 1f);
        private static readonly Color OpponentColor = new Color(0.95f, 0.25f, 0.30f, 1f);
        private const float SpawnHeightOffset = 0.55f;

        public BuildingDefinition Definition { get; private set; }
        public CreatureSide Side { get; private set; }
        public int Lane { get; private set; }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public CreatureHealthBar HealthBar { get; set; }

        public bool IsAlive => CurrentHealth > 0;

        // Applies replicated health to an already-created building.
        public void RestoreHealth(int currentHealth)
        {
            CurrentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        }

        public static BuildingInstance Create(BuildingDefinition definition, CreatureSide side, int lane, Transform parent, Vector3 basePosition, Material fallbackMaterial)
        {
            if (definition == null)
                return null;

            GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider collider = building.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            building.name = definition.DisplayName + "_Lane" + (lane + 1);
            if (parent != null)
                building.transform.SetParent(parent, false);
            building.transform.position = basePosition + Vector3.up * SpawnHeightOffset;
            building.transform.rotation = Quaternion.identity;
            building.transform.localScale = new Vector3(0.9f, 0.85f, 0.9f);

            Renderer renderer = building.GetComponent<Renderer>();
            if (renderer != null && fallbackMaterial != null)
                renderer.sharedMaterial = fallbackMaterial;

            ApplyTint(building, side);

            BuildingInstance instance = building.AddComponent<BuildingInstance>();
            instance.Configure(definition, side, lane);
            return instance;
        }

        private static void ApplyTint(GameObject building, CreatureSide side)
        {
            Renderer renderer = building != null ? building.GetComponent<Renderer>() : null;
            if (renderer == null || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty(BaseColorId))
                return;

            Color color = side == CreatureSide.Player ? PlayerColor : OpponentColor;
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(block);
        }

        private void Configure(BuildingDefinition definition, CreatureSide side, int lane)
        {
            Definition = definition;
            Side = side;
            Lane = lane;
            MaxHealth = definition != null ? definition.MaxHealth : 0;
            CurrentHealth = MaxHealth;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
                return;

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            Debug.Log("[Building] " + (Definition != null ? Definition.DisplayName : "Building")
                + " in lane " + (Lane + 1) + " took " + amount + " damage (" + CurrentHealth + "/" + MaxHealth + ").");

            if (CurrentHealth == 0)
            {
                Debug.Log("[Building] " + (Definition != null ? Definition.DisplayName : "Building")
                    + " in lane " + (Lane + 1) + " was destroyed.");
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (HealthBar != null)
                Destroy(HealthBar.gameObject);
        }
    }
}