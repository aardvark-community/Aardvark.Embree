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
    public V3f lower;
    private readonly float align0;
    public V3f upper;
    private readonly float align1;
}

/// <summary>
/// Linear axis-aligned bounding box representation
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct RTCLinearBounds
{
    public RTCBounds bounds0;
    public RTCBounds bounds1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RTCPointQueryFunctionArguments
{
    public IntPtr query;          // RTCPointQuery*
    public IntPtr userPtr;        // void*
    public uint primID;
    public uint geomID;
    public IntPtr context;        // RTCPointQueryContext* (opaque for us)
    public float similarityScale; // >0 when instance transform is similarity
}

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal unsafe delegate bool RTCPointQueryFunction(ref RTCPointQueryFunctionArguments args);

[StructLayout(LayoutKind.Sequential)]
public struct RTCPointQuery
{
    public V3f p;
    public float time;
    public float radius;
}

[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCPointQuery4
{
    public fixed float x[4];
    public fixed float y[4];
    public fixed float z[4];
    public fixed float time[4];
    public fixed float radius[4];
}

public unsafe struct RTCPointQuery8
{
    public fixed float x[8];
    public fixed float y[8];
    public fixed float z[8];
    public fixed float time[8];
    public fixed float radius[8];
}

public unsafe struct RTCPointQuery16
{
    public fixed float x[16];
    public fixed float y[16];
    public fixed float z[16];
    public fixed float time[16];
    public fixed float radius[16];
}

/// <summary>
/// NOTE: assuming RTC_MAX_INSTANCE_LEVEL_COUNT = 1
/// </summary>
public struct RTCPointQueryContext
{
    /// <summary>
    /// accumulated 4x4 column major matrices from world space to instance space.
    /// undefined if size == 0.
    /// </summary>
    public M44f world2inst;

    /// <summary>
    /// accumulated 4x4 column major matrices from instance space to world space.
    /// undefined if size == 0.
    /// </summary>
    public M44f inst2world;

    /// <summary>
    /// instance ids. 
    /// </summary>
    public uint instID;

    /// <summary>
    /// number of instances currently on the stack. 
    /// </summary>
    public uint instStackSize;
}



/// <summary>
/// Intersection context passed to intersect/occluded calls
/// NOTE: assuming RTC_MAX_INSTANCE_LEVEL_COUNT = 1
/// </summary>
public struct RTCIntersectContext
{
    /// <summary>
    /// intersection flags
    /// </summary>
    public RTCIntersectContextFlags flags;

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

public struct RTCRay // Align 16
{
    public V3f org;
    public float tnear;

    public V3f dir;
    public float time;

    public float tfar;
    public uint mask;
    public uint id;
    public uint flags;
}

public unsafe struct RTCRay4 // Align 16
{
    public fixed float org_x[4];
    public fixed float org_y[4];
    public fixed float org_z[4];
    public fixed float tnear[4];

    public fixed float dir_x[4];
    public fixed float dir_y[4];
    public fixed float dir_z[4];
    public fixed float time[4];

    public fixed float tfar[4];
    public fixed uint mask[4];
    public fixed uint id[4];
    public fixed uint flags[4];
}

public unsafe struct RTCRay8 // Align 16
{
    public fixed float org_x[8];
    public fixed float org_y[8];
    public fixed float org_z[8];
    public fixed float tnear[8];

    public fixed float dir_x[8];
    public fixed float dir_y[8];
    public fixed float dir_z[8];
    public fixed float time[8];

    public fixed float tfar[8];
    public fixed uint mask[8];
    public fixed uint id[8];
    public fixed uint flags[8];
}

public unsafe struct RTCRay16 // Align 16
{
    public fixed float org_x[16];
    public fixed float org_y[16];
    public fixed float org_z[16];
    public fixed float tnear[16];

    public fixed float dir_x[16];
    public fixed float dir_y[16];
    public fixed float dir_z[16];
    public fixed float time[16];

    public fixed float tfar[16];
    public fixed uint mask[16];
    public fixed uint id[16];
    public fixed uint flags[16];
}

public struct RTCHit // Align 16
{
    public V3f Ng; // unnormalized geometry normal in object space
    public V2f uv; // local hit coordinates
    public uint primID;
    public uint geomID;
    public uint instID;
}

public unsafe struct RTCHit4 // Align 16
{
    public fixed float Ng_x[4];
    public fixed float Ng_y[4];
    public fixed float Ng_z[4];

    public fixed float u[4];
    public fixed float v[4];

    public fixed uint primID[4];
    public fixed uint geomID[4];
    public fixed uint instID[4];
}

public unsafe struct RTCHit8 // Align 16
{
    public fixed float Ng_x[8];
    public fixed float Ng_y[8];
    public fixed float Ng_z[8];

    public fixed float u[8];
    public fixed float v[8];

    public fixed uint primID[8];
    public fixed uint geomID[8];
    public fixed uint instID[8];
}

public unsafe struct RTCHit16 // Align 16
{
    public fixed float Ng_x[16];
    public fixed float Ng_y[16];
    public fixed float Ng_z[16];

    public fixed float u[16];
    public fixed float v[16];

    public fixed uint primID[16];
    public fixed uint geomID[16];
    public fixed uint instID[16];
}

public struct RTCRayHit // Align 16
{
    public RTCRay ray;
    public RTCHit hit;
}

public struct RTCRayHit4 // Align 16
{
    public RTCRay4 ray;
    public RTCHit4 hit;
}

public struct RTCRayHit8 // Align 16
{
    public RTCRay8 ray;
    public RTCHit8 hit;
}

public struct RTCRayHit16 // Align 16
{
    public RTCRay16 ray;
    public RTCHit16 hit;
}

/// <summary>
/// Arguments for RTCFilterFunction1 
/// </summary>
public unsafe struct RTCFilterFunction1Arguments
{
    public uint* valid; // uint* -> write 0 to continue, 1 to stop (default)
    public void* geometryUserPtr; // void*
    public RTCIntersectContext* context; // RTCIntersectContext*
    public RTCRay* ray; // RTCRayN*
    public RTCHit* hit; // RTCHitN*
    public uint N;
}

/// <summary>
/// Arguments for RTCFilterFunctionN
/// </summary>
public unsafe struct RTCFilterFunctionNArguments
{
    public uint* valid; // write 0 to continue, 1 to stop (default)
    public void* geometryUserPtr;
    public RTCIntersectContext* context;
    public void* ray; // RTCRayN*
    public void* hit; // RTCHitN*
    public uint N;
}

/// <summary>
/// Arguments for RTCIntersectFunctionN
/// </summary>
public struct RTCIntersectFunctionNArguments
{
    public IntPtr valid; // uint*: write 0 to continue, 1 to stop (default)
    public IntPtr geometryUserPtr; // void*
    public uint primID;
    public IntPtr context; // RTCIntersectContext*
    public IntPtr rayhit; // RTCRayHitN*
    public uint N;
    public uint geomID;
}

/// <summary>
/// Arguments for RTCOccludedFunctionN
/// </summary>
public struct RTCOccludedFunctionNArguments
{
    public IntPtr valid; // uint*: write 0 to continue, 1 to stop (default)
    public IntPtr geometryUserPtr; // void*
    public uint primID;
    public IntPtr context; // RTCIntersectContext*
    public IntPtr ray; // RTCRayN*
    public uint N;
    public uint geomID;
}

/// <summary>
/// Structure for transformation respresentation as a matrix decomposition using a quaternion
/// </summary>
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
