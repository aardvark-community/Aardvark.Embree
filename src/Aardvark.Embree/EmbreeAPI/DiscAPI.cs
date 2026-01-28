using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

#pragma warning disable CS1587
public partial class EmbreeAPI
{
    /// <summary>
    /// Creates a new disc geometry (points rendered as oriented discs).
    /// Disc geometries represent points as flat circular discs oriented perpendicular to the view direction.
    /// </summary>
    /// <remarks>
    /// Note: rtcNewGeometry with RTCGeometryType.DiscPoint already exists in Geometry.cs
    /// This file documents disc-specific functionality.
    /// </remarks>

    /// <summary>
    /// Sets the disc geometry buffer (vertex buffer with position and radius).
    /// Buffer format should be FLOAT4 (x, y, z, radius).
    /// </summary>
    /// <remarks>
    /// Uses rtcSetGeometryBuffer from Geometry.cs with RTCBufferType.Vertex and RTCFormat.FLOAT4
    /// </remarks>

    /// <summary>
    /// Creates a new sphere geometry (points rendered as spheres).
    /// Sphere geometries represent points as 3D spheres.
    /// </summary>
    /// <remarks>
    /// Note: rtcNewGeometry with RTCGeometryType.SpherePoint already exists in Geometry.cs
    /// This file documents sphere-specific functionality.
    /// </remarks>

    /// <summary>
    /// Sets the sphere geometry buffer (vertex buffer with position and radius).
    /// Buffer format should be FLOAT4 (x, y, z, radius).
    /// </summary>
    /// <remarks>
    /// Uses rtcSetGeometryBuffer from Geometry.cs with RTCBufferType.Vertex and RTCFormat.FLOAT4
    /// </remarks>

    /// <summary>
    /// Creates a new oriented disc geometry (points rendered as explicitly oriented discs).
    /// Oriented disc geometries represent points as flat circular discs with explicit normal directions.
    /// </summary>
    /// <remarks>
    /// Note: rtcNewGeometry with RTCGeometryType.OrientedDiscPoint already exists in Geometry.cs
    /// This file documents oriented disc-specific functionality.
    /// </remarks>

    /// <summary>
    /// Sets the oriented disc geometry buffer (vertex buffer with position and radius).
    /// Buffer format should be FLOAT4 (x, y, z, radius).
    /// Normal buffer must be set separately using RTCBufferType.Normal with RTCFormat.FLOAT3.
    /// </summary>
    /// <remarks>
    /// Uses rtcSetGeometryBuffer from Geometry.cs with:
    /// - RTCBufferType.Vertex and RTCFormat.FLOAT4 for positions/radii
    /// - RTCBufferType.Normal and RTCFormat.FLOAT3 for normals
    /// </remarks>

    /// <summary>
    /// Sets the ray mask for disc geometry.
    /// The mask is used to filter rays based on their mask value.
    /// </summary>
    /// <remarks>
    /// Uses rtcSetGeometryMask from Geometry.cs
    /// </remarks>

    /// <summary>
    /// Sets the ray mask for oriented disc geometry.
    /// The mask is used to filter rays based on their mask value.
    /// </summary>
    /// <remarks>
    /// Uses rtcSetGeometryMask from Geometry.cs
    /// </remarks>
}
#pragma warning restore CS1587
