using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using MythMayhem.Gameplay;

namespace MythMayhem.UI
{
    public sealed class LoadingScreenController : MonoBehaviour
    {
        private const string LoadingSceneName = "LoadingScreen";
        private const string MainMenuSceneName = "MainMenu";
        private static string targetSceneName = "MainMenu";

        // A message survives the scene switch so a session that failed while
        // loading can explain itself on the next loading screen.
        private static string pendingStatusMessage;
        private static LoadingScreenController instance;

        [SerializeField] private Image loadingFill;
        [SerializeField] private Text statusLabel;
        [SerializeField] private float minimumDisplaySeconds = 1.25f;
        [SerializeField] private float networkProgressSeconds = 1.5f;
        [SerializeField] private float networkWarningSeconds = 20f;
        [SerializeField] private float networkTimeoutSeconds = 60f;

        private void Awake()
        {
            instance = this;

            string initialMessage = pendingStatusMessage;
            pendingStatusMessage = string.Empty;

            if (loadingFill == null)
                loadingFill = GameObject.Find("LoadingFill")?.GetComponent<Image>();

            if (loadingFill != null)
            {
                loadingFill.type = Image.Type.Filled;
                loadingFill.fillMethod = Image.FillMethod.Horizontal;
                loadingFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                loadingFill.fillAmount = 0f;
            }
            else
            {
                Debug.LogError("[Loading] Could not find the LoadingFill image.");
            }

            if (!string.IsNullOrEmpty(initialMessage))
                SetStatusMessage(initialMessage);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
                // The message was already shown on this screen; drop it so the
                // next match does not inherit a stale failure notice.
                pendingStatusMessage = string.Empty;
            }
        }

        private void Start()
        {
            if (NetworkSession.IsNetworked)
            {
                if (statusLabel != null && string.IsNullOrEmpty(statusLabel.text))
                {
                    statusLabel.text = NetworkSession.IsHost
                        ? "Preparing the match…"
                        : "Waiting for the host…";
                }

                StartCoroutine(AnimateNetworkProgress());
                return;
            }

            StartCoroutine(LoadTargetScene());
        }

        public static void SetTargetScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || sceneName == LoadingSceneName)
            {
                Debug.LogError("[Loading] A valid destination scene is required.");
                return;
            }

            targetSceneName = sceneName;
        }

        // Lets the networking layer explain failures while this screen is up.
        public static void SetStatusMessage(string message)
        {
            pendingStatusMessage = message ?? string.Empty;

            if (instance != null && instance.statusLabel != null)
                instance.statusLabel.text = pendingStatusMessage;
        }

        private IEnumerator LoadTargetScene()
        {
            if (!Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                ShowError("Unable to load " + targetSceneName + ".");
                Debug.LogError("[Loading] Destination scene is missing from Build Settings: " + targetSceneName);
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                ShowError("Unable to start loading.");
                Debug.LogError("[Loading] Unity did not create an async load operation for " + targetSceneName + ".");
                yield break;
            }

            operation.allowSceneActivation = false;
            float elapsed = 0f;
            while (elapsed < minimumDisplaySeconds || operation.progress < 0.9f)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(Mathf.Clamp01(operation.progress / 0.9f));
                yield return null;
            }

            SetProgress(1f);
            yield return null;
            SceneTransitionManager.FadeOutThen(() => operation.allowSceneActivation = true);
        }

        // In a networked match the host decides when the battle scene loads, so
        // this screen only shows progress. It still needs a watchdog: if the host
        // stops responding the client must not sit here forever.
        private IEnumerator AnimateNetworkProgress()
        {
            float elapsed = 0f;
            bool warned = false;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(Mathf.Min(0.9f, elapsed / Mathf.Max(0.1f, networkProgressSeconds) * 0.9f));

                if (!warned && networkWarningSeconds > 0f && elapsed >= networkWarningSeconds)
                {
                    warned = true;

                    if (statusLabel != null)
                        statusLabel.text = "Still waiting for the host…";
                }

                if (networkTimeoutSeconds > 0f && elapsed >= networkTimeoutSeconds)
                {
                    AbortNetworkWait();
                    yield break;
                }

                yield return null;
            }
        }

        private void AbortNetworkWait()
        {
            Debug.LogWarning("[Loading] The host did not finish loading the match in time.");

            LobbyConnectorLocator.Current?.Shutdown();
            SetStatusMessage("The host did not respond. Returning to the main menu…");

            if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
            {
                Debug.LogError("[Loading] The main menu is missing from Build Settings.");
                return;
            }

            SceneTransitionManager.LoadScene(MainMenuSceneName);
        }

        private void SetProgress(float progress)
        {
            if (loadingFill != null)
                loadingFill.fillAmount = Mathf.Clamp01(progress);
        }

        private void ShowError(string message)
        {
            if (statusLabel != null)
                statusLabel.text = message;
        }
    }
}
