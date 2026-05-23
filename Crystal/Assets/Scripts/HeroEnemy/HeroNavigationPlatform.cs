using UnityEngine;

namespace Crystal.HeroEnemy
{
    [DisallowMultipleComponent]
    public sealed class HeroNavigationPlatform : MonoBehaviour
    {
        [SerializeField] private Collider2D platformCollider;
        [SerializeField] private float landingPadding = 0.35f;
        [SerializeField] private bool allowPlungePerch = true;

        public Collider2D PlatformCollider => ResolveCollider();
        public float LandingPadding => landingPadding;
        public bool AllowPlungePerch => allowPlungePerch;

        private Collider2D ResolveCollider()
        {
            if (platformCollider != null)
                return platformCollider;

            platformCollider = GetComponent<Collider2D>();
            if (platformCollider == null)
                platformCollider = GetComponentInChildren<Collider2D>();

            return platformCollider;
        }

        private void Reset()
        {
            ResolveCollider();
        }

        private void OnValidate()
        {
            landingPadding = Mathf.Max(0f, landingPadding);
            ResolveCollider();
        }
    }
}
