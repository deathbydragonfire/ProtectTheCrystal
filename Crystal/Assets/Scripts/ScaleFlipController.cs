using UnityEngine;

namespace Crystal
{
    /// <summary>
    /// Controls the horizontal scale of the GameObject based on the relative position of a target enemy.
    /// Since the character faces left by default (x = 1), a negative scale (x = -1) will make her face right.
    /// </summary>
    public class ScaleFlipController : MonoBehaviour
    {
        [Tooltip("The enemy transform to track.")]
        [SerializeField] private Transform _enemyTransform;

        // Default facing is Left (Scale X = 1)
        // Flipping to Right (Scale X = -1)
        private const float SCALE_LEFT = 1f;
        private const float SCALE_RIGHT = -1f;

        private void Update()
        {
            if (_enemyTransform == null) return;

            UpdateScale();
        }

        /// <summary>
        /// Updates the local scale X based on the enemy's X position relative to the player.
        /// </summary>
        private void UpdateScale()
        {
            Vector3 currentScale = transform.localScale;
            
            // If the enemy's x position is less than player's (enemy is to the left), 
            // set x scale to 1 (face left). Otherwise, set to -1 (face right).
            float targetXScale = (_enemyTransform.position.x < transform.position.x) ? SCALE_LEFT : SCALE_RIGHT;

            if (!Mathf.Approximately(currentScale.x, targetXScale))
            {
                currentScale.x = targetXScale;
                transform.localScale = currentScale;
            }
        }
    }
}
