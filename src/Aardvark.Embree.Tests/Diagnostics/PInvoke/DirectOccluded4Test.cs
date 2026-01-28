using Aardvark.Base;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Aardvark.Embree.Tests.Diagnostics.PInvoke;

/// <summary>
/// Direct P/Invoke test for rtcOccluded4 to inspect raw tfar values.
/// Bypasses wrapper layer to diagnose occlusion detection behavior.
/// </summary>
public class DirectOccluded4Test
{
    [Fact(DisplayName = "Direct P/Invoke: Inspect rtcOccluded4 tfar values")]
    public unsafe void DirectOccluded4_InspectRawTfarValues()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

        // Large triangle in XY plane at Z=0
        var vertices = new[] { new V3f(-2, -2, 0), new V3f(2, -2, 0), new V3f(0, 2, 0) };
        var indices = new[] { 0, 1, 2 };
        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        scene.AttachGeometry(geometry);
        scene.Commit();

        // Allocate 16-byte aligned memory for RTCRay4
        int structSize = Marshal.SizeOf<RTCRay4>();
        Span<byte> buffer = stackalloc byte[structSize + 15];

        fixed (byte* bufferPtr = buffer)
        {
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
            RTCRay4* rayPacket = (RTCRay4*)alignedPtr;

            // Initialize 4 rays: 2 hit, 2 miss
            var origins = new V3f[]
            {
                new V3f(0, 0, 2),          // Should hit - center
                new V3f(5, 5, 2),          // Should miss - far away
                new V3f(-0.3f, -0.3f, 2),  // Should hit - inside
                new V3f(10, 0, 2)          // Should miss - far right
            };

            float initialTfar = float.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                rayPacket->org_x[i] = origins[i].X;
                rayPacket->org_y[i] = origins[i].Y;
                rayPacket->org_z[i] = origins[i].Z;
                rayPacket->tnear[i] = 0.0f;

                rayPacket->dir_x[i] = 0.0f;
                rayPacket->dir_y[i] = 0.0f;
                rayPacket->dir_z[i] = -1.0f;
                rayPacket->time[i] = 0.0f;

                rayPacket->tfar[i] = initialTfar;
                rayPacket->mask[i] = 0xFFFFFFFF;
                rayPacket->id[i] = (uint)i;
                rayPacket->flags[i] = 0;
            }

            Console.WriteLine($"\n=== BEFORE rtcOccluded4 ===");
            for (int i = 0; i < 4; i++)
            {
                Console.WriteLine($"Ray {i}: tfar = {rayPacket->tfar[i]} (0x{(uint)BitConverter.SingleToInt32Bits(rayPacket->tfar[i]):X8})");
            }

            // Set validity mask (all rays valid = -1)
            int* valid = stackalloc int[4];
            for (int i = 0; i < 4; i++)
                valid[i] = -1;

            var args = new RTCOccludedArguments
            {
                flags = RTCRayQueryFlags.None,
                feature_mask = 0xFFFFFFFF,
                context = IntPtr.Zero,
                filter = IntPtr.Zero,
                occluded = IntPtr.Zero
            };

            EmbreeAPI.rtcOccluded4(valid, scene.Handle, rayPacket, &args);
            device.CheckError("Direct rtcOccluded4");

            Console.WriteLine($"\n=== AFTER rtcOccluded4 ===");
            for (int i = 0; i < 4; i++)
            {
                float tfar = rayPacket->tfar[i];
                uint tfarBits = (uint)BitConverter.SingleToInt32Bits(tfar);
                bool isNegInf = tfar == float.NegativeInfinity;
                bool isLessThanZero = tfar < 0.0f;

                Console.WriteLine($"Ray {i}: tfar = {tfar} (0x{tfarBits:X8})");
                Console.WriteLine($"  - IsNegativeInfinity: {isNegInf}");
                Console.WriteLine($"  - IsLessThanZero: {isLessThanZero}");
                Console.WriteLine($"  - Expected (ray {i}): {(i == 0 || i == 2 ? "occluded" : "clear")}");
            }

            // Test single-ray for comparison
            Console.WriteLine($"\n=== Single-ray comparison ===");
            for (int i = 0; i < 4; i++)
            {
                bool singleResult = scene.Occluded(origins[i], new V3f(0, 0, -1));
                Console.WriteLine($"Ray {i} (single): {singleResult}");
            }

            // Expected: rays 0 and 2 should be occluded (hit triangle)
            // Expected: rays 1 and 3 should be clear (miss triangle)
            Assert.True(rayPacket->tfar[0] < 0.0f || rayPacket->tfar[0] == float.NegativeInfinity,
                $"Ray 0 should be occluded (tfar={rayPacket->tfar[0]})");
            Assert.True(rayPacket->tfar[2] < 0.0f || rayPacket->tfar[2] == float.NegativeInfinity,
                $"Ray 2 should be occluded (tfar={rayPacket->tfar[2]})");
        }
    }
}
