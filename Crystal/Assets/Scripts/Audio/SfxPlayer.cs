using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Singleton that plays one-shot sound effects by string key.
    /// Call SfxPlayer.Play("key") from anywhere in the codebase.
    /// Requires an SfxLibrary asset to be assigned in the Inspector.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxPlayer : MonoBehaviour
    {
        private static SfxPlayer instance;

        [SerializeField] private SfxLibrary library;

        private AudioSource audioSource;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            library.Initialize();
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// Plays the one-shot clip registered under the given key.
        /// Logs a warning if the key is not found in the library.
        /// </summary>
        public static void Play(string key)
        {
            if (instance == null)
            {
                Debug.LogError("[SfxPlayer] No SfxPlayer instance found in the scene.");
                return;
            }

            if (!instance.library.TryGet(key, out AudioClip clip))
            {
                Debug.LogWarning($"[SfxPlayer] Key '{key}' not found in the SfxLibrary.");
                return;
            }

            instance.audioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Plays the clip registered under the given key at a custom volume scale.
        /// </summary>
        public static void Play(string key, float volumeScale)
        {
            if (instance == null)
            {
                Debug.LogError("[SfxPlayer] No SfxPlayer instance found in the scene.");
                return;
            }

            if (!instance.library.TryGet(key, out AudioClip clip))
            {
                Debug.LogWarning($"[SfxPlayer] Key '{key}' not found in the SfxLibrary.");
                return;
            }

            instance.audioSource.PlayOneShot(clip, volumeScale);
        }
    }
}
