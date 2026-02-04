using Aardvark.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Memory stress tests for Aardvark.Embree.
/// Tests memory management under high load, GC pressure, and repeated operations.
/// </summary>
public class MemoryStressTests
{
    private static readonly int[] TriangleIndices = { 0, 1, 2 };
    [Theory(DisplayName = "Repeated geometry allocation and deallocation succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void RepeatedAllocation_1000Geometries_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = TriangleIndices;

        for (int i = 0; i < 1000; i++)
        {
            using var geometry = new TriangleGeometry(device, vertices, indices, quality);
            Assert.NotEqual(IntPtr.Zero, geometry.Handle);
        }

        // Force GC to verify no memory leaks
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Theory(DisplayName = "Large dataset with 100K triangles succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LargeDataset_100KTriangles_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        const int triangleCount = 100_000;
        var vertices = new V3f[triangleCount * 3];
        var indices = new int[triangleCount * 3];

        // Generate grid of triangles
        for (int i = 0; i < triangleCount; i++)
        {
            int row = i / 100;
            int col = i % 100;

            vertices[i * 3 + 0] = new V3f(col, row, 0);
            vertices[i * 3 + 1] = new V3f(col + 1, row, 0);
            vertices[i * 3 + 2] = new V3f(col, row + 1, 0);

            indices[i * 3 + 0] = i * 3 + 0;
            indices[i * 3 + 1] = i * 3 + 1;
            indices[i * 3 + 2] = i * 3 + 2;
        }

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Verify scene works
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(50.5f, 50.5f, 1),
            new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Should intersect large mesh");
        Assert.True(hit.PrimitiveId < triangleCount, "Primitive ID should be valid");
    }

    [Theory(DisplayName = "Rapid allocation under GC pressure succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void RapidAllocation_GCPressure_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = TriangleIndices;

        long memoryBefore = GC.GetTotalMemory(true);

        // Rapidly allocate and release geometries
        for (int i = 0; i < 500; i++)
        {
            using var geometry = new TriangleGeometry(device, vertices, indices, quality);
            using var scene = new Scene(device, quality, dynamic: false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            // Force GC every 50 iterations
            if (i % 50 == 0)
            {
                GC.Collect();
            }
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryAfter = GC.GetTotalMemory(false);

        // Memory growth should be bounded (less than 100MB)
        long memoryGrowth = memoryAfter - memoryBefore;
        Assert.True(memoryGrowth < 100_000_000,
            $"Memory growth {memoryGrowth} bytes should be less than 100MB");
    }

    [Theory(DisplayName = "Concurrent scene building on separate devices succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public async Task ConcurrentSceneBuilding_SeparateDevices_Succeeds(RTCBuildQuality quality)
    {
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            using var device = new Device();
            using var scene = new Scene(device, quality, dynamic: false);

            var vertices = new V3f[]
            {
                new(i, 0, 0), new(i + 1, 0, 0), new(i, 1, 0)
            };
            var indices = TriangleIndices;

            using var geometry = new TriangleGeometry(device, vertices, indices, quality);
            scene.AttachGeometry(geometry);
            scene.Commit();

            var hit = new RayHit();
            return scene.Intersect(
                new V3f(i + 0.5f, 0.5f, 1),
                new V3f(0, 0, -1),
                ref hit
            );
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.All(tasks, t => Assert.True(t.Result));
    }

    [Theory(DisplayName = "Memory leak detection after disposal succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MemoryLeakDetection_AfterDisposal_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = TriangleIndices;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryBefore = GC.GetTotalMemory(false);

        // Create and dispose many objects
        for (int i = 0; i < 100; i++)
        {
            var geometry = new TriangleGeometry(device, vertices, indices, quality);
            var scene = new Scene(device, quality, dynamic: false);
            scene.AttachGeometry(geometry);
            scene.Commit();

            // Explicitly dispose
            scene.Dispose();
            geometry.Dispose();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long memoryAfter = GC.GetTotalMemory(false);
        long memoryGrowth = memoryAfter - memoryBefore;

        // Memory growth should be minimal (less than 10MB)
        Assert.True(memoryGrowth < 10_000_000,
            $"Memory growth {memoryGrowth} bytes should be less than 10MB after disposal");
    }

    [Theory(DisplayName = "Rapid buffer updates on deformable geometry succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void RapidBufferUpdates_DeformableGeometry_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: true);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = TriangleIndices;

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Rapidly update geometry 1000 times
        for (int i = 0; i < 1000; i++)
        {
            var updatedVertices = new V3f[]
            {
                new(0, 0, i * 0.001f),
                new(1, 0, i * 0.001f),
                new(0, 1, i * 0.001f)
            };

            geometry.UpdateVertices((ReadOnlySpan<V3f>)updatedVertices);
            geometry.UpdateBuffer(RTCBufferType.Vertex);
            geometry.Commit();
            scene.Commit();
        }

        // Verify scene still works
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(0.25f, 0.25f, 2),
            new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Should still intersect after rapid updates");
    }
}
