using Aardvark.Base;
using System;
using System.Threading;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Collection definitions to isolate ray packet tests by width.
/// These tests create many Embree devices and must be split to avoid resource exhaustion.
/// Each collection runs sequentially with its own resource pool.
/// </summary>
[CollectionDefinition("RayPacket4Tests", DisableParallelization = true)]
public class RayPacket4TestCollection { }

[CollectionDefinition("RayPacket8Tests", DisableParallelization = true)]
public class RayPacket8TestCollection { }

[CollectionDefinition("RayPacket16Tests", DisableParallelization = true)]
public class RayPacket16TestCollection { }

/// <summary>
/// Integration tests for 4-wide ray packet operations.
/// </summary>
[Collection("RayPacket4Tests")]
public class RayPacket4IntegrationTests : IDisposable
{
    public void Dispose()
    {
        // Small delay to help native resource cleanup between tests
        Thread.Sleep(5);
        GC.SuppressFinalize(this);
    }
    #region Intersect4 Tests

    [Theory(DisplayName = "Intersect4 all rays hit single triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect4_AllRaysHit_ReturnsCorrectHits(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Large triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-2, -2, 0), new V3f(2, -2, 0), new V3f(0, 2, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 4 rays all aimed at triangle from above, hitting different points
        var origins = new V3f[]
        {
            new V3f(-0.5f, -0.5f, 2),
            new V3f(0.5f, -0.5f, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.0f, 0.0f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var hits = scene.Intersect4(origins, directions);

        Assert.Equal(4, hits.Length);
        Assert.All(hits, hit => Assert.NotEqual(uint.MaxValue, hit.GeometryId));
        Assert.All(hits, hit => Assert.Equal(0u, hit.GeometryId));
        Assert.All(hits, hit => Assert.True(hit.T > 0 && hit.T < float.MaxValue));
    }

    [Theory(DisplayName = "Intersect4 mixed hit and miss")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect4_MixedHitMiss_ReturnsCorrectResults(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0, centered at origin
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 2 rays hit triangle, 2 rays miss
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),      // Hit - center
            new V3f(5, 5, 2),      // Miss - far away
            new V3f(-0.3f, -0.3f, 2),  // Hit - inside
            new V3f(10, 0, 2)      // Miss - far right
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var hits = scene.Intersect4(origins, directions);

        Assert.Equal(4, hits.Length);

        // Ray 0: Should hit
        Assert.NotEqual(uint.MaxValue, hits[0].GeometryId);
        Assert.Equal(0u, hits[0].GeometryId);
        Assert.True(hits[0].T > 0 && hits[0].T < float.MaxValue);

        // Ray 1: Should miss
        Assert.Equal(uint.MaxValue, hits[1].GeometryId);
        Assert.Equal(float.MaxValue, hits[1].T);

        // Ray 2: Should hit
        Assert.NotEqual(uint.MaxValue, hits[2].GeometryId);
        Assert.Equal(0u, hits[2].GeometryId);
        Assert.True(hits[2].T > 0 && hits[2].T < float.MaxValue);

        // Ray 3: Should miss
        Assert.Equal(uint.MaxValue, hits[3].GeometryId);
        Assert.Equal(float.MaxValue, hits[3].T);
    }

    [Theory(DisplayName = "Intersect4 all rays miss")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect4_AllRaysMiss_ReturnsNoHits(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 4 rays all pointing away from triangle
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 1, 2),
            new V3f(-1, -1, 2),
            new V3f(0.5f, 0, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1)   // Away
        };

        var hits = scene.Intersect4(origins, directions);

        Assert.Equal(4, hits.Length);
        Assert.All(hits, hit => Assert.Equal(uint.MaxValue, hit.GeometryId));
        Assert.All(hits, hit => Assert.Equal(float.MaxValue, hit.T));
    }

    [Theory(DisplayName = "Intersect4 validates span length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect4_ValidatesSpanLength_ThrowsOnInsufficientElements(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Only 3 elements (need 4)
        var origins = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        var ex = Assert.Throws<ArgumentException>(() => scene.Intersect4(origins, directions));
        Assert.Contains("at least 4 elements", ex.Message);
    }

    #endregion

    #region Occluded4 Tests

    [Theory(DisplayName = "Occluded4 all rays occluded")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded4_AllOccluded_ReturnsTrue(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Large triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-2, -2, 0), new V3f(2, -2, 0), new V3f(0, 2, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 4 shadow rays all pointing at triangle
        var origins = new V3f[]
        {
            new V3f(-0.5f, -0.5f, 2),
            new V3f(0.5f, -0.5f, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.0f, 0.0f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var occluded = scene.Occluded4(origins, directions);

        Assert.Equal(4, occluded.Length);
        Assert.All(occluded, o => Assert.True(o));
    }

    [Theory(DisplayName = "Occluded4 mixed occlusion")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded4_MixedOcclusion_ReturnsCorrectResults(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 2 rays occluded, 2 clear
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),          // Occluded - center
            new V3f(5, 5, 2),          // Clear - miss
            new V3f(-0.3f, -0.3f, 2),  // Occluded - inside
            new V3f(10, 0, 2)          // Clear - miss
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var occluded = scene.Occluded4(origins, directions);

        Assert.Equal(4, occluded.Length);
        Assert.True(occluded[0]);   // Hit
        Assert.False(occluded[1]);  // Miss
        Assert.True(occluded[2]);   // Hit
        Assert.False(occluded[3]);  // Miss
    }

    [Theory(DisplayName = "Occluded4 none occluded")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded4_NoneOccluded_ReturnsFalse(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 4 rays all pointing away
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 1, 2),
            new V3f(-1, -1, 2),
            new V3f(0.5f, 0, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1)   // Away
        };

        var occluded = scene.Occluded4(origins, directions);

        Assert.Equal(4, occluded.Length);
        Assert.All(occluded, o => Assert.False(o));
    }

    [Theory(DisplayName = "Occluded4 validates span length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded4_ValidatesSpanLength_ThrowsOnInsufficientElements(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Only 3 elements (need 4)
        var origins = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        var ex = Assert.Throws<ArgumentException>(() => scene.Occluded4(origins, directions));
        Assert.Contains("at least 4 elements", ex.Message);
    }

    #endregion

    #region Zero-Copy Span Overload Tests

    [Theory(DisplayName = "Intersect4 span overload with pre-allocated results")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect4_SpanOverload_WritesToPreAllocatedSpan(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] { new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        // Pre-allocate results span
        Span<RayHit> results = stackalloc RayHit[4];

        // Call span overload directly
        scene.Intersect4(origins, directions, results);

        // Verify all hits
        for (int i = 0; i < 4; i++)
        {
            Assert.NotEqual(uint.MaxValue, results[i].GeometryId);
            Assert.Equal(0u, results[i].GeometryId);
            Assert.True(results[i].T > 0 && results[i].T < float.MaxValue);
        }
    }

    [Theory(DisplayName = "Occluded4 span overload with pre-allocated results")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded4_SpanOverload_WritesToPreAllocatedSpan(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] { new V3f(0, 0, 2), new V3f(5, 5, 2), new V3f(-0.3f, 0, 2), new V3f(10, 0, 2) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        // Pre-allocate results span
        Span<bool> results = stackalloc bool[4];

        // Call span overload directly
        scene.Occluded4(origins, directions, results);

        // Verify occlusion results
        Assert.True(results[0]);   // Hit
        Assert.False(results[1]);  // Miss
        Assert.True(results[2]);   // Hit
        Assert.False(results[3]);  // Miss
    }

    [Theory(DisplayName = "Intersect4 span overload validates results length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect4_SpanOverload_ValidatesResultsLength(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] { new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        // Results array too small (only 3 elements) - convert to span in lambda
        var resultsArray = new RayHit[3];

        var ex = Assert.Throws<ArgumentException>(() => scene.Intersect4(origins, directions, resultsArray.AsSpan()));
        Assert.Contains("at least 4 elements", ex.Message);
    }

    [Theory(DisplayName = "Occluded4 span overload validates results length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded4_SpanOverload_ValidatesResultsLength(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] { new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        // Results array too small (only 3 elements) - convert to span in lambda
        var resultsArray = new bool[3];

        var ex = Assert.Throws<ArgumentException>(() => scene.Occluded4(origins, directions, resultsArray.AsSpan()));
        Assert.Contains("at least 4 elements", ex.Message);
    }

    #endregion
}

/// <summary>
/// Integration tests for 8-wide ray packet operations.
/// </summary>
[Collection("RayPacket8Tests")]
public class RayPacket8IntegrationTests : IDisposable
{
    public void Dispose()
    {
        // Small delay to help native resource cleanup between tests
        Thread.Sleep(5);
        GC.SuppressFinalize(this);
    }

    #region Intersect8 Tests

    [Theory(DisplayName = "Intersect8 all rays hit single triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect8_AllRaysHit_ReturnsCorrectHits(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Large triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-2, -2, 0), new V3f(2, -2, 0), new V3f(0, 2, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 8 rays all aimed at triangle from above, hitting different points
        var origins = new V3f[]
        {
            new V3f(-0.5f, -0.5f, 2),
            new V3f(0.5f, -0.5f, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.0f, 0.0f, 2),
            new V3f(-0.25f, -0.25f, 2),
            new V3f(0.25f, -0.25f, 2),
            new V3f(-0.25f, 0.25f, 2),
            new V3f(0.25f, 0.25f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var hits = scene.Intersect8(origins, directions);

        Assert.Equal(8, hits.Length);
        Assert.All(hits, hit => Assert.NotEqual(uint.MaxValue, hit.GeometryId));
        Assert.All(hits, hit => Assert.Equal(0u, hit.GeometryId));
        Assert.All(hits, hit => Assert.True(hit.T > 0 && hit.T < float.MaxValue));
    }

    [Theory(DisplayName = "Intersect8 mixed hit and miss")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect8_MixedHitMiss_ReturnsCorrectResults(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0, centered at origin
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 4 rays hit triangle, 4 rays miss
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),          // Hit - center
            new V3f(5, 5, 2),          // Miss - far away
            new V3f(-0.3f, -0.3f, 2),  // Hit - inside
            new V3f(10, 0, 2),         // Miss - far right
            new V3f(0.2f, 0.0f, 2),    // Hit - center-right
            new V3f(-10, -10, 2),      // Miss - far left-down
            new V3f(-0.2f, 0.0f, 2),   // Hit - center-left
            new V3f(0, 20, 2)          // Miss - far up
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var hits = scene.Intersect8(origins, directions);

        Assert.Equal(8, hits.Length);

        // Ray 0: Should hit
        Assert.NotEqual(uint.MaxValue, hits[0].GeometryId);
        Assert.Equal(0u, hits[0].GeometryId);
        Assert.True(hits[0].T > 0 && hits[0].T < float.MaxValue);

        // Ray 1: Should miss
        Assert.Equal(uint.MaxValue, hits[1].GeometryId);
        Assert.Equal(float.MaxValue, hits[1].T);

        // Ray 2: Should hit
        Assert.NotEqual(uint.MaxValue, hits[2].GeometryId);
        Assert.Equal(0u, hits[2].GeometryId);
        Assert.True(hits[2].T > 0 && hits[2].T < float.MaxValue);

        // Ray 3: Should miss
        Assert.Equal(uint.MaxValue, hits[3].GeometryId);
        Assert.Equal(float.MaxValue, hits[3].T);

        // Ray 4: Should hit
        Assert.NotEqual(uint.MaxValue, hits[4].GeometryId);
        Assert.Equal(0u, hits[4].GeometryId);
        Assert.True(hits[4].T > 0 && hits[4].T < float.MaxValue);

        // Ray 5: Should miss
        Assert.Equal(uint.MaxValue, hits[5].GeometryId);
        Assert.Equal(float.MaxValue, hits[5].T);

        // Ray 6: Should hit
        Assert.NotEqual(uint.MaxValue, hits[6].GeometryId);
        Assert.Equal(0u, hits[6].GeometryId);
        Assert.True(hits[6].T > 0 && hits[6].T < float.MaxValue);

        // Ray 7: Should miss
        Assert.Equal(uint.MaxValue, hits[7].GeometryId);
        Assert.Equal(float.MaxValue, hits[7].T);
    }

    [Theory(DisplayName = "Intersect8 all rays miss")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect8_AllRaysMiss_ReturnsNoHits(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 8 rays all pointing away from triangle
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 1, 2),
            new V3f(-1, -1, 2),
            new V3f(0.5f, 0, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.8f, -0.8f, 2),
            new V3f(-0.3f, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1)   // Away
        };

        var hits = scene.Intersect8(origins, directions);

        Assert.Equal(8, hits.Length);
        Assert.All(hits, hit => Assert.Equal(uint.MaxValue, hit.GeometryId));
        Assert.All(hits, hit => Assert.Equal(float.MaxValue, hit.T));
    }

    [Theory(DisplayName = "Intersect8 validates span length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect8_ValidatesSpanLength_ThrowsOnInsufficientElements(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Only 7 elements (need 8)
        var origins = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1), new V3f(-1, 0, 1), new V3f(0, -1, 1), new V3f(0.5f, 0.5f, 1), new V3f(-0.5f, -0.5f, 1) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        var ex = Assert.Throws<ArgumentException>(() => scene.Intersect8(origins, directions));
        Assert.Contains("at least 8 elements", ex.Message);
    }

    #endregion

    #region Occluded8 Tests

    [Theory(DisplayName = "Occluded8 all rays occluded")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded8_AllOccluded_ReturnsTrue(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Large triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-2, -2, 0), new V3f(2, -2, 0), new V3f(0, 2, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 8 shadow rays all pointing at triangle
        var origins = new V3f[]
        {
            new V3f(-0.5f, -0.5f, 2),
            new V3f(0.5f, -0.5f, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.0f, 0.0f, 2),
            new V3f(-0.25f, -0.25f, 2),
            new V3f(0.25f, -0.25f, 2),
            new V3f(-0.25f, 0.25f, 2),
            new V3f(0.25f, 0.25f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var occluded = scene.Occluded8(origins, directions);

        Assert.Equal(8, occluded.Length);
        Assert.All(occluded, o => Assert.True(o));
    }

    [Theory(DisplayName = "Occluded8 mixed occlusion")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded8_MixedOcclusion_ReturnsCorrectResults(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 4 rays occluded, 4 clear
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),          // Occluded - center
            new V3f(5, 5, 2),          // Clear - miss
            new V3f(-0.3f, -0.3f, 2),  // Occluded - inside
            new V3f(10, 0, 2),         // Clear - miss
            new V3f(0.2f, 0.0f, 2),    // Occluded - center-right
            new V3f(-10, -10, 2),      // Clear - miss
            new V3f(-0.2f, 0.0f, 2),   // Occluded - center-left
            new V3f(0, 20, 2)          // Clear - miss
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var occluded = scene.Occluded8(origins, directions);

        Assert.Equal(8, occluded.Length);
        Assert.True(occluded[0]);   // Hit
        Assert.False(occluded[1]);  // Miss
        Assert.True(occluded[2]);   // Hit
        Assert.False(occluded[3]);  // Miss
        Assert.True(occluded[4]);   // Hit
        Assert.False(occluded[5]);  // Miss
        Assert.True(occluded[6]);   // Hit
        Assert.False(occluded[7]);  // Miss
    }

    [Theory(DisplayName = "Occluded8 none occluded")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded8_NoneOccluded_ReturnsFalse(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 8 rays all pointing away
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 1, 2),
            new V3f(-1, -1, 2),
            new V3f(0.5f, 0, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.8f, -0.8f, 2),
            new V3f(-0.3f, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1)   // Away
        };

        var occluded = scene.Occluded8(origins, directions);

        Assert.Equal(8, occluded.Length);
        Assert.All(occluded, o => Assert.False(o));
    }

    [Theory(DisplayName = "Occluded8 validates span length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded8_ValidatesSpanLength_ThrowsOnInsufficientElements(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Only 7 elements (need 8)
        var origins = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1), new V3f(-1, 0, 1), new V3f(0, -1, 1), new V3f(0.5f, 0.5f, 1), new V3f(-0.5f, -0.5f, 1) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        var ex = Assert.Throws<ArgumentException>(() => scene.Occluded8(origins, directions));
        Assert.Contains("at least 8 elements", ex.Message);
    }

    #endregion

    #region Zero-Copy Span Overload Tests

    [Theory(DisplayName = "Intersect8 span overload with pre-allocated results")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect8_SpanOverload_WritesToPreAllocatedSpan(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] {
            new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2), new V3f(-0.1f, -0.1f, 2), new V3f(0.2f, -0.2f, 2), new V3f(-0.2f, 0.2f, 2)
        };
        var directions = new V3f[] {
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1)
        };

        // Pre-allocate results span
        Span<RayHit> results = stackalloc RayHit[8];

        // Call span overload directly
        scene.Intersect8(origins, directions, results);

        // Verify all hits
        for (int i = 0; i < 8; i++)
        {
            Assert.NotEqual(uint.MaxValue, results[i].GeometryId);
            Assert.Equal(0u, results[i].GeometryId);
            Assert.True(results[i].T > 0 && results[i].T < float.MaxValue);
        }
    }

    [Theory(DisplayName = "Occluded8 span overload with pre-allocated results")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded8_SpanOverload_WritesToPreAllocatedSpan(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] {
            new V3f(0, 0, 2), new V3f(5, 5, 2), new V3f(-0.3f, 0, 2), new V3f(10, 0, 2),
            new V3f(0.2f, 0, 2), new V3f(-10, -10, 2), new V3f(-0.2f, 0, 2), new V3f(0, 20, 2)
        };
        var directions = new V3f[] {
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1)
        };

        // Pre-allocate results span
        Span<bool> results = stackalloc bool[8];

        // Call span overload directly
        scene.Occluded8(origins, directions, results);

        // Verify occlusion results
        Assert.True(results[0]);   // Hit
        Assert.False(results[1]);  // Miss
        Assert.True(results[2]);   // Hit
        Assert.False(results[3]);  // Miss
        Assert.True(results[4]);   // Hit
        Assert.False(results[5]);  // Miss
        Assert.True(results[6]);   // Hit
        Assert.False(results[7]);  // Miss
    }

    [Theory(DisplayName = "Intersect8 span overload validates results length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect8_SpanOverload_ValidatesResultsLength(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] {
            new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2), new V3f(-0.1f, -0.1f, 2), new V3f(0.2f, -0.2f, 2), new V3f(-0.2f, 0.2f, 2)
        };
        var directions = new V3f[] {
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1)
        };

        // Results array too small (only 7 elements) - convert to span in lambda
        var resultsArray = new RayHit[7];

        var ex = Assert.Throws<ArgumentException>(() => scene.Intersect8(origins, directions, resultsArray.AsSpan()));
        Assert.Contains("at least 8 elements", ex.Message);
    }

    [Theory(DisplayName = "Occluded8 span overload validates results length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded8_SpanOverload_ValidatesResultsLength(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] {
            new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2), new V3f(-0.1f, -0.1f, 2), new V3f(0.2f, -0.2f, 2), new V3f(-0.2f, 0.2f, 2)
        };
        var directions = new V3f[] {
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1)
        };

        // Results array too small (only 7 elements) - convert to span in lambda
        var resultsArray = new bool[7];

        var ex = Assert.Throws<ArgumentException>(() => scene.Occluded8(origins, directions, resultsArray.AsSpan()));
        Assert.Contains("at least 8 elements", ex.Message);
    }

    #endregion
}

/// <summary>
/// Integration tests for 16-wide ray packet operations.
/// </summary>
[Collection("RayPacket16Tests")]
public class RayPacket16IntegrationTests : IDisposable
{
    public void Dispose()
    {
        // Small delay to help native resource cleanup between tests
        Thread.Sleep(5);
        GC.SuppressFinalize(this);
    }

    #region Intersect16 Tests

    [Theory(DisplayName = "Intersect16 all rays hit single triangle")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect16_AllRaysHit_ReturnsCorrectHits(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Large triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-2, -2, 0), new V3f(2, -2, 0), new V3f(0, 2, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 16 rays all aimed at triangle from above, hitting different points
        var origins = new V3f[]
        {
            new V3f(-0.5f, -0.5f, 2),
            new V3f(0.5f, -0.5f, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.0f, 0.0f, 2),
            new V3f(-0.25f, -0.25f, 2),
            new V3f(0.25f, -0.25f, 2),
            new V3f(-0.25f, 0.25f, 2),
            new V3f(0.25f, 0.25f, 2),
            new V3f(-0.75f, -0.25f, 2),
            new V3f(0.75f, -0.25f, 2),
            new V3f(-0.75f, 0.25f, 2),
            new V3f(0.1f, 0.1f, 2),
            new V3f(-0.1f, -0.1f, 2),
            new V3f(0.3f, -0.3f, 2),
            new V3f(-0.3f, 0.3f, 2),
            new V3f(0.0f, -0.5f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var hits = scene.Intersect16(origins, directions);

        Assert.Equal(16, hits.Length);
        Assert.All(hits, hit => Assert.NotEqual(uint.MaxValue, hit.GeometryId));
        Assert.All(hits, hit => Assert.Equal(0u, hit.GeometryId));
        Assert.All(hits, hit => Assert.True(hit.T > 0 && hit.T < float.MaxValue));
    }

    [Theory(DisplayName = "Intersect16 mixed hit and miss")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect16_MixedHitMiss_ReturnsCorrectResults(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0, centered at origin
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 8 rays hit triangle, 8 rays miss
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),          // Hit - center
            new V3f(5, 5, 2),          // Miss - far away
            new V3f(-0.3f, -0.3f, 2),  // Hit - inside
            new V3f(10, 0, 2),         // Miss - far right
            new V3f(0.2f, 0.0f, 2),    // Hit - center-right
            new V3f(-10, -10, 2),      // Miss - far left-down
            new V3f(-0.2f, 0.0f, 2),   // Hit - center-left
            new V3f(0, 20, 2),         // Miss - far up
            new V3f(0.1f, 0.1f, 2),    // Hit - center
            new V3f(15, 15, 2),        // Miss - far away
            new V3f(-0.4f, -0.4f, 2),  // Hit - inside
            new V3f(-20, 0, 2),        // Miss - far left
            new V3f(0.3f, -0.1f, 2),   // Hit - center-right
            new V3f(0, -30, 2),        // Miss - far down
            new V3f(-0.1f, 0.2f, 2),   // Hit - center-left
            new V3f(25, 0, 2)          // Miss - far right
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var hits = scene.Intersect16(origins, directions);

        Assert.Equal(16, hits.Length);

        // Ray 0: Should hit
        Assert.NotEqual(uint.MaxValue, hits[0].GeometryId);
        Assert.Equal(0u, hits[0].GeometryId);
        Assert.True(hits[0].T > 0 && hits[0].T < float.MaxValue);

        // Ray 1: Should miss
        Assert.Equal(uint.MaxValue, hits[1].GeometryId);
        Assert.Equal(float.MaxValue, hits[1].T);

        // Ray 2: Should hit
        Assert.NotEqual(uint.MaxValue, hits[2].GeometryId);
        Assert.Equal(0u, hits[2].GeometryId);
        Assert.True(hits[2].T > 0 && hits[2].T < float.MaxValue);

        // Ray 3: Should miss
        Assert.Equal(uint.MaxValue, hits[3].GeometryId);
        Assert.Equal(float.MaxValue, hits[3].T);

        // Ray 4: Should hit
        Assert.NotEqual(uint.MaxValue, hits[4].GeometryId);
        Assert.Equal(0u, hits[4].GeometryId);
        Assert.True(hits[4].T > 0 && hits[4].T < float.MaxValue);

        // Ray 5: Should miss
        Assert.Equal(uint.MaxValue, hits[5].GeometryId);
        Assert.Equal(float.MaxValue, hits[5].T);

        // Ray 6: Should hit
        Assert.NotEqual(uint.MaxValue, hits[6].GeometryId);
        Assert.Equal(0u, hits[6].GeometryId);
        Assert.True(hits[6].T > 0 && hits[6].T < float.MaxValue);

        // Ray 7: Should miss
        Assert.Equal(uint.MaxValue, hits[7].GeometryId);
        Assert.Equal(float.MaxValue, hits[7].T);

        // Ray 8: Should hit
        Assert.NotEqual(uint.MaxValue, hits[8].GeometryId);
        Assert.Equal(0u, hits[8].GeometryId);
        Assert.True(hits[8].T > 0 && hits[8].T < float.MaxValue);

        // Ray 9: Should miss
        Assert.Equal(uint.MaxValue, hits[9].GeometryId);
        Assert.Equal(float.MaxValue, hits[9].T);

        // Ray 10: Should hit
        Assert.NotEqual(uint.MaxValue, hits[10].GeometryId);
        Assert.Equal(0u, hits[10].GeometryId);
        Assert.True(hits[10].T > 0 && hits[10].T < float.MaxValue);

        // Ray 11: Should miss
        Assert.Equal(uint.MaxValue, hits[11].GeometryId);
        Assert.Equal(float.MaxValue, hits[11].T);

        // Ray 12: Should hit
        Assert.NotEqual(uint.MaxValue, hits[12].GeometryId);
        Assert.Equal(0u, hits[12].GeometryId);
        Assert.True(hits[12].T > 0 && hits[12].T < float.MaxValue);

        // Ray 13: Should miss
        Assert.Equal(uint.MaxValue, hits[13].GeometryId);
        Assert.Equal(float.MaxValue, hits[13].T);

        // Ray 14: Should hit
        Assert.NotEqual(uint.MaxValue, hits[14].GeometryId);
        Assert.Equal(0u, hits[14].GeometryId);
        Assert.True(hits[14].T > 0 && hits[14].T < float.MaxValue);

        // Ray 15: Should miss
        Assert.Equal(uint.MaxValue, hits[15].GeometryId);
        Assert.Equal(float.MaxValue, hits[15].T);
    }

    [Theory(DisplayName = "Intersect16 all rays miss")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect16_AllRaysMiss_ReturnsNoHits(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 16 rays all pointing away from triangle
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 1, 2),
            new V3f(-1, -1, 2),
            new V3f(0.5f, 0, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.8f, -0.8f, 2),
            new V3f(-0.3f, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2),
            new V3f(-0.7f, 0.2f, 2),
            new V3f(0.6f, -0.6f, 2),
            new V3f(-0.4f, -0.4f, 2),
            new V3f(0.9f, 0.1f, 2),
            new V3f(-0.2f, -0.8f, 2),
            new V3f(0.3f, 0.7f, 2),
            new V3f(-0.9f, -0.1f, 2),
            new V3f(0.4f, 0.4f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1)   // Away
        };

        var hits = scene.Intersect16(origins, directions);

        Assert.Equal(16, hits.Length);
        Assert.All(hits, hit => Assert.Equal(uint.MaxValue, hit.GeometryId));
        Assert.All(hits, hit => Assert.Equal(float.MaxValue, hit.T));
    }

    [Theory(DisplayName = "Intersect16 validates span length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect16_ValidatesSpanLength_ThrowsOnInsufficientElements(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Only 15 elements (need 16)
        var origins = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1), new V3f(-1, 0, 1), new V3f(0, -1, 1), new V3f(0.5f, 0.5f, 1), new V3f(-0.5f, -0.5f, 1), new V3f(0.25f, 0.25f, 1), new V3f(-0.25f, -0.25f, 1), new V3f(0.75f, 0, 1), new V3f(0, 0.75f, 1), new V3f(-0.75f, 0, 1), new V3f(0, -0.75f, 1), new V3f(0.3f, -0.3f, 1), new V3f(-0.3f, 0.3f, 1) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        var ex = Assert.Throws<ArgumentException>(() => scene.Intersect16(origins, directions));
        Assert.Contains("at least 16 elements", ex.Message);
    }

    #endregion

    #region Occluded16 Tests

    [Theory(DisplayName = "Occluded16 all rays occluded")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded16_AllOccluded_ReturnsTrue(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Large triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-2, -2, 0), new V3f(2, -2, 0), new V3f(0, 2, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 16 shadow rays all pointing at triangle
        var origins = new V3f[]
        {
            new V3f(-0.5f, -0.5f, 2),
            new V3f(0.5f, -0.5f, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.0f, 0.0f, 2),
            new V3f(-0.25f, -0.25f, 2),
            new V3f(0.25f, -0.25f, 2),
            new V3f(-0.25f, 0.25f, 2),
            new V3f(0.25f, 0.25f, 2),
            new V3f(-0.75f, -0.25f, 2),
            new V3f(0.75f, -0.25f, 2),
            new V3f(-0.75f, 0.25f, 2),
            new V3f(0.1f, 0.1f, 2),
            new V3f(-0.1f, -0.1f, 2),
            new V3f(0.3f, -0.3f, 2),
            new V3f(-0.3f, 0.3f, 2),
            new V3f(0.0f, -0.5f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var occluded = scene.Occluded16(origins, directions);

        Assert.Equal(16, occluded.Length);
        Assert.All(occluded, o => Assert.True(o));
    }

    [Theory(DisplayName = "Occluded16 mixed occlusion")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded16_MixedOcclusion_ReturnsCorrectResults(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 8 rays occluded, 8 clear
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),          // Occluded - center
            new V3f(5, 5, 2),          // Clear - miss
            new V3f(-0.3f, -0.3f, 2),  // Occluded - inside
            new V3f(10, 0, 2),         // Clear - miss
            new V3f(0.2f, 0.0f, 2),    // Occluded - center-right
            new V3f(-10, -10, 2),      // Clear - miss
            new V3f(-0.2f, 0.0f, 2),   // Occluded - center-left
            new V3f(0, 20, 2),         // Clear - miss
            new V3f(0.1f, 0.1f, 2),    // Occluded - center
            new V3f(15, 15, 2),        // Clear - miss
            new V3f(-0.4f, -0.4f, 2),  // Occluded - inside
            new V3f(-20, 0, 2),        // Clear - miss
            new V3f(0.3f, -0.1f, 2),   // Occluded - center-right
            new V3f(0, -30, 2),        // Clear - miss
            new V3f(-0.1f, 0.2f, 2),   // Occluded - center-left
            new V3f(25, 0, 2)          // Clear - miss
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1),
            new V3f(0, 0, -1)
        };

        var occluded = scene.Occluded16(origins, directions);

        Assert.Equal(16, occluded.Length);
        Assert.True(occluded[0]);   // Hit
        Assert.False(occluded[1]);  // Miss
        Assert.True(occluded[2]);   // Hit
        Assert.False(occluded[3]);  // Miss
        Assert.True(occluded[4]);   // Hit
        Assert.False(occluded[5]);  // Miss
        Assert.True(occluded[6]);   // Hit
        Assert.False(occluded[7]);  // Miss
        Assert.True(occluded[8]);   // Hit
        Assert.False(occluded[9]);  // Miss
        Assert.True(occluded[10]);  // Hit
        Assert.False(occluded[11]); // Miss
        Assert.True(occluded[12]);  // Hit
        Assert.False(occluded[13]); // Miss
        Assert.True(occluded[14]);  // Hit
        Assert.False(occluded[15]); // Miss
    }

    [Theory(DisplayName = "Occluded16 none occluded")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded16_NoneOccluded_ReturnsFalse(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // 16 rays all pointing away
        var origins = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 1, 2),
            new V3f(-1, -1, 2),
            new V3f(0.5f, 0, 2),
            new V3f(-0.5f, 0.5f, 2),
            new V3f(0.8f, -0.8f, 2),
            new V3f(-0.3f, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2),
            new V3f(-0.7f, 0.2f, 2),
            new V3f(0.6f, -0.6f, 2),
            new V3f(-0.4f, -0.4f, 2),
            new V3f(0.9f, 0.1f, 2),
            new V3f(-0.2f, -0.8f, 2),
            new V3f(0.3f, 0.7f, 2),
            new V3f(-0.9f, -0.1f, 2),
            new V3f(0.4f, 0.4f, 2)
        };
        var directions = new V3f[]
        {
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1),  // Away
            new V3f(0, 0, 1)   // Away
        };

        var occluded = scene.Occluded16(origins, directions);

        Assert.Equal(16, occluded.Length);
        Assert.All(occluded, o => Assert.False(o));
    }

    [Theory(DisplayName = "Occluded16 validates span length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded16_ValidatesSpanLength_ThrowsOnInsufficientElements(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Only 15 elements (need 16)
        var origins = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1), new V3f(-1, 0, 1), new V3f(0, -1, 1), new V3f(0.5f, 0.5f, 1), new V3f(-0.5f, -0.5f, 1), new V3f(0.25f, 0.25f, 1), new V3f(-0.25f, -0.25f, 1), new V3f(0.75f, 0, 1), new V3f(0, 0.75f, 1), new V3f(-0.75f, 0, 1), new V3f(0, -0.75f, 1), new V3f(0.3f, -0.3f, 1), new V3f(-0.3f, 0.3f, 1) };
        var directions = new V3f[] { new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1) };

        var ex = Assert.Throws<ArgumentException>(() => scene.Occluded16(origins, directions));
        Assert.Contains("at least 16 elements", ex.Message);
    }

    #endregion

    #region Zero-Copy Span Overload Tests

    [Theory(DisplayName = "Intersect16 span overload with pre-allocated results")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect16_SpanOverload_WritesToPreAllocatedSpan(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] {
            new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2), new V3f(-0.1f, -0.1f, 2), new V3f(0.2f, -0.2f, 2), new V3f(-0.2f, 0.2f, 2),
            new V3f(0.4f, 0, 2), new V3f(-0.4f, 0, 2), new V3f(0, 0.4f, 2), new V3f(0, -0.4f, 2),
            new V3f(0.15f, 0.15f, 2), new V3f(-0.15f, -0.15f, 2), new V3f(0.25f, -0.25f, 2), new V3f(-0.25f, 0.25f, 2)
        };
        var directions = new V3f[] {
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1)
        };

        // Pre-allocate results span
        Span<RayHit> results = stackalloc RayHit[16];

        // Call span overload directly
        scene.Intersect16(origins, directions, results);

        // Verify all hits
        for (int i = 0; i < 16; i++)
        {
            Assert.NotEqual(uint.MaxValue, results[i].GeometryId);
            Assert.Equal(0u, results[i].GeometryId);
            Assert.True(results[i].T > 0 && results[i].T < float.MaxValue);
        }
    }

    [Theory(DisplayName = "Occluded16 span overload with pre-allocated results")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded16_SpanOverload_WritesToPreAllocatedSpan(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] {
            new V3f(0, 0, 2), new V3f(5, 5, 2), new V3f(-0.3f, 0, 2), new V3f(10, 0, 2),
            new V3f(0.2f, 0, 2), new V3f(-10, -10, 2), new V3f(-0.2f, 0, 2), new V3f(0, 20, 2),
            new V3f(0.1f, 0.1f, 2), new V3f(15, 15, 2), new V3f(-0.4f, -0.4f, 2), new V3f(-20, 0, 2),
            new V3f(0.3f, -0.1f, 2), new V3f(0, -30, 2), new V3f(-0.1f, 0.2f, 2), new V3f(25, 0, 2)
        };
        var directions = new V3f[] {
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1)
        };

        // Pre-allocate results span
        Span<bool> results = stackalloc bool[16];

        // Call span overload directly
        scene.Occluded16(origins, directions, results);

        // Verify occlusion results
        Assert.True(results[0]);    // Hit
        Assert.False(results[1]);   // Miss
        Assert.True(results[2]);    // Hit
        Assert.False(results[3]);   // Miss
        Assert.True(results[4]);    // Hit
        Assert.False(results[5]);   // Miss
        Assert.True(results[6]);    // Hit
        Assert.False(results[7]);   // Miss
        Assert.True(results[8]);    // Hit
        Assert.False(results[9]);   // Miss
        Assert.True(results[10]);   // Hit
        Assert.False(results[11]);  // Miss
        Assert.True(results[12]);   // Hit
        Assert.False(results[13]);  // Miss
        Assert.True(results[14]);   // Hit
        Assert.False(results[15]);  // Miss
    }

    [Theory(DisplayName = "Intersect16 span overload validates results length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect16_SpanOverload_ValidatesResultsLength(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] {
            new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2), new V3f(-0.1f, -0.1f, 2), new V3f(0.2f, -0.2f, 2), new V3f(-0.2f, 0.2f, 2),
            new V3f(0.4f, 0, 2), new V3f(-0.4f, 0, 2), new V3f(0, 0.4f, 2), new V3f(0, -0.4f, 2),
            new V3f(0.15f, 0.15f, 2), new V3f(-0.15f, -0.15f, 2), new V3f(0.25f, -0.25f, 2), new V3f(-0.25f, 0.25f, 2)
        };
        var directions = new V3f[] {
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1)
        };

        // Results array too small (only 15 elements) - convert to span in lambda
        var resultsArray = new RayHit[15];

        var ex = Assert.Throws<ArgumentException>(() => scene.Intersect16(origins, directions, resultsArray.AsSpan()));
        Assert.Contains("at least 16 elements", ex.Message);
    }

    [Theory(DisplayName = "Occluded16 span overload validates results length")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Occluded16_SpanOverload_ValidatesResultsLength(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var origins = new V3f[] {
            new V3f(0, 0, 2), new V3f(0.3f, 0, 2), new V3f(-0.3f, 0, 2), new V3f(0, 0.3f, 2),
            new V3f(0.1f, 0.1f, 2), new V3f(-0.1f, -0.1f, 2), new V3f(0.2f, -0.2f, 2), new V3f(-0.2f, 0.2f, 2),
            new V3f(0.4f, 0, 2), new V3f(-0.4f, 0, 2), new V3f(0, 0.4f, 2), new V3f(0, -0.4f, 2),
            new V3f(0.15f, 0.15f, 2), new V3f(-0.15f, -0.15f, 2), new V3f(0.25f, -0.25f, 2), new V3f(-0.25f, 0.25f, 2)
        };
        var directions = new V3f[] {
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1),
            new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1), new V3f(0, 0, -1)
        };

        // Results array too small (only 15 elements) - convert to span in lambda
        var resultsArray = new bool[15];

        var ex = Assert.Throws<ArgumentException>(() => scene.Occluded16(origins, directions, resultsArray.AsSpan()));
        Assert.Contains("at least 16 elements", ex.Message);
    }

    #endregion
}
