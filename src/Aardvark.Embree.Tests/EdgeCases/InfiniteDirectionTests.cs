using Aardvark.Base;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Aardvark.Embree.Tests.EdgeCases;

/// <summary>
/// Tests to verify actual behavior when ray direction contains infinity.
///
/// Claim to test: "Infinite direction may produce hits at tfar=0" or undefined behavior.
/// Purpose: Document empirically what Embree actually does with infinite direction components.
/// </summary>
public class InfiniteDirectionTests
{
    private Scene CreateSimpleTriangleScene(Device device)
    {
        // Simple triangle at z=0 plane
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        return scene;
    }

    [Fact(DisplayName = "Ray with infinite X direction component")]
    public void RayWithInfiniteXDirection_IntersectBehavior()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0, 0, -5);
        var direction = new V3f(float.PositiveInfinity, 0, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        // Document actual behavior
        // Expected: Undefined behavior (NaN propagation or no hit)
        Assert.False(hasHit, "Ray with infinite X direction should not produce valid hit");
    }

    [Fact(DisplayName = "Ray with infinite Y direction component")]
    public void RayWithInfiniteYDirection_IntersectBehavior()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0, 0, -5);
        var direction = new V3f(0, float.PositiveInfinity, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        // Document actual behavior
        Assert.False(hasHit, "Ray with infinite Y direction should not produce valid hit");
    }

    [Fact(DisplayName = "Ray with infinite Z direction component")]
    public void RayWithInfiniteZDirection_IntersectBehavior()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0, 0, -5);
        var direction = new V3f(0, 0, float.PositiveInfinity);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        // Document actual behavior
        // Even though Z direction is infinite and ray points toward triangle,
        // intersection math should produce undefined results
        Assert.False(hasHit, "Ray with infinite Z direction should not produce valid hit");
    }

    [Fact(DisplayName = "Ray with all infinite direction components")]
    public void RayWithAllInfiniteDirections_IntersectBehavior()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0, 0, -5);
        var direction = new V3f(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        // Document actual behavior
        Assert.False(hasHit, "Ray with all infinite directions should not produce valid hit");
    }

    [Fact(DisplayName = "Ray with negative infinite direction component")]
    public void RayWithNegativeInfiniteDirection_IntersectBehavior()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0, 0, -5);
        var direction = new V3f(0, 0, float.NegativeInfinity);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        // Document actual behavior
        // Negative infinity pointing away from triangle
        Assert.False(hasHit, "Ray with negative infinite Z direction should not produce valid hit");
    }

    [Fact(DisplayName = "Ray with infinite origin and valid direction")]
    public void RayWithInfiniteOrigin_IntersectBehavior()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(float.PositiveInfinity, 0, -5);
        var direction = new V3f(0, 0, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        // Document actual behavior
        // Ray starts at infinity, should not hit geometry
        Assert.False(hasHit, "Ray with infinite origin should not produce valid hit");
    }

    [Fact(DisplayName = "Ray with infinite direction - check tfar value")]
    public void RayWithInfiniteDirection_CheckTfarValue()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0, 0, -5);
        var direction = new V3f(0, 0, float.PositiveInfinity);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        Assert.False(hasHit, $"Unexpected hit detected: T={hit.T}, GeomID={hit.GeometryId}, PrimID={hit.PrimitiveId}");
    }

    [Fact(DisplayName = "Ray with infinite direction - occlusion query")]
    public void RayWithInfiniteDirection_OcclusionQuery()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0, 0, -5);
        var direction = new V3f(0, 0, float.PositiveInfinity);

        bool isOccluded = scene.Occluded(origin, direction);

        // Document actual behavior for occlusion queries
        Assert.False(isOccluded, "Ray with infinite direction should not occlude in occlusion query");
    }

    [Fact(DisplayName = "Valid ray for baseline comparison")]
    public void ValidRay_BaselineComparison()
    {
        using var device = new Device();
        using var scene = CreateSimpleTriangleScene(device);

        var origin = new V3f(0, 0, -5);
        var direction = new V3f(0, 0, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        // This should definitely hit
        Assert.True(hasHit, "Valid ray should hit triangle");
        Assert.True(hit.T > 0, $"Valid hit should have positive T value, got {hit.T}");
        Assert.Equal(0u, hit.GeometryId);
        Assert.Equal(0u, hit.PrimitiveId);
    }
}
