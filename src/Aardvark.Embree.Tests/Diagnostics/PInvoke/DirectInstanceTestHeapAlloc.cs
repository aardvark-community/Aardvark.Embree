using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// DIAGNOSTIC TEST: Heap allocation variant of direct P/Invoke test.
///
/// Purpose: Rules out stack alignment or allocation issues by using heap-allocated
/// structures instead of stackalloc. If stackalloc version fails but heap version
/// passes, indicates stack alignment problem.
///
/// Method: Uses 32-byte aligned heap allocation for RTCRayHit.
///
/// When to use:
/// - When suspecting stack alignment issues
/// - When stackalloc-based tests produce unexpected results
/// - To verify aligned heap allocation behavior
///
/// Historical context:
/// Created during Phase 2C instance geometry investigation (Nov 2025) to rule out
/// stack alignment as the cause of failures. This test PASSED, proving memory
/// allocation method was not the issue.
///
/// Result: Both stackalloc and aligned heap allocation worked, confirming bug was elsewhere
/// (specifically in Scene.Intersect() flag configuration).
///
/// See also:
/// - DirectPInvokeInstanceTest.cs for stackalloc version
/// - docs/DEBUGGING_METHODOLOGY.md section on memory layout debugging
/// </summary>
public class DirectInstanceTestHeapAlloc
{
    private static void LogDeviceError(IntPtr device, string label)
    {
        var err = EmbreeAPI.rtcGetDeviceError(device);
        var detailPtr = EmbreeAPI.rtcGetDeviceLastErrorMessage(device);
        var detail = detailPtr != IntPtr.Zero ? Marshal.PtrToStringAnsi(detailPtr) : null;

        Console.WriteLine($"[{label}] DeviceError={err}");
        if (!string.IsNullOrWhiteSpace(detail))
            Console.WriteLine($"[{label}] ErrorDetail={detail}");
    }

    [Fact]
    public unsafe void DirectPInvoke_InstanceGeometry_HeapAllocated()
    {
        Console.WriteLine("=== Direct P/Invoke Instance Test (Heap Allocated) ===");

        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        var rayHitSize = sizeof(RTCRayHit);
        var marshalSize = Marshal.SizeOf<RTCRayHit>();
        Console.WriteLine($"RTCRayHit sizeof: {rayHitSize}, Marshal.SizeOf: {marshalSize}");
        var version = (int)EmbreeAPI.rtcGetDeviceProperty(device, RTCDeviceProperty.Version);
        Console.WriteLine($"Embree Version: {version}");
        IntPtr sourceScene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        unsafe
        {
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

            // HEAP ALLOCATE RTCRayHit (aligned)
            IntPtr rayhitPtr = EmbreeMemory.AllocRayHit(out var allocationSize, out var alignment);
            try
            {
                RTCRayHit* rayhit = (RTCRayHit*)rayhitPtr;
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
                Console.WriteLine($"rayhit allocated at: 0x{((nint)rayhit):X}");
                var mod16 = (uint)((nuint)rayhit & 15);
                var mod64 = (uint)((nuint)rayhit & 63);
                Console.WriteLine($"rayhit alignment: mod16={mod16}, mod64={mod64}, allocationSize={allocationSize}, alignment={alignment}");
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

                Assert.True(hit, "Heap-allocated instance test MUST hit");
                Assert.InRange(rayhit->ray.tfar, 0.99f, 1.01f);
            }
            finally
            {
                Console.WriteLine("Freeing rayhit (EmbreeMemory.FreeAligned)...");
                EmbreeMemory.FreeAligned(rayhitPtr);
                Console.WriteLine("Freed rayhit (EmbreeMemory.FreeAligned).");
            }

            // Cleanup
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
    }

}
