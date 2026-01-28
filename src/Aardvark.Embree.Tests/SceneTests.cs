using Aardvark.Base;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for Scene creation, commit semantics, bounds calculation, geometry attachment, and lifecycle.
/// </summary>
public class SceneTests
{
    [Theory(DisplayName = "Scene creation with default parameters succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneCreation_DefaultParameters_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        Assert.NotEqual(IntPtr.Zero, scene.Handle);
    }

    [Theory(DisplayName = "Scene creation with dynamic flag succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneCreation_DynamicFlag_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: true);

        Assert.NotEqual(IntPtr.Zero, scene.Handle);
    }

    [Theory(DisplayName = "Scene AttachGeometry returns valid geometry ID")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneAttachGeometry_ReturnsValidGeometryId(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        var geometryId = scene.AttachGeometry(geometry);

        Assert.True(geometryId >= 0);
    }

    [Theory(DisplayName = "Scene Commit succeeds after attaching geometry")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneCommit_AfterAttachingGeometry_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // If commit succeeds without throwing, test passes
        Assert.True(true);
    }

    [Theory(DisplayName = "Scene GetGeometry returns attached geometry")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneGetGeometry_ReturnsAttachedGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        var geometryId = scene.AttachGeometry(geometry);
        var retrievedGeometry = scene.GetGeometry(geometryId);

        Assert.NotNull(retrievedGeometry);
        Assert.Equal(geometry, retrievedGeometry);
    }

    [Fact(DisplayName = "Scene Bounds returns valid bounds after commit")]
    public void SceneBounds_AfterCommit_ReturnsValidBounds()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        scene.AttachGeometry(geometry);
        scene.Commit();

        var bounds = scene.Bounds;

        Assert.True(bounds.IsValid);
        Assert.True(bounds.Min.X <= 1.0f);
        Assert.True(bounds.Max.X >= 0.0f);
    }

    [Fact(DisplayName = "Scene Dispose sets handle to zero")]
    public void SceneDispose_SetsHandleToZero()
    {
        using var device = new Device();
        var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);
        var originalHandle = scene.Handle;

        Assert.NotEqual(IntPtr.Zero, originalHandle);

        scene.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => scene.Handle);
    }

    [Fact(DisplayName = "Multiple scenes can be created from same device")]
    public void MultipleScenes_SameDevice_CanBeCreated()
    {
        using var device = new Device();
        using var scene1 = new Scene(device, RTCBuildQuality.Low, dynamic: false);
        using var scene2 = new Scene(device, RTCBuildQuality.Medium, dynamic: false);
        using var scene3 = new Scene(device, RTCBuildQuality.High, dynamic: true);

        Assert.NotEqual(IntPtr.Zero, scene1.Handle);
        Assert.NotEqual(IntPtr.Zero, scene2.Handle);
        Assert.NotEqual(IntPtr.Zero, scene3.Handle);

        Assert.NotEqual(scene1.Handle, scene2.Handle);
        Assert.NotEqual(scene2.Handle, scene3.Handle);
        Assert.NotEqual(scene1.Handle, scene3.Handle);
    }

    [Fact(DisplayName = "Scene Intersect returns true for ray hitting triangle")]
    public void SceneIntersect_RayHitsTriangle_ReturnsTrue()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray pointing from above down to triangle center
        var rayOrigin = new V3f(0, 0, 1);
        var rayDirection = new V3f(0, 0, -1);
        var hit = new RayHit();

        var intersected = scene.Intersect(rayOrigin, rayDirection, ref hit);

        Assert.True(intersected);
        Assert.True(hit.T > 0);
    }

    [Fact(DisplayName = "Scene Intersect returns false for ray missing triangle")]
    public void SceneIntersect_RayMissesTriangle_ReturnsFalse()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray pointing away from triangle
        var rayOrigin = new V3f(0, 0, 1);
        var rayDirection = new V3f(0, 0, 1);
        var hit = new RayHit();

        var intersected = scene.Intersect(rayOrigin, rayDirection, ref hit);

        Assert.False(intersected);
    }

    [Fact(DisplayName = "Scene Occluded returns true for ray hitting triangle")]
    public void SceneOccluded_RayHitsTriangle_ReturnsTrue()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray pointing from above down to triangle center
        var rayOrigin = new V3f(0, 0, 1);
        var rayDirection = new V3f(0, 0, -1);

        var occluded = scene.Occluded(rayOrigin, rayDirection);

        Assert.True(occluded);
    }

    [Fact(DisplayName = "Scene Occluded returns false for ray missing triangle")]
    public void SceneOccluded_RayMissesTriangle_ReturnsFalse()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray pointing away from triangle
        var rayOrigin = new V3f(0, 0, 1);
        var rayDirection = new V3f(0, 0, 1);

        var occluded = scene.Occluded(rayOrigin, rayDirection);

        Assert.False(occluded);
    }

    /// <summary>
    /// NOTE: Async methods must use explicit Dispose() calls instead of 'using' statements
    /// to prevent GC from collecting objects while tasks are still running.
    /// </summary>
    [Fact(DisplayName = "Scene operations are thread-safe with multiple concurrent queries")]
    public async Task SceneOperations_ConcurrentQueries_AreThreadSafe()
    {
        var device = new Device();
        TriangleGeometry geometry = null;
        Scene scene = null;
        try
        {
            // Triangle in XY plane at Z=0
            var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
            var indices = new[] { 0, 1, 2 };
            geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);
            scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

            scene.AttachGeometry(geometry);
            scene.Commit();

            // Perform multiple concurrent intersection tests
            var tasks = new Task<bool>[100];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    var rayOrigin = new V3f(0, 0, 1);
                    var rayDirection = new V3f(0, 0, -1);
                    var hit = new RayHit();
                    return scene.Intersect(rayOrigin, rayDirection, ref hit);
                });
            }

            var results = await Task.WhenAll(tasks);

            // All tasks should have succeeded with consistent results
            foreach (var result in results)
            {
                Assert.True(result);
            }
        }
        finally
        {
            scene?.Dispose();
            geometry?.Dispose();
            device.Dispose();
        }
    }
}
