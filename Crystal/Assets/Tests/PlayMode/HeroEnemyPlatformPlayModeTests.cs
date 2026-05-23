#if UNITY_INCLUDE_TESTS
using Crystal.HeroEnemy;
using NUnit.Framework;
using UnityEngine;

public sealed class HeroEnemyPlatformPlayModeTests
{
    [Test]
    public void PlatformNavigator_RoutesAcrossRuntimePlatforms()
    {
        GameObject navigatorOwner = new GameObject("playmode-navigator");
        HeroPlatformNavigator navigator = navigatorOwner.AddComponent<HeroPlatformNavigator>();
        GameObject stepOne = CreateNavigationPlatform("playmode-step-one", new Vector2(4f, 1f), new Vector2(2f, 0.3f));
        GameObject stepTwo = CreateNavigationPlatform("playmode-step-two", new Vector2(8f, 2f), new Vector2(2f, 0.3f));
        GameObject perch = CreateNavigationPlatform("playmode-perch", new Vector2(11f, 3f), new Vector2(2f, 0.3f));

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

        Object.DestroyImmediate(stepOne);
        Object.DestroyImmediate(stepTwo);
        Object.DestroyImmediate(perch);
        Object.DestroyImmediate(navigatorOwner);
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
}
#endif
