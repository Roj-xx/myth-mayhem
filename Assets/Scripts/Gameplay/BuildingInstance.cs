using UnityEngine;
using MythMayhem.Audio;

namespace MythMayhem.Gameplay
{
    public class BuildingInstance : MonoBehaviour, IHealthTarget
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly Color PlayerColor = new Color(0.25f, 0.45f, 1.00f, 1f);
        private static readonly Color OpponentColor = new Color(0.95f, 0.25f, 0.30f, 1f);
        private const float SpawnHeightOffset = 0.1f;

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

            GameObject prefab = definition.Prefab;
            bool usingPlaceholder = prefab == null;
            GameObject building = usingPlaceholder ? CreatePlaceholder() : Instantiate(prefab);

            building.name = definition.DisplayName + "_Lane" + (lane + 1);
            if (parent != null)
                building.transform.SetParent(parent, false);
            building.transform.position = basePosition + Vector3.up * SpawnHeightOffset;
            building.transform.rotation = SpawnRotation(side);
            building.transform.localScale = Vector3.one;

            if (usingPlaceholder)
            {
                if (fallbackMaterial != null)
                    ApplyFallbackMaterial(building, fallbackMaterial);
                ApplyTint(building, side);
            }

            CreatureVisualAnimator visualAnimator = building.GetComponent<CreatureVisualAnimator>();
            if (visualAnimator == null)
                visualAnimator = building.AddComponent<CreatureVisualAnimator>();
            visualAnimator.Bind(building);

            BuildingInstance instance = building.GetComponent<BuildingInstance>();
            if (instance == null)
                instance = building.AddComponent<BuildingInstance>();
            instance.Configure(definition, side, lane);
            return instance;
        }

        // Buildings face the enemy side, matching how summoned creatures are oriented.
        private static Quaternion SpawnRotation(CreatureSide side)
        {
            return side == CreatureSide.Opponent ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        }

        private static GameObject CreatePlaceholder()
        {
            GameObject building = new GameObject("BuildingPlaceholder");

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Collider collider = body.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            body.transform.SetParent(building.transform, false);
            body.transform.localScale = new Vector3(0.9f, 0.85f, 0.9f);
            body.transform.localPosition = new Vector3(0f, 0.425f, 0f);
            return building;
        }

        private static void ApplyFallbackMaterial(GameObject building, Material fallbackMaterial)
        {
            foreach (Renderer renderer in building.GetComponentsInChildren<Renderer>())
            {
                if (renderer != null)
                    renderer.sharedMaterial = fallbackMaterial;
            }
        }

        private static void ApplyTint(GameObject building, CreatureSide side)
        {
            Color color = side == CreatureSide.Player ? PlayerColor : OpponentColor;
            foreach (Renderer renderer in building.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty(BaseColorId))
                    continue;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(block);
            }
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
                AudioManager.PlayDeathSound();
                Debug.Log("[Building] " + (Definition != null ? Definition.DisplayName : "Building")
                    + " in lane " + (Lane + 1) + " was destroyed.");
                CreatureVisualAnimator visualAnimator = GetComponent<CreatureVisualAnimator>();
                if (visualAnimator != null)
                {
                    visualAnimator.PlayDeath();
                    Destroy(gameObject, 2f);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnDestroy()
        {
            if (HealthBar != null)
                Destroy(HealthBar.gameObject);
        }
    }
}