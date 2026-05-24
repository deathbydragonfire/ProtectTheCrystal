using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Collectable crystal chunk. When the player touches it, it raises the
    /// CrystalChunkCollectedEventChannel with the configured fill amount and destroys itself.
    /// Requires a trigger Collider2D on this GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class CrystalChunkPickup : MonoBehaviour
    {
        /// <summary>How much fill to add to the CrystalBar when collected [0, 1].</summary>
        [SerializeField, Range(0f, 1f)] private float fillAmount = 0.05f;

        [SerializeField] private CrystalChunkCollectedEventChannel eventChannel;

        private const string PlayerTag = "Player";

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(PlayerTag)) return;

            if (eventChannel == null)
            {
                Debug.LogWarning("[CrystalChunkPickup] eventChannel is not assigned.", this);
            }
            else
            {
                eventChannel.Raise(fillAmount);
                Debug.Log($"[CrystalChunkPickup] Collected — raised event with {fillAmount * 100f:F0}% fill.");
            }

            Destroy(gameObject);
        }
    }
}
