using System;
using System.Diagnostics;
using Aardvark.Base;
using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples.Examples;

/// <summary>
/// Performance Optimization Example: BVH Quality Settings and Ray Coherence
///
/// Demonstrates performance optimization techniques:
/// - BVH build quality settings (Low, Medium, High, Refit)
/// - Impact of ray coherence on performance
/// - Single rays vs batched operations
/// - Build time vs trace time trade-offs
/// - Memory usage statistics
/// - Practical optimization strategies
/// </summary>
public class PerformanceOptimizationExample : ExampleBase
{
    public override string Title => "Performance Optimization";
    public override string Description => "BVH quality, ray coherence, and performance tuning";
    public override string Category => "Performance";
    public override int OrderIndex => 9;
    public override int ApproximateLineCount => 350;

    private const int GridSize = 100;
    private const int RayCountCoherent = 100_000;
    private const int RayCountIncoherent = 100_000;

    protected override void Execute()
    {
        PrintSection("Overview");
        Print("This example demonstrates BVH quality settings and ray coherence impact.");
        Print($"Test scene: {GridSize}x{GridSize} grid = {GridSize * GridSize * 2:N0} triangles");
        Print($"Ray tests: {RayCountCoherent:N0} coherent + {RayCountIncoherent:N0} incoherent");
        Print("");

        // Part 1: BVH Quality Comparison
        PrintSection("Part 1: BVH Build Quality Comparison");
        CompareBVHQualities();

        // Part 2: Ray Coherence Impact
        PrintSection("Part 2: Ray Coherence Impact");
        TestRayCoherence();

        // Part 3: Dynamic Geometry (Refit)
        PrintSection("Part 3: Dynamic Geometry (Refit Quality)");
        TestDynamicGeometry();

        // Part 4: Optimization Recommendations
        PrintSection("Part 4: Optimization Strategies");
        PrintOptimizationTips();
    }

    /// <summary>
    /// Higher quality builds better BVH (tighter boxes, better splits) but takes longer.
    /// </summary>
    private void CompareBVHQualities()
    {
        Print("Comparing BVH build qualities (Low, Medium, High):");
        Print("Trade-off: Fast build vs. Fast tracing\n");

        var qualities = new[] { RTCBuildQuality.Low, RTCBuildQuality.Medium, RTCBuildQuality.High };
        var results = new (RTCBuildQuality quality, double buildMs, double traceMs, double throughput)[qualities.Length];

        for (int i = 0; i < qualities.Length; i++)
        {
            var quality = qualities[i];

            // Build scene
            Timer.Start("build");
            using var scene = CreateTestScene(quality);
            var buildMs = Timer.Stop();

            // Trace coherent rays
            Timer.Start("trace");
            TraceCoherentRays(scene, RayCountCoherent);
            var traceMs = Timer.Stop();
            var throughput = RayCountCoherent / traceMs * 1000.0; // rays/sec

            results[i] = (quality, buildMs, traceMs, throughput);

            Print($"{quality,6}: Build={buildMs,6:F2}ms  Trace={traceMs,6:F2}ms  " +
                  $"Throughput={Framework.Timer.FormatThroughput(RayCountCoherent, traceMs)}");
        }

        Print("\nAnalysis:");
        var lowBuild = results[0].buildMs;
        var highBuild = results[2].buildMs;
        var lowTrace = results[0].traceMs;
        var highTrace = results[2].traceMs;

        Print($"  Build time increase (Low→High): {(highBuild / lowBuild - 1) * 100:F1}%");
        Print($"  Trace time decrease (Low→High): {(1 - highTrace / lowTrace) * 100:F1}%");
        Print($"  Best for static scenes: High quality");
        Print($"  Best for dynamic scenes: Low/Medium quality");
        Print("");
    }

    /// <summary>
    /// Coherent rays share BVH traversal state (cache-friendly); incoherent rays thrash caches.
    /// </summary>
    private void TestRayCoherence()
    {
        Print("Comparing coherent vs. incoherent rays:");
        Print("Coherent = camera rays (similar directions)");
        Print("Incoherent = random rays (e.g., global illumination)\n");

        using var scene = CreateTestScene(RTCBuildQuality.High);

        Timer.Start("coherent");
        TraceCoherentRays(scene, RayCountCoherent);
        var coherentMs = Timer.Stop();
        var coherentThroughput = Framework.Timer.FormatThroughput(RayCountCoherent, coherentMs);

        Timer.Start("incoherent");
        TraceIncoherentRays(scene, RayCountIncoherent);
        var incoherentMs = Timer.Stop();
        var incoherentThroughput = Framework.Timer.FormatThroughput(RayCountIncoherent, incoherentMs);

        Print($"Coherent rays:   {coherentMs,6:F2}ms  ({coherentThroughput})");
        Print($"Incoherent rays: {incoherentMs,6:F2}ms  ({incoherentThroughput})");

        var slowdown = incoherentMs / coherentMs;
        Print($"\nIncoherent slowdown: {slowdown:F2}x");
        Print("Reason: Less effective BVH traversal cache usage");
        Print("");
    }

    /// <summary>
    /// Refit adjusts existing BVH node bounds without restructuring; much faster than rebuild.
    /// </summary>
    private void TestDynamicGeometry()
    {
        Print("Testing dynamic geometry updates (Refit vs. Rebuild):");
        Print("Scenario: Geometry vertices change every frame\n");

        const int NumFrames = 10;

        Print("Test 1: Using Refit quality");
        Timer.Start("refit");
        using (var scene = new Scene(Device!, RTCBuildQuality.Medium, dynamic: true))
        {
            var geometry = CreateDeformableGrid(GridSize, RTCBuildQuality.Refit);
            scene.AttachGeometry(geometry);
            scene.Commit();

            for (int frame = 0; frame < NumFrames; frame++)
            {
                DeformGeometry(geometry, frame);
                geometry.UpdateBuffer(RTCBufferType.Vertex);
                geometry.Commit();
                scene.Commit();

                TraceCoherentRays(scene, 10_000);
            }
        }
        var refitMs = Timer.Stop();
        var refitPerFrame = refitMs / NumFrames;

        Print("Test 2: Using High quality (full rebuild)");
        Timer.Start("rebuild");
        using (var scene = new Scene(Device!, RTCBuildQuality.High, dynamic: true))
        {
            var geometry = CreateDeformableGrid(GridSize, RTCBuildQuality.High);
            scene.AttachGeometry(geometry);
            scene.Commit();

            for (int frame = 0; frame < NumFrames; frame++)
            {
                DeformGeometry(geometry, frame);
                geometry.UpdateBuffer(RTCBufferType.Vertex);
                geometry.Commit();
                scene.Commit();

                TraceCoherentRays(scene, 10_000);
            }
        }
        var rebuildMs = Timer.Stop();
        var rebuildPerFrame = rebuildMs / NumFrames;

        Print($"\nRefit:   {refitMs,6:F2}ms total ({refitPerFrame:F2}ms/frame)");
        Print($"Rebuild: {rebuildMs,6:F2}ms total ({rebuildPerFrame:F2}ms/frame)");
        Print($"Speedup: {rebuildMs / refitMs:F2}x faster with Refit");
        Print("\nTrade-off: Refit is faster but may have slightly lower trace performance");
        Print("");
    }

    private Scene CreateTestScene(RTCBuildQuality quality)
    {
        var scene = new Scene(Device!, quality, dynamic: false);
        var geometry = CreateTriangleGrid(GridSize, quality);
        scene.AttachGeometry(geometry);
        scene.Commit();
        return scene;
    }

    private TriangleGeometry CreateTriangleGrid(int size, RTCBuildQuality quality)
    {
        var vertices = new V3f[(size + 1) * (size + 1)];
        var indices = new int[size * size * 6];

        int vIdx = 0;
        for (int y = 0; y <= size; y++)
        {
            for (int x = 0; x <= size; x++)
            {
                vertices[vIdx++] = new V3f(x, y, 0);
            }
        }

        int iIdx = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int v0 = y * (size + 1) + x;
                int v1 = v0 + 1;
                int v2 = v0 + (size + 1);
                int v3 = v2 + 1;

                indices[iIdx++] = v0;
                indices[iIdx++] = v1;
                indices[iIdx++] = v2;

                indices[iIdx++] = v1;
                indices[iIdx++] = v3;
                indices[iIdx++] = v2;
            }
        }

        return new TriangleGeometry(Device!, vertices, indices, quality);
    }

    private TriangleGeometry CreateDeformableGrid(int size, RTCBuildQuality quality)
    {
        return CreateTriangleGrid(size, quality);
    }

    private unsafe void DeformGeometry(TriangleGeometry geometry, int frame)
    {
        var ptr = geometry.GetVertexDataPointer();
        float time = frame * 0.1f;

        int size = GridSize + 1;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                float wave = (float)Math.Sin(x * 0.2f + time) * (float)Math.Cos(y * 0.2f + time);
                ptr[idx] = new V3f(x, y, wave * 2.0f);
            }
        }
    }

    private void TraceCoherentRays(Scene scene, int count)
    {
        var origin = new V3f(GridSize / 2f, GridSize / 2f, 100);
        var hit = new RayHit();
        int hitCount = 0;

        for (int i = 0; i < count; i++)
        {
            float u = (i % 100) / 100f;
            float v = ((i / 100) % 100) / 100f;

            var target = new V3f(u * GridSize, v * GridSize, 0);
            var direction = (target - origin).Normalized;

            if (scene.Intersect(origin, direction, ref hit))
            {
                hitCount++;
            }
        }
    }

    private void TraceIncoherentRays(Scene scene, int count)
    {
        var random = new Random(42);
        var hit = new RayHit();
        int hitCount = 0;

        for (int i = 0; i < count; i++)
        {
            var origin = new V3f(
                (float)random.NextDouble() * GridSize,
                (float)random.NextDouble() * GridSize,
                50 + (float)random.NextDouble() * 50
            );

            var direction = new V3f(
                (float)random.NextDouble() * 2 - 1,
                (float)random.NextDouble() * 2 - 1,
                -(float)random.NextDouble()
            ).Normalized;

            if (scene.Intersect(origin, direction, ref hit))
            {
                hitCount++;
            }
        }
    }

    private void PrintOptimizationTips()
    {
        Print("Performance Optimization Strategies:");
        Print("");

        Print("1. BVH Build Quality Selection:");
        Print("   * Static scenes → Use High quality");
        Print("   * Dynamic scenes (occasional updates) → Use Medium quality");
        Print("   * Per-frame deformation → Use Refit quality");
        Print("   * Rapid prototyping → Use Low quality");
        Print("");

        Print("2. Ray Coherence:");
        Print("   * Camera rays are naturally coherent (good performance)");
        Print("   * Shadow rays to small lights are coherent");
        Print("   * Global illumination rays are incoherent (slower)");
        Print("   * Consider batching similar rays together");
        Print("");

        Print("3. Scene Organization:");
        Print("   * Group static geometry separately from dynamic");
        Print("   * Use instancing for repeated objects");
        Print("   * Split large scenes into manageable chunks");
        Print("   * Consider level-of-detail (LOD) for distant geometry");
        Print("");

        Print("4. Memory Considerations:");
        Print("   * High quality BVH uses more memory");
        Print("   * Refit quality uses same memory as build quality");
        Print("   * Shared buffers reduce memory usage");
        Print("   * Dispose geometries and scenes when done");
        Print("");

        Print("5. Multi-threading:");
        Print("   * Embree automatically uses multiple threads");
        Print("   * Scene commit and ray tracing are parallelized");
        Print("   * Consider thread count in Device constructor");
        Print($"   * Current device: {Device!.ThreadCount} threads " +
              $"(0 = auto-detect)");
        Print("");

        Print("6. When to Use Refit:");
        Print("   [OK] Vertex positions change (skinned meshes, cloth)");
        Print("   [OK] Updates every frame or frequently");
        Print("   [OK] Topology stays constant (same triangles)");
        Print("   [X] Topology changes (different triangle count)");
        Print("   [X] Complete scene restructuring");
        Print("");

        Print("7. Profiling Tips:");
        Print("   * Separate build time from trace time");
        Print("   * Measure rays/second for trace performance");
        Print("   * Test with representative ray distributions");
        Print("   * Profile on target hardware");
        Print("");

        Print("Practical Example: 60 FPS rendering");
        Print("  Frame budget: 16.67ms");
        Print("  If scene commit takes 2ms → 14.67ms for tracing");
        Print("  At 10M rays/sec → ~146K rays per frame");
        Print("  → Choose Refit if scene updates every frame");
        Print("  → Choose High if scene is static");
    }
}
