using System.Collections.Generic;
using UnityEngine;

namespace MythMayhem.Gameplay
{
    public class DeckController : MonoBehaviour
    {
        [SerializeField] private List<DeckCard> cardPool = new List<DeckCard>();
        [SerializeField] private int initialHandSize = 5;
        [SerializeField] private int maxHandSize = 10;

        private readonly List<DeckCard> drawPile = new List<DeckCard>();
        private readonly List<DeckCard> hand = new List<DeckCard>();
        private bool initialized;

        public int HandCount => hand.Count;
        public int DrawPileSize => drawPile.Count;
        public bool IsEmpty => drawPile.Count == 0;
        public int MaxHandSize => maxHandSize;

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            drawPile.Clear();
            hand.Clear();

            if (cardPool != null)
            {
                for (int i = 0; i < cardPool.Count; i++)
                {
                    if (cardPool[i] != null)
                        drawPile.Add(cardPool[i]);
                }
            }

            ShuffleDrawPile();
            initialized = true;
        }

        public void ShuffleDrawPile()
        {
            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                DeckCard temp = drawPile[i];
                drawPile[i] = drawPile[j];
                drawPile[j] = temp;
            }
        }

        public void DealInitialHand()
        {
            if (!initialized)
                Initialize();

            hand.Clear();

            int drawCount = Mathf.Min(initialHandSize, Mathf.Min(maxHandSize, drawPile.Count));
            for (int i = 0; i < drawCount; i++)
                hand.Add(DrawFromTop());
        }

        public DeckCard DrawToHand()
        {
            if (hand.Count >= maxHandSize)
            {
                Debug.LogWarning("Hand is full (" + maxHandSize + "); draw skipped.");
                return null;
            }

            DeckCard card = DrawFromTop();
            if (card == null)
            {
                Debug.Log("Draw pile is empty; draw skipped.");
                return null;
            }

            hand.Add(card);
            return card;
        }

        public void AddToHand(DeckCard card)
        {
            if (card != null && hand.Count < maxHandSize)
                hand.Add(card);
        }

        public DeckCard[] GetHand()
        {
            return hand.ToArray();
        }

        public DeckCard GetHandCard(int index)
        {
            if (index < 0 || index >= hand.Count)
                return null;

            return hand[index];
        }

        public bool RemoveFromHand(int index)
        {
            if (index < 0 || index >= hand.Count)
                return false;

            hand.RemoveAt(index);
            return true;
        }

        private DeckCard DrawFromTop()
        {
            if (drawPile.Count == 0)
                return null;

            DeckCard card = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);
            return card;
        }
    }
}