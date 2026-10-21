using System;
using MythMayhem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythMayhem.EditorTools
{
    // Sets up the battle hand area:
    //  - the HandPanel container is kept as before but made fully transparent, and the
    //    HandPanel artwork is added as a separate child ("HandPanelArt") so it can be
    //    moved and stretched freely in the Scene view;
    //  - the per-card background boxes are hidden so each card shows only its artwork.
    // Safe to re-run; an existing HandPanelArt keeps whatever placement it has.
    public static class CardUiSetupTool
    {
        private const string ScenePath = "Assets/Scenes/PlayWithAI.unity";
        private const string HandPanelSpritePath = "Assets/Art/UI/HandPanel.png";
        private const string HandPanelArtName = "HandPanelArt";

        public static void SetupCardUi()
        {
            EnsureSpriteImport(HandPanelSpritePath);

            Scene scene = MythMayhemUi.OpenForScopedEditing(ScenePath, ScenePath);
            try
            {
                CardHandController controller = FindControllerInScene(scene);
                if (controller == null)
                    throw new Exception("No CardHandController found in " + ScenePath);

                MakeHandPanelTransparent(controller.GetComponent<Image>());
                AddHandPanelArt(controller.transform);

                // Cards: hide the placeholder background box. It stays enabled so the
                // button still has a raycast target for clicks and dragging.
                SerializedObject serialized = new SerializedObject(controller);
                SerializedProperty backgrounds = serialized.FindProperty("cardBackgrounds");
                if (backgrounds != null)
                {
                    for (int i = 0; i < backgrounds.arraySize; i++)
                    {
                        Image background = backgrounds.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                        if (background == null)
                            continue;

                        background.color = Color.clear;
                        EditorUtility.SetDirty(background);
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                MythMayhemUi.FinishScopedScene(scene, ScenePath);
                Debug.Log("[CardUI] Hand panel made transparent, HandPanel art added, card boxes hidden in " + ScenePath + ".");
            }
            catch (Exception)
            {
                MythMayhemUi.DiscardScopedScene(scene);
                throw;
            }
        }

        // Keeps the existing 9-sliced container, but with a fully transparent fill so the
        // HandPanel artwork (added separately) is what is actually seen.
        private static void MakeHandPanelTransparent(Image panel)
        {
            if (panel == null)
                return;

            Sprite builtin = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (builtin != null)
                panel.sprite = builtin;

            panel.type = Image.Type.Sliced;
            panel.preserveAspect = false;
            Color color = panel.color;
            color.a = 0f;
            panel.color = color;
            EditorUtility.SetDirty(panel);
        }

        // Adds the HandPanel artwork as a child of the panel so it can be adjusted
        // independently of the container. Only a freshly created child gets default
        // placement, so re-running never undoes manual positioning.
        private static void AddHandPanelArt(Transform parent)
        {
            Transform existing = parent.Find(HandPanelArtName);
            bool created = existing == null;
            GameObject artGo = created
                ? new GameObject(HandPanelArtName, typeof(RectTransform))
                : existing.gameObject;

            if (created)
            {
                artGo.transform.SetParent(parent, false);
                artGo.transform.SetAsFirstSibling();

                RectTransform rect = artGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.localScale = Vector3.one;
            }

            Image art = artGo.GetComponent<Image>();
            if (art == null)
                art = artGo.AddComponent<Image>();

            art.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HandPanelSpritePath);
            art.color = Color.white;
            art.type = Image.Type.Simple;
            art.preserveAspect = false;
            art.raycastTarget = false;

            EditorUtility.SetDirty(artGo);
            EditorUtility.SetDirty(art);
        }

        private static CardHandController FindControllerInScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                CardHandController controller = root.GetComponentInChildren<CardHandController>(true);
                if (controller != null)
                    return controller;
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
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = 100f;
            importer.SaveAndReimport();
        }
    }
}
