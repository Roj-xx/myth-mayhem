using UnityEngine;

namespace MythMayhem.Gameplay
{
    public class CreatureFactory : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly Color[] LaneColors =
        {
            new Color(0.35f, 0.90f, 0.50f, 1f),
            new Color(0.95f, 0.75f, 0.30f, 1f),
            new Color(0.50f, 0.70f, 1.00f, 1f),
        };

        [Header("Prefab")]
        [SerializeField] private GameObject creaturePrefab;

        [Header("Placeholder Fallback")]
        [SerializeField] private Material fallbackMaterial;

        public Material FallbackMaterial => fallbackMaterial;

        [Header("Placement")]
        [SerializeField] private Vector3 spawnScale = new Vector3(0.8f, 0.9f, 0.8f);
        [SerializeField] private Vector3 spawnRotationEuler = Vector3.zero;
        [SerializeField] private float spawnHeightOffset = 1.15f;

        [Header("Tinting")]
        [SerializeField] private bool tintByLane = true;

        [Header("Health Bar")]
        [SerializeField] private bool showHealthBars = true;
        [SerializeField] private float healthBarScale = 1f;
        [SerializeField] private Vector2 healthBarSize = new Vector2(100f, 22f);
        [SerializeField] private float healthBarHeightOffset = 1.3f;

        public GameObject Create(Transform spawnPoint, int laneIndex, CreatureDefinition definition, CreatureSide side)
        {
            GameObject prefab = definition != null && definition.Prefab != null ? definition.Prefab : creaturePrefab;
            bool usingPlaceholder = prefab == null;
            GameObject creature = usingPlaceholder ? CreatePlaceholder() : Instantiate(prefab);
            creature.name = definition != null ? definition.DisplayName : "Aswang_Lane" + (laneIndex + 1);

            Transform parent = spawnPoint.parent != null ? spawnPoint.parent : spawnPoint;
            creature.transform.SetParent(parent, false);
            creature.transform.position = spawnPoint.position + Vector3.up * spawnHeightOffset;
            creature.transform.rotation = Quaternion.Euler(spawnRotationEuler);
            creature.transform.localScale = spawnScale;

            if (usingPlaceholder && fallbackMaterial != null)
                ApplyFallbackMaterial(creature);

            if (tintByLane)
                ApplyLaneTint(creature, laneIndex);

            CreatureInstance instance = creature.GetComponent<CreatureInstance>();
            if (instance == null)
                instance = creature.AddComponent<CreatureInstance>();
            instance.Initialize(definition, side, laneIndex);

            if (showHealthBars)
                CreatureHealthBar.Create(instance, healthBarScale, healthBarSize, healthBarHeightOffset);

            return creature;
        }

        private static GameObject CreatePlaceholder()
        {
            GameObject creature = GameObject.CreatePrimitive(PrimitiveType.Capsule);

            Collider collider = creature.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            return creature;
        }

        private void ApplyFallbackMaterial(GameObject creature)
        {
            Renderer renderer = creature.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = fallbackMaterial;
        }

        private void ApplyLaneTint(GameObject creature, int laneIndex)
        {
            Color color = LaneColors[laneIndex % LaneColors.Length];
            Renderer[] renderers = creature.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                Material material = renderer.sharedMaterial;
                if (material == null || !material.HasProperty(BaseColorId))
                    continue;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, color);
                renderer.SetPropertyBlock(block);
            }
        }
    }
}
