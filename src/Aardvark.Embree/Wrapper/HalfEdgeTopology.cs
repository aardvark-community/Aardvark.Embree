using System;
using System.Collections.Generic;

namespace Aardvark.Embree;

/// <summary>
/// Provides half-edge topology queries for subdivision geometries.
/// </summary>
/// <remarks>
/// Half-edge data structures allow efficient traversal of mesh topology.
/// Each edge is represented by two half-edges (one for each adjacent face).
/// Use this to walk around faces, find adjacent faces, and query mesh connectivity.
/// Only valid for subdivision surface geometries.
/// </remarks>
public class HalfEdgeTopology
{
    private readonly IntPtr m_geometryHandle;
    private readonly uint m_topologyID;

    /// <summary>
    /// Creates a half-edge topology query object for a subdivision geometry.
    /// </summary>
    /// <param name="geometryHandle">The native Embree geometry handle</param>
    /// <param name="topologyID">The topology ID (default 0 for single-level topology)</param>
    public HalfEdgeTopology(IntPtr geometryHandle, uint topologyID = 0)
    {
        m_geometryHandle = geometryHandle;
        m_topologyID = topologyID;
    }

    /// <summary>
    /// Gets the first half-edge of a face.
    /// </summary>
    /// <param name="faceID">The face ID to query</param>
    /// <returns>The half-edge ID, or uint.MaxValue if invalid</returns>
    public uint GetFirstHalfEdge(uint faceID)
    {
        return EmbreeAPI.rtcGetGeometryFirstHalfEdge(m_geometryHandle, faceID);
    }

    /// <summary>
    /// Gets the face that a half-edge belongs to.
    /// </summary>
    /// <param name="edgeID">The half-edge ID</param>
    /// <returns>The face ID, or uint.MaxValue if invalid</returns>
    public uint GetFace(uint edgeID)
    {
        return EmbreeAPI.rtcGetGeometryFace(m_geometryHandle, edgeID);
    }

    /// <summary>
    /// Gets the next half-edge in the face (counter-clockwise traversal).
    /// </summary>
    /// <param name="edgeID">The current half-edge ID</param>
    /// <returns>The next half-edge ID, or uint.MaxValue if invalid</returns>
    public uint GetNextHalfEdge(uint edgeID)
    {
        return EmbreeAPI.rtcGetGeometryNextHalfEdge(m_geometryHandle, edgeID);
    }

    /// <summary>
    /// Gets the previous half-edge in the face (clockwise traversal).
    /// </summary>
    /// <param name="edgeID">The current half-edge ID</param>
    /// <returns>The previous half-edge ID, or uint.MaxValue if invalid</returns>
    public uint GetPreviousHalfEdge(uint edgeID)
    {
        return EmbreeAPI.rtcGetGeometryPreviousHalfEdge(m_geometryHandle, edgeID);
    }

    /// <summary>
    /// Gets the opposite half-edge (the half-edge on the adjacent face sharing the same edge).
    /// </summary>
    /// <param name="edgeID">The half-edge ID</param>
    /// <returns>The opposite half-edge ID, or uint.MaxValue if no adjacent face (boundary edge)</returns>
    public uint GetOppositeHalfEdge(uint edgeID)
    {
        return EmbreeAPI.rtcGetGeometryOppositeHalfEdge(m_geometryHandle, m_topologyID, edgeID);
    }

    /// <summary>
    /// Enumerates all half-edges around a face in counter-clockwise order.
    /// </summary>
    /// <param name="faceID">The face ID to enumerate</param>
    /// <returns>Sequence of half-edge IDs for the face</returns>
    public IEnumerable<uint> EnumerateFaceHalfEdges(uint faceID)
    {
        var firstEdge = GetFirstHalfEdge(faceID);
        if (firstEdge == uint.MaxValue)
            yield break;

        var currentEdge = firstEdge;
        do
        {
            yield return currentEdge;
            currentEdge = GetNextHalfEdge(currentEdge);

            if (currentEdge == uint.MaxValue)
                break;
        }
        while (currentEdge != firstEdge);
    }
}
