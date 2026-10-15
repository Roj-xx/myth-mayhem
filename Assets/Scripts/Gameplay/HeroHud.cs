using UnityEngine;
using UnityEngine.UI;

namespace MythMayhem.Gameplay
{
    public class HeroHud : MonoBehaviour
    {
        private static Font cachedFont;

        private MatchController match;
        private Text playerLabel;
        private Text opponentLabel;
        private GameObject gameOverPanel;
        private Text gameOverLabel;

        public static HeroHud Create(MatchController matchController)
        {
            Canvas canvas = FindTargetCanvas();

            GameObject root = new GameObject("HeroHud", typeof(RectTransform));
            if (canvas != null)
                root.transform.SetParent(canvas.transform, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            HeroHud hud = root.AddComponent<HeroHud>();
            hud.Initialize(matchController);
            return hud;
        }

        private static Canvas FindTargetCanvas()
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas fallback = null;

            foreach (Canvas canvas in canvases)
            {
                if (canvas == null || !canvas.isActiveAndEnabled)
                    continue;

                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    return canvas;

                if (fallback == null)
                    fallback = canvas;
            }

            return fallback;
        }

        private void Initialize(MatchController matchController)
        {
            match = matchController;

            playerLabel = CreateLabel("PlayerHeroLabel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(520f, 64f), new Color(0.45f, 0.72f, 1f, 1f));
            opponentLabel = CreateLabel("OpponentHeroLabel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(520f, 64f), new Color(1f, 0.5f, 0.5f, 1f));

            BuildGameOverOverlay();
        }

        public void Refresh(int playerHealth, int opponentHealth, int maxHealth)
        {
            if (playerLabel != null)
                playerLabel.text = "YOU   " + playerHealth + " / " + maxHealth;

            if (opponentLabel != null)
                opponentLabel.text = "ENEMY   " + opponentHealth + " / " + maxHealth;
        }

        public void ShowGameOver(MatchOutcome outcome)
        {
            if (gameOverPanel == null)
                return;

            gameOverPanel.SetActive(true);

            if (gameOverLabel == null)
                return;

            switch (outcome)
            {
                case MatchOutcome.PlayerWin:
                    gameOverLabel.text = "VICTORY";
                    gameOverLabel.color = new Color(0.45f, 0.9f, 0.5f, 1f);
                    break;
                case MatchOutcome.OpponentWin:
                    gameOverLabel.text = "DEFEAT";
                    gameOverLabel.color = new Color(1f, 0.42f, 0.42f, 1f);
                    break;
                default:
                    gameOverLabel.text = "DRAW";
                    gameOverLabel.color = new Color(0.95f, 0.85f, 0.4f, 1f);
                    break;
            }
        }

        private void BuildGameOverOverlay()
        {
            RectTransform rect = transform as RectTransform;

            GameObject panel = new GameObject("GameOver", typeof(RectTransform));
            panel.transform.SetParent(rect, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            Stretch(panelRect);

            Image scrim = panel.AddComponent<Image>();
            scrim.color = new Color(0f, 0f, 0f, 0.72f);
            scrim.raycastTarget = true;

            gameOverLabel = CreateLabel("GameOverText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(900f, 200f), Color.white);
            gameOverLabel.fontSize = 96;
            gameOverLabel.fontStyle = FontStyle.Bold;

            Button restart = CreateButton("RestartButton", panelRect, new Vector2(0f, -120f), new Vector2(420f, 130f), "RESTART");
            restart.onClick.AddListener(OnRestartClicked);

            gameOverPanel = panel;
            gameOverPanel.SetActive(false);
        }

        private void OnRestartClicked()
        {
            if (match != null)
                match.Restart();
        }

        private Text CreateLabel(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = go.AddComponent<Text>();
            text.font = GetFont();
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchorMin.x <= 0.01f
                ? TextAnchor.MiddleLeft
                : (anchorMin.x >= 0.99f ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter);
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, string label)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = go.AddComponent<Image>();
            image.color = new Color(0.9f, 0.72f, 0.28f, 1f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateChildLabel(rect, "Label", label);
            text.fontSize = 40;

            return button;
        }

        private static Text CreateChildLabel(RectTransform parent, string name, string content)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>());

            Text text = go.AddComponent<Text>();
            text.font = GetFont();
            text.text = content;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.06f, 0.07f, 0.12f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font GetFont()
        {
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (cachedFont == null)
                cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return cachedFont;
        }
    }
}
