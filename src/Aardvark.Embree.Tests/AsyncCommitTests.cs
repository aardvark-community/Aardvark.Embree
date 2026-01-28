using Aardvark.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for async scene commit functionality.
/// Embree 4 supports non-blocking scene commits that can be waited on using rtcJoinCommitScene.
/// NOTE: Async methods must use explicit Dispose() calls instead of 'using' statements
/// to prevent GC from collecting objects while tasks are still running.
/// </summary>
public class AsyncCommitTests
{
    [Theory(DisplayName = "CommitAsync followed by CommitAsyncWait succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CommitAsync_FollowedByWait_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.CommitAsync();
        scene.CommitAsyncWait();

        // Scene should be usable after async commit completes
        var hit = new RayHit();
        var rayOrigin = new V3f(0, 0, 1);
        var rayDirection = new V3f(0, 0, -1);
        var intersected = scene.Intersect(rayOrigin, rayDirection, ref hit);

        Assert.True(intersected);
    }

    [Theory(DisplayName = "Scene is usable after async commit completes")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CommitAsync_SceneUsableAfterWait_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.CommitAsync();
        scene.CommitAsyncWait();

        // Verify ray queries work correctly
        var hit = new RayHit();
        var rayOrigin = new V3f(0, 0, 1);
        var rayDirection = new V3f(0, 0, -1);

        Assert.True(scene.Intersect(rayOrigin, rayDirection, ref hit));
        Assert.True(hit.T > 0);
        Assert.Equal(0u, hit.GeometryId);
    }

    [Theory(DisplayName = "Multiple scenes can commit asynchronously in parallel")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task MultipleScenes_CommitAsyncInParallel_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        Scene[] scenes = null;
        TriangleGeometry[] geometries = null;
        try
        {
            const int sceneCount = 10;
            scenes = new Scene[sceneCount];
            geometries = new TriangleGeometry[sceneCount];

            // Create scenes and geometries
            for (int i = 0; i < sceneCount; i++)
            {
                var vertices = new[]
                {
                    new V3f(i, 0, 0),
                    new V3f(i + 1, 0, 0),
                    new V3f(i, 1, 0)
                };
                var indices = new[] { 0, 1, 2 };
                geometries[i] = new TriangleGeometry(device, vertices, indices, quality);
                scenes[i] = new Scene(device, quality, dynamic: false);
                scenes[i].AttachGeometry(geometries[i]);
            }

            // Start async commits in parallel
            var commitTasks = Enumerable.Range(0, sceneCount).Select(i => Task.Run(() =>
            {
                scenes[i].CommitAsync();
            })).ToArray();

            await Task.WhenAll(commitTasks);

            // Wait for all commits to complete
            var waitTasks = Enumerable.Range(0, sceneCount).Select(i => Task.Run(() =>
            {
                scenes[i].CommitAsyncWait();
            })).ToArray();

            await Task.WhenAll(waitTasks);

            // Verify all scenes are usable
            for (int i = 0; i < sceneCount; i++)
            {
                var hit = new RayHit();
                var rayOrigin = new V3f(i + 0.5f, 0.5f, 1);
                var rayDirection = new V3f(0, 0, -1);
                Assert.True(scenes[i].Intersect(rayOrigin, rayDirection, ref hit));
            }
        }
        finally
        {
            if (scenes != null)
            {
                foreach (var scene in scenes)
                {
                    scene?.Dispose();
                }
            }
            if (geometries != null)
            {
                foreach (var geometry in geometries)
                {
                    geometry?.Dispose();
                }
            }
            device.Dispose();
        }
    }

    [Theory(DisplayName = "CommitAsync with dynamic scene succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CommitAsync_DynamicScene_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: true);

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.CommitAsync();
        scene.CommitAsyncWait();

        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1);
        var rayDirection = new V3f(0, 0, -1);

        Assert.True(scene.Intersect(rayOrigin, rayDirection, ref hit));
    }

    [Theory(DisplayName = "CommitAsyncWait throws when scene is disposed")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CommitAsyncWait_DisposedScene_Throws(RTCBuildQuality quality)
    {
        using var device = new Device();
        var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.CommitAsync();
        scene.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scene.CommitAsyncWait());
    }

    [Theory(DisplayName = "CommitAsync throws when scene is disposed")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CommitAsync_DisposedScene_Throws(RTCBuildQuality quality)
    {
        using var device = new Device();
        var scene = new Scene(device, quality, dynamic: false);

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        scene.AttachGeometry(geometry);
        scene.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scene.CommitAsync());
    }

    [Theory(DisplayName = "Async commit allows background work during BVH build")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task CommitAsync_AllowsBackgroundWork_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        Scene scene = null;
        TriangleGeometry geometry = null;
        try
        {
            // Create a larger scene to ensure commit takes measurable time
            var vertices = new V3f[300];
            var indices = new int[900];
            for (int i = 0; i < 100; i++)
            {
                vertices[i * 3] = new V3f(i, 0, 0);
                vertices[i * 3 + 1] = new V3f(i + 1, 0, 0);
                vertices[i * 3 + 2] = new V3f(i, 1, 0);
                indices[i * 9] = i * 3;
                indices[i * 9 + 1] = i * 3 + 1;
                indices[i * 9 + 2] = i * 3 + 2;
                indices[i * 9 + 3] = i * 3 + 1;
                indices[i * 9 + 4] = i * 3 + 2;
                indices[i * 9 + 5] = i * 3;
                indices[i * 9 + 6] = i * 3 + 2;
                indices[i * 9 + 7] = i * 3;
                indices[i * 9 + 8] = i * 3 + 1;
            }

            geometry = new TriangleGeometry(device, vertices, indices, quality);
            scene = new Scene(device, quality, dynamic: false);
            scene.AttachGeometry(geometry);

            // Start async commit and do other work
            var commitTask = Task.Run(() => scene.CommitAsync());
            var backgroundWorkDone = false;
            var backgroundTask = Task.Run(async () =>
            {
                await Task.Delay(10); // Simulate some work
                backgroundWorkDone = true;
            });

            await commitTask;
            await backgroundTask;
            scene.CommitAsyncWait();

            Assert.True(backgroundWorkDone);

            // Verify scene is usable
            var hit = new RayHit();
            var rayOrigin = new V3f(50, 0.5f, 1);
            var rayDirection = new V3f(0, 0, -1);
            Assert.True(scene.Intersect(rayOrigin, rayDirection, ref hit));
        }
        finally
        {
            scene?.Dispose();
            geometry?.Dispose();
            device.Dispose();
        }
    }
}
