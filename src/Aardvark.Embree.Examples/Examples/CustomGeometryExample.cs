using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Aardvark.Embree;
using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples.Examples;

/// <summary>
/// Custom Geometry Example: Procedural Geometry and User-Defined Intersection
///
/// Demonstrates advanced custom geometry techniques:
/// - Procedural sphere generation using user geometry
/// - Custom bounds function implementation
/// - Custom ray-sphere intersection routine
/// - Mathematical implicit surfaces (spheres)
/// - Efficient bounding box calculations
/// - Practical applications in terrain and procedural content
/// </summary>
public class CustomGeometryExample : ExampleBase
{
    public override string Title => "Custom Geometry & Procedural Generation";
    public override string Description => "User-defined geometry with custom intersection routines";
    public override string Category => "Advanced Features";
    public override int OrderIndex => 9;
    public override int ApproximateLineCount => 350;

    // Sphere data for procedural generation
    private class SphereData
    {
        public V3f[] Centers { get; set; }
        public float[] Radii { get; set; }
        public int Count { get; set; }

        public SphereData(int count)
        {
            Count = count;
            Centers = new V3f[count];
            Radii = new float[count];
        }
    }

    protected override void Execute()
    {
        PrintSection("Overview");
        Print("This example demonstrates custom/user-defined geometry in Embree.");
        Print("We'll create procedural spheres using custom intersection routines.");
        Print("");
        Print("Key concepts:");
        Print("  1. RTCBoundsFunction - Calculate bounding boxes for primitives");
        Print("  2. RTCIntersectFunction - Custom ray-primitive intersection");
        Print("  3. Procedural geometry generation (no vertex/index buffers)");
        Print("  4. Analytical ray-sphere intersection mathematics");
        Print("");

        // Demonstrate procedural sphere generation
        DemonstrateProceduralSpheres();

        // Demonstrate mathematical explanations
        PrintMathematicalBackground();
    }

    /// <summary>
    /// Demonstrates procedural sphere generation with custom intersection.
    ///
    /// Algorithm: Analytical Ray-Sphere Intersection
    /// Given: Ray R(t) = O + t*D where O=origin, D=direction
    /// Sphere: |P - C|² = r² where C=center, r=radius
    ///
    /// Substitute ray equation into sphere equation:
    /// |O + t*D - C|² = r²
    ///
    /// Expand: (O + t*D - C)·(O + t*D - C) = r²
    /// Let L = O - C (vector from sphere center to ray origin)
    /// (L + t*D)·(L + t*D) = r²
    /// L·L + 2t(D·L) + t²(D·D) = r²
    ///
    /// Quadratic form: at² + bt + c = 0
    /// where: a = D·D (usually 1 for normalized direction)
    ///        b = 2(D·L)
    ///        c = L·L - r²
    ///
    /// Discriminant: Δ = b² - 4ac
    /// If Δ < 0: no intersection
    /// If Δ ≥ 0: t = (-b ± √Δ) / 2a (choose smaller positive t)
    /// </summary>
    private void DemonstrateProceduralSpheres()
    {
        PrintSection("Procedural Sphere Generation");
        Print("Creating 8 procedural spheres in a 2×2×2 grid pattern...");
        Print("");

        // Generate procedural sphere data
        var sphereData = GenerateProceduralSphereGrid();
        Print($"Generated {sphereData.Count} spheres:");
        for (int i = 0; i < sphereData.Count; i++)
        {
            var c = sphereData.Centers[i];
            var r = sphereData.Radii[i];
            Print($"  Sphere {i}: center=({c.X:F1}, {c.Y:F1}, {c.Z:F1}), radius={r:F2}");
        }
        Print("");

        // Create user geometry with custom callbacks
        Print("Setting up custom geometry callbacks:");
        Print("  → RTCBoundsFunction: Compute axis-aligned bounding box");
        Print("  → RTCIntersectFunction: Analytical ray-sphere intersection");
        Print("  → RTCOccludedFunction: Fast shadow ray testing");
        Print("");

        unsafe
        {
            using var userGeom = new UserGeometry(
                Device!,
                (uint)sphereData.Count,
                BoundsCallback,
                IntersectCallback,
                OccludedCallback,
                sphereData);

        // Build scene
        using var scene = new Scene(Device!, RTCBuildQuality.High, false);
        scene.AttachGeometry(userGeom);
        scene.Commit();

        Print("Scene committed. Testing ray intersections...");
        Print("");

            // Test ray intersections
            TestProceduralGeometry(scene, sphereData);
        }
    }

    /// <summary>
    /// Generates a 2×2×2 grid of spheres with varying radii.
    /// This demonstrates procedural generation for applications like:
    /// - Particle systems
    /// - Molecular visualization
    /// - Procedural planets/asteroids
    /// - Bubble simulations
    /// </summary>
    private static SphereData GenerateProceduralSphereGrid()
    {
        const int gridSize = 2;
        const float spacing = 3.0f;
        const float baseRadius = 0.8f;

        int totalSpheres = gridSize * gridSize * gridSize;
        var data = new SphereData(totalSpheres);

        int index = 0;
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Position in grid
                    data.Centers[index] = new V3f(
                        x * spacing - spacing * (gridSize - 1) * 0.5f,
                        y * spacing - spacing * (gridSize - 1) * 0.5f,
                        z * spacing - spacing * (gridSize - 1) * 0.5f
                    );

                    // Vary radius slightly for visual interest
                    data.Radii[index] = baseRadius + (float)Math.Sin(index) * 0.2f;
                    index++;
                }
            }
        }

        return data;
    }

    /// <summary>
    /// Bounds callback: Computes axis-aligned bounding box for a sphere primitive.
    /// This is called by Embree during BVH construction.
    ///
    /// For a sphere at center C with radius r:
    /// AABB.min = C - (r, r, r)
    /// AABB.max = C + (r, r, r)
    /// </summary>
    private static unsafe void BoundsCallback(RTCBoundsFunctionArguments* args)
    {
        // GCHandle converts between managed object and IntPtr; Embree passes our data through geometryUserPtr
        var handle = GCHandle.FromIntPtr(args->geometryUserPtr);
        var sphereData = (SphereData)handle.Target!;

        uint primID = args->primID;
        var center = sphereData.Centers[primID];
        var radius = sphereData.Radii[primID];

        // Tight AABB improves BVH culling; loose bounds waste traversal
        args->bounds_o->lower = new V3f(
            center.X - radius,
            center.Y - radius,
            center.Z - radius
        );
        args->bounds_o->upper = new V3f(
            center.X + radius,
            center.Y + radius,
            center.Z + radius
        );
    }

    /// <summary>
    /// Intersect callback: Analytical ray-sphere intersection.
    ///
    /// Mathematical derivation (see DemonstrateProceduralSpheres for full derivation):
    /// 1. Form vector L from sphere center to ray origin
    /// 2. Compute quadratic coefficients: a = D·D, b = 2(D·L), c = L·L - r²
    /// 3. Calculate discriminant: Δ = b² - 4ac
    /// 4. If Δ ≥ 0, solve for t: t = (-b - √Δ) / 2a
    /// 5. Check t ∈ [tnear, tfar] and update hit information
    ///
    /// Normal calculation: N = (HitPoint - Center) / radius
    /// UV coordinates: Using spherical coordinates (θ, φ)
    /// </summary>
    private static unsafe void IntersectCallback(RTCIntersectFunctionNArguments* args)
    {
        var handle = GCHandle.FromIntPtr(args->geometryUserPtr);
        var sphereData = (SphereData)handle.Target!;

        uint primID = args->primID;
        var center = sphereData.Centers[primID];
        var radius = sphereData.Radii[primID];

        // N=1: single-ray callback (vs N=4/8/16 for SIMD packets)
        var rayHit = (RTCRayHit*)args->rayhit;
        var ray = &rayHit->ray;
        var hit = &rayHit->hit;

        // L = vector from sphere center to ray origin (O - C)
        // This translation moves the problem to sphere-centered coordinates
        V3f L = new V3f(
            ray->org.X - center.X,
            ray->org.Y - center.Y,
            ray->org.Z - center.Z
        );

        // Quadratic coefficients from ray-sphere equation: at² + bt + c = 0
        // a = D·D = |direction|² (usually 1, but we compute for unnormalized rays)
        // b = 2(D·L) = how aligned is ray with vector to center (positive = approaching)
        // c = |L|² - r² = squared distance from origin to center, minus sphere radius²
        float a = ray->dir.X * ray->dir.X + ray->dir.Y * ray->dir.Y + ray->dir.Z * ray->dir.Z;
        float b = 2.0f * (ray->dir.X * L.X + ray->dir.Y * L.Y + ray->dir.Z * L.Z);
        float c = L.X * L.X + L.Y * L.Y + L.Z * L.Z - radius * radius;

        // Discriminant Δ = b² - 4ac determines intersection geometry:
        //   Δ < 0: no real roots → ray passes by sphere without touching
        //   Δ = 0: one root (tangent) → ray grazes sphere surface
        //   Δ > 0: two roots → ray enters and exits sphere
        float discriminant = b * b - 4.0f * a * c;
        if (discriminant < 0.0f)
            return;

        // Take smaller root (-b - √Δ) for entry point; larger root is exit
        float sqrtDisc = (float)Math.Sqrt(discriminant);
        float t = (-b - sqrtDisc) / (2.0f * a);

        // Embree tracks closest hit in tfar; only accept if this hit is nearer
        if (t < ray->tnear || t > ray->tfar)
            return;

        // Update tfar so subsequent BVH nodes can be culled if farther
        ray->tfar = t;
        hit->primID = primID;
        hit->geomID = args->geomID;

        V3f hitPoint = new V3f(
            ray->org.X + t * ray->dir.X,
            ray->org.Y + t * ray->dir.Y,
            ray->org.Z + t * ray->dir.Z
        );

        // Embree expects unnormalized Ng; shading code normalizes later
        hit->Ng = new V3f(
            hitPoint.X - center.X,
            hitPoint.Y - center.Y,
            hitPoint.Z - center.Z
        );

        // Convert sphere normal to spherical UV coordinates:
        // u = longitude: atan2(x, z) measures angle around Y-axis → [0, 1]
        // v = latitude: asin(y) measures angle from equator → [0, 1]
        // atan2 range [-π, π] → divide by 2π; asin range [-π/2, π/2] → divide by π
        V3f normal = hit->Ng / radius;
        float u = (float)(Math.Atan2(normal.X, normal.Z) / (2.0 * Math.PI)) + 0.5f;
        float v = (float)(Math.Asin(normal.Y) / Math.PI) + 0.5f;
        hit->uv = new V2f(u, v);
    }

    /// <summary>
    /// Occlusion callback: Fast shadow ray testing.
    /// Similar to intersection but only checks if ray hits (doesn't compute full hit info).
    /// </summary>
    private static unsafe void OccludedCallback(RTCOccludedFunctionNArguments* args)
    {
        var handle = GCHandle.FromIntPtr(args->geometryUserPtr);
        var sphereData = (SphereData)handle.Target!;

        uint primID = args->primID;
        var center = sphereData.Centers[primID];
        var radius = sphereData.Radii[primID];

        var ray = (RTCRay*)args->ray;

        V3f L = new V3f(
            ray->org.X - center.X,
            ray->org.Y - center.Y,
            ray->org.Z - center.Z
        );

        // Same quadratic as Intersect, but we only need to know IF we hit, not WHERE
        float a = ray->dir.X * ray->dir.X + ray->dir.Y * ray->dir.Y + ray->dir.Z * ray->dir.Z;
        float b = 2.0f * (ray->dir.X * L.X + ray->dir.Y * L.Y + ray->dir.Z * L.Z);
        float c = L.X * L.X + L.Y * L.Y + L.Z * L.Z - radius * radius;

        float discriminant = b * b - 4.0f * a * c;
        if (discriminant < 0.0f)
            return;

        float sqrtDisc = (float)Math.Sqrt(discriminant);
        float t = (-b - sqrtDisc) / (2.0f * a);

        if (t >= ray->tnear && t <= ray->tfar)
        {
            // Mark ray as occluded by setting tfar = -∞ (Embree's occlusion convention)
            // This triggers early BVH traversal exit—no need to find *closest* hit
            // Makes shadow rays much faster: we only care IF blocked, not WHERE
            ray->tfar = float.NegativeInfinity;
        }
    }

    /// <summary>
    /// Tests the procedural geometry with various ray queries.
    /// </summary>
    private void TestProceduralGeometry(Scene scene, SphereData sphereData)
    {
        PrintSection("Ray Intersection Tests");

        // Test 1: Ray aimed at first sphere
        Print("Test 1: Ray aimed at sphere 0");
        var rayOrigin = new V3f(-10.0f, 0.0f, 0.0f);
        var rayDirection = new V3f(1.0f, 0.0f, 0.0f);
        var hit = new RayHit();

        if (scene.Intersect(rayOrigin, rayDirection, ref hit))
        {
            Print($"  [OK] HIT sphere {hit.PrimitiveId} at distance {hit.T:F2}");
            var normal = hit.Normal.Normalized;
            Print($"    Normal: ({normal.X:F3}, {normal.Y:F3}, {normal.Z:F3})");
            Print($"    UV: ({hit.Coord.X:F3}, {hit.Coord.Y:F3})");
        }
        else
        {
            Print("  [X] MISS");
        }
        Print("");

        // Test 2: Ray aimed at sphere from different angle
        Print("Test 2: Diagonal ray through grid");
        rayOrigin = new V3f(-10.0f, -10.0f, -10.0f);
        rayDirection = new V3f(1.0f, 1.0f, 1.0f).Normalized;

        if (scene.Intersect(rayOrigin, rayDirection, ref hit))
        {
            Print($"  [OK] HIT sphere {hit.PrimitiveId} at distance {hit.T:F2}");
            var hitCenter = sphereData.Centers[hit.PrimitiveId];
            Print($"    Sphere center: ({hitCenter.X:F1}, {hitCenter.Y:F1}, {hitCenter.Z:F1})");
        }
        else
        {
            Print("  [X] MISS");
        }
        Print("");

        // Test 3: Occlusion test
        Print("Test 3: Occlusion/shadow ray test");
        rayOrigin = new V3f(-5.0f, 0.0f, 0.0f);
        rayDirection = new V3f(1.0f, 0.0f, 0.0f);
        bool occluded = scene.Occluded(rayOrigin, rayDirection, 0.0f, 100.0f);
        Print($"  Ray occluded: {occluded}");
        Print("");

        // Performance test
        PrintSection("Performance Test");
        Print("Testing 100,000 random rays...");

        var random = new Random(42);
        int hitCount = 0;
        Timer.Start("custom-geometry");

        for (int i = 0; i < 100000; i++)
        {
            var origin = new V3f(
                (float)(random.NextDouble() * 20.0 - 10.0),
                (float)(random.NextDouble() * 20.0 - 10.0),
                (float)(random.NextDouble() * 20.0 - 10.0)
            );
            var dir = new V3f(
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0),
                (float)(random.NextDouble() * 2.0 - 1.0)
            ).Normalized;

            if (scene.Intersect(origin, dir, ref hit))
                hitCount++;
        }

        var elapsed = Timer.Stop();
        PrintPerformance("Custom geometry rays", 100000, elapsed);
        Print($"Hit rate: {hitCount / 1000.0:F1}%");
        Print("");
    }

    /// <summary>
    /// Explains the mathematical background and practical applications.
    /// </summary>
    private static void PrintMathematicalBackground()
    {
        PrintSection("Mathematical Background");
        Print("Implicit Surface Representation:");
        Print("  Sphere: f(x,y,z) = (x-cx)² + (y-cy)² + (z-cz)² - r² = 0");
        Print("  Points where f=0 are on the surface");
        Print("  f<0: inside sphere, f>0: outside sphere");
        Print("");

        Print("Quadratic Solution:");
        Print("  For at² + bt + c = 0, t = (-b ± √(b²-4ac)) / 2a");
        Print("  We take the smaller positive root (entry point)");
        Print("  Discriminant Δ = b²-4ac determines intersection:");
        Print("    Δ < 0: no intersection (ray misses)");
        Print("    Δ = 0: tangent (grazing hit)");
        Print("    Δ > 0: two intersections (entry and exit)");
        Print("");

        PrintSection("Practical Applications");
        Print("Custom geometry enables:");
        Print("  * Procedural terrain generation (heightfields, fractals)");
        Print("  * Implicit surfaces (metaballs, blobby objects)");
        Print("  * Mathematical surfaces (torus, superquadrics)");
        Print("  * Particle systems with analytical shapes");
        Print("  * CSG operations (constructive solid geometry)");
        Print("  * Volumetric primitives (ellipsoids, capsules)");
        Print("  * Infinite procedural geometry (fractal landscapes)");
        Print("");

        PrintSection("Performance Considerations");
        Print("  * BVH accelerates intersection tests (O(log n) vs O(n))");
        Print("  * Tight bounding boxes improve culling efficiency");
        Print("  * Analytical intersection often faster than triangulation");
        Print("  * Occlusion callback can skip expensive calculations");
        Print("  * Memory efficient: no vertex/index buffers needed");
        Print("");

        Print("Next steps:");
        Print("  * Try implementing torus or capsule intersection");
        Print("  * Explore fractal geometry (Mandelbulb, Julia sets)");
        Print("  * Combine with motion blur for animated procedurals");
        Print("  * Use for collision detection in physics simulations");
    }
}
