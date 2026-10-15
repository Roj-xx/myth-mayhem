using System;
using UnityEngine;
using UnityEngine.UI;
using MythMayhem.Gameplay;

namespace MythMayhem.UI
{
    // Drives the online lobby. Builds its own UI at runtime so no scene wiring or
    // editor generator is required. The Relay join code is the only shareable token.
    public class LobbyMenuController : MonoBehaviour
    {
        private GameObject panelRoot;
        private Text statusLabel;
        private Text codeLabel;
        private InputField codeInput;
        private Button createButton;
        private Button joinButton;

        public static LobbyMenuController Instance { get; private set; }

        // Returns the existing lobby, or creates one on the fly so callers never
        // need a scene reference.
        public static LobbyMenuController GetOrCreate()
        {
            if (Instance != null)
                return Instance;

            GameObject host = new GameObject("LobbyMenu");
            return host.AddComponent<LobbyMenuController>();
        }

        private void Awake()
        {
            Instance = this;
            BuildUi();
            SetPanelVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Open()
        {
            SetPanelVisible(true);

            if (codeLabel != null)
                codeLabel.text = string.Empty;

            SetBusy(false);
            SetStatus(LobbyConnectorLocator.Available
                ? "Create a lobby, or enter a friend's code to join."
                : "Online play is not available in this build.");
        }

        public void Close()
        {
            LobbyConnectorLocator.Current?.Shutdown();
            SetPanelVisible(false);
        }

        public async void CreateLobby()
        {
            SetBusy(true);
            SetStatus("Creating lobby...");

            try
            {
                ILobbyConnector connector = LobbyConnectorLocator.Ensure();
                if (connector == null)
                    throw new InvalidOperationException("Networking is unavailable.");

                string code = await connector.CreateLobbyAsync(1);

                if (codeLabel != null)
                    codeLabel.text = "CODE: " + code;

                SetStatus("Share the code, then wait for your opponent to join.");
            }
            catch (Exception e)
            {
                Debug.LogError("[Lobby] Create failed: " + e);
                SetStatus("Could not create lobby: " + e.Message);
                SetBusy(false);
            }
        }

        public async void JoinLobby()
        {
            SetBusy(true);
            SetStatus("Joining...");

            try
            {
                ILobbyConnector connector = LobbyConnectorLocator.Ensure();
                if (connector == null)
                    throw new InvalidOperationException("Networking is unavailable.");

                string code = codeInput != null ? codeInput.text : string.Empty;
                await connector.JoinLobbyAsync(code);
                SetStatus("Connected. Loading battle...");
            }
            catch (Exception e)
            {
                Debug.LogError("[Lobby] Join failed: " + e);
                SetStatus("Could not join: " + e.Message);
                SetBusy(false);
            }
        }

        public void CopyCode()
        {
            if (codeLabel == null)
                return;

            string code = codeLabel.text.Replace("CODE: ", string.Empty);
            if (!string.IsNullOrEmpty(code))
                GUIUtility.systemCopyBuffer = code;
        }

        private void SetBusy(bool busy)
        {
            if (createButton != null)
                createButton.interactable = !busy;

            if (joinButton != null)
                joinButton.interactable = !busy;
        }

        private void SetStatus(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);
        }

        // ------------------------------------------------------- runtime UI build

        private void BuildUi()
        {
            Font font = LoadFont();

            GameObject canvasObject = new GameObject("LobbyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            panelRoot = CreateImage(canvasObject.transform, "Scrim", new Color(0f, 0f, 0f, 0.72f));

            GameObject card = CreateImage(panelRoot.transform, "Card", new Color(0.03f, 0.04f, 0.07f, 0.98f));
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(760f, 760f);
            cardRect.anchoredPosition = Vector2.zero;

            CreateLabel(card.transform, "Title", "ONLINE PVP", font, 56, new Vector2(0f, 300f), new Vector2(680f, 80f), new Color(0.96f, 0.82f, 0.42f));
            statusLabel = CreateLabel(card.transform, "Status", string.Empty, font, 28, new Vector2(0f, 220f), new Vector2(680f, 90f), new Color(0.62f, 0.67f, 0.78f));
            codeLabel = CreateLabel(card.transform, "Code", string.Empty, font, 40, new Vector2(0f, 140f), new Vector2(680f, 70f), new Color(0.93f, 0.94f, 0.97f));

            codeInput = CreateInput(card.transform, "CodeInput", font, new Vector2(0f, 60f), new Vector2(660f, 76f));

            CreateButton(card.transform, "CreateButton", "CREATE LOBBY", font, new Vector2(0f, -30f), CreateLobby);
            joinButton = CreateButton(card.transform, "JoinButton", "JOIN", font, new Vector2(0f, -120f), JoinLobby);
            createButton = FindButton(card.transform, "CreateButton");

            CreateButton(card.transform, "CopyButton", "COPY CODE", font, new Vector2(-190f, -215f), CopyCode);
            CreateButton(card.transform, "CloseButton", "CLOSE", font, new Vector2(190f, -215f), Close);
        }

        private GameObject CreateImage(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name, typeof(Image));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.GetComponent<Image>().color = color;
            return go;
        }

        private Text CreateLabel(Transform parent, string name, string text, Font font, int size, Vector2 position, Vector2 dimensions, Color color)
        {
            GameObject go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;

            Text label = go.GetComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.color = color;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private Button CreateButton(Transform parent, string name, string label, Font font, Vector2 position, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(380f, 84f);
            rect.anchoredPosition = position;

            Image background = go.GetComponent<Image>();
            background.color = new Color(0.106f, 0.137f, 0.2f, 1f);

            Button button = go.GetComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.16f, 0.22f, 0.34f, 1f);
            colors.pressedColor = new Color(0.09f, 0.12f, 0.18f, 1f);
            button.colors = colors;
            button.onClick.AddListener(() => onClick());

            CreateLabel(go.transform, "Label", label, font, 30, Vector2.zero, new Vector2(360f, 84f), new Color(0.93f, 0.94f, 0.97f));
            return button;
        }

        private InputField CreateInput(Transform parent, string name, Font font, Vector2 position, Vector2 dimensions)
        {
            GameObject go = new GameObject(name, typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = dimensions;
            rect.anchoredPosition = position;

            go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);

            InputField input = go.GetComponent<InputField>();
            input.targetGraphic = go.GetComponent<Image>();
            input.characterLimit = 12;

            Text text = CreateLabel(go.transform, "Text", string.Empty, font, 30, Vector2.zero, new Vector2(dimensions.x - 30f, dimensions.y - 16f), new Color(0.05f, 0.06f, 0.1f));
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;

            Text placeholder = CreateLabel(go.transform, "Placeholder", "ENTER CODE", font, 30, Vector2.zero, new Vector2(dimensions.x - 30f, dimensions.y - 16f), new Color(0.3f, 0.33f, 0.4f));
            placeholder.alignment = TextAnchor.MiddleLeft;

            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static Button FindButton(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == name)
                    return child.GetComponent<Button>();
            }

            return null;
        }

        private static Font LoadFont()
        {
            Font font = null;

            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                // Ignore; fall through to the next option.
            }

            if (font == null)
            {
                try
                {
                    font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
                catch
                {
                    // Ignore; text will fall back to the default font.
                }
            }

            return font;
        }
    }
}
