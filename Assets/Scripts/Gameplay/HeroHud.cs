using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MythMayhem.Audio;

namespace MythMayhem.Gameplay
{
    public class HeroHud : MonoBehaviour
    {
        private const float GameOverFadeDuration = 0.6f;
        private static Font cachedFont;
        private static Sprite cachedFillSprite;

        [SerializeField] private Sprite playerHpBarSprite;
        [SerializeField] private Sprite opponentHpBarSprite;
        [SerializeField] private Sprite victoryBannerSprite;
        [SerializeField] private Sprite defeatBannerSprite;
        [SerializeField] private Sprite drawBannerSprite;
        [SerializeField] private Image playerHpBarFill;
        [SerializeField] private Text playerHpText;
        [SerializeField] private Image opponentHpBarFill;
        [SerializeField] private Text opponentHpText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverLabel;
        [SerializeField] private Image gameOverBanner;
        [SerializeField] private Button restartButton;

        private MatchController match;
        private CanvasGroup gameOverCanvasGroup;
        private Coroutine gameOverFadeRoutine;

        // Uses a HeroHud already placed in the scene, so it stays editable in the
        // Scene view. Falls back to building one at runtime when the scene has none.
        public static HeroHud Resolve(MatchController matchController, bool allowRuntimeBuild)
        {
            HeroHud hud = UnityEngine.Object.FindFirstObjectByType<HeroHud>();
            if (hud == null)
            {
                if (!allowRuntimeBuild)
                    return null;

                Canvas canvas = FindTargetCanvas();

                GameObject root = new GameObject("HeroHud", typeof(RectTransform));
                if (canvas != null)
                    root.transform.SetParent(canvas.transform, false);

                RectTransform rect = root.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                hud = root.AddComponent<HeroHud>();
            }

            hud.Initialize(matchController);
            return hud;
        }

        // Bakes the HUD (HP bars, game-over panel, banners, label backgrounds and
        // the end-turn skin) into the open scene so it can be edited visually.
        // Called by the editor setup tool; skipped in builds.
#if UNITY_EDITOR
        public void EditorBuild()
        {
            if (playerHpBarSprite == null)
                playerHpBarSprite = LoadGameSprite("PlayerHPBar");
            if (opponentHpBarSprite == null)
                opponentHpBarSprite = LoadGameSprite("OpponentHPBar");
            if (victoryBannerSprite == null)
                victoryBannerSprite = LoadGameSprite("VictoryBanner");
            if (defeatBannerSprite == null)
                defeatBannerSprite = LoadGameSprite("DefeatBanner");
            if (drawBannerSprite == null)
                drawBannerSprite = LoadGameSprite("DrawBanner");

            if (playerHpBarFill == null || opponentHpBarFill == null)
                BuildHpBars();
            if (gameOverPanel == null)
                BuildGameOverOverlay();

            ApplySceneSkin();
        }
#endif

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

            if (playerHpBarSprite == null)
                playerHpBarSprite = LoadGameSprite("PlayerHPBar");
            if (opponentHpBarSprite == null)
                opponentHpBarSprite = LoadGameSprite("OpponentHPBar");
            if (victoryBannerSprite == null)
                victoryBannerSprite = LoadGameSprite("VictoryBanner");
            if (defeatBannerSprite == null)
                defeatBannerSprite = LoadGameSprite("DefeatBanner");
            if (drawBannerSprite == null)
                drawBannerSprite = LoadGameSprite("DrawBanner");

            // Only build what the scene doesn't already provide, so a baked-in HUD
            // (editable in the Scene view) is reused instead of duplicated.
            if (playerHpBarFill == null || playerHpText == null || opponentHpBarFill == null || opponentHpText == null)
                BuildHpBars();

            if (gameOverPanel == null)
                BuildGameOverOverlay();

            gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
            if (gameOverCanvasGroup == null)
                gameOverCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
            gameOverCanvasGroup.alpha = 1f;
            gameOverCanvasGroup.interactable = true;
            gameOverCanvasGroup.blocksRaycasts = true;
            gameOverPanel.SetActive(false);

            if (restartButton != null)
            {
                AudioManager.HookButton(restartButton);
                restartButton.onClick.RemoveListener(OnRestartClicked);
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            ApplySceneSkin();
        }

        public void Refresh(int playerHealth, int opponentHealth, int maxHealth)
        {
            int max = Mathf.Max(1, maxHealth);
            float playerRatio = Mathf.Clamp01((float)playerHealth / (float)max);
            float opponentRatio = Mathf.Clamp01((float)opponentHealth / (float)max);

            if (playerHpBarFill != null)
            {
                if (playerHpBarFill.sprite == null)
                    playerHpBarFill.sprite = GetFillSprite();
                playerHpBarFill.type = Image.Type.Filled;
                playerHpBarFill.fillMethod = Image.FillMethod.Horizontal;
                playerHpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                playerHpBarFill.fillAmount = playerRatio;
            }
            if (playerHpText != null)
                playerHpText.text = playerHealth.ToString();

            if (opponentHpBarFill != null)
            {
                if (opponentHpBarFill.sprite == null)
                    opponentHpBarFill.sprite = GetFillSprite();
                opponentHpBarFill.type = Image.Type.Filled;
                opponentHpBarFill.fillMethod = Image.FillMethod.Horizontal;
                opponentHpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                opponentHpBarFill.fillAmount = opponentRatio;
            }
            if (opponentHpText != null)
                opponentHpText.text = opponentHealth.ToString();
        }

        public void ShowGameOver(MatchOutcome outcome)
        {
            if (gameOverPanel == null)
                return;

            if (gameOverFadeRoutine != null)
                StopCoroutine(gameOverFadeRoutine);

            if (gameOverCanvasGroup == null)
            {
                gameOverCanvasGroup = gameOverPanel.GetComponent<CanvasGroup>();
                if (gameOverCanvasGroup == null)
                    gameOverCanvasGroup = gameOverPanel.AddComponent<CanvasGroup>();
            }

            gameOverCanvasGroup.alpha = 0f;
            gameOverCanvasGroup.interactable = false;
            gameOverCanvasGroup.blocksRaycasts = true;
            gameOverPanel.SetActive(true);

            Sprite banner = null;
            if (outcome == MatchOutcome.PlayerWin)
                banner = victoryBannerSprite;
            else if (outcome == MatchOutcome.OpponentWin)
                banner = defeatBannerSprite;
            else if (outcome == MatchOutcome.Draw)
                banner = drawBannerSprite;

            if (gameOverBanner != null)
            {
                gameOverBanner.sprite = banner;
                gameOverBanner.enabled = banner != null;
            }

            if (gameOverLabel != null)
            {
                gameOverLabel.enabled = banner == null;
                if (banner == null)
                {
                    gameOverLabel.text = "DRAW";
                    gameOverLabel.color = new Color(0.95f, 0.85f, 0.4f, 1f);
                }
            }

            gameOverFadeRoutine = StartCoroutine(FadeGameOverIn());
        }

        private IEnumerator FadeGameOverIn()
        {
            float elapsed = 0f;
            while (elapsed < GameOverFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                gameOverCanvasGroup.alpha = Mathf.Clamp01(elapsed / GameOverFadeDuration);
                yield return null;
            }

            gameOverCanvasGroup.alpha = 1f;
            gameOverCanvasGroup.interactable = true;
            gameOverFadeRoutine = null;
        }

        private void BuildHpBars()
        {
            // Player HP bar - top-left
            GameObject playerRoot = new GameObject("PlayerHPBar", typeof(RectTransform));
            playerRoot.transform.SetParent(transform, false);
            RectTransform pr = playerRoot.GetComponent<RectTransform>();
            pr.anchorMin = new Vector2(0f, 1f);
            pr.anchorMax = new Vector2(0f, 1f);
            pr.pivot = new Vector2(0f, 1f);
            pr.anchoredPosition = new Vector2(24f, -24f);
            pr.sizeDelta = new Vector2(420f, 56f);

            // Fill behind the frame
            GameObject playerFillGo = new GameObject("Fill", typeof(RectTransform));
            playerFillGo.transform.SetParent(playerRoot.transform, false);
            RectTransform pfr = playerFillGo.GetComponent<RectTransform>();
            pfr.anchorMin = new Vector2(0f, 0f);
            pfr.anchorMax = new Vector2(1f, 1f);
            pfr.offsetMin = new Vector2(6f, 6f);
            pfr.offsetMax = new Vector2(-6f, -6f);
            playerHpBarFill = playerFillGo.AddComponent<Image>();
            playerHpBarFill.color = new Color(0.20f, 0.85f, 0.30f, 1f);
            playerHpBarFill.raycastTarget = false;
            playerHpBarFill.type = Image.Type.Filled;
            playerHpBarFill.fillMethod = Image.FillMethod.Horizontal;
            playerHpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            // Frame PNG on top
            GameObject playerFrameGo = new GameObject("Frame", typeof(RectTransform));
            playerFrameGo.transform.SetParent(playerRoot.transform, false);
            RectTransform pframe = playerFrameGo.GetComponent<RectTransform>();
            Stretch(pframe);
            Image playerFrame = playerFrameGo.AddComponent<Image>();
            playerFrame.sprite = playerHpBarSprite;
            playerFrame.preserveAspect = false;
            playerFrame.color = Color.white;
            playerFrame.raycastTarget = false;
            playerFrame.type = Image.Type.Simple;

            // HP text (current only)
            GameObject playerHpGo = new GameObject("HPText", typeof(RectTransform));
            playerHpGo.transform.SetParent(playerRoot.transform, false);
            RectTransform phtRect = playerHpGo.GetComponent<RectTransform>();
            Stretch(phtRect);
            playerHpText = playerHpGo.AddComponent<Text>();
            playerHpText.font = GetFont();
            playerHpText.fontSize = 28;
            playerHpText.fontStyle = FontStyle.Bold;
            playerHpText.alignment = TextAnchor.MiddleCenter;
            playerHpText.color = Color.white;
            playerHpText.horizontalOverflow = HorizontalWrapMode.Overflow;
            playerHpText.verticalOverflow = VerticalWrapMode.Overflow;
            playerHpText.raycastTarget = false;
            playerHpText.text = "100";

            // Opponent HP bar - top-right
            GameObject oppRoot = new GameObject("OpponentHPBar", typeof(RectTransform));
            oppRoot.transform.SetParent(transform, false);
            RectTransform orRect = oppRoot.GetComponent<RectTransform>();
            orRect.anchorMin = new Vector2(1f, 1f);
            orRect.anchorMax = new Vector2(1f, 1f);
            orRect.pivot = new Vector2(1f, 1f);
            orRect.anchoredPosition = new Vector2(-24f, -24f);
            orRect.sizeDelta = new Vector2(420f, 56f);

            // Fill behind
            GameObject oppFillGo = new GameObject("Fill", typeof(RectTransform));
            oppFillGo.transform.SetParent(oppRoot.transform, false);
            RectTransform offr = oppFillGo.GetComponent<RectTransform>();
            offr.anchorMin = new Vector2(0f, 0f);
            offr.anchorMax = new Vector2(1f, 1f);
            offr.offsetMin = new Vector2(6f, 6f);
            offr.offsetMax = new Vector2(-6f, -6f);
            opponentHpBarFill = oppFillGo.AddComponent<Image>();
            opponentHpBarFill.color = new Color(0.95f, 0.3f, 0.25f, 1f);
            opponentHpBarFill.raycastTarget = false;
            opponentHpBarFill.type = Image.Type.Filled;
            opponentHpBarFill.fillMethod = Image.FillMethod.Horizontal;
            opponentHpBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;

            // Frame
            GameObject oppFrameGo = new GameObject("Frame", typeof(RectTransform));
            oppFrameGo.transform.SetParent(oppRoot.transform, false);
            RectTransform oframe = oppFrameGo.GetComponent<RectTransform>();
            Stretch(oframe);
            Image oppFrame = oppFrameGo.AddComponent<Image>();
            oppFrame.sprite = opponentHpBarSprite;
            oppFrame.preserveAspect = false;
            oppFrame.color = Color.white;
            oppFrame.raycastTarget = false;
            oppFrame.type = Image.Type.Simple;

            // HP text
            GameObject oppHpGo = new GameObject("HPText", typeof(RectTransform));
            oppHpGo.transform.SetParent(oppRoot.transform, false);
            RectTransform ohtRect = oppHpGo.GetComponent<RectTransform>();
            Stretch(ohtRect);
            opponentHpText = oppHpGo.AddComponent<Text>();
            opponentHpText.font = GetFont();
            opponentHpText.fontSize = 28;
            opponentHpText.fontStyle = FontStyle.Bold;
            opponentHpText.alignment = TextAnchor.MiddleCenter;
            opponentHpText.color = Color.white;
            opponentHpText.horizontalOverflow = HorizontalWrapMode.Overflow;
            opponentHpText.verticalOverflow = VerticalWrapMode.Overflow;
            opponentHpText.raycastTarget = false;
            opponentHpText.text = "100";
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

            victoryBannerSprite = LoadGameSprite("VictoryBanner");
            defeatBannerSprite = LoadGameSprite("DefeatBanner");
            drawBannerSprite = LoadGameSprite("DrawBanner");

            GameObject bannerGo = new GameObject("GameOverBanner", typeof(RectTransform));
            bannerGo.transform.SetParent(panelRect, false);
            RectTransform bannerRect = bannerGo.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
            bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.anchoredPosition = new Vector2(0f, 120f);
            bannerRect.sizeDelta = new Vector2(720f, 240f);
            gameOverBanner = bannerGo.AddComponent<Image>();
            gameOverBanner.preserveAspect = true;
            gameOverBanner.raycastTarget = false;
            gameOverBanner.enabled = false;

            restartButton = CreateButton("RestartButton", panelRect, new Vector2(0f, -120f), new Vector2(420f, 130f), "RESTART");
            AudioManager.HookButton(restartButton);

            gameOverPanel = panel;
            gameOverPanel.SetActive(false);
        }

        private void OnRestartClicked()
        {
            if (match != null)
                match.Restart();
        }

        // Sprites live under Assets/Resources/UI/Game so the exact same assets are
        // loaded in the editor and in builds - edit them there and both pick it up.
        private static Sprite LoadGameSprite(string assetName)
        {
            Sprite sprite = Resources.Load<Sprite>("UI/Game/" + assetName);
            if (sprite != null)
                return sprite;

            Texture2D texture = Resources.Load<Texture2D>("UI/Game/" + assetName);
            if (texture != null)
                return CreateSprite(texture);

            return null;
        }

        private static Sprite CreateSprite(Texture2D texture)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // Skins the scene-built HUD (end turn button + label backgrounds) without
        // touching any behaviour that is already wired on those objects.
        private void ApplySceneSkin()
        {
            Transform canvas = transform.parent;
            if (canvas == null)
                return;

            Canvas.ForceUpdateCanvases();

            Transform endTurn = canvas.Find("EndTurnButton");
            if (endTurn != null)
            {
                Image endTurnImage = endTurn.GetComponent<Image>();
                Sprite endTurnSprite = LoadGameSprite("EndTurnButton");
                if (endTurnImage != null && endTurnSprite != null)
                {
                    endTurnImage.sprite = endTurnSprite;
                    endTurnImage.type = Image.Type.Simple;
                    endTurnImage.preserveAspect = false;
                }
            }

            PlaceBackground(canvas.Find("TurnLabel"), "TurnBG", LoadGameSprite("TurnBG"), new Vector2(400f, 143f));
            PlaceBackground(canvas.Find("SummonPointsLabel"), "SummonPointsBG", LoadGameSprite("SummonPointsBG"), new Vector2(440f, 157f));
        }

        // Creates an image on the label's own parent and inserts it directly before
        // the label so it renders behind the text, centred on the label.
        private static void PlaceBackground(Transform label, string name, Sprite sprite, Vector2 size)
        {
            if (label == null || sprite == null || label.parent == null)
                return;

            // Already placed (e.g. baked into the scene) - don't add a duplicate.
            if (label.parent.Find(name) != null)
                return;

            RectTransform labelRect = label as RectTransform;
            if (labelRect == null)
                return;

            Vector3[] corners = new Vector3[4];
            labelRect.GetWorldCorners(corners);
            Vector3 center = (corners[0] + corners[2]) * 0.5f;

            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(label.parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.position = center;

            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;

            go.transform.SetSiblingIndex(label.GetSiblingIndex());
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

        private static Sprite GetFillSprite()
        {
            if (cachedFillSprite == null)
                cachedFillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);

            return cachedFillSprite;
        }
    }
}
