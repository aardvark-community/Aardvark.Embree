using Aardvark.Base;
using System;
using System.Reflection;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for exception handling, null checks, and error conditions.
/// </summary>
public class ErrorHandlingTests
{
    [Fact(DisplayName = "EmbreeBuffer Update with null data throws")]
    public void EmbreeBufferUpdate_NullData_Throws()
    {
        using var device = new Device();
        var vertices = new V3f[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0) };

        using var buffer = EmbreeBuffer.Create(device, vertices);

        Assert.Throws<ArgumentException>(() => buffer.Update(null));
    }

    [Theory(DisplayName = "TriangleGeometry creation with mismatched index count succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void TriangleGeometryCreation_MismatchedIndexCount_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2, 0 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Theory(DisplayName = "Scene GetClosestPoint with infinite maxRadius succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneGetClosestPoint_InfiniteMaxRadius_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var query = new V3f(1000, 1000, 1000);
        var result = scene.GetClosestPoint(query, float.PositiveInfinity);

        Assert.True(result.IsValid);
    }

    [Theory(DisplayName = "Scene Intersect with invalid ray direction handles gracefully")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneIntersect_InvalidRayDirection_HandlesGracefully(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var ray = new Ray3f(new V3f(0.25f, 0.25f, -1), new V3f(0, 0, 0));
        var hit = new RayHit();

        // Should not crash with zero-length direction
        var result = scene.Intersect(ray.Origin, ray.Direction, ref hit);
    }

    [Theory(DisplayName = "TriangleGeometry with out-of-bounds indices succeeds")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void TriangleGeometry_OutOfBoundsIndices_Succeeds(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 10 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Theory(DisplayName = "Scene Intersect with negative minT and maxT handles correctly")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SceneIntersect_NegativeMinMaxT_HandlesCorrectly(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var ray = new Ray3f(new V3f(0.25f, 0.25f, -1), new V3f(0, 0, 1));
        var hit = new RayHit();

        var result = scene.Intersect(ray.Origin, ray.Direction, ref hit, -1.0f, -0.5f);

        Assert.False(result);
    }

    [Theory(DisplayName = "Device CheckError does not throw on valid operations")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DeviceCheckError_ValidOperations_NoThrow(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);
        var hasError = device.CheckError("test operation");

        Assert.False(hasError);
    }

    [Fact(DisplayName = "Device creation with negative thread count throws")]
    public void DeviceCreation_NegativeThreadCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Device(-1));
    }

    [Fact(DisplayName = "Device CheckError after dispose throws ObjectDisposedException")]
    public void DeviceCheckError_AfterDispose_Throws()
    {
        var device = new Device();
        device.Dispose();

        Assert.Throws<ObjectDisposedException>(() => device.CheckError("test"));
    }

    [Fact(DisplayName = "Device with excessive thread count creates device but may fail operations")]
    public void Device_ExcessiveThreadCount_CreatesButMayFail()
    {
        // Note: Creating device with high thread count may succeed but operations could fail
        // This test verifies we can create device with large thread count
        using var device = new Device(10000);
        Assert.NotEqual(IntPtr.Zero, device.Handle);
        // Actual operation failures would be caught by CheckError throwing exceptions
    }

    [Fact(DisplayName = "Device stores memory monitor callback and clears it")]
    public void Device_SetMemoryMonitorFunction_StoresCallback()
    {
        using var device = new Device();
        device.SetMemoryMonitorFunction((bytes, post) => true);

        var field = typeof(Device).GetField("m_memoryMonitorCallback", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.NotNull(field?.GetValue(device));

        device.SetMemoryMonitorFunction(null);
        Assert.Null(field?.GetValue(device));
    }

    [Fact(DisplayName = "Device stores error callback and clears it")]
    public void Device_SetErrorFunction_StoresCallback()
    {
        using var device = new Device();
        device.SetErrorFunction((error, message) => { });

        var field = typeof(Device).GetField("m_errorCallback", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        Assert.NotNull(field?.GetValue(device));

        device.SetErrorFunction(null);
        Assert.Null(field?.GetValue(device));
    }

    [Fact(DisplayName = "Scene Collide throws after dispose")]
    public void SceneCollide_AfterDispose_Throws()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Low);
        var scene0 = new Scene(device, RTCBuildQuality.Low, false);
        scene0.AttachGeometry(geometry);
        scene0.Commit();

        using var scene1 = new Scene(device, RTCBuildQuality.Low, false);
        scene1.AttachGeometry(geometry);
        scene1.Commit();

        scene0.Dispose();

        Assert.Throws<ObjectDisposedException>(() => scene0.Collide(scene1));
        Assert.Throws<ObjectDisposedException>(() => scene0.Collide(scene1, _ => { }));
    }

    [Fact(DisplayName = "Collision PointQuery throws after dispose")]
    public void CollisionPointQuery_AfterDispose_Throws()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Low);
        var scene = new Scene(device, RTCBuildQuality.Low, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        scene.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            Collision.PointQuery(scene, new V3f(0.1f, 0.1f, 1f), float.PositiveInfinity, (ref RTCPointQueryFunctionArguments args) => true, IntPtr.Zero);
        });
    }
}
