using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public partial class EmbreeAPI
{
    /// <summary>
    /// Creates a new scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern IntPtr rtcNewScene(IntPtr device);

    /// <summary>
    /// Returns the device the scene got created in. The reference count of the device is incremented by this function.
    /// </summary>
    [DllImport("embree4")]
    public static extern IntPtr rtcGetSceneDevice(IntPtr scene);

    /// <summary>
    /// Retains the scene (increments the reference count).
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcRetainScene(IntPtr scene);

    /// <summary>
    /// Releases the scene (decrements the reference count).
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcReleaseScene(IntPtr scene);


    /// <summary>
    /// Attaches the geometry to a scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern uint rtcAttachGeometry(IntPtr scene, IntPtr geometry);

    /// <summary>
    /// Attaches the geometry to a scene using the specified geometry ID.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcAttachGeometryByID(IntPtr scene, IntPtr geometry, uint geomID);

    /// <summary>
    /// Detaches the geometry from the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcDetachGeometry(IntPtr scene, uint geomID);

    /// <summary>
    /// Gets a geometry handle from the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern IntPtr rtcGetGeometry(IntPtr scene, uint geomID);

    /// <summary>
    /// Commits the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcCommitScene(IntPtr scene);

    /// <summary>
    /// Commits the scene from multiple threads.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcJoinCommitScene(IntPtr scene);


    /// <summary>
    /// Sets the build quality of the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcSetSceneBuildQuality(IntPtr scene, RTCBuildQuality quality);

    /// <summary>
    /// Sets the scene flags.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcSetSceneFlags(IntPtr scene, RTCSceneFlags flags);

    /// <summary>
    /// Sets the scene flags.
    /// </summary>
    [DllImport("embree4")]
    public static extern RTCSceneFlags rtcGetSceneFlags(IntPtr scene);

    /// <summary>
    /// Returns the axis-aligned bounds of the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcGetSceneBounds(IntPtr scene, out RTCBounds bound);

    /// <summary>
    /// Returns the linear axis-aligned bounds of the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern void rtcGetSceneLinearBounds(IntPtr scene, out RTCLinearBounds bound);



    /// <summary>
    /// Perform a closest point query with a packet of 4 points with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern bool rtcPointQuery4(out int valid, IntPtr scene, RTCPointQuery4 query, RTCPointQueryContext context, IntPtr queryFunc, IntPtr userPtr);

    /// <summary>
    /// Perform a closest point query with a packet of 4 points with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern bool rtcPointQuery8(out int valid, IntPtr scene, RTCPointQuery8 query, RTCPointQueryContext context, IntPtr queryFunc, IntPtr userPtr);

    /// <summary>
    /// Perform a closest point query with a packet of 4 points with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static extern bool rtcPointQuery16(out int valid, IntPtr scene, RTCPointQuery16 query, RTCPointQueryContext context, IntPtr queryFunc, IntPtr userPtr);

    /// <summary>
    /// Intersects a single ray with the scene.
    /// </summary>

    [DllImport("embree4")]
    public static unsafe extern void rtcIntersect1(IntPtr scene, RTCRayHit* rayhit, RTCIntersectArguments* args);

    /// <summary>
    /// Intersects a packet of 4 rays with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static unsafe extern void rtcIntersect4(int* valid, IntPtr scene, RTCRayHit4* rayhit, RTCIntersectArguments* args);

    /// <summary>
    /// Intersects a packet of 8 rays with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static unsafe extern void rtcIntersect8(int* valid, IntPtr scene, RTCRayHit8* rayhit, RTCIntersectArguments* args);

    /// <summary>
    /// Intersects a packet of 16 rays with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static unsafe extern void rtcIntersect16(int* valid, IntPtr scene, RTCRayHit16* rayhit, RTCIntersectArguments* args);

    /// <summary>
    /// Tests a single ray for occlusion with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static unsafe extern void rtcOccluded1(IntPtr scene, RTCRay* ray, RTCOccludedArguments* args);

    /// <summary>
    /// Tests a packet of 4 rays for occlusion occluded with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static unsafe extern void rtcOccluded4(int* valid, IntPtr scene, RTCRay4* ray, RTCOccludedArguments* args);

    /// <summary>
    /// Tests a packet of 8 rays for occlusion with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static unsafe extern void rtcOccluded8(int* valid, IntPtr scene, RTCRay8* ray, RTCOccludedArguments* args);

    /// <summary>
    /// Tests a packet of 16 rays for occlusion with the scene.
    /// </summary>
    [DllImport("embree4")]
    public static unsafe extern void rtcOccluded16(int* valid, IntPtr scene, RTCRay16* ray, RTCOccludedArguments* args);

}
