using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Curve control point with position and radius.
/// </summary>
/// <remarks>
/// Used by all curve geometry types. The radius controls the curve thickness.
/// For normal-oriented curves, additional normal data may be required via separate buffers.
/// </remarks>
public struct CurveVertex
{
    /// <summary>
    /// 3D position of the control point.
    /// </summary>
    public V3f Position;

    /// <summary>
    /// Radius of the curve at this control point.
    /// </summary>
    public float Radius;

    /// <summary>
    /// Creates a curve control point.
    /// </summary>
    /// <param name="position">3D position</param>
    /// <param name="radius">Curve radius at this point</param>
    public CurveVertex(V3f position, float radius)
    {
        Position = position;
        Radius = radius;
    }
}

/// <summary>
/// Base class for curve geometries (Bezier, B-spline, Hermite, Catmull-Rom, Linear).
/// Curves can be flat (ribbon-like), round (tube-like), or normal-oriented.
/// </summary>
/// <remarks>
/// <para>
/// Embree 4 supports 15 curve types derived from 5 basis functions (Linear, Bezier, B-spline, Hermite, Catmull-Rom)
/// and 3 curve variants (Flat, Round, Normal-oriented). Linear curves also support a Cone variant.
/// </para>
/// <para>
/// Control point requirements vary by basis type:
/// - Linear: 2 points per segment
/// - Bezier: 4 points per segment (cubic)
/// - B-spline: 4 points per segment (cubic)
/// - Hermite: 4 points per segment (position + tangent pairs)
/// - Catmull-Rom: 4 points per segment (uniform parameterization)
/// </para>
/// <para>
/// Curve variants:
/// - Flat: Ribbon-like curves with constant width (faster rendering)
/// - Round: Tube-like curves with circular cross-section (highest quality)
/// - Normal-oriented: Curves oriented according to provided normals
/// - Cone (Linear only): Linear segments with discontinuous edges
/// </para>
/// <para>
/// Motion blur is supported via multiple time steps using rtcSetGeometryTimeStepCount.
/// </para>
/// </remarks>
public abstract class CurveGeometry : EmbreeGeometry
{
    /// <summary>
    /// Curve control point buffer (position + radius).
    /// </summary>
    protected readonly EmbreeBuffer<CurveVertex> m_vertices;

    /// <summary>
    /// Curve index buffer (one index per curve segment).
    /// </summary>
    protected readonly EmbreeBuffer<uint> m_indices;
    private readonly int m_vertexCount;

    /// <summary>
    /// Creates a curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="type">Curve geometry type (determines basis function and variant)</param>
    /// <param name="vertices">Curve control points (position + radius); count must match basis requirements</param>
    /// <param name="indices">Curve indices (one index per curve segment, points to first control point)</param>
    /// <param name="quality">Build quality (affects BVH construction)</param>
    /// <remarks>
    /// The vertices buffer is stored in FLOAT4 format (x, y, z, radius).
    /// The number of control points per curve depends on the basis type.
    /// </remarks>
    protected CurveGeometry(Device device, RTCGeometryType type, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, type, quality)
    {
        m_vertexCount = vertices.Length;
        m_vertices = EmbreeBuffer.Create(device, vertices);
        m_indices = EmbreeBuffer.Create(device, indices);

        // Curve index buffer
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT, m_indices.Handle, 0, (nuint)sizeof(uint), (nuint)indices.Length);
        // Curve vertex buffer (FLOAT4 = x, y, z, radius)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT4, m_vertices.Handle, 0, (nuint)(sizeof(float) * 4), (nuint)vertices.Length);

        Commit();

        device.CheckError($"Create {GetType().Name}");
    }

    /// <summary>
    /// Updates the control point buffer with new data.
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    /// <param name="controlPoints">New control point data; must match the original vertex count</param>
    /// <remarks>
    /// This copies data from managed memory to the Embree buffer.
    /// You must call UpdateBuffer(RTCBufferType.Vertex) and then Commit() for changes to take effect.
    /// </remarks>
    public unsafe void UpdateControlPoints(ReadOnlyMemory<CurveVertex> controlPoints)
    {
        ThrowIfDisposed();
        if (controlPoints.Length != m_vertexCount)
            throw new ArgumentException($"Control point count ({controlPoints.Length}) does not match original count ({m_vertexCount})", nameof(controlPoints));
        var span = controlPoints.Span;
        var ptr = m_vertices.GetDataPointer();
        for (int i = 0; i < span.Length; i++)
        {
            ptr[i] = span[i];
        }
    }

    /// <summary>
    /// Updates the control point buffer with new data from a span (zero-copy).
    /// After calling this, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    /// <param name="controlPoints">New control point data; must match the original vertex count</param>
    /// <remarks>
    /// This is the zero-copy Span API for high-performance updates.
    /// You must call UpdateBuffer(RTCBufferType.Vertex) and then Commit() for changes to take effect.
    /// </remarks>
    public unsafe void UpdateControlPoints(ReadOnlySpan<CurveVertex> controlPoints)
    {
        ThrowIfDisposed();
        if (controlPoints.Length != m_vertexCount)
            throw new ArgumentException($"Control point count ({controlPoints.Length}) does not match original count ({m_vertexCount})", nameof(controlPoints));
        var ptr = m_vertices.GetDataPointer();
        for (int i = 0; i < controlPoints.Length; i++)
        {
            ptr[i] = controlPoints[i];
        }
    }

    /// <summary>
    /// Gets a pointer to the control point buffer data for direct in-place manipulation.
    /// After modifying the data, you must call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// </summary>
    /// <returns>Unsafe pointer to the vertex buffer</returns>
    /// <remarks>
    /// Use this for maximum performance when directly manipulating control points in place.
    /// You must call UpdateBuffer(RTCBufferType.Vertex) and then Commit() after modifications.
    /// </remarks>
    public unsafe CurveVertex* GetControlPointDataPointer() => m_vertices.GetDataPointer();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_vertices.Dispose();
            m_indices.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Round (tube-like) Bezier curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic Bezier curves with circular cross-section (tube-like).
/// Each curve segment requires exactly 4 control points.
/// </para>
/// <para>
/// Basis function: Cubic Bezier (P(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃)
/// </para>
/// <para>
/// Round curves provide the highest visual quality with proper circular cross-sections,
/// but are more expensive to render than flat curves.
/// </para>
/// </remarks>
public class RoundBezierCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a round Bezier curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public RoundBezierCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.RoundBezierCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Flat (ribbon-like) Bezier curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic Bezier curves with constant width (ribbon-like).
/// Each curve segment requires exactly 4 control points.
/// </para>
/// <para>
/// Basis function: Cubic Bezier (P(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃)
/// </para>
/// <para>
/// Flat curves are faster to render than round curves and suitable for ribbon or trail effects.
/// </para>
/// </remarks>
public class FlatBezierCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a flat Bezier curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public FlatBezierCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.FlatBezierCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Normal-oriented Bezier curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic Bezier curves oriented according to provided normals.
/// Each curve segment requires exactly 4 control points plus corresponding normals.
/// </para>
/// <para>
/// Basis function: Cubic Bezier (P(t) = (1-t)³P₀ + 3(1-t)²tP₁ + 3(1-t)t²P₂ + t³P₃)
/// </para>
/// <para>
/// Normal-oriented curves allow precise control over curve orientation,
/// useful for hair, fur, or other directional curve rendering.
/// </para>
/// </remarks>
public class NormalOrientedBezierCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a normal-oriented Bezier curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    /// <remarks>
    /// Normal data must be provided via rtcSetGeometryBuffer with RTCBufferType.Normal.
    /// </remarks>
    public NormalOrientedBezierCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.NormalOrientedBezierCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Round (tube-like) B-spline curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic B-spline curves with circular cross-section (tube-like).
/// Each curve segment requires exactly 4 control points.
/// </para>
/// <para>
/// Basis function: Cubic B-spline (uniform knot vector).
/// B-splines provide C² continuity and do not interpolate control points.
/// </para>
/// <para>
/// Round curves provide the highest visual quality with proper circular cross-sections,
/// but are more expensive to render than flat curves.
/// </para>
/// </remarks>
public class RoundBSplineCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a round B-spline curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public RoundBSplineCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.RoundBsplineCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Flat (ribbon-like) B-spline curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic B-spline curves with constant width (ribbon-like).
/// Each curve segment requires exactly 4 control points.
/// </para>
/// <para>
/// Basis function: Cubic B-spline (uniform knot vector).
/// B-splines provide C² continuity and do not interpolate control points.
/// </para>
/// <para>
/// Flat curves are faster to render than round curves and suitable for ribbon or trail effects.
/// </para>
/// </remarks>
public class FlatBSplineCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a flat B-spline curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public FlatBSplineCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.FlatBsplineCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Normal-oriented B-spline curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic B-spline curves oriented according to provided normals.
/// Each curve segment requires exactly 4 control points plus corresponding normals.
/// </para>
/// <para>
/// Basis function: Cubic B-spline (uniform knot vector).
/// B-splines provide C² continuity and do not interpolate control points.
/// </para>
/// <para>
/// Normal-oriented curves allow precise control over curve orientation,
/// useful for hair, fur, or other directional curve rendering.
/// </para>
/// </remarks>
public class NormalOrientedBSplineCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a normal-oriented B-spline curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    /// <remarks>
    /// Normal data must be provided via rtcSetGeometryBuffer with RTCBufferType.Normal.
    /// </remarks>
    public NormalOrientedBSplineCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.NormalOrientedBsplineCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Round (tube-like) Hermite curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic Hermite curves with circular cross-section (tube-like).
/// Each curve segment requires exactly 4 control points arranged as two position-tangent pairs.
/// </para>
/// <para>
/// Basis function: Cubic Hermite interpolation.
/// Control points represent: P₀, T₀ (tangent at P₀), P₁, T₁ (tangent at P₁).
/// Hermite curves interpolate positions and tangents, providing precise endpoint control.
/// </para>
/// <para>
/// Round curves provide the highest visual quality with proper circular cross-sections,
/// but are more expensive to render than flat curves.
/// </para>
/// </remarks>
public class RoundHermiteCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a round Hermite curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment: P₀, T₀, P₁, T₁)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public RoundHermiteCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.RoundHermiteCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Flat (ribbon-like) Hermite curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic Hermite curves with constant width (ribbon-like).
/// Each curve segment requires exactly 4 control points arranged as two position-tangent pairs.
/// </para>
/// <para>
/// Basis function: Cubic Hermite interpolation.
/// Control points represent: P₀, T₀ (tangent at P₀), P₁, T₁ (tangent at P₁).
/// Hermite curves interpolate positions and tangents, providing precise endpoint control.
/// </para>
/// <para>
/// Flat curves are faster to render than round curves and suitable for ribbon or trail effects.
/// </para>
/// </remarks>
public class FlatHermiteCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a flat Hermite curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment: P₀, T₀, P₁, T₁)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public FlatHermiteCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.FlatHermiteCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Normal-oriented Hermite curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Cubic Hermite curves oriented according to provided normals.
/// Each curve segment requires exactly 4 control points arranged as two position-tangent pairs,
/// plus corresponding normals.
/// </para>
/// <para>
/// Basis function: Cubic Hermite interpolation.
/// Control points represent: P₀, T₀ (tangent at P₀), P₁, T₁ (tangent at P₁).
/// Hermite curves interpolate positions and tangents, providing precise endpoint control.
/// </para>
/// <para>
/// Normal-oriented curves allow precise control over curve orientation,
/// useful for hair, fur, or other directional curve rendering.
/// </para>
/// </remarks>
public class NormalOrientedHermiteCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a normal-oriented Hermite curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment: P₀, T₀, P₁, T₁)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    /// <remarks>
    /// Normal data must be provided via rtcSetGeometryBuffer with RTCBufferType.Normal.
    /// </remarks>
    public NormalOrientedHermiteCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.NormalOrientedHermiteCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Round (tube-like) Catmull-Rom curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Catmull-Rom spline curves with circular cross-section (tube-like).
/// Each curve segment requires exactly 4 control points.
/// </para>
/// <para>
/// Basis function: Catmull-Rom spline (uniform parameterization).
/// Catmull-Rom curves interpolate the middle two control points (P₁ and P₂),
/// with P₀ and P₃ controlling tangents. Provides C¹ continuity.
/// </para>
/// <para>
/// Round curves provide the highest visual quality with proper circular cross-sections,
/// but are more expensive to render than flat curves.
/// </para>
/// </remarks>
public class RoundCatmullRomCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a round Catmull-Rom curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment, interpolates middle two)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public RoundCatmullRomCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.RoundCatmullRomCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Flat (ribbon-like) Catmull-Rom curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Catmull-Rom spline curves with constant width (ribbon-like).
/// Each curve segment requires exactly 4 control points.
/// </para>
/// <para>
/// Basis function: Catmull-Rom spline (uniform parameterization).
/// Catmull-Rom curves interpolate the middle two control points (P₁ and P₂),
/// with P₀ and P₃ controlling tangents. Provides C¹ continuity.
/// </para>
/// <para>
/// Flat curves are faster to render than round curves and suitable for ribbon or trail effects.
/// </para>
/// </remarks>
public class FlatCatmullRomCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a flat Catmull-Rom curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment, interpolates middle two)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public FlatCatmullRomCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.FlatCatmullRomCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Normal-oriented Catmull-Rom curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Catmull-Rom spline curves oriented according to provided normals.
/// Each curve segment requires exactly 4 control points plus corresponding normals.
/// </para>
/// <para>
/// Basis function: Catmull-Rom spline (uniform parameterization).
/// Catmull-Rom curves interpolate the middle two control points (P₁ and P₂),
/// with P₀ and P₃ controlling tangents. Provides C¹ continuity.
/// </para>
/// <para>
/// Normal-oriented curves allow precise control over curve orientation,
/// useful for hair, fur, or other directional curve rendering.
/// </para>
/// </remarks>
public class NormalOrientedCatmullRomCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a normal-oriented Catmull-Rom curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (4 per curve segment, interpolates middle two)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    /// <remarks>
    /// Normal data must be provided via rtcSetGeometryBuffer with RTCBufferType.Normal.
    /// </remarks>
    public NormalOrientedCatmullRomCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.NormalOrientedCatmullRomCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Round (tube-like) linear curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Linear curves with circular cross-section (tube-like).
/// Each curve segment requires exactly 2 control points.
/// </para>
/// <para>
/// Basis function: Linear interpolation between endpoints.
/// Simplest curve type with C⁰ continuity (continuous but not smooth).
/// </para>
/// <para>
/// Round curves provide smooth circular cross-sections.
/// For discontinuous edges (cone-like), use ConeLinearCurveGeometry.
/// </para>
/// </remarks>
public class RoundLinearCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a round linear curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (2 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public RoundLinearCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.RoundLinearCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Flat (ribbon-like) linear curve geometry.
/// </summary>
/// <remarks>
/// <para>
/// Linear curves with constant width (ribbon-like).
/// Each curve segment requires exactly 2 control points.
/// </para>
/// <para>
/// Basis function: Linear interpolation between endpoints.
/// Simplest curve type with C⁰ continuity (continuous but not smooth).
/// </para>
/// <para>
/// Flat curves are the fastest to render and suitable for simple line effects.
/// </para>
/// </remarks>
public class FlatLinearCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a flat linear curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (2 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public FlatLinearCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.FlatLinearCurve, vertices, indices, quality)
    {
    }
}

/// <summary>
/// Cone linear curve geometry (discontinuous at edge boundaries).
/// </summary>
/// <remarks>
/// <para>
/// Linear curves shaped as truncated cones with discontinuous edges.
/// Each curve segment requires exactly 2 control points.
/// </para>
/// <para>
/// Basis function: Linear interpolation between endpoints.
/// Unlike round linear curves, cone curves have sharp discontinuities at segment boundaries,
/// forming true conical frustums between control points.
/// </para>
/// <para>
/// Use cone curves when sharp edges are desired (e.g., technical drawings, wireframes).
/// For smooth tubes, use RoundLinearCurveGeometry instead.
/// </para>
/// </remarks>
public class ConeLinearCurveGeometry : CurveGeometry
{
    /// <summary>
    /// Creates a cone linear curve geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="vertices">Control points (2 per curve segment)</param>
    /// <param name="indices">Curve indices (one per segment, points to first control point)</param>
    /// <param name="quality">Build quality</param>
    public ConeLinearCurveGeometry(Device device, ReadOnlyMemory<CurveVertex> vertices, ReadOnlyMemory<uint> indices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.ConeLinearCurve, vertices, indices, quality)
    {
    }
}
