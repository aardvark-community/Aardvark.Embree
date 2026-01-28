using Aardvark.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for thread safety and concurrent access to Embree scenes.
/// NOTE: Async methods must use explicit Dispose() calls instead of 'using' statements
/// to prevent GC from collecting objects while tasks are still running.
/// </summary>
public class ThreadSafetyTests
{
    [Theory(DisplayName = "Concurrent intersect calls on same scene succeed")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task ConcurrentIntersect_SameScene_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        TriangleGeometry geometry = null;
        Scene scene = null;
        try
        {
            var vertices = new V3f[]
            {
                new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
            };
            var indices = new int[] { 0, 1, 2 };

            geometry = new TriangleGeometry(device, vertices, indices, quality);
            scene = new Scene(device, quality, false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
            {
                var ray = new Ray3f(new V3f(0.25f, 0.25f, -1), new V3f(0, 0, 1));
                var hit = new RayHit();
                return scene.Intersect(ray.Origin, ray.Direction, ref hit);
            })).ToArray();

            await Task.WhenAll(tasks);

            Assert.All(tasks, t => Assert.True(t.Result));
        }
        finally
        {
            scene?.Dispose();
            geometry?.Dispose();
            device.Dispose();
        }
    }

    [Theory(DisplayName = "Concurrent intersect calls with different rays succeed")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task ConcurrentIntersect_DifferentRays_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        TriangleGeometry geometry = null;
        Scene scene = null;
        try
        {
            var vertices = new V3f[]
            {
                new(-10, -10, 0), new(10, -10, 0), new(0, 10, 0)
            };
            var indices = new int[] { 0, 1, 2 };

            geometry = new TriangleGeometry(device, vertices, indices, quality);
            scene = new Scene(device, quality, false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
            {
                var x = (i % 10) - 5;
                var y = (i / 10) - 5;
                var ray = new Ray3f(new V3f(x, y, -10), new V3f(0, 0, 1));
                var hit = new RayHit();
                return scene.Intersect(ray.Origin, ray.Direction, ref hit);
            })).ToArray();

            await Task.WhenAll(tasks);

            var hitCount = tasks.Count(t => t.Result);
            Assert.True(hitCount > 0);
        }
        finally
        {
            scene?.Dispose();
            geometry?.Dispose();
            device.Dispose();
        }
    }

    [Theory(DisplayName = "Concurrent scene commit with dynamic scene succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task ConcurrentSceneCommit_DynamicScene_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        TriangleGeometry geometry = null;
        Scene scene = null;
        try
        {
            var vertices = new V3f[]
            {
                new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
            };
            var indices = new int[] { 0, 1, 2 };

            geometry = new TriangleGeometry(device, vertices, indices, quality);
            scene = new Scene(device, quality, true);
            scene.AttachGeometry(geometry);

            var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
            {
                scene.Commit();
            })).ToArray();

            await Task.WhenAll(tasks);

            Assert.All(tasks, t => Assert.True(t.IsCompletedSuccessfully));
        }
        finally
        {
            scene?.Dispose();
            geometry?.Dispose();
            device.Dispose();
        }
    }

    [Theory(DisplayName = "Concurrent GetClosestPoint calls on same scene succeed")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task ConcurrentGetClosestPoint_SameScene_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        TriangleGeometry geometry = null;
        Scene scene = null;
        try
        {
            var vertices = new V3f[]
            {
                new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
            };
            var indices = new int[] { 0, 1, 2 };

            geometry = new TriangleGeometry(device, vertices, indices, quality);
            scene = new Scene(device, quality, false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
            {
                var query = new V3f(0.25f, 0.25f, 1);
                return scene.GetClosestPoint(query);
            })).ToArray();

            await Task.WhenAll(tasks);

            Assert.All(tasks, t => Assert.True(t.Result.IsValid));
        }
        finally
        {
            scene?.Dispose();
            geometry?.Dispose();
            device.Dispose();
        }
    }

    [Theory(DisplayName = "Concurrent geometry creation on same device succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task ConcurrentGeometryCreation_SameDevice_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        TriangleGeometry[] geometries = null;
        try
        {
            var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
            {
                var vertices = new V3f[]
                {
                    new(i, 0, 0), new(i + 1, 0, 0), new(i, 1, 0)
                };
                var indices = new int[] { 0, 1, 2 };
                return new TriangleGeometry(device, vertices, indices, quality);
            })).ToArray();

            geometries = await Task.WhenAll(tasks);

            Assert.All(geometries, g => Assert.NotEqual(IntPtr.Zero, g.Handle));
        }
        finally
        {
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

    [Fact(DisplayName = "Concurrent buffer creation on same device succeeds")]
    public async Task ConcurrentBufferCreation_SameDevice_Succeeds()
    {
        var device = new Device();
        EmbreeBuffer<V3f>[] buffers = null;
        try
        {
            var tasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
            {
                var vertices = new V3f[]
                {
                    new(i, 0, 0), new(i + 1, 0, 0), new(i, 1, 0)
                };
                return EmbreeBuffer.Create(device, vertices);
            })).ToArray();

            buffers = await Task.WhenAll(tasks);

            Assert.All(buffers, b => Assert.NotEqual(IntPtr.Zero, b.Handle));
        }
        finally
        {
            if (buffers != null)
            {
                foreach (var buffer in buffers)
                {
                    buffer?.Dispose();
                }
            }
            device.Dispose();
        }
    }

    [Theory(DisplayName = "Concurrent intersect and GetClosestPoint calls succeed")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task ConcurrentIntersectAndGetClosestPoint_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        TriangleGeometry geometry = null;
        Scene scene = null;
        try
        {
            var vertices = new V3f[]
            {
                new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
            };
            var indices = new int[] { 0, 1, 2 };

            geometry = new TriangleGeometry(device, vertices, indices, quality);
            scene = new Scene(device, quality, false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            var intersectTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
            {
                var ray = new Ray3f(new V3f(0.25f, 0.25f, -1), new V3f(0, 0, 1));
                var hit = new RayHit();
                return scene.Intersect(ray.Origin, ray.Direction, ref hit);
            })).ToArray();

            var closestPointTasks = Enumerable.Range(0, 50).Select(i => Task.Run(() =>
            {
                var query = new V3f(0.25f, 0.25f, 1);
                return scene.GetClosestPoint(query);
            })).ToArray();

            await Task.WhenAll(intersectTasks.Cast<Task>().Concat(closestPointTasks.Cast<Task>()).ToArray());

            Assert.All(intersectTasks, t => Assert.True(t.Result));
            Assert.All(closestPointTasks, t => Assert.True(t.Result.IsValid));
        }
        finally
        {
            scene?.Dispose();
            geometry?.Dispose();
            device.Dispose();
        }
    }

    [Theory(DisplayName = "Multiple threads can query scene bounds concurrently")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task ConcurrentBoundsQuery_Succeeds(RTCBuildQuality quality)
    {
        var device = new Device();
        TriangleGeometry geometry = null;
        Scene scene = null;
        try
        {
            var vertices = new V3f[]
            {
                new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
            };
            var indices = new int[] { 0, 1, 2 };

            geometry = new TriangleGeometry(device, vertices, indices, quality);
            scene = new Scene(device, quality, false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
            {
                return scene.Bounds;
            })).ToArray();

            await Task.WhenAll(tasks);

            Assert.All(tasks, t => Assert.False(t.Result.IsInvalid));
        }
        finally
        {
            scene?.Dispose();
            geometry?.Dispose();
            device.Dispose();
        }
    }

    [Fact(DisplayName = "Concurrent buffer updates on separate buffers succeed")]
    public async Task ConcurrentBufferUpdate_SeparateBuffers_Succeeds()
    {
        var device = new Device();
        EmbreeBuffer<V3f>[] buffers = null;
        try
        {
            buffers = Enumerable.Range(0, 10).Select(i =>
            {
                var vertices = new V3f[] { new(i, 0, 0), new(i + 1, 0, 0), new(i, 1, 0) };
                return EmbreeBuffer.Create(device, vertices);
            }).ToArray();

            var tasks = buffers.Select((buffer, i) => Task.Run(() =>
            {
                var newVertices = new V3f[] { new(i * 10, 0, 0), new(i * 10 + 1, 0, 0), new(i * 10, 1, 0) };
                buffer.Update(newVertices);
            })).ToArray();

            await Task.WhenAll(tasks);

            Assert.All(tasks, t => Assert.True(t.IsCompletedSuccessfully));
        }
        finally
        {
            if (buffers != null)
            {
                foreach (var buffer in buffers)
                {
                    buffer?.Dispose();
                }
            }
            device.Dispose();
        }
    }
}
