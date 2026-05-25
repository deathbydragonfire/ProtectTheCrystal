using Crystal.HeroEnemy;
using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Listens to a CombatHealth component and raises the PlayerDiedEventChannel
    /// when that entity's health reaches zero.
    /// Attach this to the player GameObject and assign the player's CombatHealth.
    /// </summary>
    public sealed class PlayerDiedEventRaiser : MonoBehaviour
    {
        [SerializeField] private CombatHealth playerCombatHealth;
        [SerializeField] private PlayerDiedEventChannel playerDiedEventChannel;

        private void OnEnable()
        {
            if (playerCombatHealth == null)
            {
                Debug.LogWarning("[PlayerDiedEventRaiser] No CombatHealth assigned — player death will not be detected.", this);
                return;
            }

            playerCombatHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (playerCombatHealth != null)
                playerCombatHealth.Died -= OnPlayerDied;
        }

        private void OnPlayerDied(CombatHealth _)
        {
            if (playerDiedEventChannel == null)
            {
                Debug.LogWarning("[PlayerDiedEventRaiser] Cannot raise player died event because no event channel is assigned.", this);
                return;
            }

            Debug.Log("[PlayerDiedEventRaiser] Player died — raising PlayerDiedEventChannel.");
            playerDiedEventChannel.Raise();
        }
    }
}
