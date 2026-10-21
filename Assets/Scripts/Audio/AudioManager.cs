using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MythMayhem.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private const string LibraryResourceName = "SFXAudioLibrary";
        private static AudioManager instance;

        private readonly HashSet<int> hookedButtons = new HashSet<int>();
        private readonly HashSet<string> loggedMissingClips = new HashSet<string>();

        private GameAudioLibrary library;
        private AudioSource musicSource;
        private AudioSource oneShotSource;
        private string activeSceneName;
        private bool pvpLobbyOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject audioObject = new GameObject("AudioManager");
            DontDestroyOnLoad(audioObject);
            instance = audioObject.AddComponent<AudioManager>();
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

            library = Resources.Load<GameAudioLibrary>(LibraryResourceName);
            if (library == null)
                Debug.LogError("[Audio] Missing Resources/SFXAudioLibrary asset.");

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = 0.55f;

            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            oneShotSource.spatialBlend = 0f;
            oneShotSource.volume = 1f;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        public static void SetPvpLobbyOpen(bool isOpen)
        {
            if (instance == null)
            {
                Debug.LogError("[Audio] AudioManager is not initialized.");
                return;
            }

            instance.pvpLobbyOpen = isOpen;
            instance.UpdateMusic();
        }

        public static void HookButton(Button button)
        {
            if (instance == null)
            {
                Debug.LogError("[Audio] AudioManager is not initialized.");
                return;
            }

            if (button != null)
            {
                button.onClick.RemoveListener(instance.PlayClickSound);
                instance.hookedButtons.Remove(button.GetInstanceID());
            }
            instance.BindButton(button);
        }

        public static void HookAllButtons()
        {
            if (instance == null)
            {
                Debug.LogError("[Audio] AudioManager is not initialized.");
                return;
            }

            instance.BindAllButtons();
        }

        public static void PlaySpawnSound()
        {
            if (instance == null)
            {
                Debug.LogError("[Audio] AudioManager is not initialized.");
                return;
            }

            instance.PlayOneShot(instance.library != null ? instance.library.SpawnSound : null, "SpawnSound");
        }

        public static void PlayDeathSound()
        {
            if (instance == null)
            {
                Debug.LogError("[Audio] AudioManager is not initialized.");
                return;
            }

            instance.PlayOneShot(instance.library != null ? instance.library.DeathSound : null, "DeathSound");
        }

        public static void PlaySplashSound()
        {
            if (instance == null)
            {
                Debug.LogError("[Audio] AudioManager is not initialized.");
                return;
            }

            instance.PlayOneShot(instance.library != null ? instance.library.SplashSound : null, "SplashSound");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (activeSceneName != scene.name)
            {
                activeSceneName = scene.name;
                pvpLobbyOpen = false;
                hookedButtons.Clear();

                if (scene.name == "SplashScreen")
                    PlayOneShot(library != null ? library.SplashSound : null, "SplashSound");
            }

            UpdateMusic();
            BindAllButtons();
        }

        private void UpdateMusic()
        {
            AudioClip clip = null;
            string expectedClipName = null;

            if (activeSceneName == "MainMenu")
            {
                expectedClipName = pvpLobbyOpen ? "PlaySound" : "MainSound";
                if (library != null)
                    clip = pvpLobbyOpen ? library.PlaySound : library.MainSound;
            }
            else if (activeSceneName == "PlayWithAI"
                || (!string.IsNullOrEmpty(activeSceneName) && activeSceneName.IndexOf("pvp", System.StringComparison.OrdinalIgnoreCase) >= 0))
            {
                expectedClipName = "PlaySound";
                clip = library != null ? library.PlaySound : null;
            }

            if (clip == null)
            {
                if (expectedClipName != null)
                    LogMissingClip(expectedClipName);
                musicSource.Stop();
                musicSource.clip = null;
                return;
            }

            if (musicSource.clip == clip && musicSource.isPlaying)
                return;

            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.Play();
        }

        private void BindAllButtons()
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Button button in buttons)
                BindButton(button);
        }

        private void BindButton(Button button)
        {
            if (button == null || !hookedButtons.Add(button.GetInstanceID()))
                return;

            button.onClick.AddListener(PlayClickSound);
        }

        private void PlayClickSound()
        {
            PlayOneShot(library != null ? library.ClickSound : null, "ClickSound");
        }

        private void PlayOneShot(AudioClip clip, string clipName)
        {
            if (clip == null)
            {
                LogMissingClip(clipName);
                return;
            }

            oneShotSource.PlayOneShot(clip);
        }

        private void LogMissingClip(string clipName)
        {
            if (loggedMissingClips.Add(clipName))
                Debug.LogError("[Audio] No " + clipName + " clip is assigned in Resources/SFXAudioLibrary.");
        }
    }
}
