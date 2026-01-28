using Aardvark.Base;
using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples.Examples;

/// <summary>
/// Example 1: Basic Ray Tracing
///
/// Demonstrates fundamental ray tracing concepts:
/// - Creating a Device and Scene
/// - Building triangle geometry (spheres approximated with triangles)
/// - Single ray intersection queries
/// - Understanding RayHit results
/// - Occlusion testing for shadows
/// - ASCII art visualization
/// - Complete scene lifecycle
///
/// This example creates a simple scene with spheres (approximated as icospheres)
/// and a ground plane, then casts rays in a grid pattern from above to create
/// an ASCII art visualization.
/// </summary>
public class BasicRayTracing : ExampleBase
{
    public override string Title => "Basic Ray Tracing";
    public override string Description => "Single ray intersection and occlusion queries";
    public override string Category => "Ray Tracing Queries";
    public override int OrderIndex => 1;
    public override int ApproximateLineCount => 280;

    protected override void Execute()
    {
        PrintSection("1. Scene Setup");
        Print("Creating a scene with spheres and a ground plane...");

        // Scene builds a BVH (bounding volume hierarchy) for O(log n) ray queries instead of O(n)
        // Medium quality balances build time vs traversal speed; static scenes use a more optimized BVH
        using var scene = new Scene(Device!, RTCBuildQuality.Medium, dynamic: false);

        // Embree only supports triangles/quads natively; spheres must be tessellated
        // Icosphere subdivision yields more uniform triangles than UV-sphere
        Print($"  - Creating sphere 1 at (0, 0, 0) with radius 1.0");
        var sphere1 = CreateIcosphere(Device!, center: new V3f(0, 0, 0), radius: 1.0f, subdivisions: 2);
        var sphere1Id = scene.AttachGeometry(sphere1);

        Print($"  - Creating sphere 2 at (3, 0, 0.5) with radius 0.8");
        var sphere2 = CreateIcosphere(Device!, center: new V3f(3, 0, 0.5f), radius: 0.8f, subdivisions: 2);
        var sphere2Id = scene.AttachGeometry(sphere2);

        Print($"  - Creating sphere 3 at (-2.5, 1.5, -0.3) with radius 0.6");
        var sphere3 = CreateIcosphere(Device!, center: new V3f(-2.5f, 1.5f, -0.3f), radius: 0.6f, subdivisions: 2);
        var sphere3Id = scene.AttachGeometry(sphere3);

        // Create a ground plane (two triangles forming a quad)
        Print($"  - Creating ground plane at z = -1.5");
        var groundPlane = CreateGroundPlane(Device!, z: -1.5f, size: 10.0f);
        var groundId = scene.AttachGeometry(groundPlane);

        // Commit triggers BVH construction; must be called after any geometry change, before queries
        Print("  - Committing scene (building acceleration structure)...");
        scene.Commit();
        Print("  [OK] Scene ready for ray queries");

        PrintSection("2. Basic Ray Intersection");
        Print("Shooting a single ray straight down from above...");

        // Ray direction doesn't need normalization; Embree handles it internally
        var rayOrigin = new V3f(0, 0, 10);
        var rayDirection = new V3f(0, 0, -1);
        var hit = new RayHit();

        // Intersect finds the closest hit along the ray
        // hit.T = distance, hit.Normal = unnormalized surface normal (object space)
        // hit.PrimitiveId = triangle index, hit.GeometryId = which geometry in the scene
        bool didHit = scene.Intersect(rayOrigin, rayDirection, ref hit);

        if (didHit)
        {
            Print($"  [OK] HIT at distance {hit.T:F2}");
            Print($"    Ray: origin=({rayOrigin.X:F1}, {rayOrigin.Y:F1}, {rayOrigin.Z:F1}), " +
                  $"dir=({rayDirection.X:F1}, {rayDirection.Y:F1}, {rayDirection.Z:F1})");
            Print($"    Geometry ID: {hit.GeometryId} (0-2=spheres, 3=ground)");
            Print($"    Triangle ID: {hit.PrimitiveId}");
            Print($"    Hit point: ({rayOrigin.X + rayDirection.X * hit.T:F2}, " +
                  $"{rayOrigin.Y + rayDirection.Y * hit.T:F2}, " +
                  $"{rayOrigin.Z + rayDirection.Z * hit.T:F2})");
            Print($"    Normal: ({hit.Normal.X:F3}, {hit.Normal.Y:F3}, {hit.Normal.Z:F3})");
            Print($"    Barycentric: u={hit.Coord.X:F2}, v={hit.Coord.Y:F2}");
        }
        else
        {
            Print("  [X] No hit (ray missed all geometry)");
        }

        PrintSection("3. Shadow Rays (Occlusion Queries)");
        Print("Testing if there's geometry between two points...");

        // Occluded exits on first hit (no distance/normal needed); faster than Intersect for shadows
        var lightPos = new V3f(5, 5, 10);
        var surfacePoint = new V3f(0, 0, 1);

        var shadowRayDir = lightPos - surfacePoint;
        var shadowRayLength = shadowRayDir.Length;
        shadowRayDir = shadowRayDir.Normalized;

        // minT=0.001 avoids self-intersection; maxT=shadowRayLength stops at the light
        bool isInShadow = scene.Occluded(surfacePoint, shadowRayDir, minT: 0.001f, maxT: shadowRayLength);

        Print($"  Light position: ({lightPos.X:F1}, {lightPos.Y:F1}, {lightPos.Z:F1})");
        Print($"  Surface point: ({surfacePoint.X:F1}, {surfacePoint.Y:F1}, {surfacePoint.Z:F1})");
        Print($"  Shadow ray length: {shadowRayLength:F2}");
        Print(isInShadow
            ? "  [OK] OCCLUDED - Point is in shadow"
            : "  [X] NOT OCCLUDED - Point is lit");

        PrintSection("4. Grid Ray Casting (ASCII Visualization)");
        Print("Casting rays in a grid pattern from above to create an ASCII visualization...");
        Print("Legend: '#' = hit, '.' = miss\n");

        const int width = 60;
        const int height = 20;
        const float viewWidth = 8.0f;
        const float viewHeight = 5.0f;
        const float viewZ = 10.0f;

        var hits = 0;
        var misses = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var worldX = (x / (float)width - 0.5f) * viewWidth;
                var worldY = (y / (float)height - 0.5f) * viewHeight;

                var origin = new V3f(worldX, worldY, viewZ);
                var direction = new V3f(0, 0, -1);

                var testHit = new RayHit();
                if (scene.Intersect(origin, direction, ref testHit))
                {
                    // Depth-based shading: closer hits = darker characters
                    var depth = testHit.T;
                    char c = depth < 8 ? '#' : (depth < 10 ? '+' : '.');
                    Console.Write(c);
                    hits++;
                }
                else
                {
                    Console.Write('.');
                    misses++;
                }
            }
            Console.WriteLine();
        }

        var totalRays = width * height;
        Print($"\n  Total rays cast: {totalRays}");
        Print($"  Hits: {hits} ({100.0 * hits / totalRays:F1}%)");
        Print($"  Misses: {misses} ({100.0 * misses / totalRays:F1}%)");

        PrintSection("5. Performance Test");
        Print("Casting 100,000 rays to measure throughput...");

        Timer.Start("Ray casting");
        const int perfTestRays = 100_000;
        var random = new Random(42); // Seeded for reproducibility

        for (int i = 0; i < perfTestRays; i++)
        {
            var rx = (float)(random.NextDouble() - 0.5) * 10;
            var ry = (float)(random.NextDouble() - 0.5) * 10;
            var rz = 10.0f + (float)random.NextDouble() * 5;

            var rOrigin = new V3f(rx, ry, rz);
            var rDir = new V3f(0, 0, -1);
            var rHit = new RayHit();

            scene.Intersect(rOrigin, rDir, ref rHit);
        }

        var elapsed = Timer.Stop();
        PrintPerformance("  Ray tracing performance", perfTestRays, elapsed);

        PrintSection("Summary");
        Print("[OK] Created scene with multiple geometries");
        Print("[OK] Performed intersection queries (Scene.Intersect)");
        Print("[OK] Tested occlusion (Scene.Occluded)");
        Print("[OK] Generated ASCII art visualization");
        Print("[OK] Measured ray tracing performance");
        Print("");
        Print("Key Embree Concepts Demonstrated:");
        Print("  - Device: The ray tracing context");
        Print("  - Scene: Container for geometry with acceleration structure");
        Print("  - TriangleGeometry: Triangle mesh representation");
        Print("  - Ray: Origin + Direction + tnear/tfar range");
        Print("  - RayHit: Intersection result with distance, normal, coords");
        Print("  - Commit: Building the acceleration structure");
        Print("  - Intersect: Find closest hit");
        Print("  - Occluded: Fast shadow ray test");
    }

    /// <summary>
    /// Icosphere yields more uniform triangles than UV-sphere (no pole singularities).
    /// </summary>
    private static TriangleGeometry CreateIcosphere(Device device, V3f center, float radius, int subdivisions)
    {
        // Golden ratio places icosahedron vertices on a sphere
        var t = (1.0f + MathF.Sqrt(5.0f)) / 2.0f;

        var baseVertices = new[]
        {
            new V3f(-1,  t,  0), new V3f( 1,  t,  0), new V3f(-1, -t,  0), new V3f( 1, -t,  0),
            new V3f( 0, -1,  t), new V3f( 0,  1,  t), new V3f( 0, -1, -t), new V3f( 0,  1, -t),
            new V3f( t,  0, -1), new V3f( t,  0,  1), new V3f(-t,  0, -1), new V3f(-t,  0,  1)
        };

        var baseFaces = new[]
        {
            0, 11, 5,   0, 5, 1,    0, 1, 7,    0, 7, 10,   0, 10, 11,
            1, 5, 9,    5, 11, 4,   11, 10, 2,  10, 7, 6,   7, 1, 8,
            3, 9, 4,    3, 4, 2,    3, 2, 6,    3, 6, 8,    3, 8, 9,
            4, 9, 5,    2, 4, 11,   6, 2, 10,   8, 6, 7,    9, 8, 1
        };

        var vertices = new V3f[baseVertices.Length];
        for (int i = 0; i < baseVertices.Length; i++)
        {
            vertices[i] = baseVertices[i].Normalized * radius + center;
        }

        var indices = new int[baseFaces.Length];
        Array.Copy(baseFaces, indices, baseFaces.Length);

        return new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);
    }

    private static TriangleGeometry CreateGroundPlane(Device device, float z, float size)
    {
        var halfSize = size / 2;

        var vertices = new[]
        {
            new V3f(-halfSize, -halfSize, z),
            new V3f( halfSize, -halfSize, z),
            new V3f( halfSize,  halfSize, z),
            new V3f(-halfSize,  halfSize, z)
        };

        // Quad as two triangles sharing edge (0,2)
        var indices = new[] { 0, 1, 2, 0, 2, 3 };

        return new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);
    }
}
