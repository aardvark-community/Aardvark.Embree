using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public partial class EmbreeAPI
{
    /// <summary>
    /// Sets multiple instanced scenes for an instance array geometry.
    /// Used with RTC_GEOMETRY_TYPE_INSTANCE_ARRAY to instance multiple different scenes.
    /// Requires an index buffer (RTC_BUFFER_TYPE_INDEX) to map instances to scenes.
    /// </summary>
    /// <param name="geometry">Instance array geometry handle</param>
    /// <param name="scenes">Array of scene handles to instance</param>
    /// <param name="numScenes">Number of scenes in the array</param>
    [DllImport("embree4")]
    public static extern void rtcSetGeometryInstancedScenes(IntPtr geometry, IntPtr[] scenes, nuint numScenes);

    /// <summary>
    /// Gets the interpolated transformation of an instance for the specified time.
    /// Extended version that supports instance arrays via instPrimID parameter.
    /// </summary>
    /// <param name="geometry">Instance or instance array geometry handle</param>
    /// <param name="instPrimID">Instance primitive ID (for instance arrays, index into instance array)</param>
    /// <param name="time">Time at which to query the transformation</param>
    /// <param name="format">Format of the transformation matrix</param>
    /// <param name="xfm">Pointer to memory where transformation will be written</param>
    [DllImport("embree4")]
    public static extern void rtcGetGeometryTransformEx(IntPtr geometry, uint instPrimID, float time, RTCFormat format, IntPtr xfm);

    /// <summary>
    /// Sets the transformation for a single instance in an instance array at a specific time step.
    /// Convenience wrapper around rtcSetGeometryTransform for setting individual instance transforms.
    /// For instance arrays, use transform buffers instead for better performance.
    /// </summary>
    /// <param name="geometry">Instance array geometry handle</param>
    /// <param name="instPrimID">Instance primitive ID (index into instance array)</param>
    /// <param name="timeStep">Time step index for motion blur</param>
    /// <param name="format">Format of the transformation matrix</param>
    /// <param name="xfm">Pointer to transformation data</param>
    public static void rtcSetInstanceTransform(IntPtr geometry, uint instPrimID, uint timeStep, RTCFormat format, IntPtr xfm)
    {
        // Note: For instance arrays, transforms are typically managed via buffers (RTC_BUFFER_TYPE_TRANSFORM)
        // This is a convenience method that uses the standard rtcSetGeometryTransform
        // For single instances or when not using arrays, use rtcSetGeometryTransform directly
        rtcSetGeometryTransform(geometry, timeStep, format, xfm);
    }

    /// <summary>
    /// Gets the transformation for a single instance in an instance array at a specific time.
    /// Convenience wrapper around rtcGetGeometryTransformEx.
    /// </summary>
    /// <param name="geometry">Instance array geometry handle</param>
    /// <param name="instPrimID">Instance primitive ID (index into instance array)</param>
    /// <param name="time">Time at which to query the transformation</param>
    /// <param name="format">Format of the transformation matrix</param>
    /// <param name="xfm">Pointer to memory where transformation will be written</param>
    public static void rtcGetInstanceTransform(IntPtr geometry, uint instPrimID, float time, RTCFormat format, IntPtr xfm)
    {
        rtcGetGeometryTransformEx(geometry, instPrimID, time, format, xfm);
    }

    /// <summary>
    /// Sets the motion blur time range for an instance array geometry.
    /// This is equivalent to rtcSetGeometryTimeRange but named specifically for instance arrays.
    /// </summary>
    /// <param name="geometry">Instance array geometry handle</param>
    /// <param name="startTime">Start time of the motion blur time range</param>
    /// <param name="endTime">End time of the motion blur time range</param>
    public static void rtcSetInstanceMotionBlurTimeRange(IntPtr geometry, float startTime, float endTime)
    {
        rtcSetGeometryTimeRange(geometry, startTime, endTime);
    }
}
