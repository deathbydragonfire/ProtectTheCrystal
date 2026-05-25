using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Crystal
{
    /// <summary>
    /// Drives the crystal charge bar in the header.
    /// Uses anchorMax.x on the fill RectTransform to represent the charge percent.
    /// The fill color lerps from red (0%) to white (100%).
    /// The crystal shine SpriteRenderer alpha lerps from 0 (0%) to 1 (100%).
    /// Vibrates up and down while the bar is full.
    /// </summary>
    public sealed class CrystalBar : MonoBehaviour
    {
        public event Action Filled;

        /// <summary>The RectTransform of the fill — anchorMax.x is driven to represent the charge percent.</summary>
        [SerializeField] private RectTransform fillRect;
        [SerializeField] private Image fillImage;
        [SerializeField] private SpriteRenderer crystalShine;

        [SerializeField, Range(0f, 1f)] private float fillPercent = 1f;

        /// <summary>
        /// The logical fill percentage [0, 1] — the final target of any in-progress animation.
        /// Use this to evaluate game conditions rather than the animated visual value.
        /// </summary>
        public float FillPercent => _targetFillPercent;

        /// <summary>Duration in seconds for the fill animation when adding charge.</summary>
        [SerializeField] private float fillDuration = 0.2f;

        [Header("Full Vibration")]
        [SerializeField] private Transform crystalTransform;
        [SerializeField] private float vibrateAmplitude = 3f;
        [SerializeField] private float vibrateFrequency = 12f;

        [SerializeField] private CrystalChunkCollectedEventChannel eventChannel;

        private static readonly Color ColorEmpty = Color.red;
        private static readonly Color ColorFull  = Color.white;
        private const float FullFillThreshold = 0.999f;

        private float _targetFillPercent;
        private bool _filledRaised;

        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private Vector3 _baseCrystalLocalPosition;
        private Coroutine _fillCoroutine;
        private Coroutine _vibrateCoroutine;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _baseAnchoredPosition = _rectTransform.anchoredPosition;
            _targetFillPercent = Mathf.Clamp01(fillPercent);
            _filledRaised = _targetFillPercent >= FullFillThreshold;

            if (crystalTransform != null)
                _baseCrystalLocalPosition = crystalTransform.localPosition;
        }

        private void OnEnable()
        {
            if (eventChannel != null)
                eventChannel.Subscribe(AddFill);
        }

        private void OnDisable()
        {
            if (eventChannel != null)
                eventChannel.Unsubscribe(AddFill);
        }

        private void OnValidate()
        {
            if (fillRect != null && fillImage != null)
                SetFill(fillPercent);
        }

        /// <summary>
        /// Sets the fill bar instantly to the given percentage [0, 1].
        /// </summary>
        public void SetFill(float percent)
        {
            percent = Mathf.Clamp01(percent);
            fillPercent = percent;
            if (percent < FullFillThreshold)
                _filledRaised = false;

            fillRect.anchorMax = new Vector2(percent, fillRect.anchorMax.y);
            fillImage.color = Color.Lerp(ColorEmpty, ColorFull, percent);

            if (crystalShine != null)
            {
                Color shineColor = crystalShine.color;
                shineColor.a = percent;
                crystalShine.color = shineColor;
            }
        }

        /// <summary>
        /// Adds the given amount [0, 1] to the current fill, animating smoothly over fillDuration seconds.
        /// Stacks correctly if called while a previous animation is still running.
        /// </summary>
        public void AddFill(float amount)
        {
            _targetFillPercent = Mathf.Clamp01(_targetFillPercent + amount);

            if (_fillCoroutine != null)
                StopCoroutine(_fillCoroutine);

            _fillCoroutine = StartCoroutine(AnimateFill(fillPercent, _targetFillPercent, fillDuration));
        }

        private IEnumerator AnimateFill(float from, float to, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetFill(Mathf.Lerp(from, to, t));
                yield return null;
            }

            SetFill(to);
            _fillCoroutine = null;

            if (to >= FullFillThreshold)
            {
                SfxPlayer.Play("ready");
                StartVibrate();
                RaiseFilledOnce();
            }
        }

        private void RaiseFilledOnce()
        {
            if (_filledRaised)
                return;

            _filledRaised = true;
            Filled?.Invoke();
        }

        private void StartVibrate()
        {
            if (_vibrateCoroutine != null)
                return;

            _vibrateCoroutine = StartCoroutine(VibrateLoop());
        }

        private void StopVibrate()
        {
            if (_vibrateCoroutine == null)
                return;

            StopCoroutine(_vibrateCoroutine);
            _vibrateCoroutine = null;
            _rectTransform.anchoredPosition = _baseAnchoredPosition;

            if (crystalTransform != null)
                crystalTransform.localPosition = _baseCrystalLocalPosition;
        }

        private IEnumerator VibrateLoop()
        {
            float time = 0f;

            while (true)
            {
                time += Time.deltaTime;
                float offset = Mathf.Sin(time * vibrateFrequency) * vibrateAmplitude;
                _rectTransform.anchoredPosition = _baseAnchoredPosition + new Vector2(0f, offset);

                if (crystalTransform != null)
                    crystalTransform.localPosition = _baseCrystalLocalPosition + new Vector3(0f, offset, 0f);

                yield return null;
            }
        }
    }
}
