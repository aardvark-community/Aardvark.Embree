using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public partial class EmbreeAPI
{
    /// <summary>
    /// Sets the transformation matrices for two time steps of an instance geometry.
    /// This is used for motion blur interpolation between two transformation states.
    /// </summary>
    /// <param name="geometry">The instance geometry handle</param>
    /// <param name="format">The format of the transformation matrices (e.g., RTC_FORMAT_FLOAT3X4_ROW_MAJOR)</param>
    /// <param name="xfm0">Pointer to the first transformation matrix (time step 0)</param>
    /// <param name="xfm1">Pointer to the second transformation matrix (time step 1)</param>
    [DllImport("embree4")]
    public static extern void rtcSetGeometryTransform2(IntPtr geometry, RTCFormat format, IntPtr xfm0, IntPtr xfm1);

    /// <summary>
    /// Convenience method: Sets two transformation matrices for motion blur.
    /// </summary>
    /// <param name="geometry">The instance geometry handle</param>
    /// <param name="format">The format of the transformation matrices</param>
    /// <param name="xfm0">The first transformation matrix (12 or 16 floats depending on format)</param>
    /// <param name="xfm1">The second transformation matrix (12 or 16 floats depending on format)</param>
    public static unsafe void rtcSetGeometryTransform2(IntPtr geometry, RTCFormat format, float[] xfm0, float[] xfm1)
    {
        if (xfm0 == null) throw new ArgumentNullException(nameof(xfm0));
        if (xfm1 == null) throw new ArgumentNullException(nameof(xfm1));

        // Validate array sizes based on format
        int expectedSize = format switch
        {
            RTCFormat.FLOAT3X4_ROW_MAJOR => 12,
            RTCFormat.FLOAT3X4_COLUMN_MAJOR => 12,
            RTCFormat.FLOAT4X4_COLUMN_MAJOR => 16,
            _ => throw new ArgumentException($"Unsupported transformation format: {format}", nameof(format))
        };

        if (xfm0.Length != expectedSize)
            throw new ArgumentException($"Transform array xfm0 must have {expectedSize} elements for format {format}", nameof(xfm0));
        if (xfm1.Length != expectedSize)
            throw new ArgumentException($"Transform array xfm1 must have {expectedSize} elements for format {format}", nameof(xfm1));

        fixed (float* pXfm0 = xfm0)
        fixed (float* pXfm1 = xfm1)
        {
            rtcSetGeometryTransform2(geometry, format, (IntPtr)pXfm0, (IntPtr)pXfm1);
        }
    }

    /// <summary>
    /// Convenience method: Sets two transformation matrices for motion blur using row-major 3x4 matrices.
    /// </summary>
    /// <param name="geometry">The instance geometry handle</param>
    /// <param name="xfm0">The first 3x4 row-major transformation matrix (12 floats)</param>
    /// <param name="xfm1">The second 3x4 row-major transformation matrix (12 floats)</param>
    public static void rtcSetGeometryTransform2RowMajor(IntPtr geometry, float[] xfm0, float[] xfm1)
    {
        rtcSetGeometryTransform2(geometry, RTCFormat.FLOAT3X4_ROW_MAJOR, xfm0, xfm1);
    }

    /// <summary>
    /// Convenience method: Sets two transformation matrices for motion blur using column-major 3x4 matrices.
    /// </summary>
    /// <param name="geometry">The instance geometry handle</param>
    /// <param name="xfm0">The first 3x4 column-major transformation matrix (12 floats)</param>
    /// <param name="xfm1">The second 3x4 column-major transformation matrix (12 floats)</param>
    public static void rtcSetGeometryTransform2ColumnMajor(IntPtr geometry, float[] xfm0, float[] xfm1)
    {
        rtcSetGeometryTransform2(geometry, RTCFormat.FLOAT3X4_COLUMN_MAJOR, xfm0, xfm1);
    }

    // Note: The following methods are already implemented in Geometry.cs but are motion blur related:
    // - rtcSetGeometryTimeStepCount: Sets the number of motion blur time steps
    // - rtcSetGeometryTimeRange: Sets the motion blur time range
    // - rtcSetGeometryVertexAttributeTopology: Binds vertex attributes to topology (used in motion blur)
    // - rtcInterpolate: Interpolates vertex data (with motion blur support)
    // - rtcInterpolateN: Interpolates N vertex values (with motion blur support)
    // - rtcSetGeometryTransform: Sets transformation for specific time step (already in Geometry.cs)
    // - rtcSetGeometryTransformQuaternion: Sets quaternion transformation for time step (already in Geometry.cs)
    // - rtcGetGeometryTransform: Gets interpolated transformation at specific time (already in Geometry.cs)
}