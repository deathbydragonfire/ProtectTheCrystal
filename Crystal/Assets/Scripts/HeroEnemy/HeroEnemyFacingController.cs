using UnityEngine;

namespace Crystal.HeroEnemy
{
    /// <summary>
    /// Flips the hero enemy's visual root X scale based on horizontal movement direction.
    /// The scale is only updated when the direction actually changes — not every frame.
    /// Positive X scale faces right; negative X scale faces left.
    ///
    /// Call LockFacing to override velocity-based flipping (e.g. during attacks),
    /// and UnlockFacing to resume automatic behaviour.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class HeroEnemyFacingController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;

        [Header("Settings")]
        [Tooltip("Horizontal velocity magnitude below which direction changes are ignored.")]
        [SerializeField] private float velocityDeadZone = 0.05f;

        private Rigidbody2D body;
        private float currentFacingSign = 1f;
        private bool facingLocked;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            currentFacingSign = Mathf.Sign(visualRoot != null ? visualRoot.localScale.x : 1f);
        }

        private void Update()
        {
            if (visualRoot == null || facingLocked)
                return;

            float velocityX = body.linearVelocity.x;

            if (Mathf.Abs(velocityX) <= velocityDeadZone)
                return;

            float desiredSign = Mathf.Sign(velocityX);

            if (desiredSign == currentFacingSign)
                return;

            currentFacingSign = desiredSign;

            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * currentFacingSign;
            visualRoot.localScale = scale;
        }

        /// <summary>
        /// Suppresses velocity-based facing and immediately snaps to face the given world position.
        /// </summary>
        public void LockFacingToward(Vector2 targetPosition)
        {
            facingLocked = true;

            if (visualRoot == null)
                return;

            float deltaX = targetPosition.x - transform.position.x;
            if (Mathf.Abs(deltaX) <= velocityDeadZone)
                return;

            float desiredSign = Mathf.Sign(deltaX);
            if (desiredSign == currentFacingSign)
                return;

            currentFacingSign = desiredSign;

            Vector3 scale = visualRoot.localScale;
            scale.x = Mathf.Abs(scale.x) * currentFacingSign;
            visualRoot.localScale = scale;
        }

        /// <summary>
        /// Resumes automatic velocity-based facing after a LockFacingToward call.
        /// </summary>
        public void UnlockFacing()
        {
            facingLocked = false;
        }
    }
}
