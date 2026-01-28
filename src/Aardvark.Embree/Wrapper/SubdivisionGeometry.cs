using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Catmull-Clark subdivision surface geometry.
/// </summary>
/// <remarks>
/// Subdivision surfaces provide smooth surfaces from coarse control meshes using the Catmull-Clark algorithm.
/// The control mesh is automatically subdivided at render time based on the tessellation rate.
/// Supports edge and vertex creases for sharp features (e.g., cube edges).
/// Use for organic surfaces, character models, or any geometry requiring smooth appearance without dense vertex data.
/// Higher tessellation rates produce smoother results but increase rendering cost.
/// </remarks>
public class SubdivisionGeometry : EmbreeGeometry
{
    private readonly EmbreeBuffer<V3f> m_vertices;
    private readonly EmbreeBuffer<uint> m_indices;
    private readonly EmbreeBuffer<uint> m_faces;
    private RTCDisplacementFunctionN m_displacementFunc;

    /// <summary>
    /// Creates a subdivision surface geometry from a control mesh.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="vertices">Control mesh vertex positions</param>
    /// <param name="indices">Vertex indices for all faces (concatenated, e.g., [face0_v0, face0_v1, face0_v2, face1_v0, ...])</param>
    /// <param name="faces">Number of vertices per face (e.g., [3, 4, 3] for triangle, quad, triangle)</param>
    /// <param name="quality">Build quality for acceleration structures</param>
    /// <param name="subdivisionMode">Boundary interpolation mode (SmoothBoundary or PinCorners)</param>
    /// <param name="tessellationRate">Tessellation rate controlling subdivision level (typical range: 1-16, default 4.0)</param>
    /// <remarks>
    /// The indices array contains all vertex indices concatenated together.
    /// The faces array specifies how many vertices belong to each face.
    /// Example: For 2 faces (triangle + quad), indices might be [0,1,2,0,2,3,4] and faces would be [3,4].
    /// </remarks>
    public SubdivisionGeometry(Device device, ReadOnlyMemory<V3f> vertices, ReadOnlyMemory<uint> indices, ReadOnlyMemory<uint> faces,
                               RTCBuildQuality quality, RTCSubdivisionMode subdivisionMode = RTCSubdivisionMode.SmoothBoundary, float tessellationRate = 4.0f)
        : base(device, RTCGeometryType.Subdivision, quality)
    {
        m_vertices = EmbreeBuffer.Create(device, vertices);
        m_indices = EmbreeBuffer.Create(device, indices);
        m_faces = EmbreeBuffer.Create(device, faces);

        // Set vertex buffer (FLOAT3)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3, m_vertices.Handle, 0, (nuint)(sizeof(float) * 3), (nuint)vertices.Length);

        // Set index buffer
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Index, 0, RTCFormat.UINT, m_indices.Handle, 0, (nuint)sizeof(uint), (nuint)indices.Length);

        // Set face buffer (number of vertices per face)
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.Face, 0, RTCFormat.UINT, m_faces.Handle, 0, (nuint)sizeof(uint), (nuint)faces.Length);

        // Set subdivision parameters
        EmbreeAPI.rtcSetGeometrySubdivisionMode(Handle, 0, subdivisionMode);
        EmbreeAPI.rtcSetGeometryTessellationRate(Handle, tessellationRate);

        Commit();

        device.CheckError("Create SubdivisionGeometry");
    }

    /// <summary>
    /// Sets edge crease weights for sharp edges in the subdivision surface.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="edgeCreaseIndices">Pairs of vertex indices defining creased edges (e.g., [v0, v1, v2, v3] for two edges)</param>
    /// <param name="edgeCreaseWeights">Crease sharpness weights per edge (0 = smooth, >10 = sharp, infinity = hard crease)</param>
    /// <remarks>
    /// Edge creases create sharp features along edges (e.g., cube edges).
    /// The edgeCreaseIndices array contains pairs of vertex indices (length = 2 * number of edges).
    /// Weights typically range from 0 (no crease) to 10+ (sharp crease). Use float.PositiveInfinity for infinitely sharp edges.
    /// Must call after construction to add creases.
    /// </remarks>
    public void SetEdgeCreases(Device device, ReadOnlyMemory<uint> edgeCreaseIndices, ReadOnlyMemory<float> edgeCreaseWeights)
    {
        if (edgeCreaseIndices.Length / 2 != edgeCreaseWeights.Length)
            throw new ArgumentException("Edge crease indices must be pairs, and weights must match number of pairs");

        var indexBuffer = EmbreeBuffer.Create(device, edgeCreaseIndices);
        var weightBuffer = EmbreeBuffer.Create(device, edgeCreaseWeights);

        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.EdgeCreaseIndex, 0, RTCFormat.UINT2, indexBuffer.Handle, 0, (nuint)(sizeof(uint) * 2), (nuint)(edgeCreaseIndices.Length / 2));
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.EdgeCreaseWeight, 0, RTCFormat.FLOAT, weightBuffer.Handle, 0, (nuint)sizeof(float), (nuint)edgeCreaseWeights.Length);

        Commit();
    }

    /// <summary>
    /// Sets vertex crease weights for sharp vertices in the subdivision surface.
    /// </summary>
    /// <param name="device">The Embree device</param>
    /// <param name="vertexCreaseIndices">Vertex indices to apply creases to</param>
    /// <param name="vertexCreaseWeights">Crease sharpness weights per vertex (0 = smooth, >10 = sharp, infinity = hard corner)</param>
    /// <remarks>
    /// Vertex creases create sharp corners at specific vertices (e.g., cube corners).
    /// Weights typically range from 0 (no crease) to 10+ (sharp corner). Use float.PositiveInfinity for infinitely sharp corners.
    /// Must call after construction to add creases.
    /// </remarks>
    public void SetVertexCreases(Device device, ReadOnlyMemory<uint> vertexCreaseIndices, ReadOnlyMemory<float> vertexCreaseWeights)
    {
        if (vertexCreaseIndices.Length != vertexCreaseWeights.Length)
            throw new ArgumentException("Vertex crease indices and weights must have same length");

        var indexBuffer = EmbreeBuffer.Create(device, vertexCreaseIndices);
        var weightBuffer = EmbreeBuffer.Create(device, vertexCreaseWeights);

        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.VertexCreaseIndex, 0, RTCFormat.UINT, indexBuffer.Handle, 0, (nuint)sizeof(uint), (nuint)vertexCreaseIndices.Length);
        EmbreeAPI.rtcSetGeometryBuffer(Handle, RTCBufferType.VertexCreaseWeight, 0, RTCFormat.FLOAT, weightBuffer.Handle, 0, (nuint)sizeof(float), (nuint)vertexCreaseWeights.Length);

        Commit();
    }

    /// <summary>
    /// Gets a half-edge topology query object for this subdivision geometry.
    /// </summary>
    /// <param name="topologyID">The topology ID (default 0 for single-level topology)</param>
    /// <returns>A HalfEdgeTopology object for querying mesh connectivity</returns>
    /// <remarks>
    /// Half-edge topology allows efficient traversal of mesh edges and faces.
    /// Use this to walk around faces, find adjacent faces, and query mesh connectivity.
    /// </remarks>
    public HalfEdgeTopology GetTopology(uint topologyID = 0)
    {
        return new HalfEdgeTopology(Handle, topologyID);
    }

    /// <summary>
    /// Sets a displacement function callback for procedural surface displacement.
    /// </summary>
    /// <param name="displacementFunc">Callback function to compute displaced surface positions</param>
    /// <remarks>
    /// Displacement mapping allows procedural modification of the subdivided surface geometry.
    /// The callback receives parametric coordinates (u,v), geometric normals, and base positions,
    /// and must write displaced positions to the output arrays.
    /// Common uses: height map displacement, procedural detail, animated deformation.
    /// The delegate is stored to prevent garbage collection.
    /// Must call Commit() after setting the displacement function.
    /// </remarks>
    public void SetDisplacementFunction(RTCDisplacementFunctionN displacementFunc)
    {
        if (displacementFunc == null)
            throw new ArgumentNullException(nameof(displacementFunc));

        m_displacementFunc = displacementFunc;
        var funcPtr = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(displacementFunc);
        EmbreeAPI.rtcSetGeometryDisplacementFunction(Handle, funcPtr);
    }

    /// <summary>
    /// Disposes resources used by the subdivision geometry.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if called from finalizer</param>
    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            m_vertices.Dispose();
            m_indices.Dispose();
            m_faces.Dispose();
        }
        base.Dispose(disposing);
    }
}
