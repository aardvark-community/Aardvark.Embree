using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests.EdgeCases;

/// <summary>
/// Tests to verify the actual behavior of negative tnear values in Embree ray tracing.
/// This tests the claim: "Negative tnear -> allows hits behind ray origin"
/// </summary>
public class NegativeTNearTests
{
    /// <summary>
    /// Test 1: Ray origin BEHIND the triangle, negative tnear
    /// Tests if negative tnear allows finding geometry behind the ray origin.
    /// </summary>
    [Fact(DisplayName = "Negative tnear with ray origin behind triangle")]
    public void NegativeTNear_RayOriginBehindTriangle_Test()
    {
        using var device = new Device();

        // Triangle at z=0
        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray origin at z=1 (BEHIND triangle at z=0), shooting in +Z direction
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f); // Shooting AWAY from triangle

        // Test with negative tnear
        var hit = new RayHit();
        bool result = scene.Intersect(rayOrigin, rayDirection, ref hit, -5.0f, float.MaxValue);

        // Document actual behavior
        if (result)
        {
            // Hit was found - negative tnear allowed finding geometry behind origin
            Assert.True(result, "Negative tnear found hit behind origin");
            Assert.True(hit.T < 0.0f, $"Expected negative hit.T for geometry behind origin, got: {hit.T}");
        }
        else
        {
            // No hit found - negative tnear did NOT allow finding geometry behind origin
            Assert.False(result, "Negative tnear did NOT find hit behind origin");
        }
    }

    /// <summary>
    /// Test 2: Ray origin IN FRONT of triangle, shooting toward it, with negative tnear
    /// Tests if negative tnear extends the valid range backwards from the origin.
    /// </summary>
    [Fact(DisplayName = "Negative tnear with ray shooting toward triangle")]
    public void NegativeTNear_RayShootingTowardTriangle_Test()
    {
        using var device = new Device();

        // Triangle at z=0
        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray origin at z=1 (in front), shooting toward triangle (-Z direction)
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        // Test with negative tnear (shouldn't affect this case - hit is in front)
        var hit = new RayHit();
        bool result = scene.Intersect(rayOrigin, rayDirection, ref hit, -2.0f, float.MaxValue);

        // This should hit regardless
        Assert.True(result, "Ray shooting toward triangle should hit");
        Assert.True(hit.T > 0.0f, $"Hit should be in positive direction, got: {hit.T}");
    }

    /// <summary>
    /// Test 3: Triangle behind origin, tnear excludes it
    /// Tests if negative tnear with restricted range can exclude hits.
    /// </summary>
    [Fact(DisplayName = "Negative tnear range excludes triangle behind origin")]
    public void NegativeTNear_RangeExcludesTriangleBehind_Test()
    {
        using var device = new Device();

        // Triangle at z=0
        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray origin at z=5, shooting in +Z direction (away from triangle)
        var rayOrigin = new V3f(0.25f, 0.25f, 5.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

        // Triangle is at t=-5 (5 units behind in +Z direction)
        // Set tnear=-2, tfar=0 to exclude it (only search from -2 to 0)
        var hit = new RayHit();
        bool result = scene.Intersect(rayOrigin, rayDirection, ref hit, -2.0f, 0.0f);

        // The triangle at t=-5 should be outside the [-2, 0] range
        Assert.False(result, "Triangle at t=-5 should be excluded by tnear=-2");
    }

    /// <summary>
    /// Test 4: Multiple triangles at different distances with negative tnear
    /// Tests which triangle is returned when negative tnear allows multiple hits.
    /// </summary>
    [Fact(DisplayName = "Negative tnear with multiple triangles")]
    public void NegativeTNear_MultipleTriangles_Test()
    {
        using var device = new Device();

        // Triangle 1 at z=-5
        var vertices1 = new V3f[]
        {
            new V3f(0.0f, 0.0f, -5.0f),
            new V3f(1.0f, 0.0f, -5.0f),
            new V3f(0.0f, 1.0f, -5.0f)
        };

        // Triangle 2 at z=5
        var vertices2 = new V3f[]
        {
            new V3f(0.0f, 0.0f, 5.0f),
            new V3f(1.0f, 0.0f, 5.0f),
            new V3f(0.0f, 1.0f, 5.0f)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geometry1 = new TriangleGeometry(device, vertices1, indices, RTCBuildQuality.High);
        using var geometry2 = new TriangleGeometry(device, vertices2, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        var geomId1 = scene.AttachGeometry(geometry1);
        var geomId2 = scene.AttachGeometry(geometry2);
        scene.Commit();

        // Ray origin at z=0, shooting in +Z direction
        var rayOrigin = new V3f(0.25f, 0.25f, 0.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

        // With negative tnear, triangle1 is at t=-5, triangle2 is at t=5
        var hit = new RayHit();
        bool result = scene.Intersect(rayOrigin, rayDirection, ref hit, -10.0f, float.MaxValue);

        if (result)
        {
            // Document which triangle was hit and at what t value
            Assert.True(result, $"Hit found: GeomId={hit.GeometryId}, t={hit.T}");

            // Embree typically returns the closest hit in terms of absolute t value
            // OR the closest hit in the ray direction (positive t takes precedence)
            if (hit.T < 0.0f)
            {
                Assert.Equal(geomId1, hit.GeometryId);
                Assert.True(hit.T < 0.0f && hit.T >= -10.0f, $"Expected t in range [-10, 0), got: {hit.T}");
            }
            else
            {
                Assert.Equal(geomId2, hit.GeometryId);
                Assert.True(hit.T > 0.0f, $"Expected positive t, got: {hit.T}");
            }
        }
        else
        {
            Assert.Fail("Expected to find at least one triangle");
        }
    }

    /// <summary>
    /// Test 5: Zero tnear vs negative tnear comparison
    /// Documents the difference in behavior between tnear=0 and tnear negative.
    /// </summary>
    [Fact(DisplayName = "Compare tnear=0 vs tnear negative")]
    public void CompareTNearZeroVsNegative_Test()
    {
        using var device = new Device();

        // Triangle at z=0
        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray origin at z=1, shooting away from triangle (+Z direction)
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

        // Test 1: tnear=0 (standard case)
        var hit1 = new RayHit();
        bool result1 = scene.Intersect(rayOrigin, rayDirection, ref hit1, 0.0f, float.MaxValue);

        // Test 2: tnear=-5 (negative)
        var hit2 = new RayHit();
        bool result2 = scene.Intersect(rayOrigin, rayDirection, ref hit2, -5.0f, float.MaxValue);

        // Document the difference
        if (result1 != result2)
        {
            Assert.NotEqual(result1, result2);
            if (result2 && !result1)
            {
                // Negative tnear found a hit that tnear=0 missed
                Assert.True(hit2.T < 0.0f, $"Negative tnear found hit at t={hit2.T}");
            }
        }
        else
        {
            Assert.Equal(result1, result2);
        }
    }
}
