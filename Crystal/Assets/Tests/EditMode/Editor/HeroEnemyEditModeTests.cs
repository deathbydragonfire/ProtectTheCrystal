using Crystal.HeroEnemy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class HeroEnemyEditModeTests
{
    [Test]
    public void CombatHealth_ClampsHealingAndRaisesDeathOnce()
    {
        GameObject owner = new GameObject("health-test");
        CombatHealth health = owner.AddComponent<CombatHealth>();
        int deathCount = 0;
        health.Died += _ => deathCount++;

        health.ApplyDamage(new DamageInfo(40f));
        Assert.AreEqual(60f, health.CurrentHealth);

        health.Heal(999f);
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth);

        health.ApplyDamage(new DamageInfo(999f));
        health.ApplyDamage(new DamageInfo(999f));

        Assert.AreEqual(0f, health.CurrentHealth);
        Assert.AreEqual(1, deathCount);
        Assert.IsFalse(health.IsAlive);

        Object.DestroyImmediate(owner);
    }

    [Test]
    public void HealingPickup_UsesDefinitionHealingAndDeactivatesOnConsume()
    {
        HealingItemDefinition definition = CreateHealingDefinition(35f);
        GameObject targetOwner = new GameObject("healing-target");
        CombatHealth health = targetOwner.AddComponent<CombatHealth>();
        HeroHealingPickup pickup = CreateHealingPickup("health-pickup", definition);

        health.ApplyDamage(new DamageInfo(10f));

        Assert.IsTrue(pickup.TryConsume(health));
        Assert.AreEqual(health.MaxHealth, health.CurrentHealth);
        Assert.IsFalse(pickup.gameObject.activeSelf);
        Assert.IsFalse(pickup.IsAvailable);

        Object.DestroyImmediate(pickup.gameObject);
        Object.DestroyImmediate(targetOwner);
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void HealingPickup_ConsumedPickupCannotBeConsumedAgain()
    {
        HealingItemDefinition definition = CreateHealingDefinition(35f);
        GameObject targetOwner = new GameObject("healing-target");
        CombatHealth health = targetOwner.AddComponent<CombatHealth>();
        HeroHealingPickup pickup = CreateHealingPickup("health-pickup", definition, false);

        health.ApplyDamage(new DamageInfo(50f));

        Assert.IsTrue(pickup.TryConsume(health));
        Assert.AreEqual(85f, health.CurrentHealth);
        Assert.IsFalse(pickup.IsAvailable);

        health.ApplyDamage(new DamageInfo(20f));

        Assert.IsFalse(pickup.TryConsume(health));
        Assert.AreEqual(65f, health.CurrentHealth);

        Object.DestroyImmediate(pickup.gameObject);
        Object.DestroyImmediate(targetOwner);
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void HealingPickupSpawnPoint_SpawnsOnceAndTracksPickup()
    {
        HealingItemDefinition definition = CreateHealingDefinition(20f);
        HeroHealingPickup pickupPrefab = CreateHealingPickup("pickup-prefab", definition);
        GameObject spawnOwner = new GameObject("spawn-point");
        spawnOwner.transform.position = new Vector3(2f, 3f, 0f);
        HeroHealingPickupSpawnPoint spawnPoint = spawnOwner.AddComponent<HeroHealingPickupSpawnPoint>();
        SerializedObject serializedSpawnPoint = new SerializedObject(spawnPoint);
        serializedSpawnPoint.FindProperty("pickupPrefab").objectReferenceValue = pickupPrefab;
        serializedSpawnPoint.FindProperty("spawnOnStart").boolValue = false;
        serializedSpawnPoint.ApplyModifiedPropertiesWithoutUndo();

        HeroHealingPickup firstPickup = spawnPoint.Spawn();
        HeroHealingPickup secondPickup = spawnPoint.Spawn();

        Assert.IsNotNull(firstPickup);
        Assert.AreSame(firstPickup, secondPickup);
        Assert.AreSame(firstPickup, spawnPoint.SpawnedPickup);
        Assert.AreEqual(spawnOwner.transform, firstPickup.transform.parent);
        Assert.That(Vector3.Distance(spawnOwner.transform.position, firstPickup.transform.position), Is.LessThan(0.0001f));
        Assert.AreEqual(pickupPrefab.name, firstPickup.name);

        Object.DestroyImmediate(spawnOwner);
        Object.DestroyImmediate(pickupPrefab.gameObject);
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void HealingPickupSpawner_SpawnNowKeepsOnlyOneActivePickup()
    {
        HealingItemDefinition definition = CreateHealingDefinition(20f);
        HeroHealingPickup pickupPrefab = CreateHealingPickup("pickup-prefab", definition);
        GameObject spawnerOwner = new GameObject("pickup-spawner");
        HeroHealingPickupSpawner spawner = spawnerOwner.AddComponent<HeroHealingPickupSpawner>();
        GameObject spawnPointOwner = new GameObject("spawn-point");
        spawnPointOwner.transform.position = new Vector3(4f, 5f, 0f);
        SerializedObject serializedSpawner = new SerializedObject(spawner);
        serializedSpawner.FindProperty("pickupPrefab").objectReferenceValue = pickupPrefab;
        serializedSpawner.FindProperty("spawnDelayRange").vector2Value = new Vector2(15f, 35f);
        SerializedProperty spawnPointsProperty = serializedSpawner.FindProperty("spawnPoints");
        spawnPointsProperty.arraySize = 1;
        spawnPointsProperty.GetArrayElementAtIndex(0).objectReferenceValue = spawnPointOwner.transform;
        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

        HeroHealingPickup firstPickup = spawner.SpawnNow();
        HeroHealingPickup secondPickup = spawner.SpawnNow();

        Assert.IsNotNull(firstPickup);
        Assert.AreSame(firstPickup, secondPickup);
        Assert.AreSame(firstPickup, spawner.ActivePickup);
        Assert.AreEqual(spawnerOwner.transform, firstPickup.transform.parent);
        Assert.That(Vector3.Distance(spawnPointOwner.transform.position, firstPickup.transform.position), Is.LessThan(0.0001f));

        Object.DestroyImmediate(spawnPointOwner);
        Object.DestroyImmediate(spawnerOwner);
        Object.DestroyImmediate(pickupPrefab.gameObject);
        Object.DestroyImmediate(definition);
    }

    [Test]
    public void Decision_DamageInterruptsCurrentPriority()
    {
        HeroEnemyDecisionWeights weights = CreateDefaultWeights();
        HeroEnemyDecision decision = HeroEnemyDecisionEvaluator.Evaluate(new HeroEnemyDecisionInput
        {
            WasDamagedRecently = true,
            HeroLowHealth = true,
            HealingAvailable = true,
            TargetLowHealth = true,
            TargetOnGround = true,
            InLeapRange = true,
            CanLeapPlunge = true
        }, weights);

        Object.DestroyImmediate(weights);
        Assert.AreEqual(HeroEnemyDecision.Retreat, decision);
    }

    [Test]
    public void Decision_HealingWinsOverFinisherAggression()
    {
        HeroEnemyDecisionWeights weights = CreateDefaultWeights();
        HeroEnemyDecision decision = HeroEnemyDecisionEvaluator.Evaluate(new HeroEnemyDecisionInput
        {
            HeroLowHealth = true,
            HealingAvailable = true,
            TargetLowHealth = true,
            TargetOnGround = true,
            InLeapRange = true,
            CanLeapPlunge = true
        }, weights);

        Object.DestroyImmediate(weights);
        Assert.AreEqual(HeroEnemyDecision.SeekHealing, decision);
    }

    [Test]
    public void Decision_GroundedTargetChoosesPlungeAttack()
    {
        HeroEnemyDecisionWeights weights = CreateLeapOnlyWeights();
        HeroEnemyDecision decision = HeroEnemyDecisionEvaluator.Evaluate(new HeroEnemyDecisionInput
        {
            TargetOnGround = true,
            InLeapRange = true,
            InBowRange = true,
            HasLineOfSight = true,
            HasHeightAdvantage = true,
            CanLeapPlunge = true,
            CanBow = true
        }, weights);

        Object.DestroyImmediate(weights);
        Assert.AreEqual(HeroEnemyDecision.LeapPlungeAttack, decision);
    }

    [Test]
    public void Decision_CeilingTargetUsesBowWhenLineOfSightExists()
    {
        HeroEnemyDecisionWeights weights = CreateDefaultWeights();
        HeroEnemyDecision decision = HeroEnemyDecisionEvaluator.Evaluate(new HeroEnemyDecisionInput
        {
            TargetOnCeiling = true,
            InBowRange = true,
            HasLineOfSight = true,
            CanBow = true
        }, weights);

        Object.DestroyImmediate(weights);
        Assert.AreEqual(HeroEnemyDecision.BowAttack, decision);
    }

    [Test]
    public void Decision_CeilingTargetJumpsForLineOfSightWhenBlocked()
    {
        HeroEnemyDecisionWeights weights = CreateDefaultWeights();
        HeroEnemyDecision decision = HeroEnemyDecisionEvaluator.Evaluate(new HeroEnemyDecisionInput
        {
            TargetOnCeiling = true,
            InBowRange = true,
            HasLineOfSight = false,
            CanBow = true
        }, weights);

        Object.DestroyImmediate(weights);
        Assert.AreEqual(HeroEnemyDecision.JumpForLineOfSight, decision);
    }

    [Test]
    public void Decision_GroundedTargetUsesBowWhenPlungeHeightIsUnavailable()
    {
        HeroEnemyDecisionWeights weights = CreateDefaultWeights();
        HeroEnemyDecision decision = HeroEnemyDecisionEvaluator.Evaluate(new HeroEnemyDecisionInput
        {
            TargetOnGround = true,
            InLeapRange = true,
            InBowRange = true,
            HasLineOfSight = true,
            CanLeapPlunge = false,
            CanBow = true
        }, weights);

        Object.DestroyImmediate(weights);
        Assert.AreEqual(HeroEnemyDecision.BowAttack, decision);
    }

    [Test]
    public void PlatformJumpVelocity_ScalesBelowMaximumForReachableJump()
    {
        HeroPlatformJumpTuning tuning = new HeroPlatformJumpTuning(29.43f, 30f, 8f, 0.7f);

        bool reachable = HeroPlatformNavigator.TryCalculateJumpVelocity(
            Vector2.zero,
            new Vector2(3f, 1.5f),
            tuning,
            out Vector2 velocity,
            out float flightTime);

        Assert.IsTrue(reachable);
        Assert.That(velocity.y, Is.GreaterThan(0f));
        Assert.That(velocity.y, Is.LessThan(30f));
        Assert.That(Mathf.Abs(velocity.x), Is.LessThanOrEqualTo(8.001f));
        Assert.That(flightTime, Is.GreaterThan(0f));
    }

    [Test]
    public void PlatformJumpVelocity_RejectsUnrealisticLongJump()
    {
        HeroPlatformJumpTuning tuning = new HeroPlatformJumpTuning(29.43f, 10f, 4f, 0.7f);

        bool reachable = HeroPlatformNavigator.TryCalculateJumpVelocity(
            Vector2.zero,
            new Vector2(30f, 0f),
            tuning,
            out _,
            out _);

        Assert.IsFalse(reachable);
    }

    [Test]
    public void PlatformNavigator_UsesSteppingStoneRouteOverImpossibleDirectJump()
    {
        GameObject navigatorOwner = new GameObject("navigator-test");
        HeroPlatformNavigator navigator = navigatorOwner.AddComponent<HeroPlatformNavigator>();
        GameObject stepOne = CreateNavigationPlatform("step-one", new Vector2(4f, 1f), new Vector2(2f, 0.3f));
        GameObject stepTwo = CreateNavigationPlatform("step-two", new Vector2(8f, 2f), new Vector2(2f, 0.3f));
        GameObject perch = CreateNavigationPlatform("perch", new Vector2(11f, 3f), new Vector2(2f, 0.3f));

        Physics2D.SyncTransforms();

        bool found = navigator.TryFindRoute(
            Vector2.zero,
            new Vector2(11f, 3.65f),
            default,
            default,
            default,
            3f,
            12f,
            HeroPlatformRoutePurpose.ApproachGoal,
            0f,
            0f,
            out HeroPlatformRoute route);

        Assert.IsTrue(found);
        Assert.That(route.StepCount, Is.GreaterThanOrEqualTo(2));
        Assert.That(route.GetStep(0).LandingPoint.x, Is.LessThan(6f));

        Object.DestroyImmediate(perch);
        Object.DestroyImmediate(stepTwo);
        Object.DestroyImmediate(stepOne);
        Object.DestroyImmediate(navigatorOwner);
    }

    [Test]
    public void PlatformNavigator_ReplansFromMovedPlatformPositions()
    {
        GameObject navigatorOwner = new GameObject("navigator-repath-test");
        HeroPlatformNavigator navigator = navigatorOwner.AddComponent<HeroPlatformNavigator>();
        GameObject platform = CreateNavigationPlatform("moving-platform", new Vector2(3f, 1f), new Vector2(2f, 0.3f));

        Physics2D.SyncTransforms();
        Assert.IsTrue(navigator.TryFindRoute(
            Vector2.zero,
            new Vector2(3f, 1.65f),
            default,
            default,
            default,
            3f,
            12f,
            HeroPlatformRoutePurpose.ApproachGoal,
            0f,
            0f,
            out HeroPlatformRoute firstRoute));

        float firstLandingX = firstRoute.GetStep(0).LandingPoint.x;
        platform.transform.position = new Vector2(5f, 1f);
        Physics2D.SyncTransforms();

        Assert.IsTrue(navigator.TryFindRoute(
            Vector2.zero,
            new Vector2(5f, 1.65f),
            default,
            default,
            default,
            3f,
            12f,
            HeroPlatformRoutePurpose.ApproachGoal,
            0f,
            0f,
            out HeroPlatformRoute movedRoute));

        Assert.That(movedRoute.GetStep(0).LandingPoint.x, Is.GreaterThan(firstLandingX + 1f));

        Object.DestroyImmediate(platform);
        Object.DestroyImmediate(navigatorOwner);
    }

    [Test]
    public void PlatformNavigator_JumpsToRaisedPlatformFromSideInsteadOfUnderside()
    {
        GameObject navigatorOwner = new GameObject("navigator-underside-test");
        HeroPlatformNavigator navigator = navigatorOwner.AddComponent<HeroPlatformNavigator>();
        GameObject platform = CreateNavigationPlatform("overhead-platform", new Vector2(0f, 2f), new Vector2(4f, 0.4f));

        Physics2D.SyncTransforms();

        bool found = navigator.TryFindRoute(
            Vector2.zero,
            new Vector2(0f, 2.65f),
            default,
            default,
            1 << platform.layer,
            3f,
            16f,
            HeroPlatformRoutePurpose.ApproachGoal,
            0f,
            0f,
            0.75f,
            out HeroPlatformRoute route);

        Assert.IsTrue(found);
        HeroPlatformJumpStep firstStep = route.GetStep(0);
        Assert.That(Mathf.Abs(firstStep.TakeoffPoint.x), Is.GreaterThan(2.1f));
        Assert.That(Mathf.Abs(firstStep.LandingPoint.x), Is.GreaterThan(0.9f));

        Object.DestroyImmediate(platform);
        Object.DestroyImmediate(navigatorOwner);
    }

    [Test]
    public void ArrowProjectile_AlignsLocalUpToFlightDirection()
    {
        GameObject owner = new GameObject("arrow-test");
        owner.AddComponent<Rigidbody2D>();
        owner.AddComponent<BoxCollider2D>();
        HeroArrowProjectile projectile = owner.AddComponent<HeroArrowProjectile>();

        projectile.Fire(Vector2.right, 1f, null);

        Assert.That(Vector2.Dot(owner.transform.up, Vector2.right), Is.GreaterThan(0.99f));

        Object.DestroyImmediate(owner);
    }

    [Test]
    public void Motor_MoveTowardStopsWhenHorizontallyAligned()
    {
        GameObject owner = new GameObject("motor-test");
        Rigidbody2D body = owner.AddComponent<Rigidbody2D>();
        HeroEnemyMotor motor = owner.AddComponent<HeroEnemyMotor>();

        body.linearVelocity = new Vector2(3f, -2f);
        motor.MoveToward(new Vector2(owner.transform.position.x + 0.05f, owner.transform.position.y + 4f));

        Assert.That(motor.Velocity.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(motor.Velocity.y, Is.EqualTo(-2f).Within(0.0001f));

        Object.DestroyImmediate(owner);
    }

    [Test]
    public void Motor_NavigationPositionUsesScaledColliderBottom()
    {
        GameObject owner = new GameObject("scaled-motor-test");
        owner.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        owner.AddComponent<Rigidbody2D>();
        CapsuleCollider2D collider = owner.AddComponent<CapsuleCollider2D>();
        collider.offset = new Vector2(0f, -0.5f);
        collider.size = new Vector2(19f, 19f);
        HeroEnemyMotor motor = owner.AddComponent<HeroEnemyMotor>();

        Physics2D.SyncTransforms();

        float expectedProbeY = collider.bounds.min.y + 0.04f;
        Assert.That(motor.NavigationPosition.y, Is.EqualTo(expectedProbeY).Within(0.0001f));
        Assert.That(motor.NavigationPosition.y, Is.LessThan(owner.transform.position.y - 0.8f));

        Object.DestroyImmediate(owner);
    }

    private static HeroEnemyDecisionWeights CreateDefaultWeights()
    {
        return ScriptableObject.CreateInstance<HeroEnemyDecisionWeights>();
    }

    private static HeroEnemyDecisionWeights CreateLeapOnlyWeights()
    {
        HeroEnemyDecisionWeights weights = ScriptableObject.CreateInstance<HeroEnemyDecisionWeights>();
        weights.bowBaseScore = 0f;
        weights.recentBowPenalty = 0f;
        weights.movingBowBaseScore = 0f;
        weights.approachBaseScore = 0f;
        return weights;
    }

    private static GameObject CreateNavigationPlatform(string name, Vector2 position, Vector2 size)
    {
        GameObject platform = new GameObject(name);
        platform.transform.position = position;
        BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
        collider.size = size;
        platform.AddComponent<HeroNavigationPlatform>();
        return platform;
    }

    private static HealingItemDefinition CreateHealingDefinition(float healAmount)
    {
        HealingItemDefinition definition = ScriptableObject.CreateInstance<HealingItemDefinition>();
        SerializedObject serializedDefinition = new SerializedObject(definition);
        serializedDefinition.FindProperty("displayName").stringValue = "Health Kit";
        serializedDefinition.FindProperty("healAmount").floatValue = healAmount;
        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        return definition;
    }

    private static HeroHealingPickup CreateHealingPickup(string name, HealingItemDefinition definition, bool deactivateOnConsume = true)
    {
        GameObject owner = new GameObject(name);
        owner.AddComponent<CircleCollider2D>();
        HeroHealingPickup pickup = owner.AddComponent<HeroHealingPickup>();
        SerializedObject serializedPickup = new SerializedObject(pickup);
        serializedPickup.FindProperty("itemDefinition").objectReferenceValue = definition;
        serializedPickup.FindProperty("deactivateOnConsume").boolValue = deactivateOnConsume;
        serializedPickup.FindProperty("destroyOnConsume").boolValue = false;
        serializedPickup.ApplyModifiedPropertiesWithoutUndo();
        return pickup;
    }
}
