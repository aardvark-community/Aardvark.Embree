using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for generic buffer management, type safety, and update semantics.
/// </summary>
public class EmbreeBufferTests
{
    [Fact(DisplayName = "EmbreeBuffer creation with ReadOnlyMemory<V3f> succeeds")]
    public void EmbreeBufferCreation_ReadOnlyMemoryV3f_Succeeds()
    {
        using var device = new Device();
        var vertices = new V3f[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0) };
        ReadOnlyMemory<V3f> memory = vertices;

        using var buffer = EmbreeBuffer.Create(device, memory);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);
    }

    [Fact(DisplayName = "EmbreeBuffer creation with array succeeds")]
    public void EmbreeBufferCreation_Array_Succeeds()
    {
        using var device = new Device();
        var vertices = new V3f[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0) };

        using var buffer = EmbreeBuffer.Create(device, vertices);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);
    }

    [Fact(DisplayName = "EmbreeBuffer creation with int data succeeds")]
    public void EmbreeBufferCreation_IntData_Succeeds()
    {
        using var device = new Device();
        var indices = new int[] { 0, 1, 2 };

        using var buffer = EmbreeBuffer.Create(device, indices);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);
    }

    [Fact(DisplayName = "EmbreeBuffer creation with float data succeeds")]
    public void EmbreeBufferCreation_FloatData_Succeeds()
    {
        using var device = new Device();
        var floats = new float[] { 1.0f, 2.0f, 3.0f, 4.0f };

        using var buffer = EmbreeBuffer.Create(device, floats);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);
    }

    [Fact(DisplayName = "EmbreeBuffer supports Memory<T> slices")]
    public void EmbreeBufferCreation_MemorySlice_Succeeds()
    {
        using var device = new Device();
        var vertices = new V3f[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0), new(1, 1, 0) };
        ReadOnlyMemory<V3f> slice = vertices.AsMemory().Slice(1, 2);

        using var buffer = EmbreeBuffer.Create(device, slice);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);
    }

    [Fact(DisplayName = "EmbreeBuffer Update with same size succeeds")]
    public void EmbreeBufferUpdate_SameSize_Succeeds()
    {
        using var device = new Device();
        var vertices1 = new V3f[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0) };
        var vertices2 = new V3f[] { new(2, 2, 2), new(3, 3, 3), new(4, 4, 4) };

        using var buffer = EmbreeBuffer.Create(device, vertices1);
        buffer.Update(vertices2);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);
    }

    [Fact(DisplayName = "EmbreeBuffer Update with different size throws")]
    public void EmbreeBufferUpdate_DifferentSize_Throws()
    {
        using var device = new Device();
        var vertices1 = new V3f[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0) };
        var vertices2 = new V3f[] { new(2, 2, 2), new(3, 3, 3) };

        using var buffer = EmbreeBuffer.Create(device, vertices1);

        Assert.Throws<ArgumentException>(() => buffer.Update(vertices2));
    }

    [Fact(DisplayName = "EmbreeBuffer Dispose sets handle to zero")]
    public void EmbreeBufferDispose_SetsHandleToZero()
    {
        using var device = new Device();
        var vertices = new V3f[] { new(0, 0, 0), new(1, 0, 0), new(0, 1, 0) };
        var buffer = EmbreeBuffer.Create(device, vertices);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);

        buffer.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => buffer.Handle);
    }

    [Theory(DisplayName = "EmbreeBuffer shared buffer can be used by multiple geometries")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void EmbreeBuffer_SharedBuffer_MultipleGeometries(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0),
            new(1, 1, 0), new(2, 1, 0), new(1, 2, 0)
        };
        var indices1 = new int[] { 0, 1, 2 };
        var indices2 = new int[] { 3, 4, 5 };

        using var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        using var indexBuffer1 = EmbreeBuffer.Create(device, indices1);
        using var indexBuffer2 = EmbreeBuffer.Create(device, indices2);

        // Both geometries use the same vertex buffer
        using var geo1 = new TriangleGeometry(device, vertexBuffer, 0, 3, indexBuffer1, 0, 1, quality);
        using var geo2 = new TriangleGeometry(device, vertexBuffer, 3, 3, indexBuffer2, 0, 1, quality);

        Assert.NotEqual(IntPtr.Zero, geo1.Handle);
        Assert.NotEqual(IntPtr.Zero, geo2.Handle);
    }

    [Fact(DisplayName = "EmbreeBuffer can handle empty data")]
    public void EmbreeBufferCreation_EmptyData_Succeeds()
    {
        using var device = new Device();
        var vertices = Array.Empty<V3f>();

        using var buffer = EmbreeBuffer.Create(device, vertices);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);
    }

    [Fact(DisplayName = "EmbreeBuffer creation with large dataset succeeds")]
    public void EmbreeBufferCreation_LargeDataset_Succeeds()
    {
        using var device = new Device();
        var vertices = new V3f[100000];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new V3f(i, i, i);
        }

        using var buffer = EmbreeBuffer.Create(device, vertices);

        Assert.NotEqual(IntPtr.Zero, buffer.Handle);
    }
}
