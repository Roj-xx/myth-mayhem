using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MythMayhem.Gameplay;

namespace MythMayhem.UI
{
    public class CardHandController : MonoBehaviour
    {
        private const string InstructionMessage = "Select a card, then tap one of your lanes";

        [SerializeField] private Button[] cardButtons;
        [SerializeField] private Image[] cardBackgrounds;
        [SerializeField] private Text instructionLabel;
        [SerializeField] private DeckController playerDeck;
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private int maxHandSlots = 10;

        [SerializeField] private float cardMargin = 16f;
        [SerializeField] private float cardGap = 10f;
        [SerializeField] private float cardBaseWidth = 280f;
        [SerializeField] private float cardMinWidth = 140f;
        [SerializeField] private float cardHeight = 240f;
        [SerializeField] private float cardBottomOffset = 16f;

        [SerializeField] private Color normalColor = new Color(0.20f, 0.18f, 0.32f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.96f, 0.78f, 0.32f, 1f);

        private readonly List<Button> handButtons = new List<Button>();
        private readonly List<Image> handBackgrounds = new List<Image>();
        private readonly List<Text> handLabels = new List<Text>();

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

        public event Action<int> SelectionChanged;

        private void Awake()
        {
            BuildSlots();
        }

        private void BuildSlots()
        {
            handButtons.Clear();
            handBackgrounds.Clear();
            handLabels.Clear();

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

                    handLabels.Add(cardButtons[i].GetComponentInChildren<Text>());
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
                if (handButtons[i] == null)
                    continue;

                int index = i;
                handButtons[i].onClick.AddListener(() => Select(index));

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
            RefreshInteractivity();
        }

        public void RefreshInteractivity()
        {
            if (handButtons.Count == 0)
                return;

            bool playerTurn = turnManager == null || turnManager.IsActive(CreatureSide.Player);
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
                bool playable = card != null && card.Type == CardType.Creature && card.CreatureDefinition != null;

                if (i < handLabels.Count && handLabels[i] != null)
                {
                    CreatureDefinition definition = card != null ? card.CreatureDefinition : null;
                    string label = definition != null && !string.IsNullOrEmpty(definition.DisplayName)
                        ? definition.DisplayName
                        : "?";
                    handLabels[i].text = label;
                }

                button.interactable = playerTurn && playable;

                if (i < handBackgrounds.Count && handBackgrounds[i] != null)
                    handBackgrounds[i].color = (i == SelectedIndex) ? selectedColor : normalColor;
            }

            RebuildLayout(handCount);
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
                rect.sizeDelta = new Vector2(width, cardHeight);
                rect.anchoredPosition = new Vector2(startX + i * (width + gap), cardBottomOffset);
            }
        }

        private void OnDestroy()
        {
            foreach (Button button in handButtons)
            {
                if (button != null)
                    button.onClick.RemoveAllListeners();
            }
        }

        private bool IsSlotPlayable(int index)
        {
            if (index < 0 || index >= MaxSlotCount || playerDeck == null || index >= playerDeck.HandCount)
                return false;

            DeckCard card = playerDeck.GetHandCard(index);
            return card != null && card.Type == CardType.Creature && card.CreatureDefinition != null;
        }

        private void Select(int index)
        {
            if (!IsSlotPlayable(index))
                return;

            SelectedIndex = index;
            RefreshInteractivity();

            if (instructionLabel != null)
            {
                CreatureDefinition definition = SelectedDefinition;
                string creatureName = definition != null ? definition.DisplayName : "Unknown";
                instructionLabel.text = creatureName + " selected - tap one of your lanes";
            }

            SelectionChanged?.Invoke(index);
        }

        public void ConsumeSelected()
        {
            if (!HasSelection)
                return;

            int index = SelectedIndex;
            if (playerDeck != null)
                playerDeck.RemoveFromHand(index);

            SelectedIndex = -1;
            RefreshInteractivity();

            if (instructionLabel != null)
                instructionLabel.text = InstructionMessage;
        }
    }
}