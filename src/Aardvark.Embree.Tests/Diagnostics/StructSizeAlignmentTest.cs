using Aardvark.Embree;
using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.Diagnostics;

public class StructSizeAlignmentTest
{
    private readonly ITestOutputHelper output;

    public StructSizeAlignmentTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void PrintStructSizes()
    {
        output.WriteLine("Struct Size Analysis for RayPackets");
        output.WriteLine("=====================================\n");

        // RTCRay4 family
        var ray4Size = Marshal.SizeOf<RTCRay4>();
        var hit4Size = Marshal.SizeOf<RTCHit4>();
        var rayHit4Size = Marshal.SizeOf<RTCRayHit4>();

        output.WriteLine("RTCRay4:");
        output.WriteLine($"  Size: {ray4Size} bytes");
        output.WriteLine($"  Expected: 192 bytes (12 arrays * 4 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCRay4), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        output.WriteLine("RTCHit4:");
        output.WriteLine($"  Size: {hit4Size} bytes");
        output.WriteLine($"  Expected: 128 bytes (8 arrays * 4 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCHit4), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        output.WriteLine("RTCRayHit4:");
        output.WriteLine($"  Size: {rayHit4Size} bytes");
        output.WriteLine($"  Expected: {ray4Size + hit4Size} bytes (ray + hit)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCRayHit4), typeof(StructLayoutAttribute))}");
        output.WriteLine($"  Buffer calculation: {rayHit4Size} + 15 = {rayHit4Size + 15} bytes");
        output.WriteLine("");

        // RTCRay8 family
        var ray8Size = Marshal.SizeOf<RTCRay8>();
        var hit8Size = Marshal.SizeOf<RTCHit8>();
        var rayHit8Size = Marshal.SizeOf<RTCRayHit8>();

        output.WriteLine("RTCRay8:");
        output.WriteLine($"  Size: {ray8Size} bytes");
        output.WriteLine($"  Expected: 384 bytes (12 arrays * 8 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCRay8), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        output.WriteLine("RTCHit8:");
        output.WriteLine($"  Size: {hit8Size} bytes");
        output.WriteLine($"  Expected: 256 bytes (8 arrays * 8 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCHit8), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        output.WriteLine("RTCRayHit8:");
        output.WriteLine($"  Size: {rayHit8Size} bytes");
        output.WriteLine($"  Expected: {ray8Size + hit8Size} bytes (ray + hit)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCRayHit8), typeof(StructLayoutAttribute))}");
        output.WriteLine($"  Buffer calculation: {rayHit8Size} + 31 = {rayHit8Size + 31} bytes");
        output.WriteLine("");

        // RTCRay16 family
        var ray16Size = Marshal.SizeOf<RTCRay16>();
        var hit16Size = Marshal.SizeOf<RTCHit16>();
        var rayHit16Size = Marshal.SizeOf<RTCRayHit16>();

        output.WriteLine("RTCRay16:");
        output.WriteLine($"  Size: {ray16Size} bytes");
        output.WriteLine($"  Expected: 768 bytes (12 arrays * 16 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCRay16), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        output.WriteLine("RTCHit16:");
        output.WriteLine($"  Size: {hit16Size} bytes");
        output.WriteLine($"  Expected: 512 bytes (8 arrays * 16 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCHit16), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        output.WriteLine("RTCRayHit16:");
        output.WriteLine($"  Size: {rayHit16Size} bytes");
        output.WriteLine($"  Expected: {ray16Size + hit16Size} bytes (ray + hit)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCRayHit16), typeof(StructLayoutAttribute))}");
        output.WriteLine($"  Buffer calculation: {rayHit16Size} + 63 = {rayHit16Size + 63} bytes");
        output.WriteLine("");

        // Validate sizes
        // NOTE: RTCHit4/8/16 now include instPrimID[N] array (added in Embree 4 for instance arrays)
        // RTCHit4: 8 fields * 4 elements + instPrimID[4] = (8+1)*4*4 = 144 bytes
        // RTCHit8: 8 fields * 8 elements + instPrimID[8] = (8+1)*8*4 = 288 bytes
        // RTCHit16: 8 fields * 16 elements + instPrimID[16] = (8+1)*16*4 = 576 bytes
        Assert.Equal(192, ray4Size);
        Assert.Equal(144, hit4Size);  // Updated: was 128, now includes instPrimID[4]
        Assert.Equal(336, rayHit4Size);  // Updated: was 320

        Assert.Equal(384, ray8Size);
        Assert.Equal(288, hit8Size);  // Updated: was 256, now includes instPrimID[8]
        Assert.Equal(672, rayHit8Size);  // Updated: was 640

        Assert.Equal(768, ray16Size);
        Assert.Equal(576, hit16Size);  // Updated: was 512, now includes instPrimID[16]
        Assert.Equal(1344, rayHit16Size);  // Updated: was 1280
    }

    [Fact]
    public unsafe void CheckRTCPointQueryStructs()
    {
        // These structs are missing StructLayout attributes!
        output.WriteLine("Point Query Struct Analysis");
        output.WriteLine("===========================\n");

        var pq4Size = Marshal.SizeOf<RTCPointQuery4>();
        var pq8Size = Marshal.SizeOf<RTCPointQuery8>();
        var pq16Size = Marshal.SizeOf<RTCPointQuery16>();

        output.WriteLine("RTCPointQuery4:");
        output.WriteLine($"  Size: {pq4Size} bytes");
        output.WriteLine($"  Expected: 80 bytes (5 arrays * 4 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCPointQuery4), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        output.WriteLine("RTCPointQuery8:");
        output.WriteLine($"  Size: {pq8Size} bytes");
        output.WriteLine($"  Expected: 160 bytes (5 arrays * 8 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCPointQuery8), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        output.WriteLine("RTCPointQuery16:");
        output.WriteLine($"  Size: {pq16Size} bytes");
        output.WriteLine($"  Expected: 320 bytes (5 arrays * 16 elements * 4 bytes)");
        output.WriteLine($"  StructLayout: {Attribute.IsDefined(typeof(RTCPointQuery16), typeof(StructLayoutAttribute))}");
        output.WriteLine("");

        // These should equal expected sizes
        Assert.Equal(80, pq4Size);
        Assert.Equal(160, pq8Size);
        Assert.Equal(320, pq16Size);
    }

    [Fact]
    public unsafe void VerifyRay16HitAlignment()
    {
        output.WriteLine("RTCRay16/RTCHit16 Alignment Verification");
        output.WriteLine("=========================================\n");

        var ray16Size = Marshal.SizeOf<RTCRay16>();
        var hit16Size = Marshal.SizeOf<RTCHit16>();
        var rayHit16Size = Marshal.SizeOf<RTCRayHit16>();

        output.WriteLine($"RTCRay16 size:    {ray16Size} bytes");
        output.WriteLine($"RTCHit16 size:    {hit16Size} bytes");
        output.WriteLine($"RTCRayHit16 size: {rayHit16Size} bytes\n");

        // Expected sizes based on Embree 4 specification
        // NOTE: RTCHit16 now includes instPrimID[16] array (added in Embree 4 for instance arrays)
        int expectedRay16 = 12 * 16 * 4; // 12 arrays * 16 elements * 4 bytes
        int expectedHit16 = 9 * 16 * 4;  // 9 arrays * 16 elements * 4 bytes (was 8, now includes instPrimID)
        int expectedRayHit16 = expectedRay16 + expectedHit16;

        output.WriteLine("Expected sizes:");
        output.WriteLine($"  RTCRay16:    {expectedRay16} bytes (12 arrays * 16 elements * 4 bytes)");
        output.WriteLine($"  RTCHit16:    {expectedHit16} bytes (9 arrays * 16 elements * 4 bytes)");
        output.WriteLine($"  RTCRayHit16: {expectedRayHit16} bytes\n");

        // Verify sizes match
        output.WriteLine("Size verification:");
        output.WriteLine($"  RTCRay16:    {ray16Size} == {expectedRay16} ? {ray16Size == expectedRay16}");
        output.WriteLine($"  RTCHit16:    {hit16Size} == {expectedHit16} ? {hit16Size == expectedHit16}");
        output.WriteLine($"  RTCRayHit16: {rayHit16Size} == {expectedRayHit16} ? {rayHit16Size == expectedRayHit16}\n");

        // Verify RTCRay16 size is aligned to 64-byte boundary
        output.WriteLine("Alignment verification:");
        output.WriteLine($"  RTCRay16 size divisible by 64: {ray16Size % 64 == 0} ({ray16Size} / 64 = {ray16Size / 64})");

        // Get offset of hit field in RTCRayHit16
        var hitOffset = Marshal.OffsetOf<RTCRayHit16>("hit");
        output.WriteLine($"  Offset of 'hit' field in RTCRayHit16: {hitOffset} bytes");
        output.WriteLine($"  'hit' field aligned to 64 bytes: {(long)hitOffset % 64 == 0}");

        // Assertions
        Assert.Equal(expectedRay16, ray16Size);
        Assert.Equal(expectedHit16, hit16Size);
        Assert.Equal(expectedRayHit16, rayHit16Size);
        Assert.True(ray16Size % 64 == 0, $"RTCRay16 size ({ray16Size}) must be aligned to 64-byte boundary");
        Assert.True((long)hitOffset % 64 == 0, $"hit field offset ({hitOffset}) must be aligned to 64-byte boundary");
    }
}
