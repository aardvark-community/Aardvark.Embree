using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests;

public class SceneBuildQualityPlatformTests
{
    private readonly ITestOutputHelper _output;

    public SceneBuildQualityPlatformTests(ITestOutputHelper output)
    {
        _output = output;
    }

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

            _output.WriteLine($"OS: {RuntimeInformation.OSDescription}");
            _output.WriteLine($"Embree Version: {device.Version}");
            _output.WriteLine($"rtcSetSceneBuildQuality(Refit) error: {err}");
            if (!string.IsNullOrWhiteSpace(detail))
                _output.WriteLine($"Error detail: {detail}");

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
