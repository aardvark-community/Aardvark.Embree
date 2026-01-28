using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Point with position and radius.
/// </summary>
/// <remarks>
/// Used for sphere and disc point geometries where orientation is not specified.
/// The point is rendered as a sphere or disc centered at Position with the given Radius.
/// </remarks>
public struct Point
{
    /// <summary>
    /// The center position of the point.
    /// </summary>
    public V3f Position;

    /// <summary>
    /// The radius of the point (sphere or disc).
    /// </summary>
    public float Radius;

    /// <summary>
    /// Initializes a new point with position and radius.
    /// </summary>
    /// <param name="position">The center position</param>
    /// <param name="radius">The radius</param>
    public Point(V3f position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}

/// <summary>
/// Oriented disc point with position, normal, and radius.
/// </summary>
/// <remarks>
/// Used for oriented disc point geometries where the disc orientation is explicitly specified.
/// The disc is rendered perpendicular to the Normal vector.
/// </remarks>
public struct OrientedPoint
{
    /// <summary>
    /// The center position of the disc.
    /// </summary>
    public V3f Position;

    /// <summary>
    /// The radius of the disc.
    /// </summary>
    public float Radius;

    /// <summary>
    /// The normal vector defining the disc orientation (perpendicular to the disc plane).
    /// </summary>
    public V3f Normal;

    /// <summary>
    /// Initializes a new oriented point with position, radius, and normal.
    /// </summary>
    /// <param name="position">The center position</param>
    /// <param name="radius">The radius</param>
    /// <param name="normal">The normal vector</param>
    public OrientedPoint(V3f position, float radius, V3f normal)
    {
        Position = position;
        Radius = radius;
        Normal = normal;
    }
}

/// <summary>
/// Base class for point geometries.
/// </summary>
/// <remarks>
/// Point geometries render individual points as 3D primitives (spheres or discs).
/// Use SpherePointGeometry for spherical points, DiscPointGeometry for flat circular discs,
/// or OrientedDiscPointGeometry for discs with explicit normals.
/// </remarks>
public abstract class PointGeometry : EmbreeGeometry
{
    /// <summary>
    /// Initializes a new point geometry with the specified type and quality.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="type">The specific point geometry type (Sphere, Disc, or OrientedDisc)</param>
    /// <param name="quality">The build quality for acceleration structures</param>
    protected PointGeometry(Device device, RTCGeometryType type, RTCBuildQuality quality)
        : base(device, type, quality)
    {
    }
}

/// <summary>
/// Sphere point geometry - points rendered as spheres.
/// </summary>
/// <remarks>
/// Each point is rendered as a perfect sphere with the specified radius.
/// Use this for volume-like point representations where the primitive is visible from all angles.
/// Spheres are more expensive to render than discs but provide correct appearance from any viewpoint.
/// </remarks>
public class SpherePointGeometry : PointGeometry
{
    private readonly EmbreeBuffer<Point> m_points;

    /// <summary>
    /// Creates sphere point geometry from a collection of points.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="points">Array of points with position and radius</param>
    /// <param name="quality">Build quality for acceleration structures</param>
    public SpherePointGeometry(Device device, ReadOnlyMemory<Point> points, RTCBuildQuality quality)
        : base(device, RTCGeometryType.SpherePoint, quality)
    {
        m_points = EmbreeBuffer.Create(device, points);

        // Point vertex buffer (FLOAT4 = x, y, z, radius)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT4, m_points.Handle, 0, (nuint)(sizeof(float) * 4), (nuint)points.Length);

        Commit();

        device.CheckError("Create SpherePointGeometry");
    }

    /// <summary>
    /// Updates the point buffer with new data.
    /// </summary>
    /// <param name="points">New point data to copy into the buffer</param>
    /// <remarks>
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit() to update the acceleration structure.
    /// </remarks>
    public unsafe void UpdatePoints(ReadOnlyMemory<Point> points)
    {
        var span = points.Span;
        var ptr = m_points.GetDataPointer();
        for (int i = 0; i < span.Length; i++)
        {
            ptr[i] = span[i];
        }
    }

    /// <summary>
    /// Updates the point buffer with new data from a span (zero-copy).
    /// </summary>
    /// <param name="points">New point data to copy into the buffer</param>
    /// <remarks>
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit() to update the acceleration structure.
    /// This overload avoids allocation by directly accepting a span.
    /// </remarks>
    public unsafe void UpdatePoints(ReadOnlySpan<Point> points)
    {
        var ptr = m_points.GetDataPointer();
        for (int i = 0; i < points.Length; i++)
        {
            ptr[i] = points[i];
        }
    }

    /// <summary>
    /// Gets a pointer to the point buffer data for direct in-place manipulation.
    /// </summary>
    /// <returns>Pointer to the point buffer</returns>
    /// <remarks>
    /// After modifying the data through the pointer, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit() to update the acceleration structure.
    /// Use with caution - direct pointer manipulation bypasses safety checks.
    /// </remarks>
    public unsafe Point* GetPointDataPointer() => m_points.GetDataPointer();

    /// <summary>
    /// Disposes resources used by the sphere point geometry.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
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

/// <summary>
/// Disc point geometry - points rendered as discs (flat circles).
/// </summary>
/// <remarks>
/// Each point is rendered as a flat circular disc with the specified radius.
/// The disc orientation is automatically determined to face the camera (billboard behavior).
/// Use this for efficient point rendering when spherical appearance is not required.
/// Discs are cheaper to render than spheres but may appear incorrect when viewed at grazing angles.
/// </remarks>
public class DiscPointGeometry : PointGeometry
{
    private readonly EmbreeBuffer<Point> m_points;

    /// <summary>
    /// Creates disc point geometry from a collection of points.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="points">Array of points with position and radius</param>
    /// <param name="quality">Build quality for acceleration structures</param>
    public DiscPointGeometry(Device device, ReadOnlyMemory<Point> points, RTCBuildQuality quality)
        : base(device, RTCGeometryType.DiscPoint, quality)
    {
        m_points = EmbreeBuffer.Create(device, points);

        // Point vertex buffer (FLOAT4 = x, y, z, radius)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT4, m_points.Handle, 0, (nuint)(sizeof(float) * 4), (nuint)points.Length);

        Commit();

        device.CheckError("Create DiscPointGeometry");
    }

    /// <summary>
    /// Updates the point buffer with new data.
    /// </summary>
    /// <param name="points">New point data to copy into the buffer</param>
    /// <remarks>
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit() to update the acceleration structure.
    /// </remarks>
    public unsafe void UpdatePoints(ReadOnlyMemory<Point> points)
    {
        var span = points.Span;
        var ptr = m_points.GetDataPointer();
        for (int i = 0; i < span.Length; i++)
        {
            ptr[i] = span[i];
        }
    }

    /// <summary>
    /// Updates the point buffer with new data from a span (zero-copy).
    /// </summary>
    /// <param name="points">New point data to copy into the buffer</param>
    /// <remarks>
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit() to update the acceleration structure.
    /// This overload avoids allocation by directly accepting a span.
    /// </remarks>
    public unsafe void UpdatePoints(ReadOnlySpan<Point> points)
    {
        var ptr = m_points.GetDataPointer();
        for (int i = 0; i < points.Length; i++)
        {
            ptr[i] = points[i];
        }
    }

    /// <summary>
    /// Gets a pointer to the point buffer data for direct in-place manipulation.
    /// </summary>
    /// <returns>Pointer to the point buffer</returns>
    /// <remarks>
    /// After modifying the data through the pointer, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit() to update the acceleration structure.
    /// Use with caution - direct pointer manipulation bypasses safety checks.
    /// </remarks>
    public unsafe Point* GetPointDataPointer() => m_points.GetDataPointer();

    /// <summary>
    /// Disposes resources used by the disc point geometry.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
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

/// <summary>
/// Oriented disc point geometry - points rendered as oriented discs with explicit normals.
/// </summary>
/// <remarks>
/// Each point is rendered as a flat circular disc oriented perpendicular to the specified normal vector.
/// Use this when you need explicit control over disc orientation (e.g., for splat rendering or oriented particles).
/// Unlike DiscPointGeometry (which billboards to the camera), these discs maintain fixed world-space orientations.
/// </remarks>
public class OrientedDiscPointGeometry : PointGeometry
{
    private readonly EmbreeBuffer<Point> m_positions;
    private readonly EmbreeBuffer<V3f> m_normals;

    /// <summary>
    /// Creates oriented disc point geometry from a collection of oriented points.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="points">Array of oriented points with position, radius, and normal</param>
    /// <param name="quality">Build quality for acceleration structures</param>
    public OrientedDiscPointGeometry(Device device, ReadOnlyMemory<OrientedPoint> points, RTCBuildQuality quality)
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

        // Point vertex buffer (FLOAT4 = x, y, z, radius)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT4, m_positions.Handle, 0, (nuint)(sizeof(float) * 4), (nuint)points.Length);

        // Normal buffer (FLOAT3)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Normal, 0, RTCFormat.FLOAT3, m_normals.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)normals.Length);

        Commit();

        device.CheckError("Create OrientedDiscPointGeometry");
    }

    /// <summary>
    /// Disposes resources used by the oriented disc point geometry.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
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
