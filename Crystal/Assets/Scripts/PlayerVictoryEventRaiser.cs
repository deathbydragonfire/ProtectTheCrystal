using Crystal.HeroEnemy;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Reads the crystal fill level from CrystalBar and the hero enemy's death to determine
    /// the correct end-game outcome.
    ///
    /// Victory condition:  hero enemy dies AND crystal fill is at 100 %.
    /// Fake-reset condition: hero enemy dies AND crystal fill is below 100 %.
    /// </summary>
    public sealed class PlayerVictoryEventRaiser : MonoBehaviour
    {
        [SerializeField] private CombatHealth enemyCombatHealth;
        [SerializeField] private CrystalBar crystalBar;
        [SerializeField] private HeroDefeatedEventChannel heroDefeatedEventChannel;
        [SerializeField] private PlayerVictoryEventChannel playerVictoryEventChannel;

        /// <summary>Fill amount considered "full". Kept slightly below 1 to tolerate floating-point accumulation.</summary>
        private const float FullFillThreshold = 0.999f;

        private void OnEnable()
        {
            if (enemyCombatHealth == null)
                Debug.LogWarning("[PlayerVictoryEventRaiser] No enemy CombatHealth assigned — victory condition cannot be evaluated.", this);
            else
                enemyCombatHealth.Died += OnEnemyDied;

            if (crystalBar == null)
                Debug.LogWarning("[PlayerVictoryEventRaiser] No CrystalBar assigned — crystal fill cannot be read.", this);
        }

        private void OnDisable()
        {
            if (enemyCombatHealth != null)
                enemyCombatHealth.Died -= OnEnemyDied;
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
            if (playerVictoryEventChannel == null)
            {
                Debug.LogWarning("[PlayerVictoryEventRaiser] Cannot raise victory event because no PlayerVictoryEventChannel is assigned.", this);
                return;
            }

            playerVictoryEventChannel.Raise();
        }

        private void RaiseHeroDefeated()
        {
            if (heroDefeatedEventChannel == null)
            {
                Debug.LogWarning("[PlayerVictoryEventRaiser] Cannot raise hero defeated event because no HeroDefeatedEventChannel is assigned.", this);
                return;
            }

            Debug.Log($"[PlayerVictoryEventRaiser] Hero defeated — crystal fill: {(crystalBar != null ? crystalBar.FillPercent.ToString("F3") : "no CrystalBar")} (threshold: {FullFillThreshold:F3})", this);
            heroDefeatedEventChannel.Raise();
        }
    }
}
