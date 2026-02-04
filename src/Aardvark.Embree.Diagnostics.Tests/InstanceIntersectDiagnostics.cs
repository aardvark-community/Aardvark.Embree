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
        RunDirectInstanceIntersect(AllocationMode.HeapUnaligned, CleanupMode.Full);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_StackAllocated()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_StackAllocated ===");
        RunDirectInstanceIntersect(AllocationMode.StackAligned, CleanupMode.Full);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_HeapAligned16()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_HeapAligned16 ===");
        RunDirectInstanceIntersect(AllocationMode.HeapAligned16, CleanupMode.Full);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_HeapAligned16_NoCleanup()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_HeapAligned16_NoCleanup ===");
        RunDirectInstanceIntersect(AllocationMode.HeapAligned16, CleanupMode.None);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_HeapAligned64()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_HeapAligned64 ===");
        RunDirectInstanceIntersect(AllocationMode.HeapAligned64, CleanupMode.Full);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_HeapAligned64_NoCleanup()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_HeapAligned64_NoCleanup ===");
        RunDirectInstanceIntersect(AllocationMode.HeapAligned64, CleanupMode.None);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_HeapAligned32()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_HeapAligned32 ===");
        RunDirectInstanceIntersect(AllocationMode.HeapAligned32, CleanupMode.Full);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_HeapAligned32_NoCleanup()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_HeapAligned32_NoCleanup ===");
        RunDirectInstanceIntersect(AllocationMode.HeapAligned32, CleanupMode.None);
    }

    [Fact]
    public unsafe void DirectInstanceIntersect_HeapAligned16_SkipInstanceRelease()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        Console.WriteLine("=== DirectInstanceIntersect_HeapAligned16_SkipInstanceRelease ===");
        RunDirectInstanceIntersect(AllocationMode.HeapAligned16, CleanupMode.SkipInstanceRelease);
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

    private enum AllocationMode
    {
        StackAligned,
        HeapUnaligned,
        HeapAligned16,
        HeapAligned32,
        HeapAligned64,
    }

    private enum CleanupMode
    {
        None,
        Full,
        SkipInstanceRelease,
    }

    private static unsafe void RunDirectInstanceIntersect(AllocationMode allocationMode, CleanupMode cleanupMode)
    {
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        var version = (int)EmbreeAPI.rtcGetDeviceProperty(device, RTCDeviceProperty.Version);
        Console.WriteLine($"Embree Version: {version}");
        var rayHitSize = sizeof(RTCRayHit);
        var marshalSize = Marshal.SizeOf<RTCRayHit>();
        Console.WriteLine($"RTCRayHit sizeof: {rayHitSize}, Marshal.SizeOf: {marshalSize}");

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
        void* alignedPtrRaw = null;
        try
        {
            if (allocationMode == AllocationMode.HeapUnaligned)
            {
                rayhitPtr = Marshal.AllocHGlobal(rayHitSize);
                rayhit = (RTCRayHit*)rayhitPtr;
            }
            else if (allocationMode == AllocationMode.HeapAligned16 || allocationMode == AllocationMode.HeapAligned32 || allocationMode == AllocationMode.HeapAligned64)
            {
                var alignment = allocationMode switch
                {
                    AllocationMode.HeapAligned64 => 64u,
                    AllocationMode.HeapAligned32 => 32u,
                    _ => 16u
                };
                var size = RoundUpToAlignment((nuint)rayHitSize, alignment);
                alignedPtrRaw = NativeMemory.AlignedAlloc(size, alignment);
                if (alignedPtrRaw == null)
                    throw new InvalidOperationException("AlignedAlloc returned null.");
                rayhit = (RTCRayHit*)alignedPtrRaw;
            }
            else
            {
                byte* stackBuffer = stackalloc byte[rayHitSize + 15];
                IntPtr alignedPtr = new IntPtr(((long)stackBuffer + 15) & ~15L);
                rayhit = (RTCRayHit*)alignedPtr;
            }

            var mod16 = (uint)((nuint)rayhit & 15);
            var mod64 = (uint)((nuint)rayhit & 63);
            Console.WriteLine($"rayhit alignment: mod16={mod16}, mod64={mod64}");

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
            Console.WriteLine("Completed intersect call.");
        }
        finally
        {
            if (rayhitPtr != IntPtr.Zero)
            {
                Console.WriteLine("Freeing rayhit (Marshal.FreeHGlobal)...");
                Marshal.FreeHGlobal(rayhitPtr);
                Console.WriteLine("Freed rayhit (Marshal.FreeHGlobal).");
            }
            if (alignedPtrRaw != null)
            {
                Console.WriteLine("Freeing rayhit (NativeMemory.AlignedFree)...");
                NativeMemory.AlignedFree(alignedPtrRaw);
                Console.WriteLine("Freed rayhit (NativeMemory.AlignedFree).");
            }
        }

        if (cleanupMode == CleanupMode.Full)
        {
            Console.WriteLine("Cleanup: rtcReleaseGeometry(instance)");
            EmbreeAPI.rtcReleaseGeometry(instance);
            Console.WriteLine("Cleanup: rtcReleaseGeometry(geom)");
            EmbreeAPI.rtcReleaseGeometry(geom);
            Console.WriteLine("Cleanup: rtcReleaseScene(topScene)");
            EmbreeAPI.rtcReleaseScene(topScene);
            Console.WriteLine("Cleanup: rtcReleaseScene(sourceScene)");
            EmbreeAPI.rtcReleaseScene(sourceScene);
            Console.WriteLine("Cleanup: rtcReleaseDevice(device)");
            EmbreeAPI.rtcReleaseDevice(device);
        }
        else if (cleanupMode == CleanupMode.SkipInstanceRelease)
        {
            Console.WriteLine("Cleanup: rtcReleaseGeometry(geom)");
            EmbreeAPI.rtcReleaseGeometry(geom);
            Console.WriteLine("Cleanup: rtcReleaseScene(topScene)");
            EmbreeAPI.rtcReleaseScene(topScene);
            Console.WriteLine("Cleanup: rtcReleaseScene(sourceScene)");
            EmbreeAPI.rtcReleaseScene(sourceScene);
            Console.WriteLine("Cleanup: rtcReleaseDevice(device)");
            EmbreeAPI.rtcReleaseDevice(device);
        }
        else
        {
            Console.WriteLine("Cleanup skipped.");
        }
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

    private static nuint RoundUpToAlignment(nuint size, uint alignment)
    {
        var mask = (nuint)alignment - 1;
        return (size + mask) & ~mask;
    }
}
