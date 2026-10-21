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
        private static string targetSceneName = "MainMenu";

        [SerializeField] private Image loadingFill;
        [SerializeField] private Text statusLabel;
        [SerializeField] private float minimumDisplaySeconds = 1.25f;
        [SerializeField] private float networkProgressSeconds = 1.5f;

        private void Awake()
        {
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
        }

        private void Start()
        {
            if (NetworkSession.IsNetworked)
            {
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

        private IEnumerator AnimateNetworkProgress()
        {
            float elapsed = 0f;
            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                SetProgress(Mathf.Min(0.9f, elapsed / Mathf.Max(0.1f, networkProgressSeconds) * 0.9f));
                yield return null;
            }
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
