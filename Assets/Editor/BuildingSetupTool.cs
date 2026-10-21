using System;
using MythMayhem.Gameplay;
using UnityEditor;
using UnityEngine;

namespace MythMayhem.EditorTools
{
    // Turns the imported Meshy building FBXs into spawnable building prefabs.
    // Buildings do not attack; a CreatureVisualAnimator provides the procedural
    // spawn pop-in and death topple (its attack path is simply never triggered).
    //
    // Safe to re-run: existing materials/prefabs are reused and overwritten.
    public static class BuildingSetupTool
    {
        private const string CharactersRoot = "Assets/Art/Characters";
        private const string CardsRoot = "Assets/Data/Cards";
        private const string PrefabsRoot = "Assets/Prefabs";
        private const string BuildingPrefabsFolder = PrefabsRoot + "/Buildings";
        private const string MaterialsRoot = "Assets/Materials";
        private const string BuildingMaterialsFolder = MaterialsRoot + "/Buildings";

        // Each entry maps a BuildingDefinition asset to its model and albedo texture.
        private struct BuildingSpec
        {
            public string Name;
            public string CardName;
            public string Folder;
            public string ModelFile;
            public string TexturePrefix;
            public float TargetHeight;

            public BuildingSpec(string name, string cardName, string folder, string modelFile, string texturePrefix, float targetHeight)
            {
                Name = name;
                CardName = cardName;
                Folder = folder;
                ModelFile = modelFile;
                TexturePrefix = texturePrefix;
                TargetHeight = targetHeight;
            }
        }

        private static readonly BuildingSpec[] Specs =
        {
            new BuildingSpec("Bantayog", "Building_Bantayog", "Bantayog", "Meshy_AI_Shrine_LowPoly_Optimi_1015154959_texture", "Meshy_AI_Shrine_LowPoly_Optimi_1015154959", 2.0f),
            new BuildingSpec("Sagrado", "Building_Sagrado", "Sagrado", "Meshy_AI_SunShrine_LowPoly_Opt_1015160211_texture", "Meshy_AI_SunShrine_LowPoly_Opt_1015160211", 2.2f),
        };

        public static void SetupBuildings()
        {
            EnsureFolder(PrefabsRoot);
            EnsureFolder(BuildingPrefabsFolder);
            EnsureFolder(MaterialsRoot);
            EnsureFolder(BuildingMaterialsFolder);

            int built = 0;
            foreach (BuildingSpec spec in Specs)
            {
                try
                {
                    if (BuildBuilding(spec))
                        built++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[Buildings] Failed to build " + spec.Name + ": " + e.Message);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Buildings] Finished. Built " + built + " of " + Specs.Length + " building prefabs.");
        }

        private static bool BuildBuilding(BuildingSpec spec)
        {
            string modelPath = CharactersRoot + "/" + spec.Folder + "/" + spec.ModelFile + ".fbx";
            string albedoPath = CharactersRoot + "/" + spec.Folder + "/" + spec.TexturePrefix + "_texture.png";
            string definitionPath = CardsRoot + "/" + spec.CardName + ".asset";

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                Debug.LogError("[Buildings] Model not imported yet at " + modelPath +
                               ". Wait for Unity to finish importing the FBX, then run again.");
                return false;
            }

            EnsureAlbedoImport(albedoPath);
            Material material = CreateMaterial(spec.Name, albedoPath);

            GameObject wrapper = new GameObject(spec.Name);
            GameObject instance = UnityEngine.Object.Instantiate(model);
            instance.name = spec.Name;
            instance.transform.SetParent(wrapper.transform, false);

            ApplyMaterial(instance, material);
            StripNonVisualComponents(instance);
            Normalize(instance, spec.TargetHeight);
            AssignVisualAnimator(wrapper, instance);

            string prefabPath = BuildingPrefabsFolder + "/" + spec.Name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(wrapper, prefabPath);
            UnityEngine.Object.DestroyImmediate(wrapper);

            AssignDefinitionPrefab(definitionPath, prefab);
            Debug.Log("[Buildings] Built " + spec.Name + " prefab at " + prefabPath + ".");
            return true;
        }

        private static Material CreateMaterial(string name, string albedoPath)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            string path = BuildingMaterialsFolder + "/" + name + "_Mat.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            Texture albedo = AssetDatabase.LoadAssetAtPath<Texture>(albedoPath);
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", albedo);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", albedo);

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.35f);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.35f);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureAlbedoImport(string albedoPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(albedoPath) as TextureImporter;
            if (importer == null)
                return;

            bool changed = false;
            if (!importer.sRGBTexture)
            {
                importer.sRGBTexture = true;
                changed = true;
            }

            if (importer.mipmapEnabled == false)
            {
                importer.mipmapEnabled = true;
                changed = true;
            }

            if (changed)
                importer.SaveAndReimport();
        }

        private static void StripNonVisualComponents(GameObject root)
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                UnityEngine.Object.DestroyImmediate(camera);

            foreach (Light light in root.GetComponentsInChildren<Light>(true))
                UnityEngine.Object.DestroyImmediate(light);
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                int slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                Material[] materials = new Material[slotCount];
                for (int i = 0; i < slotCount; i++)
                    materials[i] = material;

                renderer.sharedMaterials = materials;
            }
        }

        // Scales the model so it stands TargetHeight units tall and re-seats it so the
        // base of its bounds sits on the wrapper origin (grounded, centred).
        private static void Normalize(GameObject model, float targetHeight)
        {
            if (!TryGetBounds(model, out Bounds bounds))
                return;

            float height = bounds.size.y;
            if (height > 0.0001f)
                model.transform.localScale = Vector3.one * (targetHeight / height);

            if (!TryGetBounds(model, out bounds))
                return;

            Vector3 offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            model.transform.position += offset;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            return true;
        }

        // Buildings are static meshes with no rig, so the procedural animator drives the
        // whole model child (spawn pop-in + death topple). The visual is wired explicitly
        // because there is no Animator component for CreatureVisualAnimator to auto-detect.
        private static void AssignVisualAnimator(GameObject wrapper, GameObject modelInstance)
        {
            CreatureVisualAnimator animator = wrapper.GetComponent<CreatureVisualAnimator>();
            if (animator == null)
                animator = wrapper.AddComponent<CreatureVisualAnimator>();

            SerializedObject serialized = new SerializedObject(animator);
            SerializedProperty visual = serialized.FindProperty("visual");
            if (visual != null)
            {
                visual.objectReferenceValue = modelInstance.transform;
                serialized.ApplyModifiedProperties();
            }
        }

        private static void AssignDefinitionPrefab(string definitionPath, GameObject prefab)
        {
            ScriptableObject definition = AssetDatabase.LoadAssetAtPath<ScriptableObject>(definitionPath);
            if (definition == null)
            {
                Debug.LogWarning("[Buildings] No BuildingDefinition found at " + definitionPath + ".");
                return;
            }

            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty property = serialized.FindProperty("prefab");
            if (property == null)
            {
                Debug.LogWarning("[Buildings] " + definitionPath + " has no 'prefab' field.");
                return;
            }

            property.objectReferenceValue = prefab;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(definition);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
