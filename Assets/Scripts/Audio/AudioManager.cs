using UnityEngine;

namespace SlotMachine.UI
{
    /// <summary>
    /// Centralized audio playback manager (Singleton).
    /// 
    /// Attach this to a persistent GameObject in the scene.
    /// Wire AudioClip references in the Inspector and call
    /// AudioManager.Instance.PlayXxx() from anywhere.
    /// 
    /// All audio is optional — null clips are safely skipped.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        // ---------------------------------------------------------------
        // Singleton
        // ---------------------------------------------------------------

        public static AudioManager Instance { get; private set; }

        // ---------------------------------------------------------------
        // Inspector — Audio Clips
        // ---------------------------------------------------------------

        [Header("Reel Sounds")]
        [Tooltip("Looping spin whoosh played while reels spin.")]
        public AudioClip spinLoopClip;

        [Tooltip("Mechanical click/stop sound when a reel locks in.")]
        public AudioClip reelStopClip;

        [Header("Win Sounds")]
        [Tooltip("Triumphant jingle for Jackpot / Big Win.")]
        public AudioClip bigWinClip;

        [Tooltip("Smaller chime for regular wins.")]
        public AudioClip smallWinClip;

        [Tooltip("Fun bonus sound for Wild / Free Spin awards.")]
        public AudioClip bonusClip;

        [Tooltip("Lose sound or silence — played on no-match.")]
        public AudioClip loseClip;

        [Header("UI Sounds")]
        [Tooltip("Button click sound.")]
        public AudioClip clickClip;

        [Header("Volume")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;

        // ---------------------------------------------------------------
        // Private
        // ---------------------------------------------------------------

        private AudioSource _audioSource;
        private AudioSource _loopSource;  // Dedicated source for spin loop

        // ---------------------------------------------------------------
        // Unity Lifecycle
        // ---------------------------------------------------------------

        private void Awake()
        {
            // Enforce singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = GetComponent<AudioSource>();
            _audioSource.volume = masterVolume;

            // Create a separate AudioSource for the looping spin sound
            _loopSource = gameObject.AddComponent<AudioSource>();
            _loopSource.loop = true;
            _loopSource.volume = masterVolume * 0.6f;  // Slightly quieter than SFX
        }

        // ---------------------------------------------------------------
        // Public Playback API
        // ---------------------------------------------------------------

        /// <summary>Starts the spinning reel loop sound.</summary>
        public void PlaySpinLoop()
        {
            if (spinLoopClip == null || _loopSource.isPlaying) return;
            _loopSource.clip = spinLoopClip;
            _loopSource.Play();
        }

        /// <summary>Stops the spinning reel loop sound.</summary>
        public void StopSpinLoop()
        {
            _loopSource.Stop();
        }

        /// <summary>Plays the single reel-stop click.</summary>
        public void PlayReelStop()
        {
            PlayOneShot(reelStopClip);
        }

        /// <summary>
        /// Plays the appropriate win sound.
        /// </summary>
        /// <param name="isJackpot">True for Jackpot — plays the bigger jingle.</param>
        public void PlayWinSound(bool isJackpot)
        {
            PlayOneShot(isJackpot ? bigWinClip : smallWinClip);
        }

        /// <summary>Plays the bonus/Wild award sound.</summary>
        public void PlayBonusSound()
        {
            PlayOneShot(bonusClip);
        }

        /// <summary>Plays the no-win sound.</summary>
        public void PlayLoseSound()
        {
            PlayOneShot(loseClip);
        }

        /// <summary>Plays the button click sound.</summary>
        public void PlayClickSound()
        {
            PlayOneShot(clickClip);
        }

        // ---------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------

        private void PlayOneShot(AudioClip clip)
        {
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, masterVolume);
        }
    }
}
