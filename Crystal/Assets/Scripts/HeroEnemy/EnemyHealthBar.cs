using UnityEngine;

namespace Crystal.HeroEnemy
{
    /// <summary>
    /// Drives a world-space health bar UI that floats above the enemy's head.
    /// Uses anchorMax.x on the fill RectTransform to represent health — no sprite required.
    /// Subscribes to CombatHealth events to keep the bar in sync.
    /// </summary>
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CombatHealth combatHealth;

        /// <summary>The RectTransform of the fill — anchorMax.x is driven to represent health percent.</summary>
        [SerializeField] private RectTransform fillRect;

        [Header("Settings")]
        [SerializeField] private bool hideWhenFull = true;
        [SerializeField] private GameObject barRoot;

        // Tracks whether Start has run so OnEnable re-syncs correctly on disable/re-enable.
        private bool started;

        private void Awake()
        {
            if (combatHealth == null)
                combatHealth = GetComponentInParent<CombatHealth>();
        }

        /// <summary>
        /// Runs after all Awake calls, guaranteeing CombatHealth has initialised
        /// its currentHealth from serialized values before we read it.
        /// </summary>
        private void Start()
        {
            started = true;
            SetFill(combatHealth.HealthPercent);
        }

        private void OnEnable()
        {
            combatHealth.HealthChanged += OnHealthChanged;
            combatHealth.Died += OnDied;

            // Re-sync when re-enabled mid-game (after Start has already run).
            if (started)
                SetFill(combatHealth.HealthPercent);
        }

        private void OnDisable()
        {
            combatHealth.HealthChanged -= OnHealthChanged;
            combatHealth.Died -= OnDied;
        }

        private void OnHealthChanged(float current, float max)
        {
            float percent = max <= 0f ? 0f : current / max;
            SetFill(percent);
        }

        private void OnDied(CombatHealth _)
        {
            if (barRoot != null)
                barRoot.SetActive(false);
        }

        private void SetFill(float percent)
        {
            percent = Mathf.Clamp01(percent);

            // Drive fill width via anchorMax.x so it stretches from left edge to percent.
            fillRect.anchorMax = new Vector2(percent, fillRect.anchorMax.y);

            if (hideWhenFull && barRoot != null)
                barRoot.SetActive(!Mathf.Approximately(percent, 1f));
        }
    }
}
