using Crystal.HeroEnemy;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Converts crystal-full and hero-death signals into end-game outcomes.
    ///
    /// Victory condition: crystal fill reaches 100%.
    /// Backup victory condition: the hero enemy dies while the crystal is already full.
    /// Fake-reset condition: the hero enemy dies before the crystal is full.
    /// </summary>
    public sealed class PlayerVictoryEventRaiser : MonoBehaviour
    {
        [SerializeField] private CombatHealth enemyCombatHealth;
        [SerializeField] private CrystalBar crystalBar;
        [SerializeField] private HeroDefeatedEventChannel heroDefeatedEventChannel;
        [SerializeField] private PlayerVictoryEventChannel playerVictoryEventChannel;

        /// <summary>Fill amount considered "full". Kept slightly below 1 to tolerate floating-point accumulation.</summary>
        private const float FullFillThreshold = 0.999f;

        private bool _hasRaisedEndState;

        private void OnEnable()
        {
            if (enemyCombatHealth == null)
                Debug.LogWarning("[PlayerVictoryEventRaiser] No enemy CombatHealth assigned - early hero-defeated outcome cannot be evaluated.", this);
            else
                enemyCombatHealth.Died += OnEnemyDied;

            if (crystalBar == null)
                Debug.LogWarning("[PlayerVictoryEventRaiser] No CrystalBar assigned - crystal-full victory cannot be detected.", this);
            else
                crystalBar.Filled += OnCrystalFilled;
        }

        private void OnDisable()
        {
            if (enemyCombatHealth != null)
                enemyCombatHealth.Died -= OnEnemyDied;

            if (crystalBar != null)
                crystalBar.Filled -= OnCrystalFilled;
        }

        private void OnCrystalFilled()
        {
            RaiseVictory();
        }

        private void OnEnemyDied(CombatHealth _)
        {
            bool isCrystalFull = crystalBar != null && crystalBar.FillPercent >= FullFillThreshold;

            if (isCrystalFull)
                RaiseVictory();
            else
                RaiseHeroDefeated();
        }

        private void RaiseVictory()
        {
            if (_hasRaisedEndState)
                return;

            if (playerVictoryEventChannel == null)
            {
                Debug.LogWarning("[PlayerVictoryEventRaiser] Cannot raise victory event because no PlayerVictoryEventChannel is assigned.", this);
                return;
            }

            _hasRaisedEndState = true;
            Debug.Log("[PlayerVictoryEventRaiser] Crystal full - raising PlayerVictoryEventChannel.", this);
            playerVictoryEventChannel.Raise();
        }

        private void RaiseHeroDefeated()
        {
            if (_hasRaisedEndState)
                return;

            if (heroDefeatedEventChannel == null)
            {
                Debug.LogWarning("[PlayerVictoryEventRaiser] Cannot raise hero defeated event because no HeroDefeatedEventChannel is assigned.", this);
                return;
            }

            _hasRaisedEndState = true;
            Debug.Log($"[PlayerVictoryEventRaiser] Hero defeated before crystal was full - crystal fill: {(crystalBar != null ? crystalBar.FillPercent.ToString("F3") : "no CrystalBar")} (threshold: {FullFillThreshold:F3})", this);
            heroDefeatedEventChannel.Raise();
        }
    }
}
