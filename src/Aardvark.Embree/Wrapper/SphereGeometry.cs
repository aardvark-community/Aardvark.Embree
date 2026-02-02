using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Sphere geometry - points rendered as 3D spheres.
/// Spheres are true 3D primitives (unlike discs which are view-oriented).
/// </summary>
public class SphereGeometry : EmbreeGeometry
{
    private readonly EmbreeBuffer<Point> m_points;
    private readonly int m_pointCount;

    /// <summary>
    /// Creates sphere geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="points">Points with position and radius</param>
    /// <param name="quality">Build quality</param>
    public SphereGeometry(Device device, ReadOnlyMemory<Point> points, RTCBuildQuality quality)
        : base(device, RTCGeometryType.SpherePoint, quality)
    {
        m_pointCount = points.Length;
        m_points = EmbreeBuffer.Create(device, points);

        // Note: rtcSetGeometryPrimitiveCount does not exist in Embree 4.
        // The primitive count is determined by the buffer count parameter.

        // Point vertex buffer (FLOAT4 = x, y, z, radius)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT4,
            m_points.Handle, 0, (nuint)(sizeof(float) * 4), (nuint)points.Length);
        device.CheckError("SphereGeometry.rtcSetGeometryBuffer(Vertex)");

        // Enable geometry before commit
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("SphereGeometry.rtcEnableGeometry");

        Commit();
    }

    /// <summary>
    /// Creates sphere geometry from an existing buffer.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="pointBuffer">Point buffer</param>
    /// <param name="offset">Offset in buffer (in bytes)</param>
    /// <param name="count">Number of points</param>
    /// <param name="quality">Build quality</param>
    public SphereGeometry(Device device, EmbreeBuffer<Point> pointBuffer, int offset, int count, RTCBuildQuality quality)
        : base(device, RTCGeometryType.SpherePoint, quality)
    {
        EmbreeAPI.rtcRetainBuffer(pointBuffer.Handle);
        device.CheckError("SphereGeometry.rtcRetainBuffer");

        m_points = pointBuffer;
        m_pointCount = count;

        // Note: rtcSetGeometryPrimitiveCount does not exist in Embree 4.
        // The primitive count is determined by the buffer count parameter.

        // Point vertex buffer (FLOAT4 = x, y, z, radius)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT4,
            m_points.Handle, (nuint)offset, (nuint)(sizeof(float) * 4), (nuint)count);
        device.CheckError("SphereGeometry.rtcSetGeometryBuffer(Vertex)");

        // Enable geometry before commit
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("SphereGeometry.rtcEnableGeometry");

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
        m_device.CheckError("SphereGeometry.SetMask");
    }

    /// <summary>
    /// Updates the point buffer with new data.
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    public unsafe void UpdatePoints(ReadOnlyMemory<Point> points)
    {
        ThrowIfDisposed();
        if (points.Length != m_pointCount)
            throw new ArgumentException($"Point count ({points.Length}) does not match original count ({m_pointCount})", nameof(points));
        var span = points.Span;
        var ptr = m_points.GetDataPointer();
        for (int i = 0; i < span.Length; i++)
        {
            ptr[i] = span[i];
        }
    }

    /// <summary>
    /// Updates the point buffer with new data from a span (zero-copy).
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    public unsafe void UpdatePoints(ReadOnlySpan<Point> points)
    {
        ThrowIfDisposed();
        if (points.Length != m_pointCount)
            throw new ArgumentException($"Point count ({points.Length}) does not match original count ({m_pointCount})", nameof(points));
        var ptr = m_points.GetDataPointer();
        for (int i = 0; i < points.Length; i++)
        {
            ptr[i] = points[i];
        }
    }

    /// <summary>
    /// Gets a pointer to the point buffer data for direct in-place manipulation.
    /// After modifying the data, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    public unsafe Point* GetPointDataPointer()
    {
        ThrowIfDisposed();
        return m_points.GetDataPointer();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_points.Dispose();
        }
        base.Dispose(disposing);
    }
}
