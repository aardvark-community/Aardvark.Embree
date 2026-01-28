using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public partial class EmbreeAPI
{
    /// <summary>
    /// Creates a new BVH object.
    /// </summary>
    /// <param name="device">The device handle</param>
    /// <returns>A handle to the newly created BVH</returns>
    [DllImport("embree4")]
    public static extern IntPtr rtcNewBVH(IntPtr device);

    /// <summary>
    /// Builds a BVH using the provided build arguments.
    /// Returns the root node of the constructed BVH.
    /// </summary>
    /// <param name="args">Build arguments structure</param>
    /// <returns>Pointer to the root node of the BVH</returns>
    [DllImport("embree4")]
    public static unsafe extern IntPtr rtcBuildBVH(RTCBuildArguments* args);

    /// <summary>
    /// Allocates memory using the thread local allocator.
    /// Can only be called from BVH builder callbacks.
    /// </summary>
    /// <param name="allocator">The thread local allocator handle</param>
    /// <param name="bytes">Number of bytes to allocate</param>
    /// <param name="align">Alignment requirement</param>
    /// <returns>Pointer to allocated memory</returns>
    [DllImport("embree4")]
    public static extern IntPtr rtcThreadLocalAlloc(IntPtr allocator, UIntPtr bytes, UIntPtr align);

    /// <summary>
    /// Retains the BVH (increments reference count).
    /// </summary>
    /// <param name="bvh">The BVH handle</param>
    [DllImport("embree4")]
    public static extern void rtcRetainBVH(IntPtr bvh);

    /// <summary>
    /// Releases the BVH (decrements reference count).
    /// </summary>
    /// <param name="bvh">The BVH handle</param>
    [DllImport("embree4")]
    public static extern void rtcReleaseBVH(IntPtr bvh);
}

/// <summary>
/// Input build primitive for the BVH builder.
/// Must match native RTCBuildPrimitive layout (32-byte aligned).
/// Pack=32 ensures 32-byte alignment required by Embree's AVX2 SIMD instructions
/// for optimal BVH build performance.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 32)]
public struct RTCBuildPrimitive
{
    /// <summary>Lower bound X coordinate</summary>
    public float lower_x;
    /// <summary>Lower bound Y coordinate</summary>
    public float lower_y;
    /// <summary>Lower bound Z coordinate</summary>
    public float lower_z;
    /// <summary>Geometry ID</summary>
    public uint geomID;
    /// <summary>Upper bound X coordinate</summary>
    public float upper_x;
    /// <summary>Upper bound Y coordinate</summary>
    public float upper_y;
    /// <summary>Upper bound Z coordinate</summary>
    public float upper_z;
    /// <summary>Primitive ID</summary>
    public uint primID;
}

/// <summary>
/// Build flags for BVH construction.
/// </summary>
[Flags]
public enum RTCBuildFlags
{
    /// <summary>No special flags, use default BVH build</summary>
    None = 0,
    /// <summary>Optimize for dynamic scenes with frequent geometry updates</summary>
    Dynamic = 1 << 0
}

/// <summary>
/// Callback to create a BVH node.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate IntPtr RTCCreateNodeFunction(IntPtr allocator, uint childCount, IntPtr userPtr);

/// <summary>
/// Callback to set children pointers for a BVH node.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void RTCSetNodeChildrenFunction(IntPtr nodePtr, IntPtr* children, uint childCount, IntPtr userPtr);

/// <summary>
/// Callback to set bounding boxes for all children of a BVH node.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void RTCSetNodeBoundsFunction(IntPtr nodePtr, RTCBounds** bounds, uint childCount, IntPtr userPtr);

/// <summary>
/// Callback to create a BVH leaf node.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate IntPtr RTCCreateLeafFunction(IntPtr allocator, RTCBuildPrimitive* primitives, UIntPtr primitiveCount, IntPtr userPtr);

/// <summary>
/// Callback to split a build primitive for spatial splits.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void RTCSplitPrimitiveFunction(RTCBuildPrimitive* primitive, uint dimension, float position, RTCBounds* leftBounds, RTCBounds* rightBounds, IntPtr userPtr);

/// <summary>
/// Progress monitor callback for BVH build progress.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate bool RTCProgressMonitorFunction(IntPtr userPtr, double n);

/// <summary>
/// Arguments structure for BVH building.
/// Must match native RTCBuildArguments layout.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCBuildArguments
{
    /// <summary>Size of this structure in bytes</summary>
    public UIntPtr byteSize;

    /// <summary>Build quality setting</summary>
    public RTCBuildQuality buildQuality;
    /// <summary>Build flags</summary>
    public RTCBuildFlags buildFlags;
    /// <summary>Maximum branching factor</summary>
    public uint maxBranchingFactor;
    /// <summary>Maximum tree depth</summary>
    public uint maxDepth;
    /// <summary>SAH block size</summary>
    public uint sahBlockSize;
    /// <summary>Minimum leaf size</summary>
    public uint minLeafSize;
    /// <summary>Maximum leaf size</summary>
    public uint maxLeafSize;
    /// <summary>Traversal cost for SAH</summary>
    public float traversalCost;
    /// <summary>Intersection cost for SAH</summary>
    public float intersectionCost;

    /// <summary>BVH handle</summary>
    public IntPtr bvh;
    /// <summary>Pointer to primitives array</summary>
    public RTCBuildPrimitive* primitives;
    /// <summary>Number of primitives</summary>
    public UIntPtr primitiveCount;
    /// <summary>Capacity of primitives array</summary>
    public UIntPtr primitiveArrayCapacity;

    /// <summary>Create node callback pointer</summary>
    public IntPtr createNode;
    /// <summary>Set node children callback pointer</summary>
    public IntPtr setNodeChildren;
    /// <summary>Set node bounds callback pointer</summary>
    public IntPtr setNodeBounds;
    /// <summary>Create leaf callback pointer</summary>
    public IntPtr createLeaf;
    /// <summary>Split primitive callback pointer</summary>
    public IntPtr splitPrimitive;
    /// <summary>Build progress callback pointer</summary>
    public IntPtr buildProgress;
    /// <summary>User data pointer</summary>
    public IntPtr userPtr;

    /// <summary>
    /// Creates default BVH build arguments.
    /// </summary>
    public static RTCBuildArguments Default()
    {
        return new RTCBuildArguments
        {
            byteSize = (UIntPtr)sizeof(RTCBuildArguments),
            buildQuality = RTCBuildQuality.Medium,
            buildFlags = RTCBuildFlags.None,
            maxBranchingFactor = 2,
            maxDepth = 32,
            sahBlockSize = 1,
            minLeafSize = 1,
            maxLeafSize = 32, // RTC_BUILD_MAX_PRIMITIVES_PER_LEAF
            traversalCost = 1.0f,
            intersectionCost = 1.0f,
            bvh = IntPtr.Zero,
            primitives = null,
            primitiveCount = UIntPtr.Zero,
            primitiveArrayCapacity = UIntPtr.Zero,
            createNode = IntPtr.Zero,
            setNodeChildren = IntPtr.Zero,
            setNodeBounds = IntPtr.Zero,
            createLeaf = IntPtr.Zero,
            splitPrimitive = IntPtr.Zero,
            buildProgress = IntPtr.Zero,
            userPtr = IntPtr.Zero
        };
    }
}
