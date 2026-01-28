using Aardvark.Base;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.EdgeCases;

/// <summary>
/// Tests to verify the actual behavior of unnormalized ray directions in Embree.
///
/// Goal: Determine what ACTUALLY happens when ray direction is unnormalized.
/// Claim (AdvancedFeaturesExample.cs:48): "Unnormalized direction -> tfar is in direction-vector units, not world units"
/// </summary>
public class UnnormalizedDirectionTests
{
    private readonly ITestOutputHelper _output;

    public UnnormalizedDirectionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "Unnormalized direction: verify tfar behavior with length 2.0")]
    public void UnnormalizedDirection_Length2_VerifyTfarBehavior()
    {
        using var device = new Device();

        // Simple triangle at Z=10 in world space
        var vertices = new V3f[]
        {
            new V3f(-5, -5, 10),
            new V3f(5, -5, 10),
            new V3f(0, 5, 10)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0, 0, 0);

        // Test 1: Normalized direction (length 1.0)
        var normalizedDir = new V3f(0, 0, 1); // length = 1.0
        var hitNormalized = new RayHit();
        bool hasHitNormalized = scene.Intersect(origin, normalizedDir, ref hitNormalized);

        _output.WriteLine("=== NORMALIZED DIRECTION (length 1.0) ===");
        _output.WriteLine($"Direction: {normalizedDir}, Length: {normalizedDir.Length:F6}");
        _output.WriteLine($"Hit: {hasHitNormalized}");
        if (hasHitNormalized)
        {
            _output.WriteLine($"tfar (hit.T): {hitNormalized.T:F6}");
            _output.WriteLine($"Expected world distance: 10.0");
            _output.WriteLine($"Actual hit position: origin + dir * T = {origin + normalizedDir * hitNormalized.T}");
        }
        _output.WriteLine("");

        // Test 2: Unnormalized direction (length 2.0) - same direction, doubled magnitude
        var unnormalizedDir2 = new V3f(0, 0, 2); // length = 2.0
        var hitUnnormalized2 = new RayHit();
        bool hasHitUnnormalized2 = scene.Intersect(origin, unnormalizedDir2, ref hitUnnormalized2);

        _output.WriteLine("=== UNNORMALIZED DIRECTION (length 2.0) ===");
        _output.WriteLine($"Direction: {unnormalizedDir2}, Length: {unnormalizedDir2.Length:F6}");
        _output.WriteLine($"Hit: {hasHitUnnormalized2}");
        if (hasHitUnnormalized2)
        {
            _output.WriteLine($"tfar (hit.T): {hitUnnormalized2.T:F6}");
            _output.WriteLine($"Actual hit position: origin + dir * T = {origin + unnormalizedDir2 * hitUnnormalized2.T}");
            _output.WriteLine("");
            _output.WriteLine($"ANALYSIS:");
            _output.WriteLine($"  - If tfar is in 'direction-vector units': T should be ~5.0 (10 / 2)");
            _output.WriteLine($"  - If tfar is in 'world units': T should be ~10.0 (same as normalized)");
            _output.WriteLine($"  - Actual T value: {hitUnnormalized2.T:F6}");

            // Calculate what the T value means
            var worldDistance = (origin + unnormalizedDir2 * hitUnnormalized2.T - origin).Length;
            _output.WriteLine($"  - World distance to hit: {worldDistance:F6}");
        }
        _output.WriteLine("");

        Assert.True(hasHitNormalized, "Normalized ray should hit the triangle");
        Assert.True(hasHitUnnormalized2, "Unnormalized ray (length 2) should hit the triangle");
    }

    [Fact(DisplayName = "Unnormalized direction: verify tfar behavior with length 0.5")]
    public void UnnormalizedDirection_LengthHalf_VerifyTfarBehavior()
    {
        using var device = new Device();

        // Simple triangle at Z=10 in world space
        var vertices = new V3f[]
        {
            new V3f(-5, -5, 10),
            new V3f(5, -5, 10),
            new V3f(0, 5, 10)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0, 0, 0);

        // Test 1: Normalized direction (length 1.0)
        var normalizedDir = new V3f(0, 0, 1); // length = 1.0
        var hitNormalized = new RayHit();
        bool hasHitNormalized = scene.Intersect(origin, normalizedDir, ref hitNormalized);

        _output.WriteLine("=== NORMALIZED DIRECTION (length 1.0) ===");
        _output.WriteLine($"Direction: {normalizedDir}, Length: {normalizedDir.Length:F6}");
        _output.WriteLine($"Hit: {hasHitNormalized}");
        if (hasHitNormalized)
        {
            _output.WriteLine($"tfar (hit.T): {hitNormalized.T:F6}");
            _output.WriteLine($"Expected world distance: 10.0");
            _output.WriteLine($"Actual hit position: origin + dir * T = {origin + normalizedDir * hitNormalized.T}");
        }
        _output.WriteLine("");

        // Test 2: Unnormalized direction (length 0.5) - same direction, halved magnitude
        var unnormalizedDirHalf = new V3f(0, 0, 0.5f); // length = 0.5
        var hitUnnormalizedHalf = new RayHit();
        bool hasHitUnnormalizedHalf = scene.Intersect(origin, unnormalizedDirHalf, ref hitUnnormalizedHalf);

        _output.WriteLine("=== UNNORMALIZED DIRECTION (length 0.5) ===");
        _output.WriteLine($"Direction: {unnormalizedDirHalf}, Length: {unnormalizedDirHalf.Length:F6}");
        _output.WriteLine($"Hit: {hasHitUnnormalizedHalf}");
        if (hasHitUnnormalizedHalf)
        {
            _output.WriteLine($"tfar (hit.T): {hitUnnormalizedHalf.T:F6}");
            _output.WriteLine($"Actual hit position: origin + dir * T = {origin + unnormalizedDirHalf * hitUnnormalizedHalf.T}");
            _output.WriteLine("");
            _output.WriteLine($"ANALYSIS:");
            _output.WriteLine($"  - If tfar is in 'direction-vector units': T should be ~20.0 (10 / 0.5)");
            _output.WriteLine($"  - If tfar is in 'world units': T should be ~10.0 (same as normalized)");
            _output.WriteLine($"  - Actual T value: {hitUnnormalizedHalf.T:F6}");

            // Calculate what the T value means
            var worldDistance = (origin + unnormalizedDirHalf * hitUnnormalizedHalf.T - origin).Length;
            _output.WriteLine($"  - World distance to hit: {worldDistance:F6}");
        }
        _output.WriteLine("");

        Assert.True(hasHitNormalized, "Normalized ray should hit the triangle");
        Assert.True(hasHitUnnormalizedHalf, "Unnormalized ray (length 0.5) should hit the triangle");
    }

    [Fact(DisplayName = "Unnormalized direction: comprehensive comparison across multiple lengths")]
    public void UnnormalizedDirection_ComprehensiveComparison_MultipleDirectionLengths()
    {
        using var device = new Device();

        // Simple triangle at Z=10 in world space
        var vertices = new V3f[]
        {
            new V3f(-5, -5, 10),
            new V3f(5, -5, 10),
            new V3f(0, 5, 10)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0, 0, 0);

        _output.WriteLine("=== COMPREHENSIVE DIRECTION LENGTH TEST ===");
        _output.WriteLine("Testing rays with different direction lengths pointing at triangle at Z=10");
        _output.WriteLine("");
        _output.WriteLine($"{"Dir Length",-12} | {"tfar (T)",-12} | {"World Dist",-12} | {"T * DirLen",-12} | Hit Pos");
        _output.WriteLine(new string('-', 80));

        var directionLengths = new[] { 0.25f, 0.5f, 1.0f, 2.0f, 4.0f, 10.0f };

        foreach (var length in directionLengths)
        {
            var direction = new V3f(0, 0, length);
            var hit = new RayHit();
            bool hasHit = scene.Intersect(origin, direction, ref hit);

            if (hasHit)
            {
                var hitPos = origin + direction * hit.T;
                var worldDist = (hitPos - origin).Length;
                var tTimesLength = hit.T * length;

                _output.WriteLine($"{length,-12:F6} | {hit.T,-12:F6} | {worldDist,-12:F6} | {tTimesLength,-12:F6} | {hitPos}");
            }
            else
            {
                _output.WriteLine($"{length,-12:F6} | NO HIT");
            }
        }

        _output.WriteLine("");
        _output.WriteLine("CONCLUSION:");
        _output.WriteLine("If 'T * DirLen' is constant (~10.0), then tfar is in direction-vector units.");
        _output.WriteLine("If 'tfar (T)' is constant (~10.0), then tfar is in world units.");
        _output.WriteLine("If 'World Dist' is constant (~10.0), the hit position is always correct.");
    }

    [Fact(DisplayName = "Unnormalized direction: verify tfar with diagonal ray")]
    public void UnnormalizedDirection_DiagonalRay_VerifyTfarBehavior()
    {
        using var device = new Device();

        // Triangle at a diagonal position
        var vertices = new V3f[]
        {
            new V3f(8, 8, 8),
            new V3f(12, 8, 8),
            new V3f(10, 12, 8)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0, 0, 0);
        var targetPoint = new V3f(10, 10, 8);

        // Test 1: Normalized direction
        var normalizedDir = (targetPoint - origin).Normalized;
        var hitNormalized = new RayHit();
        bool hasHitNormalized = scene.Intersect(origin, normalizedDir, ref hitNormalized);

        _output.WriteLine("=== DIAGONAL RAY - NORMALIZED ===");
        _output.WriteLine($"Direction: {normalizedDir}, Length: {normalizedDir.Length:F6}");
        _output.WriteLine($"Target point: {targetPoint}");
        _output.WriteLine($"Expected world distance: {(targetPoint - origin).Length:F6}");
        if (hasHitNormalized)
        {
            _output.WriteLine($"tfar (hit.T): {hitNormalized.T:F6}");
            _output.WriteLine($"Hit position: {origin + normalizedDir * hitNormalized.T}");
        }
        _output.WriteLine("");

        // Test 2: Unnormalized direction (length ~2.0)
        var unnormalizedDir = (targetPoint - origin).Normalized * 2.0f;
        var hitUnnormalized = new RayHit();
        bool hasHitUnnormalized = scene.Intersect(origin, unnormalizedDir, ref hitUnnormalized);

        _output.WriteLine("=== DIAGONAL RAY - UNNORMALIZED (length ~2.0) ===");
        _output.WriteLine($"Direction: {unnormalizedDir}, Length: {unnormalizedDir.Length:F6}");
        _output.WriteLine($"Target point: {targetPoint}");
        if (hasHitUnnormalized)
        {
            _output.WriteLine($"tfar (hit.T): {hitUnnormalized.T:F6}");
            _output.WriteLine($"Hit position: {origin + unnormalizedDir * hitUnnormalized.T}");
            _output.WriteLine("");
            _output.WriteLine($"COMPARISON:");
            _output.WriteLine($"  Normalized T: {hitNormalized.T:F6}");
            _output.WriteLine($"  Unnormalized T: {hitUnnormalized.T:F6}");
            _output.WriteLine($"  Ratio (Normalized/Unnormalized): {hitNormalized.T / hitUnnormalized.T:F6}");
            _output.WriteLine($"  Direction length ratio: {unnormalizedDir.Length / normalizedDir.Length:F6}");
            _output.WriteLine("");
            _output.WriteLine($"  If ratios match, tfar is in direction-vector units.");
        }

        Assert.True(hasHitNormalized, "Normalized diagonal ray should hit");
        Assert.True(hasHitUnnormalized, "Unnormalized diagonal ray should hit");
    }
}
