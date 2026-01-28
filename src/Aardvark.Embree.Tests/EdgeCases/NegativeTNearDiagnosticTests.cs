using Aardvark.Base;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.EdgeCases;

/// <summary>
/// Diagnostic tests with output to verify actual behavior of negative tnear values.
/// </summary>
public class NegativeTNearDiagnosticTests
{
    private readonly ITestOutputHelper _output;

    public NegativeTNearDiagnosticTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "DIAGNOSTIC: Negative tnear with ray origin behind triangle")]
    public void DiagnosticTest1_NegativeTNear_RayOriginBehindTriangle()
    {
        using var device = new Device();
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

        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

        _output.WriteLine("TEST 1: Ray behind triangle, shooting away");
        _output.WriteLine($"  Triangle at z=0");
        _output.WriteLine($"  Ray origin: {rayOrigin}");
        _output.WriteLine($"  Ray direction: {rayDirection}");
        _output.WriteLine($"  tnear=-5.0, tfar=MaxValue");

        var hit = new RayHit();
        bool result = scene.Intersect(rayOrigin, rayDirection, ref hit, -5.0f, float.MaxValue);

        _output.WriteLine($"  Result: {(result ? "HIT" : "MISS")}");
        if (result)
        {
            _output.WriteLine($"  Hit.T: {hit.T}");
            _output.WriteLine($"  CLAIM VERIFIED: Negative tnear DOES allow hits behind origin");
        }
        else
        {
            _output.WriteLine($"  CLAIM REJECTED: Negative tnear does NOT allow hits behind origin");
        }
    }

    [Fact(DisplayName = "DIAGNOSTIC: Multiple triangles with negative tnear")]
    public void DiagnosticTest2_MultipleTriangles()
    {
        using var device = new Device();

        var vertices1 = new V3f[]
        {
            new V3f(0.0f, 0.0f, -5.0f),
            new V3f(1.0f, 0.0f, -5.0f),
            new V3f(0.0f, 1.0f, -5.0f)
        };

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

        var rayOrigin = new V3f(0.25f, 0.25f, 0.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

        _output.WriteLine("TEST 2: Multiple triangles with negative tnear");
        _output.WriteLine($"  Triangle 1 at z=-5 (GeomId={geomId1})");
        _output.WriteLine($"  Triangle 2 at z=5 (GeomId={geomId2})");
        _output.WriteLine($"  Ray origin: {rayOrigin}");
        _output.WriteLine($"  Ray direction: {rayDirection}");
        _output.WriteLine($"  Expected t for triangle 1: -5");
        _output.WriteLine($"  Expected t for triangle 2: +5");
        _output.WriteLine($"  tnear=-10.0, tfar=MaxValue");

        var hit = new RayHit();
        bool result = scene.Intersect(rayOrigin, rayDirection, ref hit, -10.0f, float.MaxValue);

        _output.WriteLine($"  Result: {(result ? "HIT" : "MISS")}");
        if (result)
        {
            _output.WriteLine($"  Hit GeomId: {hit.GeometryId}");
            _output.WriteLine($"  Hit.T: {hit.T}");
            if (hit.T < 0.0f)
            {
                _output.WriteLine($"  Hit triangle BEHIND origin (t={hit.T})");
                _output.WriteLine($"  CLAIM VERIFIED: Negative tnear allows hits behind origin");
            }
            else
            {
                _output.WriteLine($"  Hit triangle IN FRONT of origin (t={hit.T})");
                _output.WriteLine($"  CLAIM PARTIALLY VERIFIED: Embree prefers forward hits over backward hits");
            }
        }
    }

    [Fact(DisplayName = "DIAGNOSTIC: Compare tnear=0 vs tnear negative")]
    public void DiagnosticTest3_CompareTNearZeroVsNegative()
    {
        using var device = new Device();
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

        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

        _output.WriteLine("TEST 3: Compare tnear=0 vs tnear=-5");
        _output.WriteLine($"  Triangle at z=0");
        _output.WriteLine($"  Ray origin: {rayOrigin} (1 unit in front)");
        _output.WriteLine($"  Ray direction: {rayDirection} (shooting away)");

        var hit1 = new RayHit();
        bool result1 = scene.Intersect(rayOrigin, rayDirection, ref hit1, 0.0f, float.MaxValue);
        _output.WriteLine($"  tnear=0: {(result1 ? $"HIT at t={hit1.T}" : "MISS")}");

        var hit2 = new RayHit();
        bool result2 = scene.Intersect(rayOrigin, rayDirection, ref hit2, -5.0f, float.MaxValue);
        _output.WriteLine($"  tnear=-5: {(result2 ? $"HIT at t={hit2.T}" : "MISS")}");

        if (result1 != result2)
        {
            _output.WriteLine($"  BEHAVIOR DIFFERENCE: tnear=-5 changes result");
            _output.WriteLine($"  CLAIM VERIFIED: Negative tnear DOES allow hits behind origin");
        }
        else if (!result1 && !result2)
        {
            _output.WriteLine($"  BOTH MISS: Negative tnear does NOT find hits behind origin");
            _output.WriteLine($"  CLAIM REJECTED");
        }
        else
        {
            _output.WriteLine($"  BOTH HIT: No difference (forward hit found in both cases)");
        }
    }
}
