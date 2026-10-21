using UnityEngine;
using UnityEngine.UI;
using MythMayhem.Gameplay;

namespace MythMayhem.UI
{
    public class ReturnToMenuButton : MonoBehaviour
    {
        [SerializeField] private string menuSceneName = "MainMenu";
        [SerializeField] private Button button;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(ReturnToMenu);
        }

        public void ReturnToMenu()
        {
            if (NetworkSession.IsNetworked)
                LobbyConnectorLocator.Current?.Shutdown();

            SceneTransitionManager.LoadScene(menuSceneName);
        }
    }
}
