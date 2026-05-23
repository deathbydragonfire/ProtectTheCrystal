using UnityEngine;

namespace Crystal.HeroEnemy
{
    public sealed class HeroHealingPickupFloatAnimation : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float hoverAmplitude = 0.8f;
        [SerializeField] private float hoverFrequency = 1.2f;
        [SerializeField] private float scalePulseAmount = 0.04f;
        [SerializeField] private float phaseOffset;

        private Vector3 baseLocalPosition;
        private Vector3 baseLocalScale;
        private bool hasBasePose;

        private void Awake()
        {
            CacheBasePose();
        }

        private void OnEnable()
        {
            if (!hasBasePose)
                CacheBasePose();
            else
                ResetPose();
        }

        private void OnDisable()
        {
            ResetPose();
        }

        private void Update()
        {
            if (visualRoot == null)
                return;

            float phase = (Time.time * hoverFrequency + phaseOffset) * Mathf.PI * 2f;
            float hover = Mathf.Sin(phase) * hoverAmplitude;
            float scale = 1f + Mathf.Sin(phase + Mathf.PI * 0.5f) * scalePulseAmount;

            visualRoot.localPosition = baseLocalPosition + Vector3.up * hover;
            visualRoot.localScale = baseLocalScale * scale;
        }

        private void CacheBasePose()
        {
            if (visualRoot == null)
                visualRoot = transform.childCount > 0 ? transform.GetChild(0) : transform;

            baseLocalPosition = visualRoot.localPosition;
            baseLocalScale = visualRoot.localScale;
            hasBasePose = true;
        }

        private void ResetPose()
        {
            if (visualRoot == null || !hasBasePose)
                return;

            visualRoot.localPosition = baseLocalPosition;
            visualRoot.localScale = baseLocalScale;
        }

        private void OnValidate()
        {
            hoverAmplitude = Mathf.Max(0f, hoverAmplitude);
            hoverFrequency = Mathf.Max(0f, hoverFrequency);
            scalePulseAmount = Mathf.Max(0f, scalePulseAmount);
        }
    }
}
