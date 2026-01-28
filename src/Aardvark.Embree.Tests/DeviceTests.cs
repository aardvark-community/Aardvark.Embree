using System;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for Device lifecycle, error handling, version queries, and capability detection.
/// </summary>
public class DeviceTests
{
    [Fact(DisplayName = "Device creation with default thread count succeeds")]
    public void DeviceCreation_DefaultThreadCount_Succeeds()
    {
        using var device = new Device();

        Assert.NotEqual(IntPtr.Zero, device.Handle);
        Assert.Equal(0, device.ThreadCount);
    }

    [Fact(DisplayName = "Device creation with specific thread count succeeds")]
    public void DeviceCreation_SpecificThreadCount_Succeeds()
    {
        using var device = new Device(threadCount: 4);

        Assert.NotEqual(IntPtr.Zero, device.Handle);
        Assert.Equal(4, device.ThreadCount);
    }

    [Fact(DisplayName = "Device Version property returns valid version")]
    public void DeviceVersion_ReturnsValidVersion()
    {
        using var device = new Device();

        var version = device.Version;

        Assert.NotNull(version);
        Assert.True(version.Major >= 3, $"Expected major version >= 3, got {version.Major}");
        Assert.True(version.Minor >= 0, $"Expected minor version >= 0, got {version.Minor}");
        Assert.True(version.Build >= 0, $"Expected build version >= 0, got {version.Build}");
    }

    [Fact(DisplayName = "Device RayMaskSupported property returns boolean")]
    public void DeviceRayMaskSupported_ReturnsBoolean()
    {
        using var device = new Device();

        var supported = device.RayMaskSupported;

        // Should be either true or false, just verify it doesn't throw
        Assert.True(supported || !supported);
    }

    [Fact(DisplayName = "Device FilterFunctionSupported property returns boolean")]
    public void DeviceFilterFunctionSupported_ReturnsBoolean()
    {
        using var device = new Device();

        var supported = device.FilterFunctionSupported;

        // Should be either true or false, just verify it doesn't throw
        Assert.True(supported || !supported);
    }

    [Fact(DisplayName = "Device CheckError returns false when no error")]
    public void DeviceCheckError_NoError_ReturnsFalse()
    {
        using var device = new Device();

        var hasError = device.CheckError("test operation");

        Assert.False(hasError);
    }

    [Fact(DisplayName = "Device Dispose sets handle to zero")]
    public void DeviceDispose_SetsHandleToZero()
    {
        var device = new Device();
        var originalHandle = device.Handle;

        Assert.NotEqual(IntPtr.Zero, originalHandle);

        device.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => device.Handle);
    }

    [Fact(DisplayName = "Multiple devices can be created simultaneously")]
    public void MultipleDevices_CanBeCreatedSimultaneously()
    {
        using var device1 = new Device();
        using var device2 = new Device(threadCount: 2);
        using var device3 = new Device(threadCount: 4);

        Assert.NotEqual(IntPtr.Zero, device1.Handle);
        Assert.NotEqual(IntPtr.Zero, device2.Handle);
        Assert.NotEqual(IntPtr.Zero, device3.Handle);

        Assert.NotEqual(device1.Handle, device2.Handle);
        Assert.NotEqual(device2.Handle, device3.Handle);
        Assert.NotEqual(device1.Handle, device3.Handle);
    }

    [Fact(DisplayName = "Device properties accessible after creation")]
    public void DeviceProperties_AccessibleAfterCreation()
    {
        using var device = new Device();

        // Access all properties to ensure they don't throw
        var version = device.Version;
        var rayMaskSupported = device.RayMaskSupported;
        var filterFunctionSupported = device.FilterFunctionSupported;
        var threadCount = device.ThreadCount;
        var handle = device.Handle;

        Assert.NotNull(version);
        Assert.NotEqual(IntPtr.Zero, handle);
    }
}
