using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public partial class EmbreeAPI
{
    /// <summary>
    /// Performs a closest point query on a scene.
    /// </summary>
    /// <param name="scene">The scene handle</param>
    /// <param name="query">Point query structure</param>
    /// <param name="context">Query context with instance stack</param>
    /// <param name="queryFunc">Callback function invoked for each potential closest primitive</param>
    /// <param name="userPtr">User data pointer passed to callback</param>
    /// <returns>True if the query was successful</returns>
    [DllImport("embree4")]
    public static unsafe extern bool rtcPointQuery(
        IntPtr scene,
        RTCPointQuery* query,
        RTCPointQueryContext* context,
        IntPtr queryFunc,
        IntPtr userPtr);

    /// <summary>
    /// Performs a closest point query with a packet of 4 points.
    /// </summary>
    /// <param name="valid">Array of 4 integers indicating which points are valid (non-zero = valid)</param>
    /// <param name="scene">The scene handle</param>
    /// <param name="query">Point query structure for 4 points</param>
    /// <param name="context">Query context with instance stack</param>
    /// <param name="queryFunc">Callback function invoked for each potential closest primitive</param>
    /// <param name="userPtr">Array of 4 user data pointers passed to callbacks</param>
    /// <returns>True if the query was successful</returns>
    [DllImport("embree4")]
    public static unsafe extern bool rtcPointQuery4(
        int* valid,
        IntPtr scene,
        RTCPointQuery4* query,
        RTCPointQueryContext* context,
        IntPtr queryFunc,
        IntPtr* userPtr);

    /// <summary>
    /// Performs a closest point query with a packet of 8 points.
    /// </summary>
    /// <param name="valid">Array of 8 integers indicating which points are valid (non-zero = valid)</param>
    /// <param name="scene">The scene handle</param>
    /// <param name="query">Point query structure for 8 points</param>
    /// <param name="context">Query context with instance stack</param>
    /// <param name="queryFunc">Callback function invoked for each potential closest primitive</param>
    /// <param name="userPtr">Array of 8 user data pointers passed to callbacks</param>
    /// <returns>True if the query was successful</returns>
    [DllImport("embree4")]
    public static unsafe extern bool rtcPointQuery8(
        int* valid,
        IntPtr scene,
        RTCPointQuery8* query,
        RTCPointQueryContext* context,
        IntPtr queryFunc,
        IntPtr* userPtr);

    /// <summary>
    /// Performs a closest point query with a packet of 16 points.
    /// </summary>
    /// <param name="valid">Array of 16 integers indicating which points are valid (non-zero = valid)</param>
    /// <param name="scene">The scene handle</param>
    /// <param name="query">Point query structure for 16 points</param>
    /// <param name="context">Query context with instance stack</param>
    /// <param name="queryFunc">Callback function invoked for each potential closest primitive</param>
    /// <param name="userPtr">Array of 16 user data pointers passed to callbacks</param>
    /// <returns>True if the query was successful</returns>
    [DllImport("embree4")]
    public static unsafe extern bool rtcPointQuery16(
        int* valid,
        IntPtr scene,
        RTCPointQuery16* query,
        RTCPointQueryContext* context,
        IntPtr queryFunc,
        IntPtr* userPtr);

    /// <summary>
    /// Performs collision detection between two scenes.
    /// Invokes the callback for all overlapping primitive pairs.
    /// </summary>
    /// <param name="scene0">First scene handle</param>
    /// <param name="scene1">Second scene handle</param>
    /// <param name="callback">Callback function receiving collision results</param>
    /// <param name="userPtr">User data pointer passed to callback</param>
    [DllImport("embree4")]
    public static extern void rtcCollide(
        IntPtr scene0,
        IntPtr scene1,
        IntPtr callback,
        IntPtr userPtr);

    /// <summary>
    /// Sets a custom collision callback function on a geometry.
    /// This allows per-geometry collision filtering or custom collision behavior.
    /// Note: This function may not be available in all Embree builds.
    /// </summary>
    /// <param name="geometry">The geometry handle</param>
    /// <param name="callback">The collision callback function</param>
    [DllImport("embree4")]
    public static extern void rtcSetGeometryCollideCallback(IntPtr geometry, IntPtr callback);
}
