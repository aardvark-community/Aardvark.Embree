using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Diagnostics.Tests;

public class InstanceIntersectDiagnostics
{
    [Fact]
    public unsafe void DirectInstanceIntersect_HeapAllocated()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_HeapAllocated ===");
        RunDirectInstanceIntersect(heapAllocate: true);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_StackAllocated()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_StackAllocated ===");
        RunDirectInstanceIntersect(heapAllocate: false);
    }

    [Fact]
    public unsafe void WrapperInstanceIntersect()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== WrapperInstanceIntersect ===");
        using var device = new Device();
        Console.WriteLine($"Embree Version: {device.Version}");

        var vertices = new V3f[]
        {
            new(0, 0, 0),
            new(1, 0, 0),
            new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var instance = new InstanceGeometry(device, geometry, Affine3f.Identity, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        scene.AttachGeometry(instance);
        scene.Commit();

        var hit = new RayHit();
        var origin = new V3f(0.25f, 0.25f, 1.0f);
        var direction = new V3f(0.0f, 0.0f, -1.0f);

        Console.WriteLine($"Before Scene.Intersect: origin={origin}, direction={direction}");
        var result = scene.Intersect(origin, direction, ref hit);
        Console.WriteLine($"After Scene.Intersect: hit={result}, geomID={hit.GeometryId}, primID={hit.PrimitiveId}, tfar={hit.T}");

        Assert.True(result);
        Assert.InRange(hit.T, 0.99f, 1.01f);
    }

    private static unsafe void RunDirectInstanceIntersect(bool heapAllocate)
    {
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        var version = (int)EmbreeAPI.rtcGetDeviceProperty(device, RTCDeviceProperty.Version);
        Console.WriteLine($"Embree Version: {version}");

        IntPtr sourceScene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set vertices
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3), 3
        );
        float* vertices = (float*)vertexBuffer;
        vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
        vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
        vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;

        // Set indices
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Index, 0, RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3), 1
        );
        uint* indices = (uint*)indexBuffer;
        indices[0] = 0; indices[1] = 1; indices[2] = 2;

        EmbreeAPI.rtcCommitGeometry(geom);
        LogDeviceError(device, "after rtcCommitGeometry");
        EmbreeAPI.rtcAttachGeometry(sourceScene, geom);
        LogDeviceError(device, "after rtcAttachGeometry(sourceScene)");
        EmbreeAPI.rtcCommitScene(sourceScene);
        LogDeviceError(device, "after rtcCommitScene(sourceScene)");

        // Create instance
        IntPtr instance = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Instance);
        EmbreeAPI.rtcSetGeometryInstancedScene(instance, sourceScene);
        EmbreeAPI.rtcSetGeometryTimeStepCount(instance, 1);
        LogDeviceError(device, "after instance setup");

        float* transform = stackalloc float[12];
        transform[0] = 1.0f; transform[1] = 0.0f; transform[2] = 0.0f; transform[3] = 0.0f;
        transform[4] = 0.0f; transform[5] = 1.0f; transform[6] = 0.0f; transform[7] = 0.0f;
        transform[8] = 0.0f; transform[9] = 0.0f; transform[10] = 1.0f; transform[11] = 0.0f;
        EmbreeAPI.rtcSetGeometryTransform(instance, 0, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)transform);
        LogDeviceError(device, "after rtcSetGeometryTransform(instance)");
        EmbreeAPI.rtcCommitGeometry(instance);
        LogDeviceError(device, "after rtcCommitGeometry(instance)");

        IntPtr topScene = EmbreeAPI.rtcNewScene(device);
        EmbreeAPI.rtcAttachGeometry(topScene, instance);
        LogDeviceError(device, "after rtcAttachGeometry(topScene)");
        EmbreeAPI.rtcCommitScene(topScene);
        LogDeviceError(device, "after rtcCommitScene(topScene)");

        RTCRayHit* rayhit;
        IntPtr rayhitPtr = IntPtr.Zero;
        try
        {
            if (heapAllocate)
            {
                rayhitPtr = Marshal.AllocHGlobal(Marshal.SizeOf<RTCRayHit>());
                rayhit = (RTCRayHit*)rayhitPtr;
            }
            else
            {
                byte* stackBuffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];
                IntPtr alignedPtr = new IntPtr(((long)stackBuffer + 15) & ~15L);
                rayhit = (RTCRayHit*)alignedPtr;
            }

            rayhit->ray.org = new V3f(0.25f, 0.25f, 1.0f);
            rayhit->ray.dir = new V3f(0.0f, 0.0f, -1.0f);
            rayhit->ray.tnear = 0.0f;
            rayhit->ray.tfar = float.PositiveInfinity;
            rayhit->ray.time = 0.0f;
            rayhit->ray.mask = 0xFFFFFFFF;
            rayhit->ray.id = 0;
            rayhit->ray.flags = 0;
            rayhit->hit.geomID = unchecked((uint)-1);
            rayhit->hit.primID = unchecked((uint)-1);
            rayhit->hit.instID_0 = unchecked((uint)-1);

            Console.WriteLine($"Ray: origin=({rayhit->ray.org}), direction=({rayhit->ray.dir})");
            Console.WriteLine($"rayhit allocated at: 0x{((long)rayhit):X}");
            Console.WriteLine($"Before rtcIntersect1: tnear={rayhit->ray.tnear}, tfar={rayhit->ray.tfar}, time={rayhit->ray.time}, mask={rayhit->ray.mask}, flags={rayhit->ray.flags}");
            Console.WriteLine($"Before rtcIntersect1: geomID={rayhit->hit.geomID}, primID={rayhit->hit.primID}, instID={rayhit->hit.instID_0}");
            LogDeviceError(device, "before rtcIntersect1");

            RTCIntersectArguments args = new RTCIntersectArguments
            {
                flags = RTCRayQueryFlags.None,
                feature_mask = 0xFFFFFFFF,
                context = IntPtr.Zero,
                filter = IntPtr.Zero,
                intersect = IntPtr.Zero
            };

            Console.WriteLine("Calling rtcIntersect1...");
            EmbreeAPI.rtcIntersect1(topScene, rayhit, &args);
            Console.WriteLine("rtcIntersect1 returned");
            LogDeviceError(device, "after rtcIntersect1");
            Console.WriteLine($"After rtcIntersect1: tfar={rayhit->ray.tfar}, geomID={rayhit->hit.geomID}, primID={rayhit->hit.primID}, instID={rayhit->hit.instID_0}");

            bool hit = rayhit->hit.geomID != unchecked((uint)-1);
            Console.WriteLine($"Hit: {hit}, geomID: {rayhit->hit.geomID}, tfar: {rayhit->ray.tfar}");
        }
        finally
        {
            if (rayhitPtr != IntPtr.Zero)
                Marshal.FreeHGlobal(rayhitPtr);
        }

        // Cleanup
        EmbreeAPI.rtcReleaseGeometry(instance);
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcReleaseScene(topScene);
        EmbreeAPI.rtcReleaseScene(sourceScene);
        EmbreeAPI.rtcReleaseDevice(device);
    }

    private static void LogDeviceError(IntPtr device, string label)
    {
        var err = EmbreeAPI.rtcGetDeviceError(device);
        var detailPtr = EmbreeAPI.rtcGetDeviceLastErrorMessage(device);
        var detail = detailPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(detailPtr) : null;

        Console.WriteLine($"[{label}] DeviceError={err}");
        if (!string.IsNullOrWhiteSpace(detail))
            Console.WriteLine($"[{label}] ErrorDetail={detail}");
    }
}
