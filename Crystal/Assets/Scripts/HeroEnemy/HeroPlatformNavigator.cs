using System.Collections.Generic;
using UnityEngine;

namespace Crystal.HeroEnemy
{
    public enum HeroPlatformRoutePurpose
    {
        ApproachGoal,
        PlungePerch
    }

    public readonly struct HeroPlatformJumpStep
    {
        public HeroPlatformJumpStep(Vector2 takeoffPoint, Vector2 landingPoint, Vector2 launchVelocity, float flightTime, Collider2D targetCollider)
        {
            TakeoffPoint = takeoffPoint;
            LandingPoint = landingPoint;
            LaunchVelocity = launchVelocity;
            FlightTime = flightTime;
            TargetCollider = targetCollider;
        }

        public Vector2 TakeoffPoint { get; }
        public Vector2 LandingPoint { get; }
        public Vector2 LaunchVelocity { get; }
        public float FlightTime { get; }
        public Collider2D TargetCollider { get; }
    }

    public sealed class HeroPlatformRoute
    {
        private readonly HeroPlatformJumpStep[] steps;

        public HeroPlatformRoute(HeroPlatformJumpStep[] steps, Vector2 finalPoint, float cost)
        {
            this.steps = steps;
            FinalPoint = finalPoint;
            Cost = cost;
        }

        public int StepCount => steps != null ? steps.Length : 0;
        public Vector2 FinalPoint { get; }
        public float Cost { get; }

        public HeroPlatformJumpStep GetStep(int index)
        {
            return steps[index];
        }
    }

    public readonly struct HeroPlatformJumpTuning
    {
        public HeroPlatformJumpTuning(float gravity, float maxVerticalVelocity, float maxHorizontalVelocity, float arcPadding)
        {
            Gravity = gravity;
            MaxVerticalVelocity = maxVerticalVelocity;
            MaxHorizontalVelocity = maxHorizontalVelocity;
            ArcPadding = arcPadding;
        }

        public float Gravity { get; }
        public float MaxVerticalVelocity { get; }
        public float MaxHorizontalVelocity { get; }
        public float ArcPadding { get; }
    }

    public sealed class HeroPlatformNavigator : MonoBehaviour
    {
        [Header("Platform Discovery")]
        [SerializeField] private bool searchGroundLayerForTaggedPlatforms = true;
        [SerializeField] private bool requirePlatformTag = true;
        [SerializeField] private bool includeAllAuthoredPlatforms = true;
        [SerializeField] private string platformTag = "Platform";
        [SerializeField] private float platformSearchRadius = 24f;
        [SerializeField] private float platformTopPadding = 0.08f;
        [SerializeField] private float fallbackLandingPadding = 0.35f;

        [Header("Route Shape")]
        [SerializeField] private int maxRouteHops = 8;
        [SerializeField] private float groundTakeoffRunDistance = 12f;
        [SerializeField] private float routeGoalRadius = 1.25f;
        [SerializeField] private float routeGoalHeightTolerance = 1.75f;
        [SerializeField] private float minRouteProgress = 0.25f;

        [Header("Jump Limits")]
        [SerializeField] private float maxJumpHorizontalDistance = 8f;
        [SerializeField] private float maxJumpHorizontalSpeed = 10f;
        [SerializeField] private float maxDropHeight = 5f;
        [SerializeField] private float jumpArcPadding = 0.7f;
        [SerializeField] private float longJumpPenalty = 10f;

        [Header("Clearance")]
        [SerializeField] private float arcClearanceRadius = 0.25f;
        [SerializeField] private int arcClearanceSamples = 12;
        [SerializeField] private float targetLandingContactTolerance = 0.12f;

        private readonly List<PlatformSurface> surfaces = new List<PlatformSurface>(24);
        private readonly List<HeroPlatformJumpStep> rebuiltSteps = new List<HeroPlatformJumpStep>(8);

        public float PlatformSearchRadius => platformSearchRadius;
        public float MaxJumpHorizontalSpeed => maxJumpHorizontalSpeed;

        public bool TryFindRoute(
            Vector2 startPosition,
            Vector2 goalPosition,
            LayerMask groundLayer,
            LayerMask platformLayer,
            LayerMask obstacleLayer,
            float gravityScale,
            float maxVerticalVelocity,
            HeroPlatformRoutePurpose purpose,
            float minPerchHeight,
            float perchHorizontalWindow,
            out HeroPlatformRoute route)
        {
            return TryFindRoute(
                startPosition,
                goalPosition,
                groundLayer,
                platformLayer,
                obstacleLayer,
                gravityScale,
                maxVerticalVelocity,
                purpose,
                minPerchHeight,
                perchHorizontalWindow,
                arcClearanceRadius,
                out route);
        }

        public bool TryFindRoute(
            Vector2 startPosition,
            Vector2 goalPosition,
            LayerMask groundLayer,
            LayerMask platformLayer,
            LayerMask obstacleLayer,
            float gravityScale,
            float maxVerticalVelocity,
            HeroPlatformRoutePurpose purpose,
            float minPerchHeight,
            float perchHorizontalWindow,
            float bodyClearanceRadius,
            out HeroPlatformRoute route)
        {
            route = null;

            DiscoverSurfaces(startPosition, groundLayer, platformLayer);
            if (surfaces.Count == 0)
                return false;

            PlatformSurface start = CreateStartSurface(startPosition);
            List<PlatformSurface> nodes = new List<PlatformSurface>(surfaces.Count + 1) { start };
            nodes.AddRange(surfaces);

            int nodeCount = nodes.Count;
            float[] costs = new float[nodeCount];
            int[] previous = new int[nodeCount];
            int[] hops = new int[nodeCount];
            bool[] closed = new bool[nodeCount];
            HeroPlatformJumpStep[] previousSteps = new HeroPlatformJumpStep[nodeCount];

            for (int i = 0; i < nodeCount; i++)
            {
                costs[i] = float.PositiveInfinity;
                previous[i] = -1;
                hops[i] = int.MaxValue;
            }

            costs[0] = 0f;
            hops[0] = 0;

            HeroPlatformJumpTuning jumpTuning = new HeroPlatformJumpTuning(
                Mathf.Abs(Physics2D.gravity.y * gravityScale),
                maxVerticalVelocity,
                maxJumpHorizontalSpeed,
                jumpArcPadding);

            for (int i = 0; i < nodeCount; i++)
            {
                int current = FindOpenNodeWithLowestCost(costs, closed);
                if (current < 0)
                    break;

                closed[current] = true;
                if (hops[current] >= maxRouteHops)
                    continue;

                for (int next = 1; next < nodeCount; next++)
                {
                    if (next == current || closed[next])
                        continue;

                    PlatformSurface from = nodes[current];
                    PlatformSurface to = nodes[next];
                    if (!TryBuildJumpStep(from, to, goalPosition, jumpTuning, obstacleLayer, bodyClearanceRadius, out HeroPlatformJumpStep step, out float edgeCost))
                        continue;

                    float candidateCost = costs[current] + edgeCost;
                    if (candidateCost >= costs[next])
                        continue;

                    costs[next] = candidateCost;
                    previous[next] = current;
                    previousSteps[next] = step;
                    hops[next] = hops[current] + 1;
                }
            }

            int bestNode = -1;
            float bestCost = float.PositiveInfinity;
            for (int i = 1; i < nodeCount; i++)
            {
                if (float.IsPositiveInfinity(costs[i]) || !IsRouteDestination(nodes[i], startPosition, goalPosition, purpose, minPerchHeight, perchHorizontalWindow))
                    continue;

                Vector2 destinationPoint = nodes[i].PointClosestTo(goalPosition.x);
                float goalCost = Vector2.Distance(destinationPoint, goalPosition);
                float candidateCost = costs[i] + goalCost;
                if (candidateCost >= bestCost)
                    continue;

                bestCost = candidateCost;
                bestNode = i;
            }

            if (bestNode < 0)
                return false;

            rebuiltSteps.Clear();
            int node = bestNode;
            while (node > 0)
            {
                rebuiltSteps.Add(previousSteps[node]);
                node = previous[node];
            }

            rebuiltSteps.Reverse();
            if (rebuiltSteps.Count == 0)
                return false;

            HeroPlatformJumpStep[] steps = rebuiltSteps.ToArray();
            route = new HeroPlatformRoute(steps, steps[steps.Length - 1].LandingPoint, bestCost);
            return true;
        }

        public bool HasNavigationPlatforms(Vector2 startPosition, LayerMask groundLayer, LayerMask platformLayer)
        {
            DiscoverSurfaces(startPosition, groundLayer, platformLayer);
            return surfaces.Count > 0;
        }

        public static bool TryCalculateJumpVelocity(
            Vector2 start,
            Vector2 end,
            HeroPlatformJumpTuning tuning,
            out Vector2 velocity,
            out float flightTime)
        {
            velocity = Vector2.zero;
            flightTime = 0f;

            float gravity = Mathf.Abs(tuning.Gravity);
            float maxVerticalVelocity = Mathf.Abs(tuning.MaxVerticalVelocity);
            float maxHorizontalVelocity = Mathf.Abs(tuning.MaxHorizontalVelocity);
            if (gravity <= 0.001f || maxVerticalVelocity <= 0.001f || maxHorizontalVelocity <= 0.001f)
                return false;

            float deltaY = end.y - start.y;
            float peakDelta = Mathf.Max(tuning.ArcPadding, deltaY + tuning.ArcPadding, 0.05f);
            float minVerticalVelocity = Mathf.Sqrt(2f * gravity * peakDelta);
            if (minVerticalVelocity > maxVerticalVelocity)
                return false;

            float minFlightTime = CalculateFlightTime(minVerticalVelocity, deltaY, gravity);
            float maxFlightTime = CalculateFlightTime(maxVerticalVelocity, deltaY, gravity);
            if (minFlightTime <= 0f || maxFlightTime <= 0f)
                return false;

            float horizontalDistance = Mathf.Abs(end.x - start.x);
            if (horizontalDistance / maxFlightTime > maxHorizontalVelocity)
                return false;

            float chosenVerticalVelocity = minVerticalVelocity;
            if (horizontalDistance / minFlightTime > maxHorizontalVelocity)
            {
                float low = minVerticalVelocity;
                float high = maxVerticalVelocity;
                for (int i = 0; i < 8; i++)
                {
                    float mid = (low + high) * 0.5f;
                    float midTime = CalculateFlightTime(mid, deltaY, gravity);
                    if (horizontalDistance / midTime > maxHorizontalVelocity)
                        low = mid;
                    else
                        high = mid;
                }

                chosenVerticalVelocity = high;
            }

            flightTime = CalculateFlightTime(chosenVerticalVelocity, deltaY, gravity);
            if (flightTime <= 0f)
                return false;

            velocity = new Vector2((end.x - start.x) / flightTime, chosenVerticalVelocity);
            return Mathf.Abs(velocity.x) <= maxHorizontalVelocity + 0.001f;
        }

        private static float CalculateFlightTime(float verticalVelocity, float deltaY, float gravity)
        {
            float discriminant = verticalVelocity * verticalVelocity - 2f * gravity * deltaY;
            if (discriminant < 0f)
                return -1f;

            return (verticalVelocity + Mathf.Sqrt(discriminant)) / gravity;
        }

        private void DiscoverSurfaces(Vector2 center, LayerMask groundLayer, LayerMask platformLayer)
        {
            surfaces.Clear();
            int searchMask = platformLayer.value | (searchGroundLayerForTaggedPlatforms ? groundLayer.value : 0);
            if (searchMask != 0)
            {
                Collider2D[] overlaps = Physics2D.OverlapCircleAll(center, platformSearchRadius, searchMask);
                for (int i = 0; i < overlaps.Length; i++)
                    TryAddSurface(overlaps[i], platformLayer);
            }

            HeroNavigationPlatform[] authoredPlatforms = FindObjectsByType<HeroNavigationPlatform>(FindObjectsInactive.Exclude);
            for (int i = 0; i < authoredPlatforms.Length; i++)
            {
                HeroNavigationPlatform platform = authoredPlatforms[i];
                if (platform == null || !platform.isActiveAndEnabled)
                    continue;

                Collider2D platformCollider = platform.PlatformCollider;
                if (platformCollider == null)
                    continue;

                if (!includeAllAuthoredPlatforms && Vector2.Distance(center, platformCollider.bounds.center) > platformSearchRadius)
                    continue;

                TryAddSurface(platformCollider, platformLayer);
            }
        }

        private void TryAddSurface(Collider2D platformCollider, LayerMask platformLayer)
        {
            if (!TryCreateSurface(platformCollider, platformLayer, out PlatformSurface surface))
                return;

            for (int i = 0; i < surfaces.Count; i++)
            {
                if (surfaces[i].Collider == surface.Collider)
                    return;
            }

            surfaces.Add(surface);
        }

        private bool TryCreateSurface(Collider2D platformCollider, LayerMask platformLayer, out PlatformSurface surface)
        {
            surface = default;
            if (platformCollider == null || platformCollider.isTrigger)
                return false;

            HeroNavigationPlatform authoredPlatform = platformCollider.GetComponentInParent<HeroNavigationPlatform>();
            if (authoredPlatform == null && !IsFallbackNavigationPlatform(platformCollider, platformLayer))
                return false;

            Bounds bounds = platformCollider.bounds;
            if (bounds.size.x <= 0.05f || bounds.size.y <= 0.01f)
                return false;

            float landingPadding = authoredPlatform != null ? authoredPlatform.LandingPadding : fallbackLandingPadding;
            bool allowPlungePerch = authoredPlatform == null || authoredPlatform.AllowPlungePerch;
            surface = PlatformSurface.FromCollider(platformCollider, bounds, platformTopPadding, landingPadding, allowPlungePerch);
            return true;
        }

        private PlatformSurface CreateStartSurface(Vector2 startPosition)
        {
            for (int i = 0; i < surfaces.Count; i++)
            {
                if (surfaces[i].ContainsTopPoint(startPosition, 0.35f))
                    return surfaces[i];
            }

            return PlatformSurface.CreateStart(startPosition, groundTakeoffRunDistance);
        }

        private bool IsFallbackNavigationPlatform(Collider2D platformCollider, LayerMask platformLayer)
        {
            if (!requirePlatformTag || string.IsNullOrWhiteSpace(platformTag))
                return IsLayerInMask(platformCollider.gameObject.layer, platformLayer);

            Transform current = platformCollider.transform;
            while (current != null)
            {
                if (current.gameObject.tag == platformTag)
                    return true;

                current = current.parent;
            }

            return false;
        }

        private bool TryBuildJumpStep(
            PlatformSurface from,
            PlatformSurface to,
            Vector2 goalPosition,
            HeroPlatformJumpTuning jumpTuning,
            LayerMask obstacleLayer,
            float bodyClearanceRadius,
            out HeroPlatformJumpStep step,
            out float cost)
        {
            step = default;
            cost = float.PositiveInfinity;

            if (from.Collider != null && from.Collider == to.Collider)
                return false;

            float heightDelta = to.TopY - from.TopY;
            if (heightDelta < -maxDropHeight)
                return false;

            Vector2 bestTakeoff = default;
            Vector2 bestLanding = default;
            Vector2 bestVelocity = default;
            float bestFlightTime = 0f;

            JumpCandidate[] candidates = new JumpCandidate[8];
            int candidateCount = BuildJumpCandidates(from, to, goalPosition, Mathf.Max(bodyClearanceRadius, fallbackLandingPadding), candidates);

            for (int i = 0; i < candidateCount; i++)
            {
                float takeoffX = candidates[i].TakeoffX;
                float landingX = candidates[i].LandingX;
                Vector2 takeoff = new Vector2(takeoffX, from.TopY);
                Vector2 landing = new Vector2(landingX, to.TopY);
                float horizontalDistance = Mathf.Abs(landing.x - takeoff.x);

                if (horizontalDistance > maxJumpHorizontalDistance)
                    continue;

                if (!TryCalculateJumpVelocity(takeoff, landing, jumpTuning, out Vector2 velocity, out float flightTime))
                    continue;

                if (!IsJumpArcClear(takeoff, velocity, flightTime, jumpTuning.Gravity, obstacleLayer, from.Collider, to))
                    continue;

                float normalizedDistance = maxJumpHorizontalDistance > 0f ? horizontalDistance / maxJumpHorizontalDistance : 1f;
                float normalizedSpeed = jumpTuning.MaxHorizontalVelocity > 0f ? Mathf.Abs(velocity.x) / jumpTuning.MaxHorizontalVelocity : 1f;
                float jumpCost = 1f
                    + horizontalDistance
                    + Mathf.Max(0f, heightDelta) * 0.75f
                    + normalizedDistance * normalizedDistance * longJumpPenalty
                    + normalizedSpeed * normalizedSpeed * longJumpPenalty;

                if (jumpCost >= cost)
                    continue;

                cost = jumpCost;
                bestTakeoff = takeoff;
                bestLanding = landing;
                bestVelocity = velocity;
                bestFlightTime = flightTime;
            }

            if (float.IsPositiveInfinity(cost))
                return false;

            step = new HeroPlatformJumpStep(bestTakeoff, bestLanding, bestVelocity, bestFlightTime, to.Collider);
            return true;
        }

        private int BuildJumpCandidates(
            PlatformSurface from,
            PlatformSurface to,
            Vector2 goalPosition,
            float bodyClearanceRadius,
            JumpCandidate[] candidates)
        {
            int count = 0;
            float goalLanding = to.ClampX(goalPosition.x);
            count = AddJumpCandidate(candidates, count, from.ClampX(goalLanding), goalLanding);

            float fromCenterLanding = to.ClampX(from.Center.x);
            count = AddJumpCandidate(candidates, count, from.ClampX(fromCenterLanding), fromCenterLanding);

            count = AddJumpCandidate(candidates, count, from.ClampX(to.Center.x), to.Center.x);

            float preferredEdgeLanding = goalPosition.x >= from.Center.x ? to.MinX : to.MaxX;
            count = AddJumpCandidate(candidates, count, from.ClampX(preferredEdgeLanding), preferredEdgeLanding);

            if (to.TopY > from.TopY + 0.1f)
            {
                float leftLanding = to.ClampX(to.RawMinX + bodyClearanceRadius);
                float leftTakeoff = from.ClampX(to.RawMinX - bodyClearanceRadius);
                count = AddJumpCandidate(candidates, count, leftTakeoff, leftLanding);

                float rightLanding = to.ClampX(to.RawMaxX - bodyClearanceRadius);
                float rightTakeoff = from.ClampX(to.RawMaxX + bodyClearanceRadius);
                count = AddJumpCandidate(candidates, count, rightTakeoff, rightLanding);
            }

            return count;
        }

        private static int AddJumpCandidate(JumpCandidate[] candidates, int count, float takeoffX, float landingX)
        {
            for (int i = 0; i < count; i++)
            {
                if (Mathf.Abs(candidates[i].TakeoffX - takeoffX) <= 0.01f
                    && Mathf.Abs(candidates[i].LandingX - landingX) <= 0.01f)
                {
                    return count;
                }
            }

            if (count >= candidates.Length)
                return count;

            candidates[count] = new JumpCandidate(takeoffX, landingX);
            return count + 1;
        }

        private bool IsJumpArcClear(
            Vector2 takeoff,
            Vector2 launchVelocity,
            float flightTime,
            float gravity,
            LayerMask obstacleLayer,
            Collider2D sourceCollider,
            PlatformSurface targetSurface)
        {
            if (obstacleLayer.value == 0 || arcClearanceRadius <= 0f || arcClearanceSamples <= 1)
                return true;

            for (int i = 1; i < arcClearanceSamples; i++)
            {
                float t = flightTime * i / arcClearanceSamples;
                Vector2 position = takeoff + launchVelocity * t + Vector2.down * (0.5f * gravity * t * t);
                Collider2D[] hits = Physics2D.OverlapCircleAll(position, arcClearanceRadius, obstacleLayer);

                for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
                {
                    Collider2D hit = hits[hitIndex];
                    if (hit == null || hit.isTrigger || hit == sourceCollider)
                        continue;

                    if (hit == targetSurface.Collider && IsAllowedTargetPlatformContact(position, targetSurface))
                        continue;

                    return false;
                }
            }

            return true;
        }

        private bool IsAllowedTargetPlatformContact(Vector2 probePosition, PlatformSurface targetSurface)
        {
            return probePosition.y >= targetSurface.TopY - targetLandingContactTolerance;
        }

        private bool IsRouteDestination(
            PlatformSurface surface,
            Vector2 startPosition,
            Vector2 goalPosition,
            HeroPlatformRoutePurpose purpose,
            float minPerchHeight,
            float perchHorizontalWindow)
        {
            Vector2 destinationPoint = surface.PointClosestTo(goalPosition.x);

            if (purpose == HeroPlatformRoutePurpose.PlungePerch)
            {
                return surface.AllowPlungePerch
                    && surface.TopY >= goalPosition.y + minPerchHeight
                    && Mathf.Abs(destinationPoint.x - goalPosition.x) <= perchHorizontalWindow;
            }

            float currentDistance = Vector2.Distance(startPosition, goalPosition);
            float platformDistance = Vector2.Distance(destinationPoint, goalPosition);
            bool closeEnough = platformDistance <= routeGoalRadius;
            bool highEnough = surface.TopY >= goalPosition.y - routeGoalHeightTolerance;
            bool usefulProgress = currentDistance - platformDistance >= minRouteProgress;
            return usefulProgress && (closeEnough || highEnough);
        }

        private static int FindOpenNodeWithLowestCost(float[] costs, bool[] closed)
        {
            int best = -1;
            float bestCost = float.PositiveInfinity;
            for (int i = 0; i < costs.Length; i++)
            {
                if (closed[i] || costs[i] >= bestCost)
                    continue;

                best = i;
                bestCost = costs[i];
            }

            return best;
        }

        private static bool IsLayerInMask(int layer, LayerMask mask)
        {
            return (mask.value & (1 << layer)) != 0;
        }

        private void OnValidate()
        {
            if (platformTag == null)
                platformTag = string.Empty;

            platformSearchRadius = Mathf.Max(0f, platformSearchRadius);
            platformTopPadding = Mathf.Max(0f, platformTopPadding);
            fallbackLandingPadding = Mathf.Max(0f, fallbackLandingPadding);
            maxRouteHops = Mathf.Max(1, maxRouteHops);
            groundTakeoffRunDistance = Mathf.Max(0f, groundTakeoffRunDistance);
            routeGoalRadius = Mathf.Max(0.1f, routeGoalRadius);
            routeGoalHeightTolerance = Mathf.Max(0f, routeGoalHeightTolerance);
            minRouteProgress = Mathf.Max(0f, minRouteProgress);
            maxJumpHorizontalDistance = Mathf.Max(0.1f, maxJumpHorizontalDistance);
            maxJumpHorizontalSpeed = Mathf.Max(0.1f, maxJumpHorizontalSpeed);
            maxDropHeight = Mathf.Max(0f, maxDropHeight);
            jumpArcPadding = Mathf.Max(0.05f, jumpArcPadding);
            longJumpPenalty = Mathf.Max(0f, longJumpPenalty);
            arcClearanceRadius = Mathf.Max(0f, arcClearanceRadius);
            arcClearanceSamples = Mathf.Clamp(arcClearanceSamples, 2, 24);
            targetLandingContactTolerance = Mathf.Max(0f, targetLandingContactTolerance);
        }

        private readonly struct JumpCandidate
        {
            public JumpCandidate(float takeoffX, float landingX)
            {
                TakeoffX = takeoffX;
                LandingX = landingX;
            }

            public float TakeoffX { get; }
            public float LandingX { get; }
        }

        private readonly struct PlatformSurface
        {
            private PlatformSurface(Collider2D platformCollider, float minX, float maxX, float rawMinX, float rawMaxX, float topY, float bottomY, bool allowPlungePerch)
            {
                Collider = platformCollider;
                MinX = minX;
                MaxX = maxX;
                RawMinX = rawMinX;
                RawMaxX = rawMaxX;
                TopY = topY;
                BottomY = bottomY;
                AllowPlungePerch = allowPlungePerch;
                Center = new Vector2((minX + maxX) * 0.5f, topY);
            }

            public Collider2D Collider { get; }
            public float MinX { get; }
            public float MaxX { get; }
            public float RawMinX { get; }
            public float RawMaxX { get; }
            public float TopY { get; }
            public float BottomY { get; }
            public bool AllowPlungePerch { get; }
            public Vector2 Center { get; }

            public static PlatformSurface CreateStart(Vector2 position, float takeoffRunDistance)
            {
                float minX = position.x - takeoffRunDistance;
                float maxX = position.x + takeoffRunDistance;
                return new PlatformSurface(null, minX, maxX, minX, maxX, position.y, position.y, false);
            }

            public static PlatformSurface FromCollider(Collider2D collider, Bounds bounds, float topPadding, float landingPadding, bool allowPlungePerch)
            {
                float minX = bounds.min.x + landingPadding;
                float maxX = bounds.max.x - landingPadding;
                if (minX > maxX)
                {
                    float centerX = bounds.center.x;
                    minX = centerX;
                    maxX = centerX;
                }

                return new PlatformSurface(collider, minX, maxX, bounds.min.x, bounds.max.x, bounds.max.y + topPadding, bounds.min.y, allowPlungePerch);
            }

            public float ClampX(float x)
            {
                return Mathf.Clamp(x, MinX, MaxX);
            }

            public Vector2 PointClosestTo(float x)
            {
                return new Vector2(ClampX(x), TopY);
            }

            public bool ContainsTopPoint(Vector2 point, float tolerance)
            {
                return point.x >= MinX - tolerance
                    && point.x <= MaxX + tolerance
                    && Mathf.Abs(point.y - TopY) <= tolerance;
            }
        }
    }
}
