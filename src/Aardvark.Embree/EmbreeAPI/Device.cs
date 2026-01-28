using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public partial class EmbreeAPI
{
    /// <summary>
    /// Creates a new Embree device.
    /// </summary>
    // CharSet.Ansi explicitly specified because this function takes a string parameter
    // (config). Other P/Invokes in this project don't need CharSet because they only
    // take IntPtr/numeric types. Embree expects UTF-8/ANSI strings.
    [DllImport("embree4", CharSet=CharSet.Ansi)]
    public static extern IntPtr rtcNewDevice(string config = null);

    /// <summary>
    /// Retains the Embree device (increments the reference count).
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcRetainDevice(IntPtr device);

    /// <summary>
    /// Releases an Embree device (decrements the reference count).
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcReleaseDevice(IntPtr device);

    /// <summary>
    /// Returns the error code.
    /// </summary>
    [DllImport("embree4")]
    public static extern RTCDeviceError rtcGetDeviceError(IntPtr device);

    /// <summary>
    /// Returns additional information about the last error (Embree 4.3.3+).
    /// This can be used when device creation failed and rtcErrorFunction could not be set up.
    /// </summary>
    [DllImport("embree4", CharSet=CharSet.Ansi)]
    public static extern IntPtr rtcGetDeviceLastErrorMessage(IntPtr device);

    /// <summary>
    /// Returns a string representation of an error code (Embree 4.3.3+).
    /// Useful for convenient error information reporting.
    /// </summary>
    [DllImport("embree4", CharSet=CharSet.Ansi)]
    public static extern IntPtr rtcGetErrorString(RTCDeviceError error);

    /// <summary>
    /// Gets a device property.
    /// </summary>
    [DllImport("embree4")]
    public static extern nint rtcGetDeviceProperty(IntPtr device, RTCDeviceProperty prop);

    /// <summary>
    /// Sets a device property.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcSetDeviceProperty(IntPtr device, RTCDeviceProperty prop, nint value);

    /// <summary>
    /// Sets a memory monitor callback function for tracking memory allocations.
    /// </summary>
    /// <param name="device">The device handle.</param>
    /// <param name="memoryMonitor">The callback function to invoke on memory operations.</param>
    /// <param name="userPtr">User pointer passed to the callback.</param>
    /// <remarks>
    /// The callback is invoked before (post=false) and after (post=true) memory operations.
    /// Returning false from the callback can abort the allocation.
    /// </remarks>
    [DllImport("embree4")]
    public static extern void rtcSetDeviceMemoryMonitorFunction(IntPtr device, RTCMemoryMonitorFunction memoryMonitor, IntPtr userPtr);

    /// <summary>
    /// Sets an error callback function for the device.
    /// </summary>
    /// <param name="device">The device handle.</param>
    /// <param name="error">The callback function to invoke on errors.</param>
    /// <param name="userPtr">User pointer passed to the callback.</param>
    /// <remarks>
    /// The callback is invoked whenever an error occurs. Allows custom error handling instead of default behavior.
    /// </remarks>
    [DllImport("embree4")]
    public static extern void rtcSetDeviceErrorFunction(IntPtr device, RTCErrorFunction error, IntPtr userPtr);
}

/// <summary>
/// Memory monitor callback function signature.
/// </summary>
/// <param name="ptr">User pointer passed to rtcSetDeviceMemoryMonitorFunction.</param>
/// <param name="bytes">Number of bytes being allocated (positive) or freed (negative).</param>
/// <param name="post">True if called after the operation, false if called before.</param>
/// <returns>Return false to abort the allocation (only effective when post=false).</returns>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool RTCMemoryMonitorFunction(IntPtr ptr, nint bytes, bool post);

/// <summary>
/// Error callback function signature.
/// </summary>
/// <param name="userPtr">User pointer passed to rtcSetDeviceErrorFunction.</param>
/// <param name="code">The error code.</param>
/// <param name="message">Error message string.</param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void RTCErrorFunction(IntPtr userPtr, RTCDeviceError code, [MarshalAs(UnmanagedType.LPStr)] string message);
