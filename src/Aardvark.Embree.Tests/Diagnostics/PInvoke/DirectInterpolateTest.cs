using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// DIAGNOSTIC TEST: Direct P/Invoke interpolation test.
///
/// Purpose: Proves P/Invoke bindings for rtcInterpolate work correctly by exactly replicating
/// the C++ baseline test (embree_interpolate_reference.cpp) using only raw P/Invoke calls.
///
/// Bypasses: ALL wrapper classes (Scene, TriangleGeometry, etc.)
///
/// When to use:
/// - When wrapper-based interpolation tests fail
/// - To isolate whether bug is in P/Invoke bindings vs wrapper implementation
/// - As reference for correct P/Invoke usage
///
/// Historical context:
/// Created during interpolation investigation (Nov 2025).
/// Tests the N>4 vertex attribute case which previously caused issues.
/// BOB's C++ reference test proved native Embree works correctly.
/// This test proves whether C# P/Invoke bindings are correct.
///
/// See also:
/// - docs/DEBUGGING_METHODOLOGY.md for multi-layer debugging strategy
/// - Diagnostics/Native/embree_interpolate_reference.cpp for C++ baseline
/// - .boss/tasks/task-cpp-interpolation-reference-2b9c.md for BOB's findings
/// </summary>
public class DirectInterpolateTest
{
    [Fact]
    public void DirectPInvoke_Interpolate_Buffer0_3Attributes()
    {
        Console.WriteLine("=== Direct P/Invoke Interpolate Test: Buffer 0 (3 attributes) ===\n");

        // Setup: Create device, scene, and triangle with vertex attributes
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);
        Console.WriteLine("Device created");

        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set vertex attribute count - REQUIRED for vertex attribute buffers
        EmbreeAPI.rtcSetGeometryVertexAttributeCount(geom, 3);  // 3 attribute buffers

        // Set triangle vertices (triangle at origin in XY plane)
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.Vertex,
            0,
            RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3),
            3
        );
        unsafe
        {
            float* vertices = (float*)vertexBuffer;
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;  // v0: (0,0,0)
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;  // v1: (1,0,0)
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;  // v2: (0,1,0)
        }
        Console.WriteLine("Triangle vertices: (0,0,0), (1,0,0), (0,1,0)");

        // Set triangle indices
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.Index,
            0,
            RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3),
            1
        );
        unsafe
        {
            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
        }
        Console.WriteLine("Triangle indices: 0, 1, 2");

        // Set vertex attributes for buffer 0 (3 attributes)
        IntPtr attribBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.VertexAttribute,
            0,
            RTCFormat.FLOAT,
            (nuint)sizeof(float),
            3
        );
        if (attribBuffer == IntPtr.Zero)
        {
            RTCDeviceError error = EmbreeAPI.rtcGetDeviceError(device);
            throw new Exception($"Failed to allocate vertex attribute buffer. Error: {error}");
        }
        unsafe
        {
            float* attrib = (float*)attribBuffer;
            attrib[0] = 1.0f;  // v0: 1.0
            attrib[1] = 2.0f;  // v1: 2.0
            attrib[2] = 3.0f;  // v2: 3.0
        }
        Console.WriteLine("Set 3 vertex attributes: 1.0, 2.0, 3.0");

        EmbreeAPI.rtcCommitGeometry(geom);
        uint geomID = EmbreeAPI.rtcAttachGeometry(scene, geom);
        EmbreeAPI.rtcCommitScene(scene);
        Console.WriteLine($"Geometry committed and attached (geomID={geomID})\n");

        // Perform ray intersection to get hit point
        Console.WriteLine("Performing ray intersection...");
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];
        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
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

                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                EmbreeAPI.rtcIntersect1(scene, rayhit, &args);

                Assert.True(rayhit->hit.geomID != unchecked((uint)-1), "Ray must hit geometry");
                Console.WriteLine($"Hit: geomID={rayhit->hit.geomID}, primID={rayhit->hit.primID}");
                Console.WriteLine($"Hit UV: ({rayhit->hit.uv.X}, {rayhit->hit.uv.Y})");

                // Test interpolation using rtcInterpolate directly
                Console.WriteLine("\nTesting rtcInterpolate for buffer 0 (3 attributes)...");

                float result = 0.0f;
                unsafe
                {
                    IntPtr resultPtr = new IntPtr(&result);
                    EmbreeAPI.rtcInterpolate(
                        geom,
                        rayhit->hit.primID,
                        rayhit->hit.uv.X,
                        rayhit->hit.uv.Y,
                        RTCBufferType.VertexAttribute,
                        0,  // buffer slot
                        resultPtr,
                        IntPtr.Zero,  // dPdu
                        IntPtr.Zero,  // dPdv
                        1   // valueCount
                    );
                }

                // Expected: (1-u-v)*1.0 + u*2.0 + v*3.0
                // With u=0.25, v=0.25: (0.5)*1.0 + 0.25*2.0 + 0.25*3.0 = 0.5 + 0.5 + 0.75 = 1.75
                float u = rayhit->hit.uv.X;
                float v = rayhit->hit.uv.Y;
                float expected = (1.0f - u - v) * 1.0f + u * 2.0f + v * 3.0f;

                Console.WriteLine($"Result: {result}");
                Console.WriteLine($"Expected: {expected}");
                Console.WriteLine($"Difference: {Math.Abs(result - expected)}");

                Assert.InRange(result, expected - 0.001f, expected + 0.001f);
                Console.WriteLine("Status: PASS\n");
            }
        }

        // Cleanup
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcReleaseScene(scene);
        EmbreeAPI.rtcReleaseDevice(device);
    }

    [Fact]
    public void DirectPInvoke_Interpolate_Buffer1_6Attributes_NGreaterThan4()
    {
        Console.WriteLine("=== Direct P/Invoke Interpolate Test: Buffer 1 (6 attributes - N>4 test) ===\n");

        // Setup: Create device, scene, and triangle with vertex attributes
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);
        Console.WriteLine("Device created");

        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set vertex attribute count - REQUIRED for vertex attribute buffers
        EmbreeAPI.rtcSetGeometryVertexAttributeCount(geom, 3);  // 3 attribute buffers

        // Set triangle vertices
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.Vertex,
            0,
            RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3),
            3
        );
        unsafe
        {
            float* vertices = (float*)vertexBuffer;
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;
        }
        Console.WriteLine("Triangle vertices: (0,0,0), (1,0,0), (0,1,0)");

        // Set triangle indices
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.Index,
            0,
            RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3),
            1
        );
        unsafe
        {
            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
        }

        // CRITICAL TEST: Set vertex attributes for buffer 1 (simulating 6 attributes - N>4 case)
        // BOB's C++ test used buffer slot 1 to test N>4 attributes
        IntPtr attribBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.VertexAttribute,
            1,  // buffer slot 1
            RTCFormat.FLOAT,
            (nuint)sizeof(float),
            3
        );
        if (attribBuffer == IntPtr.Zero)
        {
            RTCDeviceError error = EmbreeAPI.rtcGetDeviceError(device);
            throw new Exception($"Failed to allocate vertex attribute buffer. Error: {error}");
        }
        unsafe
        {
            float* attrib = (float*)attribBuffer;
            attrib[0] = 10.0f;  // v0: 10.0
            attrib[1] = 20.0f;  // v1: 20.0
            attrib[2] = 30.0f;  // v2: 30.0
        }
        Console.WriteLine("Set buffer 1 with values: 10.0, 20.0, 30.0");

        EmbreeAPI.rtcCommitGeometry(geom);
        uint geomID = EmbreeAPI.rtcAttachGeometry(scene, geom);
        EmbreeAPI.rtcCommitScene(scene);
        Console.WriteLine($"Geometry committed and attached (geomID={geomID})\n");

        // Perform ray intersection
        Console.WriteLine("Performing ray intersection...");
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];
        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
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

                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                EmbreeAPI.rtcIntersect1(scene, rayhit, &args);

                Assert.True(rayhit->hit.geomID != unchecked((uint)-1), "Ray must hit geometry");
                Console.WriteLine($"Hit: geomID={rayhit->hit.geomID}, primID={rayhit->hit.primID}");
                Console.WriteLine($"Hit UV: ({rayhit->hit.uv.X}, {rayhit->hit.uv.Y})");

                // CRITICAL TEST: Interpolate buffer 1 (N>4 case)
                Console.WriteLine("\nTesting rtcInterpolate for buffer 1 (N>4 attributes - CRITICAL TEST)...");

                float result = 0.0f;
                unsafe
                {
                    IntPtr resultPtr = new IntPtr(&result);
                    EmbreeAPI.rtcInterpolate(
                        geom,
                        rayhit->hit.primID,
                        rayhit->hit.uv.X,
                        rayhit->hit.uv.Y,
                        RTCBufferType.VertexAttribute,
                        1,  // buffer slot 1 - THIS IS THE CRITICAL TEST
                        resultPtr,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        1
                    );
                }

                // Expected: (1-u-v)*10.0 + u*20.0 + v*30.0
                // With u=0.25, v=0.25: (0.5)*10.0 + 0.25*20.0 + 0.25*30.0 = 5.0 + 5.0 + 7.5 = 17.5
                float u = rayhit->hit.uv.X;
                float v = rayhit->hit.uv.Y;
                float expected = (1.0f - u - v) * 10.0f + u * 20.0f + v * 30.0f;

                Console.WriteLine($"Result: {result}");
                Console.WriteLine($"Expected: {expected}");
                Console.WriteLine($"Difference: {Math.Abs(result - expected)}");

                Assert.InRange(result, expected - 0.001f, expected + 0.001f);
                Console.WriteLine("Status: PASS\n");
            }
        }

        // Cleanup
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcReleaseScene(scene);
        EmbreeAPI.rtcReleaseDevice(device);
    }

    [Fact]
    public void DirectPInvoke_Interpolate_Buffer2_4Attributes_BoundaryCase()
    {
        Console.WriteLine("=== Direct P/Invoke Interpolate Test: Buffer 2 (4 attributes - boundary test) ===\n");

        // Setup: Create device, scene, and triangle with vertex attributes
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);
        Console.WriteLine("Device created");

        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set vertex attribute count - REQUIRED for vertex attribute buffers
        EmbreeAPI.rtcSetGeometryVertexAttributeCount(geom, 3);  // 3 attribute buffers

        // Set triangle vertices
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.Vertex,
            0,
            RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3),
            3
        );
        unsafe
        {
            float* vertices = (float*)vertexBuffer;
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;
        }
        Console.WriteLine("Triangle vertices: (0,0,0), (1,0,0), (0,1,0)");

        // Set triangle indices
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.Index,
            0,
            RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3),
            1
        );
        unsafe
        {
            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
        }

        // Set vertex attributes for buffer 2 (4 attributes - boundary case)
        IntPtr attribBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom,
            RTCBufferType.VertexAttribute,
            2,  // buffer slot 2
            RTCFormat.FLOAT,
            (nuint)sizeof(float),
            3
        );
        if (attribBuffer == IntPtr.Zero)
        {
            RTCDeviceError error = EmbreeAPI.rtcGetDeviceError(device);
            throw new Exception($"Failed to allocate vertex attribute buffer. Error: {error}");
        }
        unsafe
        {
            float* attrib = (float*)attribBuffer;
            attrib[0] = 100.0f;  // v0: 100.0
            attrib[1] = 200.0f;  // v1: 200.0
            attrib[2] = 300.0f;  // v2: 300.0
        }
        Console.WriteLine("Set buffer 2 with values: 100.0, 200.0, 300.0");

        EmbreeAPI.rtcCommitGeometry(geom);
        uint geomID = EmbreeAPI.rtcAttachGeometry(scene, geom);
        EmbreeAPI.rtcCommitScene(scene);
        Console.WriteLine($"Geometry committed and attached (geomID={geomID})\n");

        // Perform ray intersection
        Console.WriteLine("Performing ray intersection...");
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];
        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
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

                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                EmbreeAPI.rtcIntersect1(scene, rayhit, &args);

                Assert.True(rayhit->hit.geomID != unchecked((uint)-1), "Ray must hit geometry");
                Console.WriteLine($"Hit: geomID={rayhit->hit.geomID}, primID={rayhit->hit.primID}");
                Console.WriteLine($"Hit UV: ({rayhit->hit.uv.X}, {rayhit->hit.uv.Y})");

                // Test interpolation for buffer 2 (4 attributes - boundary case)
                Console.WriteLine("\nTesting rtcInterpolate for buffer 2 (4 attributes - boundary test)...");

                float result = 0.0f;
                unsafe
                {
                    IntPtr resultPtr = new IntPtr(&result);
                    EmbreeAPI.rtcInterpolate(
                        geom,
                        rayhit->hit.primID,
                        rayhit->hit.uv.X,
                        rayhit->hit.uv.Y,
                        RTCBufferType.VertexAttribute,
                        2,  // buffer slot 2
                        resultPtr,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        1
                    );
                }

                // Expected: (1-u-v)*100.0 + u*200.0 + v*300.0
                // With u=0.25, v=0.25: (0.5)*100.0 + 0.25*200.0 + 0.25*300.0 = 50.0 + 50.0 + 75.0 = 175.0
                float u = rayhit->hit.uv.X;
                float v = rayhit->hit.uv.Y;
                float expected = (1.0f - u - v) * 100.0f + u * 200.0f + v * 300.0f;

                Console.WriteLine($"Result: {result}");
                Console.WriteLine($"Expected: {expected}");
                Console.WriteLine($"Difference: {Math.Abs(result - expected)}");

                Assert.InRange(result, expected - 0.001f, expected + 0.001f);
                Console.WriteLine("Status: PASS\n");
            }
        }

        // Cleanup
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcReleaseScene(scene);
        EmbreeAPI.rtcReleaseDevice(device);
    }
}
