using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for geometry update APIs (UpdateVertices, UpdateControlPoints, UpdatePoints, GetDataPointer).
/// </summary>
public class GeometryUpdateTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void TriangleGeometry_UpdateVertices_Memory_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial triangle
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Ray should hit at origin
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.25f, 0.25f, -1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
        Assert.True(hit.T > 0);

        // Update vertices - move triangle up in Z
        var newVertices = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(0, 1, 2)
        };
        geom.UpdateVertices(new System.ReadOnlyMemory<V3f>(newVertices));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit (verify geometry moved)
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(0.25f, 0.25f, 1), new V3f(0, 0, 1), ref hit2, 0.0f, float.MaxValue);
        Assert.True(intersected2);
        Assert.True(hit2.T > 0);
        Assert.True(hit2.T < 1.5f); // Should hit near Z=2, so distance from Z=1 is ~1
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void TriangleGeometry_UpdateVertices_Span_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial triangle
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Update vertices using Span - move triangle up in Z
        var newVertices = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(0, 1, 2)
        };
        geom.UpdateVertices(new System.ReadOnlySpan<V3f>(newVertices));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.25f, 0.25f, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void TriangleGeometry_GetVertexDataPointer_InPlaceModification_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial triangle
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Modify vertices in-place using pointer
        unsafe
        {
            var ptr = geom.GetVertexDataPointer();
            for (int i = 0; i < 3; i++)
            {
                ptr[i].Z = 2; // Move all vertices to Z=2
            }
        }

        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.25f, 0.25f, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void TriangleGeometry_UpdateWithoutSceneCommit_DoesNotTakeEffect(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial triangle
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Update vertices but don't commit scene
        var newVertices = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(0, 1, 2)
        };
        geom.UpdateVertices(new System.ReadOnlyMemory<V3f>(newVertices));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        // Note: scene.Commit() NOT called

        // Ray at Z=0 should still hit (update not visible)
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.25f, 0.25f, -1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);

        // Now commit scene - update should take effect
        scene.Commit();

        // Ray at Z=2 should hit (verify scene commit made update visible)
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(0.25f, 0.25f, 1), new V3f(0, 0, 1), ref hit2, 0.0f, float.MaxValue);
        Assert.True(intersected2);
        Assert.True(hit2.T < 1.5f); // Should hit near Z=2
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void QuadGeometry_UpdateVertices_Memory_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial quad
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2, 3 };

        using var geom = new QuadGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Ray should hit at origin
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.5f, 0.5f, -1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);

        // Update vertices - move quad up in Z
        var newVertices = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(1, 1, 2),
            new V3f(0, 1, 2)
        };
        geom.UpdateVertices(new System.ReadOnlyMemory<V3f>(newVertices));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit (verify geometry moved)
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(0.5f, 0.5f, 1), new V3f(0, 0, 1), ref hit2, 0.0f, float.MaxValue);
        Assert.True(intersected2);
        Assert.True(hit2.T < 1.5f); // Should hit near Z=2
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void QuadGeometry_UpdateVertices_Span_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial quad
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2, 3 };

        using var geom = new QuadGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Update vertices using Span
        var newVertices = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(1, 1, 2),
            new V3f(0, 1, 2)
        };
        geom.UpdateVertices(new System.ReadOnlySpan<V3f>(newVertices));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.5f, 0.5f, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void QuadGeometry_GetVertexDataPointer_InPlaceModification_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial quad
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2, 3 };

        using var geom = new QuadGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Modify vertices in-place using pointer
        unsafe
        {
            var ptr = geom.GetVertexDataPointer();
            for (int i = 0; i < 4; i++)
            {
                ptr[i].Z = 2;
            }
        }

        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.5f, 0.5f, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CurveGeometry_UpdateControlPoints_Memory_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial curve (linear round curve with 2 segments)
        var controlPoints = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 0, 0), 0.1f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f)
        };
        var indices = new uint[] { 0, 1 }; // 2 segments

        using var geom = new RoundLinearCurveGeometry(device, controlPoints, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Ray should hit curve at origin
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.5f, 0, -1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);

        // Update control points - move curve up in Z
        var newControlPoints = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 2), 0.1f),
            new CurveVertex(new V3f(1, 0, 2), 0.1f),
            new CurveVertex(new V3f(2, 0, 2), 0.1f)
        };
        geom.UpdateControlPoints(new System.ReadOnlyMemory<CurveVertex>(newControlPoints));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit (moved geometry)
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(0.5f, 0, 1), new V3f(0, 0, 1), ref hit2, 0.0f, float.MaxValue);
        Assert.True(intersected2);
        Assert.True(hit2.T < 1.5f);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CurveGeometry_UpdateControlPoints_Span_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial curve
        var controlPoints = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 0, 0), 0.1f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f)
        };
        var indices = new uint[] { 0, 1 };

        using var geom = new RoundLinearCurveGeometry(device, controlPoints, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Update control points using Span
        var newControlPoints = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 2), 0.1f),
            new CurveVertex(new V3f(1, 0, 2), 0.1f),
            new CurveVertex(new V3f(2, 0, 2), 0.1f)
        };
        geom.UpdateControlPoints(new System.ReadOnlySpan<CurveVertex>(newControlPoints));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.5f, 0, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CurveGeometry_GetControlPointDataPointer_InPlaceModification_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial curve
        var controlPoints = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 0, 0), 0.1f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f)
        };
        var indices = new uint[] { 0, 1 };

        using var geom = new RoundLinearCurveGeometry(device, controlPoints, indices, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Modify control points in-place using pointer
        unsafe
        {
            var ptr = geom.GetControlPointDataPointer();
            for (int i = 0; i < 3; i++)
            {
                ptr[i].Position.Z = 2;
            }
        }

        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.5f, 0, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SpherePointGeometry_UpdatePoints_Memory_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial sphere points
        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var geom = new SpherePointGeometry(device, points, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Ray should hit sphere at origin
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0, 0, -1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);

        // Update points - move sphere up in Z
        var newPoints = new Point[]
        {
            new Point(new V3f(0, 0, 2), 0.5f)
        };
        geom.UpdatePoints(new System.ReadOnlyMemory<Point>(newPoints));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit (moved geometry)
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, 1), ref hit2, 0.0f, float.MaxValue);
        Assert.True(intersected2);
        Assert.True(hit2.T < 1.5f);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SpherePointGeometry_UpdatePoints_Span_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial sphere points
        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var geom = new SpherePointGeometry(device, points, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Update points using Span
        var newPoints = new Point[]
        {
            new Point(new V3f(0, 0, 2), 0.5f)
        };
        geom.UpdatePoints(new System.ReadOnlySpan<Point>(newPoints));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SpherePointGeometry_GetPointDataPointer_InPlaceModification_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial sphere points
        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var geom = new SpherePointGeometry(device, points, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Modify points in-place using pointer
        unsafe
        {
            var ptr = geom.GetPointDataPointer();
            ptr[0].Position.Z = 2;
        }

        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscPointGeometry_UpdatePoints_Memory_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial disc points
        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var geom = new DiscPointGeometry(device, points, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Ray should hit disc at origin
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0, 0, -1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);

        // Update points - move disc up in Z
        var newPoints = new Point[]
        {
            new Point(new V3f(0, 0, 2), 0.5f)
        };
        geom.UpdatePoints(new System.ReadOnlyMemory<Point>(newPoints));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, 1), ref hit2, 0.0f, float.MaxValue);
        Assert.True(intersected2);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscPointGeometry_UpdatePoints_Span_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial disc points
        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var geom = new DiscPointGeometry(device, points, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Update points using Span
        var newPoints = new Point[]
        {
            new Point(new V3f(0, 0, 2), 0.5f)
        };
        geom.UpdatePoints(new System.ReadOnlySpan<Point>(newPoints));
        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscPointGeometry_GetPointDataPointer_InPlaceModification_Works(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create initial disc points
        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var geom = new DiscPointGeometry(device, points, quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Modify points in-place using pointer
        unsafe
        {
            var ptr = geom.GetPointDataPointer();
            ptr[0].Position.Z = 2;
        }

        geom.UpdateBuffer(RTCBufferType.Vertex);
        geom.Commit();
        scene.Commit();

        // Ray at Z=2 should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue);
        Assert.True(intersected);
    }
}
