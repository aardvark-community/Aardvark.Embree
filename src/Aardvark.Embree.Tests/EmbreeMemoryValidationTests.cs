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
    public void ValidateRayHitAlignment_ThrowsOnMisalignedPointer()
    {
        var size = Marshal.SizeOf<RTCRayHit>() + 64;
        IntPtr raw = Marshal.AllocHGlobal(size);
        try
        {
            var misaligned = IntPtr.Add(raw, 1);
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidateRayHitAlignment(misaligned, "test"));
        }
        finally
        {
            Marshal.FreeHGlobal(raw);
        }
    }

    [Fact]
    public void ValidateRayHitPacketAlignments_ThrowOnMisalignedPointer()
    {
        IntPtr raw = Marshal.AllocHGlobal(256);
        try
        {
            var misaligned = IntPtr.Add(raw, 1);
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidateRayHit4Alignment(misaligned, "rayhit4"));
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidateRayHit8Alignment(misaligned, "rayhit8"));
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidateRayHit16Alignment(misaligned, "rayhit16"));
        }
        finally
        {
            Marshal.FreeHGlobal(raw);
        }
    }

    [Fact]
    public void ValidatePointQueryAlignments_ThrowOnMisalignedPointer()
    {
        IntPtr raw = Marshal.AllocHGlobal(256);
        try
        {
            var misaligned = IntPtr.Add(raw, 1);
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidatePointQueryAlignment(misaligned, "pq"));
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidatePointQuery4Alignment(misaligned, "pq4"));
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidatePointQuery8Alignment(misaligned, "pq8"));
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidatePointQuery16Alignment(misaligned, "pq16"));
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidatePointQueryContextAlignment(misaligned, "pqctx"));
        }
        finally
        {
            Marshal.FreeHGlobal(raw);
        }
    }

    [Fact]
    public void ValidateBoundsAlignments_ThrowOnMisalignedPointer()
    {
        IntPtr raw = Marshal.AllocHGlobal(256);
        try
        {
            var misaligned = IntPtr.Add(raw, 1);
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidateBoundsAlignment(misaligned, "bounds"));
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidateLinearBoundsAlignment(misaligned, "linear"));
        }
        finally
        {
            Marshal.FreeHGlobal(raw);
        }
    }

    [Fact]
    public void ValidateBuildPrimitiveAlignment_ThrowsOnMisalignedPointer()
    {
        IntPtr raw = Marshal.AllocHGlobal(256);
        try
        {
            var misaligned = IntPtr.Add(raw, 1);
            Assert.Throws<InvalidOperationException>(() =>
                EmbreeMemory.ValidateBuildPrimitiveAlignment(misaligned, "prims"));
        }
        finally
        {
            Marshal.FreeHGlobal(raw);
        }
    }
}
