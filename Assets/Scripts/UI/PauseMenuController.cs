using UnityEngine;
using UnityEngine.UI;
using MythMayhem.Audio;
using MythMayhem.Gameplay;

namespace MythMayhem.UI
{
    public class PauseMenuController : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button concedeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private TurnManager turnManager;
        [SerializeField] private MatchController matchController;

        private bool isPaused;
        private float timeScaleBeforePause = 1f;

        private void Awake()
        {
            if (pauseButton != null)
            {
                AudioManager.HookButton(pauseButton);
                pauseButton.onClick.AddListener(OpenPauseMenu);
            }

            if (resumeButton != null)
            {
                AudioManager.HookButton(resumeButton);
                resumeButton.onClick.AddListener(ResumeGame);
            }

            if (concedeButton != null)
            {
                AudioManager.HookButton(concedeButton);
                concedeButton.onClick.AddListener(Concede);
            }

            if (mainMenuButton != null)
            {
                AudioManager.HookButton(mainMenuButton);
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            }

            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (pauseButton != null)
                pauseButton.onClick.RemoveListener(OpenPauseMenu);

            if (resumeButton != null)
                resumeButton.onClick.RemoveListener(ResumeGame);

            if (concedeButton != null)
                concedeButton.onClick.RemoveListener(Concede);

            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);

            RestoreTimeAndTimer();
        }

        public void OpenPauseMenu()
        {
            if (isPaused || pausePanel == null)
                return;

            if (matchController != null && matchController.IsMatchOver)
                return;

            isPaused = true;
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;

            if (turnManager != null)
                turnManager.SetPaused(true);

            pausePanel.SetActive(true);
        }

        public void ResumeGame()
        {
            if (!isPaused)
                return;

            isPaused = false;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            Time.timeScale = timeScaleBeforePause;

            if (turnManager != null)
                turnManager.SetPaused(false);
        }

        public void Concede()
        {
            if (matchController == null || matchController.IsMatchOver)
            {
                ResumeGame();
                return;
            }

            // Restore the clock before handing over to the existing defeat flow,
            // so the game-over overlay can fade in and its buttons can respond.
            ResumeGame();

            // In PvP only the host may decide the result. Ask it to apply the
            // concession so both peers agree on the outcome.
            if (NetworkSession.IsNetworked)
            {
                if (NetworkSession.Intents != null)
                    NetworkSession.Intents.Concede();
                else
                    Debug.LogWarning("[PvP] Cannot concede while the match is still starting.");

                return;
            }

            matchController.EndMatch(MatchOutcome.OpponentWin);
        }

        public void ReturnToMainMenu()
        {
            RestoreTimeAndTimer();

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (NetworkSession.IsNetworked)
                LobbyConnectorLocator.Current?.Shutdown();

            SceneTransitionManager.LoadScene(mainMenuSceneName);
        }

        private void RestoreTimeAndTimer()
        {
            if (turnManager != null)
                turnManager.SetPaused(false);

            if (!isPaused)
                return;

            Time.timeScale = timeScaleBeforePause;
            isPaused = false;
        }
    }
}
