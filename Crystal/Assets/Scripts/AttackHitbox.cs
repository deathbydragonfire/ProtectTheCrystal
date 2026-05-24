using Crystal.HeroEnemy;
using UnityEngine;

/// <summary>
/// Attached to the attack hitbox child GameObject.
/// Applies damage to the first IDamageable hit per activation cycle.
/// Enable/disable this GameObject to begin and end an attack window.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AttackHitbox : MonoBehaviour
{
    // ── State ────────────────────────────────────────────────────────────────

    private float _damage;
    private bool _hitDelivered;

    // ── API ──────────────────────────────────────────────────────────────────

    /// <summary>Prepares the hitbox for a new attack swing with the given damage value.</summary>
    public void Arm(float damage)
    {
        _damage = damage;
        _hitDelivered = false;
    }

    // ── Unity Lifecycle ──────────────────────────────────────────────────────

    private void OnEnable()
    {
        // Reset hit flag each time the hitbox is activated (new attack).
        _hitDelivered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hitDelivered) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target == null) return;

        _hitDelivered = true;

        var info = new DamageInfo(
            amount: _damage,
            source: gameObject,
            point: other.ClosestPoint(transform.position),
            direction: (other.transform.position - transform.position).normalized
        );

        target.ApplyDamage(info);
        Debug.Log($"[AttackHitbox] Hit '{other.name}' for {_damage} damage.");
    }
}
