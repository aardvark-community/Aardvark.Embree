using Aardvark.Base;
using Aardvark.Embree;
using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.Diagnostics;

/// <summary>
/// Tests for potential memory corruption in RayPacket operations
/// </summary>
public class MemoryCorruptionTest
{
    private readonly ITestOutputHelper output;

    public MemoryCorruptionTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public unsafe void TestRayPacket4BufferBoundaries()
    {
        output.WriteLine("Testing RTCRayHit4 buffer boundaries");
        output.WriteLine("====================================\n");

        int structSize = Marshal.SizeOf<RTCRayHit4>();
        int bufferSize = structSize + 15; // 16-byte alignment

        output.WriteLine($"Struct size: {structSize} bytes");
        output.WriteLine($"Buffer size: {bufferSize} bytes");
        output.WriteLine("");

        // Allocate with guard bytes
        Span<byte> buffer = stackalloc byte[bufferSize + 16]; // Extra 16 bytes as guards

        // Fill entire buffer with sentinel pattern
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = 0xAA;

        fixed (byte* bufferPtr = buffer)
        {
            // Align pointer
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
            RTCRayHit4* rayHitPtr = (RTCRayHit4*)alignedPtr;

            long offset = (long)rayHitPtr - (long)bufferPtr;
            output.WriteLine($"Buffer start: 0x{(long)bufferPtr:X}");
            output.WriteLine($"Aligned ptr:  0x{(long)rayHitPtr:X}");
            output.WriteLine($"Offset: {offset} bytes");
            output.WriteLine($"Usable space: {buffer.Length - offset} bytes");
            output.WriteLine("");

            // Initialize struct (this should not overwrite guard bytes)
            for (int i = 0; i < 4; i++)
            {
                rayHitPtr->ray.org_x[i] = 1.0f;
                rayHitPtr->ray.org_y[i] = 2.0f;
                rayHitPtr->ray.org_z[i] = 3.0f;
                rayHitPtr->ray.tnear[i] = 0.0f;

                rayHitPtr->ray.dir_x[i] = 0.0f;
                rayHitPtr->ray.dir_y[i] = 0.0f;
                rayHitPtr->ray.dir_z[i] = -1.0f;
                rayHitPtr->ray.time[i] = 0.0f;

                rayHitPtr->ray.tfar[i] = float.MaxValue;
                rayHitPtr->ray.mask[i] = 0xFFFFFFFF;
                rayHitPtr->ray.id[i] = (uint)i;
                rayHitPtr->ray.flags[i] = 0;

                rayHitPtr->hit.geomID[i] = 0xFFFFFFFF;
                rayHitPtr->hit.primID[i] = 0xFFFFFFFF;
                rayHitPtr->hit.instID[i] = 0xFFFFFFFF;
            }

            // Check if we overwrote any guard bytes
            int guardStart = (int)(offset + structSize);
            bool corruption = false;
            for (int i = guardStart; i < buffer.Length; i++)
            {
                if (buffer[i] != 0xAA)
                {
                    output.WriteLine($"ERROR: Guard byte at offset {i} corrupted! Expected 0xAA, got 0x{buffer[i]:X}");
                    corruption = true;
                }
            }

            if (!corruption)
                output.WriteLine("SUCCESS: No buffer overflow detected");
            else
                output.WriteLine("FAILURE: Buffer overflow detected!");

            Assert.False(corruption, "Buffer overflow detected in RTCRayHit4");
        }
    }

    [Fact]
    public unsafe void TestRayPacket8BufferBoundaries()
    {
        output.WriteLine("Testing RTCRayHit8 buffer boundaries");
        output.WriteLine("====================================\n");

        int structSize = Marshal.SizeOf<RTCRayHit8>();
        int bufferSize = structSize + 31; // 32-byte alignment

        output.WriteLine($"Struct size: {structSize} bytes");
        output.WriteLine($"Buffer size: {bufferSize} bytes");
        output.WriteLine("");

        // Allocate with guard bytes
        Span<byte> buffer = stackalloc byte[bufferSize + 32];

        // Fill with sentinel
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = 0xBB;

        fixed (byte* bufferPtr = buffer)
        {
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 31) & ~31L);
            RTCRayHit8* rayHitPtr = (RTCRayHit8*)alignedPtr;

            long offset = (long)rayHitPtr - (long)bufferPtr;

            // Initialize struct
            for (int i = 0; i < 8; i++)
            {
                rayHitPtr->ray.org_x[i] = 1.0f;
                rayHitPtr->ray.org_y[i] = 2.0f;
                rayHitPtr->ray.org_z[i] = 3.0f;
                rayHitPtr->ray.tnear[i] = 0.0f;

                rayHitPtr->ray.dir_x[i] = 0.0f;
                rayHitPtr->ray.dir_y[i] = 0.0f;
                rayHitPtr->ray.dir_z[i] = -1.0f;
                rayHitPtr->ray.time[i] = 0.0f;

                rayHitPtr->ray.tfar[i] = float.MaxValue;
                rayHitPtr->ray.mask[i] = 0xFFFFFFFF;
                rayHitPtr->ray.id[i] = (uint)i;
                rayHitPtr->ray.flags[i] = 0;

                rayHitPtr->hit.geomID[i] = 0xFFFFFFFF;
                rayHitPtr->hit.primID[i] = 0xFFFFFFFF;
                rayHitPtr->hit.instID[i] = 0xFFFFFFFF;
            }

            // Check guards
            int guardStart = (int)(offset + structSize);
            bool corruption = false;
            for (int i = guardStart; i < buffer.Length; i++)
            {
                if (buffer[i] != 0xBB)
                {
                    output.WriteLine($"ERROR: Guard byte at offset {i} corrupted! Expected 0xBB, got 0x{buffer[i]:X}");
                    corruption = true;
                }
            }

            if (!corruption)
                output.WriteLine("SUCCESS: No buffer overflow detected");

            Assert.False(corruption, "Buffer overflow detected in RTCRayHit8");
        }
    }

    [Fact]
    public unsafe void TestRayPacket16BufferBoundaries()
    {
        output.WriteLine("Testing RTCRayHit16 buffer boundaries");
        output.WriteLine("=====================================\n");

        int structSize = Marshal.SizeOf<RTCRayHit16>();
        int bufferSize = structSize + 63; // 64-byte alignment

        output.WriteLine($"Struct size: {structSize} bytes");
        output.WriteLine($"Buffer size: {bufferSize} bytes");
        output.WriteLine("");

        // Allocate with guard bytes
        Span<byte> buffer = stackalloc byte[bufferSize + 64];

        // Fill with sentinel
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = 0xCC;

        fixed (byte* bufferPtr = buffer)
        {
            IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 63) & ~63L);
            RTCRayHit16* rayHitPtr = (RTCRayHit16*)alignedPtr;

            long offset = (long)rayHitPtr - (long)bufferPtr;

            // Initialize struct
            for (int i = 0; i < 16; i++)
            {
                rayHitPtr->ray.org_x[i] = 1.0f;
                rayHitPtr->ray.org_y[i] = 2.0f;
                rayHitPtr->ray.org_z[i] = 3.0f;
                rayHitPtr->ray.tnear[i] = 0.0f;

                rayHitPtr->ray.dir_x[i] = 0.0f;
                rayHitPtr->ray.dir_y[i] = 0.0f;
                rayHitPtr->ray.dir_z[i] = -1.0f;
                rayHitPtr->ray.time[i] = 0.0f;

                rayHitPtr->ray.tfar[i] = float.MaxValue;
                rayHitPtr->ray.mask[i] = 0xFFFFFFFF;
                rayHitPtr->ray.id[i] = (uint)i;
                rayHitPtr->ray.flags[i] = 0;

                rayHitPtr->hit.geomID[i] = 0xFFFFFFFF;
                rayHitPtr->hit.primID[i] = 0xFFFFFFFF;
                rayHitPtr->hit.instID[i] = 0xFFFFFFFF;
            }

            // Check guards
            int guardStart = (int)(offset + structSize);
            bool corruption = false;
            for (int i = guardStart; i < buffer.Length; i++)
            {
                if (buffer[i] != 0xCC)
                {
                    output.WriteLine($"ERROR: Guard byte at offset {i} corrupted! Expected 0xCC, got 0x{buffer[i]:X}");
                    corruption = true;
                }
            }

            if (!corruption)
                output.WriteLine("SUCCESS: No buffer overflow detected");

            Assert.False(corruption, "Buffer overflow detected in RTCRayHit16");
        }
    }

    [Fact]
    public unsafe void TestStructLayoutConsistency()
    {
        output.WriteLine("Testing struct layout consistency (Debug vs Release)");
        output.WriteLine("====================================================\n");

        // Check if structs have consistent sizes
        int ray4 = Marshal.SizeOf<RTCRay4>();
        int hit4 = Marshal.SizeOf<RTCHit4>();
        int rayHit4 = Marshal.SizeOf<RTCRayHit4>();

        int ray8 = Marshal.SizeOf<RTCRay8>();
        int hit8 = Marshal.SizeOf<RTCHit8>();
        int rayHit8 = Marshal.SizeOf<RTCRayHit8>();

        int ray16 = Marshal.SizeOf<RTCRay16>();
        int hit16 = Marshal.SizeOf<RTCHit16>();
        int rayHit16 = Marshal.SizeOf<RTCRayHit16>();

        output.WriteLine("Expected sizes:");
        output.WriteLine($"  RTCRay4:    192 bytes (actual: {ray4})");
        output.WriteLine($"  RTCHit4:    144 bytes (actual: {hit4})  [includes instPrimID[4]]");
        output.WriteLine($"  RTCRayHit4: 336 bytes (actual: {rayHit4})");
        output.WriteLine("");
        output.WriteLine($"  RTCRay8:    384 bytes (actual: {ray8})");
        output.WriteLine($"  RTCHit8:    288 bytes (actual: {hit8})  [includes instPrimID[8]]");
        output.WriteLine($"  RTCRayHit8: 672 bytes (actual: {rayHit8})");
        output.WriteLine("");
        output.WriteLine($"  RTCRay16:   768 bytes (actual: {ray16})");
        output.WriteLine($"  RTCHit16:   576 bytes (actual: {hit16})  [includes instPrimID[16]]");
        output.WriteLine($"  RTCRayHit16: 1344 bytes (actual: {rayHit16})");
        output.WriteLine("");

        // These must match exactly for Debug and Release to behave the same
        // NOTE: RTCHit4/8/16 now include instPrimID[N] array (added in Embree 4 for instance arrays)
        Assert.Equal(192, ray4);
        Assert.Equal(144, hit4);   // Updated: was 128, now includes instPrimID[4]
        Assert.Equal(336, rayHit4); // Updated: was 320

        Assert.Equal(384, ray8);
        Assert.Equal(288, hit8);   // Updated: was 256, now includes instPrimID[8]
        Assert.Equal(672, rayHit8); // Updated: was 640

        Assert.Equal(768, ray16);
        Assert.Equal(576, hit16);   // Updated: was 512, now includes instPrimID[16]
        Assert.Equal(1344, rayHit16); // Updated: was 1280

        // Check that RayHit structs are exactly Ray + Hit
        Assert.Equal(ray4 + hit4, rayHit4);
        Assert.Equal(ray8 + hit8, rayHit8);
        Assert.Equal(ray16 + hit16, rayHit16);

        output.WriteLine("All struct sizes are consistent!");
    }
}
