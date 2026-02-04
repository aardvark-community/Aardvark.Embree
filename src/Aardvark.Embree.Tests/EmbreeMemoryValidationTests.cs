using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Aardvark.Embree.Tests;

public class EmbreeMemoryValidationTests
{
    [Fact]
    public void ValidateRayHitAlignment_AllowsAligned()
    {
        IntPtr ptr = IntPtr.Zero;
        try
        {
            ptr = EmbreeMemory.AllocRayHit(out _, out _);
            EmbreeMemory.ValidateRayHitAlignment(ptr);
        }
        finally
        {
            EmbreeMemory.FreeAligned(ptr);
        }
    }

    [Fact]
    public void ValidateRayHitAlignment_ThrowsOnMisalignedPointerOnMacosIntel()
    {
        var size = Marshal.SizeOf<RTCRayHit>() + 1;
        IntPtr raw = Marshal.AllocHGlobal(size);
        try
        {
            var misaligned = IntPtr.Add(raw, 1);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
                RuntimeInformation.ProcessArchitecture == Architecture.X64)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    EmbreeMemory.ValidateRayHitAlignment(misaligned, "test"));
            }
            else
            {
                EmbreeMemory.ValidateRayHitAlignment(misaligned, "test");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(raw);
        }
    }
}
