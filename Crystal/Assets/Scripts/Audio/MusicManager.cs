using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crystal
{
    /// <summary>
    /// Scene-placeable music controller for boss, victory, and main menu tracks.
    /// Assign the three AudioClips in the Inspector and wire the victory event channel.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicManager : MonoBehaviour
    {
        private enum StartupTrack
        {
            None,
            BossFight,
            MainMenu
        }

        private static MusicManager instance;

        [Header("Tracks")]
        [SerializeField] private AudioClip bossFightTrack;
        [SerializeField] private AudioClip victoryTrack;
        [SerializeField] private AudioClip mainMenuTrack;

        [Header("Events")]
        [SerializeField] private PlayerVictoryEventChannel playerVictoryEventChannel;

        [Header("Scene Routing")]
        [SerializeField] private bool routeMusicBySceneName = true;
        [SerializeField] private string bossFightSceneName = "SampleScene";
        [SerializeField] private string mainMenuSceneName = "Main Manu";

        [Header("Playback")]
        [SerializeField] private StartupTrack startupTrack = StartupTrack.BossFight;
        [SerializeField] private bool persistAcrossScenes = true;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.65f;
        [SerializeField] private float crossfadeDuration = 0.75f;

        private AudioSource audioSource;
        private Coroutine fadeRoutine;
        private bool isActiveInstance;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                instance.AbsorbMissingAssignmentsFrom(this);
                Destroy(gameObject);
                return;
            }

            instance = this;
            isActiveInstance = true;

            if (persistAcrossScenes)
                DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.volume = musicVolume;
        }

        private void OnEnable()
        {
            if (!isActiveInstance)
                return;

            if (playerVictoryEventChannel != null)
                playerVictoryEventChannel.Subscribe(PlayVictoryTrack);

            if (routeMusicBySceneName)
                SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Start()
        {
            if (!isActiveInstance)
                return;

            if (routeMusicBySceneName && TryPlayTrackForScene(SceneManager.GetActiveScene()))
                return;

            switch (startupTrack)
            {
                case StartupTrack.BossFight:
                    PlayBossFightTrack();
                    break;
                case StartupTrack.MainMenu:
                    PlayMainMenuTrack();
                    break;
            }
        }

        private void OnDisable()
        {
            if (!isActiveInstance)
                return;

            if (playerVictoryEventChannel != null)
                playerVictoryEventChannel.Unsubscribe(PlayVictoryTrack);

            if (routeMusicBySceneName)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        private void OnValidate()
        {
            musicVolume = Mathf.Clamp01(musicVolume);
            crossfadeDuration = Mathf.Max(0f, crossfadeDuration);
        }

        public void PlayBossFightTrack()
        {
            PlayTrack(bossFightTrack, "boss fight");
        }

        public void PlayVictoryTrack()
        {
            PlayTrack(victoryTrack, "victory");
        }

        public void PlayMainMenuTrack()
        {
            PlayTrack(mainMenuTrack, "main menu");
        }

        public static void PlayMainMenuTrackFromActiveManager()
        {
            if (instance != null)
                instance.PlayMainMenuTrack();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            if (!isActiveInstance || !routeMusicBySceneName)
                return;

            TryPlayTrackForScene(scene);
        }

        private bool TryPlayTrackForScene(Scene scene)
        {
            if (SceneNameMatches(scene, mainMenuSceneName))
            {
                PlayMainMenuTrack();
                return true;
            }

            if (SceneNameMatches(scene, bossFightSceneName))
            {
                PlayBossFightTrack();
                return true;
            }

            return false;
        }

        private static bool SceneNameMatches(Scene scene, string expectedName)
        {
            return !string.IsNullOrWhiteSpace(expectedName) && scene.name == expectedName;
        }

        private void AbsorbMissingAssignmentsFrom(MusicManager other)
        {
            if (other == null)
                return;

            if (bossFightTrack == null)
                bossFightTrack = other.bossFightTrack;

            if (victoryTrack == null)
                victoryTrack = other.victoryTrack;

            if (mainMenuTrack == null)
                mainMenuTrack = other.mainMenuTrack;

            if (playerVictoryEventChannel == null && other.playerVictoryEventChannel != null)
            {
                playerVictoryEventChannel = other.playerVictoryEventChannel;
                if (isActiveAndEnabled)
                    playerVictoryEventChannel.Subscribe(PlayVictoryTrack);
            }

            if (string.IsNullOrWhiteSpace(bossFightSceneName))
                bossFightSceneName = other.bossFightSceneName;

            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
                mainMenuSceneName = other.mainMenuSceneName;
        }

        private void PlayTrack(AudioClip clip, string label)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[MusicManager] No {label} track assigned.", this);
                return;
            }

            if (audioSource.clip == clip && audioSource.isPlaying)
                return;

            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);

            fadeRoutine = StartCoroutine(CrossfadeTo(clip));
        }

        private IEnumerator CrossfadeTo(AudioClip nextClip)
        {
            if (crossfadeDuration <= 0f || !audioSource.isPlaying)
            {
                audioSource.clip = nextClip;
                audioSource.volume = musicVolume;
                audioSource.Play();
                fadeRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            float startVolume = audioSource.volume;

            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float percent = Mathf.Clamp01(elapsed / crossfadeDuration);
                audioSource.volume = Mathf.Lerp(startVolume, 0f, percent);
                yield return null;
            }

            audioSource.clip = nextClip;
            audioSource.Play();

            elapsed = 0f;
            while (elapsed < crossfadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float percent = Mathf.Clamp01(elapsed / crossfadeDuration);
                audioSource.volume = Mathf.Lerp(0f, musicVolume, percent);
                yield return null;
            }

            audioSource.volume = musicVolume;
            fadeRoutine = null;
        }
    }
}
