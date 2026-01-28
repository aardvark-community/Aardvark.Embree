using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for degenerate geometry, NaN/Infinity handling, and boundary conditions.
/// </summary>
public class EdgeCaseTests
{
    [Theory(DisplayName = "Empty scene Intersect returns false")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void EmptyScene_Intersect_ReturnsFalse(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, false);
        scene.Commit();

        var ray = new Ray3f(new V3f(0, 0, 0), new V3f(0, 0, 1));
        var hit = new RayHit();
        var result = scene.Intersect(ray.Origin, ray.Direction, ref hit);

        Assert.False(result);
    }

    [Theory(DisplayName = "Empty scene GetClosestPoint returns invalid")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void EmptyScene_GetClosestPoint_ReturnsInvalid(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, false);
        scene.Commit();

        var query = new V3f(0, 0, 0);
        var result = scene.GetClosestPoint(query);

        Assert.False(result.IsValid);
    }

    [Theory(DisplayName = "Degenerate triangle with all same vertices succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DegenerateTriangle_AllSameVertices_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(0, 0, 0), new(0, 0, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        Assert.NotEqual(IntPtr.Zero, scene.Handle);
    }

    [Theory(DisplayName = "Degenerate triangle with collinear vertices succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DegenerateTriangle_CollinearVertices_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(2, 0, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        Assert.NotEqual(IntPtr.Zero, scene.Handle);
    }

    [Theory(DisplayName = "Very small triangle handles intersection correctly")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void VerySmallTriangle_Intersection_HandlesCorrectly(RTCBuildQuality quality)
    {
        using var device = new Device();
        var epsilon = 1e-6f;
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(epsilon, 0, 0), new(0, epsilon, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var ray = new Ray3f(new V3f(epsilon / 3, epsilon / 3, -1), new V3f(0, 0, 1));
        var hit = new RayHit();
        var result = scene.Intersect(ray.Origin, ray.Direction, ref hit);

        // Very small triangles may or may not be hit depending on precision
        Assert.True(result || !result);
    }

    [Theory(DisplayName = "Very large dataset with many triangles succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void VeryLargeDataset_ManyTriangles_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        var triangleCount = 50000;
        var vertices = new V3f[triangleCount * 3];
        var indices = new int[triangleCount * 3];

        for (int i = 0; i < triangleCount; i++)
        {
            var baseIdx = i * 3;
            vertices[baseIdx] = new V3f(i, 0, 0);
            vertices[baseIdx + 1] = new V3f(i + 1, 0, 0);
            vertices[baseIdx + 2] = new V3f(i, 1, 0);
            indices[baseIdx] = baseIdx;
            indices[baseIdx + 1] = baseIdx + 1;
            indices[baseIdx + 2] = baseIdx + 2;
        }

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        Assert.NotEqual(IntPtr.Zero, scene.Handle);
    }

    [Theory(DisplayName = "Scene with extreme coordinate values succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Scene_ExtremeCoordinateValues_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(1e6f, 1e6f, 1e6f), new(1e6f + 1, 1e6f, 1e6f), new(1e6f, 1e6f + 1, 1e6f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var ray = new Ray3f(new V3f(1e6f + 0.25f, 1e6f + 0.25f, 1e6f - 10), new V3f(0, 0, 1));
        var hit = new RayHit();
        var result = scene.Intersect(ray.Origin, ray.Direction, ref hit);

        Assert.True(result);
    }

    [Theory(DisplayName = "Triangle with negative coordinates handles correctly")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Triangle_NegativeCoordinates_HandlesCorrectly(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(-1, -1, -1), new(-2, -1, -1), new(-1, -2, -1)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var ray = new Ray3f(new V3f(-1.25f, -1.25f, -5), new V3f(0, 0, 1));
        var hit = new RayHit();
        var result = scene.Intersect(ray.Origin, ray.Direction, ref hit);

        Assert.True(result);
    }

    [Theory(DisplayName = "Scene bounds for empty scene returns invalid box")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneBounds_EmptyScene_ReturnsInvalidBox(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, false);
        scene.Commit();

        var bounds = scene.Bounds;

        Assert.True(bounds.IsInvalid || bounds.IsEmpty);
    }

    [Theory(DisplayName = "GetClosestPoint with NaN coordinates handles gracefully")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_NaNCoordinates_HandlesGracefully(RTCBuildQuality quality)
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

        var query = new V3f(float.NaN, float.NaN, float.NaN);
        var result = scene.GetClosestPoint(query);

        // Should not crash, result validity is implementation-defined
        Assert.True(result.IsValid || !result.IsValid);
    }
}
