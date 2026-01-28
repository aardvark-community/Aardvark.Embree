using Aardvark.Embree;
using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.Diagnostics;

public class AlignmentDetailTest
{
    private readonly ITestOutputHelper output;

    public AlignmentDetailTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void CheckStructLayoutDetails()
    {
        output.WriteLine("Detailed StructLayout Attribute Analysis");
        output.WriteLine("==========================================\n");

        CheckStruct<RTCRay4>("RTCRay4");
        CheckStruct<RTCRay8>("RTCRay8");
        CheckStruct<RTCRay16>("RTCRay16");
        CheckStruct<RTCHit4>("RTCHit4");
        CheckStruct<RTCHit8>("RTCHit8");
        CheckStruct<RTCHit16>("RTCHit16");
        CheckStruct<RTCRayHit4>("RTCRayHit4");
        CheckStruct<RTCRayHit8>("RTCRayHit8");
        CheckStruct<RTCRayHit16>("RTCRayHit16");
        CheckStruct<RTCPointQuery4>("RTCPointQuery4");
        CheckStruct<RTCPointQuery8>("RTCPointQuery8");
        CheckStruct<RTCPointQuery16>("RTCPointQuery16");
    }

    private void CheckStruct<T>(string name) where T : struct
    {
        var attrs = typeof(T).GetCustomAttributes(typeof(StructLayoutAttribute), false);
        output.WriteLine($"{name}:");
        output.WriteLine($"  Size: {Marshal.SizeOf<T>()} bytes");

        if (attrs.Length > 0)
        {
            var layout = (StructLayoutAttribute)attrs[0];
            output.WriteLine($"  StructLayout: {layout.Value}");
            output.WriteLine($"  Pack: {layout.Pack}");
            output.WriteLine($"  Size (attribute): {layout.Size}");
            output.WriteLine($"  CharSet: {layout.CharSet}");
        }
        else
        {
            output.WriteLine("  ERROR: NO StructLayoutAttribute found!");
        }
        output.WriteLine("");
    }

    [Fact]
    public unsafe void CheckAlignmentCalculations()
    {
        output.WriteLine("Alignment Calculation Verification");
        output.WriteLine("====================================\n");

        // RTCRayHit4 (16-byte alignment)
        int size4 = Marshal.SizeOf<RTCRayHit4>();
        int buffer4 = size4 + 15;
        output.WriteLine($"RTCRayHit4 (16-byte alignment):");
        output.WriteLine($"  Struct size: {size4}");
        output.WriteLine($"  Buffer size: {buffer4}");
        output.WriteLine($"  Alignment mask: ~15L = ...{Convert.ToString(~15L, 2)}");
        output.WriteLine("");

        // Simulate alignment
        byte* testPtr = stackalloc byte[buffer4];
        long unalignedAddr = (long)testPtr;
        long alignedAddr = (unalignedAddr + 15) & ~15L;
        long offset = alignedAddr - unalignedAddr;
        output.WriteLine($"  Simulated unaligned address: 0x{unalignedAddr:X}");
        output.WriteLine($"  After (addr + 15): 0x{unalignedAddr + 15:X}");
        output.WriteLine($"  After & ~15L: 0x{alignedAddr:X}");
        output.WriteLine($"  Offset: {offset} bytes");
        output.WriteLine($"  Remaining space: {buffer4 - offset} bytes (need {size4})");
        if (buffer4 - offset < size4)
            output.WriteLine("  ERROR: Insufficient buffer space!");
        output.WriteLine("");

        // RTCRayHit8 (32-byte alignment)
        int size8 = Marshal.SizeOf<RTCRayHit8>();
        int buffer8 = size8 + 31;
        output.WriteLine($"RTCRayHit8 (32-byte alignment):");
        output.WriteLine($"  Struct size: {size8}");
        output.WriteLine($"  Buffer size: {buffer8}");
        output.WriteLine($"  Alignment mask: ~31L");
        output.WriteLine("");

        // RTCRayHit16 (64-byte alignment)
        int size16 = Marshal.SizeOf<RTCRayHit16>();
        int buffer16 = size16 + 63;
        output.WriteLine($"RTCRayHit16 (64-byte alignment):");
        output.WriteLine($"  Struct size: {size16}");
        output.WriteLine($"  Buffer size: {buffer16}");
        output.WriteLine($"  Alignment mask: ~63L");
        output.WriteLine("");

        // Check valid buffer sizes
        output.WriteLine("POTENTIAL ISSUES:");
        output.WriteLine("=================");

        // Worst case: pointer is 1 byte before alignment boundary
        // For 16-byte: need size + 15 bytes
        // For 32-byte: need size + 31 bytes
        // For 64-byte: need size + 63 bytes

        output.WriteLine($"RTCRayHit4: {size4} + 15 = {buffer4} bytes buffer (OK)");
        output.WriteLine($"RTCRayHit8: {size8} + 31 = {buffer8} bytes buffer (OK)");
        output.WriteLine($"RTCRayHit16: {size16} + 63 = {buffer16} bytes buffer (OK)");
    }

    [Fact]
    public unsafe void TestActualStackAllocAlignment()
    {
        output.WriteLine("Actual stackalloc Alignment Test");
        output.WriteLine("=================================\n");

        // Test RTCRayHit4
        Span<byte> buffer4 = stackalloc byte[Marshal.SizeOf<RTCRayHit4>() + 15];
        fixed (byte* bufferPtr4 = buffer4)
        {
            long addr = (long)bufferPtr4;
            long aligned = (addr + 15) & ~15L;
            long offset = aligned - addr;

            output.WriteLine($"RTCRayHit4:");
            output.WriteLine($"  Original address: 0x{addr:X16} (mod 16 = {addr % 16})");
            output.WriteLine($"  Aligned address:  0x{aligned:X16} (mod 16 = {aligned % 16})");
            output.WriteLine($"  Offset: {offset} bytes");
            output.WriteLine($"  Buffer remaining: {buffer4.Length - offset} bytes (need {Marshal.SizeOf<RTCRayHit4>()})");

            if ((aligned % 16) != 0)
                output.WriteLine("  ERROR: Alignment failed! Not 16-byte aligned!");
            if (buffer4.Length - offset < Marshal.SizeOf<RTCRayHit4>())
                output.WriteLine("  ERROR: Buffer overflow risk!");
            output.WriteLine("");
        }

        // Test RTCRayHit8
        Span<byte> buffer8 = stackalloc byte[Marshal.SizeOf<RTCRayHit8>() + 31];
        fixed (byte* bufferPtr8 = buffer8)
        {
            long addr = (long)bufferPtr8;
            long aligned = (addr + 31) & ~31L;
            long offset = aligned - addr;

            output.WriteLine($"RTCRayHit8:");
            output.WriteLine($"  Original address: 0x{addr:X16} (mod 32 = {addr % 32})");
            output.WriteLine($"  Aligned address:  0x{aligned:X16} (mod 32 = {aligned % 32})");
            output.WriteLine($"  Offset: {offset} bytes");
            output.WriteLine($"  Buffer remaining: {buffer8.Length - offset} bytes (need {Marshal.SizeOf<RTCRayHit8>()})");

            if ((aligned % 32) != 0)
                output.WriteLine("  ERROR: Alignment failed! Not 32-byte aligned!");
            if (buffer8.Length - offset < Marshal.SizeOf<RTCRayHit8>())
                output.WriteLine("  ERROR: Buffer overflow risk!");
            output.WriteLine("");
        }

        // Test RTCRayHit16
        Span<byte> buffer16 = stackalloc byte[Marshal.SizeOf<RTCRayHit16>() + 63];
        fixed (byte* bufferPtr16 = buffer16)
        {
            long addr = (long)bufferPtr16;
            long aligned = (addr + 63) & ~63L;
            long offset = aligned - addr;

            output.WriteLine($"RTCRayHit16:");
            output.WriteLine($"  Original address: 0x{addr:X16} (mod 64 = {addr % 64})");
            output.WriteLine($"  Aligned address:  0x{aligned:X16} (mod 64 = {aligned % 64})");
            output.WriteLine($"  Offset: {offset} bytes");
            output.WriteLine($"  Buffer remaining: {buffer16.Length - offset} bytes (need {Marshal.SizeOf<RTCRayHit16>()})");

            if ((aligned % 64) != 0)
                output.WriteLine("  ERROR: Alignment failed! Not 64-byte aligned!");
            if (buffer16.Length - offset < Marshal.SizeOf<RTCRayHit16>())
                output.WriteLine("  ERROR: Buffer overflow risk!");
            output.WriteLine("");
        }
    }
}
