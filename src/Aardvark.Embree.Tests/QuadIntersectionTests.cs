using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

public class QuadIntersectionTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void QuadGeometry_CanIntersect(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a simple quad in the XY plane at Z=0
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray from above pointing down through center of quad
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect quad");
        Assert.InRange(hit.T, 0.9f, 1.1f); // Should hit at distance ~1
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void QuadGeometry_RayMisses(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a small quad centered at origin in XY plane
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray outside quad bounds should miss
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(5, 5, 1),
            new V3f(0, 0, -1),
            ref hit
        );

        Assert.False(intersected, "Ray outside quad bounds should miss");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void QuadGeometry_Occlusion(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a quad in the XY plane
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray should be occluded by quad
        bool occluded = scene.Occluded(
            rayOrigin: new V3f(0, 0, 1),
            rayDirection: new V3f(0, 0, -1)
        );

        Assert.True(occluded, "Ray should be occluded by quad");
    }

    [Fact]
    public void QuadGeometry_NoOcclusionWhenMissing()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create a quad in the XY plane
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray outside quad bounds should not be occluded
        bool occluded = scene.Occluded(
            rayOrigin: new V3f(5, 5, 1),
            rayDirection: new V3f(0, 0, -1)
        );

        Assert.False(occluded, "Ray outside quad bounds should not be occluded");
    }

    [Fact]
    public void QuadGeometry_IntersectMultipleQuads()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create three quads at different Z positions
        var vertices = new V3f[]
        {
            // Quad 1 at Z=0
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0),

            // Quad 2 at Z=2
            new V3f(-1, -1, 2),
            new V3f( 1, -1, 2),
            new V3f( 1,  1, 2),
            new V3f(-1,  1, 2),

            // Quad 3 at Z=4
            new V3f(-1, -1, 4),
            new V3f( 1, -1, 4),
            new V3f( 1,  1, 4),
            new V3f(-1,  1, 4)
        };

        var indices = new int[]
        {
            0, 1, 2, 3,    // Quad 1
            4, 5, 6, 7,    // Quad 2
            8, 9, 10, 11   // Quad 3
        };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray from above should hit first quad at Z=4
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(0, 0, 5),
            new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect quad");
        Assert.InRange(hit.T, 0.9f, 1.1f); // Distance to Z=4 quad
        Assert.Equal(2u, hit.PrimitiveId); // Third quad (index 2)
    }

    [Fact]
    public void QuadGeometry_IntersectFromBothSides()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create a quad in the XY plane at Z=0
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray from +Z side
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(0, 0, 1),
            new V3f(0, 0, -1),
            ref hit1
        );
        Assert.True(intersected1, "Ray from +Z should intersect");

        // Ray from -Z side
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            new V3f(0, 0, -1),
            new V3f(0, 0, 1),
            ref hit2
        );
        Assert.True(intersected2, "Ray from -Z should intersect");
    }

    [Fact]
    public void QuadGeometry_CornerIntersection()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create a quad in the XY plane
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray aimed at corner
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(0.9f, 0.9f, 1),
            new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray aimed at corner should intersect");
    }

    [Fact]
    public void QuadGeometry_EdgeIntersection()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create a quad in the XY plane
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray aimed at edge
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(1.0f, 0, 1),
            new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray aimed at edge should intersect");
    }

    [Fact]
    public void QuadGeometry_ObliqueAngle()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create a quad in the XY plane
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray at oblique angle
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(0, 0, 2),
            new V3f(0.1f, 0.1f, -1).Normalized,
            ref hit
        );

        Assert.True(intersected, "Ray at oblique angle should intersect");
    }

    [Fact]
    public void QuadGeometry_NonPlanarQuad()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create a non-planar quad (vertices not coplanar)
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0.5f),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray should still intersect (Embree handles non-planar quads)
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(0, 0, 1),
            new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect non-planar quad");
    }

    [Fact]
    public void QuadGeometry_RespectsTMinTMax()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create a quad at Z=0
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray with tmax too short should miss
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            rayOrigin: new V3f(0, 0, 10),
            rayDirection: new V3f(0, 0, -1),
            ref hit1,
            minT: 0,
            maxT: 5.0f  // Too short to reach quad at distance 10
        );
        Assert.False(intersected1, "Ray with short tmax should miss");

        // Ray with tmin too large should miss
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            rayOrigin: new V3f(0, 0, 10),
            rayDirection: new V3f(0, 0, -1),
            ref hit2,
            minT: 15.0f,  // Too far, quad is at distance 10
            maxT: float.MaxValue
        );
        Assert.False(intersected2, "Ray with large tmin should miss");

        // Ray with appropriate range should hit
        var hit3 = new RayHit();
        bool intersected3 = scene.Intersect(
            rayOrigin: new V3f(0, 0, 10),
            rayDirection: new V3f(0, 0, -1),
            ref hit3,
            minT: 0,
            maxT: 20.0f
        );
        Assert.True(intersected3, "Ray with appropriate range should hit");
    }

    [Fact]
    public void QuadGeometry_ParallelRayMisses()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Create a quad in the XY plane at Z=0
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f( 1, -1, 0),
            new V3f( 1,  1, 0),
            new V3f(-1,  1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var quad = new QuadGeometry(device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(quad);
        scene.Commit();

        // Ray parallel to quad plane should miss
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(0, 0, 1),
            new V3f(1, 0, 0),  // Parallel to XY plane
            ref hit
        );

        Assert.False(intersected, "Ray parallel to quad should miss");
    }
}
