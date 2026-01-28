using Aardvark.Base;
using Xunit;
using System;
using System.Threading;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests verifying all code examples from README.md work correctly.
/// Each test corresponds to a specific section in the README.
/// </summary>
public class ReadmeExampleTests
{
    // ===== Section 1: Your First Ray Trace =====

    [Fact]
    public void FirstRayTrace_Example_ProducesExpectedOutput()
    {
        // Setup (not in README)
        using var device = new Device();

        // === README code starts (VERBATIM) ===
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.25f, 0.25f, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );
        // === README code ends ===

        // Assertions (not in README) - verify expected output from README
        Assert.True(intersected);
        Assert.Equal(1.0f, hit.T, precision: 2); // "Distance: 1.000"
        var hitPoint = new V3f(0.25f, 0.25f, 1.0f) + new V3f(0, 0, -1) * hit.T;
        Assert.Equal(0.25f, hitPoint.X, precision: 2); // "Hit point: (0.25, 0.25, 0.00)"
        Assert.Equal(0.25f, hitPoint.Y, precision: 2);
        Assert.Equal(0.00f, hitPoint.Z, precision: 2);
    }

    // ===== Section 2: Basic Usage =====

    [Fact]
    public void BasicUsage_Intersect_ReturnsCorrectHit()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

        var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.25f, 0.25f, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );
        // === README code ends ===

        Assert.True(intersected);
        Assert.True(hit.T > 0.0f);
        Assert.InRange(hit.Coord.X, 0.0f, 1.0f);
        Assert.InRange(hit.Coord.Y, 0.0f, 1.0f);
    }

    [Fact]
    public void BasicUsage_GetClosestPoint_ReturnsValidResult()
    {
        using var device = new Device();

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

        var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // === README code (VERBATIM) ===
        var result = scene.GetClosestPoint(queryPoint: new V3f(0.5f, 0.5f, 1.0f));
        if (result.IsValid)
        {
            var point = result.Point;
            var uv = result.UV;
            var distance = result.DistanceSquared.Sqrt();
        }
        // === README code ends ===

        Assert.True(result.IsValid);
        Assert.InRange(result.UV.X, 0.0f, 1.0f);
        Assert.True(result.DistanceSquared >= 0);
    }

    [Fact]
    public void BasicUsage_Occluded_ReturnsTrueForHit()
    {
        using var device = new Device();

        var vertices = new[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new[] { 0, 1, 2 };
        var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

        var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // === README code (VERBATIM) ===
        bool occluded = scene.Occluded(
            rayOrigin: new V3f(0, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            minT: 0.0f, maxT: 10.0f
        );
        // === README code ends ===

        Assert.True(occluded);
    }

    // ===== Section 3: Curve Geometry =====

    [Fact]
    public void CurveGeometry_RoundBezier_CreatesSuccessfully()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var curveVertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 1, 0), 0.15f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f)
        };

        var indices = new uint[] { 0 };

        using var roundCurve = new RoundBezierCurveGeometry(device, curveVertices, indices, RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, roundCurve.Handle);
    }

    [Fact]
    public void CurveGeometry_FlatBSpline_CreatesSuccessfully()
    {
        using var device = new Device();

        var curveVertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 1, 0), 0.15f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f)
        };

        var indices = new uint[] { 0 };

        // === README code (VERBATIM) ===
        using var flatCurve = new FlatBSplineCurveGeometry(device, curveVertices, indices, RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, flatCurve.Handle);
    }

    [Fact]
    public void CurveGeometry_NormalOrientedHermite_CreatesSuccessfully()
    {
        using var device = new Device();

        var curveVertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 1, 0), 0.15f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f)
        };

        var indices = new uint[] { 0 };

        // === README code (VERBATIM) ===
        using var orientedCurve = new NormalOrientedHermiteCurveGeometry(device, curveVertices, indices, RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, orientedCurve.Handle);
    }

    // ===== Section 4: Point Geometry =====

    [Fact]
    public void PointGeometry_Sphere_CreatesSuccessfully()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var spherePoints = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(1, 1, 1), 0.3f)
        };
        using var spheres = new SpherePointGeometry(device, spherePoints, RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, spheres.Handle);
    }

    [Fact]
    public void PointGeometry_Disc_CreatesSuccessfully()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var discPoints = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };
        using var discs = new DiscPointGeometry(device, discPoints, RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, discs.Handle);
    }

    [Fact]
    public void PointGeometry_OrientedDisc_CreatesSuccessfully()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var orientedPoints = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 0.5f, new V3f(0, 0, 1))
        };
        using var orientedDiscs = new OrientedDiscPointGeometry(device, orientedPoints, RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, orientedDiscs.Handle);
    }

    // ===== Section 5: Subdivision Surfaces =====

    [Fact]
    public void SubdivisionSurfaces_WithCreases_CreatesSuccessfully()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0), new V3f(1, -1, 0),
            new V3f(1, 1, 0), new V3f(-1, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(
            device, vertices, indices, faces,
            RTCBuildQuality.High,
            RTCSubdivisionMode.SmoothBoundary,
            tessellationRate: 4.0f
        );

        var edgeIndices = new uint[] { 0, 1 };
        var edgeWeights = new float[] { 5.0f };
        subdiv.SetEdgeCreases(device, edgeIndices, edgeWeights);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }

    // ===== Section 6: Grid Meshes =====

    [Fact]
    public void GridMeshes_HeightField_CreatesSuccessfully()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        uint width = 10;
        uint height = 10;

        var vertices = new V3f[width * height];
        for (uint y = 0; y < height; y++)
        {
            for (uint x = 0; x < width; x++)
            {
                float z = (float)(Math.Sin(x * 0.5f) * Math.Cos(y * 0.5f));
                vertices[y * width + x] = new V3f(x, y, z);
            }
        }

        // README doesn't show RTCGrid creation, but it's required
        var grids = new[] { new RTCGrid { startVertexID = 0, stride = (ushort)width, width = (ushort)width, height = (ushort)height } };
        using var grid = new GridGeometry(device, vertices, grids, RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, grid.Handle);
    }

    // ===== Section 7: User-Defined Geometry =====

    [Fact]
    public unsafe void UserGeometry_WithBoundsAndIntersect_WorksCorrectly()
    {
        using var device = new Device();

        // === README code with corrected field names for actual API ===
        RTCBoundsFunction boundsFunc = (args) =>
        {
            var bounds = args->bounds_o;
            bounds->lower = new V3f(-1.0f, -1.0f, -1.0f);
            bounds->upper = new V3f(1.0f, 1.0f, 1.0f);
        };

        RTCIntersectFunction intersectFunc = (RTCIntersectFunctionNArguments* args) =>
        {
            // Unit sphere intersection at origin
            var rayhit = (RTCRayHit*)args->rayhit;
            var ray = &rayhit->ray;

            var ox = ray->org.X;
            var oy = ray->org.Y;
            var oz = ray->org.Z;
            var dx = ray->dir.X;
            var dy = ray->dir.Y;
            var dz = ray->dir.Z;

            // Ray-sphere intersection: |O + tD|^2 = 1
            var a = dx * dx + dy * dy + dz * dz;
            var b = 2.0f * (ox * dx + oy * dy + oz * dz);
            var c = ox * ox + oy * oy + oz * oz - 1.0f;

            var discriminant = b * b - 4.0f * a * c;
            if (discriminant >= 0)
            {
                var t = (-b - (float)Math.Sqrt(discriminant)) / (2.0f * a);
                if (t >= ray->tnear && t <= ray->tfar)
                {
                    var hit = &rayhit->hit;
                    ray->tfar = t;
                    hit->geomID = args->geomID;
                    hit->primID = 0;
                    hit->uv.X = 0.5f;
                    hit->uv.Y = 0.5f;
                    hit->Ng.X = ox + t * dx;
                    hit->Ng.Y = oy + t * dy;
                    hit->Ng.Z = oz + t * dz;
                }
            }
        };

        using var userGeom = new UserGeometry(
            device,
            primitiveCount: 1,
            boundsFunc: boundsFunc,
            intersectFunc: intersectFunc,
            quality: RTCBuildQuality.High
        );
        // === README code ends ===

        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
        scene.AttachGeometry(userGeom);
        scene.Commit();

        // Test ray from (0,0,-3) along +Z should hit sphere at t≈2
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, -3),
            rayDirection: new V3f(0, 0, 1),
            ref hit
        );

        Assert.True(intersected);
        Assert.InRange(hit.T, 1.9f, 2.1f);

        // Keep callbacks alive
        GC.KeepAlive(boundsFunc);
        GC.KeepAlive(intersectFunc);
    }

    // ===== Section 8: Zero-Copy Span API =====

    [Fact]
    public void ZeroCopySpan_TriangleGeometry_CreatesSuccessfully()
    {
        using var device = new Device();

        // === README shows Span API, but implementation uses arrays ===
        Span<V3f> vertices = stackalloc V3f[3]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        Span<int> indices = stackalloc int[3] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices.ToArray(), indices.ToArray(), RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Fact]
    public void ZeroCopySpan_CurveGeometry_CreatesSuccessfully()
    {
        using var device = new Device();

        // === README shows Span API, but implementation uses arrays ===
        Span<CurveVertex> curveVerts = stackalloc CurveVertex[2]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 0, 0), 0.1f)
        };
        Span<uint> curveIndices = stackalloc uint[1] { 0 };
        using var curve = new RoundLinearCurveGeometry(device, curveVerts.ToArray(), curveIndices.ToArray(), RTCBuildQuality.High);
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, curve.Handle);
    }

    // ===== Section 9: Motion Blur =====

    [Fact]
    public void MotionBlur_TwoStep_InterpolatesCorrectly()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var verticesT0 = new V3f[]
        {
            new V3f(-1, -1, 2),
            new V3f(1, -1, 2),
            new V3f(0, 1, 2)
        };

        var verticesT1 = new V3f[]
        {
            new V3f(-1, -1, 4),
            new V3f(1, -1, 4),
            new V3f(0, 1, 4)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            RTCBuildQuality.High);

        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 0),
            rayDirection: new V3f(0, 0, 1),
            ref hit,
            minT: 0.0f,
            maxT: float.MaxValue,
            time: 0.5f
        );
        // === README code ends ===

        Assert.True(intersected);
        Assert.InRange(hit.T, 2.9f, 3.1f); // At t=0.5, triangle at Z=3 (halfway between 2 and 4)

        // Test at t=0
        hit = new RayHit();
        intersected = scene.Intersect(new V3f(0, 0, 0), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue, time: 0.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 1.9f, 2.1f);

        // Test at t=1.0
        hit = new RayHit();
        intersected = scene.Intersect(new V3f(0, 0, 0), new V3f(0, 0, 1), ref hit, 0.0f, float.MaxValue, time: 1.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 3.9f, 4.1f);
    }

    [Fact]
    public void MotionBlur_MultiStep_InterpolatesCorrectly()
    {
        using var device = new Device();

        // === README code with complete implementation ===
        var verticesT0 = new V3f[]
        {
            new V3f(-1, -1, 2),
            new V3f(1, -1, 2),
            new V3f(0, 1, 2)
        };

        var verticesT1 = new V3f[]
        {
            new V3f(-1, -1, 4),
            new V3f(1, -1, 4),
            new V3f(0, 1, 4)
        };

        var verticesT2 = new V3f[]
        {
            new V3f(-1, -1, 6),
            new V3f(1, -1, 6),
            new V3f(0, 1, 6)
        };

        var verticesT3 = new V3f[]
        {
            new V3f(-1, -1, 8),
            new V3f(1, -1, 8),
            new V3f(0, 1, 8)
        };

        var indices = new int[] { 0, 1, 2 };

        var timeSteps = new ReadOnlyMemory<V3f>[]
        {
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<V3f>(verticesT2),
            new ReadOnlyMemory<V3f>(verticesT3)
        };
        using var multiStepGeom = new MotionBlurGeometry(device, timeSteps, new ReadOnlyMemory<int>(indices), RTCBuildQuality.High);
        // === README code ends ===

        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
        scene.AttachGeometry(multiStepGeom);
        scene.Commit();

        var rayOrigin = new V3f(0, 0, 0);
        var rayDirection = new V3f(0, 0, 1);

        // Test time interpolation
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time: 0.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 1.9f, 2.1f); // Z=2 at t=0

        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time: 0.33f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 3.9f, 4.1f); // Z≈4 at t=0.33

        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time: 0.67f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 5.9f, 6.1f); // Z≈6 at t=0.67

        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time: 1.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 7.9f, 8.1f); // Z=8 at t=1.0
    }

    [Fact]
    public void MotionBlur_TimeRange_LimitsVisibility()
    {
        using var device = new Device();

        var verticesT0 = new V3f[]
        {
            new V3f(-1, -1, 2),
            new V3f(1, -1, 2),
            new V3f(0, 1, 2)
        };

        var verticesT1 = new V3f[]
        {
            new V3f(-1, -1, 4),
            new V3f(1, -1, 4),
            new V3f(0, 1, 4)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            RTCBuildQuality.High);

        // === README code (VERBATIM) ===
        geom.SetTimeRange(0.2f, 0.8f);
        geom.Commit();
        // === README code ends ===

        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        var rayOrigin = new V3f(0, 0, 0);
        var rayDirection = new V3f(0, 0, 1);

        // Outside time range (t=0.1) should miss
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time: 0.1f);
        Assert.False(intersected);

        // Inside time range (t=0.5) should hit
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time: 0.5f);
        Assert.True(intersected);

        // Outside time range (t=0.9) should miss
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time: 0.9f);
        Assert.False(intersected);
    }

    // ===== Section 10: Dynamic Geometry Updates =====

    [Fact]
    public void DynamicUpdates_UpdateVertices_ModifiesGeometry()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var vertices = new V3f[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: true);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var newVertices = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1) };
        geometry.UpdateVertices(new ReadOnlyMemory<V3f>(newVertices));
        geometry.UpdateBuffer(RTCBufferType.Vertex);
        geometry.Commit();
        scene.Commit();

        Span<V3f> updatedVertices = stackalloc V3f[3]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(0, 1, 2)
        };
        geometry.UpdateVertices(updatedVertices);
        geometry.UpdateBuffer(RTCBufferType.Vertex);
        geometry.Commit();
        scene.Commit();
        // === README code ends ===

        // Verify final position at Z=2
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.25f, 0.25f, 0), new V3f(0, 0, 1), ref hit);
        Assert.True(intersected);
        Assert.InRange(hit.T, 1.9f, 2.1f);
    }

    [Fact]
    public void DynamicUpdates_PointerAccess_ModifiesGeometry()
    {
        using var device = new Device();

        var vertices = new V3f[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: true);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // === README code (VERBATIM) ===
        unsafe
        {
            V3f* vertexPtr = geometry.GetVertexDataPointer();
            vertexPtr[0] = new V3f(0, 0, 3);
            vertexPtr[1] = new V3f(1, 0, 3);
            vertexPtr[2] = new V3f(0, 1, 3);
            geometry.Commit();
            scene.Commit();
        }
        // === README code ends ===

        // Verify final position at Z=3
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.25f, 0.25f, 0), new V3f(0, 0, 1), ref hit);
        Assert.True(intersected);
        Assert.InRange(hit.T, 2.9f, 3.1f);
    }

    // ===== Section 11: Async Commit =====

    [Fact]
    public void AsyncCommit_WithBackgroundWork_Succeeds()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        var vertices = new V3f[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var indices = new int[] { 0, 1, 2 };
        var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

        // === README code with working implementation ===
        scene.AttachGeometry(geometry);

        scene.CommitAsync();

        // Do other work here while BVH builds
        bool workDone = false;
        Thread.Sleep(10); // Simulate work
        workDone = true;

        scene.CommitAsyncWait();
        // === README code ends ===

        Assert.True(workDone);

        // Verify scene is queryable
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.25f, 0.25f, 1), new V3f(0, 0, -1), ref hit);
        Assert.True(intersected);
    }

    // ===== Section 12: Half-Edge Topology =====

    [Fact]
    public void HalfEdgeTopology_EnumerateFaceEdges_Succeeds()
    {
        using var device = new Device();

        // === README code (VERBATIM) ===
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0), new V3f(1, -1, 0),
            new V3f(1, 1, 0), new V3f(-1, 1, 0)
        };
        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            RTCBuildQuality.High, RTCSubdivisionMode.SmoothBoundary, 4.0f);

        var topology = subdiv.GetTopology();

        foreach (var edgeID in topology.EnumerateFaceHalfEdges(faceID: 0))
        {
            var oppositeEdge = topology.GetOppositeHalfEdge(edgeID);
            if (oppositeEdge != uint.MaxValue)
            {
                var neighborFace = topology.GetFace(oppositeEdge);
            }
            else
            {
                // Boundary edge (no neighbor)
            }
        }

        var firstEdge = topology.GetFirstHalfEdge(faceID: 0);
        var nextEdge = topology.GetNextHalfEdge(firstEdge);
        var prevEdge = topology.GetPreviousHalfEdge(firstEdge);
        // === README code ends ===

        Assert.True(firstEdge != uint.MaxValue);
        Assert.True(nextEdge != uint.MaxValue);
        Assert.True(prevEdge != uint.MaxValue);
    }

    // ===== Section 13: Displacement Mapping =====

    [Fact]
    public unsafe void DisplacementMapping_ProceduralHeight_ModifiesGeometry()
    {
        using var device = new Device();

        // === README code (field names corrected for actual API) ===
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0), new V3f(1, -1, 0),
            new V3f(1, 1, 0), new V3f(-1, 1, 0)
        };
        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            RTCBuildQuality.High, RTCSubdivisionMode.SmoothBoundary, 4.0f);

        RTCDisplacementFunctionN displacementFunc = (args) =>
        {
            var N = (int)args->N;
            var u = args->u;
            var v = args->v;
            var Ng_x = args->Ng_x;
            var Ng_y = args->Ng_y;
            var Ng_z = args->Ng_z;
            var posX = args->P_x;  // Actual API uses P_x, not x
            var posY = args->P_y;  // Actual API uses P_y, not y
            var posZ = args->P_z;  // Actual API uses P_z, not z

            for (int i = 0; i < N; i++)
            {
                float height = (float)(Math.Sin(u[i] * 5.0f) * Math.Cos(v[i] * 5.0f)) * 0.2f;

                posX[i] += Ng_x[i] * height;
                posY[i] += Ng_y[i] * height;
                posZ[i] += Ng_z[i] * height;
            }
        };

        subdiv.SetDisplacementFunction(displacementFunc);
        subdiv.Commit();
        // === README code ends ===

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);

        GC.KeepAlive(displacementFunc);
    }
}
