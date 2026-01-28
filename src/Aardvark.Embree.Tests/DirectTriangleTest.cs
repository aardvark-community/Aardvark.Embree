using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Test direct P/Invoke with a simple triangle (NO instancing) to verify basic rtcIntersect1 works
/// </summary>
public class DirectTriangleTest
{
    [Fact]
    public void DirectPInvoke_SimpleTriangle_ShouldIntersect()
    {
        Console.WriteLine("=== Direct P/Invoke Simple Triangle Test ===");

        unsafe
        {
            // Step 1: Create device
            IntPtr device = EmbreeAPI.rtcNewDevice(null);
            Assert.NotEqual(IntPtr.Zero, device);
            Console.WriteLine($"Device created: 0x{device.ToInt64():X}");

            // Step 2: Create scene
            IntPtr scene = EmbreeAPI.rtcNewScene(device);
            Assert.NotEqual(IntPtr.Zero, scene);
            Console.WriteLine($"Scene created: 0x{scene.ToInt64():X}");

            // Step 3: Create triangle geometry
            IntPtr geom = EmbreeAPI.rtcNewGeometry(device, RTCGeometryType.Triangle);
            Assert.NotEqual(IntPtr.Zero, geom);
            Console.WriteLine($"Triangle geometry created: 0x{geom.ToInt64():X}");

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

            float* vertices = (float*)vertexBuffer;
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;  // v0
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;  // v1
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;  // v2
            Console.WriteLine("Vertices set: (0,0,0), (1,0,0), (0,1,0)");

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

            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
            Console.WriteLine("Indices set: 0, 1, 2");

            // Commit geometry
            EmbreeAPI.rtcCommitGeometry(geom);
            Console.WriteLine("Geometry committed");

            // Attach to scene
            uint geomID = EmbreeAPI.rtcAttachGeometry(scene, geom);
            Console.WriteLine($"Geometry attached to scene (geomID={geomID})");

            // Commit scene
            EmbreeAPI.rtcCommitScene(scene);
            Console.WriteLine("Scene committed");

            // Test ray intersection
            Console.WriteLine("\nTesting ray intersection...");

            // Allocate with 16-byte alignment for SIMD
            Span<byte> rayhitBuffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];
            fixed (byte* rayhitBufferPtr = rayhitBuffer)
            {
                IntPtr alignedPtr = new IntPtr((long)(rayhitBufferPtr + 15) & ~15L);
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

                Console.WriteLine($"Ray: origin=({rayhit->ray.org}), direction=({rayhit->ray.dir})");

                RTCIntersectArguments args = new RTCIntersectArguments
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = IntPtr.Zero,
                    intersect = IntPtr.Zero
                };

                EmbreeAPI.rtcIntersect1(scene, rayhit, &args);
                Console.WriteLine("rtcIntersect1 called");

                Console.WriteLine($"\nResults:");
                Console.WriteLine($"  geomID: {rayhit->hit.geomID}");
                Console.WriteLine($"  primID: {rayhit->hit.primID}");
                Console.WriteLine($"  tfar: {rayhit->ray.tfar}");

                bool hit = rayhit->hit.geomID != unchecked((uint)-1);
                Console.WriteLine($"\n=== RESULT: {(hit ? "SUCCESS" : "FAILED")} ===");

                if (hit)
                {
                    Console.WriteLine($"Hit distance: {rayhit->ray.tfar}");
                    Console.WriteLine($"Expected: ~1.0 (ray from z=1 to z=0)");
                }

                // Cleanup
                EmbreeAPI.rtcReleaseGeometry(geom);
                EmbreeAPI.rtcReleaseScene(scene);
                EmbreeAPI.rtcReleaseDevice(device);

                // ASSERT
                Assert.True(hit, "Simple triangle MUST intersect");
                Assert.InRange(rayhit->ray.tfar, 0.99f, 1.01f);
            }
        }
    }
}
