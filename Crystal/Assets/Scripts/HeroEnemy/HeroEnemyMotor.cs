using System.Collections;
using UnityEngine;

namespace Crystal.HeroEnemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class HeroEnemyMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private HeroPlatformNavigator platformNavigator;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 4.5f;
        [SerializeField] private float aggressiveMoveSpeedMultiplier = 1.25f;
        [SerializeField] private float retreatMoveSpeedMultiplier = 1.35f;
        [SerializeField] private float gravityScale = 3f;
        [SerializeField] private float movementDeadZone = 0.1f;

        [Header("Jump")]
        [Tooltip("Maximum upward velocity the hero can use. Platform jumps scale down from this instead of always using the full value.")]
        [SerializeField] private float jumpForce = 11f;
        [SerializeField] private float lineOfSightJumpCooldown = 1f;
        [SerializeField] private float retreatJumpChance = 0.35f;
        [SerializeField] private float retreatJumpCooldown = 0.9f;
        [SerializeField] private float shortHopGravityMultiplier = 2.5f;
        [SerializeField] private float shortHopDuration = 0.12f;

        [Header("Dash")]
        [SerializeField] private float dashSpeed = 11f;
        [SerializeField] private float dashDuration = 0.14f;
        [SerializeField] private float dashCooldown = 1.15f;

        [Header("Surface Checks")]
        [Tooltip("Uses the attached collider's bottom as the feet probe. This avoids prefab scale/child offset mistakes that can leave the hero thinking it is airborne.")]
        [SerializeField] private bool useColliderBottomGroundCheck = true;
        [SerializeField] private float colliderBottomGroundInset = 0.04f;
        [SerializeField] private float groundCheckRadius = 0.16f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask platformLayer;
        [SerializeField] private LayerMask obstacleLayer;
        [SerializeField] private Vector2 obstacleCheckOffset = new Vector2(0f, 0.2f);
        [SerializeField] private float obstacleCheckDistance = 0.8f;

        [Header("Platform Route Following")]
        [SerializeField] private float platformRouteMinGoalHeightDelta = 0.75f;
        [SerializeField] private float platformTakeoffArrivalDistance = 0.25f;
        [SerializeField] private float platformLandingHorizontalTolerance = 0.9f;
        [SerializeField] private float platformLandingVerticalTolerance = 0.35f;
        [SerializeField] private float failedPlatformJumpRepathDelay = 0.45f;
        [SerializeField] private float platformJumpCooldown = 0.2f;
        [SerializeField] private float platformRepathInterval = 0.2f;
        [SerializeField] private float platformGoalRepathDistance = 0.75f;
        [SerializeField] private float landingSettleTime = 0.08f;
        [SerializeField] private float airborneRouteHoldTime = 0.12f;

        [Header("Facing")]
        [SerializeField] private bool spriteFacesRightByDefault = true;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private readonly Collider2D[] groundProbeHits = new Collider2D[8];
        private bool aggressive;
        private float nextDashTime;
        private float dashUntilTime;
        private float activeDashDirection;
        private float nextLineOfSightJumpTime;
        private float nextRetreatJumpTime;
        private float nextPlatformJumpTime;
        private float nextPlatformRepathTime;
        private float platformJumpStartedTime;
        private float settleUntilTime;
        private HeroPlatformRoute activeRoute;
        private int activeRouteStepIndex;
        private bool platformJumpInProgress;
        private bool hasActiveRoute;
        private Vector2 activeRouteGoal;
        private HeroPlatformRoutePurpose activeRoutePurpose;
        private float activeMinPerchHeight;
        private float activePerchHorizontalWindow;
        private Vector2 expectedPlatformLandingPoint;
        private Collider2D expectedPlatformLandingCollider;

        public bool IsGrounded => CheckGrounded();
        public Vector2 NavigationPosition => GetGroundProbePosition();
        public Vector2 Velocity => EnsureBody() ? body.linearVelocity : Vector2.zero;

        public void SetAggressive(bool value)
        {
            aggressive = value;
        }

        public void MoveToward(Vector2 targetPosition)
        {
            float deltaX = targetPosition.x - transform.position.x;
            if (Mathf.Abs(deltaX) <= movementDeadZone)
            {
                StopHorizontal();
                return;
            }

            float direction = Mathf.Sign(deltaX);
            FaceDirection(direction);
            SetHorizontalVelocity(direction * GetMoveSpeed());
        }

        public bool MoveTowardUsingPlatforms(Vector2 targetPosition, bool forcePlatformRoute)
        {
            if (!forcePlatformRoute && !ShouldConsiderPlatformRoute(targetPosition))
                return false;

            return MoveAlongPlatformRoute(targetPosition, HeroPlatformRoutePurpose.ApproachGoal, 0f, 0f);
        }

        public bool MoveTowardPlungePerch(Vector2 targetPosition, float minHeightAdvantage, float horizontalWindow)
        {
            return MoveAlongPlatformRoute(
                targetPosition,
                HeroPlatformRoutePurpose.PlungePerch,
                minHeightAdvantage,
                horizontalWindow);
        }

        public void MoveAwayFrom(Vector2 threatPosition)
        {
            ClearPlatformRoute();
            float direction = GetHorizontalDirection(transform.position.x - threatPosition.x);
            FaceDirection(direction);
            SetHorizontalVelocity(direction * moveSpeed * retreatMoveSpeedMultiplier);
        }

        public void RetreatFrom(Vector2 threatPosition, bool allowJump)
        {
            ClearPlatformRoute();
            float direction = GetHorizontalDirection(transform.position.x - threatPosition.x);
            FaceDirection(direction);

            if (Time.time >= nextDashTime)
                Dash(direction);
            else
                SetHorizontalVelocity(direction * moveSpeed * retreatMoveSpeedMultiplier);

            if (allowJump && ShouldJumpDuringRetreat(direction))
                Jump();
        }

        public void JumpForLineOfSight(Vector2 targetPosition)
        {
            ClearPlatformRoute();
            MoveToward(targetPosition);

            if (!IsGrounded || Time.time < nextLineOfSightJumpTime)
                return;

            nextLineOfSightJumpTime = Time.time + lineOfSightJumpCooldown;
            JumpToHeight(Mathf.Max(0f, targetPosition.y - transform.position.y + 0.5f));
        }

        public void DashAwayFrom(Vector2 threatPosition)
        {
            ClearPlatformRoute();
            float direction = GetHorizontalDirection(transform.position.x - threatPosition.x);
            FaceDirection(direction);
            Dash(direction);
        }

        public void Jump()
        {
            if (!EnsureBody())
                return;

            body.gravityScale = gravityScale;
            body.linearVelocity = new Vector2(body.linearVelocity.x, jumpForce);
        }

        public void LaunchToward(Vector2 targetPosition, float horizontalSpeed, float upwardVelocity)
        {
            if (!EnsureBody())
                return;

            ClearPlatformRoute();
            float direction = GetHorizontalDirection(targetPosition.x - transform.position.x);
            FaceDirection(direction);
            body.gravityScale = gravityScale;
            body.linearVelocity = new Vector2(direction * Mathf.Abs(horizontalSpeed), Mathf.Abs(upwardVelocity));
        }

        public void Plunge(float downwardSpeed)
        {
            if (!EnsureBody())
                return;

            ClearPlatformRoute();
            body.gravityScale = gravityScale;
            body.linearVelocity = new Vector2(0f, -Mathf.Abs(downwardSpeed));
        }

        public void PlungeToward(float downwardSpeed, float horizontalBias)
        {
            if (!EnsureBody())
                return;

            ClearPlatformRoute();
            body.gravityScale = gravityScale;
            body.linearVelocity = new Vector2(horizontalBias, -Mathf.Abs(downwardSpeed));
        }

        public void JumpToHeight(float targetHeight)
        {
            if (!EnsureBody())
                return;

            float gravity = Physics2D.gravity.magnitude * gravityScale;
            float neededVelocity = Mathf.Sqrt(2f * gravity * Mathf.Max(0f, targetHeight));
            neededVelocity = Mathf.Min(neededVelocity, jumpForce);
            body.linearVelocity = new Vector2(body.linearVelocity.x, neededVelocity);
        }

        public void ShortHop(float targetHeight)
        {
            StartCoroutine(ShortHopCoroutine(targetHeight));
        }

        public void StopHorizontal()
        {
            if (!EnsureBody())
                return;

            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
        }

        public void StopAllMovement()
        {
            if (!EnsureBody())
                return;

            ClearPlatformRoute();
            body.linearVelocity = Vector2.zero;
        }

        public void FacePosition(Vector2 targetPosition)
        {
            FaceDirection(GetHorizontalDirection(targetPosition.x - transform.position.x));
        }

        private void Awake()
        {
            EnsureBody();
            bodyCollider = GetComponent<Collider2D>();

            if (platformNavigator == null)
                platformNavigator = GetComponent<HeroPlatformNavigator>();
        }

        private void Update()
        {
            if (!EnsureBody())
                return;

            if (platformJumpInProgress && IsGrounded && Time.time >= platformJumpStartedTime + airborneRouteHoldTime)
            {
                platformJumpInProgress = false;
                if (ReachedExpectedPlatformLanding())
                {
                    activeRouteStepIndex++;
                    hasActiveRoute = false;
                }
                else
                {
                    ClearPlatformRoute();
                    nextPlatformRepathTime = Time.time + failedPlatformJumpRepathDelay;
                }

                settleUntilTime = Time.time + landingSettleTime;
            }

            if (Time.time < dashUntilTime)
                body.linearVelocity = new Vector2(activeDashDirection * dashSpeed, body.linearVelocity.y);
        }

        private bool MoveAlongPlatformRoute(
            Vector2 goalPosition,
            HeroPlatformRoutePurpose purpose,
            float minPerchHeight,
            float perchHorizontalWindow)
        {
            if (platformNavigator == null)
                return false;

            if (platformJumpInProgress || !IsGrounded)
                return hasActiveRoute || Time.time < platformJumpStartedTime + airborneRouteHoldTime;

            if (Time.time < settleUntilTime)
            {
                StopHorizontal();
                return true;
            }

            if (!EnsurePlatformRoute(goalPosition, purpose, minPerchHeight, perchHorizontalWindow))
            {
                StopHorizontal();
                return false;
            }

            if (activeRoute == null || activeRouteStepIndex >= activeRoute.StepCount)
            {
                StopHorizontal();
                return true;
            }

            HeroPlatformJumpStep step = activeRoute.GetStep(activeRouteStepIndex);
            float horizontalDelta = step.TakeoffPoint.x - NavigationPosition.x;
            if (Mathf.Abs(horizontalDelta) > platformTakeoffArrivalDistance)
            {
                float direction = Mathf.Sign(horizontalDelta);
                FaceDirection(direction);
                SetHorizontalVelocity(direction * GetMoveSpeed());
                return true;
            }

            FaceDirection(step.LandingPoint.x - NavigationPosition.x);
            StopHorizontal();

            if (Time.time < nextPlatformJumpTime)
                return true;

            LaunchPlatformJump(step);
            return true;
        }

        private bool EnsurePlatformRoute(
            Vector2 goalPosition,
            HeroPlatformRoutePurpose purpose,
            float minPerchHeight,
            float perchHorizontalWindow)
        {
            bool sameRequest = hasActiveRoute
                && activeRoute != null
                && activeRoutePurpose == purpose
                && Mathf.Abs(activeMinPerchHeight - minPerchHeight) <= 0.01f
                && Mathf.Abs(activePerchHorizontalWindow - perchHorizontalWindow) <= 0.01f
                && Vector2.Distance(activeRouteGoal, goalPosition) <= platformGoalRepathDistance;

            if (sameRequest && activeRouteStepIndex < activeRoute.StepCount)
                return true;

            if (Time.time < nextPlatformRepathTime)
                return false;

            nextPlatformRepathTime = Time.time + platformRepathInterval;
            bool foundRoute = platformNavigator.TryFindRoute(
                NavigationPosition,
                goalPosition,
                groundLayer,
                platformLayer,
                obstacleLayer,
                gravityScale,
                jumpForce,
                purpose,
                minPerchHeight,
                perchHorizontalWindow,
                GetPlatformBodyClearanceRadius(),
                out HeroPlatformRoute route);

            if (!foundRoute || route == null || route.StepCount == 0)
            {
                ClearPlatformRoute();
                return false;
            }

            activeRoute = route;
            activeRouteStepIndex = 0;
            activeRouteGoal = goalPosition;
            activeRoutePurpose = purpose;
            activeMinPerchHeight = minPerchHeight;
            activePerchHorizontalWindow = perchHorizontalWindow;
            hasActiveRoute = true;
            return true;
        }

        private void LaunchPlatformJump(HeroPlatformJumpStep step)
        {
            if (!EnsureBody())
                return;

            Vector2 velocity = step.LaunchVelocity;
            FaceDirection(velocity.x);
            body.gravityScale = gravityScale;
            body.linearVelocity = velocity;
            platformJumpInProgress = true;
            hasActiveRoute = true;
            expectedPlatformLandingPoint = step.LandingPoint;
            expectedPlatformLandingCollider = step.TargetCollider;
            platformJumpStartedTime = Time.time;
            nextPlatformJumpTime = Time.time + platformJumpCooldown;
        }

        private float GetMoveSpeed()
        {
            return aggressive ? moveSpeed * aggressiveMoveSpeedMultiplier : moveSpeed;
        }

        private void SetHorizontalVelocity(float velocityX)
        {
            if (!EnsureBody())
                return;

            body.linearVelocity = new Vector2(velocityX, body.linearVelocity.y);
        }

        private void Dash(float direction)
        {
            if (!EnsureBody())
                return;

            activeDashDirection = direction;
            nextDashTime = Time.time + dashCooldown;
            dashUntilTime = Time.time + dashDuration;
            body.linearVelocity = new Vector2(direction * dashSpeed, body.linearVelocity.y);
        }

        private IEnumerator ShortHopCoroutine(float targetHeight)
        {
            JumpToHeight(targetHeight);
            float t = 0f;
            while (t < shortHopDuration && body != null && body.linearVelocity.y > 0f)
            {
                body.gravityScale = gravityScale * shortHopGravityMultiplier;
                t += Time.deltaTime;
                yield return null;
            }

            if (body != null)
                body.gravityScale = gravityScale;
        }

        private bool ShouldJumpDuringRetreat(float direction)
        {
            if (!IsGrounded || Time.time < nextRetreatJumpTime)
                return false;

            bool obstacleAhead = Physics2D.Raycast(
                (Vector2)transform.position + obstacleCheckOffset,
                Vector2.right * direction,
                obstacleCheckDistance,
                obstacleLayer);

            if (!obstacleAhead && Random.value > retreatJumpChance)
                return false;

            nextRetreatJumpTime = Time.time + retreatJumpCooldown;
            return true;
        }

        private int GroundSurfaceMask => groundLayer.value | platformLayer.value;

        private float GetPlatformBodyClearanceRadius()
        {
            Collider2D collider = GetBodyCollider();
            if (collider == null)
                return groundCheckRadius;

            return Mathf.Max(groundCheckRadius, collider.bounds.extents.x);
        }

        private bool EnsureBody()
        {
            if (body == null)
                body = GetComponent<Rigidbody2D>();

            if (body == null)
                return false;

            body.gravityScale = gravityScale;
            body.freezeRotation = true;
            return true;
        }

        private Collider2D GetBodyCollider()
        {
            if (bodyCollider == null)
                bodyCollider = GetComponent<Collider2D>();

            return bodyCollider;
        }

        private Vector2 GetGroundProbePosition()
        {
            Collider2D collider = GetBodyCollider();
            if (useColliderBottomGroundCheck && collider != null)
            {
                Bounds bounds = collider.bounds;
                return new Vector2(bounds.center.x, bounds.min.y + colliderBottomGroundInset);
            }

            if (groundCheck != null)
                return groundCheck.position;

            return transform.position;
        }

        private bool CheckGrounded()
        {
            int groundMask = GroundSurfaceMask;
            if (groundMask == 0)
                return false;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(groundMask);
            filter.useTriggers = false;

            int hitCount = Physics2D.OverlapCircle(GetGroundProbePosition(), groundCheckRadius, filter, groundProbeHits);
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = groundProbeHits[i];
                if (hit == null || IsOwnCollider(hit))
                    continue;

                return true;
            }

            return false;
        }

        private bool IsOwnCollider(Collider2D hit)
        {
            Collider2D collider = GetBodyCollider();
            if (hit == collider)
                return true;

            return hit.transform == transform || hit.transform.IsChildOf(transform);
        }

        private bool ReachedExpectedPlatformLanding()
        {
            Vector2 navigationPosition = NavigationPosition;
            bool nearExpectedPoint = Mathf.Abs(navigationPosition.x - expectedPlatformLandingPoint.x) <= platformLandingHorizontalTolerance
                && Mathf.Abs(navigationPosition.y - expectedPlatformLandingPoint.y) <= platformLandingVerticalTolerance;

            if (nearExpectedPoint)
                return true;

            if (expectedPlatformLandingCollider == null)
                return false;

            Bounds bounds = expectedPlatformLandingCollider.bounds;
            bool overExpectedCollider = navigationPosition.x >= bounds.min.x - platformLandingHorizontalTolerance
                && navigationPosition.x <= bounds.max.x + platformLandingHorizontalTolerance
                && navigationPosition.y >= bounds.max.y - platformLandingVerticalTolerance
                && navigationPosition.y <= bounds.max.y + platformLandingVerticalTolerance;

            return overExpectedCollider;
        }

        private bool ShouldConsiderPlatformRoute(Vector2 targetPosition)
        {
            if (platformNavigator == null)
                return false;

            float verticalDelta = targetPosition.y - NavigationPosition.y;
            if (verticalDelta >= platformRouteMinGoalHeightDelta)
                return true;

            float direction = GetHorizontalDirection(targetPosition.x - transform.position.x);
            float distance = Mathf.Min(Mathf.Abs(targetPosition.x - transform.position.x), obstacleCheckDistance);
            if (distance <= 0f)
                return false;

            return Physics2D.Raycast(
                (Vector2)transform.position + obstacleCheckOffset,
                Vector2.right * direction,
                distance,
                obstacleLayer);
        }

        private void ClearPlatformRoute()
        {
            hasActiveRoute = false;
            platformJumpInProgress = false;
            activeRoute = null;
            activeRouteStepIndex = 0;
        }

        private void FaceDirection(float direction)
        {
            if (visualRoot == null || Mathf.Abs(direction) <= movementDeadZone)
                return;

            Vector3 scale = visualRoot.localScale;
            float facingSign = spriteFacesRightByDefault ? Mathf.Sign(direction) : -Mathf.Sign(direction);
            scale.x = Mathf.Abs(scale.x) * facingSign;
            visualRoot.localScale = scale;
        }

        private float GetHorizontalDirection(float deltaX)
        {
            if (Mathf.Abs(deltaX) <= movementDeadZone)
                return GetFacingDirection();

            return Mathf.Sign(deltaX);
        }

        private float GetFacingDirection()
        {
            if (visualRoot == null)
                return 1f;

            bool facingLeft = (visualRoot.localScale.x < 0f) == spriteFacesRightByDefault;
            return facingLeft ? -1f : 1f;
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            aggressiveMoveSpeedMultiplier = Mathf.Max(0f, aggressiveMoveSpeedMultiplier);
            retreatMoveSpeedMultiplier = Mathf.Max(0f, retreatMoveSpeedMultiplier);
            gravityScale = Mathf.Max(0f, gravityScale);
            movementDeadZone = Mathf.Max(0f, movementDeadZone);
            jumpForce = Mathf.Max(0f, jumpForce);
            lineOfSightJumpCooldown = Mathf.Max(0f, lineOfSightJumpCooldown);
            retreatJumpChance = Mathf.Clamp01(retreatJumpChance);
            retreatJumpCooldown = Mathf.Max(0f, retreatJumpCooldown);
            shortHopGravityMultiplier = Mathf.Max(1f, shortHopGravityMultiplier);
            shortHopDuration = Mathf.Max(0f, shortHopDuration);
            dashSpeed = Mathf.Max(0f, dashSpeed);
            dashDuration = Mathf.Max(0f, dashDuration);
            dashCooldown = Mathf.Max(0f, dashCooldown);
            colliderBottomGroundInset = Mathf.Max(0f, colliderBottomGroundInset);
            groundCheckRadius = Mathf.Max(0.01f, groundCheckRadius);
            obstacleCheckDistance = Mathf.Max(0f, obstacleCheckDistance);
            platformRouteMinGoalHeightDelta = Mathf.Max(0f, platformRouteMinGoalHeightDelta);
            platformTakeoffArrivalDistance = Mathf.Max(0.01f, platformTakeoffArrivalDistance);
            platformLandingHorizontalTolerance = Mathf.Max(0.01f, platformLandingHorizontalTolerance);
            platformLandingVerticalTolerance = Mathf.Max(0.01f, platformLandingVerticalTolerance);
            failedPlatformJumpRepathDelay = Mathf.Max(0f, failedPlatformJumpRepathDelay);
            platformJumpCooldown = Mathf.Max(0f, platformJumpCooldown);
            platformRepathInterval = Mathf.Max(0.01f, platformRepathInterval);
            platformGoalRepathDistance = Mathf.Max(0.01f, platformGoalRepathDistance);
            landingSettleTime = Mathf.Max(0f, landingSettleTime);
            airborneRouteHoldTime = Mathf.Max(0f, airborneRouteHoldTime);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GetGroundProbePosition(), groundCheckRadius);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                (Vector2)transform.position + obstacleCheckOffset,
                (Vector2)transform.position + obstacleCheckOffset + Vector2.right * obstacleCheckDistance);

            if (activeRoute != null && activeRouteStepIndex < activeRoute.StepCount)
            {
                HeroPlatformJumpStep step = activeRoute.GetStep(activeRouteStepIndex);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(step.TakeoffPoint, 0.18f);
                Gizmos.DrawWireSphere(step.LandingPoint, 0.18f);
                Gizmos.DrawLine(step.TakeoffPoint, step.LandingPoint);
            }
        }
    }
}
