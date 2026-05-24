using Crystal.HeroEnemy;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Drives Ciela's 5-heart header bar based on her CombatHealth.
    /// Hearts are drained from the outermost (Heart 4) inward.
    /// Each heart represents 20% of max health.
    /// </summary>
    public sealed class CielaHealthDisplay : MonoBehaviour
    {
        [System.Serializable]
        private struct Heart
        {
            public GameObject full;
            public GameObject half;
            public GameObject empty;
        }

        [Header("References")]
        [SerializeField] private CombatHealth combatHealth;

        [Header("Hearts (index 0 = closest to portrait, index 4 = furthest)")]
        [SerializeField] private Heart[] hearts = new Heart[5];

        private const int HeartCount = 5;
        private const float HealthPerHeart = 1f / HeartCount;

        // Each heart covers 20% of health. The half threshold is at the 10% mark within a heart.
        private const float HalfHeartThreshold = 0.5f;

        private void OnEnable()
        {
            combatHealth.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            combatHealth.HealthChanged -= OnHealthChanged;
        }

        private void Start()
        {
            Refresh(combatHealth.HealthPercent);
        }

        /// <summary>
        /// Refreshes all heart visuals to match the given health percentage [0, 1].
        /// </summary>
        public void Refresh(float healthPercent)
        {
            healthPercent = Mathf.Clamp01(healthPercent);

            // Scale health into [0, HeartCount] to get per-heart fill amounts.
            float scaledHealth = healthPercent * HeartCount;

            for (int i = 0; i < HeartCount; i++)
            {
                // How much of this heart is filled (0 = empty, 1 = full).
                float fill = Mathf.Clamp01(scaledHealth - i);

                bool isFull = fill >= 1f;
                bool isHalf = !isFull && fill >= HalfHeartThreshold;
                bool isEmpty = fill < HalfHeartThreshold;

                if (hearts[i].full != null)
                    hearts[i].full.SetActive(isFull);

                if (hearts[i].half != null)
                    hearts[i].half.SetActive(isHalf);

                if (hearts[i].empty != null)
                    hearts[i].empty.SetActive(isEmpty);
            }
        }

        private void OnHealthChanged(float current, float max)
        {
            float percent = max <= 0f ? 0f : current / max;
            Refresh(percent);
        }

        private void OnValidate()
        {
            if (hearts.Length != HeartCount)
                Debug.LogWarning($"[CielaHealthDisplay] Hearts array must have exactly {HeartCount} entries.", this);
        }
    }
}
