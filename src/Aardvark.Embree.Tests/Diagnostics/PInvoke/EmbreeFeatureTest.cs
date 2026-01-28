using System;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.Diagnostics.PInvoke;

/// <summary>
/// DIAGNOSTIC TEST: Check Embree feature support in the loaded library.
/// </summary>
public class EmbreeFeatureTest
{
    private readonly ITestOutputHelper _output;

    public EmbreeFeatureTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CheckEmbreeVersion()
    {
        var device = new Device();
        _output.WriteLine($"Embree Version: {device.Version}");
        device.Dispose();
    }

    [Fact]
    public void CheckInstanceGeometryType()
    {
        _output.WriteLine("Testing RTC_GEOMETRY_TYPE_INSTANCE...");
        IntPtr device = EmbreeAPI.rtcNewDevice(null);

        var err1 = EmbreeAPI.rtcGetDeviceError(device);
        _output.WriteLine($"Device created, error: {err1}");

        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Instance);
        var err2 = EmbreeAPI.rtcGetDeviceError(device);
        _output.WriteLine($"Instance geometry creation, error: {err2}");
        _output.WriteLine($"Instance geometry handle: {(geom == IntPtr.Zero ? "NULL" : $"0x{geom.ToInt64():X}")}");

        if (geom != IntPtr.Zero)
        {
            EmbreeAPI.rtcReleaseGeometry(geom);
            _output.WriteLine("Instance geometry SUPPORTED");
        }

        EmbreeAPI.rtcReleaseDevice(device);
        Assert.NotEqual(IntPtr.Zero, geom);
    }

    [Fact]
    public void CheckInstanceArrayGeometryType()
    {
        _output.WriteLine("Testing RTC_GEOMETRY_TYPE_INSTANCE_ARRAY...");
        IntPtr device = EmbreeAPI.rtcNewDevice(null);

        var err1 = EmbreeAPI.rtcGetDeviceError(device);
        _output.WriteLine($"Device created, error: {err1}");

        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.InstanceArray);
        var err2 = EmbreeAPI.rtcGetDeviceError(device);
        _output.WriteLine($"InstanceArray geometry creation, error: {err2}");
        _output.WriteLine($"InstanceArray geometry handle: {(geom == IntPtr.Zero ? "NULL" : $"0x{geom.ToInt64():X}")}");

        if (geom != IntPtr.Zero)
        {
            EmbreeAPI.rtcReleaseGeometry(geom);
            _output.WriteLine("InstanceArray geometry SUPPORTED");
        }
        else
        {
            _output.WriteLine("InstanceArray geometry NOT SUPPORTED - check EMBREE_GEOMETRY_INSTANCE_ARRAY compile flag");
        }

        EmbreeAPI.rtcReleaseDevice(device);

        // Even if it's null, we're testing to understand the state
        _output.WriteLine($"InstanceArray geometry result: {(geom != IntPtr.Zero ? "SUPPORTED" : "NOT SUPPORTED")}");
    }

    [Fact]
    public void CheckInstanceArrayTimeStepBufferSlot()
    {
        _output.WriteLine("Testing InstanceArray time step and buffer slot relationship...");
        IntPtr device = EmbreeAPI.rtcNewDevice(null);

        // Create bottom-level scene
        IntPtr blScene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);
        IntPtr vb = EmbreeAPI.rtcSetNewGeometryBuffer(geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, (nuint)(12), 3);
        unsafe
        {
            float* v = (float*)vb;
            v[0] = 0; v[1] = 0; v[2] = 0;
            v[3] = 1; v[4] = 0; v[5] = 0;
            v[6] = 0; v[7] = 1; v[8] = 0;
        }
        IntPtr ib = EmbreeAPI.rtcSetNewGeometryBuffer(geom, RTCBufferType.Index, 0, RTCFormat.UINT3, (nuint)(12), 1);
        unsafe
        {
            uint* idx = (uint*)ib;
            idx[0] = 0; idx[1] = 1; idx[2] = 2;
        }
        EmbreeAPI.rtcCommitGeometry(geom);
        EmbreeAPI.rtcAttachGeometry(blScene, geom);
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcCommitScene(blScene);
        _output.WriteLine("  Bottom-level scene created");

        // Create instance array
        IntPtr instanceArray = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.InstanceArray);
        _output.WriteLine($"  InstanceArray created: {(instanceArray != IntPtr.Zero ? "OK" : "FAILED")}");
        CheckError(device, "rtcNewGeometry(InstanceArray)");

        // Try setting time step count to 2 (for motion blur)
        _output.WriteLine("\n  Trying rtcSetGeometryTimeStepCount(2) to increase buffer slots...");
        EmbreeAPI.rtcSetGeometryTimeStepCount(instanceArray, 2);
        CheckError(device, "rtcSetGeometryTimeStepCount(2)");

        // Now try setting buffer with slot=0
        _output.WriteLine("\n  Trying rtcSetNewGeometryBuffer(Transform, slot=0)...");
        const int numInstances = 3;
        IntPtr buffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            instanceArray,
            RTCBufferType.Transform,
            0, // slot = 0
            RTCFormat.FLOAT3X4_ROW_MAJOR,
            (nuint)(12 * sizeof(float)),
            (nuint)numInstances);
        var err0 = EmbreeAPI.rtcGetDeviceError(device);
        _output.WriteLine($"  slot=0 result: buffer={buffer}, error={err0}");

        // Try slot 1
        _output.WriteLine("\n  Trying rtcSetNewGeometryBuffer(Transform, slot=1)...");
        IntPtr buffer1 = EmbreeAPI.rtcSetNewGeometryBuffer(
            instanceArray,
            RTCBufferType.Transform,
            1, // slot = 1
            RTCFormat.FLOAT3X4_ROW_MAJOR,
            (nuint)(12 * sizeof(float)),
            (nuint)numInstances);
        var err1 = EmbreeAPI.rtcGetDeviceError(device);
        _output.WriteLine($"  slot=1 result: buffer={buffer1}, error={err1}");

        // Cleanup
        if (instanceArray != IntPtr.Zero)
            EmbreeAPI.rtcReleaseGeometry(instanceArray);
        EmbreeAPI.rtcReleaseScene(blScene);
        EmbreeAPI.rtcReleaseDevice(device);

        _output.WriteLine($"\nResult: slot=0 {(buffer != IntPtr.Zero ? "OK" : "FAILED")}, slot=1 {(buffer1 != IntPtr.Zero ? "OK" : "FAILED")}");
    }

    private void CheckError(IntPtr device, string op)
    {
        var err = EmbreeAPI.rtcGetDeviceError(device);
        if (err != RTCDeviceError.None)
            _output.WriteLine($"    ERROR after {op}: {err}");
    }
}
