using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// DIAGNOSTIC TEST: Direct P/Invoke test for rtcIntersect16 with heap allocation.
///
/// Purpose: Isolate whether Intersect16 crashes are due to:
/// - Stack allocation (stackalloc) vs heap allocation (Marshal.AllocHGlobal)
/// - Wrapper layer logic vs direct P/Invoke
/// - Alignment issues in buffer allocation
///
/// Method:
/// - Uses direct P/Invoke calls (NOT wrapper classes)
/// - Allocates RTCRayHit16 on heap using Marshal.AllocHGlobal
/// - Tests exact scenario: 16 rays with alternating hit/miss pattern
/// - Runs test 10 times in a loop to check reproducibility
/// - Prints detailed diagnostics before and after rtcIntersect16
///
/// Expected outcome:
/// - If this test passes consistently but wrapper test fails: bug is in wrapper layer
/// - If this test crashes: bug is in P/Invoke bindings or native library
/// - If results are non-deterministic: memory corruption or alignment issue
///
/// Historical context:
/// Created during Phase 2D (Nov 2025) to investigate Intersect16 crash regression.
/// The wrapper test (Intersect16_MixedHitMiss_ReturnsCorrectResults) was crashing
/// intermittently. This test isolates the native layer to rule out wrapper issues.
///
/// See also:
/// - RayPacketIntegrationTests.cs:Intersect16_MixedHitMiss_ReturnsCorrectResults
/// - Scene.RayPackets.cs:Intersect16
/// - Native/embree_intersect16_test.cpp (C++ baseline)
/// </summary>
public class DirectIntersect16Test
{
    [Fact]
    public void DirectPInvoke_Intersect16_HeapAllocated_10Iterations()
    {
        Console.WriteLine("=== Direct P/Invoke Intersect16 Test (Heap Allocated) ===");
        Console.WriteLine("Testing 16 rays: 8 hit, 8 miss - 10 iterations");
        Console.WriteLine();

        int passCount = 0;
        int failCount = 0;

        for (int iteration = 0; iteration < 10; iteration++)
        {
            Console.WriteLine($"--- Iteration {iteration + 1}/10 ---");

            bool success = RunSingleIteration();

            if (success)
            {
                passCount++;
                Console.WriteLine($"Iteration {iteration + 1}: PASS");
            }
            else
            {
                failCount++;
                Console.WriteLine($"Iteration {iteration + 1}: FAIL");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"=== Summary: {passCount} passes, {failCount} fails ===");
        Assert.Equal(10, passCount);
    }

    /// <summary>
    /// Test 1: Delayed Cleanup - After rtcIntersect16, trigger GC before cleanup
    /// </summary>
    [Fact]
    public void Test1_DelayedCleanup_GCBeforeRelease()
    {
        Console.WriteLine("=== Test 1: Delayed Cleanup (GC before release) ===");
        Console.WriteLine("Testing 3 iterations");
        Console.WriteLine();

        int passCount = 0;
        int crashCount = 0;

        for (int iteration = 0; iteration < 3; iteration++)
        {
            Console.WriteLine($"--- Iteration {iteration + 1}/3 ---");

            try
            {
                bool success = RunDelayedCleanup();
                if (success)
                {
                    passCount++;
                    Console.WriteLine($"Iteration {iteration + 1}: PASS");
                }
            }
            catch (Exception ex)
            {
                crashCount++;
                Console.WriteLine($"Iteration {iteration + 1}: CRASH - {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"=== Summary: {passCount} passes, {crashCount} crashes ===");
        Assert.Equal(3, passCount);
    }

    /// <summary>
    /// Test 2: No Cleanup - Never release resources, let process exit handle it
    /// </summary>
    [Fact]
    public void Test2_NoCleanup_NeverReleaseResources()
    {
        Console.WriteLine("=== Test 2: No Cleanup (Never release resources) ===");
        Console.WriteLine("Testing 3 iterations");
        Console.WriteLine();

        int passCount = 0;
        int crashCount = 0;

        for (int iteration = 0; iteration < 3; iteration++)
        {
            Console.WriteLine($"--- Iteration {iteration + 1}/3 ---");

            try
            {
                bool success = RunNoCleanup();
                if (success)
                {
                    passCount++;
                    Console.WriteLine($"Iteration {iteration + 1}: PASS");
                }
            }
            catch (Exception ex)
            {
                crashCount++;
                Console.WriteLine($"Iteration {iteration + 1}: CRASH - {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"=== Summary: {passCount} passes, {crashCount} crashes ===");
        Assert.Equal(3, passCount);
    }

    /// <summary>
    /// Test 3: Buffer Overrun Check - Detect if Embree writes beyond buffer
    /// </summary>
    [Fact]
    public void Test3_BufferOverrunCheck_SentinelValues()
    {
        Console.WriteLine("=== Test 3: Buffer Overrun Check (Sentinel values) ===");
        Console.WriteLine("Testing 3 iterations");
        Console.WriteLine();

        int passCount = 0;
        int overrunCount = 0;

        for (int iteration = 0; iteration < 3; iteration++)
        {
            Console.WriteLine($"--- Iteration {iteration + 1}/3 ---");

            try
            {
                bool success = RunBufferOverrunCheck(out bool overrunDetected);
                if (overrunDetected)
                {
                    overrunCount++;
                    Console.WriteLine($"Iteration {iteration + 1}: BUFFER OVERRUN DETECTED!");
                }
                else if (success)
                {
                    passCount++;
                    Console.WriteLine($"Iteration {iteration + 1}: PASS (no overrun)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Iteration {iteration + 1}: CRASH - {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine($"=== Summary: {passCount} passes (no overrun), {overrunCount} buffer overruns ===");
        Assert.Equal(0, overrunCount); // Fail if any overrun detected
    }

    private unsafe bool RunSingleIteration()
    {
        // Create device
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        if (device == IntPtr.Zero)
        {
            Console.WriteLine("ERROR: Failed to create device");
            return false;
        }

        // Create scene
        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        if (scene == IntPtr.Zero)
        {
            Console.WriteLine("ERROR: Failed to create scene");
            EmbreeAPI.rtcReleaseDevice(device);
            return false;
        }

        // Create triangle geometry
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);
        if (geom == IntPtr.Zero)
        {
            Console.WriteLine("ERROR: Failed to create geometry");
            EmbreeAPI.rtcReleaseScene(scene);
            EmbreeAPI.rtcReleaseDevice(device);
            return false;
        }

        // Set vertex buffer: triangle at Z=0
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3), 3
        );
        float* vertices = (float*)vertexBuffer;
        // Triangle: (-1, -1, 0), (1, -1, 0), (0, 1, 0)
        vertices[0] = -1.0f; vertices[1] = -1.0f; vertices[2] = 0.0f;
        vertices[3] = 1.0f; vertices[4] = -1.0f; vertices[5] = 0.0f;
        vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;

        // Set index buffer
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Index, 0, RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3), 1
        );
        uint* indices = (uint*)indexBuffer;
        indices[0] = 0; indices[1] = 1; indices[2] = 2;

        // Commit geometry
        EmbreeAPI.rtcCommitGeometry(geom);

        // Attach geometry to scene
        EmbreeAPI.rtcAttachGeometry(scene, geom);

        // Commit scene
        EmbreeAPI.rtcCommitScene(scene);

        Console.WriteLine("Scene setup complete");

        // Allocate RTCRayHit16 on heap with 64-byte alignment
        int structSize = Marshal.SizeOf<RTCRayHit16>();
        int alignment = 64;
        IntPtr rawPtr = Marshal.AllocHGlobal(structSize + alignment);

        try
        {
            // Align pointer to 64-byte boundary
            long address = rawPtr.ToInt64();
            long aligned = (address + alignment - 1) & ~(alignment - 1);
            IntPtr alignedPtr = new IntPtr(aligned);
            RTCRayHit16* rayhit = (RTCRayHit16*)alignedPtr;

            Console.WriteLine($"RTCRayHit16 allocated at: 0x{aligned:X} (alignment: {aligned % 64})");
            Console.WriteLine($"  Size: {structSize} bytes");

            // Initialize rays: alternating hit/miss pattern
            // Same as RayPacketIntegrationTests.cs:Intersect16_MixedHitMiss_ReturnsCorrectResults
            var hitMissOrigins = new[]
            {
                new V3f(0, 0, 2),          // 0: Hit - center
                new V3f(5, 5, 2),          // 1: Miss - far away
                new V3f(-0.3f, -0.3f, 2),  // 2: Hit - inside
                new V3f(10, 0, 2),         // 3: Miss - far right
                new V3f(0.2f, 0.0f, 2),    // 4: Hit - center-right
                new V3f(-10, -10, 2),      // 5: Miss - far left-down
                new V3f(-0.2f, 0.0f, 2),   // 6: Hit - center-left
                new V3f(0, 20, 2),         // 7: Miss - far up
                new V3f(0.1f, 0.1f, 2),    // 8: Hit - center
                new V3f(15, 15, 2),        // 9: Miss - far away
                new V3f(-0.4f, -0.4f, 2),  // 10: Hit - inside
                new V3f(-20, 0, 2),        // 11: Miss - far left
                new V3f(0.3f, -0.1f, 2),   // 12: Hit - center-right
                new V3f(0, -30, 2),        // 13: Miss - far down
                new V3f(-0.1f, 0.2f, 2),   // 14: Hit - center-left
                new V3f(25, 0, 2)          // 15: Miss - far right
            };

            for (int i = 0; i < 16; i++)
            {
                rayhit->ray.org_x[i] = hitMissOrigins[i].X;
                rayhit->ray.org_y[i] = hitMissOrigins[i].Y;
                rayhit->ray.org_z[i] = hitMissOrigins[i].Z;
                rayhit->ray.tnear[i] = 0.0f;

                rayhit->ray.dir_x[i] = 0.0f;
                rayhit->ray.dir_y[i] = 0.0f;
                rayhit->ray.dir_z[i] = -1.0f;  // All pointing down
                rayhit->ray.time[i] = 0.0f;

                rayhit->ray.tfar[i] = float.MaxValue;
                rayhit->ray.mask[i] = 0xFFFFFFFF;
                rayhit->ray.id[i] = (uint)i;
                rayhit->ray.flags[i] = 0;

                // Initialize hit to invalid
                rayhit->hit.geomID[i] = unchecked((uint)-1);
                rayhit->hit.primID[i] = unchecked((uint)-1);
                rayhit->hit.instID[i] = unchecked((uint)-1);
            }

            Console.WriteLine("Ray data initialized");

            // Setup validity mask with 64-byte alignment
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 16 + 63];
            fixed (byte* validBufferPtr = validBuffer)
            {
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 63) & ~63L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 16; i++)
                    valid[i] = -1;

                // Setup intersect arguments
                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.Incoherent,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                Console.WriteLine("Calling rtcIntersect16...");

                // THE CRITICAL CALL
                EmbreeAPI.rtcIntersect16(valid, scene, rayhit, &args);

                Console.WriteLine("rtcIntersect16 returned successfully!");
            }

            // Check results
            int hitCount = 0;
            int missCount = 0;
            bool[] expectedHits = { true, false, true, false, true, false, true, false,
                                   true, false, true, false, true, false, true, false };

            bool allCorrect = true;

            for (int i = 0; i < 16; i++)
            {
                bool rayHit = rayhit->hit.geomID[i] != unchecked((uint)-1);

                if (rayHit != expectedHits[i])
                {
                    Console.WriteLine($"  Ray {i}: WRONG - expected {(expectedHits[i] ? "HIT" : "MISS")}, got {(rayHit ? "HIT" : "MISS")}");
                    allCorrect = false;
                }

                if (rayHit)
                {
                    hitCount++;
                }
                else
                {
                    missCount++;
                }
            }

            Console.WriteLine($"Results: {hitCount} hits, {missCount} misses");

            if (hitCount != 8 || missCount != 8)
            {
                Console.WriteLine($"ERROR: Expected 8 hits and 8 misses");
                allCorrect = false;
            }

            // Cleanup
            EmbreeAPI.rtcReleaseGeometry(geom);
            EmbreeAPI.rtcReleaseScene(scene);
            EmbreeAPI.rtcReleaseDevice(device);

            return allCorrect;
        }
        finally
        {
            // Free heap-allocated RTCRayHit16
            Marshal.FreeHGlobal(rawPtr);
        }
    }

    /// <summary>
    /// Test 1 Implementation: Delayed cleanup with GC before release
    /// </summary>
    private unsafe bool RunDelayedCleanup()
    {
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set vertex buffer
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3), 3
        );
        float* vertices = (float*)vertexBuffer;
        vertices[0] = -1.0f; vertices[1] = -1.0f; vertices[2] = 0.0f;
        vertices[3] = 1.0f; vertices[4] = -1.0f; vertices[5] = 0.0f;
        vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;

        // Set index buffer
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Index, 0, RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3), 1
        );
        uint* indices = (uint*)indexBuffer;
        indices[0] = 0; indices[1] = 1; indices[2] = 2;

        EmbreeAPI.rtcCommitGeometry(geom);
        EmbreeAPI.rtcAttachGeometry(scene, geom);
        EmbreeAPI.rtcCommitScene(scene);

        Console.WriteLine("Scene setup complete");

        // Allocate RTCRayHit16
        int structSize = Marshal.SizeOf<RTCRayHit16>();
        int alignment = 64;
        IntPtr rawPtr = Marshal.AllocHGlobal(structSize + alignment);

        try
        {
            long address = rawPtr.ToInt64();
            long aligned = (address + alignment - 1) & ~(alignment - 1);
            IntPtr alignedPtr = new IntPtr(aligned);
            RTCRayHit16* rayhit = (RTCRayHit16*)alignedPtr;

            // Initialize rays (simple pattern: center rays hit)
            for (int i = 0; i < 16; i++)
            {
                rayhit->ray.org_x[i] = 0.0f;
                rayhit->ray.org_y[i] = 0.0f;
                rayhit->ray.org_z[i] = 2.0f;
                rayhit->ray.tnear[i] = 0.0f;

                rayhit->ray.dir_x[i] = 0.0f;
                rayhit->ray.dir_y[i] = 0.0f;
                rayhit->ray.dir_z[i] = -1.0f;
                rayhit->ray.time[i] = 0.0f;

                rayhit->ray.tfar[i] = float.MaxValue;
                rayhit->ray.mask[i] = 0xFFFFFFFF;
                rayhit->ray.id[i] = (uint)i;
                rayhit->ray.flags[i] = 0;

                rayhit->hit.geomID[i] = unchecked((uint)-1);
                rayhit->hit.primID[i] = unchecked((uint)-1);
                rayhit->hit.instID[i] = unchecked((uint)-1);
            }

            // Setup validity mask
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 16 + 63];
            fixed (byte* validBufferPtr = validBuffer)
            {
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 63) & ~63L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 16; i++)
                    valid[i] = -1;

                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.Incoherent,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                Console.WriteLine("Calling rtcIntersect16...");
                EmbreeAPI.rtcIntersect16(valid, scene, rayhit, &args);
                Console.WriteLine("rtcIntersect16 returned successfully!");
            }

            // DELAYED CLEANUP: Trigger GC before releasing resources
            Console.WriteLine("Triggering GC before cleanup...");
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Console.WriteLine("GC complete, now releasing resources in order...");

            // Release in explicit order: geometry → scene → device
            Console.WriteLine("Releasing geometry...");
            EmbreeAPI.rtcReleaseGeometry(geom);

            Console.WriteLine("Releasing scene...");
            EmbreeAPI.rtcReleaseScene(scene);

            Console.WriteLine("Releasing device...");
            EmbreeAPI.rtcReleaseDevice(device);

            Console.WriteLine("All resources released successfully");
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(rawPtr);
        }
    }

    /// <summary>
    /// Test 2 Implementation: No cleanup - never release resources
    /// </summary>
    private unsafe bool RunNoCleanup()
    {
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set vertex buffer
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3), 3
        );
        float* vertices = (float*)vertexBuffer;
        vertices[0] = -1.0f; vertices[1] = -1.0f; vertices[2] = 0.0f;
        vertices[3] = 1.0f; vertices[4] = -1.0f; vertices[5] = 0.0f;
        vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;

        // Set index buffer
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Index, 0, RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3), 1
        );
        uint* indices = (uint*)indexBuffer;
        indices[0] = 0; indices[1] = 1; indices[2] = 2;

        EmbreeAPI.rtcCommitGeometry(geom);
        EmbreeAPI.rtcAttachGeometry(scene, geom);
        EmbreeAPI.rtcCommitScene(scene);

        Console.WriteLine("Scene setup complete");

        // Allocate RTCRayHit16
        int structSize = Marshal.SizeOf<RTCRayHit16>();
        int alignment = 64;
        IntPtr rawPtr = Marshal.AllocHGlobal(structSize + alignment);

        try
        {
            long address = rawPtr.ToInt64();
            long aligned = (address + alignment - 1) & ~(alignment - 1);
            IntPtr alignedPtr = new IntPtr(aligned);
            RTCRayHit16* rayhit = (RTCRayHit16*)alignedPtr;

            // Initialize rays
            for (int i = 0; i < 16; i++)
            {
                rayhit->ray.org_x[i] = 0.0f;
                rayhit->ray.org_y[i] = 0.0f;
                rayhit->ray.org_z[i] = 2.0f;
                rayhit->ray.tnear[i] = 0.0f;

                rayhit->ray.dir_x[i] = 0.0f;
                rayhit->ray.dir_y[i] = 0.0f;
                rayhit->ray.dir_z[i] = -1.0f;
                rayhit->ray.time[i] = 0.0f;

                rayhit->ray.tfar[i] = float.MaxValue;
                rayhit->ray.mask[i] = 0xFFFFFFFF;
                rayhit->ray.id[i] = (uint)i;
                rayhit->ray.flags[i] = 0;

                rayhit->hit.geomID[i] = unchecked((uint)-1);
                rayhit->hit.primID[i] = unchecked((uint)-1);
                rayhit->hit.instID[i] = unchecked((uint)-1);
            }

            // Setup validity mask
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 16 + 63];
            fixed (byte* validBufferPtr = validBuffer)
            {
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 63) & ~63L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 16; i++)
                    valid[i] = -1;

                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.Incoherent,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                Console.WriteLine("Calling rtcIntersect16...");
                EmbreeAPI.rtcIntersect16(valid, scene, rayhit, &args);
                Console.WriteLine("rtcIntersect16 returned successfully!");
            }

            // NO CLEANUP - just return
            Console.WriteLine("Test complete - NOT releasing any resources (testing if cleanup causes crash)");
            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(rawPtr);
        }
        // Note: device, scene, geom are deliberately leaked
    }

    /// <summary>
    /// Test 3 Implementation: Buffer overrun detection with sentinel values
    /// </summary>
    private unsafe bool RunBufferOverrunCheck(out bool overrunDetected)
    {
        overrunDetected = false;

        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set vertex buffer
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3), 3
        );
        float* vertices = (float*)vertexBuffer;
        vertices[0] = -1.0f; vertices[1] = -1.0f; vertices[2] = 0.0f;
        vertices[3] = 1.0f; vertices[4] = -1.0f; vertices[5] = 0.0f;
        vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;

        // Set index buffer
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Index, 0, RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3), 1
        );
        uint* indices = (uint*)indexBuffer;
        indices[0] = 0; indices[1] = 1; indices[2] = 2;

        EmbreeAPI.rtcCommitGeometry(geom);
        EmbreeAPI.rtcAttachGeometry(scene, geom);
        EmbreeAPI.rtcCommitScene(scene);

        Console.WriteLine("Scene setup complete");

        // Allocate RTCRayHit16 + 128 bytes padding with sentinel values
        int structSize = Marshal.SizeOf<RTCRayHit16>();
        int paddingSize = 128;
        int alignment = 64;
        int totalSize = structSize + paddingSize + alignment;
        IntPtr rawPtr = Marshal.AllocHGlobal(totalSize);

        try
        {
            long address = rawPtr.ToInt64();
            long aligned = (address + alignment - 1) & ~(alignment - 1);
            IntPtr alignedPtr = new IntPtr(aligned);
            RTCRayHit16* rayhit = (RTCRayHit16*)alignedPtr;

            // Fill padding after RTCRayHit16 with sentinel value 0xDEADBEEF
            uint* padding = (uint*)(alignedPtr + structSize);
            int sentinelCount = paddingSize / sizeof(uint);
            for (int i = 0; i < sentinelCount; i++)
            {
                padding[i] = 0xDEADBEEF;
            }

            Console.WriteLine($"RTCRayHit16 at: 0x{aligned:X}, size: {structSize} bytes");
            Console.WriteLine($"Padding at: 0x{(aligned + structSize):X}, size: {paddingSize} bytes");
            Console.WriteLine($"Sentinel value: 0xDEADBEEF ({sentinelCount} uint32s)");

            // Initialize rays
            for (int i = 0; i < 16; i++)
            {
                rayhit->ray.org_x[i] = 0.0f;
                rayhit->ray.org_y[i] = 0.0f;
                rayhit->ray.org_z[i] = 2.0f;
                rayhit->ray.tnear[i] = 0.0f;

                rayhit->ray.dir_x[i] = 0.0f;
                rayhit->ray.dir_y[i] = 0.0f;
                rayhit->ray.dir_z[i] = -1.0f;
                rayhit->ray.time[i] = 0.0f;

                rayhit->ray.tfar[i] = float.MaxValue;
                rayhit->ray.mask[i] = 0xFFFFFFFF;
                rayhit->ray.id[i] = (uint)i;
                rayhit->ray.flags[i] = 0;

                rayhit->hit.geomID[i] = unchecked((uint)-1);
                rayhit->hit.primID[i] = unchecked((uint)-1);
                rayhit->hit.instID[i] = unchecked((uint)-1);
            }

            // Setup validity mask
            Span<byte> validBuffer = stackalloc byte[sizeof(int) * 16 + 63];
            fixed (byte* validBufferPtr = validBuffer)
            {
                IntPtr alignedValidPtr = new IntPtr((long)(validBufferPtr + 63) & ~63L);
                int* valid = (int*)alignedValidPtr;
                for (int i = 0; i < 16; i++)
                    valid[i] = -1;

                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.Incoherent,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                Console.WriteLine("Calling rtcIntersect16...");
                EmbreeAPI.rtcIntersect16(valid, scene, rayhit, &args);
                Console.WriteLine("rtcIntersect16 returned successfully!");
            }

            // CHECK SENTINEL VALUES
            Console.WriteLine("Checking sentinel values for buffer overrun...");
            for (int i = 0; i < sentinelCount; i++)
            {
                if (padding[i] != 0xDEADBEEF)
                {
                    Console.WriteLine($"  OVERRUN at offset +{i * 4}: expected 0xDEADBEEF, got 0x{padding[i]:X}");
                    overrunDetected = true;
                }
            }

            if (!overrunDetected)
            {
                Console.WriteLine("  All sentinel values intact - no buffer overrun detected");
            }

            // Cleanup
            EmbreeAPI.rtcReleaseGeometry(geom);
            EmbreeAPI.rtcReleaseScene(scene);
            EmbreeAPI.rtcReleaseDevice(device);

            return true;
        }
        finally
        {
            Marshal.FreeHGlobal(rawPtr);
        }
    }
}
