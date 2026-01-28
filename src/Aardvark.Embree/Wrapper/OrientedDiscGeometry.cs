using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Oriented disc geometry - points rendered as flat circular discs with explicit normal directions.
/// Unlike regular discs (which are view-oriented), oriented discs have a fixed orientation defined by their normals.
/// </summary>
public class OrientedDiscGeometry : EmbreeGeometry
{
    private readonly EmbreeBuffer<Point> m_positions;
    private readonly EmbreeBuffer<V3f> m_normals;

    /// <summary>
    /// Creates oriented disc geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="points">Points with position, radius, and normal</param>
    /// <param name="quality">Build quality</param>
    public OrientedDiscGeometry(Device device, ReadOnlyMemory<OrientedPoint> points, RTCBuildQuality quality)
        : base(device, RTCGeometryType.OrientedDiscPoint, quality)
    {
        // Extract positions (with radius) and normals into separate arrays
        var positions = new Point[points.Length];
        var normals = new V3f[points.Length];

        var span = points.Span;
        for (int i = 0; i < points.Length; i++)
        {
            positions[i] = new Point(span[i].Position, span[i].Radius);
            normals[i] = span[i].Normal;
        }

        m_positions = EmbreeBuffer.Create(device, positions);
        m_normals = EmbreeBuffer.Create(device, normals);

        // Note: rtcSetGeometryPrimitiveCount does not exist in Embree 4.
        // The primitive count is determined by the buffer count parameter.

        // Point vertex buffer (FLOAT4 = x, y, z, radius)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT4,
            m_positions.Handle, 0, (nuint)(sizeof(float) * 4), (nuint)points.Length);
        device.CheckError("OrientedDiscGeometry.rtcSetGeometryBuffer(Vertex)");

        // Normal buffer (FLOAT3)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Normal, 0, RTCFormat.FLOAT3,
            m_normals.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)normals.Length);
        device.CheckError("OrientedDiscGeometry.rtcSetGeometryBuffer(Normal)");

        // Enable geometry before commit
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("OrientedDiscGeometry.rtcEnableGeometry");

        Commit();
    }

    /// <summary>
    /// Creates oriented disc geometry from existing buffers.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="positionBuffer">Position buffer (Point with position and radius)</param>
    /// <param name="normalBuffer">Normal buffer (V3f)</param>
    /// <param name="offset">Offset in buffers (in bytes)</param>
    /// <param name="count">Number of points</param>
    /// <param name="quality">Build quality</param>
    public OrientedDiscGeometry(Device device, EmbreeBuffer<Point> positionBuffer, EmbreeBuffer<V3f> normalBuffer,
                                int offset, int count, RTCBuildQuality quality)
        : base(device, RTCGeometryType.OrientedDiscPoint, quality)
    {
        EmbreeAPI.rtcRetainBuffer(positionBuffer.Handle);
        device.CheckError("OrientedDiscGeometry.rtcRetainBuffer(Position)");
        EmbreeAPI.rtcRetainBuffer(normalBuffer.Handle);
        device.CheckError("OrientedDiscGeometry.rtcRetainBuffer(Normal)");

        m_positions = positionBuffer;
        m_normals = normalBuffer;

        // Note: rtcSetGeometryPrimitiveCount does not exist in Embree 4.
        // The primitive count is determined by the buffer count parameter.

        // Point vertex buffer (FLOAT4 = x, y, z, radius)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT4,
            m_positions.Handle, (nuint)offset, (nuint)(sizeof(float) * 4), (nuint)count);
        device.CheckError("OrientedDiscGeometry.rtcSetGeometryBuffer(Vertex)");

        // Normal buffer (FLOAT3)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Normal, 0, RTCFormat.FLOAT3,
            m_normals.Handle, (nuint)offset, (nuint)(sizeof(float) * 3), (nuint)count);
        device.CheckError("OrientedDiscGeometry.rtcSetGeometryBuffer(Normal)");

        // Enable geometry before commit
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("OrientedDiscGeometry.rtcEnableGeometry");

        Commit();
    }

    /// <summary>
    /// Sets the ray mask for this geometry.
    /// The mask is used to filter rays based on their mask value.
    /// Only rays with (ray.mask &amp; geometry.mask) != 0 will be tested.
    /// </summary>
    public void SetMask(uint mask)
    {
        ThrowIfDisposed();
        EmbreeAPI.rtcSetGeometryMask(Handle, mask);
        m_device.CheckError("OrientedDiscGeometry.SetMask");
    }

    /// <summary>
    /// Updates the position buffer with new data.
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    public unsafe void UpdatePositions(ReadOnlyMemory<Point> positions)
    {
        ThrowIfDisposed();
        var span = positions.Span;
        var ptr = m_positions.GetDataPointer();
        for (int i = 0; i < span.Length; i++)
        {
            ptr[i] = span[i];
        }
    }

    /// <summary>
    /// Updates the position buffer with new data from a span (zero-copy).
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    public unsafe void UpdatePositions(ReadOnlySpan<Point> positions)
    {
        ThrowIfDisposed();
        var ptr = m_positions.GetDataPointer();
        for (int i = 0; i < positions.Length; i++)
        {
            ptr[i] = positions[i];
        }
    }

    /// <summary>
    /// Updates the normal buffer with new data.
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Normal) and Commit().
    /// </summary>
    public unsafe void UpdateNormals(ReadOnlyMemory<V3f> normals)
    {
        ThrowIfDisposed();
        var span = normals.Span;
        var ptr = m_normals.GetDataPointer();
        for (int i = 0; i < span.Length; i++)
        {
            ptr[i] = span[i];
        }
    }

    /// <summary>
    /// Updates the normal buffer with new data from a span (zero-copy).
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Normal) and Commit().
    /// </summary>
    public unsafe void UpdateNormals(ReadOnlySpan<V3f> normals)
    {
        ThrowIfDisposed();
        var ptr = m_normals.GetDataPointer();
        for (int i = 0; i < normals.Length; i++)
        {
            ptr[i] = normals[i];
        }
    }

    /// <summary>
    /// Gets a pointer to the position buffer data for direct in-place manipulation.
    /// After modifying the data, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    public unsafe Point* GetPositionDataPointer()
    {
        ThrowIfDisposed();
        return m_positions.GetDataPointer();
    }

    /// <summary>
    /// Gets a pointer to the normal buffer data for direct in-place manipulation.
    /// After modifying the data, you must call UpdateBuffer(RTCBufferType.Normal) and Commit().
    /// </summary>
    public unsafe V3f* GetNormalDataPointer()
    {
        ThrowIfDisposed();
        return m_normals.GetDataPointer();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_positions.Dispose();
            m_normals.Dispose();
        }
        base.Dispose(disposing);
    }
}
