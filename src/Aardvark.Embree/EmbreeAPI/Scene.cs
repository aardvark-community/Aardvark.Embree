using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree
{
    public partial class EmbreeAPI
    {
        /// <summary>
        /// Creates a new scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern IntPtr rtcNewScene(IntPtr device);

        /// <summary>
        /// Returns the device the scene got created in. The reference count of the device is incremented by this function.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern IntPtr rtcGetSceneDevice(IntPtr scene);

        /// <summary>
        /// Retains the scene (increments the reference count).
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcRetainScene(IntPtr scene);

        /// <summary>
        /// Releases the scene (decrements the reference count).
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcReleaseScene(IntPtr scene);


        /// <summary>
        /// Attaches the geometry to a scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern uint rtcAttachGeometry(IntPtr scene, IntPtr geometry);

        /// <summary>
        /// Attaches the geometry to a scene using the specified geometry ID.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcAttachGeometryByID(IntPtr scene, IntPtr geometry, uint geomID);

        /// <summary>
        /// Detaches the geometry from the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcDetachGeometry(IntPtr scene, uint geomID);

        /// <summary>
        /// Gets a geometry handle from the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern IntPtr rtcGetGeometry(IntPtr scene, uint geomID);


        /// <summary>
        /// Commits the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcCommitScene(IntPtr scene);

        /// <summary>
        /// Commits the scene from multiple threads.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcJoinCommitScene(IntPtr scene);


        /// <summary>
        /// Sets the build quality of the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcSetSceneBuildQuality(IntPtr scene, RTCBuildQuality quality);

        /// <summary>
        /// Sets the scene flags.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcSetSceneFlags(IntPtr scene, RTCSceneFlags flags);

        /// <summary>
        /// Sets the scene flags.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern RTCSceneFlags rtcGetSceneFlags(IntPtr scene);

        /// <summary>
        /// Returns the axis-aligned bounds of the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcGetSceneBounds(IntPtr scene, out RTCBounds bound);

        /// <summary>
        /// Returns the linear axis-aligned bounds of the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern void rtcGetSceneLinearBounds(IntPtr scene, out RTCLinearBounds bound);


        /// <summary>
        /// Perform a closest point query of the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern bool rtcPointQuery(IntPtr scene, RTCPointQuery query, RTCPointQueryContext context, IntPtr queryFunc, IntPtr userPtr);

        /// <summary>
        /// Perform a closest point query with a packet of 4 points with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern bool rtcPointQuery4(out int valid, IntPtr scene, RTCPointQuery4 query, RTCPointQueryContext context, IntPtr queryFunc, IntPtr userPtr);

        /// <summary>
        /// Perform a closest point query with a packet of 4 points with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern bool rtcPointQuery8(out int valid, IntPtr scene, RTCPointQuery8 query, RTCPointQueryContext context, IntPtr queryFunc, IntPtr userPtr);

        /// <summary>
        /// Perform a closest point query with a packet of 4 points with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static extern bool rtcPointQuery16(out int valid, IntPtr scene, RTCPointQuery16 query, RTCPointQueryContext context, IntPtr queryFunc, IntPtr userPtr);

        /// <summary>
        /// Intersects a single ray with the scene.
        /// </summary>

        [DllImport("embree3.dll")]
        public static unsafe extern void rtcIntersect1(IntPtr scene, RTCIntersectContext* context, RTCRayHit* rayhit);

        /// <summary>
        /// Intersects a packet of 4 rays with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static unsafe extern void rtcIntersect4(out int valid, IntPtr scene, RTCIntersectContext* context, RTCRayHit4* rayhit);

        /// <summary>
        /// Intersects a packet of 8 rays with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static unsafe extern void rtcIntersect8(out int valid, IntPtr scene, RTCIntersectContext* context, RTCRayHit8* rayhit);

        /// <summary>
        /// Intersects a packet of 16 rays with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static unsafe extern void rtcIntersect16(out int valid, IntPtr scene, RTCIntersectContext* context, RTCRayHit16* rayhit);

        /// <summary>
        /// Tests a single ray for occlusion with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static unsafe extern void rtcOccluded1(IntPtr scene, RTCIntersectContext* context, RTCRay* ray);

        /// <summary>
        /// Tests a packet of 4 rays for occlusion occluded with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static unsafe extern void rtcOccluded4(out int valid, IntPtr scene, RTCIntersectContext* context, RTCRay4* ray);

        /// <summary>
        /// Tests a packet of 8 rays for occlusion with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static unsafe extern void rtcOccluded8(out int valid, IntPtr scene, RTCIntersectContext* context, RTCRay8* ray);

        /// <summary>
        /// Tests a packet of 16 rays for occlusion with the scene.
        /// </summary>
        [DllImport("embree3.dll")]
        public static unsafe extern void rtcOccluded16(out int valid, IntPtr scene, RTCIntersectContext* context, RTCRay16* ray);
    }
}
