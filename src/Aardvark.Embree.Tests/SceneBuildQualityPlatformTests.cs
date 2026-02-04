using System;
using System.Runtime.InteropServices;
using Xunit;
namespace Aardvark.Embree.Tests;

public class SceneBuildQualityPlatformTests
{
    [Fact]
    public void RefitSceneBuildQuality_IsPlatformDependent()
    {
        using var device = new Device();

        var sceneHandle = EmbreeAPI.rtcNewScene(device.Handle);
        if (sceneHandle == IntPtr.Zero)
            throw new InvalidOperationException("rtcNewScene returned null.");

        try
        {
            EmbreeAPI.rtcSetSceneBuildQuality(sceneHandle, RTCBuildQuality.Refit);

            var err = EmbreeAPI.rtcGetDeviceError(device.Handle);
            var detailPtr = EmbreeAPI.rtcGetDeviceLastErrorMessage(device.Handle);
            var detail = detailPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(detailPtr) : null;

            Console.WriteLine($"OS: {RuntimeInformation.OSDescription}");
            Console.WriteLine($"Embree Version: {device.Version}");
            Console.WriteLine($"rtcSetSceneBuildQuality(Refit) error: {err}");
            if (!string.IsNullOrWhiteSpace(detail))
                Console.WriteLine($"Error detail: {detail}");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Assert.NotEqual(RTCDeviceError.None, err);
            }
            else
            {
                Assert.True(err == RTCDeviceError.None
                            || err == RTCDeviceError.Unknown
                            || err == RTCDeviceError.InvalidArgument);
            }
        }
        finally
        {
            EmbreeAPI.rtcReleaseScene(sceneHandle);
        }
    }
}
