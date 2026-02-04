using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Aardvark.Embree.Tests;

public class EmbreeMemoryTests
{
    [Fact]
    public void RayHitAllocationIsAligned()
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = EmbreeMemory.AllocRayHit(out var size, out var alignment);

            Assert.True(size >= (nuint)Marshal.SizeOf<RTCRayHit>());
            Assert.True(alignment == 16 || alignment == 32);

            var address = (nuint)ptr;
            Assert.Equal(0u, (uint)(address % alignment));

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                Assert.Equal((nuint)32, alignment);
            }
            else
            {
                Assert.Equal((nuint)16, alignment);
            }
        }
        finally
        {
            EmbreeMemory.FreeAligned(ptr);
        }
    }
}
