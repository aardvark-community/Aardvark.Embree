using Aardvark.Base;
using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Axis-aligned bounding box representation
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCBounds
{
    /// <summary>Lower corner of bounding box</summary>
    public V3f lower;
    private readonly float align0;
    /// <summary>Upper corner of bounding box</summary>
    public V3f upper;
    private readonly float align1;
}

/// <summary>
/// Linear axis-aligned bounding box representation
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCLinearBounds
{
    /// <summary>Bounds at time 0</summary>
    public RTCBounds bounds0;
    /// <summary>Bounds at time 1</summary>
    public RTCBounds bounds1;
}

/// <summary>Arguments passed to point query callback functions</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCPointQueryFunctionArguments
{
    /// <summary>Pointer to RTCPointQuery structure</summary>
    public IntPtr query;          // RTCPointQuery*
    /// <summary>User-provided context pointer</summary>
    public IntPtr userPtr;        // void*
    /// <summary>Primitive ID</summary>
    public uint primID;
    /// <summary>Geometry ID</summary>
    public uint geomID;
    /// <summary>Pointer to RTCPointQueryContext structure</summary>
    public IntPtr context;        // RTCPointQueryContext* (opaque for us)
    /// <summary>Scale factor when instance transform is a similarity transform</summary>
    public float similarityScale; // >0 when instance transform is similarity
}

/// <summary>Callback function for point queries</summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate bool RTCPointQueryFunction(ref RTCPointQueryFunctionArguments args);

/// <summary>Point query structure</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCPointQuery
{
    /// <summary>x, y, z coordinates (maps to V3f which is 3 consecutive floats)</summary>
    public V3f p;
    /// <summary>Query time</summary>
    public float time;
    /// <summary>Query radius</summary>
    public float radius;
}

/// <summary>SIMD point query structure for 4 queries</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCPointQuery4
{
    /// <summary>X coordinates</summary>
    public fixed float x[4];
    /// <summary>Y coordinates</summary>
    public fixed float y[4];
    /// <summary>Z coordinates</summary>
    public fixed float z[4];
    /// <summary>Query times</summary>
    public fixed float time[4];
    /// <summary>Query radii</summary>
    public fixed float radius[4];
}

/// <summary>SIMD point query structure for 8 queries</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCPointQuery8
{
    /// <summary>X coordinates</summary>
    public fixed float x[8];
    /// <summary>Y coordinates</summary>
    public fixed float y[8];
    /// <summary>Z coordinates</summary>
    public fixed float z[8];
    /// <summary>Query times</summary>
    public fixed float time[8];
    /// <summary>Query radii</summary>
    public fixed float radius[8];
}

/// <summary>SIMD point query structure for 16 queries</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCPointQuery16
{
    /// <summary>X coordinates</summary>
    public fixed float x[16];
    /// <summary>Y coordinates</summary>
    public fixed float y[16];
    /// <summary>Z coordinates</summary>
    public fixed float z[16];
    /// <summary>Query times</summary>
    public fixed float time[16];
    /// <summary>Query radii</summary>
    public fixed float radius[16];
}

/// <summary>
/// NOTE: assuming RTC_MAX_INSTANCE_LEVEL_COUNT = 1
/// Matches Embree 4.4.0 RTCPointQueryContext struct layout
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCPointQueryContext
{
    /// <summary>
    /// accumulated 4x4 column major matrices from world space to instance space.
    /// float world2inst[RTC_MAX_INSTANCE_LEVEL_COUNT][16]
    /// </summary>
    public M44f world2inst;

    /// <summary>
    /// accumulated 4x4 column major matrices from instance space to world space.
    /// float inst2world[RTC_MAX_INSTANCE_LEVEL_COUNT][16]
    /// </summary>
    public M44f inst2world;

    /// <summary>
    /// instance ids array - unsigned int instID[RTC_MAX_INSTANCE_LEVEL_COUNT]
    /// With RTC_MAX_INSTANCE_LEVEL_COUNT=1, this is instID[0]
    /// </summary>
    public uint instID;

    /// <summary>
    /// number of instances currently on the stack
    /// This field comes LAST in the native struct
    /// </summary>
    public uint instStackSize;
}



/// <summary>
/// Ray query context passed to intersect/occluded calls (Embree 4)
/// NOTE: assuming RTC_MAX_INSTANCE_LEVEL_COUNT = 1
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCRayQueryContext
{
    /// <summary>
    /// ray query flags
    /// </summary>
    public RTCRayQueryFlags flags;

    /// <summary>
    /// filter function to execute
    /// </summary>
    public IntPtr filter;

    /// <summary>
    /// The current stack of instance ids
    /// </summary>
    public uint instID;

    /// <summary>
    /// curve radius is set to this factor times distance to ray origin
    /// </summary>
    public float minWidthDistanceFactor;
}

/// <summary>
/// Intersection context (deprecated, use RTCRayQueryContext)
/// </summary>
[Obsolete("Use RTCRayQueryContext instead")]
[StructLayout(LayoutKind.Sequential)]
public struct RTCIntersectContext
{
    /// <summary>Ray query flags</summary>
    public RTCRayQueryFlags flags;
    /// <summary>Filter function pointer</summary>
    public IntPtr filter;
    /// <summary>Instance ID stack</summary>
    public uint instID;
    /// <summary>Curve radius distance factor</summary>
    public float minWidthDistanceFactor;
}

/// <summary>
/// Arguments for rtcIntersect functions (Embree 4)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCIntersectArguments
{
    /// <summary>Ray query flags</summary>
    public RTCRayQueryFlags flags;
    /// <summary>Feature mask for filtering</summary>
    public uint feature_mask;
    /// <summary>Pointer to context structure</summary>
    public IntPtr context;
    /// <summary>Filter function pointer</summary>
    public IntPtr filter;
    /// <summary>Intersect function pointer</summary>
    public IntPtr intersect;
}

/// <summary>
/// Arguments for rtcOccluded functions (Embree 4)
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCOccludedArguments
{
    /// <summary>Ray query flags</summary>
    public RTCRayQueryFlags flags;
    /// <summary>Feature mask for filtering</summary>
    public uint feature_mask;
    /// <summary>Pointer to context structure</summary>
    public IntPtr context;
    /// <summary>Filter function pointer</summary>
    public IntPtr filter;
    /// <summary>Occluded function pointer</summary>
    public IntPtr occluded;
}

/// <summary>Ray structure</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCRay // Align 16
{
    /// <summary>Ray origin</summary>
    public V3f org;
    /// <summary>Near clipping distance</summary>
    public float tnear;

    /// <summary>Ray direction</summary>
    public V3f dir;
    /// <summary>Ray time</summary>
    public float time;

    /// <summary>Far clipping distance</summary>
    public float tfar;
    /// <summary>Ray mask for filtering</summary>
    public uint mask;
    /// <summary>Ray ID</summary>
    public uint id;
    /// <summary>Ray flags</summary>
    public uint flags;
}

/// <summary>SIMD ray structure for 4 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCRay4 // Align 16
{
    /// <summary>Ray origin X coordinates</summary>
    public fixed float org_x[4];
    /// <summary>Ray origin Y coordinates</summary>
    public fixed float org_y[4];
    /// <summary>Ray origin Z coordinates</summary>
    public fixed float org_z[4];
    /// <summary>Near clipping distances</summary>
    public fixed float tnear[4];

    /// <summary>Ray direction X components</summary>
    public fixed float dir_x[4];
    /// <summary>Ray direction Y components</summary>
    public fixed float dir_y[4];
    /// <summary>Ray direction Z components</summary>
    public fixed float dir_z[4];
    /// <summary>Ray times</summary>
    public fixed float time[4];

    /// <summary>Far clipping distances</summary>
    public fixed float tfar[4];
    /// <summary>Ray masks</summary>
    public fixed uint mask[4];
    /// <summary>Ray IDs</summary>
    public fixed uint id[4];
    /// <summary>Ray flags</summary>
    public fixed uint flags[4];
}

/// <summary>SIMD ray structure for 8 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCRay8 // Align 32-byte (AVX)
{
    /// <summary>Ray origin X coordinates</summary>
    public fixed float org_x[8];
    /// <summary>Ray origin Y coordinates</summary>
    public fixed float org_y[8];
    /// <summary>Ray origin Z coordinates</summary>
    public fixed float org_z[8];
    /// <summary>Near clipping distances</summary>
    public fixed float tnear[8];

    /// <summary>Ray direction X components</summary>
    public fixed float dir_x[8];
    /// <summary>Ray direction Y components</summary>
    public fixed float dir_y[8];
    /// <summary>Ray direction Z components</summary>
    public fixed float dir_z[8];
    /// <summary>Ray times</summary>
    public fixed float time[8];

    /// <summary>Far clipping distances</summary>
    public fixed float tfar[8];
    /// <summary>Ray masks</summary>
    public fixed uint mask[8];
    /// <summary>Ray IDs</summary>
    public fixed uint id[8];
    /// <summary>Ray flags</summary>
    public fixed uint flags[8];
}

/// <summary>SIMD ray structure for 16 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCRay16 // Align 64-byte (AVX-512)
{
    /// <summary>Ray origin X coordinates</summary>
    public fixed float org_x[16];
    /// <summary>Ray origin Y coordinates</summary>
    public fixed float org_y[16];
    /// <summary>Ray origin Z coordinates</summary>
    public fixed float org_z[16];
    /// <summary>Near clipping distances</summary>
    public fixed float tnear[16];

    /// <summary>Ray direction X components</summary>
    public fixed float dir_x[16];
    /// <summary>Ray direction Y components</summary>
    public fixed float dir_y[16];
    /// <summary>Ray direction Z components</summary>
    public fixed float dir_z[16];
    /// <summary>Ray times</summary>
    public fixed float time[16];

    /// <summary>Far clipping distances</summary>
    public fixed float tfar[16];
    /// <summary>Ray masks</summary>
    public fixed uint mask[16];
    /// <summary>Ray IDs</summary>
    public fixed uint id[16];
    /// <summary>Ray flags</summary>
    public fixed uint flags[16];
}

/// <summary>Hit information structure</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCHit // Align 16
{
    /// <summary>Unnormalized geometry normal in object space</summary>
    public V3f Ng; // unnormalized geometry normal in object space
    /// <summary>Local hit coordinates</summary>
    public V2f uv; // local hit coordinates
    /// <summary>Primitive ID</summary>
    public uint primID;
    /// <summary>Geometry ID</summary>
    public uint geomID;
    /// <summary>Instance ID</summary>
    public uint instID_0; // instID[0] - RTC_MAX_INSTANCE_LEVEL_COUNT = 1
}

/// <summary>SIMD hit structure for 4 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCHit4 // Align 16
{
    /// <summary>Geometry normal X components</summary>
    public fixed float Ng_x[4];
    /// <summary>Geometry normal Y components</summary>
    public fixed float Ng_y[4];
    /// <summary>Geometry normal Z components</summary>
    public fixed float Ng_z[4];

    /// <summary>Local hit U coordinates</summary>
    public fixed float u[4];
    /// <summary>Local hit V coordinates</summary>
    public fixed float v[4];

    /// <summary>Primitive IDs</summary>
    public fixed uint primID[4];
    /// <summary>Geometry IDs</summary>
    public fixed uint geomID[4];
    /// <summary>Instance IDs (RTC_MAX_INSTANCE_LEVEL_COUNT = 1)</summary>
    public fixed uint instID[4];
    /// <summary>Instance primitive IDs for instance arrays (RTC_MAX_INSTANCE_LEVEL_COUNT = 1)</summary>
    public fixed uint instPrimID[4];
}

/// <summary>SIMD hit structure for 8 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCHit8 // Align 32-byte (AVX)
{
    /// <summary>Geometry normal X components</summary>
    public fixed float Ng_x[8];
    /// <summary>Geometry normal Y components</summary>
    public fixed float Ng_y[8];
    /// <summary>Geometry normal Z components</summary>
    public fixed float Ng_z[8];

    /// <summary>Local hit U coordinates</summary>
    public fixed float u[8];
    /// <summary>Local hit V coordinates</summary>
    public fixed float v[8];

    /// <summary>Primitive IDs</summary>
    public fixed uint primID[8];
    /// <summary>Geometry IDs</summary>
    public fixed uint geomID[8];
    /// <summary>Instance IDs (RTC_MAX_INSTANCE_LEVEL_COUNT = 1)</summary>
    public fixed uint instID[8];
    /// <summary>Instance primitive IDs for instance arrays (RTC_MAX_INSTANCE_LEVEL_COUNT = 1)</summary>
    public fixed uint instPrimID[8];
}

/// <summary>SIMD hit structure for 16 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCHit16 // Align 64-byte (AVX-512)
{
    /// <summary>Geometry normal X components</summary>
    public fixed float Ng_x[16];
    /// <summary>Geometry normal Y components</summary>
    public fixed float Ng_y[16];
    /// <summary>Geometry normal Z components</summary>
    public fixed float Ng_z[16];

    /// <summary>Local hit U coordinates</summary>
    public fixed float u[16];
    /// <summary>Local hit V coordinates</summary>
    public fixed float v[16];

    /// <summary>Primitive IDs</summary>
    public fixed uint primID[16];
    /// <summary>Geometry IDs</summary>
    public fixed uint geomID[16];
    /// <summary>Instance IDs (RTC_MAX_INSTANCE_LEVEL_COUNT = 1)</summary>
    public fixed uint instID[16];
    /// <summary>Instance primitive IDs for instance arrays (RTC_MAX_INSTANCE_LEVEL_COUNT = 1)</summary>
    public fixed uint instPrimID[16];
}

/// <summary>Combined ray and hit structure</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCRayHit // Align 16
{
    /// <summary>Ray information</summary>
    public RTCRay ray;
    /// <summary>Hit information</summary>
    public RTCHit hit;
}

/// <summary>SIMD combined ray and hit structure for 4 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCRayHit4 // Align 16
{
    /// <summary>Ray information for 4 rays</summary>
    public RTCRay4 ray;
    /// <summary>Hit information for 4 rays</summary>
    public RTCHit4 hit;
}

/// <summary>SIMD combined ray and hit structure for 8 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCRayHit8 // Align 32-byte (AVX)
{
    /// <summary>Ray information for 8 rays</summary>
    public RTCRay8 ray;
    /// <summary>Hit information for 8 rays</summary>
    public RTCHit8 hit;
}

/// <summary>SIMD combined ray and hit structure for 16 rays</summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCRayHit16 // Align 64-byte (AVX-512)
{
    /// <summary>Ray information for 16 rays</summary>
    public RTCRay16 ray;
    /// <summary>Hit information for 16 rays</summary>
    public RTCHit16 hit;
}

/// <summary>
/// Arguments for RTCFilterFunction1
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCFilterFunction1Arguments
{
    /// <summary>Validity mask pointer</summary>
    public uint* valid; // uint* -> write 0 to continue, 1 to stop (default)
    /// <summary>Geometry user pointer</summary>
    public void* geometryUserPtr; // void*
    /// <summary>Ray query context pointer</summary>
    public RTCRayQueryContext* context; // RTCRayQueryContext*
    /// <summary>Ray pointer</summary>
    public RTCRay* ray; // RTCRayN*
    /// <summary>Hit pointer</summary>
    public RTCHit* hit; // RTCHitN*
    /// <summary>Number of rays</summary>
    public uint N;
}

/// <summary>
/// Arguments for RTCFilterFunctionN
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCFilterFunctionNArguments
{
    /// <summary>Validity mask pointer</summary>
    public uint* valid; // write 0 to continue, 1 to stop (default)
    /// <summary>Geometry user pointer</summary>
    public void* geometryUserPtr;
    /// <summary>Ray query context pointer</summary>
    public RTCRayQueryContext* context;
    /// <summary>Ray pointer</summary>
    public void* ray; // RTCRayN*
    /// <summary>Hit pointer</summary>
    public void* hit; // RTCHitN*
    /// <summary>Number of rays</summary>
    public uint N;
}

/// <summary>
/// Arguments for RTCIntersectFunctionN
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCIntersectFunctionNArguments
{
    /// <summary>Validity mask pointer</summary>
    public IntPtr valid; // uint*: write 0 to continue, 1 to stop (default)
    /// <summary>Geometry user pointer</summary>
    public IntPtr geometryUserPtr; // void*
    /// <summary>Primitive ID</summary>
    public uint primID;
    /// <summary>Intersect context pointer</summary>
    public IntPtr context; // RTCIntersectContext*
    /// <summary>Ray-hit structure pointer</summary>
    public IntPtr rayhit; // RTCRayHitN*
    /// <summary>Number of rays</summary>
    public uint N;
    /// <summary>Geometry ID</summary>
    public uint geomID;
}

/// <summary>
/// Arguments for RTCOccludedFunctionN
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCOccludedFunctionNArguments
{
    /// <summary>Validity mask pointer</summary>
    public IntPtr valid; // uint*: write 0 to continue, 1 to stop (default)
    /// <summary>Geometry user pointer</summary>
    public IntPtr geometryUserPtr; // void*
    /// <summary>Primitive ID</summary>
    public uint primID;
    /// <summary>Intersect context pointer</summary>
    public IntPtr context; // RTCIntersectContext*
    /// <summary>Ray pointer</summary>
    public IntPtr ray; // RTCRayN*
    /// <summary>Number of rays</summary>
    public uint N;
    /// <summary>Geometry ID</summary>
    public uint geomID;
}

/// <summary>
/// Structure for transformation respresentation as a matrix decomposition using a quaternion
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCQuaternionDecomposition
{
    /// <summary>
    /// scale [x, y, z]
    /// </summary>
    public V3f scale;
    /// <summary>
    /// skey [xy, xz, yz]
    /// </summary>
    public V3f skew;
    /// <summary>
    /// shift [x, y, z]
    /// </summary>
    public V3f shift;
    /// <summary>
    /// quaterinion [r, i, j, k]
    /// </summary>
    public V4f quaterinion;
    /// <summary>
    /// translation [x, y, z]
    /// </summary>
    public V3f translation;
}

/// <summary>
/// Collision callback function called for overlapping primitive pairs.
/// The callback receives an array of collisions with a count.
/// </summary>
/// <param name="userPtr">User-provided context pointer</param>
/// <param name="collisions">Pointer to array of RTCCollision structs</param>
/// <param name="numCollisions">Number of collisions in the array</param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void RTCCollideFunc(IntPtr userPtr, RTCCollision* collisions, UIntPtr numCollisions);

/// <summary>
/// Collision information structure
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCCollision
{
    /// <summary>Geometry ID of first colliding primitive</summary>
    public uint geomID0;
    /// <summary>Primitive ID of first colliding primitive</summary>
    public uint primID0;
    /// <summary>Geometry ID of second colliding primitive</summary>
    public uint geomID1;
    /// <summary>Primitive ID of second colliding primitive</summary>
    public uint primID1;
}

/// <summary>
/// Displacement function callback for subdivision surfaces.
/// </summary>
/// <param name="args">Displacement function arguments</param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void RTCDisplacementFunctionN(RTCDisplacementFunctionNArguments* args);

/// <summary>
/// Arguments for displacement function callbacks.
/// Provides surface evaluation data for custom displacement mapping on subdivision surfaces.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCDisplacementFunctionNArguments
{
    /// <summary>User-provided geometry context pointer</summary>
    public void* geometryUserPtr;
    /// <summary>Geometry handle</summary>
    public IntPtr geometry;
    /// <summary>Primitive ID being displaced</summary>
    public uint primID;
    /// <summary>Time step index</summary>
    public uint timeStep;
    /// <summary>Array of u coordinates (barycentric/parametric)</summary>
    public float* u;
    /// <summary>Array of v coordinates (barycentric/parametric)</summary>
    public float* v;
    /// <summary>Array of geometric normal X components (input)</summary>
    public float* Ng_x;
    /// <summary>Array of geometric normal Y components (input)</summary>
    public float* Ng_y;
    /// <summary>Array of geometric normal Z components (input)</summary>
    public float* Ng_z;
    /// <summary>Array of displaced position X coordinates (output)</summary>
    public float* P_x;
    /// <summary>Array of displaced position Y coordinates (output)</summary>
    public float* P_y;
    /// <summary>Array of displaced position Z coordinates (output)</summary>
    public float* P_z;
    /// <summary>Number of evaluation points in arrays</summary>
    public uint N;
}
