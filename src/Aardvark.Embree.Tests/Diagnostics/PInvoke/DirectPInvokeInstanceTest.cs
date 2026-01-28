using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// DIAGNOSTIC TEST: Direct P/Invoke instance geometry test.
///
/// Purpose: Proves P/Invoke bindings and marshalling are correct by exactly replicating
/// the C++ baseline test (embree_instance_test.cpp) using only raw P/Invoke calls.
///
/// Bypasses: ALL wrapper classes (Scene, InstanceGeometry, Device, etc.)
///
/// When to use:
/// - When wrapper-based instance geometry tests fail
/// - To isolate whether bug is in P/Invoke bindings vs wrapper implementation
/// - As reference for correct P/Invoke usage
///
/// Historical context:
/// Created during Phase 2C instance geometry investigation (Nov 2025).
/// This test PASSED while wrapper tests FAILED, proving the bug was in Scene.Intersect()
/// wrapper method, not in P/Invoke bindings or InstanceGeometry class.
///
/// Bug found: Scene.Intersect() was using RTCRayQueryFlags.Incoherent instead of None.
///
/// See also:
/// - docs/DEBUGGING_METHODOLOGY.md for multi-layer debugging strategy
/// - Diagnostics/Native/embree_instance_test.cpp for C++ baseline
/// - Diagnostics/README.md for case study details
/// </summary>
public class DirectPInvokeInstanceTest
{
    [Fact]
    public void DirectPInvoke_InstanceGeometry_ExactCppMatch()
    {
        Console.WriteLine("=== Direct P/Invoke Instance Test (Exact C++ Match) ===");

        // Step 1: Create device
        Console.WriteLine("Step 1: Creating device...");
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);
        Console.WriteLine($"  Device created: 0x{device.ToInt64():X}");

        // Step 2: Create source scene with triangle
        Console.WriteLine("\nStep 2: Creating source scene with triangle...");
        IntPtr sourceScene = EmbreeAPI.rtcNewScene(device);
        Assert.NotEqual(IntPtr.Zero, sourceScene);
        Console.WriteLine($"  Source scene created: 0x{sourceScene.ToInt64():X}");

        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);
        Assert.NotEqual(IntPtr.Zero, geom);
        Console.WriteLine($"  Triangle geometry created: 0x{geom.ToInt64():X}");

        // Set vertices
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.Vertex,
            0,
            RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3),
            3
        );
        Assert.NotEqual(IntPtr.Zero, vertexBuffer);
        unsafe
        {
            float* vertices = (float*)vertexBuffer;
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;  // v0
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;  // v1
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;  // v2
            Console.WriteLine("  Vertices set: (0,0,0), (1,0,0), (0,1,0)");
        }

        // Set indices
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.Index,
            0,
            RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3),
            1
        );
        Assert.NotEqual(IntPtr.Zero, indexBuffer);
        unsafe
        {
            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            Console.WriteLine("  Indices set: 0, 1, 2");
        }

        // Commit geometry
        EmbreeAPI.rtcCommitGeometry(geom);
        Console.WriteLine("  Geometry committed");

        // Attach to source scene
        uint geomID = EmbreeAPI.rtcAttachGeometry(sourceScene, geom);
        Console.WriteLine($"  Geometry attached to source scene (geomID={geomID})");

        // Commit source scene
        EmbreeAPI.rtcCommitScene(sourceScene);
        Console.WriteLine("  Source scene committed");

        // Step 3: Create instance geometry
        Console.WriteLine("\nStep 3: Creating instance geometry...");
        IntPtr instance = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Instance);
        Assert.NotEqual(IntPtr.Zero, instance);
        Console.WriteLine($"  Instance geometry created: 0x{instance.ToInt64():X}");

        // Link instance to source scene
        EmbreeAPI.rtcSetGeometryInstancedScene(instance, sourceScene);
        Console.WriteLine("  Instance linked to source scene");

        // Set time step count
        EmbreeAPI.rtcSetGeometryTimeStepCount(instance, 1);
        Console.WriteLine("  Time step count set to 1");

        // Set identity transform (3x4 row-major)
        unsafe
        {
            float* transform = stackalloc float[12];
            transform[0] = 1.0f; transform[1] = 0.0f; transform[2] = 0.0f; transform[3] = 0.0f;
            transform[4] = 0.0f; transform[5] = 1.0f; transform[6] = 0.0f; transform[7] = 0.0f;
            transform[8] = 0.0f; transform[9] = 0.0f; transform[10] = 1.0f; transform[11] = 0.0f;
            EmbreeAPI.rtcSetGeometryTransform(instance, 0, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)transform);
            Console.WriteLine("  Identity transform set");
        }

        // Commit instance
        EmbreeAPI.rtcCommitGeometry(instance);
        Console.WriteLine("  Instance geometry committed");

        // Step 4: Create top-level scene with instance
        Console.WriteLine("\nStep 4: Creating top-level scene...");
        IntPtr topScene = EmbreeAPI.rtcNewScene(device);
        Assert.NotEqual(IntPtr.Zero, topScene);
        Console.WriteLine($"  Top scene created: 0x{topScene.ToInt64():X}");

        uint instID = EmbreeAPI.rtcAttachGeometry(topScene, instance);
        Console.WriteLine($"  Instance attached to top scene (instID={instID})");

        EmbreeAPI.rtcCommitScene(topScene);
        Console.WriteLine("  Top scene committed");

        // Step 5: Test ray intersection
        Console.WriteLine("\nStep 5: Testing ray intersection...");

        // Allocate 16-byte aligned memory for RTCRayHit (required by Embree 4 SIMD)
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];

        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
                // Align to 16-byte boundary
                IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
                RTCRayHit* rayhit = (RTCRayHit*)alignedPtr;

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

                Console.WriteLine($"  Ray: origin=({rayhit->ray.org}), direction=({rayhit->ray.dir})");

                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                EmbreeAPI.rtcIntersect1(topScene, rayhit, &args);
                Console.WriteLine("  rtcIntersect1 called");

                Console.WriteLine($"\n  Results:");
                Console.WriteLine($"    geomID: {rayhit->hit.geomID}");
                Console.WriteLine($"    primID: {rayhit->hit.primID}");
                Console.WriteLine($"    instID[0]: {rayhit->hit.instID_0}");
                Console.WriteLine($"    tfar: {rayhit->ray.tfar}");

                bool hit = rayhit->hit.geomID != unchecked((uint)-1);
                Console.WriteLine($"\n=== RESULT: {(hit ? "SUCCESS" : "FAILED")} ===");

                if (hit)
                {
                    Console.WriteLine($"  Hit distance: {rayhit->ray.tfar}");
                    Console.WriteLine($"  Expected: ~1.0 (ray from z=1 to z=0)");
                }

                // ASSERT: This MUST work because C++ test works
                Assert.True(hit, "Direct P/Invoke instance geometry MUST intersect (C++ works!)");
                Assert.InRange(rayhit->ray.tfar, 0.99f, 1.01f);
            }
        }

        // Cleanup
        EmbreeAPI.rtcReleaseGeometry(instance);
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcReleaseScene(topScene);
        EmbreeAPI.rtcReleaseScene(sourceScene);
        EmbreeAPI.rtcReleaseDevice(device);
    }

    [Fact]
    public void WrapperAPI_InstanceGeometry_CompareToDirect()
    {
        Console.WriteLine("\n=== Wrapper API Instance Test (For Comparison) ===");

        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        Console.WriteLine("Step 1: Creating triangle geometry via wrapper...");
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        Console.WriteLine($"  Triangle geometry handle: 0x{triangleGeom.Handle.ToInt64():X}");

        Console.WriteLine("\nStep 2: Creating InstanceGeometry via wrapper...");
        Console.WriteLine($"  Triangle geometry committed: {triangleGeom.Handle != IntPtr.Zero}");
        using var instance = new InstanceGeometry(device, triangleGeom, Affine3f.Identity, RTCBuildQuality.High);
        Console.WriteLine($"  Instance geometry handle: 0x{instance.Handle.ToInt64():X}");

        Console.WriteLine("\nStep 3: Attaching InstanceGeometry to scene and committing...");
        scene.AttachGeometry(instance);
        scene.Commit();

        Console.WriteLine("\nStep 4: Testing ray intersection...");
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.25f, 0.25f, 1f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Console.WriteLine($"  Intersected: {intersected}");
        Console.WriteLine($"  Hit T: {hit.T}");
        Console.WriteLine($"  GeomID: {hit.GeometryId}, PrimID: {hit.PrimitiveId}, InstID: {hit.InstanceId}");

        Console.WriteLine($"\n=== RESULT: {(intersected ? "SUCCESS" : "FAILED")} ===");

        Assert.True(intersected, "Wrapper API instance geometry should intersect");
    }
}
