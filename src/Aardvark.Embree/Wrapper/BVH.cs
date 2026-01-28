using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Wrapper for Embree's BVH (Bounding Volume Hierarchy) builder.
/// Allows custom BVH construction with user-defined node structures.
/// </summary>
public class BVH : IDisposable
{
    private IntPtr m_handle;
    private bool m_disposed = false;
    private readonly object m_disposeLock = new object();

    /// <summary>
    /// Gets the native Embree BVH handle.
    /// </summary>
    public IntPtr Handle
    {
        get
        {
            if (m_disposed)
                throw new ObjectDisposedException(nameof(BVH));
            return m_handle;
        }
        private set => m_handle = value;
    }

    /// <summary>
    /// Gets the Embree device this BVH was created on.
    /// </summary>
    public Device Device { get; }

    /// <summary>
    /// Creates a new BVH object.
    /// </summary>
    /// <param name="device">The Embree device</param>
    public BVH(Device device)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        m_handle = EmbreeAPI.rtcNewBVH(device.Handle);
        if (m_handle == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create BVH");
    }

    /// <summary>
    /// Builds a BVH using the provided build arguments.
    /// Returns the root node pointer of the constructed BVH.
    /// </summary>
    /// <param name="args">Build arguments with primitives and callbacks</param>
    /// <returns>Pointer to the root node of the BVH</returns>
    public unsafe IntPtr Build(RTCBuildArguments* args)
    {
        if (m_disposed)
            throw new ObjectDisposedException(nameof(BVH));

        if (args == null)
            throw new ArgumentNullException(nameof(args));

        return EmbreeAPI.rtcBuildBVH(args);
    }

    /// <summary>
    /// Builds a BVH using the provided build arguments.
    /// Returns the root node pointer of the constructed BVH.
    /// </summary>
    /// <param name="args">Build arguments with primitives and callbacks</param>
    /// <returns>Pointer to the root node of the BVH</returns>
    public unsafe IntPtr Build(ref RTCBuildArguments args)
    {
        if (m_disposed)
            throw new ObjectDisposedException(nameof(BVH));

        fixed (RTCBuildArguments* pArgs = &args)
        {
            return EmbreeAPI.rtcBuildBVH(pArgs);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc/>
    protected virtual void Dispose(bool disposing)
    {
        lock (m_disposeLock)
        {
            if (!m_disposed)
            {
                if (m_handle != IntPtr.Zero)
                {
                    EmbreeAPI.rtcReleaseBVH(m_handle);
                    m_handle = IntPtr.Zero;
                }
                m_disposed = true;
            }
        }
    }
}

/// <summary>
/// Helper class for building custom BVH structures.
/// Provides convenience methods for common BVH building scenarios.
/// </summary>
public static class BVHBuilder
{
    /// <summary>
    /// Allocates memory using the thread local allocator.
    /// Can only be called from within BVH builder callbacks.
    /// </summary>
    /// <param name="allocator">The thread local allocator handle</param>
    /// <param name="bytes">Number of bytes to allocate</param>
    /// <param name="align">Alignment requirement (default: 16)</param>
    /// <returns>Pointer to allocated memory</returns>
    public static IntPtr AllocateMemory(IntPtr allocator, ulong bytes, ulong align = 16)
    {
        return EmbreeAPI.rtcThreadLocalAlloc(allocator, (UIntPtr)bytes, (UIntPtr)align);
    }

    /// <summary>
    /// Creates default build arguments for a BVH.
    /// </summary>
    /// <param name="bvh">The BVH handle</param>
    /// <param name="primitives">Pointer to the primitive array</param>
    /// <param name="primitiveCount">Number of primitives</param>
    /// <returns>Initialized build arguments</returns>
    public static unsafe RTCBuildArguments CreateDefaultArguments(
        IntPtr bvh,
        RTCBuildPrimitive* primitives,
        ulong primitiveCount)
    {
        var args = RTCBuildArguments.Default();
        args.bvh = bvh;
        args.primitives = primitives;
        args.primitiveCount = (UIntPtr)primitiveCount;
        args.primitiveArrayCapacity = (UIntPtr)primitiveCount;
        return args;
    }

    /// <summary>
    /// Creates a build primitive from bounds and IDs.
    /// </summary>
    public static RTCBuildPrimitive CreatePrimitive(
        float minX, float minY, float minZ,
        float maxX, float maxY, float maxZ,
        uint geomID, uint primID)
    {
        return new RTCBuildPrimitive
        {
            lower_x = minX,
            lower_y = minY,
            lower_z = minZ,
            geomID = geomID,
            upper_x = maxX,
            upper_y = maxY,
            upper_z = maxZ,
            primID = primID
        };
    }
}
