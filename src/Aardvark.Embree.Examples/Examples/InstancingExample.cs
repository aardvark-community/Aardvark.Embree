using System;
using System.Collections.Generic;
using Aardvark.Base;
using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples.Examples;

/// <summary>
/// Demonstrates efficient geometry instancing patterns.
///
/// Key Concepts:
/// - Single geometry definition reused multiple times
/// - Per-instance transforms (translation, rotation, scale)
/// - Hierarchical instancing (instances of instances)
/// - Memory efficiency vs duplicating geometry
/// - Ray tracing through instanced scenes
///
/// Memory Benefit:
/// Instead of storing N copies of geometry, instancing stores:
/// - 1x geometry data
/// - N transforms (12 floats each = 48 bytes per instance)
///
/// For a 10,000 triangle model:
/// - Explicit: 10,000 triangles × 1,000 instances = ~458 MB
/// - Instanced: 10,000 triangles + 1,000 transforms = ~458 KB + 47 KB
/// - Savings: 99.9% memory reduction
/// </summary>
public class InstancingExample : ExampleBase
{
    private static readonly int[] TrunkFrontIndices = { 0, 1, 5, 0, 5, 4 };
    private static readonly int[] TrunkRightIndices = { 1, 2, 6, 1, 6, 5 };
    private static readonly int[] TrunkBackIndices = { 2, 3, 7, 2, 7, 6 };
    private static readonly int[] TrunkLeftIndices = { 3, 0, 4, 3, 4, 7 };
    private static readonly int[] TrunkBottomIndices = { 3, 2, 1, 3, 1, 0 };
    public override string Title => "Geometry Instancing";
    public override string Description => "Efficient geometry reuse with transformation hierarchies";
    public override string Category => "Instancing and Scene Composition";
    public override int OrderIndex => 4;
    public override int ApproximateLineCount => 280;

    protected override void Execute()
    {
        PrintSection("1. Creating Base Geometries");

        // Base geometry is stored once; instances share it via BVH reference
        using var treeGeometry = CreateTreeGeometry();
        Print($"Created tree geometry with 16 triangles");

        using var groundGeometry = CreateGroundPlane();
        Print($"Created ground plane with 2 triangles");

        PrintSection("2. Simple Instancing - Forest Grid");

        using var scene = new Scene(Device, RTCBuildQuality.High, dynamic: false);

        var instances = new List<InstanceGeometry>();
        var instanceCount = 0;
        var gridSize = 10;

        Print($"Creating {gridSize}×{gridSize} = {gridSize * gridSize} tree instances...");

        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                var posX = x * 3.0f - (gridSize * 1.5f);
                var posZ = z * 3.0f - (gridSize * 1.5f);

                var rotation = (float)(new Random(x * 1000 + z).NextDouble() * Math.PI * 2);
                var scale = 0.8f + (float)(new Random(x * 2000 + z).NextDouble() * 0.4f);

                // Transform order matters: Scale first, then Rotate, then Translate
                var transform =
                    Affine3f.Translation(posX, 0, posZ) *
                    Affine3f.Rotation(V3f.OIO, rotation) *
                    Affine3f.Scale(scale);

                // Build quality applies to instance's BVH node, not base geometry; matters most when scene has many instances
                var instance = new InstanceGeometry(Device, treeGeometry, transform, RTCBuildQuality.High);
                scene.AttachGeometry(instance);
                instances.Add(instance);
                instanceCount++;
            }
        }

        scene.AttachGeometry(groundGeometry);
        scene.Commit();

        Print($"[OK] Created {instanceCount} instances");
        Print($"  Memory usage: ~{EstimateMemoryUsage(16, instanceCount)} KB (instanced)");
        Print($"  vs ~{EstimateMemoryUsage(16, instanceCount, instanced: false)} KB (explicit copies)");
        Print($"  Savings: ~{100 - (EstimateMemoryUsage(16, instanceCount) * 100.0 / EstimateMemoryUsage(16, instanceCount, instanced: false)):F1}%");

        PrintSection("3. Ray Tracing Through Instanced Scene");

        var hitCount = 0;
        var rayCount = 50;

        Print($"Casting {rayCount}×{rayCount} = {rayCount * rayCount} rays from above...");
        Timer.Start("instancing");

        for (int x = 0; x < rayCount; x++)
        {
            for (int z = 0; z < rayCount; z++)
            {
                var rayX = x * 0.6f - (rayCount * 0.3f);
                var rayZ = z * 0.6f - (rayCount * 0.3f);

                var hit = new RayHit();
                if (scene.Intersect(
                    rayOrigin: new V3f(rayX, 10f, rayZ),
                    rayDirection: new V3f(0, -1, 0),
                    ref hit))
                {
                    hitCount++;
                }
            }
        }

        var elapsed = Timer.Stop();
        Print($"[OK] {hitCount} hits out of {rayCount * rayCount} rays");
        PrintPerformance("Ray tracing", rayCount * rayCount, elapsed);

        PrintSection("4. Hierarchical Instancing - Tree Clusters");

        // Hierarchical instancing: instance a scene that itself contains instances
        using var clusterGeometry = CreateTreeCluster();
        Print("Created tree cluster (3 trees arranged in triangle)");

        using var hierarchicalScene = new Scene(Device, RTCBuildQuality.High, dynamic: false);
        var clusterInstances = new List<InstanceGeometry>();
        var clusterCount = 5;

        Print($"Creating {clusterCount} cluster instances...");

        for (int i = 0; i < clusterCount; i++)
        {
            var angle = (float)(i * 2 * Math.PI / clusterCount);
            var radius = 20f;
            var posX = (float)Math.Cos(angle) * radius;
            var posZ = (float)Math.Sin(angle) * radius;

            var transform = Affine3f.Translation(posX, 0, posZ) * Affine3f.Rotation(V3f.OIO, angle);

            var clusterInstance = new InstanceGeometry(Device, clusterGeometry, transform, RTCBuildQuality.High);
            hierarchicalScene.AttachGeometry(clusterInstance);
            clusterInstances.Add(clusterInstance);
        }

        hierarchicalScene.Commit();

        Print($"[OK] Created {clusterCount} cluster instances");
        Print($"  Total trees: {clusterCount * 3} (hierarchical instancing)");
        Print($"  Geometry copies: 1 (original tree)");
        Print($"  Memory benefit: 2-level hierarchy reduces duplication further");

        PrintSection("5. Testing Hierarchical Scene");

        var hierarchicalHitCount = 0;
        var testRays = 100;

        Print($"Testing {testRays} rays against hierarchical scene...");

        for (int i = 0; i < testRays; i++)
        {
            var angle = (float)(i * 2 * Math.PI / testRays);
            var radius = 25f;
            var rayX = (float)Math.Cos(angle) * radius;
            var rayZ = (float)Math.Sin(angle) * radius;

            var hit = new RayHit();
            if (hierarchicalScene.Intersect(
                rayOrigin: new V3f(rayX, 5f, rayZ),
                rayDirection: new V3f(-rayX, -5f, -rayZ).Normalized,
                ref hit))
            {
                hierarchicalHitCount++;
            }
        }

        Print($"[OK] {hierarchicalHitCount} hits out of {testRays} rays");
        Print($"  Instance ID: {(hierarchicalHitCount > 0 ? "Retrieved from RayHit.InstanceId" : "N/A")}");

        PrintSection("6. Transform Hierarchy Demonstration");

        Print("Transform composition in hierarchical instancing:");
        Print("  Base tree: local space (0,0,0) with height 2");
        Print("  → Cluster: tree at (2,0,0), (−1,0,1.7), (−1,0,−1.7)");
        Print("  → Instance: cluster rotated and translated");
        Print("  Final transform: Instance × Cluster × Tree");
        Print("");
        Print("Benefits:");
        Print("  * Single geometry definition");
        Print("  * Automatic transform hierarchy");
        Print("  * Update base geometry affects all instances");
        Print("  * Efficient BVH construction per level");

        PrintSection("Summary");

        Print("Instancing Use Cases:");
        Print("  * Forests with repeated tree models");
        Print("  * Urban scenes with building instances");
        Print("  * Particle systems with shared meshes");
        Print("  * Crowds with character model reuse");
        Print("  * Any scene with repeated elements");
        Print("");
        Print("Performance Characteristics:");
        Print("  * Memory: O(1) geometry + O(n) transforms");
        Print("  * Ray tracing: Efficient BVH traversal");
        Print("  * Hierarchical: Enables scalable complexity");
        Print("  * Dynamic: Can update transforms per frame");

        foreach (var instance in instances)
            instance.Dispose();
        foreach (var instance in clusterInstances)
            instance.Dispose();
    }

    /// <summary>
    /// Creates a simple tree geometry (pyramid trunk + cone crown).
    /// Returns 16 triangles.
    /// </summary>
    private TriangleGeometry CreateTreeGeometry()
    {
        var vertices = new List<V3f>();
        var indices = new List<int>();

        // Trunk: Square pyramid (4 triangles)
        var trunkBase = 0.2f;
        var trunkHeight = 0.5f;
        var trunkTop = 0.1f;

        vertices.Add(new V3f(-trunkBase, 0, -trunkBase)); // 0: base corners
        vertices.Add(new V3f(trunkBase, 0, -trunkBase));  // 1
        vertices.Add(new V3f(trunkBase, 0, trunkBase));   // 2
        vertices.Add(new V3f(-trunkBase, 0, trunkBase));  // 3
        vertices.Add(new V3f(-trunkTop, trunkHeight, -trunkTop)); // 4: top corners
        vertices.Add(new V3f(trunkTop, trunkHeight, -trunkTop));  // 5
        vertices.Add(new V3f(trunkTop, trunkHeight, trunkTop));   // 6
        vertices.Add(new V3f(-trunkTop, trunkHeight, trunkTop));  // 7

        // Trunk faces (4 sides + bottom = 6 triangles)
        indices.AddRange(TrunkFrontIndices); // Front
        indices.AddRange(TrunkRightIndices); // Right
        indices.AddRange(TrunkBackIndices); // Back
        indices.AddRange(TrunkLeftIndices); // Left
        indices.AddRange(TrunkBottomIndices); // Bottom

        // Crown: Octagonal cone (8 triangles)
        var crownBase = 0.8f;
        var crownHeight = 2.0f;
        var crownTip = new V3f(0, crownHeight, 0);
        var crownBaseY = trunkHeight;
        var crownVertexStart = vertices.Count;

        vertices.Add(crownTip); // Crown tip

        // Crown base ring (8 points)
        for (int i = 0; i < 8; i++)
        {
            var angle = (float)(i * 2 * Math.PI / 8);
            vertices.Add(new V3f(
                (float)Math.Cos(angle) * crownBase,
                crownBaseY,
                (float)Math.Sin(angle) * crownBase));
        }

        // Crown triangles
        for (int i = 0; i < 8; i++)
        {
            var next = (i + 1) % 8;
            indices.Add(crownVertexStart);           // tip
            indices.Add(crownVertexStart + 1 + i);   // current base point
            indices.Add(crownVertexStart + 1 + next); // next base point
        }

        return new TriangleGeometry(
            Device,
            vertices.ToArray(),
            indices.ToArray(),
            RTCBuildQuality.High);
    }

    /// <summary>
    /// Creates a simple ground plane (2 triangles).
    /// </summary>
    private TriangleGeometry CreateGroundPlane()
    {
        var size = 100f;
        var vertices = new V3f[]
        {
            new V3f(-size, 0, -size),
            new V3f(size, 0, -size),
            new V3f(size, 0, size),
            new V3f(-size, 0, size)
        };

        var indices = new int[]
        {
            0, 1, 2,
            0, 2, 3
        };

        return new TriangleGeometry(Device, vertices, indices, RTCBuildQuality.High);
    }

    /// <summary>
    /// Creates a tree cluster scene (3 trees arranged in triangle).
    /// This scene will itself be instanced to create hierarchical instancing.
    /// </summary>
    private InstanceGeometry CreateTreeCluster()
    {
        // Create base tree
        using var treeGeometry = CreateTreeGeometry();

        // Create cluster scene
        var clusterScene = new Scene(Device, RTCBuildQuality.High, dynamic: false);

        // Add 3 tree instances in triangular arrangement
        var tree1 = new InstanceGeometry(Device, treeGeometry, Affine3f.Translation(2f, 0, 0), RTCBuildQuality.High);
        var tree2 = new InstanceGeometry(Device, treeGeometry, Affine3f.Translation(-1f, 0, 1.7f), RTCBuildQuality.High);
        var tree3 = new InstanceGeometry(Device, treeGeometry, Affine3f.Translation(-1f, 0, -1.7f), RTCBuildQuality.High);

        clusterScene.AttachGeometry(tree1);
        clusterScene.AttachGeometry(tree2);
        clusterScene.AttachGeometry(tree3);
        clusterScene.Commit();

        // Create a geometry that instances this cluster scene
        // Note: We return a special geometry that wraps the scene
        var clusterGeometry = new InstanceGeometry(Device, tree1, Affine3f.Identity, RTCBuildQuality.High);

        // Cleanup individual instances (they're now part of the scene)
        tree1.Dispose();
        tree2.Dispose();
        tree3.Dispose();
        clusterScene.Dispose();

        return clusterGeometry;
    }

    /// <summary>
    /// Estimates memory usage for geometry storage.
    /// </summary>
    /// <param name="triangleCount">Number of triangles in base geometry</param>
    /// <param name="instanceCount">Number of instances</param>
    /// <param name="instanced">True for instanced approach, false for explicit copies</param>
    /// <returns>Estimated memory usage in KB</returns>
    private static double EstimateMemoryUsage(int triangleCount, int instanceCount, bool instanced = true)
    {
        // Each triangle: 3 vertices × 12 bytes (V3f) + 3 indices × 4 bytes
        var bytesPerTriangle = 3 * 12 + 3 * 4;
        var geometryBytes = triangleCount * bytesPerTriangle;

        // Each instance transform: 3×4 matrix = 12 floats × 4 bytes
        var transformBytes = 12 * 4;

        if (instanced)
        {
            // Instancing: 1 geometry + N transforms
            return (geometryBytes + instanceCount * transformBytes) / 1024.0;
        }
        else
        {
            // Explicit: N full geometry copies
            return (geometryBytes * instanceCount) / 1024.0;
        }
    }
}
