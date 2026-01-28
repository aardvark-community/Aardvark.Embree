using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

public class DetailedStructLayoutTest
{
    [Fact]
    public void PrintDetailedStructLayout()
    {
        Console.WriteLine("=== RTCRay layout ===");
        Console.WriteLine($"Size: {Marshal.SizeOf<RTCRay>()} bytes");
        unsafe
        {
            RTCRay ray = new RTCRay();
            long baseAddr = (long)&ray;
            Console.WriteLine($"  org (V3f):   offset {(long)&ray.org - baseAddr,3} bytes, size {sizeof(V3f)} bytes");
            Console.WriteLine($"  tnear:       offset {(long)&ray.tnear - baseAddr,3} bytes, size {sizeof(float)} bytes");
            Console.WriteLine($"  dir (V3f):   offset {(long)&ray.dir - baseAddr,3} bytes, size {sizeof(V3f)} bytes");
            Console.WriteLine($"  time:        offset {(long)&ray.time - baseAddr,3} bytes, size {sizeof(float)} bytes");
            Console.WriteLine($"  tfar:        offset {(long)&ray.tfar - baseAddr,3} bytes, size {sizeof(float)} bytes");
            Console.WriteLine($"  mask:        offset {(long)&ray.mask - baseAddr,3} bytes, size {sizeof(uint)} bytes");
            Console.WriteLine($"  id:          offset {(long)&ray.id - baseAddr,3} bytes, size {sizeof(uint)} bytes");
            Console.WriteLine($"  flags:       offset {(long)&ray.flags - baseAddr,3} bytes, size {sizeof(uint)} bytes");
        }

        Console.WriteLine("\n=== RTCHit layout ===");
        Console.WriteLine($"Size: {Marshal.SizeOf<RTCHit>()} bytes");
        unsafe
        {
            RTCHit hit = new RTCHit();
            long baseAddr = (long)&hit;
            Console.WriteLine($"  Ng (V3f):    offset {(long)&hit.Ng - baseAddr,3} bytes, size {sizeof(V3f)} bytes");
            Console.WriteLine($"  uv (V2f):    offset {(long)&hit.uv - baseAddr,3} bytes, size {sizeof(V2f)} bytes");
            Console.WriteLine($"  primID:      offset {(long)&hit.primID - baseAddr,3} bytes, size {sizeof(uint)} bytes");
            Console.WriteLine($"  geomID:      offset {(long)&hit.geomID - baseAddr,3} bytes, size {sizeof(uint)} bytes");
            Console.WriteLine($"  instID_0:    offset {(long)&hit.instID_0 - baseAddr,3} bytes, size {sizeof(uint)} bytes");
        }

        Console.WriteLine("\n=== RTCRayHit layout ===");
        Console.WriteLine($"Size: {Marshal.SizeOf<RTCRayHit>()} bytes");
        unsafe
        {
            RTCRayHit rayhit = new RTCRayHit();
            long baseAddr = (long)&rayhit;
            Console.WriteLine($"  ray:         offset {(long)&rayhit.ray - baseAddr,3} bytes, size {sizeof(RTCRay)} bytes");
            Console.WriteLine($"  hit:         offset {(long)&rayhit.hit - baseAddr,3} bytes, size {sizeof(RTCHit)} bytes");
        }

        Console.WriteLine("\n=== Expected from C++ ===");
        Console.WriteLine("RTCRay: 48 bytes");
        Console.WriteLine("  org_x..org_z, tnear: 0-15 (4 floats)");
        Console.WriteLine("  dir_x..dir_z, time: 16-31 (4 floats)");
        Console.WriteLine("  tfar, mask, id, flags: 32-47 (4 values)");
        Console.WriteLine("\nRTCHit: 32 bytes");
        Console.WriteLine("  Ng_x, Ng_y, Ng_z: 0-11 (3 floats)");
        Console.WriteLine("  u, v: 12-19 (2 floats)");
        Console.WriteLine("  primID, geomID, instID[0]: 20-31 (3 uints)");
        Console.WriteLine("\nRTCRayHit: 80 bytes (48 + 32)");
    }
}
