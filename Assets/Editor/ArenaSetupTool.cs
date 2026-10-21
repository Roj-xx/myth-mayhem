using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythMayhem.EditorTools
{
    // Swaps the primitive placeholder board for the imported Meshy arena model:
    // builds a URP material from the albedo/normal maps, removes the old decorative
    // meshes, keeps the spawn points, and fits the arena to the play area.
    // Safe to re-run; it replaces any previous arena instance.
    public static class ArenaSetupTool
    {
        private const string BattleScenePath = "Assets/Scenes/PlayWithAI.unity";
        private const string ArenaFolder = "Assets/Art/Arena";
        private const string BaseName = "Meshy_AI_MythicalBattleArena_1015141808_image-to-3d-texture";
        private const string MaterialPath = ArenaFolder + "/BattleArenaMaterial.mat";
        private const string ArenaObjectName = "BattleArena";

        // Play area used by the board: 6 wide (x in -3..3) and 12 long (z in -6..6).
        private const float TargetWidthX = 6f;
        private const float TargetLengthZ = 12f;

        private static readonly string[] LegacyDecorNames =
        {
            "Board_PlayerHalf",
            "Board_OpponentHalf",
            "Lane_1",
            "Lane_2",
            "Lane_3",
        };

        public static void SetupArena()
        {
            string fbxPath = ArenaFolder + "/" + BaseName + ".fbx";
            string albedoPath = ArenaFolder + "/" + BaseName + ".png";
            string normalPath = ArenaFolder + "/" + BaseName + "_normal.png";

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null)
            {
                Debug.LogError("[Arena] Model not imported yet at " + fbxPath +
                               ". Wait for Unity to finish importing the FBX, then run again.");
                return;
            }

            Material material = CreateMaterial(albedoPath, normalPath);

            Scene scene = MythMayhemUi.OpenForScopedEditing(BattleScenePath, BattleScenePath);
            try
            {
                GameObject battlefield = FindInScene(scene, "Battlefield3D");
                if (battlefield == null)
                    throw new Exception("Battlefield3D was not found in " + BattleScenePath);

                RemoveLegacyDecoration(battlefield);

                Transform previous = FindChild(battlefield.transform, ArenaObjectName);
                if (previous != null)
                    UnityEngine.Object.DestroyImmediate(previous.gameObject);

                GameObject arena = InstantiateModel(model);
                arena.name = ArenaObjectName;
                arena.transform.SetParent(battlefield.transform, false);
                arena.transform.localPosition = Vector3.zero;
                arena.transform.localRotation = Quaternion.identity;
                arena.transform.localScale = Vector3.one;

                StripNonVisualComponents(arena);
                ApplyMaterial(arena, material);
                FitToBoard(arena);

                EditorUtility.SetDirty(arena);
                EditorSceneManager.MarkSceneDirty(scene);
                MythMayhemUi.FinishScopedScene(scene, BattleScenePath);

                Debug.Log("[Arena] Battle arena placed. Fine-tune its position/scale in the Inspector if needed.");
            }
            catch (Exception)
            {
                MythMayhemUi.DiscardScopedScene(scene);
                throw;
            }
        }

        private static Material CreateMaterial(string albedoPath, string normalPath)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            EnsureNormalMapImport(normalPath);

            Texture albedo = AssetDatabase.LoadAssetAtPath<Texture>(albedoPath);
            Texture normal = AssetDatabase.LoadAssetAtPath<Texture>(normalPath);

            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", albedo);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", albedo);

            if (normal != null)
            {
                if (material.HasProperty("_BumpMap"))
                    material.SetTexture("_BumpMap", normal);
                else if (material.HasProperty("_NormalMap"))
                    material.SetTexture("_NormalMap", normal);

                material.EnableKeyword("_NORMALMAP");
            }

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", 0.15f);
            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", 0.15f);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureNormalMapImport(string normalPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.NormalMap)
                return;

            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }

        private static void RemoveLegacyDecoration(GameObject battlefield)
        {
            foreach (string name in LegacyDecorNames)
            {
                Transform child = FindChild(battlefield.transform, name);
                if (child != null)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static GameObject InstantiateModel(GameObject model)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(model);

            return instance;
        }

        private static void StripNonVisualComponents(GameObject arena)
        {
            foreach (Camera camera in arena.GetComponentsInChildren<Camera>(true))
                UnityEngine.Object.DestroyImmediate(camera);

            foreach (Light light in arena.GetComponentsInChildren<Light>(true))
                UnityEngine.Object.DestroyImmediate(light);
        }

        private static void ApplyMaterial(GameObject arena, Material material)
        {
            foreach (Renderer renderer in arena.GetComponentsInChildren<Renderer>(true))
            {
                int slotCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                Material[] materials = new Material[slotCount];
                for (int i = 0; i < slotCount; i++)
                    materials[i] = material;

                renderer.sharedMaterials = materials;
            }
        }

        private static void FitToBoard(GameObject arena)
        {
            if (!TryGetBounds(arena, out Bounds bounds))
                return;

            float scaleX = bounds.size.x > 0.0001f ? TargetWidthX / bounds.size.x : 1f;
            float scaleZ = bounds.size.z > 0.0001f ? TargetLengthZ / bounds.size.z : 1f;
            float scale = Mathf.Min(scaleX, scaleZ);

            arena.transform.localScale = Vector3.one * scale;

            if (!TryGetBounds(arena, out bounds))
                return;

            // Center over the origin and rest the base on the board plane (y = 0).
            Vector3 offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            arena.transform.position += offset;
        }

        private static bool TryGetBounds(GameObject go, out Bounds bounds)
        {
            Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
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

        private static GameObject FindInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;

                Transform found = FindChild(root.transform, name);
                if (found != null)
                    return found.gameObject;
            }

            return null;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child;
            }

            return null;
        }
    }
}
