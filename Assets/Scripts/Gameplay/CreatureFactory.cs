using UnityEngine;
using MythMayhem.Audio;

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
        [SerializeField] private Vector3 spawnScale = Vector3.one;
        [SerializeField] private Vector3 spawnRotationEuler = Vector3.zero;
        [SerializeField] private bool faceOpponent = true;
        [SerializeField] private float spawnHeightOffset = 0.1f;

        [Header("Animation")]
        [Tooltip("Drives spawn pop-in / attack lunge / death topple in code. Use while the imported FBX clips contain no baked motion; turn off once real animations are exported.")]
        [SerializeField] private bool proceduralFallback = true;

        [Header("Tinting")]
        [Tooltip("Only the placeholder fallback is tinted; real creature prefabs keep their own textures.")]
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
            creature.transform.rotation = SpawnRotation(side);
            creature.transform.localScale = spawnScale;

            if (usingPlaceholder && fallbackMaterial != null)
                ApplyFallbackMaterial(creature);

            if (usingPlaceholder && tintByLane)
                ApplyLaneTint(creature, laneIndex);

            CreatureInstance instance = creature.GetComponent<CreatureInstance>();
            if (instance == null)
                instance = creature.AddComponent<CreatureInstance>();

            CreatureVisualAnimator visualAnimator = creature.GetComponent<CreatureVisualAnimator>();
            if (proceduralFallback && visualAnimator == null)
                visualAnimator = creature.AddComponent<CreatureVisualAnimator>();
            if (visualAnimator != null)
                visualAnimator.Bind(creature);

            instance.Initialize(definition, side, laneIndex);

            if (showHealthBars)
                CreatureHealthBar.Create(instance, healthBarScale, healthBarSize, healthBarHeightOffset);

            AudioManager.PlaySpawnSound();
            return creature;
        }

        private Quaternion SpawnRotation(CreatureSide side)
        {
            Quaternion rotation = Quaternion.Euler(spawnRotationEuler);
            if (faceOpponent && side == CreatureSide.Opponent)
                rotation *= Quaternion.Euler(0f, 180f, 0f);

            return rotation;
        }

        private static GameObject CreatePlaceholder()
        {
            GameObject creature = new GameObject("CreaturePlaceholder");

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Collider collider = body.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            body.transform.SetParent(creature.transform, false);
            body.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f);

            return creature;
        }

        private void ApplyFallbackMaterial(GameObject creature)
        {
            foreach (Renderer renderer in creature.GetComponentsInChildren<Renderer>())
            {
                if (renderer != null)
                    renderer.sharedMaterial = fallbackMaterial;
            }
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
