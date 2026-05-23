using UnityEngine;

namespace Crystal.HeroEnemy
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class HeroArrowProjectile : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private float damage = 8f;
        [SerializeField] private LayerMask hitLayers = ~0;
        [SerializeField] private bool destroyOnHit = true;

        [Header("Visual Alignment")]
        [Tooltip("Degrees added after aligning the projectile's local up axis to its flight direction.")]
        [SerializeField] private float visualRotationOffsetDegrees;

        private Rigidbody2D body;
        private GameObject source;
        private Vector2 direction = Vector2.right;
        private float activeDamage;
        private float despawnTime;

        public void Fire(Vector2 fireDirection, float damageOverride, GameObject damageSource)
        {
            direction = fireDirection.sqrMagnitude > 0.0001f ? fireDirection.normalized : Vector2.right;
            activeDamage = damageOverride > 0f ? damageOverride : damage;
            source = damageSource;
            despawnTime = Time.time + lifetime;

            if (body == null)
                body = GetComponent<Rigidbody2D>();

            body.linearVelocity = direction * speed;
            ApplyFlightRotation();
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;

            activeDamage = damage;
        }

        private void OnEnable()
        {
            despawnTime = Time.time + lifetime;
        }

        private void Update()
        {
            if (Time.time >= despawnTime)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHit(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryHit(collision.collider);
        }

        private void TryHit(Collider2D other)
        {
            if (other == null || !IsInHitLayer(other.gameObject.layer) || IsSource(other.transform))
                return;

            IDamageable damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && damageable.IsAlive)
            {
                damageable.ApplyDamage(new DamageInfo(activeDamage, source, transform.position, direction, "Arrow"));
            }

            if (destroyOnHit)
                Destroy(gameObject);
        }

        private bool IsSource(Transform other)
        {
            return source != null && (other.gameObject == source || other.IsChildOf(source.transform));
        }

        private bool IsInHitLayer(int layer)
        {
            return (hitLayers.value & (1 << layer)) != 0;
        }

        private void OnValidate()
        {
            speed = Mathf.Max(0f, speed);
            lifetime = Mathf.Max(0.01f, lifetime);
            damage = Mathf.Max(0f, damage);

            Collider2D projectileCollider = GetComponent<Collider2D>();
            if (projectileCollider != null)
                projectileCollider.isTrigger = true;
        }

        private void ApplyFlightRotation()
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f + visualRotationOffsetDegrees;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (body != null)
                body.SetRotation(angle);
        }
    }
}
