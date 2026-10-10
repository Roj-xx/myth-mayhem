using UnityEngine;
using UnityEngine.UI;

namespace MythMayhem.UI
{
    public class UITestController : MonoBehaviour
    {
        private const string InstructionMessage = "Select a card, then tap one of your lanes";

        [SerializeField] private Text statusLabel;

        private void Awake()
        {
            if (statusLabel != null)
                statusLabel.text = InstructionMessage;
        }
    }
}
