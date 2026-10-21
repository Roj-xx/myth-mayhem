using UnityEngine;
using UnityEngine.EventSystems;

namespace MythMayhem.UI
{
    // Forwards interaction on a single hand card to the owning CardHandController.
    // A click toggles the enlarged preview; a drag carries the card and tries to
    // play it on the lane it is dropped over.
    public class CardDragHandler : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private CardHandController hand;
        private int index;

        public void Configure(CardHandController owner, int slotIndex)
        {
            hand = owner;
            index = slotIndex;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (hand != null)
                hand.HandleCardClicked(index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (hand != null)
                hand.HandleCardBeginDrag(index, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (hand != null)
                hand.HandleCardDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (hand != null)
                hand.HandleCardEndDrag(eventData);
        }
    }
}
