using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree
{
    public partial class EmbreeAPI
    {
        /// <summary>
        /// Creates a new geometry of specified type.
        /// </summary>
        [DllImport("embree3")]
        public static extern IntPtr rtcNewGeometry(IntPtr device, RTCGeometryType type);

        /// <summary>
        /// Retains the geometry (increments the reference count). 
        /// </summary>
        [DllImport("embree3")]
        public static extern void rtcRetainGeometry(IntPtr geometry);

        /// <summary>
        /// Releases the geometry (decrements the reference count) 
        /// </summary>
        [DllImport("embree3")]
        public static extern void rtcReleaseGeometry(IntPtr geometry);

        /// <summary>
        /// Commits the geometry.
        /// </summary>
        [DllImport("embree3")]
        public static extern void rtcCommitGeometry(IntPtr geometry);


        /// <summary>
        /// Enables the geometry.
        /// </summary>
        [DllImport("embree3")]
        public static extern void rtcEnableGeometry(IntPtr geometry);

        /// <summary>
        /// Disables the geometry. 
        /// </summary>
        [DllImport("embree3")]
        public static extern void rtcDisableGeometry(IntPtr geometry);


        /// <summary>
		/// Sets the number of motion blur time steps of the geometry. 
		/// </summary>
        [DllImport("embree3")]
        public static extern void rtcSetGeometryTimeStepCount(IntPtr geometry, uint timeStepCount);

        /// <summary>
		/// Sets the motion blur time range of the geometry. 
		/// </summary>
        [DllImport("embree3")]
        public static extern void rtcSetGeometryTimeRange(IntPtr geometry, float startTime, float endTime);

        /// <summary>
		/// Sets the number of vertex attributes of the geometry. 
		/// </summary>
        [DllImport("embree3")]
        public static extern void rtcSetGeometryVertexAttributeCount(IntPtr geometry, uint vertexAttributeCount);

        /// <summary>
		/// Sets the ray mask of the geometry. 
		/// </summary>
        [DllImport("embree3")]
        public static extern void rtcSetGeometryMask(IntPtr geometry, uint mask);

        /// <summary>
		/// Sets the build quality of the geometry. 
		/// </summary>
        [DllImport("embree3")]
        public static extern void rtcSetGeometryBuildQuality(IntPtr geometry, RTCBuildQuality quality);

        /// <summary>
		/// Sets the maximal curve or point radius scale allowed by min-width feature. 
		/// </summary>
        [DllImport("embree3")]
        public static extern void rtcSetGeometryMaxRadiusScale(IntPtr geometry, float maxRadiusScale);


        /// <summary>
		/// Sets a geometry buffer. 
		/// </summary>
        [DllImport("embree3")]
        public static extern void rtcSetGeometryBuffer(IntPtr geometry, RTCBufferType type, uint slot, RTCFormat format, IntPtr buffer, ulong byteOffset, ulong byteStride, ulong itemCount); // TODO: nuint instead of ulong

        /// <summary>
		/// Sets a shared geometry buffer. 
		/// </summary>
        [DllImport("embree3")]
        public static extern void rtcSetSharedGeometryBuffer(IntPtr geometry, RTCBufferType type, uint slot, RTCFormat format, IntPtr ptr, ulong byteOffset, ulong byteStride, ulong itemCount); // TODO: nuint instead of ulong

        /// <summary>
		/// Creates and sets a new geometry buffer. 
		/// </summary>
        [DllImport("embree3")] 
        public static extern IntPtr rtcSetNewGeometryBuffer(IntPtr geometry, RTCBufferType type, uint slot, RTCFormat format, ulong byteStride, ulong itemCount); // TODO: nuint instead of ulong

        /// <summary>
		/// Returns the pointer to the data of a buffer. 
		/// </summary>
        [DllImport("embree3")]
        public static extern IntPtr rtcGetGeometryBufferData(IntPtr geometry, RTCBufferType type, uint slot);

        /// <summary>
		/// Updates a geometry buffer. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcUpdateGeometryBuffer(IntPtr geometry, RTCBufferType type, uint slot);


        /// <summary>
		/// Sets the intersection filter callback function of the geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryIntersectFilterFunction(IntPtr geometry, IntPtr filter);

        /// <summary>
		/// Sets the occlusion filter callback function of the geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryOccludedFilterFunction(IntPtr geometry, IntPtr filter);

        /// <summary>
		/// Sets the user-defined data pointer of the geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryUserData(IntPtr geometry, IntPtr ptr);

        /// <summary>
		/// Gets the user-defined data pointer of the geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern IntPtr rtcGetGeometryUserData(IntPtr geometry);

        /// <summary>
		/// Set the point query callback function of a geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryPointQueryFunction(IntPtr geometry, IntPtr pointQuery);

        /// <summary>
		/// Sets the number of primitives of a user geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryUserPrimitiveCount(IntPtr geometry, uint userPrimitiveCount);

        /// <summary>
		/// Sets the bounding callback function to calculate bounding boxes for user primitives. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryBoundsFunction(IntPtr geometry, IntPtr bounds, IntPtr userPtr);

        /// <summary>
		/// Set the intersect callback function of a user geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryIntersectFunction(IntPtr geometry, IntPtr intersect);

        /// <summary>
		/// Set the occlusion callback function of a user geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryOccludedFunction(IntPtr geometry, IntPtr occluded);

        /// <summary>
		/// Invokes the intersection filter from the intersection callback function. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcFilterIntersection(RTCIntersectFunctionNArguments args, RTCFilterFunctionNArguments filterArgs);

        /// <summary>
		/// Invokes the occlusion filter from the occlusion callback function. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcFilterOcclusion(RTCOccludedFunctionNArguments args, RTCFilterFunctionNArguments filterArgs);


        /// <summary>
		/// Sets the instanced scene of an instance geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryInstancedScene(IntPtr geometry, IntPtr scene);

        /// <summary>
		/// Sets the transformation of an instance for the specified time step. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryTransform(IntPtr geometry, uint timeStep, RTCFormat format, IntPtr xfm);

        /// <summary>
		/// Sets the transformation quaternion of an instance for the specified time step. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryTransformQuaternion(IntPtr geometry, uint timeStep, RTCQuaternionDecomposition qd);

        /// <summary>
		/// Returns the interpolated transformation of an instance for the specified time. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcGetGeometryTransform(IntPtr geometry, float time, RTCFormat format, IntPtr xfm);


        /// <summary>
		/// Sets the uniform tessellation rate of the geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryTessellationRate(IntPtr geometry, float tessellationRate);

        /// <summary>
		/// Sets the number of topologies of a subdivision surface. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryTopologyCount(IntPtr geometry, uint topologyCount);

        /// <summary>
		/// Sets the subdivision interpolation mode. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometrySubdivisionMode(IntPtr geometry, uint topologyID, RTCSubdivisionMode mode);

        /// <summary>
		/// Binds a vertex attribute to a topology of the geometry. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryVertexAttributeTopology(IntPtr geometry, uint vertexAttributeID, uint topologyID);

        /// <summary>
		/// Sets the displacement callback function of a subdivision surface. 
		/// </summary>
        [DllImport("embree3")]
		public static extern void rtcSetGeometryDisplacementFunction(IntPtr geometry, IntPtr displacement);

        /// <summary>
		/// Returns the first half edge of a face. 
		/// </summary>
        [DllImport("embree3")]
		public static extern uint rtcGetGeometryFirstHalfEdge(IntPtr geometry, uint faceID);

        /// <summary>
		/// Returns the face the half edge belongs to. 
		/// </summary>
        [DllImport("embree3")]
		public static extern uint rtcGetGeometryFace(IntPtr geometry, uint edgeID);

        /// <summary>
		/// Returns next half edge. 
		/// </summary>
        [DllImport("embree3")]
		public static extern uint rtcGetGeometryNextHalfEdge(IntPtr geometry, uint edgeID);

        /// <summary>
		/// Returns previous half edge. 
		/// </summary>
        [DllImport("embree3")]
		public static extern uint rtcGetGeometryPreviousHalfEdge(IntPtr geometry, uint edgeID);

        /// <summary>
		/// Returns opposite half edge. 
		/// </summary>
        [DllImport("embree3")]
		public static extern uint rtcGetGeometryOppositeHalfEdge(IntPtr geometry, uint topologyID, uint edgeID);
    }
}
