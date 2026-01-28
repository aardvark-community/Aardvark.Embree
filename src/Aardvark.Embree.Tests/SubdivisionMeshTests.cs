using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

public class SubdivisionMeshTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateSimpleSubdivisionMesh(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a simple quad (4 vertices)
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 }; // One face with 4 vertices

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.SmoothBoundary, 4.0f);

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_CanIntersect(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a simple quad in XY plane at Z=0
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(1, 1, 0),
            new V3f(-1, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        scene.AttachGeometry(subdiv);
        scene.Commit();

        // Ray from above pointing down at center
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect subdivision surface");
        Assert.Equal(1.0f, hit.T, 2);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_DifferentBoundaryModes(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        // Test smooth boundary mode
        using var smoothBoundary = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.SmoothBoundary);
        Assert.NotEqual(IntPtr.Zero, smoothBoundary.Handle);

        // Test no boundary mode
        using var noBoundary = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.NoBoundary);
        Assert.NotEqual(IntPtr.Zero, noBoundary.Handle);

        // Test pin boundary mode (was EdgeOnly in Embree 3)
        using var pinBoundary = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.PinBoundary);
        Assert.NotEqual(IntPtr.Zero, pinBoundary.Handle);

        // Test pin corners mode (was EdgeAndCorner in Embree 3)
        using var pinCorners = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.PinCorners);
        Assert.NotEqual(IntPtr.Zero, pinCorners.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_DifferentTessellationRates(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        // Test different tessellation rates
        using var lowTess = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.SmoothBoundary, 1.0f);
        Assert.NotEqual(IntPtr.Zero, lowTess.Handle);

        using var mediumTess = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.SmoothBoundary, 4.0f);
        Assert.NotEqual(IntPtr.Zero, mediumTess.Handle);

        using var highTess = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.SmoothBoundary, 16.0f);
        Assert.NotEqual(IntPtr.Zero, highTess.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_WithEdgeCreases(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a cube control mesh
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(1, 1, 0), new V3f(0, 1, 0),
            new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(1, 1, 1), new V3f(0, 1, 1)
        };

        var indices = new uint[]
        {
            0, 1, 2, 3,  // bottom
            4, 5, 6, 7,  // top
            0, 1, 5, 4,  // front
            2, 3, 7, 6,  // back
            0, 4, 7, 3,  // left
            1, 2, 6, 5   // right
        };

        var faces = new uint[] { 4, 4, 4, 4, 4, 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        // Add edge creases to make sharp edges
        var edgeCreaseIndices = new uint[]
        {
            0, 1,  // bottom front edge
            1, 2,  // bottom right edge
            2, 3,  // bottom back edge
            3, 0   // bottom left edge
        };

        var edgeCreaseWeights = new float[]
        {
            float.PositiveInfinity,  // sharp
            float.PositiveInfinity,
            float.PositiveInfinity,
            float.PositiveInfinity
        };

        subdiv.SetEdgeCreases(device, edgeCreaseIndices, edgeCreaseWeights);

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_WithVertexCreases(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        // Add vertex creases to make sharp corners
        var vertexCreaseIndices = new uint[] { 0, 2 };
        var vertexCreaseWeights = new float[] { float.PositiveInfinity, float.PositiveInfinity };

        subdiv.SetVertexCreases(device, vertexCreaseIndices, vertexCreaseWeights);

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_WithMultipleFaces(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create two connected quads
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0),
            new V3f(2, 0, 0),
            new V3f(2, 1, 0)
        };

        var indices = new uint[]
        {
            0, 1, 2, 3,  // first quad
            1, 4, 5, 2   // second quad (shares edge with first)
        };

        var faces = new uint[] { 4, 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_ProperDisposal(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);
        var handle = subdiv.Handle;

        Assert.NotEqual(IntPtr.Zero, handle);

        subdiv.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => subdiv.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_WithVariableFaceSizes(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Mix of triangle, quad, and pentagon
        var vertices = new V3f[]
        {
            // Triangle
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0.5f, 1, 0),
            // Quad
            new V3f(2, 0, 0),
            new V3f(3, 0, 0),
            new V3f(3, 1, 0),
            new V3f(2, 1, 0),
            // Pentagon
            new V3f(4, 0, 0),
            new V3f(5, 0, 0),
            new V3f(5.5f, 0.5f, 0),
            new V3f(4.5f, 1, 0),
            new V3f(3.5f, 0.5f, 0)
        };

        var indices = new uint[]
        {
            0, 1, 2,           // triangle
            3, 4, 5, 6,        // quad
            7, 8, 9, 10, 11    // pentagon
        };

        var faces = new uint[] { 3, 4, 5 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_EdgeCreases_ValidationCheck(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        // Mismatched edge crease data should throw
        var edgeCreaseIndices = new uint[] { 0, 1, 1, 2 }; // 2 edge pairs
        var edgeCreaseWeights = new float[] { 1.0f };      // Only 1 weight

        Assert.Throws<ArgumentException>(() =>
            subdiv.SetEdgeCreases(device, edgeCreaseIndices, edgeCreaseWeights));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SubdivisionMesh_VertexCreases_ValidationCheck(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        // Mismatched vertex crease data should throw
        var vertexCreaseIndices = new uint[] { 0, 1, 2 };
        var vertexCreaseWeights = new float[] { 1.0f, 2.0f }; // Mismatched count

        Assert.Throws<ArgumentException>(() =>
            subdiv.SetVertexCreases(device, vertexCreaseIndices, vertexCreaseWeights));
    }
}
