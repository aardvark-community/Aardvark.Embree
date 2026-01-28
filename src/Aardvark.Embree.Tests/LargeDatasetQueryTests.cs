using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

public class LargeDatasetQueryTests
{
    private const int TriangleCount = 50000;

    /// <summary>
    /// Creates a scene with 50,000 triangles arranged in a linear strip along the X-axis.
    /// Each triangle i is a right triangle spanning X coordinates from i to i+1, with height 1 in Y.
    /// All triangles lie in the Z=0 plane.
    /// </summary>
    private static (Device device, Scene scene) CreateLargeTriangleScene(RTCBuildQuality quality = RTCBuildQuality.Medium)
    {
        var device = new Device();
        var vertices = new V3f[TriangleCount * 3];
        var indices = new int[TriangleCount * 3];

        for (int i = 0; i < TriangleCount; i++)
        {
            var baseIdx = i * 3;
            vertices[baseIdx] = new V3f(i, 0, 0);          // vertex 0: (i, 0, 0)
            vertices[baseIdx + 1] = new V3f(i + 1, 0, 0);  // vertex 1: (i+1, 0, 0)
            vertices[baseIdx + 2] = new V3f(i, 1, 0);      // vertex 2: (i, 1, 0)
            indices[baseIdx] = baseIdx;
            indices[baseIdx + 1] = baseIdx + 1;
            indices[baseIdx + 2] = baseIdx + 2;
        }

        var geometry = new TriangleGeometry(device, vertices, indices, quality);
        var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        return (device, scene);
    }

    /// <summary>
    /// Creates a scene with 50,000 small triangles randomly placed in the unit cube [0,1]^3.
    /// Each triangle has random position and orientation with edge lengths ~0.01 units.
    /// Uses seeded RNG for reproducibility.
    /// </summary>
    private static (Device device, Scene scene) CreateRandomTriangleScene(RTCBuildQuality quality = RTCBuildQuality.Medium, int seed = 42)
    {
        var random = new System.Random(seed);
        var device = new Device();
        var vertices = new V3f[TriangleCount * 3];
        var indices = new int[TriangleCount * 3];

        for (int i = 0; i < TriangleCount; i++)
        {
            var baseIdx = i * 3;

            // Random center in [0,1]^3
            var center = new V3f(
                (float)random.NextDouble(),
                (float)random.NextDouble(),
                (float)random.NextDouble()
            );

            // Random orientation: two random offset vectors for a small triangle
            var size = 0.01f;
            var offset1 = new V3f(
                (float)(random.NextDouble() - 0.5) * size,
                (float)(random.NextDouble() - 0.5) * size,
                (float)(random.NextDouble() - 0.5) * size
            );
            var offset2 = new V3f(
                (float)(random.NextDouble() - 0.5) * size,
                (float)(random.NextDouble() - 0.5) * size,
                (float)(random.NextDouble() - 0.5) * size
            );

            vertices[baseIdx] = center;
            vertices[baseIdx + 1] = center + offset1;
            vertices[baseIdx + 2] = center + offset2;

            indices[baseIdx] = baseIdx;
            indices[baseIdx + 1] = baseIdx + 1;
            indices[baseIdx + 2] = baseIdx + 2;
        }

        var geometry = new TriangleGeometry(device, vertices, indices, quality);
        var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        return (device, scene);
    }

    #region Intersection Tests

    [Theory(DisplayName = "Large dataset - ray hits first triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_RayHitsFirstTriangle_ReturnsCorrectPrimitive(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit);

        Assert.True(result);
        Assert.True(hit.T > 0.0f);
        Assert.Equal(0u, hit.GeometryId);
        Assert.Equal(0u, hit.PrimitiveId);

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - ray hits middle triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_RayHitsMiddleTriangle_ReturnsCorrectPrimitive(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        var hit = new RayHit();
        var rayOrigin = new V3f(25000.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit);

        Assert.True(result);
        Assert.True(hit.T > 0.0f);
        Assert.Equal(0u, hit.GeometryId);
        Assert.Equal(25000u, hit.PrimitiveId);

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - ray hits last triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_RayHitsLastTriangle_ReturnsCorrectPrimitive(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        var hit = new RayHit();
        var rayOrigin = new V3f(49999.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit);

        Assert.True(result);
        Assert.True(hit.T > 0.0f);
        Assert.Equal(0u, hit.GeometryId);
        Assert.Equal(49999u, hit.PrimitiveId);

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - ray misses all triangles")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_RayMissesAllTriangles_ReturnsFalse(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        var hit = new RayHit();
        var rayOrigin = new V3f(60000f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit);

        Assert.False(result);

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - multiple random ray hits")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_MultipleRandomRayHits_ReturnsCorrectPrimitives(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        // Test 10 random triangle positions
        var testIndices = new uint[] { 100, 1000, 5000, 10000, 15000, 20000, 30000, 35000, 40000, 45000 };

        foreach (var triangleIndex in testIndices)
        {
            var hit = new RayHit();
            var rayOrigin = new V3f(triangleIndex + 0.25f, 0.25f, 1.0f);
            var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

            var result = scene.Intersect(rayOrigin, rayDirection, ref hit);

            Assert.True(result, $"Ray should hit triangle {triangleIndex}");
            Assert.Equal(triangleIndex, hit.PrimitiveId);
        }

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - occlusion test")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_OcclusionTest_ReturnsCorrectResults(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        // Test hit case
        var rayOriginHit = new V3f(1000.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);
        var resultHit = scene.Occluded(rayOriginHit, rayDirection);
        Assert.True(resultHit, "Ray should be occluded by triangle");

        // Test miss case
        var rayOriginMiss = new V3f(60000f, 0.25f, 1.0f);
        var resultMiss = scene.Occluded(rayOriginMiss, rayDirection);
        Assert.False(resultMiss, "Ray should not be occluded");

        scene.Dispose();
        device.Dispose();
    }

    #endregion

    #region Closest Point Tests

    [Theory(DisplayName = "Large dataset - closest point to first triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_ClosestPointToFirstTriangle_ReturnsCorrectPrimitive(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        var queryPoint = new V3f(0.5f, 0.5f, 0.1f);
        var result = scene.GetClosestPoint(queryPoint);

        Assert.True(result.IsValid);
        Assert.Equal(0u, result.GeomID);
        Assert.Equal(0u, result.PrimID);
        Assert.True(result.DistanceSquared < 0.02f, $"Distance squared should be small, was {result.DistanceSquared}");

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - closest point to middle triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_ClosestPointToMiddleTriangle_ReturnsCorrectPrimitive(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        var queryPoint = new V3f(25000.5f, 0.5f, 0.1f);
        var result = scene.GetClosestPoint(queryPoint);

        Assert.True(result.IsValid);
        Assert.Equal(0u, result.GeomID);
        Assert.Equal(25000u, result.PrimID);
        Assert.True(result.DistanceSquared < 0.02f, $"Distance squared should be small, was {result.DistanceSquared}");

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - closest point to last triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_ClosestPointToLastTriangle_ReturnsCorrectPrimitive(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        var queryPoint = new V3f(49999.5f, 0.5f, 0.1f);
        var result = scene.GetClosestPoint(queryPoint);

        Assert.True(result.IsValid);
        Assert.Equal(0u, result.GeomID);
        Assert.Equal(49999u, result.PrimID);
        Assert.True(result.DistanceSquared < 0.02f, $"Distance squared should be small, was {result.DistanceSquared}");

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - far query point")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_FarQueryPoint_FindsClosestTriangle(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        var queryPoint = new V3f(25000f, 0.5f, 100f);
        var result = scene.GetClosestPoint(queryPoint);

        Assert.True(result.IsValid);
        Assert.Equal(0u, result.GeomID);
        // Should find triangle 25000 or nearby
        Assert.InRange(result.PrimID, 24900u, 25100u);
        Assert.True(result.DistanceSquared > 9000f, $"Distance should be large, was {result.DistanceSquared}");

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - query on triangle surface")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_QueryOnTriangleSurface_ReturnsNearZeroDistance(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        // Point on triangle 100: vertices are (100,0,0), (101,0,0), (100,1,0)
        // Use barycentric coordinates to get a point inside the triangle
        var queryPoint = new V3f(100.333f, 0.333f, 0f);
        var result = scene.GetClosestPoint(queryPoint);

        Assert.True(result.IsValid);
        Assert.Equal(0u, result.GeomID);
        Assert.Equal(100u, result.PrimID);
        Assert.True(result.DistanceSquared < 0.01f, $"Distance squared should be near zero, was {result.DistanceSquared}");

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - multiple random closest point queries")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_MultipleRandomClosestPointQueries_ReturnsCorrectPrimitives(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        // Test 10 random triangle positions
        var testIndices = new uint[] { 50, 500, 2500, 7500, 12500, 17500, 22500, 32500, 37500, 42500 };

        foreach (var triangleIndex in testIndices)
        {
            var queryPoint = new V3f(triangleIndex + 0.5f, 0.5f, 0.1f);
            var result = scene.GetClosestPoint(queryPoint);

            Assert.True(result.IsValid, $"Query should find closest point near triangle {triangleIndex}");
            Assert.Equal(triangleIndex, result.PrimID);
            Assert.True(result.DistanceSquared < 0.02f, $"Distance should be small for triangle {triangleIndex}");
        }

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - closest point with maxRadius constraint")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_ClosestPointWithMaxRadius_RespectsRadiusConstraint(RTCBuildQuality quality)
    {
        var (device, scene) = CreateLargeTriangleScene(quality);

        // Query point near triangle 1000, but with very small radius
        var queryPoint = new V3f(1000.5f, 0.5f, 10f); // 10 units above triangle
        var smallRadius = 5f; // Radius too small to reach any triangle

        var result = scene.GetClosestPoint(queryPoint, smallRadius);

        // With small radius, should not find anything
        Assert.False(result.IsValid, "Should not find any geometry within small radius");

        // Now with large radius, should find triangle
        var largeRadius = 15f;
        var result2 = scene.GetClosestPoint(queryPoint, largeRadius);

        Assert.True(result2.IsValid, "Should find geometry within large radius");
        Assert.Equal(1000u, result2.PrimID);

        scene.Dispose();
        device.Dispose();
    }

    #endregion

    #region Fuzzing Tests

    [Theory(DisplayName = "Large dataset - random rays fuzzing (no exceptions or invalid values)")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_RandomRaysFuzzing_NoExceptionsOrInvalidValues(RTCBuildQuality quality)
    {
        var (device, scene) = CreateRandomTriangleScene(quality);
        var random = new System.Random(42);
        const int queryCount = 1000;

        for (int i = 0; i < queryCount; i++)
        {
            // Random ray origin in/around unit cube [-0.5, 1.5]^3
            var rayOrigin = new V3f(
                (float)random.NextDouble() * 2.0f - 0.5f,
                (float)random.NextDouble() * 2.0f - 0.5f,
                (float)random.NextDouble() * 2.0f - 0.5f
            );

            // Random ray direction (normalized)
            var rayDirection = new V3f(
                (float)random.NextDouble() * 2.0f - 1.0f,
                (float)random.NextDouble() * 2.0f - 1.0f,
                (float)random.NextDouble() * 2.0f - 1.0f
            );
            rayDirection = rayDirection.Normalized;

            var hit = new RayHit();
            var result = scene.Intersect(rayOrigin, rayDirection, ref hit);

            // If hit, validate invariants
            if (result)
            {
                Assert.True(hit.T >= 0.0f, $"Query {i}: Hit T must be non-negative, was {hit.T}");
                Assert.False(float.IsNaN(hit.T), $"Query {i}: Hit T must not be NaN");
                Assert.False(float.IsInfinity(hit.T), $"Query {i}: Hit T must be finite");
                Assert.True(hit.GeometryId != unchecked((uint)-1), $"Query {i}: GeometryId must be valid");
                Assert.True(hit.PrimitiveId < TriangleCount, $"Query {i}: PrimitiveId {hit.PrimitiveId} must be < {TriangleCount}");

                // Validate barycentric coordinates
                Assert.False(float.IsNaN(hit.Coord.X), $"Query {i}: Barycentric U must not be NaN");
                Assert.False(float.IsNaN(hit.Coord.Y), $"Query {i}: Barycentric V must not be NaN");
                Assert.True(hit.Coord.X >= 0.0f && hit.Coord.X <= 1.0f, $"Query {i}: Barycentric U {hit.Coord.X} must be in [0,1]");
                Assert.True(hit.Coord.Y >= 0.0f && hit.Coord.Y <= 1.0f, $"Query {i}: Barycentric V {hit.Coord.Y} must be in [0,1]");

                // Validate normal
                Assert.False(float.IsNaN(hit.Normal.X), $"Query {i}: Normal.X must not be NaN");
                Assert.False(float.IsNaN(hit.Normal.Y), $"Query {i}: Normal.Y must not be NaN");
                Assert.False(float.IsNaN(hit.Normal.Z), $"Query {i}: Normal.Z must not be NaN");
            }
        }

        scene.Dispose();
        device.Dispose();
    }

    [Theory(DisplayName = "Large dataset - random closest point queries fuzzing (no exceptions or invalid values)")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_RandomClosestPointFuzzing_NoExceptionsOrInvalidValues(RTCBuildQuality quality)
    {
        var (device, scene) = CreateRandomTriangleScene(quality);
        var random = new System.Random(42);
        const int queryCount = 1000;

        for (int i = 0; i < queryCount; i++)
        {
            // Random query point in/around unit cube [-0.5, 1.5]^3
            var queryPoint = new V3f(
                (float)random.NextDouble() * 2.0f - 0.5f,
                (float)random.NextDouble() * 2.0f - 0.5f,
                (float)random.NextDouble() * 2.0f - 0.5f
            );

            var result = scene.GetClosestPoint(queryPoint);

            // If valid result, validate invariants
            if (result.IsValid)
            {
                Assert.True(result.DistanceSquared >= 0.0f, $"Query {i}: DistanceSquared must be non-negative, was {result.DistanceSquared}");
                Assert.False(float.IsNaN(result.DistanceSquared), $"Query {i}: DistanceSquared must not be NaN");
                Assert.False(float.IsInfinity(result.DistanceSquared), $"Query {i}: DistanceSquared must be finite");
                Assert.True(result.GeomID != unchecked((uint)-1), $"Query {i}: GeomID must be valid");
                Assert.True(result.PrimID < TriangleCount, $"Query {i}: PrimID {result.PrimID} must be < {TriangleCount}");

                // Validate closest point
                Assert.False(float.IsNaN(result.Point.X), $"Query {i}: Point.X must not be NaN");
                Assert.False(float.IsNaN(result.Point.Y), $"Query {i}: Point.Y must not be NaN");
                Assert.False(float.IsNaN(result.Point.Z), $"Query {i}: Point.Z must not be NaN");

                // Validate barycentric coordinates
                Assert.False(float.IsNaN(result.UV.X), $"Query {i}: Barycentric U must not be NaN");
                Assert.False(float.IsNaN(result.UV.Y), $"Query {i}: Barycentric V must not be NaN");
                Assert.True(result.UV.X >= 0.0f && result.UV.X <= 1.0f, $"Query {i}: Barycentric U {result.UV.X} must be in [0,1]");
                Assert.True(result.UV.Y >= 0.0f && result.UV.Y <= 1.0f, $"Query {i}: Barycentric V {result.UV.Y} must be in [0,1]");
            }
        }

        scene.Dispose();
        device.Dispose();
    }

    #endregion
}
