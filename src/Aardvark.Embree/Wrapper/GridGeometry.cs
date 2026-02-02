using Aardvark.Base;
using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Grid primitive structure defining a rectangular region of structured vertices.
/// </summary>
/// <remarks>
/// Defines a rectangular grid within a vertex buffer for use with GridGeometry.
/// Grid cells are defined implicitly by the rectangular arrangement of vertices.
/// Total primitives = (width-1) * (height-1) quads.
/// Stride allows non-contiguous grid data within a larger vertex buffer.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct RTCGrid
{
    /// <summary>
    /// Start vertex index in the vertex buffer
    /// </summary>
    public uint startVertexID;

    /// <summary>
    /// Number of vertices to skip for each row (typically equals width for contiguous grids)
    /// </summary>
    public uint stride;

    /// <summary>
    /// Width of the grid (number of vertices in u direction)
    /// </summary>
    public ushort width;

    /// <summary>
    /// Height of the grid (number of vertices in v direction)
    /// </summary>
    public ushort height;
}

/// <summary>
/// Grid geometry for structured meshes like terrain and heightmaps.
/// </summary>
/// <remarks>
/// Grid geometry represents structured rectangular meshes efficiently.
/// Each grid defines a rectangular region of vertices forming implicit quads.
/// Ideal for terrain, heightmaps, and regularly sampled surfaces.
/// Multiple grid primitives can reference the same vertex buffer.
/// All buffer operations support Span&lt;T&gt; APIs for zero-copy operations.
/// Dispose releases native Embree resources and buffer memory.
/// </remarks>
public class GridGeometry : EmbreeGeometry
{
    private readonly EmbreeBuffer<V3f> m_vertices;
    private readonly EmbreeBuffer<RTCGrid> m_grids;
    private readonly int m_vertexCount;

    /// <summary>
    /// Creates grid geometry from vertex positions and grid definitions.
    /// </summary>
    /// <param name="device">Embree device to create geometry on</param>
    /// <param name="vertices">Vertex positions (FLOAT3 format)</param>
    /// <param name="grids">Grid definitions (each defines a rectangular region within the vertex buffer)</param>
    /// <param name="quality">Build quality affecting BVH construction</param>
    /// <remarks>
    /// Creates internal Embree buffers for vertices and grid descriptors.
    /// Each RTCGrid references vertices by index, allowing efficient memory use for multiple grids.
    /// Grid stride is 12 bytes (size of RTCGrid structure).
    /// Commits the geometry immediately after buffer setup.
    /// Memory: Allocates native buffers owned by Embree.
    /// </remarks>
    public GridGeometry(Device device, ReadOnlyMemory<V3f> vertices, ReadOnlyMemory<RTCGrid> grids, RTCBuildQuality quality)
        : base(device, RTCGeometryType.Grid, quality)
    {
        m_vertexCount = vertices.Length;
        m_vertices = EmbreeBuffer.Create(device, vertices);
        m_grids = EmbreeBuffer.Create(device, grids);

        // Note: rtcSetGeometryPrimitiveCount does not exist in Embree 4.
        // The primitive count is determined by the buffer count parameter.

        // Set vertex buffer (FLOAT3)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            m_vertices.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);
        device.CheckError("GridGeometry.rtcSetGeometryBuffer(Vertex)");

        // Set grid buffer (GRID format)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Grid, 0, RTCFormat.GRID,
            m_grids.Handle, 0, (nuint)12, (nuint)grids.Length);
        device.CheckError("GridGeometry.rtcSetGeometryBuffer(Grid)");

        // Enable geometry before commit
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("GridGeometry.rtcEnableGeometry");

        Commit();
    }

    /// <summary>
    /// Creates grid geometry from existing Embree buffers with byte offsets.
    /// </summary>
    /// <param name="device">Embree device to create geometry on</param>
    /// <param name="vertexBuffer">Existing vertex buffer (V3f elements)</param>
    /// <param name="vertexOffset">Byte offset into vertex buffer</param>
    /// <param name="vertexCount">Number of vertices to use</param>
    /// <param name="gridBuffer">Existing grid descriptor buffer (RTCGrid elements)</param>
    /// <param name="gridOffset">Byte offset into grid buffer</param>
    /// <param name="gridCount">Number of grid descriptors</param>
    /// <param name="quality">Build quality affecting BVH construction</param>
    /// <remarks>
    /// Uses existing buffers without copying data - retains buffer references.
    /// Grid stride is 12 bytes (size of RTCGrid structure).
    /// Commits the geometry immediately after buffer setup.
    /// Memory: Retains references to provided buffers, does not allocate new buffers.
    /// </remarks>
    public GridGeometry(Device device, EmbreeBuffer<V3f> vertexBuffer, int vertexOffset, int vertexCount,
                        EmbreeBuffer<RTCGrid> gridBuffer, int gridOffset, int gridCount, RTCBuildQuality quality)
        : base(device, RTCGeometryType.Grid, quality)
    {
        EmbreeAPI.rtcRetainBuffer(vertexBuffer.Handle);
        device.CheckError("GridGeometry.rtcRetainBuffer(Vertex)");
        EmbreeAPI.rtcRetainBuffer(gridBuffer.Handle);
        device.CheckError("GridGeometry.rtcRetainBuffer(Grid)");

        m_vertices = vertexBuffer;
        m_grids = gridBuffer;
        m_vertexCount = vertexCount;

        // Note: rtcSetGeometryPrimitiveCount does not exist in Embree 4.
        // The primitive count is determined by the buffer count parameter.

        // Set vertex buffer (FLOAT3)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            m_vertices.Handle, (nuint)vertexOffset, (nuint)(sizeof(float) * 3), (nuint)vertexCount);
        device.CheckError("GridGeometry.rtcSetGeometryBuffer(Vertex)");

        // Set grid buffer (GRID format)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Grid, 0, RTCFormat.GRID,
            m_grids.Handle, (nuint)gridOffset, (nuint)12, (nuint)gridCount);
        device.CheckError("GridGeometry.rtcSetGeometryBuffer(Grid)");

        // Enable geometry before commit
        EmbreeAPI.rtcEnableGeometry(Handle);
        device.CheckError("GridGeometry.rtcEnableGeometry");

        Commit();
    }

    /// <summary>
    /// Updates vertex positions by copying from a ReadOnlyMemory source.
    /// </summary>
    /// <param name="vertices">New vertex positions to copy</param>
    /// <remarks>
    /// Copies data into the internal vertex buffer.
    /// After calling this, you MUST call UpdateBuffer(RTCBufferType.Vertex) and Commit() to notify Embree of changes.
    /// Use with RTC_BUILD_QUALITY_REFIT for efficient deformable geometry updates (e.g., animated terrain).
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
    /// Use with RTC_BUILD_QUALITY_REFIT for efficient deformable geometry updates (e.g., animated terrain).
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
    /// Use with RTC_BUILD_QUALITY_REFIT for efficient deformable geometry updates (e.g., animated terrain).
    /// Pointer becomes invalid after Dispose is called.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">If geometry has been disposed</exception>
    public unsafe V3f* GetVertexDataPointer()
    {
        ThrowIfDisposed();
        return m_vertices.GetDataPointer();
    }

    /// <summary>
    /// Gets a pointer to the grid descriptor buffer for direct in-place manipulation.
    /// </summary>
    /// <returns>Pointer to the first grid descriptor in the buffer</returns>
    /// <remarks>
    /// Enables direct memory access for modifying grid structure.
    /// After modifying data through this pointer, you MUST call UpdateBuffer(RTCBufferType.Grid) and Commit().
    /// Modifying grid descriptors changes which vertices are used for each grid region.
    /// Pointer becomes invalid after Dispose is called.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">If geometry has been disposed</exception>
    public unsafe RTCGrid* GetGridDataPointer()
    {
        ThrowIfDisposed();
        return m_grids.GetDataPointer();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_vertices.Dispose();
            m_grids.Dispose();
        }
        base.Dispose(disposing);
    }
}
