using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MythMayhem.UI;

public static class PvPLobbySceneUiSetup
{
    private const string ModalPath = "Assets/Art/UI/Modal.png";
    private const string PrimaryButtonPath = "Assets/Art/UI/Buttons/ButtonYellow.png";
    private const string SecondaryButtonPath = "Assets/Art/UI/Buttons/ButtonBlue.png";

    [MenuItem("Myth Mayhem/Create Editable PvP Lobby UI")]
    private static void CreateEditableLobbyUi()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "PvPLobby")
        {
            Debug.LogError("[Lobby UI] Open the PvPLobby scene before creating its editable UI.");
            return;
        }

        LobbyMenuController controller = Object.FindFirstObjectByType<LobbyMenuController>();
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (controller == null || canvas == null)
        {
            Debug.LogError("[Lobby UI] The PvPLobby scene needs its Canvas and LobbyMenuController.");
            return;
        }

        if (controller.transform.Find("LobbyUI") != null)
        {
            Debug.LogWarning("[Lobby UI] Editable lobby UI already exists. Edit it in the Hierarchy.");
            return;
        }

        SerializedObject serializedController = new SerializedObject(controller);
        Font font = serializedController.FindProperty("uiFont").objectReferenceValue as Font;
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        Sprite modal = LoadSprite(ModalPath, "Modal_0");
        Sprite primaryButton = LoadSprite(PrimaryButtonPath, "ButtonYellow_0");
        Sprite secondaryButton = LoadSprite(SecondaryButtonPath, "ButtonBlue_0");
        if (modal == null || primaryButton == null || secondaryButton == null)
        {
            Debug.LogError("[Lobby UI] Could not load one or more lobby style sprites.");
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create Editable PvP Lobby UI");

        GameObject root = CreateUiObject("LobbyUI", canvas.transform);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        GameObject scrim = CreateUiObject("TransparentScrim", root.transform, typeof(Image));
        Stretch(scrim.GetComponent<RectTransform>());
        Image scrimImage = scrim.GetComponent<Image>();
        scrimImage.color = new Color(0f, 0f, 0f, 0f);
        scrimImage.raycastTarget = false;

        GameObject card = CreateUiObject("LobbyModal", scrim.transform, typeof(Image));
        RectTransform cardRect = card.GetComponent<RectTransform>();
        Center(cardRect, new Vector2(1000f, 740f), Vector2.zero);
        Image cardImage = card.GetComponent<Image>();
        cardImage.sprite = modal;
        cardImage.color = Color.white;
        cardImage.preserveAspect = false;
        cardImage.raycastTarget = false;

        Text title = CreateLabel(card.transform, "Title", "PVP LOBBY", font, 56,
            new Vector2(0f, 280f), new Vector2(820f, 76f), new Color(0.96f, 0.82f, 0.42f));
        Text status = CreateLabel(card.transform, "Status", string.Empty, font, 26,
            new Vector2(0f, 190f), new Vector2(820f, 100f), new Color(0.62f, 0.67f, 0.78f));
        Text code = CreateLabel(card.transform, "Code", string.Empty, font, 38,
            new Vector2(0f, 90f), new Vector2(820f, 60f), new Color(0.93f, 0.94f, 0.97f));

        InputField input = CreateInput(card.transform, font, new Vector2(0f, 0f), new Vector2(760f, 76f));
        Button create = CreateButton(card.transform, "CreateButton", "CREATE LOBBY", font,
            new Vector2(0f, -100f), new Vector2(620f, 106f), primaryButton);
        Button join = CreateButton(card.transform, "JoinButton", "JOIN WITH CODE", font,
            new Vector2(0f, -210f), new Vector2(620f, 106f), secondaryButton);
        Button copy = CreateButton(card.transform, "CopyButton", "COPY CODE", font,
            new Vector2(-180f, -310f), new Vector2(300f, 72f), null);
        Button back = CreateButton(card.transform, "BackButton", "BACK TO MENU", font,
            new Vector2(180f, -310f), new Vector2(300f, 72f), null);

        serializedController.Update();
        serializedController.FindProperty("panelRoot").objectReferenceValue = root;
        serializedController.FindProperty("statusLabel").objectReferenceValue = status;
        serializedController.FindProperty("codeLabel").objectReferenceValue = code;
        serializedController.FindProperty("codeInput").objectReferenceValue = input;
        serializedController.FindProperty("createButton").objectReferenceValue = create;
        serializedController.FindProperty("joinButton").objectReferenceValue = join;
        serializedController.FindProperty("copyButton").objectReferenceValue = copy;
        serializedController.FindProperty("backButton").objectReferenceValue = back;
        serializedController.ApplyModifiedProperties();

        root.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log("[Lobby UI] Editable PvP lobby controls are in the Hierarchy under Canvas > LobbyUI. Save the scene to keep them.");
    }

    private static Sprite LoadSprite(string path, string subAssetName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object asset in assets)
        {
            if (asset is Sprite sprite && sprite.name == subAssetName)
                return sprite;
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreateUiObject(string name, Transform parent, params System.Type[] components)
    {
        System.Type[] types = new System.Type[components.Length + 1];
        types[0] = typeof(RectTransform);
        components.CopyTo(types, 1);

        GameObject gameObject = new GameObject(name, types);
        Undo.RegisterCreatedObjectUndo(gameObject, "Create PvP Lobby UI");
        Undo.SetTransformParent(gameObject.transform, parent, "Parent PvP Lobby UI Object");
        return gameObject;
    }

    private static Text CreateLabel(Transform parent, string name, string value, Font font, int size,
        Vector2 position, Vector2 dimensions, Color color)
    {
        GameObject gameObject = CreateUiObject(name, parent, typeof(CanvasRenderer), typeof(Text));
        Center(gameObject.GetComponent<RectTransform>(), dimensions, position);

        Text text = gameObject.GetComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Font font,
        Vector2 position, Vector2 dimensions, Sprite sprite)
    {
        GameObject gameObject = CreateUiObject(name, parent, typeof(CanvasRenderer), typeof(Image), typeof(Button));
        Center(gameObject.GetComponent<RectTransform>(), dimensions, position);

        Image background = gameObject.GetComponent<Image>();
        background.color = sprite != null ? Color.white : new Color(0.106f, 0.137f, 0.2f, 1f);
        background.sprite = sprite;
        background.preserveAspect = sprite != null;

        Button button = gameObject.GetComponent<Button>();
        button.targetGraphic = background;

        Text buttonLabel = CreateLabel(gameObject.transform, "Label", label, font, 30,
            Vector2.zero, new Vector2(dimensions.x - 24f, dimensions.y - 12f), new Color(0.93f, 0.94f, 0.97f));
        RectTransform labelRect = buttonLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 6f);
        labelRect.offsetMax = new Vector2(-12f, -6f);
        return button;
    }

    private static InputField CreateInput(Transform parent, Font font, Vector2 position, Vector2 dimensions)
    {
        GameObject gameObject = CreateUiObject("CodeInput", parent, typeof(CanvasRenderer), typeof(Image), typeof(InputField));
        Center(gameObject.GetComponent<RectTransform>(), dimensions, position);
        Image background = gameObject.GetComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.95f);

        InputField input = gameObject.GetComponent<InputField>();
        input.targetGraphic = background;
        input.characterLimit = 12;

        Text text = CreateLabel(gameObject.transform, "Text", string.Empty, font, 30,
            Vector2.zero, new Vector2(dimensions.x - 30f, dimensions.y - 16f), new Color(0.05f, 0.06f, 0.1f));
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        Stretch(text.rectTransform, new Vector2(15f, 8f), new Vector2(-15f, -8f));

        Text placeholder = CreateLabel(gameObject.transform, "Placeholder", "ENTER CODE", font, 30,
            Vector2.zero, new Vector2(dimensions.x - 30f, dimensions.y - 16f), new Color(0.3f, 0.33f, 0.4f));
        placeholder.alignment = TextAnchor.MiddleLeft;
        Stretch(placeholder.rectTransform, new Vector2(15f, 8f), new Vector2(-15f, -8f));

        input.textComponent = text;
        input.placeholder = placeholder;
        return input;
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect, Vector2? offsetMin = null, Vector2? offsetMax = null)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin ?? Vector2.zero;
        rect.offsetMax = offsetMax ?? Vector2.zero;
    }
}
