using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythMayhem.EditorTools
{
    public static class ArenaTerrainSetupTool
    {
        private const string ScenePath = "Assets/Scenes/PlayWithAI.unity";
        private const string GeneratedFolderPath = "Assets/TerrainSampleAssets/Generated";
        private const string TerrainDataPath =
            "Assets/TerrainSampleAssets/Generated/ArenaBorderTerrainData.asset";
        private const int HeightmapResolution = 129;
        private const int DetailResolution = 32;

        [MenuItem("Myth Mayhem/Create Editable Arena Terrain")]
        private static void CreateEditableArenaTerrain()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                EditorUtility.DisplayDialog(
                    "Open the AI battle scene",
                    "Open PlayWithAI before creating the arena terrain.",
                    "OK");
                return;
            }

            GameObject arenaGround = GameObject.Find("ArenaGround");
            if (arenaGround == null)
            {
                EditorUtility.DisplayDialog(
                    "Arena ground not found",
                    "The PlayWithAI scene does not contain an active ArenaGround.",
                    "OK");
                return;
            }

            if (arenaGround.transform.Find("ArenaEdgeTerrain") != null)
            {
                EditorUtility.DisplayDialog(
                    "Terrain already exists",
                    "ArenaEdgeTerrain is already a child of ArenaGround. Edit it in the Hierarchy and Terrain Inspector.",
                    "OK");
                return;
            }

            TerrainLayer groundLayer = LoadAsset<TerrainLayer>(
                "Assets/TerrainSampleAssets/TerrainLayers/Grass_Moss_TerrainLayer.terrainlayer");
            Texture2D heightmap = LoadAsset<Texture2D>(
                "Assets/TerrainSampleAssets/Textures/Heightmaps/Hills02_Heightmap.tiff");
            GameObject[] foliagePrefabs =
            {
                LoadAsset<GameObject>("Assets/TerrainSampleAssets/Prefabs/Grass_A.prefab"),
                LoadAsset<GameObject>("Assets/TerrainSampleAssets/Prefabs/Fern_A.prefab"),
                LoadAsset<GameObject>("Assets/TerrainSampleAssets/Prefabs/Bush_A.prefab")
            };

            if (AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath) != null)
            {
                EditorUtility.DisplayDialog(
                    "Terrain data already exists",
                    TerrainDataPath + " already exists. Rename or remove it before generating new terrain.",
                    "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(GeneratedFolderPath))
                AssetDatabase.CreateFolder("Assets/TerrainSampleAssets", "Generated");

            TerrainData terrainData = CreateTerrainData(groundLayer, heightmap, foliagePrefabs);
            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
            AssetDatabase.SaveAssets();

            GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = "ArenaEdgeTerrain";
            Undo.RegisterCreatedObjectUndo(terrainObject, "Create editable arena terrain");

            Terrain terrain = terrainObject.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.detailObjectDistance = 50f;
            terrain.detailObjectDensity = 0.65f;
            terrain.heightmapPixelError = 10f;

            terrainObject.transform.SetParent(arenaGround.transform, true);
            Vector3 parentScale = arenaGround.transform.lossyScale;
            terrainObject.transform.localScale = new Vector3(
                1f / parentScale.x,
                1f / parentScale.y,
                1f / parentScale.z);

            Bounds groundBounds = new Bounds(
                arenaGround.transform.position,
                Vector3.Scale(Vector3.one * 10f, arenaGround.transform.lossyScale));
            terrainObject.transform.position = new Vector3(
                groundBounds.center.x - terrainData.size.x * 0.5f,
                groundBounds.min.y - 0.1f,
                groundBounds.center.z - terrainData.size.z * 0.5f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = terrainObject;

            EditorUtility.DisplayDialog(
                "Arena terrain created",
                "Editable hilly terrain with moss, grass, ferns, and bushes has been saved under ArenaGround. Select ArenaEdgeTerrain to edit its TerrainData.",
                "OK");
        }

        private static T LoadAsset<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException("Required terrain asset was not found or imported: " + path);

            return asset;
        }

        private static TerrainData CreateTerrainData(
            TerrainLayer groundLayer,
            Texture2D heightmap,
            GameObject[] foliagePrefabs)
        {
            const int alphamapResolution = 16;
            TerrainData data = new TerrainData
            {
                heightmapResolution = HeightmapResolution,
                alphamapResolution = alphamapResolution,
                baseMapResolution = 256,
                size = new Vector3(44f, 5f, 44f),
                terrainLayers = new[] { groundLayer }
            };

            data.SetHeights(0, 0, CreateHeights(heightmap));

            float[,,] alphamap = new float[alphamapResolution, alphamapResolution, 1];
            for (int z = 0; z < alphamapResolution; z++)
            {
                for (int x = 0; x < alphamapResolution; x++)
                    alphamap[z, x, 0] = 1f;
            }

            data.SetAlphamaps(0, 0, alphamap);
            AddFoliage(data, foliagePrefabs);
            return data;
        }

        private static float[,] CreateHeights(Texture2D heightmap)
        {
            float[,] heights = new float[HeightmapResolution, HeightmapResolution];

            for (int z = 0; z < HeightmapResolution; z++)
            {
                float nz = z / (float)(HeightmapResolution - 1) * 2f - 1f;
                for (int x = 0; x < HeightmapResolution; x++)
                {
                    float nx = x / (float)(HeightmapResolution - 1) * 2f - 1f;
                    float edge = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(nz));
                    float border = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.67f, 0.97f, edge));
                    float hills = heightmap.GetPixelBilinear(
                        x / (float)(HeightmapResolution - 1),
                        z / (float)(HeightmapResolution - 1)).r;

                    heights[z, x] = border * (0.12f + hills * 0.76f);
                }
            }

            return heights;
        }

        private static void AddFoliage(TerrainData data, GameObject[] foliagePrefabs)
        {
            data.SetDetailResolution(DetailResolution, 8);
            DetailPrototype[] prototypes = new DetailPrototype[foliagePrefabs.Length];

            for (int i = 0; i < foliagePrefabs.Length; i++)
            {
                prototypes[i] = new DetailPrototype
                {
                    prototype = foliagePrefabs[i],
                    usePrototypeMesh = true,
                    renderMode = DetailRenderMode.VertexLit,
                    minWidth = i == 2 ? 1.1f : 0.45f,
                    maxWidth = i == 2 ? 1.8f : 0.9f,
                    minHeight = i == 2 ? 1.1f : 0.45f,
                    maxHeight = i == 2 ? 1.8f : 0.9f,
                    noiseSpread = 0.2f
                };
            }

            data.detailPrototypes = prototypes;
            for (int type = 0; type < prototypes.Length; type++)
            {
                int[,] details = new int[DetailResolution, DetailResolution];
                for (int z = 0; z < DetailResolution; z++)
                {
                    float nz = z / (float)(DetailResolution - 1) * 2f - 1f;
                    for (int x = 0; x < DetailResolution; x++)
                    {
                        float nx = x / (float)(DetailResolution - 1) * 2f - 1f;
                        float edge = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(nz));
                        if (edge < 0.72f)
                            continue;

                        int variation = (x * 73856093 ^ z * 19349663) & 7;
                        if (type == 0)
                            details[z, x] = 1 + variation % 2;
                        else if (type == 1 && variation == 0)
                            details[z, x] = 1;
                        else if (type == 2 && variation == 3)
                            details[z, x] = 1;
                    }
                }

                data.SetDetailLayer(0, 0, type, details);
            }
        }
    }
}
