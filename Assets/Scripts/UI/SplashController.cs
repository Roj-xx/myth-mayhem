using System.Collections;
using UnityEngine;
using UnityEngine.Video;

namespace MythMayhem.UI
{
    [RequireComponent(typeof(VideoPlayer))]
    public class SplashController : MonoBehaviour
    {
        [SerializeField] private string loadingSceneName = "LoadingScreen";
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private float prepareTimeoutSeconds = 15f;

        private VideoPlayer videoPlayer;
        private bool transitioning;
        private bool videoFailed;

        private void Awake()
        {
            videoPlayer = GetComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = false;
            videoPlayer.renderMode = VideoRenderMode.CameraFarPlane;
            videoPlayer.targetCamera = Camera.main;
            videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
            videoPlayer.loopPointReached += HandleVideoFinished;
            videoPlayer.errorReceived += HandleVideoError;

            if (videoPlayer.targetCamera == null)
                Debug.LogError("[Splash] No Main Camera is available to display the splash video.");
        }

        private void OnDestroy()
        {
            if (videoPlayer == null)
                return;

            videoPlayer.loopPointReached -= HandleVideoFinished;
            videoPlayer.errorReceived -= HandleVideoError;
        }

        private void Start()
        {
            StartCoroutine(PlaySplash());
        }

        private IEnumerator PlaySplash()
        {
            if (videoPlayer.clip == null && string.IsNullOrWhiteSpace(videoPlayer.url))
            {
                Debug.LogError("[Splash] SplashVid has no video clip or URL.");
                TransitionToLoadingScreen();
                yield break;
            }

            videoPlayer.Prepare();
            float elapsed = 0f;
            while (!videoPlayer.isPrepared && !videoFailed && elapsed < prepareTimeoutSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (videoFailed || !videoPlayer.isPrepared)
            {
                if (!videoFailed)
                    Debug.LogError("[Splash] Timed out preparing the splash video.");

                TransitionToLoadingScreen();
                yield break;
            }

            videoPlayer.Play();
        }

        private void HandleVideoFinished(VideoPlayer source)
        {
            TransitionToLoadingScreen();
        }

        private void HandleVideoError(VideoPlayer source, string message)
        {
            videoFailed = true;
            Debug.LogError("[Splash] Video playback failed: " + message);
            TransitionToLoadingScreen();
        }

        private void TransitionToLoadingScreen()
        {
            if (transitioning)
                return;

            transitioning = true;
            LoadingScreenController.SetTargetScene(nextSceneName);
            SceneTransitionManager.LoadSceneImmediately(loadingSceneName);
        }
    }
}
