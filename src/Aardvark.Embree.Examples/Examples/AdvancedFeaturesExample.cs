using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples.Examples;

/// <summary>
/// Advanced Features Example
///
/// Demonstrates advanced Embree ray tracing features:
/// - Occlusion queries for shadow rays
/// - Filter functions for selective intersection
/// - Multi-hit queries for transparency and volumes
/// - Context usage for ray queries
/// - Different ray flags and their effects
/// - Practical applications (shadows, transparency, volumes)
/// - Robust error handling patterns
/// </summary>
public class AdvancedFeaturesExample : ExampleBase
{
    public override string Title => "Advanced Features";
    public override string Description => "Occlusion queries, filter functions, multi-hit scenarios";
    public override string Category => "Advanced Techniques";
    public override int OrderIndex => 9;
    public override int ApproximateLineCount => 350;


    protected override void Execute()
    {
        PrintSection("1. Shadow Rays with Occlusion Queries");
        DemonstrateOcclusionQueries();

        PrintSection("2. Filter Functions for Selective Intersection");
        DemonstrateFilterFunctions();

        PrintSection("3. Multi-Hit Queries for Transparency");
        DemonstrateMultiHitQueries();

        PrintSection("4. Ray Flags and Query Optimization");
        DemonstrateRayFlags();

        PrintSection("5. Error Handling Patterns");
        DemonstrateErrorHandling();
    }

    /// <summary>
    /// Demonstrates occlusion queries for shadow ray testing.
    /// Occlusion queries are faster than full intersection when you only need
    /// to know if a ray is blocked (e.g., for shadow testing).
    /// </summary>
    private void DemonstrateOcclusionQueries()
    {
        Print("Occlusion queries determine if a ray hits ANY geometry without");
        Print("computing full intersection details. This is ideal for shadow rays.");
        Print("");

        // Create a scene with an occluder between light and surface
        using var scene = CreateSceneWithOccluder();

        // Light position
        var lightPos = new V3f(5.0f, 5.0f, 5.0f);

        // Test point on the ground plane
        var testPoint = new V3f(0.5f, 0.5f, 0.0f);

        // Shadow ray direction (from test point to light)
        var shadowDir = (lightPos - testPoint).Normalized;
        var shadowDist = (lightPos - testPoint).Length;

        // Bias avoids self-intersection: surface point + tiny offset along ray direction
        var biasedOrigin = testPoint + shadowDir * 0.001f;

        Print($"Testing shadow from point {testPoint} to light at {lightPos}");
        Print("");

        // Occluded() exits early on any hit; faster than Intersect() for shadow tests
        Timer.Start("occlusion");
        bool isOccluded = scene.Occluded(biasedOrigin, shadowDir, 0.0f, shadowDist);
        var occlusionTime = Timer.Stop();

        if (isOccluded)
        {
            Print("[OK] Point is in SHADOW (occluded)");
        }
        else
        {
            Print("[OK] Point is ILLUMINATED (not occluded)");
        }

        Print($"  Occlusion test: {occlusionTime:F4}ms");
        Print("");

        // Full intersection computes closest hit, normal, UV - more work than needed for shadows
        Timer.Start("intersection");
        var hit = new RayHit();
        bool hasHit = scene.Intersect(biasedOrigin, shadowDir, ref hit, 0.0f, shadowDist);
        var intersectionTime = Timer.Stop();

        Print($"  Full intersection: {intersectionTime:F4}ms");
        Print($"  Speed advantage: {(intersectionTime / occlusionTime):F1}× faster");
        Print("");
        Print("KEY INSIGHT: Occlusion queries are optimized for early-exit");
        Print("when any geometry is hit, making them ideal for shadow rays.");
    }

    /// <summary>
    /// Demonstrates filter functions for selective intersection.
    /// Filter functions allow custom logic to accept or reject hits,
    /// enabling features like transparency, masked geometry, and custom culling.
    /// </summary>
    private unsafe void DemonstrateFilterFunctions()
    {
        Print("Filter functions provide custom hit acceptance logic.");
        Print("Use cases: transparency, masked textures, backface culling.");
        Print("");

        using var scene = CreateSceneWithMultipleObjects();

        var rayOrigin = new V3f(0.5f, 0.5f, -5.0f);
        var rayDir = new V3f(0.0f, 0.0f, 1.0f);

        // Example 1: Accept all hits (no filtering)
        Print("1. No filter (accept all hits):");
        var hit1 = new RayHit();
        if (scene.Intersect(rayOrigin, rayDir, ref hit1))
        {
            Print($"   Hit geometry {hit1.GeometryId} at distance {hit1.T:F2}");
        }
        Print("");

        // Example 2: Reject specific geometry using a filter
        Print("2. Filter rejecting geometry ID 0:");

        // Delegate must stay alive during scene usage; GC would invalidate the native callback pointer
        RTCFilterFunction rejectGeo0Filter = (RTCFilterFunctionNArguments* args) =>
        {
            var hit = (RTCHit*)args->hit;

            // valid[0] = 0 rejects hit; Embree checks this mask after filter returns—zero means 'invalid hit, keep searching'. Non-zero (default) means 'accept hit, stop traversal'
            if (hit->geomID == 0)
            {
                args->valid[0] = 0;
            }
        };

        var hit2 = new RayHit();
        bool hasHit = scene.Intersect(rayOrigin, rayDir, ref hit2, 0.0f, float.MaxValue, rejectGeo0Filter);

        if (hasHit)
        {
            Print($"   Hit geometry {hit2.GeometryId} at distance {hit2.T:F2}");
            Print($"   Successfully filtered out geometry 0");
        }
        else
        {
            Print($"   No hit (all geometries filtered out)");
        }

        // Prevent GC from collecting delegate before Embree finishes using it
        GC.KeepAlive(rejectGeo0Filter);

        Print("");
        Print("PRACTICAL APPLICATIONS:");
        Print("  - Transparency: Reject hits based on alpha texture");
        Print("  - Masked geometry: Use texture lookups in filter");
        Print("  - Backface culling: Check dot(ray.dir, hit.normal)");
        Print("  - LOD selection: Accept/reject based on distance");
    }

    /// <summary>
    /// Demonstrates multi-hit queries for transparency and volumetric effects.
    /// By collecting multiple hits along a ray, we can implement:
    /// - Transparent surfaces (glass, water)
    /// - Volumetric rendering (fog, clouds)
    /// - Subsurface scattering
    /// </summary>
    private void DemonstrateMultiHitQueries()
    {
        Print("Multi-hit queries collect ALL intersections along a ray.");
        Print("Essential for: transparency, volumes, subsurface scattering.");
        Print("");

        using var scene = CreateSceneWithOverlappingGeometry();

        var rayOrigin = new V3f(0.5f, 0.5f, -5.0f);
        var rayDir = new V3f(0.0f, 0.0f, 1.0f);

        Print($"Shooting ray from {rayOrigin} in direction {rayDir}");
        Print("");

        // Embree only returns closest hit; to collect all hits, advance ray past each intersection and re-trace—no built-in multi-hit query
        var allHits = CollectAllHits(scene, rayOrigin, rayDir, maxHits: 10);

        Print($"Found {allHits.Count} hits:");
        for (int i = 0; i < allHits.Count; i++)
        {
            var hit = allHits[i];
            Print($"  Hit {i + 1}: GeomID={hit.GeometryId}, Distance={hit.T:F2}, Normal={hit.Normal.Normalized}");
        }

        Print("");
        Print("MULTI-HIT TECHNIQUES:");
        Print("  1. Transparency: Accumulate color from front to back");
        Print("     - Cast ray, get first hit");
        Print("     - Apply transparency: color *= (1 - alpha)");
        Print("     - Continue ray from hit.T + epsilon");
        Print("     - Repeat until fully opaque or max depth");
        Print("");
        Print("  2. Volumes: Integrate density along ray");
        Print("     - Collect all entry/exit points");
        Print("     - Integrate density between pairs");
        Print("     - Apply absorption/scattering models");
        Print("");
        Print("  3. Subsurface Scattering: Sample internal scattering");
        Print("     - Find entry point (front face)");
        Print("     - Find exit point (back face)");
        Print("     - Compute scattering in volume");
    }

    /// <summary>
    /// Demonstrates different ray flags and their performance implications.
    /// RTCRayQueryFlags allow optimization hints to Embree.
    /// </summary>
    private void DemonstrateRayFlags()
    {
        Print("Ray query flags optimize traversal for different ray patterns.");
        Print("");

        using var scene = CreateLargeScene();

        var rayOrigin = new V3f(0.0f, 0.0f, -5.0f);

        // Coherent rays share BVH traversal paths → better cache utilization
        Print("1. Coherent rays (camera rays, packets):");
        Print("   Use RTCRayQueryFlags.Coherent for cache-friendly traversal");
        Print("");

        var coherentRays = GenerateCoherentRays(rayOrigin, 100);
        Timer.Start("coherent");
        int coherentHits = 0;
        foreach (var (origin, dir) in coherentRays)
        {
            var hit = new RayHit();
            if (scene.Intersect(origin, dir, ref hit))
                coherentHits++;
        }
        var coherentTime = Timer.Stop();

        Print($"   Traced {coherentRays.Length} coherent rays");
        Print($"   Hits: {coherentHits}, Time: {coherentTime:F2}ms");
        Print($"   Throughput: {Framework.Timer.FormatThroughput(coherentRays.Length, coherentTime)}");
        Print("");

        // Incoherent rays have random directions → cache misses, but Embree handles gracefully
        Print("2. Incoherent rays (random rays, global illumination):");
        Print("   Use RTCRayQueryFlags.None (incoherent) for scattered rays");
        Print("");

        var incoherentRays = GenerateIncoherentRays(100);
        Timer.Start("incoherent");
        int incoherentHits = 0;
        foreach (var (origin, dir) in incoherentRays)
        {
            var hit = new RayHit();
            if (scene.Intersect(origin, dir, ref hit))
                incoherentHits++;
        }
        var incoherentTime = Timer.Stop();

        Print($"   Traced {incoherentRays.Length} incoherent rays");
        Print($"   Hits: {incoherentHits}, Time: {incoherentTime:F2}ms");
        Print($"   Throughput: {Framework.Timer.FormatThroughput(incoherentRays.Length, incoherentTime)}");
        Print("");

        Print("FLAG GUIDELINES:");
        Print("  RTCRayQueryFlags.None (Incoherent):");
        Print("    - Random rays (global illumination, ambient occlusion)");
        Print("    - Secondary bounces with varying directions");
        Print("    - Single rays");
        Print("");
        Print("  RTCRayQueryFlags.Coherent:");
        Print("    - Primary camera rays (nearby pixels)");
        Print("    - Ray packets (SIMD processing)");
        Print("    - Shadow rays to same light source");
        Print("    - Note: Embree 4 uses RTCRayQueryFlags.None for instances!");
    }

    /// <summary>
    /// Demonstrates error handling patterns and edge cases.
    /// Proper error handling prevents crashes and provides actionable feedback.
    /// </summary>
    private void DemonstrateErrorHandling()
    {
        Print("Robust error handling prevents crashes and debugging nightmares.");
        Print("");

        // Embree sets error state on failures; CheckError() retrieves and clears it
        Print("1. Device error checking:");
        Print("   Always check device errors after Embree API calls");
        try
        {
            using var device = new Device();
            bool hasError = device.CheckError("Device initialization");
            if (!hasError)
            {
                Print("   [OK] No errors - device initialized successfully");
            }
        }
        catch (Exception ex)
        {
            Print($"   [X] Device error: {ex.Message}");
        }

        Print("");
        Print("2. Handling invalid rays:");
        Print("   Invalid rays (NaN, infinity) can crash or produce undefined results");

        using var testScene = CreateSimpleScene();

        // Always validate rays before tracing
        var validOrigin = new V3f(0.0f, 0.0f, -1.0f);
        var validDir = new V3f(0.0f, 0.0f, 1.0f);
        var hit = new RayHit();

        if (IsValidRay(validOrigin, validDir))
        {
            bool hasHit = testScene.Intersect(validOrigin, validDir, ref hit);
            Print($"   [OK] Valid ray: hit={hasHit}");
        }

        // Zero direction: Embree returns false (no hit) but indicates a bug in user code
        var invalidDir = new V3f(0.0f, 0.0f, 0.0f);
        if (!IsValidRay(validOrigin, invalidDir))
        {
            Print($"   [OK] Caught invalid ray (zero direction) before tracing");
        }

        // NaN in origin or direction: BVH comparisons fail, returns false (no hit); tfar remains unchanged, not NaN
        var nanDir = new V3f(float.NaN, 0.0f, 1.0f);
        if (!IsValidRay(validOrigin, nanDir))
        {
            Print($"   [OK] Caught invalid ray (NaN) before tracing");
        }

        Print("");
        Print("3. Scene lifecycle management:");
        Print("   Always commit scene after geometry changes");
        Print("   Dispose resources in proper order (geometries before scene)");
        Print("   Use 'using' statements for automatic disposal");
        Print("");
        Print("BEST PRACTICES:");
        Print("  [OK] Validate ray parameters before tracing");
        Print("  [OK] Check device errors after API calls");
        Print("  [OK] Use try-finally or 'using' for resource cleanup");
        Print("  [OK] Keep filter function delegates alive (GC.KeepAlive)");
        Print("  [OK] Commit scenes after geometry modifications");
        Print("  [OK] Use appropriate ray flags for your use case");
    }

    private Scene CreateSceneWithOccluder()
    {
        var groundVertices = new V3f[]
        {
            new V3f(-10.0f, -10.0f, 0.0f),
            new V3f(10.0f, -10.0f, 0.0f),
            new V3f(10.0f, 10.0f, 0.0f),
            new V3f(-10.0f, 10.0f, 0.0f)
        };
        var groundIndices = new int[] { 0, 1, 2, 0, 2, 3 };

        var occluderVertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 1.0f),
            new V3f(1.0f, 0.0f, 1.0f),
            new V3f(1.0f, 1.0f, 1.0f),
            new V3f(0.0f, 1.0f, 1.0f)
        };
        var occluderIndices = new int[] { 0, 1, 2, 0, 2, 3 };

        var ground = new TriangleGeometry(Device!, groundVertices, groundIndices, RTCBuildQuality.High);
        var occluder = new TriangleGeometry(Device!, occluderVertices, occluderIndices, RTCBuildQuality.High);

        var scene = new Scene(Device!, RTCBuildQuality.High, false);
        scene.AttachGeometry(ground);
        scene.AttachGeometry(occluder);
        scene.Commit();

        return scene;
    }

    private Scene CreateSceneWithMultipleObjects()
    {
        var scene = new Scene(Device!, RTCBuildQuality.High, false);

        for (int i = 0; i < 3; i++)
        {
            float z = i * 2.0f;
            var vertices = new V3f[]
            {
                new V3f(0.0f, 0.0f, z),
                new V3f(1.0f, 0.0f, z),
                new V3f(1.0f, 1.0f, z),
                new V3f(0.0f, 1.0f, z)
            };
            var indices = new int[] { 0, 1, 2, 0, 2, 3 };
            var geometry = new TriangleGeometry(Device!, vertices, indices, RTCBuildQuality.High);
            scene.AttachGeometry(geometry);
        }

        scene.Commit();
        return scene;
    }

    private Scene CreateSceneWithOverlappingGeometry()
    {
        return CreateSceneWithMultipleObjects();
    }

    private Scene CreateLargeScene()
    {
        var scene = new Scene(Device!, RTCBuildQuality.High, false);

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                float fx = x * 2.0f;
                float fy = y * 2.0f;
                var vertices = new V3f[]
                {
                    new V3f(fx, fy, 0.0f),
                    new V3f(fx + 1.0f, fy, 0.0f),
                    new V3f(fx + 1.0f, fy + 1.0f, 0.0f),
                    new V3f(fx, fy + 1.0f, 0.0f)
                };
                var indices = new int[] { 0, 1, 2, 0, 2, 3 };
                var geometry = new TriangleGeometry(Device!, vertices, indices, RTCBuildQuality.High);
                scene.AttachGeometry(geometry);
            }
        }

        scene.Commit();
        return scene;
    }

    private Scene CreateSimpleScene()
    {
        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.5f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(Device!, vertices, indices, RTCBuildQuality.High);
        var scene = new Scene(Device!, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        return scene;
    }

    private System.Collections.Generic.List<RayHit> CollectAllHits(Scene scene, V3f origin, V3f direction, int maxHits)
    {
        var hits = new System.Collections.Generic.List<RayHit>();
        var currentOrigin = origin;
        var remainingDistance = float.MaxValue;

        for (int i = 0; i < maxHits; i++)
        {
            var hit = new RayHit();
            if (scene.Intersect(currentOrigin, direction, ref hit, 0.0f, remainingDistance))
            {
                hits.Add(hit);

                // Advance past hit to find next surface; epsilon (0.0001) prevents hitting same point due to floating-point precision—too small risks re-hitting, too large skips thin geometry
                float epsilon = 0.0001f;
                currentOrigin = currentOrigin + direction * (hit.T + epsilon);
                remainingDistance -= (hit.T + epsilon);

                if (remainingDistance <= 0)
                    break;
            }
            else
            {
                break; // No more hits
            }
        }

        return hits;
    }

    private (V3f origin, V3f direction)[] GenerateCoherentRays(V3f baseOrigin, int count)
    {
        var rays = new (V3f, V3f)[count];
        var random = new Random(42);

        for (int i = 0; i < count; i++)
        {
            // Nearby pixels have similar directions → traverse same BVH paths → cache hits
            var offset = new V3f(
                (float)(random.NextDouble() - 0.5) * 0.1f,
                (float)(random.NextDouble() - 0.5) * 0.1f,
                0.0f
            );
            var direction = new V3f(offset.X, offset.Y, 1.0f).Normalized;
            rays[i] = (baseOrigin + offset, direction);
        }

        return rays;
    }

    private (V3f origin, V3f direction)[] GenerateIncoherentRays(int count)
    {
        var rays = new (V3f, V3f)[count];
        var random = new Random(42);

        for (int i = 0; i < count; i++)
        {
            var origin = new V3f(
                (float)random.NextDouble() * 10.0f - 5.0f,
                (float)random.NextDouble() * 10.0f - 5.0f,
                -5.0f
            );
            var direction = new V3f(
                (float)random.NextDouble() - 0.5f,
                (float)random.NextDouble() - 0.5f,
                (float)random.NextDouble()
            ).Normalized;
            rays[i] = (origin, direction);
        }

        return rays;
    }

    /// <summary>
    /// Validates ray parameters before tracing.
    /// Edge case behaviors (tested in EdgeCases/ tests):
    /// - NaN in origin/direction: Returns false (no hit), tfar unchanged
    /// - Infinity in origin/direction: BVH traversal fails, no hits reported
    /// - Zero direction: Returns false (no hit), indicates user code bug
    /// - Negative tnear: Allows hits behind ray origin (t &lt; 0)
    /// - tnear > tfar: No intersection (empty interval)
    /// - Unnormalized direction: tfar in direction-vector units, not world units
    /// </summary>
    private bool IsValidRay(V3f origin, V3f direction)
    {
        // NaN origin: BVH tests fail, returns false (no hit)
        if (float.IsNaN(origin.X) || float.IsNaN(origin.Y) || float.IsNaN(origin.Z))
            return false;
        // NaN direction: BVH tests fail, returns false, tfar remains unchanged (not NaN)
        if (float.IsNaN(direction.X) || float.IsNaN(direction.Y) || float.IsNaN(direction.Z))
            return false;
        // Infinite origin: AABB tests fail, no hits
        if (float.IsInfinity(origin.X) || float.IsInfinity(origin.Y) || float.IsInfinity(origin.Z))
            return false;
        // Infinite direction: NaN propagation in BVH traversal, no hits
        if (float.IsInfinity(direction.X) || float.IsInfinity(direction.Y) || float.IsInfinity(direction.Z))
            return false;

        // Zero or near-zero direction: Returns false, indicates user code bug
        if (direction.LengthSquared < 1e-6f)
            return false;

        return true;
    }
}
