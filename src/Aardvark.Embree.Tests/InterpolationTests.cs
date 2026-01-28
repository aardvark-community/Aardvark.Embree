using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for interpolation API covering position interpolation on triangles and quads.
/// Tests the high-level wrapper methods in GeometryInterpolation extension class.
/// </summary>
public class InterpolationTests
{
    /// <summary>
    /// Tests basic position interpolation at triangle center (u=1/3, v=1/3).
    /// Expected result is the average of all three vertices.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolatePositionOnTriangle_CenterPoint_ReturnsAveragePosition(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),  // v0
            new V3f(3.0f, 0.0f, 0.0f),  // v1
            new V3f(0.0f, 3.0f, 0.0f)   // v2
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // Interpolate at center of triangle
        // Center of triangle: u=1/3, v=1/3
        // Expected: (1 - 1/3 - 1/3) * v0 + (1/3) * v1 + (1/3) * v2
        //         = (1/3) * (0,0,0) + (1/3) * (3,0,0) + (1/3) * (0,3,0)
        //         = (1,1,0)
        float u = 1.0f / 3.0f;
        float v = 1.0f / 3.0f;
        uint primId = 0;

        var result = scene.InterpolatePosition(geomId, primId, u, v);

        Assert.InRange(result.X, 1.0f - 0.001f, 1.0f + 0.001f);
        Assert.InRange(result.Y, 1.0f - 0.001f, 1.0f + 0.001f);
        Assert.InRange(result.Z, 0.0f - 0.001f, 0.0f + 0.001f);
    }

    /// <summary>
    /// Tests interpolation at u=0, v=0 which should return the first vertex exactly.
    /// Verifies correct barycentric coordinate interpretation.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateAtCorner_Vertex0_ReturnsExactVertex(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(10.0f, 20.0f, 30.0f),  // v0 - should be returned
            new V3f(1.0f, 2.0f, 3.0f),     // v1
            new V3f(4.0f, 5.0f, 6.0f)      // v2
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // At u=0, v=0: weight = (1-0-0)*v0 + 0*v1 + 0*v2 = v0
        float u = 0.0f;
        float v = 0.0f;
        uint primId = 0;

        var result = scene.InterpolatePosition(geomId, primId, u, v);

        Assert.InRange(result.X, vertices[0].X - 0.001f, vertices[0].X + 0.001f);
        Assert.InRange(result.Y, vertices[0].Y - 0.001f, vertices[0].Y + 0.001f);
        Assert.InRange(result.Z, vertices[0].Z - 0.001f, vertices[0].Z + 0.001f);
    }

    /// <summary>
    /// Tests interpolation at u=1, v=0 which should return the second vertex exactly.
    /// Verifies correct barycentric coordinate interpretation for vertex 1.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateAtCorner_Vertex1_ReturnsExactVertex(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(10.0f, 20.0f, 30.0f),  // v0
            new V3f(1.0f, 2.0f, 3.0f),     // v1 - should be returned
            new V3f(4.0f, 5.0f, 6.0f)      // v2
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // At u=1, v=0: weight = (1-1-0)*v0 + 1*v1 + 0*v2 = v1
        float u = 1.0f;
        float v = 0.0f;
        uint primId = 0;

        var result = scene.InterpolatePosition(geomId, primId, u, v);

        Assert.InRange(result.X, vertices[1].X - 0.001f, vertices[1].X + 0.001f);
        Assert.InRange(result.Y, vertices[1].Y - 0.001f, vertices[1].Y + 0.001f);
        Assert.InRange(result.Z, vertices[1].Z - 0.001f, vertices[1].Z + 0.001f);
    }

    /// <summary>
    /// Tests interpolation at u=0, v=1 which should return the third vertex exactly.
    /// Verifies correct barycentric coordinate interpretation for vertex 2.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateAtCorner_Vertex2_ReturnsExactVertex(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(10.0f, 20.0f, 30.0f),  // v0
            new V3f(1.0f, 2.0f, 3.0f),     // v1
            new V3f(4.0f, 5.0f, 6.0f)      // v2 - should be returned
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // At u=0, v=1: weight = (1-0-1)*v0 + 0*v1 + 1*v2 = v2
        float u = 0.0f;
        float v = 1.0f;
        uint primId = 0;

        var result = scene.InterpolatePosition(geomId, primId, u, v);

        Assert.InRange(result.X, vertices[2].X - 0.001f, vertices[2].X + 0.001f);
        Assert.InRange(result.Y, vertices[2].Y - 0.001f, vertices[2].Y + 0.001f);
        Assert.InRange(result.Z, vertices[2].Z - 0.001f, vertices[2].Z + 0.001f);
    }

    /// <summary>
    /// Tests interpolation along edge between v0 and v1 (v=0).
    /// Verifies linear interpolation along triangle edges.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateAtEdge_V0ToV1_ReturnsEdgePoint(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),  // v0
            new V3f(10.0f, 0.0f, 0.0f), // v1
            new V3f(0.0f, 10.0f, 0.0f)  // v2
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // Interpolate at midpoint of edge v0-v1: u=0.5, v=0
        // Expected: (1-0.5-0)*v0 + 0.5*v1 + 0*v2 = 0.5*(0,0,0) + 0.5*(10,0,0) = (5,0,0)
        float u = 0.5f;
        float v = 0.0f;
        uint primId = 0;

        var result = scene.InterpolatePosition(geomId, primId, u, v);

        Assert.InRange(result.X, 5.0f - 0.001f, 5.0f + 0.001f);
        Assert.InRange(result.Y, 0.0f - 0.001f, 0.0f + 0.001f);
        Assert.InRange(result.Z, 0.0f - 0.001f, 0.0f + 0.001f);
    }

    /// <summary>
    /// Tests interpolation on a quad geometry.
    /// Quads are split into triangles by Embree, verifies interpolation works correctly.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateOnQuad_CenterPoint_ReturnsInterpolatedPosition(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a unit quad in XY plane
        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),  // v0
            new V3f(1.0f, 0.0f, 0.0f),  // v1
            new V3f(1.0f, 1.0f, 0.0f),  // v2
            new V3f(0.0f, 1.0f, 0.0f)   // v3
        };
        var indices = new int[] { 0, 1, 2, 3 };

        using var geometry = new QuadGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // For a quad split into two triangles, test interpolation at u=0.5, v=0.5
        // This should give a point near the quad center
        // Since Embree splits quads, the exact primitive ID and UV depend on the split
        // We'll test primitive 0 (first triangle)
        float u = 0.5f;
        float v = 0.5f;
        uint primId = 0;

        var result = scene.InterpolatePosition(geomId, primId, u, v);

        // Result should be somewhere on the quad surface (z=0)
        Assert.InRange(result.Z, -0.001f, 0.001f);
        // X and Y should be within quad bounds
        Assert.InRange(result.X, -0.001f, 1.001f);
        Assert.InRange(result.Y, -0.001f, 1.001f);
    }

    /// <summary>
    /// Tests interpolation with first-order derivatives (tangent vectors).
    /// Verifies that derivatives are computed correctly along u and v directions.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateWithDerivatives_TriangleCenter_ReturnsPositionAndTangents(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a triangle with known tangent directions
        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),  // v0
            new V3f(6.0f, 0.0f, 0.0f),  // v1 - defines u direction
            new V3f(0.0f, 9.0f, 0.0f)   // v2 - defines v direction
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        float u = 0.5f;
        float v = 0.25f;
        uint primId = 0;

        scene.InterpolateWithDerivatives(geomId, primId, u, v, out V3f position, out V3f dPdu, out V3f dPdv);

        // Position should be interpolated correctly
        // P = (1-u-v)*v0 + u*v1 + v*v2 = (0.25)*(0,0,0) + (0.5)*(6,0,0) + (0.25)*(0,9,0) = (3, 2.25, 0)
        Assert.InRange(position.X, 3.0f - 0.001f, 3.0f + 0.001f);
        Assert.InRange(position.Y, 2.25f - 0.001f, 2.25f + 0.001f);
        Assert.InRange(position.Z, -0.001f, 0.001f);

        // For a linear triangle:
        // dP/du = v1 - v0 = (6,0,0) - (0,0,0) = (6,0,0)
        Assert.InRange(dPdu.X, 6.0f - 0.001f, 6.0f + 0.001f);
        Assert.InRange(dPdu.Y, -0.001f, 0.001f);
        Assert.InRange(dPdu.Z, -0.001f, 0.001f);

        // dP/dv = v2 - v0 = (0,9,0) - (0,0,0) = (0,9,0)
        Assert.InRange(dPdv.X, -0.001f, 0.001f);
        Assert.InRange(dPdv.Y, 9.0f - 0.001f, 9.0f + 0.001f);
        Assert.InRange(dPdv.Z, -0.001f, 0.001f);
    }

    /// <summary>
    /// Tests batch interpolation for multiple points on the same geometry.
    /// Verifies that batch operation produces same results as individual calls.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateBatch_MultiplePoints_ReturnsAllPositions(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(10.0f, 0.0f, 0.0f),
            new V3f(0.0f, 10.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // Test 3 different points
        uint[] primIds = { 0, 0, 0 };
        float[] us = { 0.0f, 1.0f, 0.5f };
        float[] vs = { 0.0f, 0.0f, 0.5f };

        var results = scene.InterpolateBatch(geomId, primIds, us, vs);

        Assert.Equal(3, results.Length);

        // Point 0: u=0, v=0 -> v0 = (0,0,0)
        Assert.InRange(results[0].X, -0.001f, 0.001f);
        Assert.InRange(results[0].Y, -0.001f, 0.001f);
        Assert.InRange(results[0].Z, -0.001f, 0.001f);

        // Point 1: u=1, v=0 -> v1 = (10,0,0)
        Assert.InRange(results[1].X, 10.0f - 0.001f, 10.0f + 0.001f);
        Assert.InRange(results[1].Y, -0.001f, 0.001f);
        Assert.InRange(results[1].Z, -0.001f, 0.001f);

        // Point 2: u=0.5, v=0.5 -> center-ish
        // (1-0.5-0.5)*(0,0,0) + 0.5*(10,0,0) + 0.5*(0,10,0) = (5,5,0)
        Assert.InRange(results[2].X, 5.0f - 0.001f, 5.0f + 0.001f);
        Assert.InRange(results[2].Y, 5.0f - 0.001f, 5.0f + 0.001f);
        Assert.InRange(results[2].Z, -0.001f, 0.001f);
    }

    /// <summary>
    /// Tests interpolation on geometry with multiple triangles.
    /// Verifies primitiveId correctly selects the target triangle.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateOnMultiTriangle_DifferentPrimitives_ReturnsCorrectPositions(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Two triangles side by side
        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),   // v0 - triangle 0
            new V3f(1.0f, 0.0f, 0.0f),   // v1 - shared
            new V3f(0.0f, 1.0f, 0.0f),   // v2 - triangle 0
            new V3f(1.0f, 1.0f, 5.0f)    // v3 - triangle 1 (different Z)
        };
        var indices = new int[] { 0, 1, 2, 1, 3, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // Interpolate at same UV on both triangles
        float u = 0.5f;
        float v = 0.25f;

        var result0 = scene.InterpolatePosition(geomId, 0, u, v);
        var result1 = scene.InterpolatePosition(geomId, 1, u, v);

        // Triangle 0 is in Z=0 plane
        Assert.InRange(result0.Z, -0.001f, 0.001f);

        // Triangle 1 has a vertex at Z=5, so interpolated Z should be > 0
        Assert.True(result1.Z > 0.5f);
    }

    /// <summary>
    /// Tests interpolation on triangle in 3D (not axis-aligned).
    /// Verifies interpolation works for arbitrary triangle orientations.
    /// </summary>
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InterpolateOn3DTriangle_ArbitraryOrientation_ReturnsCorrectPosition(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Triangle in 3D space
        var vertices = new V3f[]
        {
            new V3f(1.0f, 2.0f, 3.0f),
            new V3f(4.0f, 5.0f, 6.0f),
            new V3f(7.0f, 8.0f, 9.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        uint geomId = scene.AttachGeometry(geometry);
        scene.Commit();

        // Interpolate at center
        float u = 1.0f / 3.0f;
        float v = 1.0f / 3.0f;
        uint primId = 0;

        var result = scene.InterpolatePosition(geomId, primId, u, v);

        // Expected: (1/3) * (1,2,3) + (1/3) * (4,5,6) + (1/3) * (7,8,9) = (4,5,6)
        Assert.InRange(result.X, 4.0f - 0.001f, 4.0f + 0.001f);
        Assert.InRange(result.Y, 5.0f - 0.001f, 5.0f + 0.001f);
        Assert.InRange(result.Z, 6.0f - 0.001f, 6.0f + 0.001f);
    }
}
