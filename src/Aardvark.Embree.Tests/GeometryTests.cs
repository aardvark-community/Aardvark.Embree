using Aardvark.Base;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for TriangleGeometry creation, validation, updates, and motion blur support.
/// </summary>
public class TriangleGeometryTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateTriangleGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanCreateTriangleGeometryWithMultipleTriangles()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0),
            new V3f(1, 1, 0)
        };

        var indices = new int[] { 0, 1, 2, 1, 3, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanCreateTriangleGeometryFromBuffers()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var indexBuffer = EmbreeBuffer.Create(device, indices);

        using var geometry = new TriangleGeometry(device, vertexBuffer, 0, vertices.Length, indexBuffer, 0, 1, RTCBuildQuality.Medium);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanCreateTriangleGeometryFromBuffersWithOffset()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(-1, -1, -1),
            new V3f(-2, -2, -2),
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { -1, -2, -3, 2, 3, 4 };

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var indexBuffer = EmbreeBuffer.Create(device, indices);

        using var geometry = new TriangleGeometry(device, vertexBuffer, 2 * sizeof(float) * 3, 3, indexBuffer, 3 * sizeof(int), 1, RTCBuildQuality.Medium);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanAttachTriangleGeometryToScene()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);
        using var scene = new Scene(device, RTCBuildQuality.Medium, false);

        var geomId = scene.AttachGeometry(geometry);

        Assert.True(geomId >= 0);
    }

    [Fact]
    public void CanDisableTriangleGeometry()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        EmbreeAPI.rtcDisableGeometry(geometry.Handle);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanEnableTriangleGeometry()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        EmbreeAPI.rtcDisableGeometry(geometry.Handle);
        EmbreeAPI.rtcEnableGeometry(geometry.Handle);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanSetGeometryMask()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        EmbreeAPI.rtcSetGeometryMask(geometry.Handle, 0xFF);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanCommitTriangleGeometry()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        geometry.Commit();

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanCreateLargeTriangleGeometry()
    {
        using var device = new Device();

        var vertexCount = 10000;
        var triangleCount = vertexCount / 3;

        var vertices = new V3f[vertexCount];
        var indices = new int[triangleCount * 3];

        var random = new Random(42);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = new V3f(
                (float)random.NextDouble() * 100,
                (float)random.NextDouble() * 100,
                (float)random.NextDouble() * 100
            );
        }

        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = i;
        }

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanDisposeTriangleGeometry()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);

        geometry.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => geometry.Handle);
    }
}

public class QuadGeometryTests
{
    [Fact]
    public void CanCreateQuadGeometry()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var geometry = new EmbreeGeometry(device, RTCGeometryType.Quad, RTCBuildQuality.Medium);

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var indexBuffer = EmbreeBuffer.Create(device, indices);

        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, indexBuffer.Handle, 0, (nuint)(sizeof(int) * 4), (nuint)1);
        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, vertexBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

        geometry.Commit();

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanCreateQuadGeometryWithMultipleQuads()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0),
            new V3f(1, 0, 0),
            new V3f(2, 0, 0),
            new V3f(2, 1, 0),
            new V3f(1, 1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3, 4, 5, 6, 7 };

        using var geometry = new EmbreeGeometry(device, RTCGeometryType.Quad, RTCBuildQuality.Medium);

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var indexBuffer = EmbreeBuffer.Create(device, indices);

        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, indexBuffer.Handle, 0, sizeof(int) * 4, 2);
        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, vertexBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

        geometry.Commit();

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanAttachQuadGeometryToScene()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var geometry = new EmbreeGeometry(device, RTCGeometryType.Quad, RTCBuildQuality.Medium);

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var indexBuffer = EmbreeBuffer.Create(device, indices);

        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, indexBuffer.Handle, 0, (nuint)(sizeof(int) * 4), (nuint)1);
        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, vertexBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

        geometry.Commit();

        using var scene = new Scene(device, RTCBuildQuality.Medium, false);

        var geomId = scene.AttachGeometry(geometry);

        Assert.True(geomId >= 0);
    }

    [Fact]
    public void CanDisableQuadGeometry()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var geometry = new EmbreeGeometry(device, RTCGeometryType.Quad, RTCBuildQuality.Medium);

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var indexBuffer = EmbreeBuffer.Create(device, indices);

        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, indexBuffer.Handle, 0, (nuint)(sizeof(int) * 4), (nuint)1);
        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, vertexBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

        geometry.Commit();

        EmbreeAPI.rtcDisableGeometry(geometry.Handle);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanEnableQuadGeometry()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var geometry = new EmbreeGeometry(device, RTCGeometryType.Quad, RTCBuildQuality.Medium);

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var indexBuffer = EmbreeBuffer.Create(device, indices);

        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, indexBuffer.Handle, 0, (nuint)(sizeof(int) * 4), (nuint)1);
        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, vertexBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

        geometry.Commit();

        EmbreeAPI.rtcDisableGeometry(geometry.Handle);
        EmbreeAPI.rtcEnableGeometry(geometry.Handle);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanSetQuadGeometryMask()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        using var geometry = new EmbreeGeometry(device, RTCGeometryType.Quad, RTCBuildQuality.Medium);

        var vertexBuffer = EmbreeBuffer.Create(device, vertices);
        var indexBuffer = EmbreeBuffer.Create(device, indices);

        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, indexBuffer.Handle, 0, (nuint)(sizeof(int) * 4), (nuint)1);
        EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, vertexBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

        geometry.Commit();

        EmbreeAPI.rtcSetGeometryMask(geometry.Handle, 0xFF);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanCreateQuadGeometryWithDifferentBuildQualities()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2, 3 };

        var qualities = new[] { RTCBuildQuality.Low, RTCBuildQuality.Medium, RTCBuildQuality.High };

        foreach (var quality in qualities)
        {
            using var geometry = new EmbreeGeometry(device, RTCGeometryType.Quad, quality);

            var vertexBuffer = EmbreeBuffer.Create(device, vertices);
            var indexBuffer = EmbreeBuffer.Create(device, indices);

            EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, indexBuffer.Handle, 0, (nuint)(sizeof(int) * 4), (nuint)1);
            EmbreeAPI.rtcSetGeometryBuffer(geometry.Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, vertexBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

            geometry.Commit();

            Assert.NotEqual(IntPtr.Zero, geometry.Handle);
        }
    }
}

public class UserGeometryTests
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void BoundsFunction(RTCBoundsFunctionArguments* args);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void IntersectFunction(RTCIntersectFunctionNArguments* args);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void OccludedFunction(RTCOccludedFunctionNArguments* args);

    [StructLayout(LayoutKind.Sequential)]
    private struct RTCBoundsFunctionArguments
    {
        public IntPtr geometryUserPtr;
        public uint primID;
        public uint timeStep;
        public IntPtr bounds_o;
    }

    [Fact]
    public void CanCreateUserGeometry()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanSetUserGeometryPrimitiveCount()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        EmbreeAPI.rtcSetGeometryUserPrimitiveCount(geometry.Handle, 10);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public unsafe void CanSetUserGeometryBoundsFunction()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        BoundsFunction boundsFunc = (RTCBoundsFunctionArguments* args) =>
        {
            var bounds = (RTCBounds*)args->bounds_o;
            bounds->lower = new V3f(-1, -1, -1);
            bounds->upper = new V3f(1, 1, 1);
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(boundsFunc);
        EmbreeAPI.rtcSetGeometryBoundsFunction(geometry.Handle, funcPtr, IntPtr.Zero);

        EmbreeAPI.rtcSetGeometryUserPrimitiveCount(geometry.Handle, 1);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);

        GC.KeepAlive(boundsFunc);
    }

    [Fact]
    public unsafe void CanSetUserGeometryIntersectFunction()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        IntersectFunction intersectFunc = (RTCIntersectFunctionNArguments* args) =>
        {
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(intersectFunc);
        EmbreeAPI.rtcSetGeometryIntersectFunction(geometry.Handle, funcPtr);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);

        GC.KeepAlive(intersectFunc);
    }

    [Fact]
    public unsafe void CanSetUserGeometryOccludedFunction()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        OccludedFunction occludedFunc = (RTCOccludedFunctionNArguments* args) =>
        {
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(occludedFunc);
        EmbreeAPI.rtcSetGeometryOccludedFunction(geometry.Handle, funcPtr);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);

        GC.KeepAlive(occludedFunc);
    }

    [Fact]
    public void CanSetUserGeometryUserData()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        var userData = new IntPtr(12345);
        EmbreeAPI.rtcSetGeometryUserData(geometry.Handle, userData);

        var retrievedData = EmbreeAPI.rtcGetGeometryUserData(geometry.Handle);

        Assert.Equal(userData, retrievedData);
    }

    [Fact]
    public void CanAttachUserGeometryToScene()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        EmbreeAPI.rtcSetGeometryUserPrimitiveCount(geometry.Handle, 1);
        geometry.Commit();

        using var scene = new Scene(device, RTCBuildQuality.Medium, false);

        var geomId = scene.AttachGeometry(geometry);

        Assert.True(geomId >= 0);
    }

    [Fact]
    public void CanDisableUserGeometry()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        EmbreeAPI.rtcSetGeometryUserPrimitiveCount(geometry.Handle, 1);
        geometry.Commit();

        EmbreeAPI.rtcDisableGeometry(geometry.Handle);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanEnableUserGeometry()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        EmbreeAPI.rtcSetGeometryUserPrimitiveCount(geometry.Handle, 1);
        geometry.Commit();

        EmbreeAPI.rtcDisableGeometry(geometry.Handle);
        EmbreeAPI.rtcEnableGeometry(geometry.Handle);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanSetUserGeometryMask()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        EmbreeAPI.rtcSetGeometryMask(geometry.Handle, 0xFF);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanCommitUserGeometry()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        EmbreeAPI.rtcSetGeometryUserPrimitiveCount(geometry.Handle, 1);

        geometry.Commit();

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanDisposeUserGeometry()
    {
        using var device = new Device();
        var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        geometry.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => geometry.Handle);
    }

    [Fact]
    public void CanSetUserGeometryIntersectFilterFunction()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry.Handle, IntPtr.Zero);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void CanSetUserGeometryOccludedFilterFunction()
    {
        using var device = new Device();
        using var geometry = new EmbreeGeometry(device, RTCGeometryType.User);

        EmbreeAPI.rtcSetGeometryOccludedFilterFunction(geometry.Handle, IntPtr.Zero);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }
}
