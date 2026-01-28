using Aardvark.Base;
using System;
using System.Linq;
using Xunit;

namespace Aardvark.Embree.Tests;

public class HalfEdgeTopologyTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanQueryHalfEdgesOnSimpleQuad(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces, quality);

        var topology = subdiv.GetTopology();

        var firstEdge = topology.GetFirstHalfEdge(0);
        Assert.NotEqual(uint.MaxValue, firstEdge);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanTraverseFaceHalfEdges(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces, quality);

        var topology = subdiv.GetTopology();

        var firstEdge = topology.GetFirstHalfEdge(0);
        Assert.NotEqual(uint.MaxValue, firstEdge);

        var edge = firstEdge;
        var nextEdge = topology.GetNextHalfEdge(edge);
        Assert.NotEqual(uint.MaxValue, nextEdge);
        Assert.NotEqual(edge, nextEdge);

        var prevEdge = topology.GetPreviousHalfEdge(edge);
        Assert.NotEqual(uint.MaxValue, prevEdge);

        var faceId = topology.GetFace(edge);
        Assert.Equal(0u, faceId);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void EnumerateFaceHalfEdges_ReturnsAllEdgesInFace(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces, quality);

        var topology = subdiv.GetTopology();

        var faceEdges = topology.EnumerateFaceHalfEdges(0).ToArray();

        Assert.Equal(4, faceEdges.Length);
        Assert.Equal(faceEdges.Distinct().Count(), faceEdges.Length);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OppositeHalfEdge_WorksOnCube(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(1, 1, 0), new V3f(0, 1, 0),
            new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(1, 1, 1), new V3f(0, 1, 1)
        };

        var indices = new uint[]
        {
            0, 1, 2, 3,
            4, 5, 6, 7,
            0, 1, 5, 4,
            2, 3, 7, 6,
            0, 4, 7, 3,
            1, 2, 6, 5
        };

        var faces = new uint[] { 4, 4, 4, 4, 4, 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces, quality);

        var topology = subdiv.GetTopology();

        var foundOpposite = false;
        for (uint faceID = 0; faceID < 6; faceID++)
        {
            foreach (var edgeID in topology.EnumerateFaceHalfEdges(faceID))
            {
                var oppositeEdge = topology.GetOppositeHalfEdge(edgeID);
                if (oppositeEdge != uint.MaxValue)
                {
                    var oppositeFace = topology.GetFace(oppositeEdge);
                    if (oppositeFace != faceID)
                    {
                        foundOpposite = true;
                        break;
                    }
                }
            }
            if (foundOpposite) break;
        }

        Assert.True(foundOpposite, "Expected to find at least one edge with opposite on a different face in a cube");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void EnumerateFaceHalfEdges_ReturnsCorrectlyForMultipleFaces(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0),
            new V3f(2, 0, 0),
            new V3f(2, 1, 0)
        };

        var indices = new uint[]
        {
            0, 1, 2, 3,
            1, 4, 5, 2
        };

        var faces = new uint[] { 4, 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces, quality);

        var topology = subdiv.GetTopology();

        var face0Edges = topology.EnumerateFaceHalfEdges(0).ToArray();
        var face1Edges = topology.EnumerateFaceHalfEdges(1).ToArray();

        Assert.Equal(4, face0Edges.Length);
        Assert.Equal(4, face1Edges.Length);

        Assert.Empty(face0Edges.Intersect(face1Edges));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void HalfEdgeTopology_HandlesInvalidFaceID(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces, quality);

        var topology = subdiv.GetTopology();

        var invalidEdge = topology.GetFirstHalfEdge(999);
        Assert.Equal(uint.MaxValue, invalidEdge);
    }
}
