using UnityEngine;
using UnityEngine.UI;

namespace MythMayhem.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private string battleSceneName = "PlayWithAI";
        [SerializeField] private string pvpLobbySceneName = "PvPLobby";
        [SerializeField] private string tutorialSceneName = "Tutorial";
        [SerializeField] private GameObject messageOverlay;
        [SerializeField] private Text messageTitle;
        [SerializeField] private Text messageBody;

        public void PlayVsAi()
        {
            if (string.IsNullOrEmpty(battleSceneName))
                return;

            SceneTransitionManager.LoadSceneWithLoadingScreen(battleSceneName);
        }

        public void ShowTutorial()
        {
            if (string.IsNullOrWhiteSpace(tutorialSceneName))
            {
                Debug.LogError("[MainMenu] Tutorial scene name is not set.");
                return;
            }

            SceneTransitionManager.LoadSceneWithFade(tutorialSceneName);
        }

        public void ShowPvp()
        {
            if (string.IsNullOrWhiteSpace(pvpLobbySceneName))
            {
                Debug.LogError("[MainMenu] PvP lobby scene name is not set.");
                return;
            }

            SceneTransitionManager.LoadScene(pvpLobbySceneName);
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
