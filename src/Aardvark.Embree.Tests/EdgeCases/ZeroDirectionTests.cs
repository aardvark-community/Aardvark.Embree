using Aardvark.Base;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.EdgeCases;

/// <summary>
/// Tests to verify actual behavior when ray direction is zero vector.
/// Reference claim (AdvancedFeaturesExample.cs:44): "Zero direction -> undefined behavior (avoid!)"
/// </summary>
public class ZeroDirectionTests
{
    private readonly ITestOutputHelper _output;

    public ZeroDirectionTests(ITestOutputHelper output)
    {
        _output = output;
    }
    [Fact(DisplayName = "Zero direction ray: verify actual behavior")]
    public void ZeroDirectionRay_ActualBehavior_Documented()
    {
        using var device = new Device();

        // Create a simple triangle scene
        var vertices = new V3f[]
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray pointing at triangle center, but with ZERO direction
        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var zeroDirection = new V3f(0, 0, 0);

        var hit = new RayHit();
        bool result = false;
        Exception exception = null;

        try
        {
            result = scene.Intersect(origin, zeroDirection, ref hit);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Document actual behavior
        if (exception != null)
        {
            // Behavior: Crashed with exception
            string msg = $"ACTUAL BEHAVIOR: Crashed with {exception.GetType().Name}: {exception.Message}";
            _output.WriteLine(msg);
            Console.WriteLine(msg);
            Assert.Fail(msg);
        }
        else
        {
            // Behavior: Did not crash
            string behavior = result
                ? $"ACTUAL BEHAVIOR: Returned hit=TRUE, tfar={hit.T}, geomID={hit.GeometryId}, primID={hit.PrimitiveId}, normal={hit.Normal}"
                : $"ACTUAL BEHAVIOR: Returned hit=FALSE, tfar={hit.T}";

            _output.WriteLine(behavior);
            Console.WriteLine(behavior);
            // Test passes - behavior documented
            Assert.True(true);
        }
    }

    [Fact(DisplayName = "Zero direction ray with Occluded query")]
    public void ZeroDirectionRay_Occluded_ActualBehavior()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var zeroDirection = new V3f(0, 0, 0);

        bool result = false;
        Exception exception = null;

        try
        {
            result = scene.Occluded(origin, zeroDirection);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception != null)
        {
            string msg = $"ACTUAL BEHAVIOR (Occluded): Crashed with {exception.GetType().Name}: {exception.Message}";
            _output.WriteLine(msg);
            Console.WriteLine(msg);
            Assert.Fail(msg);
        }
        else
        {
            string behavior = $"ACTUAL BEHAVIOR (Occluded): Returned {result}";
            _output.WriteLine(behavior);
            Console.WriteLine(behavior);
            Assert.True(true);
        }
    }

    [Fact(DisplayName = "Nearly zero direction ray (epsilon-scale)")]
    public void NearlyZeroDirectionRay_EpsilonScale_ActualBehavior()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var nearlyZeroDirection = new V3f(1e-10f, 1e-10f, 1e-10f);

        var hit = new RayHit();
        bool result = false;
        Exception exception = null;

        try
        {
            result = scene.Intersect(origin, nearlyZeroDirection, ref hit);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception != null)
        {
            string msg = $"ACTUAL BEHAVIOR (nearly zero): Crashed with {exception.GetType().Name}: {exception.Message}";
            _output.WriteLine(msg);
            Console.WriteLine(msg);
            Assert.Fail(msg);
        }
        else
        {
            string behavior = result
                ? $"ACTUAL BEHAVIOR (nearly zero): hit=TRUE, tfar={hit.T}"
                : $"ACTUAL BEHAVIOR (nearly zero): hit=FALSE, tfar={hit.T}";
            _output.WriteLine(behavior);
            Console.WriteLine(behavior);
            Assert.True(true);
        }
    }
}
