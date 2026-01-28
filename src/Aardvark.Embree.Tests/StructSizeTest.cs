using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

public class StructSizeTest
{
    [Fact]
    public void PrintStructSizes()
    {
        Console.WriteLine($"RTCRay size: {Marshal.SizeOf<RTCRay>()} bytes");
        Console.WriteLine($"RTCHit size: {Marshal.SizeOf<RTCHit>()} bytes");
        Console.WriteLine($"RTCRayHit size: {Marshal.SizeOf<RTCRayHit>()} bytes");
        Console.WriteLine($"V3f size: {Marshal.SizeOf<V3f>()} bytes");
        Console.WriteLine($"V2f size: {Marshal.SizeOf<V2f>()} bytes");
        Console.WriteLine();

        // Expected sizes from C++:
        // RTCRay: 12 floats = 48 bytes
        // RTCHit: 3 floats (Ng) + 2 floats (uv) + 3 uints (primID, geomID, instID) = 32 bytes
        Console.WriteLine("Expected from C++:");
        Console.WriteLine("RTCRay: 48 bytes (12 floats)");
        Console.WriteLine("RTCHit: 32 bytes (5 floats + 3 uints)");
        Console.WriteLine("RTCRayHit: 80 bytes (48 + 32)");

        unsafe
        {
            RTCRay ray = new RTCRay();
            Console.WriteLine($"\nRTCRay field offsets:");
            Console.WriteLine($"  org: {(long)&ray.org - (long)&ray} bytes");
            Console.WriteLine($"  tnear: {(long)&ray.tnear - (long)&ray} bytes");
            Console.WriteLine($"  dir: {(long)&ray.dir - (long)&ray} bytes");
            Console.WriteLine($"  time: {(long)&ray.time - (long)&ray} bytes");
            Console.WriteLine($"  tfar: {(long)&ray.tfar - (long)&ray} bytes");
            Console.WriteLine($"  mask: {(long)&ray.mask - (long)&ray} bytes");
            Console.WriteLine($"  id: {(long)&ray.id - (long)&ray} bytes");
            Console.WriteLine($"  flags: {(long)&ray.flags - (long)&ray} bytes");

            RTCHit hit = new RTCHit();
            Console.WriteLine($"\nRTCHit field offsets:");
            Console.WriteLine($"  Ng: {(long)&hit.Ng - (long)&hit} bytes");
            Console.WriteLine($"  uv: {(long)&hit.uv - (long)&hit} bytes");
            Console.WriteLine($"  primID: {(long)&hit.primID - (long)&hit} bytes");
            Console.WriteLine($"  geomID: {(long)&hit.geomID - (long)&hit} bytes");
            Console.WriteLine($"  instID_0: {(long)&hit.instID_0 - (long)&hit} bytes");
        }
    }
}
