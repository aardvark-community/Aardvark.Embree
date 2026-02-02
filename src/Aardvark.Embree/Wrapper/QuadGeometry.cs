using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Quad mesh geometry with 4-vertex faces, similar to triangle meshes.
/// </summary>
/// <remarks>
/// Quad geometry uses indexed vertex buffers where each quad is defined by 4 vertex indices.
/// Embree renders quads as two triangles internally but preserves quad semantics for intersection.
/// Supports vertex attribute interpolation and deformable geometry updates.
/// All buffer operations support Span&lt;T&gt; APIs for zero-copy operations.
/// Dispose releases native Embree resources and buffer memory.
/// </remarks>
public class QuadGeometry : EmbreeGeometry
{
    private readonly EmbreeBuffer<V3f> m_vertices;
    private readonly EmbreeBuffer<int> m_indices;
    private readonly int m_vertexCount;

    /// <summary>
    /// Creates quad geometry from vertex positions and quad indices.
    /// </summary>
    /// <param name="device">Embree device to create geometry on</param>
    /// <param name="vertices">Vertex positions (FLOAT3 format)</param>
    /// <param name="quadIndices">Quad indices (4 indices per quad, UINT4 format)</param>
    /// <param name="quality">Build quality affecting BVH construction</param>
    /// <remarks>
    /// Creates internal Embree buffers for vertices and indices.
    /// Quad indices must contain a multiple of 4 elements.
    /// Commits the geometry immediately after buffer setup.
    /// Memory: Allocates native buffers owned by Embree.
    /// </remarks>
    /// <exception cref="ArgumentException">If quadIndices length is not a multiple of 4</exception>
    public QuadGeometry(Device device, ReadOnlyMemory<V3f> vertices, ReadOnlyMemory<int> quadIndices, RTCBuildQuality quality)
        : base(device, RTCGeometryType.Quad, quality)
    {
        if (quadIndices.Length % 4 != 0)
            throw new ArgumentException("Quad indices must be a multiple of 4", nameof(quadIndices));

        m_vertexCount = vertices.Length;
        m_vertices = EmbreeBuffer.Create(device, vertices);
        m_indices = EmbreeBuffer.Create(device, quadIndices);

        // quad index buffer needs to be UINT4
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, m_indices.Handle, 0, (nuint)(sizeof(int) * 4), (nuint)(quadIndices.Length / 4));
        // quad vertex needs to be FLOAT3
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, m_vertices.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

        Commit();

        device.CheckError("Create QuadGeometry");
    }

    /// <summary>
    /// Creates quad geometry from existing Embree buffers with byte offsets.
    /// </summary>
    /// <param name="device">Embree device to create geometry on</param>
    /// <param name="vertexBuffer">Existing vertex buffer (V3f elements)</param>
    /// <param name="vertexOffset">Byte offset into vertex buffer</param>
    /// <param name="vertexCount">Number of vertices to use</param>
    /// <param name="indexBuffer">Existing index buffer (int32 elements)</param>
    /// <param name="indexOffset">Byte offset into index buffer</param>
    /// <param name="quadCount">Number of quads (index count = quadCount * 4)</param>
    /// <param name="quality">Build quality affecting BVH construction</param>
    /// <remarks>
    /// Uses existing buffers without copying data - retains buffer references.
    /// Commits the geometry immediately after buffer setup.
    /// Memory: Retains references to provided buffers, does not allocate new buffers.
    /// </remarks>
    public QuadGeometry(Device device, EmbreeBuffer<V3f> vertexBuffer, int vertexOffset, int vertexCount,
                        EmbreeBuffer<int> indexBuffer, int indexOffset, int quadCount, RTCBuildQuality quality)
        : base(device, RTCGeometryType.Quad, quality)
    {
        EmbreeAPI.rtcRetainBuffer(vertexBuffer.Handle);
        EmbreeAPI.rtcRetainBuffer(indexBuffer.Handle);
        m_vertices = vertexBuffer;
        m_indices = indexBuffer;
        m_vertexCount = vertexCount;

        // quad index buffer needs to be UINT4
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT4, m_indices.Handle, (nuint)indexOffset * sizeof(int), (nuint)(sizeof(int) * 4), (nuint)quadCount);
        // quad vertex needs to be FLOAT3
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, m_vertices.Handle, (nuint)vertexOffset * sizeof(float) * 3, (nuint)(sizeof(float) * 3), (nuint)vertexCount);

        Commit();

        device.CheckError("Create QuadGeometry");
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

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_vertices.Dispose();
            m_indices.Dispose();
        }
        base.Dispose(disposing);
    }
}
