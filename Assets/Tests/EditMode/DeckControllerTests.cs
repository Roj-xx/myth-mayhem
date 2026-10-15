using NUnit.Framework;
using UnityEngine;
using MythMayhem.Gameplay;

namespace MythMayhem.Tests
{
    public class DeckControllerTests
    {
        private GameObject host;
        private DeckController deck;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("DeckControllerTestHost");
            deck = host.AddComponent<DeckController>();
            deck.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
        }

        [Test]
        public void RemoveFromHand_MovesCardToDiscardPile()
        {
            deck.AddToHand(new DeckCard());

            Assert.IsTrue(deck.RemoveFromHand(0));
            Assert.AreEqual(0, deck.HandCount);
            Assert.AreEqual(1, deck.DiscardPileSize);
        }

        [Test]
        public void DrawRecyclesDiscardPile_WhenDrawPileIsEmpty()
        {
            DeckCard card = new DeckCard();
            deck.AddToHand(card);
            deck.RemoveFromHand(0);

            DeckCard drawn = deck.DrawToHand();

            Assert.AreSame(card, drawn);
            Assert.AreEqual(1, deck.HandCount);
            Assert.AreEqual(0, deck.DiscardPileSize);
        }

        [Test]
        public void DrawFromEmptyPiles_ReturnsNull()
        {
            Assert.IsNull(deck.DrawToHand());
        }

        [Test]
        public void RemoveFromHand_InvalidIndex_ReturnsFalse()
        {
            Assert.IsFalse(deck.RemoveFromHand(3));
        }
    }
}
