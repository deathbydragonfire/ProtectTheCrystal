using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Crystal
{
    public sealed class HeroDefeatedPauseMenuController : MonoBehaviour
    {
        private enum SequenceType { HeroDefeated, PlayerDied }

        [Header("Events")]
        [SerializeField] private HeroDefeatedEventChannel heroDefeatedEventChannel;
        [SerializeField] private PlayerDiedEventChannel playerDiedEventChannel;

        [Header("UI References")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private CanvasGroup menuCanvasGroup;
        [SerializeField] private RectTransform resumeButtonRectTransform;
        [SerializeField] private Graphic resumeButtonGraphic;
        [SerializeField] private RectTransform settingsButtonRectTransform;
        [SerializeField] private Graphic settingsButtonGraphic;
        [SerializeField] private RectTransform restartButtonRectTransform;
        [SerializeField] private Graphic restartButtonGraphic;
        [SerializeField] private RectTransform quitButtonRectTransform;
        [SerializeField] private Graphic quitButtonGraphic;
        [SerializeField] private Graphic[] menuButtonGraphics;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text resumeButtonLabel;
        [SerializeField] private Text settingsButtonLabel;
        [SerializeField] private Text restartButtonLabel;
        [SerializeField] private Text quitButtonLabel;
        [SerializeField] private RectTransform cursorRectTransform;
        [SerializeField] private Image cursorImage;
        [SerializeField] private Sprite cursorSprite;
        [SerializeField] private Image fadeImage;

        [Header("Text")]
        [SerializeField] private Font menuFont;
        [SerializeField] private string titleText = "PAUSED";
        [SerializeField] private string heroDefeatedTitleText = "RETRY?";
        [SerializeField] private string playerDiedTitleText = "YOU DIED";
        [SerializeField] private string resumeButtonText = "RESUME";
        [SerializeField] private string settingsButtonText = "SETTINGS";
        [SerializeField] private string restartButtonText = "RESTART";
        [SerializeField] private string quitButtonText = "QUIT";

        [Header("Timing")]
        [SerializeField] private float revealDelay = 0.25f;
        [SerializeField] private float cursorMoveDuration = 0.6f;
        [SerializeField] private float hoverPauseDuration = 0.25f;
        [SerializeField] private float clickPressDuration = 0.12f;
        [SerializeField] private float postClickDelay = 0.2f;
        [SerializeField] private float fadeDuration = 0.8f;
        [SerializeField] private AnimationCurve cursorMotionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Cursor")]
        [SerializeField] private bool randomizeCursorStart = true;
        [SerializeField] private Vector2 cursorStartAnchoredPosition = new Vector2(-360f, -180f);
        [SerializeField] private Vector2 randomCursorStartMin = new Vector2(-650f, -320f);
        [SerializeField] private Vector2 randomCursorStartMax = new Vector2(550f, 290f);
        [SerializeField] private Vector2 cursorMoveDurationRange = new Vector2(0.35f, 0.65f);
        [SerializeField] private float cursorPathBend = 230f;
        [SerializeField] private float cursorOvershootDistance = 18f;
        [SerializeField] private float cursorSettleDuration = 0.12f;
        [SerializeField] private float cursorHandJitter = 4.2f;
        [SerializeField] private float cursorHandJitterFrequency = 8.5f;
        [SerializeField] private Vector2 cursorTargetOffset = new Vector2(8f, -8f);
        [SerializeField] private Vector3 cursorNormalScale = Vector3.one;
        [SerializeField] private Vector3 cursorPressedScale = new Vector3(0.88f, 0.88f, 1f);

        [Header("Cursor Rage")]
        [SerializeField] private bool cursorRageEnabled = true;
        [SerializeField, Range(0f, 1f)] private float cursorRageChance = 0.35f;
        [SerializeField] private int cursorRageMinMisses = 1;
        [SerializeField] private int cursorRageMaxMisses = 2;
        [SerializeField] private Vector2 cursorRageMoveDurationRange = new Vector2(0.12f, 0.26f);
        [SerializeField] private Vector2 cursorRageMissDistanceRange = new Vector2(45f, 95f);
        [SerializeField] private float cursorRagePauseDuration = 0.08f;
        [SerializeField] private float cursorRageJitterMultiplier = 2.25f;
        [SerializeField] private float cursorRageOvershootMultiplier = 1.6f;
        [SerializeField] private float cursorRageButtonShakeDistance = 7f;
        [SerializeField] private float cursorRageButtonShakeDuration = 0.14f;

        [Header("Cursor Fakeouts")]
        [SerializeField] private bool quitFakeoutEnabled = true;
        [SerializeField, Range(0f, 1f)] private float quitFakeoutChance = 0.25f;
        [SerializeField] private Vector2 quitFakeoutMoveDurationRange = new Vector2(0.2f, 0.42f);
        [SerializeField] private Vector2 quitThinkDurationRange = new Vector2(0.35f, 0.8f);
        [SerializeField] private float quitThinkJitter = 4f;

        [Header("Button Feedback")]
        [SerializeField] private Color buttonNormalColor = new Color(0.92f, 0.92f, 0.92f, 1f);
        [SerializeField] private Color buttonHoverColor = Color.white;
        [SerializeField] private Color buttonPressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
        [SerializeField] private Vector3 buttonNormalScale = Vector3.one;
        [SerializeField] private Vector3 buttonHoverScale = new Vector3(1.06f, 1.06f, 1f);
        [SerializeField] private Vector3 buttonPressedScale = new Vector3(0.94f, 0.94f, 1f);

        [Header("Fade And Reload")]
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private bool freezeTimeDuringSequence = true;
        [SerializeField] private string sceneNameOverride;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip menuOpenSound;
        [SerializeField] private AudioSource cursorMoveAudioSource;
        [SerializeField] private AudioClip cursorMoveSound;
        [SerializeField] private Vector2 cursorMoveVolumeRange = new Vector2(0.45f, 1f);
        [SerializeField] private Vector2 cursorMovePitchRange = new Vector2(0.92f, 1.08f);
        [SerializeField] private Vector2 cursorMoveSpeedVolumeRange = new Vector2(280f, 1400f);
        [SerializeField] private float cursorMoveFadeInSpeed = 14f;
        [SerializeField] private float cursorMoveFadeOutSpeed = 80f;
        [SerializeField, Range(-1f, 1f)] private float cursorMoveDirectionChangeDotThreshold = 0.25f;
        [SerializeField] private float cursorMoveDirectionChangeSilenceDuration = 0.04f;
        [SerializeField] private float cursorMoveStopVolumeThreshold = 0.015f;
        [SerializeField] private bool loopCursorMoveSound = true;
        [SerializeField] private AudioClip buttonHoverSound;
        [SerializeField] private float buttonHoverSoundCooldown = 0.08f;
        [SerializeField] private AudioClip cursorClickSound;
        [SerializeField, FormerlySerializedAs("restartClickSound")] private AudioClip menuClickSound;

        private Coroutine sequenceRoutine;
        private SequenceType activeSequenceType;
        private float previousTimeScale = 1f;
        private bool timeScaleFrozen;
        private RectTransform hoveredButtonRectTransform;
        private float lastHoverSoundTime = float.NegativeInfinity;
        private bool cursorMoveSoundSegmentActive;
        private float cursorMoveSoundSegmentVolume = 1f;
        private float cursorMoveSoundMutedUntilTime;
        private Vector2 previousCursorMoveSoundDirection;

        /// <summary>Triggers the fake-restart sequence for the hero-defeated (early kill) outcome.</summary>
        public void PlayRestartSequence()
        {
            TryStartSequence(SequenceType.HeroDefeated);
        }

        /// <summary>Triggers the fake-restart sequence for the player-died outcome.</summary>
        public void PlayPlayerDiedSequence()
        {
            TryStartSequence(SequenceType.PlayerDied);
        }

        private void TryStartSequence(SequenceType sequenceType)
        {
            if (sequenceRoutine != null)
                return;

            // PlayerDied only needs the fade image - skip full UI validation.
            if (sequenceType == SequenceType.PlayerDied)
            {
                if (fadeImage == null)
                {
                    Debug.LogError("[HeroDefeatedPauseMenuController] Missing required reference: fadeImage. Cannot run PlayerDied sequence.", this);
                    return;
                }
            }
            else if (!ValidateRequiredReferences())
            {
                return;
            }

            Debug.Log($"[HeroDefeatedPauseMenuController] Starting sequence: {sequenceType}");
            activeSequenceType = sequenceType;
            sequenceRoutine = StartCoroutine(RunSequenceRoutine());
        }

        private void Awake()
        {
            EnsureAudioSource();
            EnsureCursorMoveAudioSource();
            ApplySerializedVisuals();
            HideMenuImmediate();
        }

        private void OnEnable()
        {
            if (heroDefeatedEventChannel != null)
                heroDefeatedEventChannel.Subscribe(PlayRestartSequence);
            else
                Debug.LogWarning("[HeroDefeatedPauseMenuController] No HeroDefeatedEventChannel assigned. The sequence can still be triggered via PlayRestartSequence().", this);

            if (playerDiedEventChannel != null)
                playerDiedEventChannel.Subscribe(PlayPlayerDiedSequence);
            else
                Debug.LogWarning("[HeroDefeatedPauseMenuController] No PlayerDiedEventChannel assigned. The sequence can still be triggered via PlayPlayerDiedSequence().", this);

        }

        private void OnDisable()
        {
            if (heroDefeatedEventChannel != null)
                heroDefeatedEventChannel.Unsubscribe(PlayRestartSequence);

            if (playerDiedEventChannel != null)
                playerDiedEventChannel.Unsubscribe(PlayPlayerDiedSequence);

            if (sequenceRoutine != null)
            {
                StopCoroutine(sequenceRoutine);
                sequenceRoutine = null;
            }

            StopCursorMoveSound();
            RestoreTimeScale();
        }

        private void OnValidate()
        {
            revealDelay = Mathf.Max(0f, revealDelay);
            cursorMoveDuration = Mathf.Max(0.01f, cursorMoveDuration);
            hoverPauseDuration = Mathf.Max(0f, hoverPauseDuration);
            clickPressDuration = Mathf.Max(0f, clickPressDuration);
            postClickDelay = Mathf.Max(0f, postClickDelay);
            fadeDuration = Mathf.Max(0.01f, fadeDuration);
            cursorMoveVolumeRange.x = Mathf.Clamp01(cursorMoveVolumeRange.x);
            cursorMoveVolumeRange.y = Mathf.Clamp(cursorMoveVolumeRange.y, cursorMoveVolumeRange.x, 1f);
            cursorMovePitchRange.x = Mathf.Max(0.1f, cursorMovePitchRange.x);
            cursorMovePitchRange.y = Mathf.Max(cursorMovePitchRange.x, cursorMovePitchRange.y);
            cursorMoveSpeedVolumeRange.x = Mathf.Max(0f, cursorMoveSpeedVolumeRange.x);
            cursorMoveSpeedVolumeRange.y = Mathf.Max(cursorMoveSpeedVolumeRange.x + 1f, cursorMoveSpeedVolumeRange.y);
            cursorMoveFadeInSpeed = Mathf.Max(0.01f, cursorMoveFadeInSpeed);
            cursorMoveFadeOutSpeed = Mathf.Max(cursorMoveFadeInSpeed, cursorMoveFadeOutSpeed);
            cursorMoveDirectionChangeSilenceDuration = Mathf.Max(0f, cursorMoveDirectionChangeSilenceDuration);
            cursorMoveStopVolumeThreshold = Mathf.Clamp01(cursorMoveStopVolumeThreshold);
            buttonHoverSoundCooldown = Mathf.Max(0f, buttonHoverSoundCooldown);
            cursorMoveDurationRange.x = Mathf.Max(0.01f, cursorMoveDurationRange.x);
            cursorMoveDurationRange.y = Mathf.Max(cursorMoveDurationRange.x, cursorMoveDurationRange.y);
            cursorPathBend = Mathf.Max(0f, cursorPathBend);
            cursorOvershootDistance = Mathf.Max(0f, cursorOvershootDistance);
            cursorSettleDuration = Mathf.Max(0f, cursorSettleDuration);
            cursorHandJitter = Mathf.Max(0f, cursorHandJitter);
            cursorHandJitterFrequency = Mathf.Max(0f, cursorHandJitterFrequency);
            cursorRageChance = Mathf.Clamp01(cursorRageChance);
            cursorRageMinMisses = Mathf.Max(0, cursorRageMinMisses);
            cursorRageMaxMisses = Mathf.Max(cursorRageMinMisses, cursorRageMaxMisses);
            cursorRageMoveDurationRange.x = Mathf.Max(0.01f, cursorRageMoveDurationRange.x);
            cursorRageMoveDurationRange.y = Mathf.Max(cursorRageMoveDurationRange.x, cursorRageMoveDurationRange.y);
            cursorRageMissDistanceRange.x = Mathf.Max(0f, cursorRageMissDistanceRange.x);
            cursorRageMissDistanceRange.y = Mathf.Max(cursorRageMissDistanceRange.x, cursorRageMissDistanceRange.y);
            cursorRagePauseDuration = Mathf.Max(0f, cursorRagePauseDuration);
            cursorRageJitterMultiplier = Mathf.Max(0f, cursorRageJitterMultiplier);
            cursorRageOvershootMultiplier = Mathf.Max(0f, cursorRageOvershootMultiplier);
            cursorRageButtonShakeDistance = Mathf.Max(0f, cursorRageButtonShakeDistance);
            cursorRageButtonShakeDuration = Mathf.Max(0f, cursorRageButtonShakeDuration);
            quitFakeoutChance = Mathf.Clamp01(quitFakeoutChance);
            quitFakeoutMoveDurationRange.x = Mathf.Max(0.01f, quitFakeoutMoveDurationRange.x);
            quitFakeoutMoveDurationRange.y = Mathf.Max(quitFakeoutMoveDurationRange.x, quitFakeoutMoveDurationRange.y);
            quitThinkDurationRange.x = Mathf.Max(0f, quitThinkDurationRange.x);
            quitThinkDurationRange.y = Mathf.Max(quitThinkDurationRange.x, quitThinkDurationRange.y);
            quitThinkJitter = Mathf.Max(0f, quitThinkJitter);

            Vector2 startMin = randomCursorStartMin;
            Vector2 startMax = randomCursorStartMax;
            randomCursorStartMin = Vector2.Min(startMin, startMax);
            randomCursorStartMax = Vector2.Max(startMin, startMax);

            if (cursorMotionCurve == null || cursorMotionCurve.length == 0)
                cursorMotionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

            ApplySerializedVisuals();
        }

        private IEnumerator RunSequenceRoutine()
        {
            if (activeSequenceType == SequenceType.PlayerDied)
                yield return PlayPlayerDiedSequenceRoutine();
            else
                yield return PlayFakeRestartSequenceRoutine();
        }

        private IEnumerator PlayPlayerDiedSequenceRoutine()
        {
            if (freezeTimeDuringSequence)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                timeScaleFrozen = true;
            }

            yield return WaitForUnscaledSeconds(revealDelay);
            yield return FadeToColor(fadeColor);
            yield return WaitForUnscaledSeconds(0.5f);
            ReloadScene();
        }

        private IEnumerator PlayFakeRestartSequenceRoutine()
        {
            SetTitleForSequence();
            ShowMenuImmediate();

            if (freezeTimeDuringSequence)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                timeScaleFrozen = true;
            }

            PlayOneShot(menuOpenSound);
            yield return WaitForUnscaledSeconds(revealDelay);

            yield return MoveCursorToRestartButton();

            SetButtonFeedback(restartButtonRectTransform, restartButtonGraphic, buttonHoverColor, buttonHoverScale);
            yield return WaitForUnscaledSeconds(hoverPauseDuration);

            SetButtonFeedback(restartButtonRectTransform, restartButtonGraphic, buttonPressedColor, buttonPressedScale);
            SetCursorScale(cursorPressedScale);
            PlayOneShot(cursorClickSound);
            yield return WaitForUnscaledSeconds(clickPressDuration);
            PlayOneShot(menuClickSound);

            SetButtonFeedback(restartButtonRectTransform, restartButtonGraphic, buttonHoverColor, buttonHoverScale);
            SetCursorScale(cursorNormalScale);
            yield return WaitForUnscaledSeconds(postClickDelay);

            yield return FadeToColor(fadeColor);
            ReloadScene();
        }

        private void SetTitleForSequence()
        {
            if (titleLabel == null)
                return;

            titleLabel.text = activeSequenceType switch
            {
                SequenceType.HeroDefeated  => heroDefeatedTitleText,
                SequenceType.PlayerDied    => playerDiedTitleText,
                _                          => titleText,
            };
        }

        private IEnumerator MoveCursorToRestartButton()
        {
            Vector2 currentPosition = GetCursorStartPosition();
            SetCursorPosition(currentPosition);
            SetCursorScale(cursorNormalScale);

            Vector2 target = GetRestartButtonCursorTarget();
            if (ShouldQuitFakeout())
            {
                Vector2 quitTarget = GetButtonCursorTarget(quitButtonRectTransform);
                yield return MoveCursorAlongHumanPath(
                    currentPosition,
                    quitTarget,
                    quitFakeoutMoveDurationRange,
                    cursorHandJitter * 1.35f,
                    cursorOvershootDistance);

                yield return ThinkOnQuitButton(quitTarget);
                currentPosition = cursorRectTransform.anchoredPosition;
            }

            if (ShouldCursorRage())
            {
                int missCount = Random.Range(cursorRageMinMisses, cursorRageMaxMisses + 1);
                for (int i = 0; i < missCount; i++)
                {
                    Vector2 missTarget = GetCursorMissTarget(currentPosition, target);
                    yield return MoveCursorAlongHumanPath(
                        currentPosition,
                        missTarget,
                        cursorRageMoveDurationRange,
                        cursorHandJitter * cursorRageJitterMultiplier,
                        cursorOvershootDistance * cursorRageOvershootMultiplier);

                    currentPosition = missTarget;
                    SetCursorScale(cursorPressedScale);
                    PlayOneShot(cursorClickSound);
                    yield return WaitForUnscaledSeconds(clickPressDuration * Random.Range(0.45f, 0.75f));
                    SetCursorScale(cursorNormalScale);
                    yield return ShakeRestartButton();
                    yield return WaitForUnscaledSeconds(Random.Range(cursorRagePauseDuration * 0.65f, cursorRagePauseDuration * 1.35f));
                }
            }

            yield return MoveCursorAlongHumanPath(currentPosition, target, cursorMoveDurationRange, cursorHandJitter, cursorOvershootDistance);
            SetCursorPosition(target);
        }

        private IEnumerator MoveCursorAlongHumanPath(Vector2 start, Vector2 target, Vector2 durationRange, float jitterAmount, float overshootDistance)
        {
            Vector2 overshootTarget = GetCursorOvershootTarget(start, target, overshootDistance);
            CreateCursorPath(start, overshootTarget, out Vector2 controlA, out Vector2 controlB);

            float moveDuration = GetCursorMoveDuration(durationRange);
            float jitterPhase = Random.Range(0f, Mathf.PI * 2f);
            Vector2 jitterDirection = GetPerpendicularDirection(overshootTarget - start);
            bool playMovementSound = (target - start).sqrMagnitude > 1f;

            if (playMovementSound)
                BeginCursorMoveSoundSegment();

            Vector2 previousPosition = start;
            float elapsed = 0f;
            while (elapsed < moveDuration)
            {
                float deltaTime = Time.unscaledDeltaTime;
                float percent = Mathf.Clamp01(elapsed / moveDuration);
                float curvedPercent = cursorMotionCurve.Evaluate(percent);
                Vector2 position = CubicBezier(start, controlA, controlB, overshootTarget, curvedPercent);
                position += GetCursorJitter(jitterDirection, percent, jitterPhase, jitterAmount);
                SetCursorPosition(position);
                UpdateCursorMoveSound(previousPosition, position, deltaTime);
                previousPosition = position;
                elapsed += deltaTime;
                yield return null;
            }

            yield return SettleCursorOnTarget(overshootTarget, target, jitterAmount);
            SetCursorPosition(target);

            if (playMovementSound)
                StopCursorMoveSound();
        }

        private Vector2 GetCursorStartPosition()
        {
            if (!randomizeCursorStart)
                return cursorStartAnchoredPosition;

            float x = Random.Range(randomCursorStartMin.x, randomCursorStartMax.x);
            float y = Random.Range(randomCursorStartMin.y, randomCursorStartMax.y);
            return new Vector2(x, y);
        }

        private bool ShouldCursorRage()
        {
            return cursorRageEnabled && cursorRageMaxMisses > 0 && Random.value < cursorRageChance;
        }

        private bool ShouldQuitFakeout()
        {
            return quitFakeoutEnabled && quitButtonRectTransform != null && Random.value < quitFakeoutChance;
        }

        private float GetCursorMoveDuration(Vector2 durationRange)
        {
            if (durationRange.y <= 0f)
                return cursorMoveDuration;

            return Random.Range(durationRange.x, durationRange.y);
        }

        private Vector2 GetCursorMissTarget(Vector2 start, Vector2 target)
        {
            Vector2 direction = GetDirection(target - start, GetRandomDirection());
            Vector2 perpendicular = GetPerpendicularDirection(direction) * (Random.value < 0.5f ? -1f : 1f);
            float missDistance = Random.Range(cursorRageMissDistanceRange.x, cursorRageMissDistanceRange.y);
            float forwardOffset = Random.Range(-missDistance * 0.2f, missDistance * 0.35f);
            return target + perpendicular * missDistance + direction * forwardOffset;
        }

        private Vector2 GetCursorOvershootTarget(Vector2 start, Vector2 target, float overshootDistance)
        {
            if (overshootDistance <= 0f)
                return target;

            Vector2 direction = GetDirection(target - start, Vector2.right);
            Vector2 perpendicular = GetPerpendicularDirection(direction);
            float forwardOvershoot = Random.Range(overshootDistance * 0.35f, overshootDistance);
            float sidewaysOvershoot = Random.Range(-overshootDistance * 0.35f, overshootDistance * 0.35f);
            return target + direction * forwardOvershoot + perpendicular * sidewaysOvershoot;
        }

        private void CreateCursorPath(Vector2 start, Vector2 end, out Vector2 controlA, out Vector2 controlB)
        {
            Vector2 delta = end - start;
            Vector2 perpendicular = GetPerpendicularDirection(delta);
            float bend = Random.Range(-cursorPathBend, cursorPathBend);

            if (cursorPathBend > 0f && Mathf.Abs(bend) < cursorPathBend * 0.25f)
                bend = (Random.value < 0.5f ? -1f : 1f) * cursorPathBend * 0.25f;

            float firstHandle = Random.Range(0.18f, 0.36f);
            float secondHandle = Random.Range(0.64f, 0.84f);
            controlA = start + delta * firstHandle + perpendicular * bend;
            controlB = start + delta * secondHandle - perpendicular * bend * Random.Range(0.35f, 0.75f);
        }

        private Vector2 GetCursorJitter(Vector2 jitterDirection, float percent, float phase, float jitterAmount)
        {
            if (jitterAmount <= 0f || cursorHandJitterFrequency <= 0f)
                return Vector2.zero;

            float envelope = Mathf.Sin(percent * Mathf.PI) * (1f - percent * 0.35f);
            float wave = Mathf.Sin((percent * cursorHandJitterFrequency * Mathf.PI * 2f) + phase);
            return jitterDirection * wave * jitterAmount * envelope;
        }

        private IEnumerator SettleCursorOnTarget(Vector2 start, Vector2 target, float jitterAmount)
        {
            if (cursorSettleDuration <= 0f)
                yield break;

            float jitterPhase = Random.Range(0f, Mathf.PI * 2f);
            Vector2 jitterDirection = GetPerpendicularDirection(target - start);
            Vector2 previousPosition = start;
            float elapsed = 0f;
            while (elapsed < cursorSettleDuration)
            {
                float deltaTime = Time.unscaledDeltaTime;
                float percent = Mathf.Clamp01(elapsed / cursorSettleDuration);
                float easedPercent = 1f - Mathf.Pow(1f - percent, 2f);
                Vector2 position = Vector2.LerpUnclamped(start, target, easedPercent);
                position += GetCursorJitter(jitterDirection, percent, jitterPhase, jitterAmount * 0.35f);
                SetCursorPosition(position);
                UpdateCursorMoveSound(previousPosition, position, deltaTime);
                previousPosition = position;
                elapsed += deltaTime;
                yield return null;
            }
        }

        private IEnumerator ThinkOnQuitButton(Vector2 target)
        {
            float duration = Random.Range(quitThinkDurationRange.x, quitThinkDurationRange.y);
            float phase = Random.Range(0f, Mathf.PI * 2f);
            float elapsed = 0f;

            if (duration > 0f && quitThinkJitter > 0f)
                BeginCursorMoveSoundSegment(0.55f);

            Vector2 previousPosition = target;
            while (elapsed < duration)
            {
                float deltaTime = Time.unscaledDeltaTime;
                float percent = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float radius = quitThinkJitter * Mathf.Sin(percent * Mathf.PI);
                Vector2 offset = new Vector2(
                    Mathf.Sin((percent * Mathf.PI * 6f) + phase),
                    Mathf.Cos((percent * Mathf.PI * 4f) + phase)) * radius;

                Vector2 position = target + offset;
                SetCursorPosition(position);
                UpdateCursorMoveSound(previousPosition, position, deltaTime);
                previousPosition = position;
                elapsed += deltaTime;
                yield return null;
            }

            SetCursorPosition(target);
            StopCursorMoveSound();
        }

        private IEnumerator ShakeRestartButton()
        {
            if (restartButtonRectTransform == null || cursorRageButtonShakeDistance <= 0f || cursorRageButtonShakeDuration <= 0f)
                yield break;

            Vector2 originalPosition = restartButtonRectTransform.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < cursorRageButtonShakeDuration)
            {
                float percent = Mathf.Clamp01(elapsed / cursorRageButtonShakeDuration);
                float falloff = 1f - percent;
                Vector2 shake = GetRandomDirection() * cursorRageButtonShakeDistance * falloff;
                restartButtonRectTransform.anchoredPosition = originalPosition + shake;
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            restartButtonRectTransform.anchoredPosition = originalPosition;
        }

        private static Vector2 CubicBezier(Vector2 start, Vector2 controlA, Vector2 controlB, Vector2 end, float percent)
        {
            float inverse = 1f - percent;
            return inverse * inverse * inverse * start
                + 3f * inverse * inverse * percent * controlA
                + 3f * inverse * percent * percent * controlB
                + percent * percent * percent * end;
        }

        private static Vector2 GetPerpendicularDirection(Vector2 direction)
        {
            Vector2 normalizedDirection = GetDirection(direction, Vector2.right);
            return new Vector2(-normalizedDirection.y, normalizedDirection.x);
        }

        private static Vector2 GetDirection(Vector2 value, Vector2 fallback)
        {
            return value.sqrMagnitude > 0.0001f ? value.normalized : fallback;
        }

        private static Vector2 GetRandomDirection()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        private Vector2 GetRestartButtonCursorTarget()
        {
            return GetButtonCursorTarget(restartButtonRectTransform);
        }

        private Vector2 GetButtonCursorTarget(RectTransform buttonRectTransform)
        {
            RectTransform cursorParent = cursorRectTransform.parent as RectTransform;
            if (cursorParent == null || buttonRectTransform == null)
                return cursorStartAnchoredPosition;

            Vector3 worldCenter = buttonRectTransform.TransformPoint(buttonRectTransform.rect.center);
            Vector3 localCenter = cursorParent.InverseTransformPoint(worldCenter);
            return new Vector2(localCenter.x, localCenter.y) + cursorTargetOffset;
        }

        private IEnumerator FadeToColor(Color targetColor)
        {
            float elapsed = 0f;
            SetFadeAlpha(0f);

            // Drive the fade image toward the target color at alpha 0 first so it
            // starts invisible regardless of the previous color.
            if (fadeImage != null)
                fadeImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 0f);

            while (elapsed < fadeDuration)
            {
                float percent = Mathf.Clamp01(elapsed / fadeDuration);
                if (fadeImage != null)
                    fadeImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, percent);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (fadeImage != null)
                fadeImage.color = new Color(targetColor.r, targetColor.g, targetColor.b, 1f);
        }

        private void ReloadScene()
        {
            RestoreTimeScale();

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            if (!string.IsNullOrWhiteSpace(sceneNameOverride))
            {
                SceneManager.LoadScene(sceneNameOverride);
                return;
            }

            Debug.LogError("[HeroDefeatedPauseMenuController] Cannot reload the active scene because it is not in Build Settings and no scene name override is assigned.", this);
            sequenceRoutine = null;
        }

        private bool ValidateRequiredReferences()
        {
            bool isValid = true;

            isValid &= ValidateReference(menuRoot, nameof(menuRoot));
            isValid &= ValidateReference(menuCanvasGroup, nameof(menuCanvasGroup));
            isValid &= ValidateReference(resumeButtonRectTransform, nameof(resumeButtonRectTransform));
            isValid &= ValidateReference(resumeButtonGraphic, nameof(resumeButtonGraphic));
            isValid &= ValidateReference(settingsButtonRectTransform, nameof(settingsButtonRectTransform));
            isValid &= ValidateReference(settingsButtonGraphic, nameof(settingsButtonGraphic));
            isValid &= ValidateReference(restartButtonRectTransform, nameof(restartButtonRectTransform));
            isValid &= ValidateReference(restartButtonGraphic, nameof(restartButtonGraphic));
            isValid &= ValidateReference(quitButtonRectTransform, nameof(quitButtonRectTransform));
            isValid &= ValidateReference(quitButtonGraphic, nameof(quitButtonGraphic));
            isValid &= ValidateReference(cursorRectTransform, nameof(cursorRectTransform));
            isValid &= ValidateReference(cursorImage, nameof(cursorImage));
            isValid &= ValidateReference(fadeImage, nameof(fadeImage));

            return isValid;
        }

        private bool ValidateReference(Object reference, string fieldName)
        {
            if (reference != null)
                return true;

            Debug.LogError($"[HeroDefeatedPauseMenuController] Missing required reference: {fieldName}.", this);
            return false;
        }

        private void ApplySerializedVisuals()
        {
            if (titleLabel != null)
            {
                ApplyMenuFont(titleLabel);
                titleLabel.text = titleText;
            }

            if (resumeButtonLabel != null)
            {
                ApplyMenuFont(resumeButtonLabel);
                resumeButtonLabel.text = resumeButtonText;
            }

            if (settingsButtonLabel != null)
            {
                ApplyMenuFont(settingsButtonLabel);
                settingsButtonLabel.text = settingsButtonText;
            }

            if (restartButtonLabel != null)
            {
                ApplyMenuFont(restartButtonLabel);
                restartButtonLabel.text = restartButtonText;
            }

            if (quitButtonLabel != null)
            {
                ApplyMenuFont(quitButtonLabel);
                quitButtonLabel.text = quitButtonText;
            }

            if (cursorImage != null && cursorSprite != null)
                cursorImage.sprite = cursorSprite;

            SetAllButtonsFeedback(buttonNormalColor, buttonNormalScale);
            SetCursorScale(cursorNormalScale);
            SetFadeAlpha(0f);
        }

        private void ApplyMenuFont(Text label)
        {
            if (menuFont != null)
                label.font = menuFont;
        }

        private void HideMenuImmediate()
        {
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 0f;
                menuCanvasGroup.interactable = false;
                menuCanvasGroup.blocksRaycasts = false;
            }

            if (menuRoot != null)
                menuRoot.SetActive(false);

            hoveredButtonRectTransform = null;
            SetAllButtonsFeedback(buttonNormalColor, buttonNormalScale);
            SetFadeAlpha(0f);
        }

        private void ShowMenuImmediate()
        {
            if (menuRoot != null)
                menuRoot.SetActive(true);

            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = 1f;
                menuCanvasGroup.interactable = false;
                menuCanvasGroup.blocksRaycasts = false;
            }

            hoveredButtonRectTransform = null;
            SetAllButtonsFeedback(buttonNormalColor, buttonNormalScale);
            SetCursorScale(cursorNormalScale);
            SetFadeAlpha(0f);
        }

        private void SetCursorPosition(Vector2 anchoredPosition)
        {
            if (cursorRectTransform == null)
                return;

            cursorRectTransform.anchoredPosition = anchoredPosition;
            UpdateHoveredButton(anchoredPosition);
        }

        private void UpdateHoveredButton(Vector2 cursorPosition)
        {
            RectTransform nextHoveredButton = GetButtonUnderCursor(cursorPosition);
            if (nextHoveredButton == hoveredButtonRectTransform)
                return;

            hoveredButtonRectTransform = nextHoveredButton;
            SetAllButtonsFeedback(buttonNormalColor, buttonNormalScale);

            if (hoveredButtonRectTransform != null)
            {
                SetButtonFeedback(
                    hoveredButtonRectTransform,
                    GetButtonGraphic(hoveredButtonRectTransform),
                    buttonHoverColor,
                    buttonHoverScale);
                PlayHoverSound();
            }
        }

        private RectTransform GetButtonUnderCursor(Vector2 cursorPosition)
        {
            if (IsCursorOverButton(cursorPosition, resumeButtonRectTransform))
                return resumeButtonRectTransform;

            if (IsCursorOverButton(cursorPosition, settingsButtonRectTransform))
                return settingsButtonRectTransform;

            if (IsCursorOverButton(cursorPosition, restartButtonRectTransform))
                return restartButtonRectTransform;

            if (IsCursorOverButton(cursorPosition, quitButtonRectTransform))
                return quitButtonRectTransform;

            return null;
        }

        private bool IsCursorOverButton(Vector2 cursorPosition, RectTransform buttonRectTransform)
        {
            RectTransform cursorParent = cursorRectTransform != null ? cursorRectTransform.parent as RectTransform : null;
            if (cursorParent == null || buttonRectTransform == null)
                return false;

            Vector3 worldCursorPosition = cursorParent.TransformPoint(cursorPosition);
            Vector3 buttonLocalPosition = buttonRectTransform.InverseTransformPoint(worldCursorPosition);
            return buttonRectTransform.rect.Contains(buttonLocalPosition);
        }

        private Graphic GetButtonGraphic(RectTransform buttonRectTransform)
        {
            if (buttonRectTransform == resumeButtonRectTransform)
                return resumeButtonGraphic;

            if (buttonRectTransform == settingsButtonRectTransform)
                return settingsButtonGraphic;

            if (buttonRectTransform == restartButtonRectTransform)
                return restartButtonGraphic;

            if (buttonRectTransform == quitButtonRectTransform)
                return quitButtonGraphic;

            return null;
        }

        private void SetAllButtonsFeedback(Color color, Vector3 scale)
        {
            SetAllButtonGraphics(color);
            SetButtonFeedback(resumeButtonRectTransform, resumeButtonGraphic, color, scale);
            SetButtonFeedback(settingsButtonRectTransform, settingsButtonGraphic, color, scale);
            SetButtonFeedback(restartButtonRectTransform, restartButtonGraphic, color, scale);
            SetButtonFeedback(quitButtonRectTransform, quitButtonGraphic, color, scale);
        }

        private void SetAllButtonGraphics(Color color)
        {
            if (menuButtonGraphics == null)
                return;

            foreach (Graphic buttonGraphic in menuButtonGraphics)
            {
                if (buttonGraphic != null)
                    buttonGraphic.color = color;
            }
        }

        private void SetButtonFeedback(RectTransform buttonRectTransform, Graphic buttonGraphic, Color color, Vector3 scale)
        {
            if (buttonGraphic != null)
                buttonGraphic.color = color;

            if (buttonRectTransform != null)
                buttonRectTransform.localScale = scale;
        }

        private void SetCursorScale(Vector3 scale)
        {
            if (cursorRectTransform != null)
                cursorRectTransform.localScale = scale;
        }

        private void SetFadeAlpha(float alpha)
        {
            if (fadeImage == null)
                return;

            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, Mathf.Clamp01(alpha));
        }

        private void PlayOneShot(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }

        private void PlayHoverSound()
        {
            if (Time.unscaledTime - lastHoverSoundTime < buttonHoverSoundCooldown)
                return;

            lastHoverSoundTime = Time.unscaledTime;
            PlayOneShot(buttonHoverSound);
        }

        private void BeginCursorMoveSoundSegment(float volumeMultiplier = 1f)
        {
            if (cursorMoveAudioSource == null || cursorMoveSound == null)
                return;

            cursorMoveSoundSegmentActive = true;
            cursorMoveSoundSegmentVolume = Random.Range(cursorMoveVolumeRange.x, cursorMoveVolumeRange.y) * Mathf.Clamp01(volumeMultiplier);
            cursorMoveSoundMutedUntilTime = 0f;
            previousCursorMoveSoundDirection = Vector2.zero;
            cursorMoveAudioSource.clip = cursorMoveSound;
            cursorMoveAudioSource.loop = loopCursorMoveSound;
            cursorMoveAudioSource.pitch = Random.Range(cursorMovePitchRange.x, cursorMovePitchRange.y);
            cursorMoveAudioSource.volume = 0f;
        }

        private void StopCursorMoveSound()
        {
            cursorMoveSoundSegmentActive = false;
            previousCursorMoveSoundDirection = Vector2.zero;

            if (cursorMoveAudioSource != null)
            {
                cursorMoveAudioSource.volume = 0f;
                cursorMoveAudioSource.Stop();
            }
        }

        private void UpdateCursorMoveSound(Vector2 previousPosition, Vector2 currentPosition, float deltaTime)
        {
            if (!cursorMoveSoundSegmentActive || cursorMoveAudioSource == null || cursorMoveSound == null || deltaTime <= 0f)
                return;

            Vector2 delta = currentPosition - previousPosition;
            float speed = delta.magnitude / deltaTime;
            float targetVolume = GetCursorMoveTargetVolume(speed);

            if (speed >= cursorMoveSpeedVolumeRange.x && delta.sqrMagnitude > 0.0001f)
            {
                Vector2 direction = delta.normalized;
                if (previousCursorMoveSoundDirection.sqrMagnitude > 0f
                    && Vector2.Dot(previousCursorMoveSoundDirection, direction) < cursorMoveDirectionChangeDotThreshold)
                {
                    cursorMoveSoundMutedUntilTime = Time.unscaledTime + cursorMoveDirectionChangeSilenceDuration;
                    targetVolume = 0f;
                }

                previousCursorMoveSoundDirection = direction;
            }

            if (Time.unscaledTime < cursorMoveSoundMutedUntilTime)
                targetVolume = 0f;

            float fadeSpeed = targetVolume > cursorMoveAudioSource.volume ? cursorMoveFadeInSpeed : cursorMoveFadeOutSpeed;
            cursorMoveAudioSource.volume = Mathf.MoveTowards(cursorMoveAudioSource.volume, targetVolume, fadeSpeed * deltaTime);

            if (cursorMoveAudioSource.volume > cursorMoveStopVolumeThreshold)
            {
                if (!cursorMoveAudioSource.isPlaying)
                    cursorMoveAudioSource.Play();
            }
            else
            {
                cursorMoveAudioSource.volume = 0f;
                if (cursorMoveAudioSource.isPlaying)
                    cursorMoveAudioSource.Stop();
            }
        }

        private float GetCursorMoveTargetVolume(float speed)
        {
            float speedPercent = Mathf.InverseLerp(cursorMoveSpeedVolumeRange.x, cursorMoveSpeedVolumeRange.y, speed);
            if (speedPercent <= 0f)
                return 0f;

            return cursorMoveSoundSegmentVolume * speedPercent * speedPercent;
        }

        private void EnsureAudioSource()
        {
            if (audioSource != null || !HasAnyAudioClips())
                return;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void EnsureCursorMoveAudioSource()
        {
            if (cursorMoveAudioSource != null || cursorMoveSound == null)
                return;

            cursorMoveAudioSource = gameObject.AddComponent<AudioSource>();
            cursorMoveAudioSource.playOnAwake = false;
            cursorMoveAudioSource.spatialBlend = 0f;
        }

        private bool HasAnyAudioClips()
        {
            return menuOpenSound != null
                || cursorMoveSound != null
                || buttonHoverSound != null
                || cursorClickSound != null
                || menuClickSound != null;
        }

        private void RestoreTimeScale()
        {
            if (!timeScaleFrozen)
                return;

            Time.timeScale = Mathf.Approximately(previousTimeScale, 0f) ? 1f : previousTimeScale;
            timeScaleFrozen = false;
        }

        private static IEnumerator WaitForUnscaledSeconds(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }
}
