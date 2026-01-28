using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

public class GridGeometryTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateSimpleGrid(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a 3x3 grid
        var vertices = new V3f[9];
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            vertices[y * 3 + x] = new V3f(x, y, 0);
        }

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 3,  // stride equals width for contiguous grids
                width = 3,
                height = 3
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        Assert.NotEqual(IntPtr.Zero, gridGeom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_CanIntersect(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a 2x2 grid at Z=0
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0),
            new V3f(1, 1, 0)
        };

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 2,  // stride equals width for contiguous grids
                width = 2,
                height = 2
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        scene.AttachGeometry(gridGeom);
        scene.Commit();

        // Ray from above pointing down at center
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.5f, 0.5f, 1f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect grid");
        Assert.Equal(1.0f, hit.T, 2);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_LargeGrid(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a 10x10 grid
        var vertices = new V3f[100];
        for (int y = 0; y < 10; y++)
        for (int x = 0; x < 10; x++)
        {
            vertices[y * 10 + x] = new V3f(x * 0.1f, y * 0.1f, 0);
        }

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 10,  // stride equals width for contiguous grids
                width = 10,
                height = 10
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        Assert.NotEqual(IntPtr.Zero, gridGeom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_MultipleGrids(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create vertices for two separate grids
        var vertices = new V3f[8];

        // First grid (2x2)
        vertices[0] = new V3f(0, 0, 0);
        vertices[1] = new V3f(1, 0, 0);
        vertices[2] = new V3f(0, 1, 0);
        vertices[3] = new V3f(1, 1, 0);

        // Second grid (2x2)
        vertices[4] = new V3f(2, 0, 0);
        vertices[5] = new V3f(3, 0, 0);
        vertices[6] = new V3f(2, 1, 0);
        vertices[7] = new V3f(3, 1, 0);

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 2,  // stride equals width for contiguous grids
                width = 2,
                height = 2
            },
            new RTCGrid
            {
                startVertexID = 4,
                stride = 2,  // stride equals width for contiguous grids
                width = 2,
                height = 2
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        Assert.NotEqual(IntPtr.Zero, gridGeom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_NonPlanarSurface(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a curved surface (hemisphere-like)
        var vertices = new V3f[9];
        for (int y = 0; y < 3; y++)
        for (int x = 0; x < 3; x++)
        {
            float fx = (x - 1) * 0.5f;
            float fy = (y - 1) * 0.5f;
            float z = (float)Math.Sqrt(Math.Max(0, 1.0 - fx * fx - fy * fy));
            vertices[y * 3 + x] = new V3f(fx, fy, z);
        }

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 3,  // stride equals width for contiguous grids
                width = 3,
                height = 3
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        scene.AttachGeometry(gridGeom);
        scene.Commit();

        // Ray from above should intersect curved surface
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 2),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect curved grid");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_CanUpdate(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: true);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0),
            new V3f(1, 1, 0)
        };

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 2,  // stride equals width for contiguous grids
                width = 2,
                height = 2
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        scene.AttachGeometry(gridGeom);
        scene.Commit();

        // Initial intersection at Z=0
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(0.5f, 0.5f, 1),
            new V3f(0, 0, -1),
            ref hit1
        );
        Assert.True(intersected1);
        Assert.Equal(1.0f, hit1.T, 2);

        // Update vertices to Z=0.5
        var updatedVertices = new V3f[]
        {
            new V3f(0, 0, 0.5f),
            new V3f(1, 0, 0.5f),
            new V3f(0, 1, 0.5f),
            new V3f(1, 1, 0.5f)
        };

        gridGeom.UpdateVertices((ReadOnlySpan<V3f>)updatedVertices);
        gridGeom.UpdateBuffer(RTCBufferType.Vertex);
        gridGeom.Commit();
        scene.Commit();

        // Updated intersection at Z=0.5
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            new V3f(0.5f, 0.5f, 1),
            new V3f(0, 0, -1),
            ref hit2
        );
        Assert.True(intersected2);
        Assert.Equal(0.5f, hit2.T, 2);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_WithBufferConstructor(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create buffers directly
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0),
            new V3f(1, 1, 0)
        };

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 2,  // stride equals width for contiguous grids
                width = 2,
                height = 2
            }
        };

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var gridBuffer = EmbreeBuffer.Create(device, grids);

        using var gridGeom = new GridGeometry(device, vertexBuffer, 0, vertices.Length,
                                              gridBuffer, 0, grids.Length,
                                              quality);

        Assert.NotEqual(IntPtr.Zero, gridGeom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_DirectPointerAccess(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0),
            new V3f(1, 1, 0)
        };

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 2,  // stride equals width for contiguous grids
                width = 2,
                height = 2
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        // Access and modify vertices directly
        unsafe
        {
            var vertexPtr = gridGeom.GetVertexDataPointer();
            Assert.True(vertexPtr != null);

            // Modify first vertex
            vertexPtr[0] = new V3f(0.1f, 0.1f, 0.1f);
        }

        gridGeom.UpdateBuffer(RTCBufferType.Vertex);
        gridGeom.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_ProperDisposal(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0),
            new V3f(1, 1, 0)
        };

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 2,  // stride equals width for contiguous grids
                width = 2,
                height = 2
            }
        };

        var gridGeom = new GridGeometry(device, vertices, grids, quality);
        var handle = gridGeom.Handle;

        Assert.NotEqual(IntPtr.Zero, handle);

        gridGeom.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => gridGeom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_WideGrid(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a 20x5 grid (wide and short)
        var vertices = new V3f[100];
        for (int y = 0; y < 5; y++)
        for (int x = 0; x < 20; x++)
        {
            vertices[y * 20 + x] = new V3f(x * 0.1f, y * 0.2f, 0);
        }

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 20,  // stride equals width for contiguous grids
                width = 20,
                height = 5
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        Assert.NotEqual(IntPtr.Zero, gridGeom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_TallGrid(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a 5x20 grid (narrow and tall)
        var vertices = new V3f[100];
        for (int y = 0; y < 20; y++)
        for (int x = 0; x < 5; x++)
        {
            vertices[y * 5 + x] = new V3f(x * 0.2f, y * 0.1f, 0);
        }

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 5,  // stride equals width for contiguous grids
                width = 5,
                height = 20
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        Assert.NotEqual(IntPtr.Zero, gridGeom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GridGeometry_UpdateWithSpan(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0),
            new V3f(1, 1, 0)
        };

        var grids = new RTCGrid[]
        {
            new RTCGrid
            {
                startVertexID = 0,
                stride = 2,  // stride equals width for contiguous grids
                width = 2,
                height = 2
            }
        };

        using var gridGeom = new GridGeometry(device, vertices, grids, quality);

        // Update using span (zero-copy)
        Span<V3f> updatedVertices = stackalloc V3f[]
        {
            new V3f(0, 0, 1),
            new V3f(1, 0, 1),
            new V3f(0, 1, 1),
            new V3f(1, 1, 1)
        };

        gridGeom.UpdateVertices((ReadOnlySpan<V3f>)updatedVertices);
        gridGeom.UpdateBuffer(RTCBufferType.Vertex);
        gridGeom.Commit();
    }
}
