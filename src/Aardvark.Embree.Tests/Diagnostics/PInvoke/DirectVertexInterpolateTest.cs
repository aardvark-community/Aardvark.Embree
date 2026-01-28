using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// DIAGNOSTIC TEST: Direct P/Invoke interpolation test using VERTEX buffer.
///
/// Purpose: Proves that rtcInterpolate works correctly when interpolating from VERTEX buffer
/// (position data) instead of VERTEX_ATTRIBUTE buffer.
///
/// Key Finding: The original bug was trying to use VERTEX_ATTRIBUTE buffer for position
/// interpolation, but position data should be interpolated from the VERTEX buffer.
///
/// Historical context:
/// Created during interpolation bug fix (Nov 2025).
/// Previous tests tried VERTEX_ATTRIBUTE buffer which failed with NullReferenceException.
/// This test proves native rtcInterpolate works correctly with VERTEX buffer.
///
/// See also:
/// - docs/DEBUGGING_METHODOLOGY.md for multi-layer debugging strategy
/// - Diagnostics/PInvoke/DirectInterpolateTest.cs for failed VERTEX_ATTRIBUTE approach
/// - .boss/tasks/task-fix-interpolation-bug-9e2a.md for bug analysis
/// </summary>
public class DirectVertexInterpolateTest
{
    [Fact]
    public void DirectPInvoke_Interpolate_VertexBuffer_Position()
    {
        Console.WriteLine("=== Direct P/Invoke Interpolate Test: VERTEX buffer (position) ===\n");

        // Setup: Create device, scene, and triangle
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);
        Console.WriteLine("Device created");

        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set triangle vertices (triangle at origin in XY plane)
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
        Assert.NotEqual(IntPtr.Zero, indexBuffer);

        unsafe
        {
            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
        }
        Console.WriteLine("Triangle indices: 0, 1, 2");

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

                // Test interpolation using rtcInterpolate with VERTEX buffer (not VERTEX_ATTRIBUTE)
                Console.WriteLine("\nTesting rtcInterpolate for VERTEX buffer (position)...");

                V3f result = V3f.Zero;
                unsafe
                {
                    IntPtr resultPtr = new IntPtr(&result);

                    // KEY FIX: Use RTCBufferType.Vertex (not VertexAttribute) for position interpolation
                    EmbreeAPI.rtcInterpolate(
                        geom,
                        rayhit->hit.primID,
                        rayhit->hit.uv.X,
                        rayhit->hit.uv.Y,
                        RTCBufferType.Vertex,  // <- THIS IS THE KEY: Use Vertex, not VertexAttribute
                        0,  // buffer slot
                        resultPtr,
                        IntPtr.Zero,  // dPdu
                        IntPtr.Zero,  // dPdv
                        3   // valueCount (3 floats for V3f)
                    );
                }

                // Expected: barycentric interpolation of vertices
                // (1-u-v)*(0,0,0) + u*(1,0,0) + v*(0,1,0) = (u, v, 0)
                // With u=0.25, v=0.25: (0.25, 0.25, 0)
                float u = rayhit->hit.uv.X;
                float v = rayhit->hit.uv.Y;
                V3f expected = new V3f(u, v, 0.0f);

                Console.WriteLine($"Result: {result}");
                Console.WriteLine($"Expected: {expected}");
                float distance = (result - expected).Length;
                Console.WriteLine($"Difference: {distance}");

                Assert.InRange(result.X, expected.X - 0.001f, expected.X + 0.001f);
                Assert.InRange(result.Y, expected.Y - 0.001f, expected.Y + 0.001f);
                Assert.InRange(result.Z, expected.Z - 0.001f, expected.Z + 0.001f);
                Console.WriteLine("Status: PASS\n");
            }
        }

        // Cleanup
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcReleaseScene(scene);
        EmbreeAPI.rtcReleaseDevice(device);
    }

    [Fact]
    public void DirectPInvoke_Interpolate_VertexBuffer_WithDerivatives()
    {
        Console.WriteLine("=== Direct P/Invoke Interpolate Test: VERTEX buffer with derivatives ===\n");

        // Setup: Create device, scene, and triangle
        IntPtr device = EmbreeAPI.rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);
        Console.WriteLine("Device created");

        IntPtr scene = EmbreeAPI.rtcNewScene(device);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);

        // Set triangle vertices
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
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;
        }

        // Set triangle indices
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
        }

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

                // Test interpolation with derivatives
                Console.WriteLine("\nTesting rtcInterpolate with derivatives...");

                V3f position = V3f.Zero;
                V3f dPdu = V3f.Zero;
                V3f dPdv = V3f.Zero;

                unsafe
                {
                    IntPtr positionPtr = new IntPtr(&position);
                    IntPtr dPduPtr = new IntPtr(&dPdu);
                    IntPtr dPdvPtr = new IntPtr(&dPdv);

                    EmbreeAPI.rtcInterpolate(
                        geom,
                        rayhit->hit.primID,
                        rayhit->hit.uv.X,
                        rayhit->hit.uv.Y,
                        RTCBufferType.Vertex,
                        0,
                        positionPtr,
                        dPduPtr,  // Request dPdu
                        dPdvPtr,  // Request dPdv
                        3
                    );
                }

                // Expected derivatives: dPdu = v1-v0 = (1,0,0), dPdv = v2-v0 = (0,1,0)
                V3f expectedDPdu = new V3f(1, 0, 0);
                V3f expectedDPdv = new V3f(0, 1, 0);

                Console.WriteLine($"Position: {position}");
                Console.WriteLine($"dPdu: {dPdu}, Expected: {expectedDPdu}");
                Console.WriteLine($"dPdv: {dPdv}, Expected: {expectedDPdv}");

                Assert.InRange(dPdu.X, expectedDPdu.X - 0.001f, expectedDPdu.X + 0.001f);
                Assert.InRange(dPdu.Y, expectedDPdu.Y - 0.001f, expectedDPdu.Y + 0.001f);
                Assert.InRange(dPdu.Z, expectedDPdu.Z - 0.001f, expectedDPdu.Z + 0.001f);

                Assert.InRange(dPdv.X, expectedDPdv.X - 0.001f, expectedDPdv.X + 0.001f);
                Assert.InRange(dPdv.Y, expectedDPdv.Y - 0.001f, expectedDPdv.Y + 0.001f);
                Assert.InRange(dPdv.Z, expectedDPdv.Z - 0.001f, expectedDPdv.Z + 0.001f);

                Console.WriteLine("Status: PASS\n");
            }
        }

        // Cleanup
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcReleaseScene(scene);
        EmbreeAPI.rtcReleaseDevice(device);
    }
}
