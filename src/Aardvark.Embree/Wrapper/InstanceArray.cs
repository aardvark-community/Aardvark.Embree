using Aardvark.Base;
using System;
using System.Linq;

namespace Aardvark.Embree;

/// <summary>
/// Instance array geometry for memory-efficient representation of large numbers of instances.
/// Instance arrays allow sharing transformation buffers between the application and Embree,
/// and can optionally instance multiple different scenes using an index buffer.
/// </summary>
public class InstanceArray : EmbreeGeometry
{
    private Scene[] m_sceneObjects;  // Strong references to prevent GC during InstanceArray lifetime
    private IntPtr[] m_scenes;
    private IntPtr m_transformBuffer;
    private IntPtr m_indexBuffer;
    private uint m_instanceCount;
    private uint m_timeStepCount = 1;

    /// <summary>
    /// Number of instances in the array.
    /// </summary>
    public uint InstanceCount
    {
        get
        {
            ThrowIfDisposed();
            return m_instanceCount;
        }
    }

    /// <summary>
    /// Number of time steps for motion blur.
    /// </summary>
    public uint TimeStepCount
    {
        get
        {
            ThrowIfDisposed();
            return m_timeStepCount;
        }
    }

    /// <summary>
    /// Creates an instance array with a single instanced scene.
    /// </summary>
    /// <param name="device">Device to create the instance array on</param>
    /// <param name="scene">Scene to instance</param>
    /// <param name="instanceCount">Number of instances</param>
    /// <param name="quality">Build quality for the instance array</param>
    public InstanceArray(Device device, Scene scene, uint instanceCount, RTCBuildQuality quality = RTCBuildQuality.High)
        : base(device, RTCGeometryType.InstanceArray)
    {
        if (instanceCount == 0)
            throw new ArgumentException("Instance count must be greater than zero", nameof(instanceCount));

        m_instanceCount = instanceCount;
        m_sceneObjects = new[] { scene };  // Keep strong reference to prevent GC
        m_scenes = new[] { scene.Handle };

        // Set the instanced scene
        EmbreeAPI.rtcSetGeometryInstancedScene(Handle, scene.Handle);
        device.CheckError("InstanceArray.rtcSetGeometryInstancedScene");

        // Note: rtcSetGeometryPrimitiveCount does not exist in Embree 4.
        // The instance count is determined by the transform buffer size when committed.

        // Set build quality
        EmbreeAPI.rtcSetGeometryBuildQuality(Handle, quality);

        // Set single time step (no motion blur by default)
        EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, 1);
    }

    /// <summary>
    /// Creates an instance array with multiple instanced scenes.
    /// Requires setting an index buffer to map instances to scenes.
    /// </summary>
    /// <param name="device">Device to create the instance array on</param>
    /// <param name="scenes">Array of scenes to instance</param>
    /// <param name="instanceCount">Number of instances</param>
    /// <param name="quality">Build quality for the instance array</param>
    public InstanceArray(Device device, Scene[] scenes, uint instanceCount, RTCBuildQuality quality = RTCBuildQuality.High)
        : base(device, RTCGeometryType.InstanceArray)
    {
        if (scenes == null || scenes.Length == 0)
            throw new ArgumentException("Scenes array must not be null or empty", nameof(scenes));

        if (instanceCount == 0)
            throw new ArgumentException("Instance count must be greater than zero", nameof(instanceCount));

        m_instanceCount = instanceCount;
        m_sceneObjects = scenes.ToArray();  // Keep strong references to prevent GC
        m_scenes = scenes.Select(s => s.Handle).ToArray();

        // Set multiple instanced scenes
        EmbreeAPI.rtcSetGeometryInstancedScenes(Handle, m_scenes, (nuint)m_scenes.Length);
        device.CheckError("InstanceArray.rtcSetGeometryInstancedScenes");

        // Note: rtcSetGeometryPrimitiveCount does not exist in Embree 4.
        // The instance count is determined by the transform buffer size when committed.

        // Set build quality
        EmbreeAPI.rtcSetGeometryBuildQuality(Handle, quality);

        // Set single time step (no motion blur by default)
        EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, 1);
    }

    /// <summary>
    /// Sets the index buffer for mapping instances to scenes (required when using multiple scenes).
    /// Each index specifies which scene from the scenes array is used for that instance.
    /// </summary>
    /// <param name="indices">Array of scene indices, one per instance</param>
    public void SetIndexBuffer(uint[] indices)
    {
        ThrowIfDisposed();
        if (indices == null)
            throw new ArgumentNullException(nameof(indices));

        if (indices.Length != m_instanceCount)
            throw new ArgumentException($"Index buffer size ({indices.Length}) must match instance count ({m_instanceCount})", nameof(indices));

        // Create or update index buffer
        m_indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            Handle,
            RTCBufferType.Index,
            0, // slot
            RTCFormat.UINT,
            sizeof(uint),
            (nuint)indices.Length
        );

        m_device.CheckError("InstanceArray.rtcSetNewGeometryBuffer(Index)");

        if (m_indexBuffer == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create index buffer - rtcSetNewGeometryBuffer returned null");

        // Copy indices to buffer
        unsafe
        {
            var bufferPtr = (uint*)m_indexBuffer;
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] >= m_scenes.Length)
                    throw new ArgumentException($"Index {i} has value {indices[i]} which exceeds scene count {m_scenes.Length}", nameof(indices));

                bufferPtr[i] = indices[i];
            }
        }
    }

    /// <summary>
    /// Sets the index buffer for mapping instances to scenes using Span for zero-copy access.
    /// </summary>
    /// <param name="indices">Span of scene indices, one per instance</param>
    public void SetIndexBuffer(Span<uint> indices)
    {
        ThrowIfDisposed();
        if (indices.Length != m_instanceCount)
            throw new ArgumentException($"Index buffer size ({indices.Length}) must match instance count ({m_instanceCount})", nameof(indices));

        // Create or update index buffer
        m_indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            Handle,
            RTCBufferType.Index,
            0, // slot
            RTCFormat.UINT,
            sizeof(uint),
            (nuint)indices.Length
        );

        m_device.CheckError("InstanceArray.rtcSetNewGeometryBuffer(Index)");

        if (m_indexBuffer == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create index buffer (Span) - rtcSetNewGeometryBuffer returned null");

        // Copy indices to buffer
        unsafe
        {
            var bufferPtr = (uint*)m_indexBuffer;
            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] >= m_scenes.Length)
                    throw new ArgumentException($"Index {i} has value {indices[i]} which exceeds scene count {m_scenes.Length}", nameof(indices));

                bufferPtr[i] = indices[i];
            }
        }
    }

    /// <summary>
    /// Sets the transformation buffer for all instances.
    /// Uses 3x4 row-major affine transformations (most common format).
    /// </summary>
    /// <param name="transforms">Array of transformations, one per instance</param>
    /// <param name="timeStep">Time step index (0 for static, 0-N for motion blur)</param>
    public void SetTransformBuffer(Affine3f[] transforms, uint timeStep = 0)
    {
        ThrowIfDisposed();
        if (transforms == null)
            throw new ArgumentNullException(nameof(transforms));

        if (transforms.Length != m_instanceCount)
            throw new ArgumentException($"Transform buffer size ({transforms.Length}) must match instance count ({m_instanceCount})", nameof(transforms));
        if (timeStep >= m_timeStepCount)
            throw new ArgumentOutOfRangeException(nameof(timeStep), $"Time step {timeStep} exceeds time step count {m_timeStepCount}. Call SetupMotionBlur first.");

        // Create or update transform buffer for this time step
        m_transformBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            Handle,
            RTCBufferType.Transform,
            timeStep, // slot = time step
            RTCFormat.FLOAT3X4_ROW_MAJOR,
            (nuint)(12 * sizeof(float)), // 3x4 matrix = 12 floats
            (nuint)transforms.Length
        );

        m_device.CheckError($"InstanceArray.rtcSetNewGeometryBuffer(Transform, timeStep={timeStep})");

        if (m_transformBuffer == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create transform buffer - rtcSetNewGeometryBuffer returned null");

        // Copy transforms to buffer
        unsafe
        {
            var bufferPtr = (float*)m_transformBuffer;
            for (int i = 0; i < transforms.Length; i++)
            {
                var mtx = (M34f)transforms[i];
                var srcPtr = (float*)&mtx;
                var dstPtr = bufferPtr + (i * 12);

                // Copy 12 floats (3x4 matrix)
                for (int j = 0; j < 12; j++)
                {
                    dstPtr[j] = srcPtr[j];
                }
            }
        }
    }

    /// <summary>
    /// Sets the transformation buffer for all instances using Span for zero-copy access.
    /// </summary>
    /// <param name="transforms">Span of transformations, one per instance</param>
    /// <param name="timeStep">Time step index (0 for static, 0-N for motion blur)</param>
    public void SetTransformBuffer(Span<Affine3f> transforms, uint timeStep = 0)
    {
        ThrowIfDisposed();
        if (transforms.Length != m_instanceCount)
            throw new ArgumentException($"Transform buffer size ({transforms.Length}) must match instance count ({m_instanceCount})", nameof(transforms));
        if (timeStep >= m_timeStepCount)
            throw new ArgumentOutOfRangeException(nameof(timeStep), $"Time step {timeStep} exceeds time step count {m_timeStepCount}. Call SetupMotionBlur first.");

        // Create or update transform buffer for this time step
        m_transformBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            Handle,
            RTCBufferType.Transform,
            timeStep, // slot = time step
            RTCFormat.FLOAT3X4_ROW_MAJOR,
            (nuint)(12 * sizeof(float)), // 3x4 matrix = 12 floats
            (nuint)transforms.Length
        );

        m_device.CheckError($"InstanceArray.rtcSetNewGeometryBuffer(Transform, timeStep={timeStep})");

        if (m_transformBuffer == IntPtr.Zero)
            throw new InvalidOperationException("Failed to create transform buffer (Span) - rtcSetNewGeometryBuffer returned null");

        // Copy transforms to buffer
        unsafe
        {
            var bufferPtr = (float*)m_transformBuffer;
            for (int i = 0; i < transforms.Length; i++)
            {
                var mtx = (M34f)transforms[i];
                var srcPtr = (float*)&mtx;
                var dstPtr = bufferPtr + (i * 12);

                // Copy 12 floats (3x4 matrix)
                for (int j = 0; j < 12; j++)
                {
                    dstPtr[j] = srcPtr[j];
                }
            }
        }
    }

    /// <summary>
    /// Sets up motion blur by specifying the number of time steps and optionally the time range.
    /// After calling this, use SetTransformBuffer for each time step.
    /// </summary>
    /// <param name="timeStepCount">Number of time steps (must be >= 2 for motion blur)</param>
    /// <param name="startTime">Start time of motion blur range (default 0.0)</param>
    /// <param name="endTime">End time of motion blur range (default 1.0)</param>
    public void SetupMotionBlur(uint timeStepCount, float startTime = 0.0f, float endTime = 1.0f)
    {
        ThrowIfDisposed();
        if (timeStepCount < 2)
            throw new ArgumentException("Motion blur requires at least 2 time steps", nameof(timeStepCount));

        m_timeStepCount = timeStepCount;

        // Set time step count
        EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, timeStepCount);
        m_device.CheckError("InstanceArray.rtcSetGeometryTimeStepCount");

        // Set time range
        EmbreeAPI.rtcSetInstanceMotionBlurTimeRange(Handle, startTime, endTime);
        m_device.CheckError("InstanceArray.rtcSetInstanceMotionBlurTimeRange");
    }

    /// <summary>
    /// Gets the interpolated transformation for a specific instance at a given time.
    /// </summary>
    /// <param name="instanceIndex">Index of the instance</param>
    /// <param name="time">Time at which to query the transformation</param>
    /// <returns>Interpolated transformation</returns>
    public Affine3f GetInstanceTransform(uint instanceIndex, float time = 0.0f)
    {
        ThrowIfDisposed();
        if (instanceIndex >= m_instanceCount)
            throw new ArgumentOutOfRangeException(nameof(instanceIndex), $"Instance index {instanceIndex} exceeds instance count {m_instanceCount}");

        unsafe
        {
            M34f mtx = default;
            EmbreeAPI.rtcGetInstanceTransform(Handle, instanceIndex, time, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)(&mtx));
            m_device.CheckError($"InstanceArray.rtcGetInstanceTransform(instance={instanceIndex}, time={time})");
            // M34f is row-major: [R00 R01 R02 T0], [R10 R11 R12 T1], [R20 R21 R22 T2]
            var rot = new M33f(mtx.M00, mtx.M01, mtx.M02,
                               mtx.M10, mtx.M11, mtx.M12,
                               mtx.M20, mtx.M21, mtx.M22);
            var trans = new V3f(mtx.M03, mtx.M13, mtx.M23);
            return new Affine3f(rot, trans);
        }
    }

    /// <summary>
    /// Sets the transformation for a specific instance at a specific time step.
    /// </summary>
    /// <remarks>
    /// WARNING: rtcSetInstanceTransform is NOT supported for InstanceArray geometry in Embree 4.
    /// This method only works for single Instance geometry (RTCGeometryType.Instance).
    /// For InstanceArray, use SetTransformBuffer to set all transforms at once.
    /// Calling this method on InstanceArray will generate Embree warnings and may cause
    /// crashes during cleanup on some platforms (particularly Linux).
    /// </remarks>
    /// <param name="instanceIndex">Index of the instance</param>
    /// <param name="transform">Transformation to set</param>
    /// <param name="timeStep">Time step index</param>
    [Obsolete("rtcSetInstanceTransform is not supported for InstanceArray in Embree 4. Use SetTransformBuffer instead.")]
    public void SetInstanceTransform(uint instanceIndex, Affine3f transform, uint timeStep = 0)
    {
        ThrowIfDisposed();
        if (instanceIndex >= m_instanceCount)
            throw new ArgumentOutOfRangeException(nameof(instanceIndex), $"Instance index {instanceIndex} exceeds instance count {m_instanceCount}");

        if (timeStep >= m_timeStepCount)
            throw new ArgumentOutOfRangeException(nameof(timeStep), $"Time step {timeStep} exceeds time step count {m_timeStepCount}");

        // Note: This modifies the transform buffer directly
        // If no buffer has been created yet, this will fail - user must call SetTransformBuffer first
        unsafe
        {
            var mtx = (M34f)transform;
            EmbreeAPI.rtcSetInstanceTransform(Handle, instanceIndex, timeStep, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)(&mtx));
            m_device.CheckError($"InstanceArray.rtcSetInstanceTransform(instance={instanceIndex}, timeStep={timeStep})");
        }
    }

    /// <summary>
    /// Sets a shared transformation buffer from user-allocated memory.
    /// Allows zero-copy sharing of transformation data between application and Embree.
    /// </summary>
    /// <param name="transformPtr">Pointer to transformation data</param>
    /// <param name="timeStep">Time step index</param>
    public unsafe void SetSharedTransformBuffer(float* transformPtr, uint timeStep = 0)
    {
        ThrowIfDisposed();
        if (transformPtr == null)
            throw new ArgumentNullException(nameof(transformPtr));
        if (timeStep >= m_timeStepCount)
            throw new ArgumentOutOfRangeException(nameof(timeStep), $"Time step {timeStep} exceeds time step count {m_timeStepCount}. Call SetupMotionBlur first.");

        EmbreeAPI.rtcSetSharedGeometryBuffer(
            Handle,
            RTCBufferType.Transform,
            timeStep, // slot = time step
            RTCFormat.FLOAT3X4_ROW_MAJOR,
            (IntPtr)transformPtr,
            0, // byteOffset
            (nuint)(12 * sizeof(float)), // byteStride: 3x4 matrix = 12 floats
            (nuint)m_instanceCount
        );

        m_device.CheckError($"InstanceArray.rtcSetSharedGeometryBuffer(Transform, timeStep={timeStep})");
    }

    /// <summary>
    /// Sets ray mask for intersection filtering.
    /// Only rays with matching mask bits will intersect this geometry.
    /// </summary>
    /// <param name="mask">Ray mask (default is 0xFFFFFFFF = all bits set)</param>
    public void SetMask(uint mask)
    {
        ThrowIfDisposed();
        EmbreeAPI.rtcSetGeometryMask(Handle, mask);
        m_device.CheckError("InstanceArray.rtcSetGeometryMask");
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Buffers are managed by Embree and released when geometry is released
            m_transformBuffer = IntPtr.Zero;
            m_indexBuffer = IntPtr.Zero;
            m_scenes = null;
            m_sceneObjects = null;  // Clear strong references (we don't own the scenes)
        }
        base.Dispose(disposing);
    }
}
