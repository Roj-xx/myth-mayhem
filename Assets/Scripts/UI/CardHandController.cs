using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MythMayhem.Audio;
using MythMayhem.Gameplay;

namespace MythMayhem.UI
{
    public class CardHandController : MonoBehaviour
    {
        private const string InstructionMessage = "Drag a card onto a lane to play it";

        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardBackgrounds;
        [SerializeField] private Text instructionLabel;
        [SerializeField] private DeckController playerDeck;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private SummonController summonController;
        [SerializeField] private int maxHandSlots = 10;

        [SerializeField] private float cardMargin = 16f;
        [SerializeField] private float cardGap = 10f;
        [SerializeField] private float cardBaseWidth = 280f;
        [SerializeField] private float cardMinWidth = 140f;
        [SerializeField] private float cardHeight = 240f;
        [SerializeField] private float cardAspect = 424f / 588f;
        [SerializeField] private float cardBottomOffset = 16f;

        [SerializeField] private Vector2 previewSize = new Vector2(560f, 780f);
        [SerializeField] private Vector2 previewCenter = new Vector2(0f, 100f);
        [SerializeField] private float previewBackdropAlpha = 0.72f;

        private readonly List<Button> handButtons = new List<Button>();
        private readonly List<Image> handBackgrounds = new List<Image>();
        private readonly List<Text> handLabels = new List<Text>();
        private readonly List<Image> handArt = new List<Image>();

        private int lastSignature = int.MinValue;

        public int SelectedIndex { get; private set; } = -1;
        public bool HasSelection => SelectedIndex >= 0 && IsSlotPlayable(SelectedIndex);
        public int HandCount => playerDeck != null ? playerDeck.HandCount : 0;
        public int RemainingCards => HandCount;
        public int MaxSlotCount => playerDeck != null ? playerDeck.MaxHandSize : maxHandSlots;

        public CreatureDefinition SelectedDefinition
        {
            get
            {
                if (playerDeck == null || SelectedIndex < 0 || SelectedIndex >= MaxSlotCount)
                    return null;

                DeckCard card = playerDeck.GetHandCard(SelectedIndex);
                return card != null ? card.CreatureDefinition : null;
            }
        }

        public DeckCard SelectedCard
        {
            get
            {
                if (playerDeck == null || SelectedIndex < 0 || SelectedIndex >= MaxSlotCount)
                    return null;

                return playerDeck.GetHandCard(SelectedIndex);
            }
        }

        public CardType SelectedCardType => SelectedCard != null ? SelectedCard.Type : CardType.Creature;

        public SupportCardDefinition SelectedSupportDefinition
        {
            get
            {
                DeckCard card = SelectedCard;
                return card != null ? card.SupportDefinition : null;
            }
        }

        public BuildingDefinition SelectedBuildingDefinition
        {
            get
            {
                DeckCard card = SelectedCard;
                return card != null ? card.BuildingDefinition : null;
            }
        }

        public event Action<int> SelectionChanged;

        // Preview
        private GameObject previewRoot;
        private GameObject previewCard;
        private Image previewArt;
        private Text previewLabel;
        private GameObject previewBackdrop;

        // Drag ghost
        private GameObject dragGhost;
        private GameObject dragGhostCard;
        private Image dragGhostArt;
        private Text dragGhostLabel;
        private RectTransform canvasRect;
        private bool dragging;

        private void Awake()
        {
            BuildSlots();
            BuildPreview();
            ResolveCanvas();
            if (summonController == null)
                summonController = FindFirstObjectByType<SummonController>();
        }

        private void ResolveCanvas()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;
            canvasRect = canvas.GetComponent<RectTransform>();
        }

        private void BuildSlots()
        {
            handButtons.Clear();
            handBackgrounds.Clear();
            handLabels.Clear();
            handArt.Clear();

            int slotCount = Mathf.Max(1, MaxSlotCount);

            if (cardButtons != null)
            {
                for (int i = 0; i < cardButtons.Length; i++)
                {
                    if (cardButtons[i] == null)
                        continue;

                    handButtons.Add(cardButtons[i]);

                    Image background = cardBackgrounds != null && i < cardBackgrounds.Length
                        ? cardBackgrounds[i]
                        : null;
                    if (background == null)
                        background = cardButtons[i].GetComponent<Image>();
                    handBackgrounds.Add(background);

                    Text label = cardButtons[i].GetComponentInChildren<Text>();
                    handLabels.Add(label);
                }
            }

            Button template = null;
            for (int i = 0; i < handButtons.Count; i++)
            {
                if (handButtons[i] != null)
                {
                    template = handButtons[i];
                    break;
                }
            }

            if (template == null)
                return;

            while (handButtons.Count < slotCount)
            {
                GameObject clone = Instantiate(template.gameObject, transform);
                clone.name = "HandCard_" + (handButtons.Count + 1);

                RectTransform rect = clone.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.localScale = Vector3.one;
                }

                handButtons.Add(clone.GetComponent<Button>());
                handBackgrounds.Add(clone.GetComponent<Image>());
                handLabels.Add(clone.GetComponentInChildren<Text>());
            }

            for (int i = 0; i < handButtons.Count; i++)
            {
                handArt.Add(handButtons[i] != null ? EnsureArt(handButtons[i].transform) : null);
            }

            for (int i = 0; i < handButtons.Count; i++)
            {
                if (handButtons[i] == null)
                    continue;

                int index = i;
                // Remove old onClick listeners (none persisted in code but safe)
                Button b = handButtons[i];
                b.onClick.RemoveAllListeners();
                AudioManager.HookButton(b);

                // Add drag handler
                MythMayhem.UI.CardDragHandler handler = handButtons[i].GetComponent<MythMayhem.UI.CardDragHandler>();
                if (handler == null)
                    handler = handButtons[i].gameObject.AddComponent<MythMayhem.UI.CardDragHandler>();
                handler.Configure(this, index);

                Text label = i < handLabels.Count ? handLabels[i] : null;
                if (label != null)
                {
                    int originalSize = Mathf.Max(8, label.fontSize);
                    label.resizeTextForBestFit = true;
                    label.resizeTextMinSize = 8;
                    label.resizeTextMaxSize = originalSize;
                }
            }
        }

        private void Update()
        {
            if (ComputeSignature() == lastSignature)
                return;

            RefreshInteractivity();
        }

        public void RefreshInteractivity()
        {
            lastSignature = ComputeSignature();

            if (handButtons.Count == 0)
                return;

            bool playerTurn = turnManager == null || turnManager.IsActive(NetworkSession.LocalSide);
            int handCount = playerDeck != null ? playerDeck.HandCount : 0;

            for (int i = 0; i < handButtons.Count; i++)
            {
                Button button = handButtons[i];
                if (button == null)
                    continue;

                bool hasCard = handCount > 0 && i < handCount;
                button.gameObject.SetActive(hasCard);
                if (!hasCard)
                    continue;

                DeckCard card = playerDeck.GetHandCard(i);
                bool playable = card != null && card.HasValidDefinition;

                Sprite art = ArtworkOf(card);
                Image artImage = i < handArt.Count ? handArt[i] : null;
                if (artImage != null)
                {
                    artImage.enabled = art != null;
                    artImage.sprite = art;
                }

                if (i < handLabels.Count && handLabels[i] != null)
                    handLabels[i].text = art != null ? string.Empty : BuildCardLabel(card);

                button.interactable = playerTurn && playable;

                // The card artwork is the whole card now, so the placeholder box is
                // kept invisible; it still serves as the button's raycast target.
                if (i < handBackgrounds.Count && handBackgrounds[i] != null)
                    handBackgrounds[i].color = Color.clear;
            }

            RebuildLayout(handCount);

            // If selection is stale or preview should update
            if (SelectedIndex < 0 || SelectedIndex >= handCount)
            {
                HidePreview();
            }
            else if (previewRoot != null && previewRoot.activeSelf)
            {
                ShowPreview(SelectedIndex);
            }
        }

        private int ComputeSignature()
        {
            int handCount = playerDeck != null ? playerDeck.HandCount : 0;
            int signature = handCount;
            signature = (signature * 397) ^ SelectedIndex;
            signature = (signature * 397) ^ ((turnManager == null || turnManager.IsActive(NetworkSession.LocalSide)) ? 1 : 0);

            RectTransform panel = transform as RectTransform;
            int width = panel != null ? Mathf.RoundToInt(panel.rect.width) : Screen.width;
            signature = (signature * 397) ^ width;

            for (int i = 0; i < handCount; i++)
            {
                DeckCard card = playerDeck.GetHandCard(i);
                int id = card != null && card.DefinitionName != null ? card.DefinitionName.GetHashCode() : 0;
                signature = (signature * 397) ^ id;
            }

            return signature;
        }

        private void RebuildLayout(int cardCount)
        {
            RectTransform panel = transform as RectTransform;
            float panelWidth = panel != null && panel.rect.width > 1f
                ? panel.rect.width
                : Screen.width;

            int n = Mathf.Max(1, cardCount);
            float available = Mathf.Max(10f, panelWidth - 2f * cardMargin);

            float gap = cardGap;
            float width = Mathf.Min(cardBaseWidth, (available - gap * (n - 1)) / n);
            if (width < cardMinWidth && n > 1)
            {
                gap = Mathf.Max(2f, cardGap * 0.5f);
                width = (available - gap * (n - 1)) / n;
                width = Mathf.Max(24f, width);
            }

            float aspect = cardAspect > 0.01f ? cardAspect : 0.7214f;
            float height = width / aspect;
            if (height > cardHeight)
            {
                height = cardHeight;
                width = height * aspect;
            }

            float totalWidth = width * n + gap * (n - 1);
            float startX = -totalWidth * 0.5f + width * 0.5f;

            for (int i = 0; i < handButtons.Count && i < n; i++)
            {
                if (handButtons[i] == null)
                    continue;

                RectTransform rect = handButtons[i].transform as RectTransform;
                if (rect == null)
                    continue;

                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.localScale = Vector3.one;
                rect.sizeDelta = new Vector2(width, height);
                rect.anchoredPosition = new Vector2(startX + i * (width + gap), cardBottomOffset);
            }
        }

        private void OnDestroy()
        {
            foreach (Button button in handButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    MythMayhem.UI.CardDragHandler h = button.GetComponent<MythMayhem.UI.CardDragHandler>();
                    if (h != null)
                        Destroy(h);
                }
            }
            if (previewRoot != null)
                Destroy(previewRoot);
            if (dragGhost != null)
                Destroy(dragGhost);
        }

        private bool IsSlotPlayable(int index)
        {
            if (index < 0 || index >= MaxSlotCount || playerDeck == null || index >= playerDeck.HandCount)
                return false;

            DeckCard card = playerDeck.GetHandCard(index);
            return card != null && card.HasValidDefinition;
        }

        private static Image EnsureArt(Transform parent)
        {
            Transform existing = parent.Find("CardArt");
            if (existing != null)
            {
                Image found = existing.GetComponent<Image>();
                if (found != null)
                    return found;
            }

            GameObject go = new GameObject("CardArt", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 6f);
            rect.offsetMax = new Vector2(-6f, -6f);

            Image image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.color = Color.white;
            image.enabled = false;
            return image;
        }

        private static Sprite ArtworkOf(DeckCard card)
        {
            if (card == null)
                return null;

            switch (card.Type)
            {
                case CardType.Support:
                    return card.SupportDefinition != null ? card.SupportDefinition.Artwork : null;
                case CardType.Building:
                    return card.BuildingDefinition != null ? card.BuildingDefinition.Artwork : null;
                default:
                    return card.CreatureDefinition != null ? card.CreatureDefinition.Artwork : null;
            }
        }

        private static string BuildCardLabel(DeckCard card)
        {
            if (card == null)
                return "?";

            switch (card.Type)
            {
                case CardType.Support:
                {
                    SupportCardDefinition support = card.SupportDefinition;
                    if (support == null)
                        return "?";
                    return support.DisplayName + "\n" + Flatten(support.Description) + "\nCost " + support.Cost;
                }
                case CardType.Building:
                {
                    BuildingDefinition building = card.BuildingDefinition;
                    if (building == null)
                        return "?";
                    return building.DisplayName + "\n" + Flatten(building.Description) + "\nCost " + building.Cost;
                }
                default:
                {
                    CreatureDefinition creature = card.CreatureDefinition;
                    if (creature == null)
                        return "?";
                    return creature.DisplayName + "\nATK " + creature.Attack + "  HP " + creature.MaxHealth + "\nCost " + creature.Cost;
                }
            }
        }

        private static string Flatten(string text)
        {
            return string.IsNullOrEmpty(text) ? "" : text.Replace('\n', ' ');
        }

        // Interaction handlers
        public void HandleCardClicked(int index)
        {
            if (SelectedIndex == index)
            {
                ClosePreview();
                return;
            }

            if (!IsSlotPlayable(index))
                return;

            SelectedIndex = index;
            if (instructionLabel != null)
                instructionLabel.text = InstructionMessage;
            SelectionChanged?.Invoke(index);
            ShowPreview(index);
            RefreshInteractivity();
        }

        public void HandleCardBeginDrag(int index, PointerEventData eventData)
        {
            bool playerTurn = turnManager == null || turnManager.IsActive(NetworkSession.LocalSide);
            if (!playerTurn || !IsSlotPlayable(index))
            {
                dragging = false;
                return;
            }

            SelectedIndex = index;
            dragging = true;
            HidePreview();
            EnsureDragGhost();
            ApplyCardVisual(dragGhostArt, dragGhostLabel, playerDeck.GetHandCard(index));
            if (dragGhost != null)
            {
                dragGhost.SetActive(true);
                PositionGhost(eventData.position);
            }
            SelectionChanged?.Invoke(index);
            RefreshInteractivity();
        }

        public void HandleCardDrag(PointerEventData eventData)
        {
            if (!dragging)
                return;
            PositionGhost(eventData.position);
        }

        public void HandleCardEndDrag(PointerEventData eventData)
        {
            if (dragGhost != null)
                dragGhost.SetActive(false);
            bool wasDragging = dragging;
            dragging = false;

            if (!wasDragging)
                return;

            if (summonController != null)
                summonController.TryPlaySelectedAt(eventData.position);
            else
            {
                // Fallback: try to find and play
                summonController = FindFirstObjectByType<SummonController>();
                if (summonController != null)
                    summonController.TryPlaySelectedAt(eventData.position);
            }

            // Do not clear selection here; SummonController consumes it on success.
            // If play failed, selection remains so user can retry.
        }

        public void ClosePreview()
        {
            SelectedIndex = -1;
            HidePreview();
            if (instructionLabel != null)
                instructionLabel.text = InstructionMessage;
            SelectionChanged?.Invoke(-1);
            RefreshInteractivity();
        }

        private void SetSelectionSilently(int index)
        {
            SelectedIndex = index;
        }

        // Preview and ghost
        private void BuildPreview()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;

            previewRoot = new GameObject("CardPreview", typeof(RectTransform));
            previewRoot.transform.SetParent(parent, false);
            RectTransform rootRect = previewRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.localScale = Vector3.one;

            // backdrop
            previewBackdrop = new GameObject("Backdrop", typeof(RectTransform));
            previewBackdrop.transform.SetParent(previewRoot.transform, false);
            RectTransform bd = previewBackdrop.GetComponent<RectTransform>();
            bd.anchorMin = Vector2.zero;
            bd.anchorMax = Vector2.one;
            bd.offsetMin = Vector2.zero;
            bd.offsetMax = Vector2.zero;
            Image bdImg = previewBackdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0f, 0f, previewBackdropAlpha);
            bdImg.raycastTarget = false;

            // card visual
            previewCard = BuildCardVisual("PreviewCard", previewRoot.transform, previewSize, out previewArt, out previewLabel);
            RectTransform pcRect = previewCard.GetComponent<RectTransform>();
            pcRect.anchoredPosition = previewCenter;
            pcRect.localScale = Vector3.one;
            Image pcBg = previewCard.GetComponent<Image>();
            if (pcBg != null)
            {
                pcBg.raycastTarget = true;
                Button pcBtn = previewCard.AddComponent<Button>();
                AudioManager.HookButton(pcBtn);
                pcBtn.targetGraphic = pcBg;
                pcBtn.onClick.AddListener(ClosePreview);
            }
            // enlarge label for preview
            if (previewLabel != null)
            {
                previewLabel.fontSize = 56;
                previewLabel.resizeTextForBestFit = true;
                previewLabel.resizeTextMinSize = 24;
                previewLabel.resizeTextMaxSize = 56;
            }

            previewRoot.SetActive(false);
        }

        private GameObject BuildCardVisual(string name, Transform parent, Vector2 size, out Image artOut, out Text labelOut)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            // No painted box behind the card: the artwork is the whole card. The
            // image stays as the preview button's click target.
            Image bg = go.AddComponent<Image>();
            bg.color = Color.clear;
            bg.raycastTarget = false;

            GameObject artGo = new GameObject("Art", typeof(RectTransform));
            artGo.transform.SetParent(go.transform, false);
            RectTransform artRect = artGo.GetComponent<RectTransform>();
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = new Vector2(10f, 10f);
            artRect.offsetMax = new Vector2(-10f, -10f);
            artOut = artGo.AddComponent<Image>();
            artOut.raycastTarget = false;
            artOut.preserveAspect = true;
            artOut.enabled = false;
            artOut.color = Color.white;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 16f);
            labelRect.offsetMax = new Vector2(-16f, -16f);
            labelOut = labelGo.AddComponent<Text>();
            labelOut.font = CardFont();
            labelOut.alignment = TextAnchor.MiddleCenter;
            labelOut.color = Color.white;
            labelOut.horizontalOverflow = HorizontalWrapMode.Wrap;
            labelOut.verticalOverflow = VerticalWrapMode.Overflow;
            labelOut.raycastTarget = false;
            labelOut.fontSize = 14;
            labelOut.resizeTextForBestFit = true;
            labelOut.resizeTextMinSize = 8;
            labelOut.resizeTextMaxSize = 14;

            return go;
        }

        private static Font CardFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return font;
        }

        private void ShowPreview(int index)
        {
            if (previewRoot == null)
                BuildPreview();
            DeckCard card = playerDeck != null ? playerDeck.GetHandCard(index) : null;
            if (card == null)
            {
                HidePreview();
                return;
            }
            ApplyCardVisual(previewArt, previewLabel, card);
            if (previewLabel != null)
            {
                previewLabel.fontSize = 56;
                previewLabel.resizeTextForBestFit = true;
                previewLabel.resizeTextMinSize = 24;
                previewLabel.resizeTextMaxSize = 56;
            }
            previewRoot.SetActive(true);
            previewRoot.transform.SetAsLastSibling();
        }

        private void HidePreview()
        {
            if (previewRoot != null)
                previewRoot.SetActive(false);
        }

        private void EnsureDragGhost()
        {
            if (dragGhost != null)
                return;
            Canvas canvas = GetComponentInParent<Canvas>();
            Transform parent = canvas != null ? canvas.transform : transform;
            // get card size from first visible button for ghost size
            Vector2 ghostSize = new Vector2(220f, 300f);
            if (handButtons.Count > 0 && handButtons[0] != null)
            {
                RectTransform hr = handButtons[0].GetComponent<RectTransform>();
                if (hr != null)
                    ghostSize = hr.sizeDelta * 1.15f;
            }
            dragGhost = new GameObject("CardDragGhost", typeof(RectTransform));
            dragGhost.transform.SetParent(parent, false);
            RectTransform gr = dragGhost.GetComponent<RectTransform>();
            gr.anchorMin = new Vector2(0.5f, 0.5f);
            gr.anchorMax = new Vector2(0.5f, 0.5f);
            gr.pivot = new Vector2(0.5f, 0.5f);
            gr.sizeDelta = ghostSize;
            // No painted box behind the dragged card either; only the artwork shows.
            Image grBg = dragGhost.AddComponent<Image>();
            grBg.color = Color.clear;
            grBg.raycastTarget = false;

            GameObject artGo = new GameObject("Art", typeof(RectTransform));
            artGo.transform.SetParent(dragGhost.transform, false);
            RectTransform artRect = artGo.GetComponent<RectTransform>();
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = new Vector2(10f, 10f);
            artRect.offsetMax = new Vector2(-10f, -10f);
            dragGhostArt = artGo.AddComponent<Image>();
            dragGhostArt.raycastTarget = false;
            dragGhostArt.preserveAspect = true;
            dragGhostArt.enabled = false;
            dragGhostArt.color = Color.white;

            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(dragGhost.transform, false);
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 16f);
            labelRect.offsetMax = new Vector2(-16f, -16f);
            dragGhostLabel = labelGo.AddComponent<Text>();
            dragGhostLabel.font = CardFont();
            dragGhostLabel.alignment = TextAnchor.MiddleCenter;
            dragGhostLabel.color = Color.white;
            dragGhostLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            dragGhostLabel.verticalOverflow = VerticalWrapMode.Overflow;
            dragGhostLabel.raycastTarget = false;
            dragGhostLabel.fontSize = 28;
            dragGhostLabel.resizeTextForBestFit = true;
            dragGhostLabel.resizeTextMinSize = 10;
            dragGhostLabel.resizeTextMaxSize = 28;

            dragGhost.SetActive(false);
        }

        private void ApplyCardVisual(Image art, Text label, DeckCard card)
        {
            if (art == null || label == null)
                return;
            Sprite s = ArtworkOf(card);
            if (s != null)
            {
                art.enabled = true;
                art.sprite = s;
                label.text = string.Empty;
            }
            else
            {
                art.enabled = false;
                art.sprite = null;
                label.text = BuildCardLabel(card);
            }
        }

        private void PositionGhost(Vector2 screenPosition)
        {
            if (dragGhost == null)
                return;
            if (canvasRect == null)
                ResolveCanvas();
            if (canvasRect == null)
                return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 local);
            RectTransform gr = dragGhost.GetComponent<RectTransform>();
            if (gr != null)
                gr.anchoredPosition = local;
        }

        public void ConsumeSelected()
        {
            if (!HasSelection)
                return;

            int index = SelectedIndex;
            if (playerDeck != null)
                playerDeck.RemoveFromHand(index);

            SelectedIndex = -1;
            HidePreview();
            if (instructionLabel != null)
                instructionLabel.text = InstructionMessage;

            RefreshInteractivity();
        }
    }
}
