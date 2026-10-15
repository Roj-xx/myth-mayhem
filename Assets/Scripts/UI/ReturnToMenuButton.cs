using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            SceneManager.LoadScene(menuSceneName);
        }
    }
}
