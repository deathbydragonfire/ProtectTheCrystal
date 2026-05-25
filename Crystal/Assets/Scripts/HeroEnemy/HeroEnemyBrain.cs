using UnityEngine;

namespace Crystal.HeroEnemy
{
    public sealed class HeroEnemyBrain : MonoBehaviour
    {
        [Header("Target References")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private global::SpiderGirlController targetSurfaceController;
        [SerializeField] private CombatHealth targetHealth;
        [SerializeField] private Transform targetAimPoint;
        [SerializeField] private Transform targetNavigationPoint;
        [SerializeField] private float targetNavigationVerticalOffset = -2.24f;

        [Header("Self References")]
        [SerializeField] private CombatHealth selfHealth;
        [SerializeField] private HeroEnemyMotor motor;
        [SerializeField] private HeroEnemyAttackController attacks;
        [Header("Health Priorities")]
        [SerializeField, Range(0f, 1f)] private float heroLowHealthPercent = 0.5f;
        [SerializeField, Range(0f, 1f)] private float targetFinisherHealthPercent = 0.25f;

        [Header("Healing")]
        [SerializeField] private HealingItemDefinition healingItemDefinition;
        [SerializeField] private float healingSearchRadius = 18f;
        [SerializeField] private float healingConsumeDistance = 0.85f;

        [Header("Decision Timing")]
        [SerializeField] private float decisionInterval = 0.08f;
        [SerializeField] private float missingTargetIdleDelay = 0.25f;
        [SerializeField] private float strategyMinHoldTime = 0.6f;

        [Header("Decision Weights")]
        [SerializeField] private HeroEnemyDecisionWeights _decisionWeights;

        [Header("Combat Ranges")]
        [SerializeField] private float leapPlungeMinRange = 1.25f;
        [SerializeField] private float leapPlungeMaxRange = 6.5f;
        [SerializeField] private float bowRange = 9f;

        [Header("Plunge Setup")]
        [SerializeField] private bool requireHeightAdvantageForPlunge = false;
        [SerializeField] private bool preferPlatformsForPlungeSetup = true;
        [SerializeField] private float plungeHeightAdvantage = 1.1f;
        [SerializeField] private float plungeSetupMaxRange = 8f;
        [SerializeField] private float plungePerchHorizontalWindow = 5f;

        [Header("Line Of Sight")]
        [SerializeField] private bool requireLineOfSightForBow;
        [SerializeField] private Transform lineOfSightOrigin;
        [SerializeField] private LayerMask lineOfSightBlockers;

        [Header("Platform Navigation")]
        [SerializeField] private bool usePlatformNavigation = true;
        [SerializeField] private bool forcePlatformsForCeilingTargets = true;
        [SerializeField] private bool preferPlatformsForElevatedGroundTargets = true;
        [SerializeField] private float elevatedGroundTargetHeight = 0.75f;

        [Header("Retreat")]
        [SerializeField] private float retreatDuration = 0.6f;
        [SerializeField] private float retreatSafeDistance = 5f;
        [SerializeField] private bool allowRetreatJump = true;
        [SerializeField] private float damageDecayRate = 15f;
        [SerializeField] private float retreatDamageThreshold = 12f;

        [Header("Aggression")]
        [SerializeField] private float postPlungePressureWindow = 0.5f;

        [Header("Combat")]
        [SerializeField, Range(0f, 1f)] private float feintChance = 0.25f;

        [Header("Blocked Route Fallback")]
        [SerializeField] private float blockedRouteEvadeDistance = 3.5f;
        [SerializeField, Range(0.1f, 1f)] private float blockedRouteBowApproachRangePercent = 0.85f;
        [SerializeField] private float blockedRouteRetryDelay = 0.2f;

        private float nextDecisionTime;
        private float retreatUntilTime;
        private bool setupWarningLogged;
        private bool _decisionWeightsWarningLogged;
        private HeroEnemyDecision currentDecision = HeroEnemyDecision.Idle;
        private Vector2 _targetLastPosition;
        private float _targetStationaryTimer;
        private float _targetOnGroundTimer;
        private float _postPlungePressureUntilTime;
        private float _lastBowFireTime;
        private float _recentDamageAccumulated;
        private float _strategyHoldUntilTime;
        private HeroEnemyDecision _committedDecision = HeroEnemyDecision.Idle;

        public HeroEnemyDecision CurrentDecision => currentDecision;

        public void SetTarget(Transform target, global::SpiderGirlController surfaceController, CombatHealth health, Transform aimPoint = null)
        {
            targetTransform = target;
            targetSurfaceController = surfaceController;
            targetHealth = health;
            targetAimPoint = aimPoint;
        }

        private void Awake()
        {
            if (selfHealth == null)
                selfHealth = GetComponent<CombatHealth>();

            if (motor == null)
                motor = GetComponent<HeroEnemyMotor>();

            if (attacks == null)
                attacks = GetComponent<HeroEnemyAttackController>();
        }

        private void OnEnable()
        {
            if (selfHealth != null)
            {
                selfHealth.Damaged += HandleDamaged;
                selfHealth.Died += HandleDied;
            }

            if (attacks != null)
            {
                attacks.BowFired += OnBowFired;
                attacks.PlungeCompleted += HandlePlungeCompleted;
            }
        }

        private void OnDisable()
        {
            if (selfHealth != null)
            {
                selfHealth.Damaged -= HandleDamaged;
                selfHealth.Died -= HandleDied;
            }

            if (attacks != null)
            {
                attacks.BowFired -= OnBowFired;
                attacks.PlungeCompleted -= HandlePlungeCompleted;
            }
        }

        private void Update()
        {
            if (selfHealth != null && !selfHealth.IsAlive)
                return;

            _recentDamageAccumulated = Mathf.Max(0f, _recentDamageAccumulated - damageDecayRate * Time.deltaTime);

            if (targetTransform != null)
            {
                if (Vector2.Distance((Vector2)targetTransform.position, _targetLastPosition) > 0.05f)
                {
                    _targetStationaryTimer = 0f;
                    _targetLastPosition = targetTransform.position;
                }
                else
                {
                    _targetStationaryTimer += Time.deltaTime;
                }

                if (targetSurfaceController != null && targetSurfaceController.IsOnGround)
                    _targetOnGroundTimer += Time.deltaTime;
                else
                    _targetOnGroundTimer = 0f;
            }

            if (!ValidateRuntimeSetup())
            {
                Idle();
                return;
            }

            if (attacks != null && attacks.IsBusy)
                return;

            if (Time.time < nextDecisionTime)
                return;

            nextDecisionTime = Time.time + decisionInterval;
            TickDecision();
        }

        private void TickDecision()
        {
            if (Time.time < retreatUntilTime)
            {
                HandleRetreat();
                return;
            }

            bool heroLowHealth = selfHealth != null && selfHealth.HealthPercent <= heroLowHealthPercent;
            HeroHealingPickup healingPickup = heroLowHealth ? FindBestHealingPickup() : null;

            if (heroLowHealth && healingPickup != null)
            {
                ApplyDecision(HeroEnemyDecision.SeekHealing, healingPickup, false);
                return;
            }

            if (_decisionWeights == null)
            {
                if (!_decisionWeightsWarningLogged)
                {
                    _decisionWeightsWarningLogged = true;
                    Debug.LogWarning("[HeroEnemyBrain] _decisionWeights is not assigned. Falling back to ApproachTarget.", this);
                }

                ApplyDecision(HeroEnemyDecision.ApproachTarget, healingPickup, false);
                return;
            }

            HeroEnemyDecisionInput input = BuildDecisionInput(heroLowHealth, healingPickup != null);
            HeroEnemyDecision decision = HeroEnemyDecisionEvaluator.Evaluate(input, _decisionWeights);

            if (ShouldPreferPlatformApproach(decision))
                decision = HeroEnemyDecision.ApproachGroundTarget;

            bool isSurvival = decision == HeroEnemyDecision.Dead
                || decision == HeroEnemyDecision.Retreat
                || decision == HeroEnemyDecision.SeekHealing;

            if (!isSurvival && Time.time < _strategyHoldUntilTime && _committedDecision != HeroEnemyDecision.Idle)
            {
                decision = _committedDecision;
            }
            else if (!isSurvival && decision != _committedDecision)
            {
                _committedDecision = decision;
                _strategyHoldUntilTime = Time.time + strategyMinHoldTime;
            }

            ApplyDecision(decision, healingPickup, input.TargetLowHealth);
        }

        private HeroEnemyDecisionInput BuildDecisionInput(bool heroLowHealth, bool healingAvailable)
        {
            float distance = GetTargetDistance();
            bool targetOnCeiling = targetSurfaceController != null && targetSurfaceController.IsOnCeiling;
            bool targetOnGround = targetSurfaceController == null || targetSurfaceController.IsOnGround || !targetOnCeiling;
            bool hasLineOfSight = HasLineOfSight();
            bool targetLowHealth = targetHealth != null && targetHealth.HealthPercent <= targetFinisherHealthPercent;

            return new HeroEnemyDecisionInput
            {
                HeroDead = selfHealth != null && !selfHealth.IsAlive,
                WasDamagedRecently = Time.time < retreatUntilTime,
                HeroLowHealth = heroLowHealth,
                HealingAvailable = healingAvailable,
                TargetLowHealth = targetLowHealth,
                TargetOnGround = targetOnGround,
                TargetOnCeiling = targetOnCeiling,
                HasLineOfSight = hasLineOfSight,
                InLeapRange = distance >= leapPlungeMinRange && distance <= leapPlungeMaxRange,
                InBowRange = distance <= bowRange,
                CanLeapPlunge = CanStartPlungeAttack(),
                CanBow = attacks != null && attacks.CanBow,
                HasHeightAdvantage = HasPlungeHeightAdvantage(),
                TargetStationaryDuration = _targetStationaryTimer,
                TargetOnGroundDuration = _targetOnGroundTimer,
                TimeSinceLastBow = Time.time - _lastBowFireTime,
                PostPlungePressureActive = Time.time < _postPlungePressureUntilTime
            };
        }

        private void ApplyDecision(HeroEnemyDecision decision, HeroHealingPickup healingPickup, bool aggressive)
        {
            currentDecision = decision;
            motor?.SetAggressive(aggressive);

            switch (decision)
            {
                case HeroEnemyDecision.Dead:
                    HandleDied(selfHealth);
                    break;
                case HeroEnemyDecision.Retreat:
                    HandleRetreat();
                    break;
                case HeroEnemyDecision.SeekHealing:
                    HandleHealing(healingPickup);
                    break;
                case HeroEnemyDecision.LeapPlungeAttack:
                    if (attacks != null && attacks.CanLeapPlunge && Random.value < feintChance)
                    {
                        attacks.StartLeapFeintAttack(targetTransform, aggressive);
                        break;
                    }

                    if (attacks == null || !attacks.StartLeapPlungeAttack(targetHealth, targetTransform, aggressive))
                        ApproachTarget(aggressive);
                    break;
                case HeroEnemyDecision.BowAttack:
                    if (attacks == null || !attacks.StartBowAttack(targetHealth, targetTransform, aggressive))
                        ApproachTarget(aggressive);
                    break;
                case HeroEnemyDecision.MovingBowAttack:
                    if (attacks == null || !attacks.StartMovingBowAttack(targetHealth, targetTransform, aggressive))
                        ApproachTarget(aggressive);
                    break;
                case HeroEnemyDecision.JumpForLineOfSight:
                    if (!MoveTowardGoal(GetAimTargetPosition(), true))
                        motor?.JumpForLineOfSight(GetAimTargetPosition());
                    break;
                case HeroEnemyDecision.ApproachGroundTarget:
                case HeroEnemyDecision.ApproachTarget:
                    ApproachTarget(aggressive);
                    break;
                default:
                    Idle();
                    break;
            }
        }

        private void ApproachTarget(bool aggressive = false)
        {
            currentDecision = HeroEnemyDecision.ApproachTarget;

            if (ShouldUsePlungeSetupRoute())
            {
                if (MoveTowardPlungePerch())
                    return;

                if (MoveTowardGoal(GetNavigationTargetPosition(), true))
                    return;

                HandleBlockedRouteFallback(aggressive);

                return;
            }

            bool forcePlatformRoute = ShouldForcePlatformRoute();
            if (!MoveTowardGoal(GetNavigationTargetPosition(), forcePlatformRoute) && forcePlatformRoute)
                HandleBlockedRouteFallback(aggressive);
        }

        private void HandleHealing(HeroHealingPickup pickup)
        {
            if (pickup == null)
            {
                ApproachTarget();
                return;
            }

            currentDecision = HeroEnemyDecision.SeekHealing;
            float distance = Vector2.Distance(transform.position, pickup.transform.position);

            if (distance <= healingConsumeDistance)
            {
                motor?.StopHorizontal();
                pickup.TryConsume(selfHealth);
                return;
            }

            MoveTowardGoal(pickup.transform.position, false);
        }

        private void HandleRetreat()
        {
            currentDecision = HeroEnemyDecision.Retreat;

            if (targetTransform == null)
            {
                Idle();
                return;
            }

            float distance = GetTargetDistance();
            if (distance >= retreatSafeDistance && Time.time >= retreatUntilTime - retreatDuration * 0.5f)
            {
                retreatUntilTime = 0f;
                Idle();
                return;
            }

            motor?.RetreatFrom(targetTransform.position, allowRetreatJump);
        }

        private void Idle()
        {
            currentDecision = HeroEnemyDecision.Idle;
            motor?.StopHorizontal();
            nextDecisionTime = Time.time + missingTargetIdleDelay;
        }

        private bool MoveTowardGoal(Vector2 goalPosition, bool forcePlatformRoute)
        {
            bool usingPlatforms = usePlatformNavigation
                && motor != null
                && motor.MoveTowardUsingPlatforms(goalPosition, forcePlatformRoute);

            if (!usingPlatforms)
            {
                if (forcePlatformRoute)
                    return false;

                motor?.MoveToward(goalPosition);
            }

            return usingPlatforms;
        }

        private bool MoveTowardPlungePerch()
        {
            bool usingPlatforms = usePlatformNavigation
                && motor != null
                && motor.MoveTowardPlungePerch(GetNavigationTargetPosition(), plungeHeightAdvantage, plungePerchHorizontalWindow);

            if (!usingPlatforms)
                return false;

            return true;
        }

        private void HandleBlockedRouteFallback(bool aggressive)
        {
            if (TryFallbackBowAttack(aggressive))
                return;

            if (targetTransform == null)
            {
                Idle();
                return;
            }

            float distance = GetTargetDistance();
            if (distance > bowRange * blockedRouteBowApproachRangePercent)
            {
                currentDecision = HeroEnemyDecision.ApproachTarget;
                if (IsElevatedGroundTarget())
                    motor?.JumpForLineOfSight(GetNavigationTargetPosition());
                else
                    MoveTowardGoal(GetNavigationTargetPosition(), false);

                return;
            }

            if (!HasLineOfSight())
            {
                currentDecision = HeroEnemyDecision.JumpForLineOfSight;
                motor?.JumpForLineOfSight(GetNavigationTargetPosition());
                return;
            }

            if (distance < blockedRouteEvadeDistance)
            {
                currentDecision = HeroEnemyDecision.Retreat;
                motor?.MoveAwayFrom(targetTransform.position);
                return;
            }

            currentDecision = HeroEnemyDecision.ApproachTarget;
            motor?.StopHorizontal();
            motor?.FacePosition(targetTransform.position);
            nextDecisionTime = Time.time + blockedRouteRetryDelay;
        }

        private bool TryFallbackBowAttack(bool aggressive)
        {
            if (attacks == null
                || !attacks.CanBow
                || targetTransform == null
                || GetTargetDistance() > bowRange
                || !HasLineOfSight())
            {
                return false;
            }

            currentDecision = HeroEnemyDecision.BowAttack;
            return attacks.StartBowAttack(targetHealth, targetTransform, aggressive);
        }

        private bool ShouldForcePlatformRoute()
        {
            return (forcePlatformsForCeilingTargets && targetSurfaceController != null && targetSurfaceController.IsOnCeiling)
                || IsElevatedGroundTarget()
                || ShouldUsePlungeSetupRoute();
        }

        private bool ShouldPreferPlatformApproach(HeroEnemyDecision decision)
        {
            if (ShouldUsePlungeSetupRoute())
                return true;

            if (decision == HeroEnemyDecision.LeapPlungeAttack)
                return false;

            if (decision != HeroEnemyDecision.BowAttack)
                return false;

            return IsElevatedGroundTarget();
        }

        private bool CanStartPlungeAttack()
        {
            if (attacks == null || !attacks.CanLeapPlunge)
                return false;

            return !requireHeightAdvantageForPlunge || HasPlungeHeightAdvantage();
        }

        private bool ShouldUsePlungeSetupRoute()
        {
            if (!usePlatformNavigation
                || !preferPlatformsForPlungeSetup
                || !requireHeightAdvantageForPlunge
                || targetTransform == null
                || attacks == null
                || !attacks.CanLeapPlunge
                || HasPlungeHeightAdvantage())
            {
                return false;
            }

            if (targetSurfaceController != null && targetSurfaceController.IsOnCeiling)
                return false;

            return GetTargetDistance() <= plungeSetupMaxRange;
        }

        private bool HasPlungeHeightAdvantage()
        {
            if (targetTransform == null)
                return false;

            float selfY = motor != null ? motor.NavigationPosition.y : transform.position.y;
            return selfY - GetNavigationTargetPosition().y >= plungeHeightAdvantage;
        }

        private bool IsElevatedGroundTarget()
        {
            if (!usePlatformNavigation || !preferPlatformsForElevatedGroundTargets || targetTransform == null)
                return false;

            if (targetSurfaceController != null && targetSurfaceController.IsOnCeiling)
                return false;

            return GetNavigationTargetPosition().y - (motor != null ? motor.NavigationPosition.y : transform.position.y) >= elevatedGroundTargetHeight;
        }

        private void HandleDamaged(DamageInfo damageInfo)
        {
            if (selfHealth != null && !selfHealth.IsAlive)
                return;

            _recentDamageAccumulated += damageInfo.Amount;

            if (_recentDamageAccumulated < retreatDamageThreshold)
                return;

            _recentDamageAccumulated = 0f;
            retreatUntilTime = Time.time + retreatDuration;
            attacks?.CancelCurrentAction();

            if (damageInfo.Source != null)
                motor?.DashAwayFrom(damageInfo.Source.transform.position);
            else if (targetTransform != null)
                motor?.DashAwayFrom(targetTransform.position);

            currentDecision = HeroEnemyDecision.Retreat;
        }

        private void HandleDied(CombatHealth health)
        {
            currentDecision = HeroEnemyDecision.Dead;
            attacks?.CancelCurrentAction();
            motor?.StopAllMovement();
            enabled = false;
        }

        private void HandlePlungeCompleted(bool hitTarget)
        {
            if (!hitTarget)
                return;

            _postPlungePressureUntilTime = Time.time + postPlungePressureWindow;
            attacks?.ReduceBowCooldown(0.5f);
        }

        private void OnBowFired()
        {
            _lastBowFireTime = Time.time;
        }

        private HeroHealingPickup FindBestHealingPickup()
        {
            HeroHealingPickup[] pickups = FindObjectsByType<HeroHealingPickup>(FindObjectsInactive.Exclude);
            HeroHealingPickup bestPickup = null;
            float bestSqrDistance = healingSearchRadius * healingSearchRadius;
            Vector2 selfPosition = transform.position;

            foreach (HeroHealingPickup pickup in pickups)
            {
                if (pickup == null || !pickup.IsAvailable || !pickup.Matches(healingItemDefinition))
                    continue;

                float sqrDistance = ((Vector2)pickup.transform.position - selfPosition).sqrMagnitude;
                if (sqrDistance > bestSqrDistance)
                    continue;

                bestSqrDistance = sqrDistance;
                bestPickup = pickup;
            }

            return bestPickup;
        }

        private bool HasLineOfSight()
        {
            if (!requireLineOfSightForBow)
                return true;

            if (targetTransform == null)
                return false;

            Vector2 origin = lineOfSightOrigin != null ? lineOfSightOrigin.position : transform.position;
            Vector2 target = GetAimTargetPosition();
            RaycastHit2D hit = Physics2D.Linecast(origin, target, lineOfSightBlockers);
            return hit.collider == null;
        }

        private Vector2 GetAimTargetPosition()
        {
            if (targetAimPoint != null)
                return targetAimPoint.position;

            return targetTransform != null ? targetTransform.position : transform.position;
        }

        private Vector2 GetNavigationTargetPosition()
        {
            if (targetNavigationPoint != null)
                return targetNavigationPoint.position;

            if (targetTransform == null)
                return transform.position;

            return (Vector2)targetTransform.position + Vector2.up * targetNavigationVerticalOffset;
        }

        private float GetTargetDistance()
        {
            if (targetTransform == null)
                return float.PositiveInfinity;

            return Vector2.Distance(transform.position, targetTransform.position);
        }

        private bool ValidateRuntimeSetup()
        {
            bool valid = targetTransform != null && motor != null && attacks != null && selfHealth != null;

            if (!valid && !setupWarningLogged)
            {
                setupWarningLogged = true;
                Debug.LogWarning("[HeroEnemyBrain] Missing setup. Assign targetTransform, selfHealth, motor, and attacks on the prefab instance.", this);
            }

            return valid;
        }

        private void OnValidate()
        {
            heroLowHealthPercent = Mathf.Clamp01(heroLowHealthPercent);
            targetFinisherHealthPercent = Mathf.Clamp01(targetFinisherHealthPercent);
            healingSearchRadius = Mathf.Max(0f, healingSearchRadius);
            healingConsumeDistance = Mathf.Max(0f, healingConsumeDistance);
            decisionInterval = Mathf.Max(0.01f, decisionInterval);
            missingTargetIdleDelay = Mathf.Max(0f, missingTargetIdleDelay);
            strategyMinHoldTime = Mathf.Max(0f, strategyMinHoldTime);
            leapPlungeMinRange = Mathf.Max(0f, leapPlungeMinRange);
            leapPlungeMaxRange = Mathf.Max(leapPlungeMinRange, leapPlungeMaxRange);
            bowRange = Mathf.Max(0f, bowRange);
            targetNavigationVerticalOffset = Mathf.Min(0f, targetNavigationVerticalOffset);
            plungeHeightAdvantage = Mathf.Max(0f, plungeHeightAdvantage);
            plungeSetupMaxRange = Mathf.Max(leapPlungeMaxRange, plungeSetupMaxRange);
            plungePerchHorizontalWindow = Mathf.Max(0f, plungePerchHorizontalWindow);
            elevatedGroundTargetHeight = Mathf.Max(0f, elevatedGroundTargetHeight);
            retreatDuration = Mathf.Max(0f, retreatDuration);
            retreatSafeDistance = Mathf.Max(0f, retreatSafeDistance);
            retreatDamageThreshold = Mathf.Max(0f, retreatDamageThreshold);
            damageDecayRate = Mathf.Max(0f, damageDecayRate);
            blockedRouteEvadeDistance = Mathf.Max(0f, blockedRouteEvadeDistance);
            blockedRouteBowApproachRangePercent = Mathf.Clamp(blockedRouteBowApproachRangePercent, 0.1f, 1f);
            blockedRouteRetryDelay = Mathf.Max(0.01f, blockedRouteRetryDelay);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, leapPlungeMaxRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, bowRange);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, healingSearchRadius);
        }
    }
}
