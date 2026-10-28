using System;
using UnityEngine;
using UnityEngine.UI;
using MythMayhem.Audio;
using MythMayhem.Gameplay;

namespace MythMayhem.UI
{
    // Drives the online lobby controls assigned in the scene.
    public class LobbyMenuController : MonoBehaviour
    {
        [SerializeField] private bool showOnStart;
        [SerializeField] private Font uiFont;
        [SerializeField] private Sprite modalSprite;
        [SerializeField] private Sprite primaryButtonSprite;
        [SerializeField] private Sprite secondaryButtonSprite;
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text codeLabel;
        [SerializeField] private InputField codeInput;
        [SerializeField] private Button createButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button copyButton;
        [SerializeField] private Button backButton;

        public static LobbyMenuController Instance { get; private set; }

        private const string DefaultStatus = "Create a lobby, or enter a friend's code to join.";

        private ILobbyConnector subscribedConnector;

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
            if (panelRoot == null)
                BuildRuntimeFallback();
            WireButtons();
            SetPanelVisible(showOnStart);

            Subscribe(LobbyConnectorLocator.Current);
        }

        private void OnDestroy()
        {
            Unsubscribe();

            if (Instance == this)
                Instance = null;
        }

        public void Open()
        {
            SetPanelVisible(true);
            AudioManager.SetPvpLobbyOpen(true);

            Subscribe(LobbyConnectorLocator.Current);
        }

        public void Close()
        {
            LobbyConnectorLocator.Current?.Shutdown();
            Unsubscribe();
            SetPanelVisible(false);
            AudioManager.SetPvpLobbyOpen(false);
        }

        public void BackToMenu()
        {
            LobbyConnectorLocator.Current?.Shutdown();
            Unsubscribe();
            AudioManager.SetPvpLobbyOpen(false);
            SceneTransitionManager.LoadScene("MainMenu");
        }

        public async void CreateLobby()
        {
            ILobbyConnector connector = null;
            try
            {
                connector = LobbyConnectorLocator.Ensure();
                if (connector == null)
                    throw new InvalidOperationException("Networking is unavailable.");

                Subscribe(connector);
                SetBusy(true);

                string code = await connector.CreateLobbyAsync(1);

                // A null code means the attempt was cancelled (the player left
                // the lobby while the relay allocation was in flight).
                if (string.IsNullOrEmpty(code))
                    return;

                if (codeLabel != null)
                    codeLabel.text = "CODE: " + code;
            }
            catch (Exception e)
            {
                connector?.Shutdown();
                Debug.LogError("[Lobby] Create failed: " + e);
                SetStatus("Could not create lobby: " + e.Message);
                SetBusy(false);
            }
        }

        public async void JoinLobby()
        {
            ILobbyConnector connector = null;
            try
            {
                connector = LobbyConnectorLocator.Ensure();
                if (connector == null)
                    throw new InvalidOperationException("Networking is unavailable.");

                Subscribe(connector);
                SetBusy(true);

                string code = codeInput != null ? codeInput.text : string.Empty;
                // The connector reports progress ("Connecting…", "Connected…")
                // once the handshake actually completes; never before.
                await connector.JoinLobbyAsync(code);
            }
            catch (Exception e)
            {
                connector?.Shutdown();
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

        // Follows the connector that actually owns the session. Re-subscribing is
        // harmless: the old handler is always detached first.
        private void Subscribe(ILobbyConnector connector)
        {
            if (ReferenceEquals(connector, subscribedConnector))
            {
                RefreshFromConnector();
                return;
            }

            Unsubscribe();

            subscribedConnector = connector;
            if (subscribedConnector != null)
                subscribedConnector.StateChanged += HandleLobbyStatus;

            RefreshFromConnector();
        }

        private void Unsubscribe()
        {
            if (subscribedConnector == null)
                return;

            subscribedConnector.StateChanged -= HandleLobbyStatus;
            subscribedConnector = null;
        }

        private void RefreshFromConnector()
        {
            ILobbyConnector connector = subscribedConnector != null
                ? subscribedConnector
                : LobbyConnectorLocator.Current;

            if (connector == null)
            {
                SetBusy(false);
                SetStatus(LobbyConnectorLocator.Available
                    ? DefaultStatus
                    : "Online play is not available in this build.");
                ClearCode();
                return;
            }

            ApplyStatus(connector.LastStatus);
        }

        private void HandleLobbyStatus(LobbyStatus status)
        {
            ApplyStatus(status);
        }

        private void ApplyStatus(LobbyStatus status)
        {
            if (status.State == LobbyConnectionState.Idle || status.State == LobbyConnectionState.Failed)
                ClearCode();

            string message = status.Message;
            if (string.IsNullOrEmpty(message))
            {
                message = status.State == LobbyConnectionState.Failed
                    ? "The connection failed. Please try again."
                    : DefaultStatus;
            }

            SetStatus(message);

            // Busy states lock the controls; Failed/Idle hand them back so the
            // player can retry without leaving the screen.
            SetBusy(status.IsBusy);
        }

        private void ClearCode()
        {
            if (codeLabel != null)
                codeLabel.text = string.Empty;
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

        private void WireButtons()
        {
            if (createButton != null)
                createButton.onClick.RemoveListener(CreateLobby);
            if (createButton != null)
                createButton.onClick.AddListener(CreateLobby);
            if (joinButton != null)
                joinButton.onClick.RemoveListener(JoinLobby);
            if (joinButton != null)
                joinButton.onClick.AddListener(JoinLobby);
            if (copyButton != null)
                copyButton.onClick.RemoveListener(CopyCode);
            if (copyButton != null)
                copyButton.onClick.AddListener(CopyCode);
            if (backButton != null)
                backButton.onClick.RemoveListener(BackToMenu);
            if (backButton != null)
                backButton.onClick.AddListener(BackToMenu);

            AudioManager.HookAllButtons();
        }

        private void BuildRuntimeFallback()
        {
            Font font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
            GameObject root = CreateRuntimeImage(transform, "LobbyUI", new Color(0f, 0f, 0f, 0f));
            panelRoot = root;

            GameObject card = CreateRuntimeImage(root.transform, "LobbyModal", Color.white, modalSprite);
            SetCenteredRect(card.GetComponent<RectTransform>(), new Vector2(1000f, 740f), Vector2.zero);

            CreateRuntimeLabel(card.transform, "Title", "PVP LOBBY", font, 56,
                new Vector2(0f, 280f), new Vector2(820f, 76f), new Color(0.96f, 0.82f, 0.42f));
            statusLabel = CreateRuntimeLabel(card.transform, "Status", string.Empty, font, 26,
                new Vector2(0f, 190f), new Vector2(820f, 100f), new Color(0.62f, 0.67f, 0.78f));
            codeLabel = CreateRuntimeLabel(card.transform, "Code", string.Empty, font, 38,
                new Vector2(0f, 90f), new Vector2(820f, 60f), new Color(0.93f, 0.94f, 0.97f));

            codeInput = CreateRuntimeInput(card.transform, font);
            createButton = CreateRuntimeButton(card.transform, "CreateButton", "CREATE LOBBY", font,
                new Vector2(0f, -100f), new Vector2(620f, 106f), primaryButtonSprite);
            joinButton = CreateRuntimeButton(card.transform, "JoinButton", "JOIN WITH CODE", font,
                new Vector2(0f, -210f), new Vector2(620f, 106f), secondaryButtonSprite);
            copyButton = CreateRuntimeButton(card.transform, "CopyButton", "COPY CODE", font,
                new Vector2(-180f, -310f), new Vector2(300f, 72f));
            backButton = CreateRuntimeButton(card.transform, "BackButton", "BACK TO MENU", font,
                new Vector2(180f, -310f), new Vector2(300f, 72f));
        }

        private static GameObject CreateRuntimeImage(Transform parent, string name, Color color, Sprite sprite = null)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gameObject.transform.SetParent(parent, false);

            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.preserveAspect = false;
            image.raycastTarget = sprite != null;
            return gameObject;
        }

        private static Text CreateRuntimeLabel(Transform parent, string name, string value, Font font, int size,
            Vector2 position, Vector2 dimensions, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            SetCenteredRect(gameObject.GetComponent<RectTransform>(), dimensions, position);

            Text text = gameObject.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Button CreateRuntimeButton(Transform parent, string name, string label, Font font,
            Vector2 position, Vector2 dimensions, Sprite sprite = null)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            SetCenteredRect(gameObject.GetComponent<RectTransform>(), dimensions, position);

            Image image = gameObject.GetComponent<Image>();
            image.color = sprite != null ? Color.white : new Color(0.106f, 0.137f, 0.2f, 1f);
            image.sprite = sprite;
            image.preserveAspect = sprite != null;

            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;

            Text buttonLabel = CreateRuntimeLabel(gameObject.transform, "Label", label, font, 30,
                Vector2.zero, new Vector2(dimensions.x - 24f, dimensions.y - 12f), new Color(0.93f, 0.94f, 0.97f));
            RectTransform labelRect = buttonLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 6f);
            labelRect.offsetMax = new Vector2(-12f, -6f);
            return button;
        }

        private InputField CreateRuntimeInput(Transform parent, Font font)
        {
            const float width = 760f;
            const float height = 76f;
            GameObject gameObject = new GameObject("CodeInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            gameObject.transform.SetParent(parent, false);
            SetCenteredRect(gameObject.GetComponent<RectTransform>(), new Vector2(width, height), Vector2.zero);

            Image image = gameObject.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.95f);
            InputField input = gameObject.GetComponent<InputField>();
            input.targetGraphic = image;
            input.characterLimit = 12;

            Text text = CreateRuntimeLabel(gameObject.transform, "Text", string.Empty, font, 30,
                Vector2.zero, new Vector2(width - 30f, height - 16f), new Color(0.05f, 0.06f, 0.1f));
            Text placeholder = CreateRuntimeLabel(gameObject.transform, "Placeholder", "ENTER CODE", font, 30,
                Vector2.zero, new Vector2(width - 30f, height - 16f), new Color(0.3f, 0.33f, 0.4f));
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;
            placeholder.alignment = TextAnchor.MiddleLeft;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static void SetCenteredRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
