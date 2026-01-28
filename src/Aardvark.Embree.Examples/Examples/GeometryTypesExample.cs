using System;
using System.Diagnostics;
using Aardvark.Base;
using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples.Examples;

/// <summary>
/// Geometry Types Showcase
///
/// Demonstrates all major geometry types supported by Embree 4:
/// - Triangle meshes (most common, fastest for rigid surfaces)
/// - Quad meshes (efficient for structured grids)
/// - Curve geometries (hair, fur, grass rendering)
/// - Subdivision surfaces (smooth organic shapes)
///
/// Each section includes:
/// - Creation and setup
/// - Best use cases
/// - Performance characteristics
/// - Ray intersection behavior
/// </summary>
public class GeometryTypesExample : ExampleBase
{
    public override string Title => "Geometry Types Showcase";
    public override string Description => "Compare triangle, quad, curve, and subdivision surface geometries";
    public override string Category => "Geometry";
    public override int OrderIndex => 9;
    public override int ApproximateLineCount => 450;

    protected override void Execute()
    {
        PrintSection("1. Triangle Mesh Geometry");
        DemonstrateTriangleGeometry();

        PrintSection("2. Quad Mesh Geometry");
        DemonstrateQuadGeometry();

        PrintSection("3. Curve Geometry");
        DemonstrateCurveGeometry();

        PrintSection("4. Subdivision Surface Geometry");
        DemonstrateSubdivisionGeometry();

        PrintSection("Performance Comparison Summary");
        PerformanceComparison();
    }

    /// <summary>
    /// Triangle meshes: Most common geometry type, optimal for rigid surfaces
    /// </summary>
    private void DemonstrateTriangleGeometry()
    {
        Print("Triangle meshes are the workhorse of ray tracing:");
        Print("  [OK] Fastest intersection tests");
        Print("  [OK] Lowest memory overhead");
        Print("  [OK] Best BVH construction time");
        Print("  [OK] Use case: Rigid objects, terrain, buildings");
        Print("");

        var vertices = new V3f[]
        {
            new(0, 0, 0),      // 0: base corners
            new(1, 0, 0),      // 1
            new(1, 0, 1),      // 2
            new(0, 0, 1),      // 3
            new(0.5f, 1, 0.5f) // 4: apex
        };

        var indices = new int[]
        {
            0, 1, 2,  // base triangle 1
            0, 2, 3,  // base triangle 2
            0, 1, 4,  // side 1
            1, 2, 4,  // side 2
            2, 3, 4,  // side 3
            3, 0, 4   // side 4
        };

        using var scene = new Scene(Device, RTCBuildQuality.High, false);
        using var geometry = new TriangleGeometry(Device, vertices, indices, RTCBuildQuality.High);

        scene.AttachGeometry(geometry);

        var sw = Stopwatch.StartNew();
        scene.Commit();
        sw.Stop();

        Print($"Build time: {sw.Elapsed.TotalMilliseconds:F3}ms for 6 triangles");

        // Test intersections
        var hit = new RayHit();
        var rayOrigin = new V3f(0.5f, 2f, 0.5f);
        var rayDirection = new V3f(0, -1, 0);

        if (scene.Intersect(rayOrigin, rayDirection, ref hit))
        {
            Print($"[OK] Ray hit at distance {hit.T:F3}");
            Print($"  Normal: ({hit.Normal.X:F3}, {hit.Normal.Y:F3}, {hit.Normal.Z:F3})");
            Print($"  Primitive ID: {hit.PrimitiveId}");
        }

        Timer.Start("Triangle intersection test");
        int hitCount = 0;
        const int testRays = 100000;
        var random = new Random(42);

        for (int i = 0; i < testRays; i++)
        {
            var origin = new V3f(
                (float)random.NextDouble(),
                2f,
                (float)random.NextDouble()
            );
            var dir = new V3f(0, -1, 0);

            if (scene.Intersect(origin, dir, ref hit))
                hitCount++;
        }

        var elapsed = Timer.Stop();
        Print($"Performance: {testRays:N0} rays in {elapsed:F2}ms");
        Print($"             {Framework.Timer.FormatThroughput(testRays, elapsed)}");
        Print($"Hit rate: {hitCount * 100.0 / testRays:F1}%");
    }

    /// <summary>
    /// Quad meshes: Efficient for structured grids, internally uses quads (not triangulated)
    /// </summary>
    private void DemonstrateQuadGeometry()
    {
        Print("Quad meshes are optimized for structured data:");
        Print("  [OK] Native quad intersection (no triangulation)");
        Print("  [OK] Better for subdivision surfaces");
        Print("  [OK] More compact than 2 triangles per quad");
        Print("  [OK] Use case: Terrain grids, subdivision base meshes");
        Print("");

        const int gridSize = 3;
        var vertices = new V3f[(gridSize + 1) * (gridSize + 1)];

        for (int y = 0; y <= gridSize; y++)
        {
            for (int x = 0; x <= gridSize; x++)
            {
                int idx = y * (gridSize + 1) + x;
                vertices[idx] = new V3f(
                    x * 0.5f,
                    (float)Math.Sin(x * 0.5) * (float)Math.Cos(y * 0.5) * 0.2f,
                    y * 0.5f
                );
            }
        }

        var quadIndices = new int[gridSize * gridSize * 4];
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int quadIdx = (y * gridSize + x) * 4;
                int baseIdx = y * (gridSize + 1) + x;

                quadIndices[quadIdx + 0] = baseIdx;
                quadIndices[quadIdx + 1] = baseIdx + 1;
                quadIndices[quadIdx + 2] = baseIdx + 1 + (gridSize + 1);
                quadIndices[quadIdx + 3] = baseIdx + (gridSize + 1);
            }
        }

        using var scene = new Scene(Device, RTCBuildQuality.High, false);
        using var geometry = new QuadGeometry(Device, vertices, quadIndices, RTCBuildQuality.High);

        scene.AttachGeometry(geometry);

        var sw = Stopwatch.StartNew();
        scene.Commit();
        sw.Stop();

        Print($"Build time: {sw.Elapsed.TotalMilliseconds:F3}ms for {gridSize * gridSize} quads");

        // Test intersection
        var hit = new RayHit();
        var rayOrigin = new V3f(0.75f, 2f, 0.75f);
        var rayDirection = new V3f(0, -1, 0);

        if (scene.Intersect(rayOrigin, rayDirection, ref hit))
        {
            Print($"[OK] Ray hit quad at distance {hit.T:F3}");
            Print($"  UV coords: ({hit.Coord.X:F3}, {hit.Coord.Y:F3})");
        }

        Timer.Start("Quad intersection test");
        int hitCount = 0;
        const int testRays = 100000;
        var random = new Random(42);

        for (int i = 0; i < testRays; i++)
        {
            var origin = new V3f(
                (float)random.NextDouble() * 1.5f,
                2f,
                (float)random.NextDouble() * 1.5f
            );
            var dir = new V3f(0, -1, 0);

            if (scene.Intersect(origin, dir, ref hit))
                hitCount++;
        }

        var elapsed = Timer.Stop();
        Print($"Performance: {testRays:N0} rays in {elapsed:F2}ms");
        Print($"             {Framework.Timer.FormatThroughput(testRays, elapsed)}");
        Print($"Memory: 1 quad = ~48 bytes vs 2 triangles = ~72 bytes");
    }

    /// <summary>
    /// Curve geometries: Specialized for rendering hair, fur, grass, cables
    /// </summary>
    private void DemonstrateCurveGeometry()
    {
        Print("Curve geometries enable efficient hair/fur rendering:");
        Print("  [OK] Multiple curve types: Bezier, B-spline, Catmull-Rom");
        Print("  [OK] Round (tubes), flat (ribbons), or normal-oriented");
        Print("  [OK] Variable radius along curve");
        Print("  [OK] Use case: Hair, fur, grass, cables, motion trails");
        Print("");

        // Cubic Bezier needs 4 control points; radius tapers from root to tip (like real hair)
        var controlPoints = new CurveVertex[]
        {
            new(new V3f(0, 0, 0), 0.05f),
            new(new V3f(0.2f, 0.3f, 0), 0.04f),
            new(new V3f(0.4f, 0.5f, 0.1f), 0.03f),
            new(new V3f(0.6f, 0.6f, 0.2f), 0.02f),

            new(new V3f(0.6f, 0.6f, 0.2f), 0.02f), // Shared endpoint (C0 continuity)
            new(new V3f(0.8f, 0.65f, 0.25f), 0.015f),
            new(new V3f(1.0f, 0.6f, 0.3f), 0.01f),
            new(new V3f(1.2f, 0.5f, 0.35f), 0.005f)
        };

        // Index points to first control point of each segment
        var indices = new uint[] { 0, 4 };

        using var scene = new Scene(Device, RTCBuildQuality.High, false);
        using var curveGeometry = new RoundBezierCurveGeometry(Device, controlPoints, indices, RTCBuildQuality.High);

        scene.AttachGeometry(curveGeometry);

        var sw = Stopwatch.StartNew();
        scene.Commit();
        sw.Stop();

        Print($"Build time: {sw.Elapsed.TotalMilliseconds:F3}ms for 2 Bezier curve segments");
        Print($"Curve type: Round Bezier (tube-like with circular cross-section)");

        // Test intersection with curve
        var hit = new RayHit();
        var rayOrigin = new V3f(0.6f, 1f, 0.2f);
        var rayDirection = new V3f(0, -1, 0);

        if (scene.Intersect(rayOrigin, rayDirection, ref hit))
        {
            Print($"[OK] Ray hit curve at distance {hit.T:F3}");
            Print($"  Curve parameter u: {hit.Coord.X:F3} (along curve length)");
        }

        Timer.Start("Curve intersection test");
        int hitCount = 0;
        const int testRays = 50000;
        var random = new Random(42);

        for (int i = 0; i < testRays; i++)
        {
            var origin = new V3f(
                (float)random.NextDouble() * 1.2f,
                1f,
                (float)random.NextDouble() * 0.4f
            );
            var dir = new V3f(0, -1, 0);

            if (scene.Intersect(origin, dir, ref hit))
                hitCount++;
        }

        var elapsed = Timer.Stop();
        Print($"Performance: {testRays:N0} rays in {elapsed:F2}ms");
        Print($"             {Framework.Timer.FormatThroughput(testRays, elapsed)}");
        Print($"Note: Curves are ~2-3x slower than triangles but much more memory efficient");
        Print($"      for thin geometry like hair (1 curve vs hundreds of triangles)");
    }

    /// <summary>
    /// Subdivision surfaces: Smooth surfaces from coarse control meshes
    /// </summary>
    private void DemonstrateSubdivisionGeometry()
    {
        Print("Subdivision surfaces create smooth shapes from coarse meshes:");
        Print("  [OK] Catmull-Clark subdivision scheme");
        Print("  [OK] Automatic tessellation at ray intersection time");
        Print("  [OK] No need to store dense mesh");
        Print("  [OK] Support for creases (sharp edges/corners)");
        Print("  [OK] Use case: Organic shapes, characters, smooth surfaces");
        Print("");

        // Coarse cube becomes sphere-like after Catmull-Clark subdivision
        var vertices = new V3f[]
        {
            new(-1, -1, -1), new(1, -1, -1), new(1, -1, 1), new(-1, -1, 1),
            new(-1,  1, -1), new(1,  1, -1), new(1,  1, 1), new(-1,  1, 1)
        };

        var indices = new uint[]
        {
            0, 1, 2, 3,
            4, 7, 6, 5,
            0, 4, 5, 1,
            1, 5, 6, 2,
            2, 6, 7, 3,
            3, 7, 4, 0
        };

        // Tells Embree how to split indices: 6 faces, each with 4 vertices
        var faces = new uint[] { 4, 4, 4, 4, 4, 4 };

        using var scene = new Scene(Device, RTCBuildQuality.High, false);

        // tessellationRate=8 means ~8 triangles per edge; higher = smoother but slower
        using var subdivGeometry = new SubdivisionGeometry(
            Device, vertices, indices, faces,
            RTCBuildQuality.High,
            RTCSubdivisionMode.SmoothBoundary,
            tessellationRate: 8.0f
        );

        scene.AttachGeometry(subdivGeometry);

        var sw = Stopwatch.StartNew();
        scene.Commit();
        sw.Stop();

        Print($"Build time: {sw.Elapsed.TotalMilliseconds:F3}ms");
        Print($"Control mesh: 8 vertices, 6 quad faces");
        Print($"Tessellation: Adaptive (subdivided at intersection time)");
        Print($"Result: Smooth, sphere-like surface");

        // Test intersection
        var hit = new RayHit();
        var rayOrigin = new V3f(0, 0, -5f);
        var rayDirection = new V3f(0, 0, 1);

        if (scene.Intersect(rayOrigin, rayDirection, ref hit))
        {
            Print($"[OK] Ray hit subdivision surface at distance {hit.T:F3}");
            Print($"  Normal: ({hit.Normal.X:F3}, {hit.Normal.Y:F3}, {hit.Normal.Z:F3})");
            Print($"  Surface is smooth (normals interpolated from subdivided mesh)");
        }

        Timer.Start("Subdivision surface test");
        int hitCount = 0;
        const int testRays = 50000;
        var random = new Random(42);

        for (int i = 0; i < testRays; i++)
        {
            var angle = (float)random.NextDouble() * MathF.PI * 2;
            var height = ((float)random.NextDouble() - 0.5f) * 2;
            var origin = new V3f(
                MathF.Cos(angle) * 5,
                height,
                MathF.Sin(angle) * 5
            );
            var dir = -origin.Normalized;

            if (scene.Intersect(origin, dir, ref hit))
                hitCount++;
        }

        var elapsed = Timer.Stop();
        Print($"Performance: {testRays:N0} rays in {elapsed:F2}ms");
        Print($"             {Framework.Timer.FormatThroughput(testRays, elapsed)}");
        Print($"");
        Print($"Memory benefit: Control mesh (8 verts) vs tessellated mesh (~1000+ verts)");
        Print($"Performance: ~3-5x slower than triangles, but saves memory and modeling time");
    }

    /// <summary>
    /// Compare performance across all geometry types
    /// </summary>
    private void PerformanceComparison()
    {
        const int raysPerTest = 100000;
        Print("Ray tracing throughput comparison (higher is better):");
        Print("");

        // Triangle mesh
        var triVertices = new V3f[]
        {
            new(-1, 0, -1), new(1, 0, -1), new(1, 0, 1),
            new(-1, 0, -1), new(1, 0, 1), new(-1, 0, 1)
        };
        var triIndices = new int[] { 0, 1, 2, 3, 4, 5 };

        using (var scene = new Scene(Device, RTCBuildQuality.High, false))
        using (var geom = new TriangleGeometry(Device, triVertices, triIndices, RTCBuildQuality.High))
        {
            scene.AttachGeometry(geom);
            scene.Commit();

            var hit = new RayHit();
            Timer.Start("triangle");
            for (int i = 0; i < raysPerTest; i++)
            {
                scene.Intersect(new V3f(0, 1, 0), new V3f(0, -1, 0), ref hit);
            }
            var elapsed = Timer.Stop();
            Print($"  Triangle mesh:        {Framework.Timer.FormatThroughput(raysPerTest, elapsed),-20} (baseline)");
        }

        // Quad mesh
        var quadVertices = new V3f[] { new(-1, 0, -1), new(1, 0, -1), new(1, 0, 1), new(-1, 0, 1) };
        var quadIndices = new int[] { 0, 1, 2, 3 };

        using (var scene = new Scene(Device, RTCBuildQuality.High, false))
        using (var geom = new QuadGeometry(Device, quadVertices, quadIndices, RTCBuildQuality.High))
        {
            scene.AttachGeometry(geom);
            scene.Commit();

            var hit = new RayHit();
            Timer.Start("quad");
            for (int i = 0; i < raysPerTest; i++)
            {
                scene.Intersect(new V3f(0, 1, 0), new V3f(0, -1, 0), ref hit);
            }
            var elapsed = Timer.Stop();
            Print($"  Quad mesh:            {Framework.Timer.FormatThroughput(raysPerTest, elapsed),-20} (~0.9x triangles)");
        }

        // Curve
        var curveVerts = new CurveVertex[]
        {
            new(new V3f(-1, 0, 0), 0.5f),
            new(new V3f(-0.33f, 0, 0), 0.5f),
            new(new V3f(0.33f, 0, 0), 0.5f),
            new(new V3f(1, 0, 0), 0.5f)
        };
        var curveIndices = new uint[] { 0 };

        using (var scene = new Scene(Device, RTCBuildQuality.High, false))
        using (var geom = new RoundBezierCurveGeometry(Device, curveVerts, curveIndices, RTCBuildQuality.High))
        {
            scene.AttachGeometry(geom);
            scene.Commit();

            var hit = new RayHit();
            Timer.Start("curve");
            for (int i = 0; i < raysPerTest; i++)
            {
                scene.Intersect(new V3f(0, 1, 0), new V3f(0, -1, 0), ref hit);
            }
            var elapsed = Timer.Stop();
            Print($"  Curve (Bezier):       {Framework.Timer.FormatThroughput(raysPerTest, elapsed),-20} (~0.3-0.5x triangles)");
        }

        // Subdivision surface
        var subdivVerts = new V3f[] { new(-1, 0, -1), new(1, 0, -1), new(1, 0, 1), new(-1, 0, 1) };
        var subdivIndices = new uint[] { 0, 1, 2, 3 };
        var subdivFaces = new uint[] { 4 };

        using (var scene = new Scene(Device, RTCBuildQuality.High, false))
        using (var geom = new SubdivisionGeometry(Device, subdivVerts, subdivIndices, subdivFaces, RTCBuildQuality.High))
        {
            scene.AttachGeometry(geom);
            scene.Commit();

            var hit = new RayHit();
            Timer.Start("subdiv");
            for (int i = 0; i < raysPerTest; i++)
            {
                scene.Intersect(new V3f(0, 1, 0), new V3f(0, -1, 0), ref hit);
            }
            var elapsed = Timer.Stop();
            Print($"  Subdivision surface:  {Framework.Timer.FormatThroughput(raysPerTest, elapsed),-20} (~0.2-0.4x triangles)");
        }

        Print("");
        Print("Geometry Type Selection Guide:");
        Print("  * Triangles:    General purpose, best performance, use for rigid objects");
        Print("  * Quads:        Structured grids, subdivision base meshes");
        Print("  * Curves:       Hair/fur (huge memory savings vs triangle approximation)");
        Print("  * Subdivision:  Organic shapes (compact storage, smooth surfaces)");
    }
}
