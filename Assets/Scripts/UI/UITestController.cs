using UnityEngine;
using UnityEngine.UI;

namespace MythMayhem.UI
{
    public class UITestController : MonoBehaviour
    {
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button testButton;

        private void Awake()
        {
            if (testButton != null)
                testButton.onClick.AddListener(OnTestButtonClicked);
        }

        private void OnDestroy()
        {
            if (testButton != null)
                testButton.onClick.RemoveListener(OnTestButtonClicked);
        }

        private void OnTestButtonClicked()
        {
            if (statusLabel != null)
                statusLabel.text = "UI INPUT WORKING!";
        }
    }
}
