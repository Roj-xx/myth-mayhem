using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythMayhem.UI
{
    public sealed class SceneTransitionManager : MonoBehaviour
    {
        private const float FadeOutDuration = 0.45f;
        private const float FadeInDuration = 1f;
        private const string LoadingSceneName = "LoadingScreen";
        private static SceneTransitionManager instance;

        private Image overlay;
        private Coroutine fadeOutRoutine;
        private Coroutine fadeInRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject transitionObject = new GameObject("SceneTransitionManager");
            DontDestroyOnLoad(transitionObject);
            instance = transitionObject.AddComponent<SceneTransitionManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        public static void LoadScene(string sceneName)
        {
            if (instance == null)
            {
                Debug.LogError("[SceneTransition] Manager is not initialized.");
                return;
            }

            instance.StartFadeOut(() => SceneManager.LoadScene(sceneName));
        }

        public static void LoadScene(int buildIndex)
        {
            if (instance == null)
            {
                Debug.LogError("[SceneTransition] Manager is not initialized.");
                return;
            }

            instance.StartFadeOut(() => SceneManager.LoadScene(buildIndex));
        }

        public static void LoadSceneWithFade(string sceneName)
        {
            if (instance == null)
            {
                Debug.LogError("[SceneTransition] Manager is not initialized.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[SceneTransition] Scene is missing from Build Settings: " + sceneName);
                return;
            }

            instance.StartFadeOut(() => SceneManager.LoadScene(sceneName));
        }

        public static void LoadSceneWithLoadingScreen(string sceneName)
        {
            if (instance == null)
            {
                Debug.LogError("[SceneTransition] Manager is not initialized.");
                return;
            }

            if (string.IsNullOrWhiteSpace(sceneName) || sceneName == LoadingSceneName)
            {
                Debug.LogError("[SceneTransition] A valid destination scene is required.");
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(LoadingSceneName)
                || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[SceneTransition] Loading scene or destination is missing from Build Settings: " + sceneName);
                return;
            }

            LoadingScreenController.SetTargetScene(sceneName);
            instance.StartFadeOut(() => SceneManager.LoadScene(LoadingSceneName));
        }

        public static void FadeOutThen(Action onFaded)
        {
            if (instance == null)
            {
                Debug.LogError("[SceneTransition] Manager is not initialized.");
                return;
            }

            if (onFaded == null)
            {
                Debug.LogError("[SceneTransition] Fade-out completion action is required.");
                return;
            }

            instance.StartFadeOut(onFaded);
        }

        public static void LoadSceneImmediately(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[SceneTransition] Scene is missing from Build Settings: " + sceneName);
                return;
            }

            if (instance != null)
            {
                instance.StopFadesAndClearOverlay();
                SceneManager.LoadScene(sceneName);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }

        public static void FadeOutForNetworkSceneLoad()
        {
            if (instance == null)
            {
                Debug.LogError("[SceneTransition] Manager is not initialized.");
                return;
            }

            instance.StartFadeOut(null);
        }

        private void CreateOverlay()
        {
            GameObject canvasObject = new GameObject("SceneFadeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            GameObject imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlay = imageObject.GetComponent<Image>();
            overlay.color = Color.black;
            overlay.raycastTarget = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "SplashScreen")
            {
                StopFadesAndClearOverlay();
                return;
            }

            StartFadeIn();
        }

        private void StopFadesAndClearOverlay()
        {
            if (fadeOutRoutine != null)
            {
                StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = null;
            }

            if (fadeInRoutine != null)
            {
                StopCoroutine(fadeInRoutine);
                fadeInRoutine = null;
            }

            if (overlay != null)
            {
                SetAlpha(0f);
                overlay.raycastTarget = false;
            }
        }

        private void StartFadeOut(Action onFaded)
        {
            if (fadeOutRoutine != null)
                StopCoroutine(fadeOutRoutine);
            if (fadeInRoutine != null)
            {
                StopCoroutine(fadeInRoutine);
                fadeInRoutine = null;
            }

            overlay.raycastTarget = true;
            fadeOutRoutine = StartCoroutine(FadeOutRoutine(onFaded));
        }

        private IEnumerator FadeOutRoutine(Action onFaded)
        {
            yield return Fade(overlay.color.a, 1f, FadeOutDuration);
            fadeOutRoutine = null;
            onFaded?.Invoke();
        }

        private void StartFadeIn()
        {
            if (fadeOutRoutine != null)
            {
                StopCoroutine(fadeOutRoutine);
                fadeOutRoutine = null;
            }

            if (fadeInRoutine != null)
                StopCoroutine(fadeInRoutine);

            SetAlpha(1f);
            overlay.raycastTarget = true;
            fadeInRoutine = StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            yield return null;
            yield return Fade(overlay.color.a, 0f, FadeInDuration);
            overlay.raycastTarget = false;
            fadeInRoutine = null;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }

            SetAlpha(to);
        }

        private void SetAlpha(float alpha)
        {
            Color color = overlay.color;
            color.a = alpha;
            overlay.color = color;
        }
    }
}
