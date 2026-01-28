using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Arguments for rtcInterpolate function (Embree 4)
/// Interpolates vertex data to some u/v location and optionally calculates derivatives.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCInterpolateArguments
{
    /// <summary>
    /// Geometry handle to interpolate on
    /// </summary>
    public IntPtr geometry;

    /// <summary>
    /// Primitive ID (from hit)
    /// </summary>
    public uint primID;

    /// <summary>
    /// Barycentric u coordinate
    /// </summary>
    public float u;

    /// <summary>
    /// Barycentric v coordinate
    /// </summary>
    public float v;

    /// <summary>
    /// Type of buffer to interpolate (typically RTC_BUFFER_TYPE_VERTEX_ATTRIBUTE)
    /// </summary>
    public RTCBufferType bufferType;

    /// <summary>
    /// Buffer slot index (0, 1, 2, ... for different attribute buffers)
    /// </summary>
    public uint bufferSlot;

    /// <summary>
    /// Pointer to output buffer for interpolated values
    /// </summary>
    public IntPtr P;

    /// <summary>
    /// Pointer to output buffer for first order derivative in u direction (set to IntPtr.Zero if not needed)
    /// </summary>
    public IntPtr dPdu;

    /// <summary>
    /// Pointer to output buffer for first order derivative in v direction (set to IntPtr.Zero if not needed)
    /// </summary>
    public IntPtr dPdv;

    /// <summary>
    /// Pointer to output buffer for second order derivative d²P/du² (set to IntPtr.Zero if not needed)
    /// </summary>
    public IntPtr ddPdudu;

    /// <summary>
    /// Pointer to output buffer for second order derivative d²P/dv² (set to IntPtr.Zero if not needed)
    /// </summary>
    public IntPtr ddPdvdv;

    /// <summary>
    /// Pointer to output buffer for second order derivative d²P/dudv (set to IntPtr.Zero if not needed)
    /// </summary>
    public IntPtr ddPdudv;

    /// <summary>
    /// Number of float values to interpolate
    /// </summary>
    public uint valueCount;
}
