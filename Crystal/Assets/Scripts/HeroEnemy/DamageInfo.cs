using UnityEngine;

namespace Crystal.HeroEnemy
{
    public readonly struct DamageInfo
    {
        public DamageInfo(float amount, GameObject source = null, Vector2 point = default, Vector2 direction = default, string damageType = "")
        {
            Amount = Mathf.Max(0f, amount);
            Source = source;
            Point = point;
            Direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
            DamageType = damageType ?? string.Empty;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public Vector2 Point { get; }
        public Vector2 Direction { get; }
        public string DamageType { get; }
    }

    public interface IDamageable
    {
        bool IsAlive { get; }

        void ApplyDamage(DamageInfo damageInfo);
    }
}
