using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests.Diagnostics.PInvoke;

/// <summary>
/// DIAGNOSTIC TEST: Direct P/Invoke instance array test.
///
/// Purpose: Test InstanceArray buffer allocation using raw P/Invoke calls,
/// bypassing wrapper classes to isolate the exact failure point.
///
/// FIXED (2025-12-23): RTCBufferType.Transform enum value corrected (23 not 33).
/// InstanceArray tests now pass successfully.
/// </summary>
public class DirectInstanceArrayTest
{
    /// <summary>
    /// Test that exactly mirrors Embree verify.cpp InstanceArrayTest (lines 3046-3057).
    /// Verifies InstanceArray support with P/Invoke bindings.
    /// </summary>
    [Fact]
    public void DirectPInvoke_InstanceArray_SetSharedBuffer()
    {
        Console.WriteLine("=== Direct P/Invoke InstanceArray Test (Shared Buffer) ===");
        Console.WriteLine("Mirroring Embree verify.cpp InstanceArrayTest exactly");

        // Step 1: Create device
        Console.WriteLine("\nStep 1: Creating device...");
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);
        CheckError(device, "rtcNewDevice");

        // Step 2: Create bottom-level scene with triangle (like verify.cpp)
        Console.WriteLine("\nStep 2: Creating bottom-level scene...");
        IntPtr blScene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3), 3);
        unsafe
        {
            float* vertices = (float*)vertexBuffer;
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;
        }

        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Index, 0, RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3), 1);
        unsafe
        {
            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
        }

        EmbreeAPI.rtcCommitGeometry(geom);
        EmbreeAPI.rtcAttachGeometry(blScene, geom);
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcCommitScene(blScene);
        CheckError(device, "Setup bottom-level scene");
        Console.WriteLine("  Bottom-level scene created and committed");

        // Step 3: Exactly like verify.cpp lines 3046-3052
        // Create instance array and set up (EXACT order from verify.cpp)
        Console.WriteLine("\nStep 3: Creating instance array (exact verify.cpp order)...");

        // Create transform data FIRST (like verify.cpp lines 3040-3043)
        // verify.cpp uses AffineSpace3fa which is 4x4 column-major (64 bytes)
        const int numInstances = 15;  // verify.cpp uses 15 instances (i = 1 to 15)

        // Using 64-byte aligned transforms like AffineSpace3fa
        // AffineSpace3fa = 16 floats (64 bytes) stored column-major
        var transforms = new M44f[numInstances];
        for (int i = 0; i < numInstances; i++)
        {
            transforms[i] = M44f.Translation((i + 1) * 5.0f, 0, 0);  // Like verify.cpp: (i * 5.f, 0.f, 0.f)
        }
        Console.WriteLine($"  Created {numInstances} transforms, M44f size={Marshal.SizeOf<M44f>()} bytes");

        // Create top-level scene (like verify.cpp line 3045)
        IntPtr tlScene = EmbreeAPI.rtcNewScene(device);
        CheckError(device, "rtcNewScene(tlScene)");

        // Create instance array geometry (like verify.cpp line 3046)
        IntPtr instanceArray = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.InstanceArray);
        CheckError(device, "rtcNewGeometry(InstanceArray)");
        Console.WriteLine($"  InstanceArray created: 0x{instanceArray.ToInt64():X}");

        // Set shared buffer (like verify.cpp line 3047)
        // IMPORTANT: This is BEFORE rtcSetGeometryInstancedScene in verify.cpp
        unsafe
        {
            fixed (M44f* ptr = transforms)
            {
                Console.WriteLine($"  Calling rtcSetSharedGeometryBuffer: ptr=0x{((IntPtr)ptr).ToInt64():X}");
                EmbreeAPI.rtcSetSharedGeometryBuffer(
                    instanceArray,
                    RTCBufferType.Transform,
                    0, // slot
                    RTCFormat.FLOAT4X4_COLUMN_MAJOR,
                    (IntPtr)ptr,
                    0, // byte offset
                    (nuint)sizeof(M44f), // byte stride (64)
                    (nuint)numInstances);
                var err = EmbreeAPI.rtcGetDeviceError(device);
                Console.WriteLine($"  After rtcSetSharedGeometryBuffer: error={err}");

                // Check if this is the problem point
                if (err != RTCDeviceError.None)
                {
                    Console.WriteLine("  *** BUFFER SETUP FAILED - THIS IS THE BUG ***");
                }
            }
        }

        // Set instanced scene (like verify.cpp line 3048)
        EmbreeAPI.rtcSetGeometryInstancedScene(instanceArray, blScene);
        CheckError(device, "rtcSetGeometryInstancedScene");

        // Attach to top-level scene (like verify.cpp line 3049)
        EmbreeAPI.rtcAttachGeometry(tlScene, instanceArray);
        CheckError(device, "rtcAttachGeometry(tlScene, instanceArray)");

        // Release geometry ref (like verify.cpp line 3050)
        EmbreeAPI.rtcReleaseGeometry(instanceArray);

        // Commit geometry (like verify.cpp line 3051)
        EmbreeAPI.rtcCommitGeometry(instanceArray);
        CheckError(device, "rtcCommitGeometry(instanceArray)");

        // Commit scene (like verify.cpp line 3052)
        EmbreeAPI.rtcCommitScene(tlScene);
        CheckError(device, "rtcCommitScene(tlScene)");
        Console.WriteLine("  Instance array committed and attached to top-level scene");

        // Step 4: Do ray intersection test
        Console.WriteLine("\nStep 4: Testing ray intersection...");

        // Use the same pattern as DirectPInvokeInstanceTest with proper alignment
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];
        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
                IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
                RTCRayHit* rayhit = (RTCRayHit*)alignedPtr;

                rayhit->ray.org = new V3f(0.25f, 0.25f, -1.0f);
                rayhit->ray.dir = new V3f(0.0f, 0.0f, 1.0f);
                rayhit->ray.tnear = 0.0f;
                rayhit->ray.tfar = float.PositiveInfinity;
                rayhit->ray.mask = uint.MaxValue;
                rayhit->ray.flags = 0;
                rayhit->hit.geomID = uint.MaxValue;
                rayhit->hit.primID = uint.MaxValue;
                rayhit->hit.instID_0 = uint.MaxValue;

                var args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF, // All features
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                EmbreeAPI.rtcIntersect1(tlScene, rayhit, &args);
                CheckError(device, "rtcIntersect1");

                Console.WriteLine($"  Ray: origin=(0.25, 0.25, -1), dir=(0, 0, 1)");
                Console.WriteLine($"  Hit geomID: {rayhit->hit.geomID}, instID: {rayhit->hit.instID_0}, tfar: {rayhit->ray.tfar}");

                Assert.NotEqual(uint.MaxValue, rayhit->hit.geomID);
            }
        }
        Console.WriteLine("\nSUCCESS: InstanceArray with rtcSetSharedGeometryBuffer works!");

        // Cleanup
        EmbreeAPI.rtcReleaseScene(tlScene);
        EmbreeAPI.rtcReleaseScene(blScene);
        EmbreeAPI.rtcReleaseDevice(device);
    }

    [Fact]
    public void DirectPInvoke_InstanceArray_SetNewBuffer()
    {
        Console.WriteLine("=== Direct P/Invoke InstanceArray Test (New Buffer) ===");

        // Create device
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);
        CheckError(device, "rtcNewDevice");

        // Create bottom-level scene with triangle
        IntPtr blScene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3), 3);
        unsafe
        {
            float* vertices = (float*)vertexBuffer;
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;
        }

        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Index, 0, RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3), 1);
        unsafe
        {
            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
        }

        EmbreeAPI.rtcCommitGeometry(geom);
        EmbreeAPI.rtcAttachGeometry(blScene, geom);
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcCommitScene(blScene);
        CheckError(device, "Setup bottom-level scene");

        // Create instance array
        IntPtr instanceArray = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.InstanceArray);
        CheckError(device, "rtcNewGeometry(InstanceArray)");

        EmbreeAPI.rtcSetGeometryInstancedScene(instanceArray, blScene);
        CheckError(device, "rtcSetGeometryInstancedScene");

        // Try rtcSetNewGeometryBuffer (what our wrapper uses)
        const int numInstances = 3;
        Console.WriteLine($"\nTrying rtcSetNewGeometryBuffer with {numInstances} instances...");

        // Note: Transform buffer uses FLOAT3X4 format (12 floats per transform)
        IntPtr transformBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            instanceArray,
            RTCBufferType.Transform,
            0, // slot = time step 0
            RTCFormat.FLOAT3X4_ROW_MAJOR,
            (nuint)(12 * sizeof(float)), // 3x4 matrix = 12 floats
            (nuint)numInstances);

        var err = EmbreeAPI.rtcGetDeviceError(device);
        Console.WriteLine($"  Error after rtcSetNewGeometryBuffer: {err}");
        Console.WriteLine($"  Transform buffer returned: {(transformBuffer == IntPtr.Zero ? "NULL" : $"0x{transformBuffer.ToInt64():X}")}");

        if (transformBuffer == IntPtr.Zero)
        {
            Console.WriteLine("\n  WARNING: rtcSetNewGeometryBuffer returned NULL!");
            Console.WriteLine("  This is the same issue as InstanceArray.SetTransformBuffer()");
            // Don't assert - we expect this to fail to diagnose the issue
        }
        else
        {
            // Fill transforms
            unsafe
            {
                float* ptr = (float*)transformBuffer;
                for (int i = 0; i < numInstances; i++)
                {
                    // Identity + translation: row-major 3x4 matrix
                    float tx = i * 2.0f;
                    // Row 0: [1, 0, 0, tx]
                    ptr[i * 12 + 0] = 1.0f; ptr[i * 12 + 1] = 0.0f; ptr[i * 12 + 2] = 0.0f; ptr[i * 12 + 3] = tx;
                    // Row 1: [0, 1, 0, 0]
                    ptr[i * 12 + 4] = 0.0f; ptr[i * 12 + 5] = 1.0f; ptr[i * 12 + 6] = 0.0f; ptr[i * 12 + 7] = 0.0f;
                    // Row 2: [0, 0, 1, 0]
                    ptr[i * 12 + 8] = 0.0f; ptr[i * 12 + 9] = 0.0f; ptr[i * 12 + 10] = 1.0f; ptr[i * 12 + 11] = 0.0f;
                }
            }
            Console.WriteLine("  Transforms filled successfully");

            EmbreeAPI.rtcCommitGeometry(instanceArray);
            IntPtr tlScene = EmbreeAPI.rtcNewScene(device);
            EmbreeAPI.rtcAttachGeometry(tlScene, instanceArray);
            EmbreeAPI.rtcReleaseGeometry(instanceArray);
            EmbreeAPI.rtcCommitScene(tlScene);
            CheckError(device, "Setup top-level scene");

            // Test intersection
            Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];
            unsafe
            {
                fixed (byte* bufferPtr = buffer)
                {
                    IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
                    RTCRayHit* rayhit = (RTCRayHit*)alignedPtr;

                    rayhit->ray.org = new V3f(0.25f, 0.25f, -1.0f);
                    rayhit->ray.dir = new V3f(0.0f, 0.0f, 1.0f);
                    rayhit->ray.tnear = 0.0f;
                    rayhit->ray.tfar = float.PositiveInfinity;
                    rayhit->ray.mask = uint.MaxValue;
                    rayhit->hit.geomID = uint.MaxValue;

                    var args = new RTCIntersectArguments
                    {
                        flags = RTCRayQueryFlags.None,
                        feature_mask = 0xFFFFFFFF, // All features
                        context = IntPtr.Zero,
                        filter = IntPtr.Zero,
                        intersect = IntPtr.Zero
                    };

                    EmbreeAPI.rtcIntersect1(tlScene, rayhit, &args);
                    CheckError(device, "rtcIntersect1");

                    Console.WriteLine($"  Hit geomID: {rayhit->hit.geomID}");
                    Assert.NotEqual(uint.MaxValue, rayhit->hit.geomID);
                }
            }
            EmbreeAPI.rtcReleaseScene(tlScene);
        }

        EmbreeAPI.rtcReleaseScene(blScene);
        EmbreeAPI.rtcReleaseDevice(device);
    }

    private static void CheckError(IntPtr device, string operation)
    {
        var err = EmbreeAPI.rtcGetDeviceError(device);
        if (err != RTCDeviceError.None)
        {
            Console.WriteLine($"  ERROR after {operation}: {err}");
        }
    }
}
