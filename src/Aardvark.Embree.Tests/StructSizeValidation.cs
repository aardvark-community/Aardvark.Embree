using System.Runtime.InteropServices;
using Aardvark.Embree;
using Xunit;

namespace Aardvark.Embree.Tests;

public class StructSizeValidationTests
{
    [Fact]
    public void RTCRay_Size_Is48Bytes()
    {
        var size = Marshal.SizeOf<RTCRay>();
        Assert.Equal(48, size);
    }

    [Fact]
    public void RTCHit_Size_Is32Bytes()
    {
        var size = Marshal.SizeOf<RTCHit>();
        Assert.Equal(32, size);
    }

    [Fact]
    public void RTCRayHit_Size_Is80Bytes()
    {
        var size = Marshal.SizeOf<RTCRayHit>();
        Assert.Equal(80, size);
    }

    [Fact]
    public void RTCRay_tfar_OffsetIs32()
    {
        var offset = Marshal.OffsetOf<RTCRay>("tfar").ToInt32();
        Assert.Equal(32, offset);
    }

    [Fact]
    public void RTCHit_instID_0_OffsetIs28()
    {
        var offset = Marshal.OffsetOf<RTCHit>("instID_0").ToInt32();
        Assert.Equal(28, offset);
    }

    [Fact]
    public void RTCPointQuery_Size_Is20Bytes()
    {
        // sizeof(float) * 5 = 20 bytes (x, y, z, time, radius)
        var size = Marshal.SizeOf<RTCPointQuery>();
        Assert.Equal(20, size);
    }

    [Fact]
    public void RTCPointQueryContext_Size_Is136Bytes()
    {
        // M44f is 16 floats each = 32 floats total = 128 bytes
        // + 2 uints (instID + instStackSize) = 8 bytes = 136 bytes total
        var size = Marshal.SizeOf<RTCPointQueryContext>();
        Assert.Equal(136, size);
    }

    [Fact]
    public void RTCPointQueryContext_instStackSize_OffsetIs132()
    {
        // instStackSize must be the LAST field at offset 132 (after 128 bytes of matrices + 4 bytes instID)
        var offset = Marshal.OffsetOf<RTCPointQueryContext>("instStackSize").ToInt32();
        Assert.Equal(132, offset);
    }
}
