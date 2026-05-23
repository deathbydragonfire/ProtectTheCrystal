using System.Collections;
using UnityEngine;

namespace Crystal.HeroEnemy
{
    public sealed class HeroEnemyAttackController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HeroEnemyMotor motor;
        [SerializeField] private HeroSpriteFrameAnimator spriteAnimator;
        [SerializeField] private Transform bowOrigin;
        [SerializeField] private Transform plungeHitOrigin;

        [Header("Hit Filtering")]
        [SerializeField] private LayerMask targetHitLayers = ~0;

        [Header("Bow")]
        [SerializeField] private GameObject arrowProjectilePrefab;
        [SerializeField] private float bowDamage = 8f;
        [SerializeField] private float bowAimDuration = 0.35f;
        [SerializeField] private float movingBowAimDuration = 0.5f;
        [SerializeField] private float bowRecovery = 0.2f;
        [SerializeField] private float bowCooldown = 1.1f;
        [SerializeField] private float movingBowCooldownMultiplier = 1.4f;

        [Header("Leap Plunge")]
        [SerializeField] private float leapDamage = 18f;
        [SerializeField] private float leapOverHorizontalOffset = 1.6f;
        [SerializeField] private float leapHorizontalSpeed = 7.5f;
        [SerializeField] private float leapUpwardVelocity = 10f;
        [SerializeField] private float leapAirTimeBeforePlunge = 0.32f;
        [SerializeField] private float plungeSpeed = 16f;
        [SerializeField] private float plungeHitDelay = 0.08f;
        [SerializeField] private float plungeAttackDuration = 0.28f;
        [SerializeField] private float leapRecovery = 0.35f;
        [SerializeField] private float leapCooldown = 1.85f;
        [SerializeField] private float plungeHitRadius = 1.1f;

        [Header("Aggression")]
        [SerializeField, Range(0.1f, 1f)] private float aggressiveCooldownMultiplier = 0.65f;

        [Header("Feint")]
        [SerializeField] private float feintCooldownPercent = 0.5f;

        public event System.Action BowFired;
        public event System.Action<bool> PlungeCompleted;

        private Coroutine activeAttack;
        private float nextBowTime;
        private float nextLeapTime;

        public bool IsBusy => activeAttack != null;
        public bool CanBow => !IsBusy && Time.time >= nextBowTime;
        public bool CanLeapPlunge => !IsBusy && Time.time >= nextLeapTime;

        public bool StartBowAttack(CombatHealth targetHealth, Transform targetTransform, bool aggressive)
        {
            if (!CanBow)
                return false;

            activeAttack = StartCoroutine(BowRoutine(targetHealth, targetTransform, aggressive));
            return true;
        }

        public bool StartMovingBowAttack(CombatHealth targetHealth, Transform targetTransform, bool aggressive)
        {
            if (!CanBow)
                return false;

            activeAttack = StartCoroutine(BowRoutine(targetHealth, targetTransform, aggressive, moving: true));
            return true;
        }

        public bool StartLeapPlungeAttack(CombatHealth targetHealth, Transform targetTransform, bool aggressive)
        {
            if (!CanLeapPlunge || targetTransform == null)
                return false;

            activeAttack = StartCoroutine(LeapPlungeRoutine(targetHealth, targetTransform, aggressive));
            return true;
        }

        public bool StartLeapFeintAttack(Transform targetTransform, bool aggressive)
        {
            if (!CanLeapPlunge || targetTransform == null)
                return false;

            activeAttack = StartCoroutine(LeapFeintRoutine(targetTransform, aggressive));
            return true;
        }

        public void CancelCurrentAction()
        {
            if (activeAttack == null)
                return;

            StopCoroutine(activeAttack);
            activeAttack = null;
        }

        public void ReduceBowCooldown(float amount)
        {
            nextBowTime = Mathf.Max(Time.time, nextBowTime - amount);
        }

        private void Awake()
        {
            if (motor == null)
                motor = GetComponent<HeroEnemyMotor>();

            if (spriteAnimator == null)
                spriteAnimator = GetComponent<HeroSpriteFrameAnimator>();
        }

        private IEnumerator BowRoutine(CombatHealth targetHealth, Transform targetTransform, bool aggressive, bool moving = false)
        {
            if (!moving)
                motor?.StopHorizontal();

            if (targetTransform != null)
                motor?.FacePosition(targetTransform.position);

            spriteAnimator?.Play(HeroAnimationAction.AimBow, true);
            yield return new WaitForSeconds(moving ? movingBowAimDuration : bowAimDuration);

            spriteAnimator?.Play(HeroAnimationAction.ShootBow, true);
            ShootArrow(targetHealth, targetTransform);
            BowFired?.Invoke();

            nextBowTime = Time.time + GetCooldown(bowCooldown, aggressive) * (moving ? movingBowCooldownMultiplier : 1f);
            yield return new WaitForSeconds(bowRecovery);
            activeAttack = null;
        }

        private IEnumerator LeapPlungeRoutine(CombatHealth targetHealth, Transform targetTransform, bool aggressive)
        {
            Vector2 capturedPosition = targetTransform != null ? (Vector2)targetTransform.position : (Vector2)transform.position;
            float side = Mathf.Sign(capturedPosition.x - transform.position.x);
            if (Mathf.Approximately(side, 0f))
                side = 1f;

            Vector2 leapTarget = capturedPosition + Vector2.right * side * leapOverHorizontalOffset;
            spriteAnimator?.Play(HeroAnimationAction.Leap, true);
            motor?.LaunchToward(leapTarget, leapHorizontalSpeed, leapUpwardVelocity);

            float elapsed = 0f;
            while (elapsed < leapAirTimeBeforePlunge)
            {
                elapsed += Time.deltaTime;
                if (targetTransform != null)
                    motor?.FacePosition(targetTransform.position);
                yield return null;
            }

            Vector2 currentTargetPos = targetTransform != null ? (Vector2)targetTransform.position : capturedPosition;
            float xBias = Mathf.Clamp(currentTargetPos.x - transform.position.x, -2f, 2f);

            spriteAnimator?.Play(HeroAnimationAction.Plunge, true);
            motor?.PlungeToward(plungeSpeed, xBias);

            yield return new WaitForSeconds(plungeHitDelay);

            bool hitTarget = PerformCircleHit(plungeHitOrigin != null ? plungeHitOrigin.position : transform.position, plungeHitRadius, leapDamage, "LeapPlunge", targetHealth);
            if (!hitTarget)
                hitTarget = TryDirectTargetDamage(targetHealth, targetTransform, leapDamage, "LeapPlunge", plungeHitRadius + 0.75f);

            yield return new WaitForSeconds(plungeAttackDuration);
            nextLeapTime = Time.time + GetCooldown(leapCooldown, aggressive);

            yield return new WaitForSeconds(leapRecovery);
            PlungeCompleted?.Invoke(hitTarget);
            activeAttack = null;
        }

        private IEnumerator LeapFeintRoutine(Transform targetTransform, bool aggressive)
        {
            Vector2 capturedPosition = targetTransform != null ? (Vector2)targetTransform.position : (Vector2)transform.position;
            float side = Mathf.Sign(capturedPosition.x - transform.position.x);
            if (Mathf.Approximately(side, 0f))
                side = 1f;

            Vector2 leapTarget = capturedPosition + Vector2.right * side * leapOverHorizontalOffset;
            spriteAnimator?.Play(HeroAnimationAction.Leap, true);
            motor?.LaunchToward(leapTarget, leapHorizontalSpeed, leapUpwardVelocity);

            float elapsed = 0f;
            while (elapsed < leapAirTimeBeforePlunge)
            {
                elapsed += Time.deltaTime;
                if (targetTransform != null)
                    motor?.FacePosition(targetTransform.position);
                yield return null;
            }

            Vector2 currentTargetPos = targetTransform != null ? (Vector2)targetTransform.position : capturedPosition;
            motor?.DashAwayFrom(currentTargetPos);
            nextLeapTime = Time.time + GetCooldown(leapCooldown, aggressive) * feintCooldownPercent;

            yield return new WaitForSeconds(leapRecovery);
            activeAttack = null;
        }

        private void ShootArrow(CombatHealth targetHealth, Transform targetTransform)
        {
            Transform origin = bowOrigin != null ? bowOrigin : transform;
            Vector2 direction = targetTransform != null
                ? ((Vector2)targetTransform.position - (Vector2)origin.position).normalized
                : Vector2.right * Mathf.Sign(transform.localScale.x == 0f ? 1f : transform.localScale.x);

            if (direction.sqrMagnitude <= 0.0001f)
                direction = Vector2.right;

            if (arrowProjectilePrefab == null)
            {
                TryDirectTargetDamage(targetHealth, targetTransform, bowDamage, "Arrow", float.PositiveInfinity);
                return;
            }

            GameObject arrow = Instantiate(arrowProjectilePrefab, origin.position, Quaternion.identity);
            HeroArrowProjectile projectile = arrow.GetComponent<HeroArrowProjectile>();
            if (projectile != null)
                projectile.Fire(direction, bowDamage, gameObject);
        }

        private bool PerformCircleHit(Vector2 origin, float radius, float damage, string damageType, CombatHealth targetHealth)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, targetHitLayers);
            return DamageHits(hits, damage, damageType, targetHealth, origin);
        }

        private bool DamageHits(Collider2D[] hits, float damage, string damageType, CombatHealth targetHealth, Vector2 hitOrigin)
        {
            bool hitExpectedTarget = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D hit = hits[i];

                if (hit == null || hit.transform.IsChildOf(transform))
                    continue;

                IDamageable damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.IsAlive)
                    continue;

                Vector2 direction = ((Vector2)hit.transform.position - hitOrigin).normalized;
                damageable.ApplyDamage(new DamageInfo(damage, gameObject, hit.ClosestPoint(hitOrigin), direction, damageType));

                if (targetHealth != null && ReferenceEquals(damageable, targetHealth))
                    hitExpectedTarget = true;
            }

            return hitExpectedTarget;
        }

        private bool TryDirectTargetDamage(CombatHealth targetHealth, Transform targetTransform, float damage, string damageType, float maxDistance)
        {
            if (targetHealth == null || !targetHealth.IsAlive || targetTransform == null)
                return false;

            float sqrDistance = ((Vector2)targetTransform.position - (Vector2)transform.position).sqrMagnitude;
            if (sqrDistance > maxDistance * maxDistance)
                return false;

            Vector2 direction = ((Vector2)targetTransform.position - (Vector2)transform.position).normalized;
            targetHealth.ApplyDamage(new DamageInfo(damage, gameObject, targetTransform.position, direction, damageType));
            return true;
        }

        private float GetCooldown(float baseCooldown, bool aggressive)
        {
            return baseCooldown * (aggressive ? aggressiveCooldownMultiplier : 1f);
        }

        private void OnValidate()
        {
            bowDamage = Mathf.Max(0f, bowDamage);
            bowAimDuration = Mathf.Max(0f, bowAimDuration);
            movingBowAimDuration = Mathf.Max(0f, movingBowAimDuration);
            bowRecovery = Mathf.Max(0f, bowRecovery);
            bowCooldown = Mathf.Max(0f, bowCooldown);
            movingBowCooldownMultiplier = Mathf.Max(0f, movingBowCooldownMultiplier);
            leapDamage = Mathf.Max(0f, leapDamage);
            leapOverHorizontalOffset = Mathf.Max(0f, leapOverHorizontalOffset);
            leapHorizontalSpeed = Mathf.Max(0f, leapHorizontalSpeed);
            leapUpwardVelocity = Mathf.Max(0f, leapUpwardVelocity);
            leapAirTimeBeforePlunge = Mathf.Max(0f, leapAirTimeBeforePlunge);
            plungeSpeed = Mathf.Max(0f, plungeSpeed);
            plungeHitDelay = Mathf.Max(0f, plungeHitDelay);
            plungeAttackDuration = Mathf.Max(0f, plungeAttackDuration);
            leapRecovery = Mathf.Max(0f, leapRecovery);
            leapCooldown = Mathf.Max(0f, leapCooldown);
            plungeHitRadius = Mathf.Max(0.01f, plungeHitRadius);
            feintCooldownPercent = Mathf.Clamp01(feintCooldownPercent);
        }

        private void OnDrawGizmosSelected()
        {
            if (plungeHitOrigin != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(plungeHitOrigin.position, plungeHitRadius);
            }
        }
    }
}
