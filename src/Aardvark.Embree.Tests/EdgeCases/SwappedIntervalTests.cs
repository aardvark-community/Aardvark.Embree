using Aardvark.Base;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.EdgeCases;

/// <summary>
/// Tests to verify behavior when tnear > tfar (swapped interval).
///
/// CLAIM TO TEST (from AdvancedFeaturesExample.cs line 47):
/// "tnear > tfar -> no intersection possible (empty interval)"
///
/// This test suite verifies the ACTUAL behavior of Embree when rays
/// have swapped intervals and documents any edge cases.
/// </summary>
public class SwappedIntervalTests
{
    private readonly ITestOutputHelper _output;

    public SwappedIntervalTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Tests basic case: ray pointing directly at triangle center with swapped interval.
    /// </summary>
    [Fact(DisplayName = "Ray with tnear > tfar returns no hit (basic case)")]
    public void SwappedInterval_BasicCase_ReturnsNoHit()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        // Ray pointing at triangle center from distance 5
        var origin = new V3f(0.5f, 0.5f, -5.0f);
        var direction = new V3f(0.0f, 0.0f, 1.0f);
        var hit = new RayHit();

        // Swapped interval: tnear=10, tfar=1
        // Expected hit would be at t=5, but interval [10, 1] is empty
        float tnear = 10.0f;
        float tfar = 1.0f;

        bool hasHit = scene.Intersect(origin, direction, ref hit, tnear, tfar);

        _output.WriteLine($"Origin: {origin}");
        _output.WriteLine($"Direction: {direction}");
        _output.WriteLine($"Interval: [{tnear}, {tfar}] (swapped)");
        _output.WriteLine($"HasHit: {hasHit}");
        if (hasHit)
        {
            _output.WriteLine($"Hit.T: {hit.T}");
            _output.WriteLine($"Hit.GeomID: {hit.GeometryId}");
        }

        Assert.False(hasHit, "Swapped interval should result in no hit");
    }

    /// <summary>
    /// Tests edge case: tnear equals tfar (zero-length interval).
    /// Per Embree docs: "The implementation makes no guarantees that primitives whose hit distance
    /// is exactly at (or very close to) tnear or tfar are hit or missed."
    /// In practice, Embree 4.4.0 returns a hit when t exactly equals tnear==tfar.
    /// </summary>
    [Fact(DisplayName = "Ray with tnear == tfar returns hit when intersection is exactly at that t value (observed behavior)", Skip = "Embree behavior varies by platform/configuration")]
    public void ZeroLengthInterval_ReturnsHitWhenExactMatch()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0.5f, 0.5f, -5.0f);
        var direction = new V3f(0.0f, 0.0f, 1.0f);
        var hit = new RayHit();

        // Zero-length interval at t=5 (where actual hit occurs)
        float tnear = 5.0f;
        float tfar = 5.0f;

        bool hasHit = scene.Intersect(origin, direction, ref hit, tnear, tfar);

        // Observed behavior in Embree 4.4.0 - not guaranteed by spec
        Assert.True(hasHit);
        Assert.Equal(5.0f, hit.T);
    }

    /// <summary>
    /// Tests comparison: normal interval vs swapped interval on same ray.
    /// </summary>
    [Fact(DisplayName = "Normal interval hits, swapped interval misses")]
    public void NormalVsSwapped_ComparisonTest()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0.5f, 0.5f, -5.0f);
        var direction = new V3f(0.0f, 0.0f, 1.0f);

        // Test 1: Normal interval [0, 10] should hit at t≈5
        var hit1 = new RayHit();
        bool hasHit1 = scene.Intersect(origin, direction, ref hit1, 0.0f, 10.0f);

        _output.WriteLine("=== Normal Interval [0, 10] ===");
        _output.WriteLine($"HasHit: {hasHit1}");
        if (hasHit1)
        {
            _output.WriteLine($"Hit.T: {hit1.T}");
        }

        // Test 2: Swapped interval [10, 0] should miss
        var hit2 = new RayHit();
        bool hasHit2 = scene.Intersect(origin, direction, ref hit2, 10.0f, 0.0f);

        _output.WriteLine("=== Swapped Interval [10, 0] ===");
        _output.WriteLine($"HasHit: {hasHit2}");
        if (hasHit2)
        {
            _output.WriteLine($"Hit.T: {hit2.T}");
        }

        Assert.True(hasHit1, "Normal interval should hit");
        Assert.False(hasHit2, "Swapped interval should miss");
    }

    /// <summary>
    /// Tests edge case: large swapped interval with multiple geometry.
    /// </summary>
    [Fact(DisplayName = "Swapped interval misses all geometry in multi-object scene")]
    public void SwappedInterval_MultipleGeometry_MissesAll()
    {
        using var device = new Device();
        using var scene = CreateMultiTriangleScene(device);

        var origin = new V3f(0.5f, 0.5f, -10.0f);
        var direction = new V3f(0.0f, 0.0f, 1.0f);
        var hit = new RayHit();

        // Swapped interval: should miss all three triangles at z=0, z=5, z=10
        float tnear = 100.0f;
        float tfar = 1.0f;

        bool hasHit = scene.Intersect(origin, direction, ref hit, tnear, tfar);

        _output.WriteLine($"Scene has 3 triangles at z=0, z=5, z=10");
        _output.WriteLine($"Ray origin z={origin.Z}, direction={direction}");
        _output.WriteLine($"Interval: [{tnear}, {tfar}] (swapped)");
        _output.WriteLine($"HasHit: {hasHit}");

        Assert.False(hasHit, "Swapped interval should miss all geometry");
    }

    /// <summary>
    /// Tests edge case: swapped interval with negative values.
    /// </summary>
    [Fact(DisplayName = "Swapped interval with negative values returns no hit")]
    public void SwappedInterval_NegativeValues_ReturnsNoHit()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0.5f, 0.5f, -5.0f);
        var direction = new V3f(0.0f, 0.0f, 1.0f);
        var hit = new RayHit();

        // Swapped interval with negative values: [-1, -10]
        // (This is both swapped AND behind the ray)
        float tnear = -1.0f;
        float tfar = -10.0f;

        bool hasHit = scene.Intersect(origin, direction, ref hit, tnear, tfar);

        _output.WriteLine($"Interval: [{tnear}, {tfar}] (swapped, negative)");
        _output.WriteLine($"HasHit: {hasHit}");

        Assert.False(hasHit, "Swapped negative interval should result in no hit");
    }

    /// <summary>
    /// Tests occlusion query with swapped interval.
    /// </summary>
    [Fact(DisplayName = "Occluded() with swapped interval returns false")]
    public void SwappedInterval_Occluded_ReturnsFalse()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0.5f, 0.5f, -5.0f);
        var direction = new V3f(0.0f, 0.0f, 1.0f);

        // Swapped interval for occlusion test
        float tnear = 10.0f;
        float tfar = 1.0f;

        bool isOccluded = scene.Occluded(origin, direction, tnear, tfar);

        _output.WriteLine($"Interval: [{tnear}, {tfar}] (swapped)");
        _output.WriteLine($"IsOccluded: {isOccluded}");

        Assert.False(isOccluded, "Swapped interval should not be occluded");
    }

    /// <summary>
    /// Tests extreme case: very large swapped values.
    /// </summary>
    [Fact(DisplayName = "Extremely large swapped interval returns no hit")]
    public void SwappedInterval_ExtremeValues_ReturnsNoHit()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0.5f, 0.5f, -5.0f);
        var direction = new V3f(0.0f, 0.0f, 1.0f);
        var hit = new RayHit();

        // Extremely large swapped interval
        float tnear = 1e10f;
        float tfar = -1e10f;

        bool hasHit = scene.Intersect(origin, direction, ref hit, tnear, tfar);

        _output.WriteLine($"Interval: [{tnear:E}, {tfar:E}] (extreme swapped)");
        _output.WriteLine($"HasHit: {hasHit}");

        Assert.False(hasHit, "Extreme swapped interval should result in no hit");
    }

    // Helper methods

    private static Scene CreateSimpleTriangleScene(Device device)
    {
        var vertices = new V3f[]
        {
            new(0.0f, 0.0f, 0.0f),
            new(1.0f, 0.0f, 0.0f),
            new(0.5f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        return scene;
    }

    private static Scene CreateMultiTriangleScene(Device device)
    {
        var scene = new Scene(device, RTCBuildQuality.High, false);

        // Three triangles at different z depths
        float[] zDepths = { 0.0f, 5.0f, 10.0f };

        foreach (var z in zDepths)
        {
            var vertices = new V3f[]
            {
                new V3f(0.0f, 0.0f, z),
                new V3f(1.0f, 0.0f, z),
                new V3f(0.5f, 1.0f, z)
            };
            var indices = new int[] { 0, 1, 2 };

            var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
            scene.AttachGeometry(geometry);
        }

        scene.Commit();
        return scene;
    }
}
