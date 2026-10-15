using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythMayhem.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string battleSceneName = "SampleScene";
        [SerializeField] private GameObject messageOverlay;
        [SerializeField] private Text messageTitle;
        [SerializeField] private Text messageBody;
        [SerializeField] private LobbyMenuController lobby;

        public void PlayVsAi()
        {
            if (string.IsNullOrEmpty(battleSceneName))
                return;

            SceneManager.LoadScene(battleSceneName);
        }

        public void ShowTutorial()
        {
            ShowMessage(
                "TUTORIAL",
                "A guided tutorial is coming soon.\n\nUntil then, learn the flow in a match against the AI.");
        }

        public void ShowPvp()
        {
            LobbyMenuController target = lobby != null ? lobby : LobbyMenuController.GetOrCreate();
            if (target != null)
            {
                target.Open();
                return;
            }

            ShowMessage(
                "ONLINE PVP",
                "Online battles need a Relay lobby, but matchmaking is unavailable in this build.");
        }

        public void HideMessage()
        {
            if (messageOverlay != null)
                messageOverlay.SetActive(false);
        }

        private void ShowMessage(string title, string body)
        {
            if (messageTitle != null)
                messageTitle.text = title;

            if (messageBody != null)
                messageBody.text = body;

            if (messageOverlay != null)
                messageOverlay.SetActive(true);
        }
    }
}
