using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests.EdgeCases;

/// <summary>
/// Tests to verify actual behavior when ray direction contains NaN values.
/// Referenced claim from AdvancedFeaturesExample.cs:343:
/// "NaN in direction -> tfar becomes NaN (miss)"
/// </summary>
public class NaNDirectionTests
{
    [Fact(DisplayName = "Ray with NaN in X direction component")]
    public void RayWithNaN_XDirection_VerifyBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var direction = new V3f(float.NaN, 0.0f, 1.0f);
        var hit = new RayHit();

        bool result = scene.Intersect(origin, direction, ref hit);

        // Document actual behavior
        Assert.False(result); // Verify if claim is correct: should be a miss

        // Note: hit.T is initialized to 0 by default, not float.MaxValue
        // The claim suggests tfar becomes NaN, but we observe it remains at initial value
        Assert.Equal(0.0f, hit.T);
    }

    [Fact(DisplayName = "Ray with NaN in Y direction component")]
    public void RayWithNaN_YDirection_VerifyBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var direction = new V3f(0.0f, float.NaN, 1.0f);
        var hit = new RayHit();

        bool result = scene.Intersect(origin, direction, ref hit);

        Assert.False(result);
    }

    [Fact(DisplayName = "Ray with NaN in Z direction component")]
    public void RayWithNaN_ZDirection_VerifyBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var direction = new V3f(0.0f, 0.0f, float.NaN);
        var hit = new RayHit();

        bool result = scene.Intersect(origin, direction, ref hit);

        Assert.False(result);
    }

    [Fact(DisplayName = "Ray with NaN in all direction components")]
    public void RayWithNaN_AllDirectionComponents_VerifyBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var direction = new V3f(float.NaN, float.NaN, float.NaN);
        var hit = new RayHit();

        bool result = scene.Intersect(origin, direction, ref hit);

        Assert.False(result);
    }

    [Fact(DisplayName = "Ray with NaN direction - verify tfar value after intersection")]
    public void RayWithNaN_Direction_VerifyTfarValue()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var direction = new V3f(float.NaN, 0.0f, 1.0f);
        var hit = new RayHit { T = 999.0f }; // Set known initial value

        bool result = scene.Intersect(origin, direction, ref hit);

        // Test the specific claim from AdvancedFeaturesExample.cs:343:
        // "NaN in direction -> tfar becomes NaN (miss)"

        Assert.False(result); // Should be a miss

        // Check what actually happens to hit.T (tfar)
        // The claim says it "becomes NaN", but Embree actually leaves tfar unchanged
        // when the ray direction is invalid
        Assert.Equal(999.0f, hit.T); // tfar remains at its initial value
        Assert.False(float.IsNaN(hit.T)); // tfar does NOT become NaN
    }

    [Fact(DisplayName = "Valid ray for comparison - should hit triangle")]
    public void ValidRay_ShouldHit_ComparisonBaseline()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Same origin as NaN tests, but with valid direction
        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var direction = new V3f(0.0f, 0.0f, 1.0f);
        var hit = new RayHit();

        bool result = scene.Intersect(origin, direction, ref hit);

        // This should hit the triangle
        Assert.True(result);
        Assert.True(hit.T > 0.0f && hit.T < float.MaxValue);
        Assert.False(float.IsNaN(hit.T));
    }

    [Fact(DisplayName = "Occlusion query with NaN direction")]
    public void OcclusionQuery_NaNDirection_VerifyBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var direction = new V3f(float.NaN, 0.0f, 1.0f);

        bool isOccluded = scene.Occluded(origin, direction, 0.0f, float.MaxValue);

        // Document occlusion behavior with NaN direction
        Assert.False(isOccluded);
    }
}
