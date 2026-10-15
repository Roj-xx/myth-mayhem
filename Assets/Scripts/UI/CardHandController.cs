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

                if (i < handLabels.Count && handLabels[i] != null)
                    handLabels[i].text = BuildCardLabel(card);

                button.interactable = playerTurn && playable;

                if (i < handBackgrounds.Count && handBackgrounds[i] != null)
                    handBackgrounds[i].color = (i == SelectedIndex) ? selectedColor : normalColor;
            }

            RebuildLayout(handCount);
        }

        // Cheap change-detection so the hand UI is only rebuilt when something that
        // affects it actually changes (hand contents, selection, active side, width).
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
            return card != null && card.HasValidDefinition;
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

        private void Select(int index)
        {
            if (!IsSlotPlayable(index))
                return;

            SelectedIndex = index;
            RefreshInteractivity();

            if (instructionLabel != null)
            {
                DeckCard card = SelectedCard;
                string cardName = card != null ? card.DefinitionName : "Unknown";
                string tip = card != null && card.Type != CardType.Creature
                    ? "tap a valid target"
                    : "tap one of your lanes";
                instructionLabel.text = cardName + " selected - " + tip;
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