using System;
using System.Collections.Generic;
using System.IO;
using MythMayhem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythMayhem.EditorTools
{
    internal static class MythMayhemUi
    {
        // ---------------------------------------------------------------- palette
        public static readonly Color Gold = new Color(0.831f, 0.686f, 0.216f, 1f);
        public static readonly Color GoldBright = new Color(0.96f, 0.82f, 0.42f, 1f);
        public static readonly Color TextPrimary = new Color(0.93f, 0.94f, 0.97f, 1f);
        public static readonly Color TextMuted = new Color(0.62f, 0.67f, 0.78f, 1f);
        public static readonly Color FooterColor = new Color(0.47f, 0.50f, 0.58f, 1f);
        public static readonly Color ButtonFill = new Color(0.106f, 0.137f, 0.200f, 1f);
        public static readonly Color PanelFill = new Color(0.03f, 0.04f, 0.07f, 0.97f);
        public static readonly Color ScrimColor = new Color(0f, 0f, 0f, 0.72f);
        public static readonly Color SolidFallback = new Color(0.051f, 0.078f, 0.141f, 1f);
        public static readonly Color SplashTop = new Color(0.055f, 0.078f, 0.145f, 1f);
        public static readonly Color SplashBottom = new Color(0.012f, 0.016f, 0.035f, 1f);
        public static readonly Color MenuTop = new Color(0.060f, 0.085f, 0.155f, 1f);
        public static readonly Color MenuBottom = new Color(0.020f, 0.027f, 0.051f, 1f);
        public static readonly Color GlowBlue = new Color(0.22f, 0.40f, 0.90f, 0.32f);
        public static readonly Color GlowRed = new Color(0.62f, 0.14f, 0.18f, 0.28f);

        public const string GeneratedFolder = "Assets/Art/UI";
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        private static Font _font;
        private static Sprite _glow;
        private static Sprite _gradientMenu;
        private static Sprite _gradientSplash;
        private static Sprite _sunburst;

        // ---------------------------------------------------------------- sprites

        public static Sprite GlowSprite => _glow != null ? _glow : (_glow = LoadOrCreate("GlowWhite.png", GenerateGlow, 256));
        public static Sprite MenuGradientSprite => _gradientMenu != null ? _gradientMenu : (_gradientMenu = LoadOrCreate("GradientMenu.png", () => GenerateGradient(MenuTop, MenuBottom), null));
        public static Sprite SplashGradientSprite => _gradientSplash != null ? _gradientSplash : (_gradientSplash = LoadOrCreate("GradientSplash.png", () => GenerateGradient(SplashTop, SplashBottom), null));
        public static Sprite SunburstSprite => _sunburst != null ? _sunburst : (_sunburst = LoadOrCreate("SunburstGold.png", GenerateSunburst, 512));

        private static Sprite LoadOrCreate(string fileName, Func<Texture2D> generator, int? size)
        {
            EnsureGeneratedFolder();

            string path = GeneratedFolder + "/" + fileName;
            Texture2D texture = generator();
            if (texture != null)
            {
                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                ConfigureTexture(path);
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void RegenerateGeneratedSprites()
        {
            string[] knownFiles =
            {
                "GlowWhite.png",
                "GradientMenu.png",
                "GradientSplash.png",
                "SunburstGold.png"
            };

            foreach (string file in knownFiles)
            {
                string path = GeneratedFolder + "/" + file;
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null || System.IO.File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            _glow = null;
            _gradientMenu = null;
            _gradientSplash = null;
            _sunburst = null;

            _ = GlowSprite;
            _ = MenuGradientSprite;
            _ = SplashGradientSprite;
            _ = SunburstSprite;
            AssetDatabase.SaveAssets();
        }

        private static void EnsureGeneratedFolder()
        {
            if (AssetDatabase.IsValidFolder(GeneratedFolder))
                return;

            string[] parts = GeneratedFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void ConfigureTexture(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
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

        private static Texture2D GenerateGradient(Color top, Color bottom)
        {
            const int height = 256;
            Texture2D texture = new Texture2D(2, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                texture.SetPixel(0, y, Color.Lerp(bottom, top, t));
                texture.SetPixel(1, y, Color.Lerp(bottom, top, t));
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D GenerateGlow()
        {
            const int size = 256;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                    float alpha = 1f - Mathf.Clamp01(d);
                    alpha = alpha * alpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D GenerateSunburst()
        {
            const int size = 512;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;

            float center = (size - 1) * 0.5f;
            float maxRadius = center;
            const int rays = 8;
            const float ringInner = 0.30f;
            const float ringOuter = 0.38f;
            const float rayInner = 0.34f;
            const float rayOuter = 0.94f;
            const float coreHalf = 0.11f;
            const float segment = Mathf.PI * 2f / rays;

            Color32 gold = new Color32(246, 209, 107, 255);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - center) / maxRadius;
                    float ny = (y - center) / maxRadius;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = 0f;

                    float diamond = Mathf.Abs(nx) + Mathf.Abs(ny);
                    if (diamond < coreHalf)
                    {
                        alpha = Mathf.Max(alpha, 1f);
                    }
                    else if (diamond < coreHalf + 0.035f)
                    {
                        alpha = Mathf.Max(alpha, 1f - (diamond - coreHalf) / 0.035f);
                    }

                    if (r > ringInner && r < ringOuter)
                    {
                        float edge = Mathf.Min(r - ringInner, ringOuter - r) / 0.02f;
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(edge));
                    }

                    if (r > rayInner && r < rayOuter)
                    {
                        float ang = Mathf.Atan2(ny, nx);
                        float relative = Mathf.Repeat(ang, segment) - segment * 0.5f;
                        float width = Mathf.Lerp(0.11f, 0.02f, (r - rayInner) / (rayOuter - rayInner));
                        float ad = Mathf.Abs(relative);
                        float rayAlpha = 1f - Mathf.Clamp01((ad - width * 0.35f) / (width * 0.65f));
                        rayAlpha *= Mathf.Clamp01((rayOuter - r) / 0.06f);
                        alpha = Mathf.Max(alpha, rayAlpha);
                    }

                    float clamped = Mathf.Clamp01(alpha);
                    texture.SetPixel(x, y, new Color32(gold.r, gold.g, gold.b, (byte)(clamped * 255f)));
                }
            }

            texture.Apply();
            return texture;
        }

        // ---------------------------------------------------------------- scene scope utilities

        public static Scene OpenForScopedEditing(string savedPath, string newSceneAssetPath)
        {
            Scene scene = SceneManager.GetSceneByPath(savedPath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                if (File.Exists(savedPath))
                    scene = EditorSceneManager.OpenScene(savedPath, OpenSceneMode.Additive);
                else
                {
                    // Unity throws "Cannot create a new scene additively with an untitled scene unsaved"
                    // if a leaked unsaved scene from an earlier failed build is still open. Untitled
                    // scenes in this project are only ever produced by this tool, so discard them.
                    DiscardUntitledScenes();
                    scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                }
            }

            return scene;
        }

        private static void DiscardUntitledScenes()
        {
            List<Scene> untitled = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene open = SceneManager.GetSceneAt(i);
                if (string.IsNullOrEmpty(open.path))
                    untitled.Add(open);
            }

            foreach (Scene scene in untitled)
            {
                if (scene == SceneManager.GetActiveScene())
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                else
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        public static void FinishScopedScene(Scene scene, string savePath)
        {
            if (!scene.IsValid())
                return;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, savePath);
            CloseIfNotActive(scene);
        }

        public static void DiscardScopedScene(Scene scene)
        {
            if (!scene.IsValid())
                return;

            CloseIfNotActive(scene);
        }

        private static void CloseIfNotActive(Scene scene)
        {
            if (scene.path != SceneManager.GetActiveScene().path)
                EditorSceneManager.CloseScene(scene, true);
        }

        // ---------------------------------------------------------------- canvas / event system

        public static Canvas EnsureCanvas(Scene scene)
        {
            Canvas canvas = FindCanvasInScene(scene);
            if (canvas == null)
            {
                GameObject go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(go, scene);
                canvas = go.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            RectTransform rt = canvas.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            return canvas;
        }

        public static void EnsureEventSystem(Scene scene)
        {
            List<EventSystem> systems = new List<EventSystem>();
            foreach (GameObject go in scene.GetRootGameObjects())
                systems.AddRange(go.GetComponentsInChildren<EventSystem>(true));

            // Prefer the first that is active in the hierarchy as the survivor.
            EventSystem keep = null;
            foreach (EventSystem system in systems)
            {
                if (system.gameObject.activeInHierarchy)
                {
                    keep = system;
                    break;
                }
            }

            if (keep == null && systems.Count > 0)
                keep = systems[0];

            foreach (EventSystem system in systems)
            {
                if (system == keep)
                    continue;

                UnityEngine.Object.DestroyImmediate(system.gameObject);
            }

            if (keep == null)
            {
                GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
                SceneManager.MoveGameObjectToScene(eventSystem, scene);
                keep = eventSystem.GetComponent<EventSystem>();
            }

            keep.gameObject.SetActive(true);
            ConfigureInputModule(keep.gameObject);
        }

        private static Type InputSystemModuleType()
        {
            return Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        }

        private static void ConfigureInputModule(GameObject eventSystemObject)
        {
            Type inputSystemModule = InputSystemModuleType();

            if (inputSystemModule != null)
            {
                // The project uses the Input System package: never keep a legacy module alongside it.
                foreach (StandaloneInputModule legacy in eventSystemObject.GetComponents<StandaloneInputModule>())
                    UnityEngine.Object.DestroyImmediate(legacy);

                if (eventSystemObject.GetComponent(inputSystemModule) == null)
                    eventSystemObject.AddComponent(inputSystemModule);
            }
            else
            {
                foreach (Component modern in eventSystemObject.GetComponents<Component>())
                {
                    if (modern != null && modern.GetType().FullName == "UnityEngine.InputSystem.UI.InputSystemUIInputModule")
                        UnityEngine.Object.DestroyImmediate(modern);
                }

                if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
                    eventSystemObject.AddComponent<StandaloneInputModule>();
            }
        }

        public static Camera FindFirstCamera(Scene scene)
        {
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                Camera camera = go.GetComponentInChildren<Camera>(true);
                if (camera != null)
                    return camera;
            }

            return null;
        }

        public static void EnsureSingleAudioListener(Scene scene, Camera preferred)
        {
            List<AudioListener> listeners = new List<AudioListener>();
            foreach (GameObject go in scene.GetRootGameObjects())
                listeners.AddRange(go.GetComponentsInChildren<AudioListener>(true));

            AudioListener keep = null;
            if (preferred != null)
                keep = listeners.Find(l => l.gameObject == preferred.gameObject);

            if (keep == null && listeners.Count > 0)
                keep = listeners[0];

            foreach (AudioListener listener in listeners)
            {
                if (listener != keep)
                    UnityEngine.Object.DestroyImmediate(listener);
            }

            if (keep != null)
            {
                keep.enabled = true;
                return;
            }

            Camera camera = preferred != null ? preferred : FindFirstCamera(scene);
            if (camera != null)
                camera.gameObject.AddComponent<AudioListener>();
        }

        public static GameObject FindRootInScene(Scene scene, string rootName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == rootName)
                    return root;
            }

            return null;
        }

        public static Canvas FindCanvasInScene(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Canvas canvas = root.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                    return canvas;
            }

            return null;
        }

        // ---------------------------------------------------------------- element helpers

        public static GameObject CreateGameObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
        }

        public static Image CreateImage(string name, Transform parent, Color color)
        {
            Image image = CreateGameObject(name, parent).AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            image.type = Image.Type.Simple;
            return image;
        }

        public static Image CreateStretchedImage(string name, Transform parent, Color color, Sprite sprite, bool raycast)
        {
            Image image = CreateImage(name, parent, color);
            image.sprite = sprite;
            image.raycastTarget = raycast;
            SetStretch(image.rectTransform);
            return image;
        }

        public static Text CreateText(string name, Transform parent, string content, int size, FontStyle style, Color color)
        {
            Text text = CreateGameObject(name, parent).AddComponent<Text>();
            ConfigureText(text, content, size, style, color, false);
            return text;
        }

        public static void ConfigureText(Text text, string content, int size, FontStyle style, Color color, bool bestFit)
        {
            text.font = GetFont();
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            text.resizeTextForBestFit = bestFit;
            if (bestFit)
            {
                text.resizeTextMinSize = Mathf.RoundToInt(size * 0.4f);
                text.resizeTextMaxSize = size;
            }
        }

        public static Button CreateButton(string name, Transform parent)
        {
            GameObject go = CreateGameObject(name, parent);
            Image image = go.AddComponent<Image>();
            image.color = ButtonFill;
            image.raycastTarget = true;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.85f, 0.82f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;

            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        public static Image CreateUnderline(string parentName, Transform parent, float thickness, Color color)
        {
            Image line = CreateImage(parentName, parent, color);
            RectTransform rt = line.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(0f, 0f);
            rt.offsetMax = new Vector2(0f, thickness);
            return line;
        }

        public static LayoutElement Flex(RectTransform rt, float prefWidth, float prefHeight, float flexWidth, float flexHeight)
        {
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = prefWidth;
            le.preferredHeight = prefHeight;
            le.flexibleWidth = flexWidth;
            le.flexibleHeight = flexHeight;
            return le;
        }

        public static VerticalLayoutGroup AddVerticalLayout(RectTransform rt, float spacing, int paddingTop, int paddingBottom, int paddingSide)
        {
            VerticalLayoutGroup layout = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset(paddingSide, paddingSide, paddingTop, paddingBottom);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return layout;
        }

        public static Font GetFont()
        {
            if (_font != null)
                return _font;

            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }
    }
}