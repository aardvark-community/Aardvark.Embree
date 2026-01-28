using Aardvark.Base;
using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Bounds callback for user-defined geometry primitives.
/// </summary>
/// <param name="args">Bounds function arguments</param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void RTCBoundsFunction(RTCBoundsFunctionArguments* args);

/// <summary>
/// Intersect callback for user-defined geometry.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void RTCIntersectFunction(RTCIntersectFunctionNArguments* args);

/// <summary>
/// Occlusion callback for user-defined geometry.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void RTCOccludedFunction(RTCOccludedFunctionNArguments* args);

/// <summary>
/// Arguments for bounds callback.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct RTCBoundsFunctionArguments
{
    /// <summary>User data pointer</summary>
    public IntPtr geometryUserPtr; // void*
    /// <summary>Primitive ID</summary>
    public uint primID;
    /// <summary>Time step index</summary>
    public uint timeStep;
    /// <summary>Output bounds pointer</summary>
    public RTCBounds* bounds_o; // output bounds
}

/// <summary>
/// User-defined geometry with custom intersection and bounds callbacks.
/// Allows implementing custom primitive types (e.g., spheres, implicit surfaces, procedural geometry).
/// </summary>
public class UserGeometry : EmbreeGeometry
{
    private readonly RTCBoundsFunction m_boundsFunc;
    private readonly RTCIntersectFunction m_intersectFunc;
    private readonly RTCOccludedFunction m_occludedFunc;
    private readonly GCHandle m_userDataHandle;

    /// <summary>
    /// Creates user-defined geometry.
    /// </summary>
    /// <param name="device">Embree device</param>
    /// <param name="primitiveCount">Number of user primitives</param>
    /// <param name="boundsFunc">Callback to compute bounding boxes for primitives</param>
    /// <param name="intersectFunc">Callback to perform ray-primitive intersection</param>
    /// <param name="occludedFunc">Optional callback for occlusion tests (can be null)</param>
    /// <param name="userData">Optional user data passed to callbacks</param>
    /// <param name="quality">Build quality</param>
    public UserGeometry(Device device, uint primitiveCount,
                        RTCBoundsFunction boundsFunc, RTCIntersectFunction intersectFunc,
                        RTCOccludedFunction occludedFunc = null, object userData = null,
                        RTCBuildQuality quality = RTCBuildQuality.Medium)
        : base(device, RTCGeometryType.User, quality)
    {
        if (boundsFunc == null) throw new ArgumentNullException(nameof(boundsFunc));
        if (intersectFunc == null) throw new ArgumentNullException(nameof(intersectFunc));

        m_boundsFunc = boundsFunc;
        m_intersectFunc = intersectFunc;
        m_occludedFunc = occludedFunc;

        // Set primitive count
        EmbreeAPI.rtcSetGeometryUserPrimitiveCount(Handle, primitiveCount);

        // Set callbacks
        var boundsFuncPtr = Marshal.GetFunctionPointerForDelegate(boundsFunc);
        EmbreeAPI.rtcSetGeometryBoundsFunction(Handle, boundsFuncPtr, IntPtr.Zero);

        var intersectFuncPtr = Marshal.GetFunctionPointerForDelegate(intersectFunc);
        EmbreeAPI.rtcSetGeometryIntersectFunction(Handle, intersectFuncPtr);

        if (occludedFunc != null)
        {
            var occludedFuncPtr = Marshal.GetFunctionPointerForDelegate(occludedFunc);
            EmbreeAPI.rtcSetGeometryOccludedFunction(Handle, occludedFuncPtr);
        }

        // Set user data
        if (userData != null)
        {
            m_userDataHandle = GCHandle.Alloc(userData, GCHandleType.Normal);
            EmbreeAPI.rtcSetGeometryUserData(Handle, GCHandle.ToIntPtr(m_userDataHandle));
        }

        Commit();

        device.CheckError("Create UserGeometry");
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (m_userDataHandle.IsAllocated)
                m_userDataHandle.Free();
        }
        base.Dispose(disposing);
    }
}
