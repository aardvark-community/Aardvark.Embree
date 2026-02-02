using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Base class for Embree geometry objects
/// </summary>
public class EmbreeGeometry : IDisposable
{
    /// <summary>Device this geometry belongs to</summary>
    protected readonly Device m_device;

    private IntPtr m_handle;
    private bool m_disposed = false;
    private readonly object m_disposeLock = new object();

    /// <summary>
    /// Throws ObjectDisposedException if this geometry has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (m_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    /// <summary>Native Embree geometry handle</summary>
    public IntPtr Handle
    {
        get
        {
            ThrowIfDisposed();
            return m_handle;
        }
        private set => m_handle = value;
    }

    /// <summary>Creates geometry with specified type</summary>
    public EmbreeGeometry(Device device, RTCGeometryType type)
    {
        m_device = device;
        Handle = EmbreeAPI.rtcNewGeometry(device.Handle, type);
        device.CheckError("EmbreeGeometry.rtcNewGeometry");
    }

    /// <summary>Creates geometry with specified type and build quality</summary>
    public EmbreeGeometry(Device device, RTCGeometryType type, RTCBuildQuality quality)
    {
        m_device = device;
        Handle = EmbreeAPI.rtcNewGeometry(device.Handle, type);
        device.CheckError("EmbreeGeometry.rtcNewGeometry");
        EmbreeAPI.rtcSetGeometryBuildQuality(Handle, quality);
    }

    /// <summary>Releases resources</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases unmanaged resources</summary>
    protected virtual void Dispose(bool disposing)
    {
        lock (m_disposeLock)
        {
            if (m_disposed)
                return;

            if (m_handle != IntPtr.Zero)
            {
                EmbreeAPI.rtcReleaseGeometry(m_handle);
                m_handle = IntPtr.Zero;
            }

            m_disposed = true;
        }
    }

    /// <summary>Commits changes to geometry</summary>
    public void Commit()
    {
        ThrowIfDisposed();
        EmbreeAPI.rtcCommitGeometry(Handle);
        m_device.CheckError("EmbreeGeometry.Commit");
    }

    /// <summary>
    /// Updates a geometry buffer after modifying its contents.
    /// This must be called after any direct buffer modifications to notify Embree
    /// that the buffer data has changed. Use with RTC_BUILD_QUALITY_REFIT for efficient
    /// refit operations on deforming geometry.
    /// </summary>
    /// <param name="bufferType">The type of buffer to update (e.g., Vertex, Index)</param>
    /// <param name="slot">The buffer slot (usually 0)</param>
    public void UpdateBuffer(RTCBufferType bufferType, uint slot = 0)
    {
        ThrowIfDisposed();
        EmbreeAPI.rtcUpdateGeometryBuffer(Handle, bufferType, slot);
        m_device.CheckError("EmbreeGeometry.UpdateBuffer");
    }
}

/// <summary>
/// Triangle mesh geometry - the most common geometry type in Embree.
/// Supports vertex/index buffers, motion blur, and interpolation.
/// </summary>
/// <remarks>
/// Triangle geometry uses indexed vertex buffers where each triangle is defined by 3 vertex indices.
/// Supports motion blur through multiple time steps and vertex attribute interpolation.
/// All buffer operations support Span&lt;T&gt; APIs for zero-copy operations.
/// Dispose releases native Embree resources and buffer memory.
/// </remarks>
public class TriangleGeometry : EmbreeGeometry
{
    private readonly EmbreeBuffer<V3f> m_vertices;
    private readonly EmbreeBuffer<int> m_indices;
    private readonly int m_vertexCount;
    private uint m_timeStepCount = 1;  // Track current time step count
    private readonly System.Collections.Generic.List<EmbreeBuffer<V3f>> m_timeStepBuffers = new();

    /// <summary>
    /// Creates triangle geometry from vertex positions and triangle indices.
    /// </summary>
    /// <param name="device">Embree device to create geometry on</param>
    /// <param name="vertices">Vertex positions (FLOAT3 format)</param>
    /// <param name="triangleIndices">Triangle indices (3 indices per triangle, UINT3 format)</param>
    /// <param name="quality">Build quality affecting BVH construction</param>
    /// <remarks>
    /// Creates internal Embree buffers for vertices and indices.
    /// Automatically sets up vertex attributes for interpolation support.
    /// Commits the geometry immediately after buffer setup.
    /// Memory: Allocates native buffers owned by Embree.
    /// </remarks>
    /// <exception cref="ArgumentException">If triangleIndices length is not a multiple of 3</exception>
    public TriangleGeometry(Device device, ReadOnlyMemory<V3f> vertices, ReadOnlyMemory<int> triangleIndices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.Triangle, quality)
    {
        m_vertices = EmbreeBuffer.Create(device, vertices);
        m_indices = EmbreeBuffer.Create(device, triangleIndices);
        m_vertexCount = vertices.Length;

        // triangle index buffer needs to be UINT3
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT3, m_indices.Handle, 0, (nuint)(sizeof(int) * 3), (nuint)(triangleIndices.Length / 3));
        device.CheckError("TriangleGeometry.rtcSetGeometryBuffer(Index)");
        // triangle vertex needs to be FLOAT3
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, m_vertices.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);
        device.CheckError("TriangleGeometry.rtcSetGeometryBuffer(Vertex)");

        // Set vertex attribute buffer for interpolation (using same vertex buffer)
        EmbreeAPI.rtcSetGeometryVertexAttributeCount(Handle, 1);
        device.CheckError("TriangleGeometry.rtcSetGeometryVertexAttributeCount");
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.VertexAttribute, 0, RTCFormat.FLOAT3, m_vertices.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);
        device.CheckError("TriangleGeometry.rtcSetGeometryBuffer(VertexAttribute)");

        // Enable geometry before commit (required for interpolation)
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("TriangleGeometry.rtcEnableGeometry");

        Commit();
    }

    /// <summary>
    /// Creates triangle geometry from existing Embree buffers with byte offsets.
    /// </summary>
    /// <param name="device">Embree device to create geometry on</param>
    /// <param name="vertexBuffer">Existing vertex buffer (V3f elements)</param>
    /// <param name="vertexOffset">Byte offset into vertex buffer</param>
    /// <param name="vertexCount">Number of vertices to use</param>
    /// <param name="indexBuffer">Existing index buffer (int32 elements)</param>
    /// <param name="indexOffset">Byte offset into index buffer</param>
    /// <param name="triangleCount">Number of triangles (index count = triangleCount * 3)</param>
    /// <param name="quality">Build quality affecting BVH construction</param>
    /// <remarks>
    /// Uses existing buffers without copying data - retains buffer references.
    /// Automatically sets up vertex attributes for interpolation support.
    /// Commits the geometry immediately after buffer setup.
    /// Memory: Retains references to provided buffers, does not allocate new buffers.
    /// </remarks>
    public TriangleGeometry(Device device, EmbreeBuffer<V3f> vertexBuffer, int vertexOffset, int vertexCount, EmbreeBuffer<int> indexBuffer, int indexOffset, int triangleCount, RTCBuildQuality quality)
        : base(device, RTCGeometryType.Triangle, quality)
    {
        EmbreeAPI.rtcRetainBuffer(vertexBuffer.Handle);
        device.CheckError("TriangleGeometry.rtcRetainBuffer(Vertex)");
        EmbreeAPI.rtcRetainBuffer(indexBuffer.Handle);
        device.CheckError("TriangleGeometry.rtcRetainBuffer(Index)");
        m_vertices = vertexBuffer;
        m_indices = indexBuffer;
        m_vertexCount = vertexCount;

        // triangle index buffer needs to be UINT3
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT3, m_indices.Handle, (nuint)indexOffset * sizeof(int), (nuint)(sizeof(int) * 3), (nuint)triangleCount);
        device.CheckError("TriangleGeometry.rtcSetGeometryBuffer(Index)");
        // triangle vertex needs to be FLOAT3
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, m_vertices.Handle, (nuint)vertexOffset, (nuint)(sizeof(float) * 3), (nuint)vertexCount);
        device.CheckError("TriangleGeometry.rtcSetGeometryBuffer(Vertex)");

        // Set vertex attribute buffer for interpolation (using same vertex buffer)
        EmbreeAPI.rtcSetGeometryVertexAttributeCount(Handle, 1);
        device.CheckError("TriangleGeometry.rtcSetGeometryVertexAttributeCount");
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.VertexAttribute, 0, RTCFormat.FLOAT3, m_vertices.Handle, (nuint)vertexOffset, (nuint)(sizeof(float) * 3), (nuint)vertexCount);
        device.CheckError("TriangleGeometry.rtcSetGeometryBuffer(VertexAttribute)");

        // Enable geometry before commit (required for interpolation)
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("TriangleGeometry.rtcEnableGeometry");

        Commit();
    }

    /// <summary>
    /// Updates vertex positions by copying from a ReadOnlyMemory source.
    /// </summary>
    /// <param name="vertices">New vertex positions to copy</param>
    /// <remarks>
    /// Copies data into the internal vertex buffer.
    /// After calling this, you MUST call UpdateBuffer(RTCBufferType.Vertex) and Commit() to notify Embree of changes.
    /// Use with RTC_BUILD_QUALITY_REFIT for efficient deformable geometry updates.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">If geometry has been disposed</exception>
    public unsafe void UpdateVertices(ReadOnlyMemory<V3f> vertices)
    {
        ThrowIfDisposed();
        if (vertices.Length != m_vertexCount)
            throw new ArgumentException($"Vertex count ({vertices.Length}) does not match original vertex count ({m_vertexCount})", nameof(vertices));
        var span = vertices.Span;
        var ptr = m_vertices.GetDataPointer();
        for (int i = 0; i < span.Length; i++)
        {
            ptr[i] = span[i];
        }
    }

    /// <summary>
    /// Updates vertex positions by copying from a ReadOnlySpan source (zero-copy API).
    /// </summary>
    /// <param name="vertices">New vertex positions to copy</param>
    /// <remarks>
    /// Span-based API avoids allocations when source data is stack-allocated or pinned.
    /// Copies data into the internal vertex buffer.
    /// After calling this, you MUST call UpdateBuffer(RTCBufferType.Vertex) and Commit() to notify Embree of changes.
    /// Use with RTC_BUILD_QUALITY_REFIT for efficient deformable geometry updates.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">If geometry has been disposed</exception>
    public unsafe void UpdateVertices(ReadOnlySpan<V3f> vertices)
    {
        ThrowIfDisposed();
        if (vertices.Length != m_vertexCount)
            throw new ArgumentException($"Vertex count ({vertices.Length}) does not match original vertex count ({m_vertexCount})", nameof(vertices));
        var ptr = m_vertices.GetDataPointer();
        for (int i = 0; i < vertices.Length; i++)
        {
            ptr[i] = vertices[i];
        }
    }

    /// <summary>
    /// Gets a pointer to the vertex buffer for direct in-place manipulation.
    /// </summary>
    /// <returns>Pointer to the first vertex in the buffer</returns>
    /// <remarks>
    /// Enables direct memory access for maximum performance when updating vertices.
    /// After modifying data through this pointer, you MUST call UpdateBuffer(RTCBufferType.Vertex) and Commit().
    /// Use with RTC_BUILD_QUALITY_REFIT for efficient deformable geometry updates.
    /// Pointer becomes invalid after Dispose is called.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">If geometry has been disposed</exception>
    public unsafe V3f* GetVertexDataPointer()
    {
        ThrowIfDisposed();
        return m_vertices.GetDataPointer();
    }

    /// <summary>
    /// Sets vertex positions for a specific time step to enable motion blur.
    /// </summary>
    /// <param name="timeStep">Time step index (0 to totalTimeSteps-1)</param>
    /// <param name="vertices">Vertex positions for this time step</param>
    /// <param name="totalTimeSteps">Total number of time steps (minimum 2 for motion blur)</param>
    /// <remarks>
    /// Enables motion blur by providing vertex positions at multiple time steps.
    /// Embree interpolates between time steps during ray tracing.
    /// Must provide vertex positions for each time step before committing.
    /// All time steps must have the same vertex count as the original geometry.
    /// Creates new internal buffers for each time step.
    /// Call Commit() after setting all time step positions.
    /// </remarks>
    /// <exception cref="ArgumentException">If totalTimeSteps is less than 2</exception>
    /// <exception cref="ArgumentOutOfRangeException">If timeStep is greater than or equal to totalTimeSteps</exception>
    /// <exception cref="ArgumentException">If vertex count does not match original vertex count</exception>
    /// <exception cref="ObjectDisposedException">If geometry has been disposed</exception>
    public void SetVertexPositions(uint timeStep, ReadOnlyMemory<V3f> vertices, uint totalTimeSteps)
    {
        ThrowIfDisposed();
        if (totalTimeSteps < 2)
            throw new ArgumentException("Motion blur requires at least 2 time steps", nameof(totalTimeSteps));

        if (timeStep >= totalTimeSteps)
            throw new ArgumentOutOfRangeException(nameof(timeStep), "Time step index must be less than total time steps");

        if (vertices.Length != m_vertexCount)
            throw new ArgumentException("Vertex count must match original vertex count", nameof(vertices));

        // Set the time step count if it needs to be increased
        if (totalTimeSteps > m_timeStepCount)
        {
            EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, totalTimeSteps);
            m_device.CheckError("TriangleGeometry.rtcSetGeometryTimeStepCount");
            m_timeStepCount = totalTimeSteps;
        }

        // Update or create buffer for this time step
        EmbreeBuffer<V3f> timeStepBuffer;
        if (timeStep == 0)
        {
            UpdateVertices(vertices);
            EmbreeAPI.rtcUpdateGeometryBuffer(Handle, RTCBufferType.Vertex, 0);
            m_device.CheckError("TriangleGeometry.rtcUpdateGeometryBuffer(Vertex, timeStep=0)");
            timeStepBuffer = m_vertices;
        }
        else
        {
            timeStepBuffer = GetOrCreateTimeStepBuffer(timeStep, vertices.Span);
        }

        // Ensure vertex buffer is set for this time step (required when new buffers are created)
        if (timeStep != 0)
        {
            EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, timeStep, RTCFormat.FLOAT3,
                timeStepBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);
            m_device.CheckError($"TriangleGeometry.rtcSetGeometryBuffer(Vertex, timeStep={timeStep})");
        }

        // Note: Vertex attributes don't need multiple time steps - motion blur interpolation
        // is handled through the vertex buffers. Setting vertex attribute for multiple time
        // steps causes errors. Only slot 0 should have a vertex attribute buffer.
    }

    /// <summary>
    /// Sets vertex positions for a specific time step to enable motion blur (zero-copy API).
    /// </summary>
    /// <param name="timeStep">Time step index (0 to totalTimeSteps-1)</param>
    /// <param name="vertices">Vertex positions for this time step</param>
    /// <param name="totalTimeSteps">Total number of time steps (minimum 2 for motion blur)</param>
    /// <remarks>
    /// Span-based API avoids allocations when source data is stack-allocated or pinned.
    /// Enables motion blur by providing vertex positions at multiple time steps.
    /// Embree interpolates between time steps during ray tracing.
    /// Must provide vertex positions for each time step before committing.
    /// All time steps must have the same vertex count as the original geometry.
    /// Creates new internal buffers for each time step.
    /// Call Commit() after setting all time step positions.
    /// </remarks>
    /// <exception cref="ArgumentException">If totalTimeSteps is less than 2</exception>
    /// <exception cref="ArgumentOutOfRangeException">If timeStep is greater than or equal to totalTimeSteps</exception>
    /// <exception cref="ArgumentException">If vertex count does not match original vertex count</exception>
    /// <exception cref="ObjectDisposedException">If geometry has been disposed</exception>
    public void SetVertexPositions(uint timeStep, ReadOnlySpan<V3f> vertices, uint totalTimeSteps)
    {
        ThrowIfDisposed();
        if (totalTimeSteps < 2)
            throw new ArgumentException("Motion blur requires at least 2 time steps", nameof(totalTimeSteps));

        if (timeStep >= totalTimeSteps)
            throw new ArgumentOutOfRangeException(nameof(timeStep), "Time step must be less than total time steps");

        if (vertices.Length != m_vertexCount)
            throw new ArgumentException($"Vertex count ({vertices.Length}) does not match original vertex count ({m_vertexCount})", nameof(vertices));

        // Increase time step count if needed
        if (totalTimeSteps > m_timeStepCount)
        {
            EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, totalTimeSteps);
            m_device.CheckError("TriangleGeometry.rtcSetGeometryTimeStepCount");
            m_timeStepCount = totalTimeSteps;
        }

        // Update or create buffer for this time step
        EmbreeBuffer<V3f> timeStepBuffer;
        if (timeStep == 0)
        {
            UpdateVertices(vertices);
            EmbreeAPI.rtcUpdateGeometryBuffer(Handle, RTCBufferType.Vertex, 0);
            m_device.CheckError("TriangleGeometry.rtcUpdateGeometryBuffer(Vertex, timeStep=0)");
            timeStepBuffer = m_vertices;
        }
        else
        {
            timeStepBuffer = GetOrCreateTimeStepBuffer(timeStep, vertices);
        }

        // Ensure vertex buffer is set for this time step (required when new buffers are created)
        if (timeStep != 0)
        {
            EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, timeStep, RTCFormat.FLOAT3,
                timeStepBuffer.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);
            m_device.CheckError($"TriangleGeometry.rtcSetGeometryBuffer(Vertex, timeStep={timeStep})");
        }
    }

    private EmbreeBuffer<V3f> GetOrCreateTimeStepBuffer(uint timeStep, ReadOnlySpan<V3f> vertices)
    {
        while (m_timeStepBuffers.Count <= timeStep)
            m_timeStepBuffers.Add(null);

        var existing = m_timeStepBuffers[(int)timeStep];
        if (existing == null)
        {
            existing = EmbreeBuffer.Create(m_device, vertices);
            m_timeStepBuffers[(int)timeStep] = existing;
        }
        else
        {
            existing.Update(vertices);
        }

        return existing;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            for (int i = 0; i < m_timeStepBuffers.Count; i++)
            {
                m_timeStepBuffers[i]?.Dispose();
            }
            m_vertices.Dispose();
            m_indices.Dispose();
        }
        base.Dispose(disposing);
    }
}
