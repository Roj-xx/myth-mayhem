using UnityEngine;

namespace MythMayhem.Audio
{
    [CreateAssetMenu(menuName = "Myth Mayhem/Audio Library")]
    public sealed class GameAudioLibrary : ScriptableObject
    {
        [SerializeField] private AudioClip splashSound;
        [SerializeField] private AudioClip mainSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip playSound;
        [SerializeField] private AudioClip spawnSound;
        [SerializeField] private AudioClip deathSound;

        public AudioClip SplashSound => splashSound;
        public AudioClip MainSound => mainSound;
        public AudioClip ClickSound => clickSound;
        public AudioClip PlaySound => playSound;
        public AudioClip SpawnSound => spawnSound;
        public AudioClip DeathSound => deathSound;
    }
}
