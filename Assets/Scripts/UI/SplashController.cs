using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythMayhem.UI
{
    public class SplashController : MonoBehaviour
    {
        [Header("Transition")]
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private CanvasGroup contentGroup;
        [SerializeField] private Button skipButton;

        [Header("Timing (unscaled seconds)")]
        [SerializeField] private float fadeInSeconds = 0.8f;
        [SerializeField] private float displaySeconds = 3f;
        [SerializeField] private float fadeOutSeconds = 0.55f;
        [SerializeField] private float skipFadeOutSeconds = 0.3f;

        private bool _skipped;
        private bool _transitioning;

        private void Awake()
        {
            if (contentGroup != null)
                contentGroup.alpha = 0f;
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        public void Skip()
        {
            if (_transitioning)
                return;

            _skipped = true;
        }

        private IEnumerator Run()
        {
            yield return Fade(0f, 1f, fadeInSeconds);

            float elapsed = fadeInSeconds;
            while (!_skipped && elapsed < displaySeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            float outSeconds = _skipped ? skipFadeOutSeconds : fadeOutSeconds;
            yield return Fade(contentGroup != null ? contentGroup.alpha : 1f, 0f, outSeconds);

            Transition();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (contentGroup == null)
                yield break;

            if (duration <= 0f)
            {
                contentGroup.alpha = to;
                yield break;
            }

            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                contentGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }

            contentGroup.alpha = to;
        }

        private void Transition()
        {
            if (_transitioning)
                return;

            _transitioning = true;

            if (skipButton != null)
                skipButton.interactable = false;

            if (!string.IsNullOrEmpty(nextSceneName))
                SceneManager.LoadScene(nextSceneName);
        }
    }
}