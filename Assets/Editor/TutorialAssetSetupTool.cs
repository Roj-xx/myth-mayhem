using System.Collections.Generic;
using MythMayhem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MythMayhem.EditorTools
{
    /// <summary>
    /// Prepares the artwork used by the tutorial slideshow.
    /// Slide images are shown through UI Images, so every texture assigned to the
    /// tutorial slides has to be imported as a Sprite.
    ///
    /// The slide textures are imported as a single Sprite ("spriteMode: 1"), which means
    /// they must be referenced with the texture's main file id (21300000). Referencing a
    /// hashed sprite id (the multi-sprite form) resolves to null and leaves the slide blank.
    /// </summary>
    public static class TutorialAssetSetupTool
    {
        private const string ScenePath = "Assets/Scenes/Tutorial.unity";

        private static readonly string[] SlideImagePaths =
        {
            "Assets/Art/UI/Cards/BackCard.png",
            "Assets/Art/UI/Cards/AswangCard.png",
            "Assets/Art/UI/Cards/BantayogCard.png",
            "Assets/Art/UI/Cards/HilomCard.png",
            "Assets/Resources/UI/Game/EndTurnButton.png",
            "Assets/Resources/UI/Game/VictoryBanner.png"
        };

        [MenuItem("Tools/Tutorial/Prepare Slide Assets")]
        public static void PrepareSlideAssets()
        {
            foreach (string path in SlideImagePaths)
                EnsureSpriteImport(path);

            foreach (string path in SlideImagePaths)
                LogSpriteReference(path);
        }

        [MenuItem("Tools/Tutorial/Assign Slide Images")]
        public static void AssignSlideImages()
        {
            foreach (string path in SlideImagePaths)
                EnsureSpriteImport(path);

            List<Sprite> sprites = new List<Sprite>(SlideImagePaths.Length);
            foreach (string path in SlideImagePaths)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogError("[TutorialAssets] Could not load a sprite from " + path + ".");
                    return;
                }

                sprites.Add(sprite);
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            TutorialSceneController controller = FindControllerInScene(scene);
            if (controller == null)
            {
                Debug.LogError("[TutorialAssets] No TutorialSceneController found in " + ScenePath + ".");
                return;
            }

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty slideImages = serialized.FindProperty("slideImages");
            slideImages.arraySize = sprites.Count;
            for (int i = 0; i < sprites.Count; i++)
                slideImages.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[TutorialAssets] Assigned " + sprites.Count + " slide images to " + ScenePath + ".");
        }

        private static TutorialSceneController FindControllerInScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                TutorialSceneController found = root.GetComponentInChildren<TutorialSceneController>(true);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void EnsureSpriteImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            if (importer.textureType == TextureImporterType.Sprite && importer.alphaIsTransparency && !importer.mipmapEnabled)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }

        private static void LogSpriteReference(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[TutorialAssets] Could not load a sprite from " + path + ".");
                return;
            }

            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long localId))
            {
                Debug.LogWarning("[TutorialAssets] Could not resolve the file id of " + path + ".");
                return;
            }

            Debug.Log("[TutorialAssets] " + path + " -> {fileID: " + localId + ", guid: " + guid + ", type: 3}");
        }
    }
}
