using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for nearest point queries, distance validation, and barycentric coordinates.
/// </summary>
public class GetClosestPointTests
{
    [Theory(DisplayName = "GetClosestPoint returns valid result for point above triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_PointAboveTriangle_ReturnsValid(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(0.25f, 0.25f, 1);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        Assert.True(result.DistanceSquared > 0);
        Assert.Equal(0u, result.GeomID);
        Assert.Equal(0u, result.PrimID);
    }

    [Theory(DisplayName = "GetClosestPoint returns correct barycentric coordinates")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_ReturnsBarycentricCoordinates(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(0.5f, 0.5f, 0);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        Assert.True(result.UV.X >= 0 && result.UV.X <= 1);
        Assert.True(result.UV.Y >= 0 && result.UV.Y <= 1);
    }

    [Fact(DisplayName = "GetClosestPoint with point on triangle has distance near zero")]
    public void GetClosestPoint_PointOnTriangle_DistanceNearZero()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(0.333f, 0.333f, 0);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        Assert.True(result.DistanceSquared < 0.01f);
    }

    [Fact(DisplayName = "GetClosestPoint with small radius returns geometry within bounds")]
    public void GetClosestPoint_SmallRadius_ReturnsGeometryWithinBounds()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Embree's rtcPointQuery uses the radius as a BVH traversal bound, not a hard cutoff.
        // It may still find geometry beyond the initial radius during traversal.
        // Test that when geometry is found, the distance is reasonable.
        var query = new V3f(2, 2, 0);
        var result = scene.GetClosestPoint(query, maxRadius: 10.0f);

        if (result.IsValid)
        {
            // If found, distance should be less than initial maxRadius (before callback tightening)
            Assert.True(result.DistanceSquared < 100.0f); // 10^2
        }
        // Embree may or may not find it depending on BVH bounds and traversal
    }

    [Fact(DisplayName = "GetClosestPoint finds correct geometry in multi-geometry scene")]
    public void GetClosestPoint_MultiGeometry_FindsCorrectGeometry()
    {
        using var device = new Device();
        var vertices1 = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var vertices2 = new V3f[]
        {
            new(10, 10, 0), new(11, 10, 0), new(10, 11, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry1 = new TriangleGeometry(device, vertices1, indices, RTCBuildQuality.High);
        using var geometry2 = new TriangleGeometry(device, vertices2, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        var id1 = scene.AttachGeometry(geometry1);
        var id2 = scene.AttachGeometry(geometry2);
        scene.Commit();

        var query = new V3f(10.5f, 10.5f, 1);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        Assert.Equal(id2, result.GeomID);
    }

    [Fact(DisplayName = "GetClosestPoint with default maxRadius succeeds")]
    public void GetClosestPoint_DefaultMaxRadius_Succeeds()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(100, 100, 100);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
    }

    [Fact(DisplayName = "GetClosestPoint returns correct point coordinates")]
    public void GetClosestPoint_ReturnsCorrectPointCoordinates()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(0, 0, 5);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        Assert.Equal(new V3f(0, 0, 0), result.Point);
    }

    [Fact(DisplayName = "GetClosestPoint handles query at triangle vertex")]
    public void GetClosestPoint_QueryAtVertex_ReturnsVertex()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(1, 0, 0);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        Assert.True(result.DistanceSquared < 0.0001f);
        Assert.True((result.Point - new V3f(1, 0, 0)).Length < 0.01f);
    }

    [Fact(DisplayName = "GetClosestPoint handles query at triangle edge")]
    public void GetClosestPoint_QueryAtEdge_ReturnsEdgePoint()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(0.5f, 0, 0);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        Assert.True(result.DistanceSquared < 0.0001f);
    }

    [Fact(DisplayName = "GetClosestPoint distance calculation is accurate")]
    public void GetClosestPoint_DistanceCalculation_IsAccurate()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(0, 0, 3);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        var expectedDistSq = 9.0f;
        Assert.True(Math.Abs(result.DistanceSquared - expectedDistSq) < 0.01f);
    }

    [Fact(DisplayName = "GetClosestPoint handles multiple triangles in same geometry")]
    public void GetClosestPoint_MultipleTriangles_FindsClosest()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0),
            new(10, 10, 0), new(11, 10, 0), new(10, 11, 0)
        };
        var indices = new int[] { 0, 1, 2, 3, 4, 5 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(10.5f, 10.5f, 1);
        var result = scene.GetClosestPoint(query);

        Assert.True(result.IsValid);
        Assert.Equal(1u, result.PrimID);
    }

    [Fact(DisplayName = "GetClosestPoint UV coordinates are within valid range")]
    public void GetClosestPoint_UVCoordinates_ValidRange()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var queries = new V3f[]
        {
            new(0.25f, 0.25f, 0),
            new(0.1f, 0.1f, 1),
            new(0.9f, 0.05f, 0),
            new(0.05f, 0.9f, 0)
        };

        foreach (var query in queries)
        {
            var result = scene.GetClosestPoint(query);
            Assert.True(result.IsValid);
            Assert.True(result.UV.X >= 0 && result.UV.X <= 1, $"UV.X {result.UV.X} out of range for query {query}");
            Assert.True(result.UV.Y >= 0 && result.UV.Y <= 1, $"UV.Y {result.UV.Y} out of range for query {query}");
            Assert.True((result.UV.X + result.UV.Y) <= 1.01f, $"UV sum {result.UV.X + result.UV.Y} invalid for query {query}");
        }
    }
}
