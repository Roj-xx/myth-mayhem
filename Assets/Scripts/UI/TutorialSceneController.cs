using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MythMayhem.UI
{
    public class TutorialSceneController : MonoBehaviour
    {
        private struct TutorialSlide
        {
            public readonly string Header;
            public readonly string Body;

            public TutorialSlide(string header, string body)
            {
                Header = header;
                Body = body;
            }
        }

        /// <summary>
        /// Mirrors the "Container" hierarchy authored in Assets/Scenes/Tutorial.unity.
        /// Every runtime object is named, placed and styled from these values so the
        /// slideshow looks exactly like the scene template.
        /// </summary>
        private static class Template
        {
            public const string SlideImageName = "Image";
            public const string HeaderName = "Header";
            public const string BodyName = "Body";
            public const string BackButtonName = "MainMenuButton";
            public const string NextButtonName = "NextButton";
            public const string ButtonLabelName = "Label";

            public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
            public static readonly Vector2 TopLeft = new Vector2(0f, 1f);

            public static readonly Vector2 SlideImagePosition = new Vector2(-837f, 93f);
            public static readonly Vector2 SlideImageSize = new Vector2(700f, 850f);

            public static readonly Vector2 HeaderPosition = new Vector2(398f, 387f);
            public static readonly Vector2 HeaderSize = new Vector2(1700f, 150f);
            public const int HeaderFontSize = 100;

            public static readonly Vector2 BodyPosition = new Vector2(398f, -37f);
            public static readonly Vector2 BodySize = new Vector2(1700f, 600f);
            public const int BodyFontSize = 75;

            public static readonly Vector2 BackButtonPosition = new Vector2(605f, -1198f);
            public static readonly Vector2 NextButtonPosition = new Vector2(2364f, -1198f);
            public static readonly Vector2 ButtonSize = new Vector2(700f, 162f);

            public static readonly Vector2 ButtonLabelPosition = new Vector2(0f, 5f);
            public static readonly Vector2 ButtonLabelSize = new Vector2(900f, 150f);
            public const int ButtonLabelFontSize = 65;
        }

        private static readonly TutorialSlide[] Slides =
        {
            new TutorialSlide(
                "THE ARENA",
                "The battlefield has three lanes. Each lane can hold one creature and one building on your side. Plan where each card will help most."),
            new TutorialSlide(
                "CREATURE CARDS",
                "Creature cards summon fighters. Drag one from your hand to an open lane. Its attack and health decide how it fights, and its cost uses summon points."),
            new TutorialSlide(
                "BUILDING CARDS",
                "Buildings support the lane where you place them. Some protect your hero, while others help your creature hit harder. One creature and one building can share a lane."),
            new TutorialSlide(
                "SUPPORT CARDS",
                "Support cards can boost or heal your creatures, or weaken an enemy for a short time. Use them on a lane where the effect will matter most."),
            new TutorialSlide(
                "YOUR TURN",
                "You begin with 5 summon points. Drag cards from your hand onto a lane to play them, then press End Turn when you are ready. At the start of your next turn, you gain points and draw a card."),
            new TutorialSlide(
                "TURN TIMER",
                "Each turn has a 60 second timer. If time runs out before you end your turn, it will end automatically for you. When 10 seconds or less remain, a countdown will appear on the turn label."),
            new TutorialSlide(
                "FIGHT AND WIN",
                "After both players have taken a turn, creatures fight in their lanes. An enemy building can take a hit instead of its hero. Bring the other hero's health to 0 to win; if yours reaches 0, you lose. If both reach 0 together, it is a draw.")
        };

        [Header("Slides")]
        [SerializeField] private Sprite[] slideImages;

        [Header("Template assets")]
        [SerializeField] private Font headerFont;
        [SerializeField] private Font bodyFont;
        [SerializeField] private Sprite backButtonSprite;
        [SerializeField] private Sprite nextButtonSprite;

        [Header("Labels")]
        [SerializeField] private string backButtonLabel = "Main Menu";
        [SerializeField] private string nextButtonLabel = "Proceed";

        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private Image slideImage;
        private Text headerText;
        private Text bodyText;
        private Button backButton;
        private Button nextButton;
        private int currentSlide;

        private void Awake()
        {
            EnsureEventSystem();

            Transform container = transform.Find("Container");
            if (container == null)
            {
                Debug.LogError("[Tutorial] The TutorialUI object needs a Container child.");
                enabled = false;
                return;
            }

            Font fallbackFont = FindFallbackFont();
            if (fallbackFont == null)
            {
                Debug.LogError("[Tutorial] Could not find a font for the tutorial controls.");
                enabled = false;
                return;
            }

            ResolveFonts(container, fallbackFont);
            RemoveLegacyObjects(container);
            CreateControls(container);
            ValidateSlideImages();
            ShowSlide(0);
        }

        private void OnDestroy()
        {
            if (backButton != null)
                backButton.onClick.RemoveListener(ReturnToMainMenu);

            if (nextButton != null)
                nextButton.onClick.RemoveListener(ShowNextSlide);
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));

            InputSystemUIInputModule inputModule = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        private Font FindFallbackFont()
        {
            Transform label = transform.Find("Background/Label");
            Text existingText = label != null ? label.GetComponent<Text>() : null;
            return existingText != null ? existingText.font : null;
        }

        private void ResolveFonts(Transform container, Font fallbackFont)
        {
            if (headerFont == null)
                headerFont = FindFontOnChild(container, Template.HeaderName) ?? fallbackFont;

            if (bodyFont == null)
                bodyFont = FindFontOnChild(container, Template.BodyName) ?? headerFont;
        }

        private static Font FindFontOnChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            Text text = child != null ? child.GetComponent<Text>() : null;
            return text != null ? text.font : null;
        }

        /// <summary>
        /// The slideshow used to build its own "Text" body and "BacktoMainMenuButton"
        /// objects. Those leftovers are removed so they cannot overlap the template.
        /// </summary>
        private static void RemoveLegacyObjects(Transform container)
        {
            RemoveChild(container, "Text");
            RemoveChild(container, "BacktoMainMenuButton");
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
                return;

            child.gameObject.SetActive(false);
            Object.Destroy(child.gameObject);
        }

        private void CreateControls(Transform container)
        {
            slideImage = CreateImage(
                container,
                Template.SlideImageName,
                Template.SlideImagePosition,
                Template.SlideImageSize);

            headerText = CreateText(
                container,
                Template.HeaderName,
                string.Empty,
                headerFont,
                Template.HeaderFontSize,
                TextAnchor.UpperLeft,
                Template.HeaderPosition,
                Template.HeaderSize,
                Color.white);

            bodyText = CreateText(
                container,
                Template.BodyName,
                string.Empty,
                bodyFont,
                Template.BodyFontSize,
                TextAnchor.UpperLeft,
                Template.BodyPosition,
                Template.BodySize,
                Color.white);

            backButton = CreateButton(
                container,
                Template.BackButtonName,
                backButtonLabel,
                bodyFont,
                Template.BackButtonPosition,
                Template.ButtonSize,
                backButtonSprite);

            nextButton = CreateButton(
                container,
                Template.NextButtonName,
                nextButtonLabel,
                bodyFont,
                Template.NextButtonPosition,
                Template.ButtonSize,
                nextButtonSprite);

            backButton.onClick.RemoveListener(ReturnToMainMenu);
            backButton.onClick.AddListener(ReturnToMainMenu);
            nextButton.onClick.RemoveListener(ShowNextSlide);
            nextButton.onClick.AddListener(ShowNextSlide);
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject gameObject = GetOrCreateChild(parent, name);
            if (gameObject.GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();

            Image image = gameObject.GetComponent<Image>();
            if (image == null)
                image = gameObject.AddComponent<Image>();

            ApplyRect(gameObject.GetComponent<RectTransform>(), Template.Center, Template.Center, position, size);

            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;

            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string value,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Vector2 position,
            Vector2 size,
            Color color)
        {
            GameObject gameObject = GetOrCreateChild(parent, name);
            if (gameObject.GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();

            Text text = gameObject.GetComponent<Text>();
            if (text == null)
                text = gameObject.AddComponent<Text>();

            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = true;
            text.resizeTextForBestFit = false;
            text.alignByGeometry = false;
            text.lineSpacing = 1f;
            text.raycastTarget = true;

            ApplyRect(gameObject.GetComponent<RectTransform>(), Template.Center, Template.Center, position, size);

            return text;
        }

        private Button CreateButton(
            Transform parent,
            string name,
            string label,
            Font font,
            Vector2 position,
            Vector2 size,
            Sprite sprite)
        {
            GameObject gameObject = GetOrCreateChild(parent, name);
            if (gameObject.GetComponent<CanvasRenderer>() == null)
                gameObject.AddComponent<CanvasRenderer>();

            Image background = gameObject.GetComponent<Image>();
            if (background == null)
                background = gameObject.AddComponent<Image>();

            Button button = gameObject.GetComponent<Button>();
            if (button == null)
                button = gameObject.AddComponent<Button>();

            button.targetGraphic = background;
            ApplyButtonColors(button);

            background.sprite = sprite;
            background.color = Color.white;
            background.preserveAspect = true;
            background.raycastTarget = true;
            background.type = Image.Type.Simple;

            ApplyRect(gameObject.GetComponent<RectTransform>(), Template.TopLeft, Template.TopLeft, position, size);

            // The template buttons can still carry a copy/pasted persistent call to
            // MainMenuController.ShowTutorial, which would reload the tutorial scene.
            DisablePersistentListeners(button);
            button.onClick.RemoveAllListeners();

            CreateText(
                gameObject.transform,
                Template.ButtonLabelName,
                label,
                font,
                Template.ButtonLabelFontSize,
                TextAnchor.MiddleCenter,
                Template.ButtonLabelPosition,
                Template.ButtonLabelSize,
                Color.white);

            return button;
        }

        private static void DisablePersistentListeners(Button button)
        {
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
        }

        private static void ApplyButtonColors(Button button)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.18f, 1.18f, 1.18f, 1f);
            colors.pressedColor = new Color(0.85f, 0.82f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;
            button.colors = colors;
            button.transition = Selectable.Transition.ColorTint;
        }

        private static GameObject GetOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing.gameObject;

            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.layer = parent.gameObject.layer;
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static void ApplyRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = Template.Center;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        private void ValidateSlideImages()
        {
            if (slideImages == null || slideImages.Length != Slides.Length)
            {
                Debug.LogError("[Tutorial] Assign one image for each tutorial slide.");
                return;
            }

            for (int i = 0; i < slideImages.Length; i++)
            {
                if (slideImages[i] == null)
                    Debug.LogError("[Tutorial] Slide " + (i + 1) + " is missing its image.");
            }
        }

        private void ShowNextSlide()
        {
            ShowSlide(currentSlide + 1);
        }

        private void ShowSlide(int index)
        {
            if (index < 0 || index >= Slides.Length)
                return;

            currentSlide = index;
            headerText.text = Slides[index].Header;
            bodyText.text = Slides[index].Body;
            slideImage.sprite = slideImages != null && index < slideImages.Length
                ? slideImages[index]
                : null;
            nextButton.gameObject.SetActive(index < Slides.Length - 1);
        }

        private void ReturnToMainMenu()
        {
            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                Debug.LogError("[Tutorial] Main menu scene name is not set.");
                return;
            }

            SceneTransitionManager.LoadSceneWithFade(mainMenuSceneName);
        }
    }
}
