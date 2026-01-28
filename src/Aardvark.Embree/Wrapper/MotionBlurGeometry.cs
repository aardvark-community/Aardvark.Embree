using Aardvark.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aardvark.Embree;

/// <summary>
/// Triangle geometry with motion blur support (time-varying vertices).
/// </summary>
/// <remarks>
/// Motion blur simulates moving objects during camera exposure by interpolating vertex positions over time.
/// Supports linear motion blur (2 time steps: start and end) and multi-segment motion blur (N time steps for complex motion paths).
/// Each time step provides vertex positions at a specific point in time within the [0,1] time range.
/// Use for rendering moving objects with realistic motion blur effects (e.g., fast-moving vehicles, spinning objects).
/// Linear blur (2 steps) is sufficient for constant velocity; multi-segment blur (3+ steps) handles acceleration/rotation.
/// </remarks>
public class MotionBlurGeometry : EmbreeGeometry
{
    private readonly List<EmbreeBuffer<V3f>> m_vertexBuffers;
    private readonly EmbreeBuffer<int> m_indices;
    private readonly int m_timeSteps;

    /// <summary>
    /// Creates linear motion blur geometry with 2 time steps (t=0 and t=1).
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="verticesT0">Vertex positions at time t=0 (shutter open)</param>
    /// <param name="verticesT1">Vertex positions at time t=1 (shutter close)</param>
    /// <param name="triangleIndices">Triangle indices (length = number of triangles * 3)</param>
    /// <param name="quality">Build quality for acceleration structures</param>
    /// <remarks>
    /// Linear motion blur interpolates linearly between two vertex position sets.
    /// Use this for objects moving at constant velocity during camera exposure.
    /// For accelerating/rotating objects, use the multi-segment constructor instead.
    /// </remarks>
    public MotionBlurGeometry(Device device, ReadOnlyMemory<V3f> verticesT0, ReadOnlyMemory<V3f> verticesT1, ReadOnlyMemory<int> triangleIndices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.Triangle, quality)
    {
        if (verticesT0.Length != verticesT1.Length)
            throw new ArgumentException("Vertex counts must match for all time steps");

        m_timeSteps = 2;
        m_vertexBuffers = new List<EmbreeBuffer<V3f>>();

        // Set time step count before setting buffers
        EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, (uint)m_timeSteps);
        device.CheckError("MotionBlurGeometry.rtcSetGeometryTimeStepCount");

        // Create and set vertex buffers for each time step
        m_vertexBuffers.Add(EmbreeBuffer.Create(device, verticesT0));
        m_vertexBuffers.Add(EmbreeBuffer.Create(device, verticesT1));

        for (uint i = 0; i < m_timeSteps; i++)
        {
            EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, i, RTCFormat.FLOAT3, m_vertexBuffers[(int)i].Handle, 0, (nuint)(sizeof(float) * 3), (nuint)verticesT0.Length);
            device.CheckError($"MotionBlurGeometry.rtcSetGeometryBuffer(Vertex, slot={i})");
        }

        // Create and set index buffer (shared across all time steps)
        m_indices = EmbreeBuffer.Create(device, triangleIndices);
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT3, m_indices.Handle, 0, (nuint)(sizeof(int) * 3), (nuint)(triangleIndices.Length / 3));
        device.CheckError("MotionBlurGeometry.rtcSetGeometryBuffer(Index)");

        // Set vertex attribute buffers for interpolation (only slot 0)
        EmbreeAPI.rtcSetGeometryVertexAttributeCount(Handle, 1);
        device.CheckError("MotionBlurGeometry.rtcSetGeometryVertexAttributeCount");

        // For vertex attributes with motion blur, we only set slot 0
        // The motion blur interpolation is handled through the vertex buffers
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.VertexAttribute, 0, RTCFormat.FLOAT3, m_vertexBuffers[0].Handle, 0, (nuint)(sizeof(float) * 3), (nuint)verticesT0.Length);
        device.CheckError("MotionBlurGeometry.rtcSetGeometryBuffer(VertexAttribute, slot=0)");

        // Enable geometry before commit (required for interpolation)
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("MotionBlurGeometry.rtcEnableGeometry");

        Commit();
    }

    /// <summary>
    /// Creates linear motion blur geometry with 2 time steps (t=0 and t=1) using spans.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="verticesT0">Vertex positions at time t=0 (shutter open)</param>
    /// <param name="verticesT1">Vertex positions at time t=1 (shutter close)</param>
    /// <param name="triangleIndices">Triangle indices (length = number of triangles * 3)</param>
    /// <param name="quality">Build quality for acceleration structures</param>
    /// <remarks>
    /// This overload accepts spans for zero-copy scenarios. The data is internally copied to managed arrays.
    /// </remarks>
    public MotionBlurGeometry(Device device, ReadOnlySpan<V3f> verticesT0, ReadOnlySpan<V3f> verticesT1, ReadOnlySpan<int> triangleIndices, RTCBuildQuality quality)
        : this(device, new ReadOnlyMemory<V3f>(verticesT0.ToArray()), new ReadOnlyMemory<V3f>(verticesT1.ToArray()), new ReadOnlyMemory<int>(triangleIndices.ToArray()), quality)
    {
    }

    /// <summary>
    /// Creates multi-segment motion blur geometry with N time steps.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="vertexTimeSteps">Array of vertex positions, one per time step (minimum 2 steps required)</param>
    /// <param name="triangleIndices">Triangle indices (length = number of triangles * 3, shared across time steps)</param>
    /// <param name="quality">Build quality for acceleration structures</param>
    /// <remarks>
    /// Multi-segment motion blur supports complex motion paths by interpolating through multiple vertex position sets.
    /// Time steps are evenly distributed across the [0,1] range (e.g., 3 steps = t=0, t=0.5, t=1).
    /// Use this for objects with non-linear motion (acceleration, rotation, deformation).
    /// Typical usage: 2 steps (linear), 3-4 steps (curved paths), 5+ steps (complex deformation).
    /// </remarks>
    public MotionBlurGeometry(Device device, ReadOnlyMemory<V3f>[] vertexTimeSteps, ReadOnlyMemory<int> triangleIndices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.Triangle, quality)
    {
        if (vertexTimeSteps == null || vertexTimeSteps.Length < 2)
            throw new ArgumentException("At least 2 time steps required for motion blur", nameof(vertexTimeSteps));

        int vertexCount = vertexTimeSteps[0].Length;
        foreach (var step in vertexTimeSteps)
        {
            if (step.Length != vertexCount)
                throw new ArgumentException("Vertex counts must match for all time steps");
        }

        m_timeSteps = vertexTimeSteps.Length;
        m_vertexBuffers = new List<EmbreeBuffer<V3f>>();

        // Set time step count before setting buffers
        EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, (uint)m_timeSteps);
        device.CheckError("MotionBlurGeometry.rtcSetGeometryTimeStepCount");

        // Create and set vertex buffers for each time step
        for (uint i = 0; i < m_timeSteps; i++)
        {
            var buffer = EmbreeBuffer.Create(device, vertexTimeSteps[i]);
            m_vertexBuffers.Add(buffer);
            EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, i, RTCFormat.FLOAT3, buffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertexCount);
            device.CheckError($"MotionBlurGeometry.rtcSetGeometryBuffer(Vertex, slot={i})");
        }

        // Create and set index buffer (shared across all time steps)
        m_indices = EmbreeBuffer.Create(device, triangleIndices);
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT3, m_indices.Handle, 0, (nuint)(sizeof(int) * 3), (nuint)(triangleIndices.Length / 3));
        device.CheckError("MotionBlurGeometry.rtcSetGeometryBuffer(Index)");

        // Set vertex attribute buffers for interpolation (only slot 0)
        EmbreeAPI.rtcSetGeometryVertexAttributeCount(Handle, 1);
        device.CheckError("MotionBlurGeometry.rtcSetGeometryVertexAttributeCount");

        // For vertex attributes with motion blur, we only set slot 0
        // The motion blur interpolation is handled through the vertex buffers
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.VertexAttribute, 0, RTCFormat.FLOAT3, m_vertexBuffers[0].Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertexCount);
        device.CheckError("MotionBlurGeometry.rtcSetGeometryBuffer(VertexAttribute, slot=0)");

        // Enable geometry before commit (required for interpolation)
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("MotionBlurGeometry.rtcEnableGeometry");

        Commit();
    }


    /// <summary>
    /// Sets a custom time range for motion blur geometry.
    /// </summary>
    /// <param name="startTime">Start time of visibility range (0 to 1)</param>
    /// <param name="endTime">End time of visibility range (0 to 1)</param>
    /// <remarks>
    /// Allows geometry to appear/disappear during camera shutter time for effects like fade-in/fade-out.
    /// Time range must be a sub-range of [0,1]. Default is [0,1] (visible for entire shutter duration).
    /// Example: SetTimeRange(0.5f, 1.0f) makes the object visible only in the second half of exposure.
    /// </remarks>
    public void SetTimeRange(float startTime, float endTime)
    {
        if (startTime < 0.0f || startTime > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(startTime), "Start time must be in range [0, 1]");
        if (endTime < 0.0f || endTime > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(endTime), "End time must be in range [0, 1]");
        if (startTime >= endTime)
            throw new ArgumentException("Start time must be less than end time");

        EmbreeAPI.rtcSetGeometryTimeRange(Handle, startTime, endTime);
        m_device.CheckError("MotionBlurGeometry.rtcSetGeometryTimeRange");
    }

    /// <summary>
    /// Disposes resources used by the motion blur geometry.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            foreach (var buffer in m_vertexBuffers)
                buffer.Dispose();
            m_indices.Dispose();
        }
        base.Dispose(disposing);
    }
}
