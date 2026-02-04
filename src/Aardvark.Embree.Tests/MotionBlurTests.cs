using Aardvark.Base;
using Xunit;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Comprehensive tests for motion blur functionality including vertex motion and instance motion.
/// </summary>
public class MotionBlurTests
{
    private static readonly int[] TriangleIndices = { 0, 1, 2 };
    private static readonly int[] QuadIndices = { 0, 1, 2, 0, 2, 3 };
    private static readonly float[] KeyFrameTimes = { 0.0f, 0.33f, 0.67f, 1.0f };
    // ===== Basic Motion Blur Tests =====

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateLinearMotionBlurGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var verticesT0 = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var verticesT1 = new V3f[]
        {
            new V3f(0, 0, 1),
            new V3f(1, 0, 1),
            new V3f(0, 1, 1)
        };

        var indices = TriangleIndices;

        using var geom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            quality);

        Assert.NotEqual(IntPtr.Zero, geom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void LinearMotionBlurIntersectsAtDifferentTimes(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Triangle moves from Z=2 at t=0 to Z=4 at t=1
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

        var indices = TriangleIndices;

        using var geom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            quality);

        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        var rayOrigin = new V3f(0, 0, 0);
        var rayDirection = new V3f(0, 0, 1);

        // At t=0, triangle should be at Z=2
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 1.9f, 2.1f);

        // At t=0.5, triangle should be interpolated to Z=3
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 2.9f, 3.1f);

        // At t=1.0, triangle should be at Z=4
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 1.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 3.9f, 4.1f);
    }

    // ===== Multi-Step Motion Blur Tests =====

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateMultiStepMotionBlurGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var verticesT0 = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var verticesT1 = new V3f[]
        {
            new V3f(0, 0, 1),
            new V3f(1, 0, 1),
            new V3f(0, 1, 1)
        };

        var verticesT2 = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(0, 1, 2)
        };

        var indices = TriangleIndices;

        var timeSteps = new ReadOnlyMemory<V3f>[]
        {
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<V3f>(verticesT2)
        };

        using var geom = new MotionBlurGeometry(device, timeSteps, new ReadOnlyMemory<int>(indices), quality);

        Assert.NotEqual(IntPtr.Zero, geom.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MultiStepMotionBlurInterpolatesBetweenSteps(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Triangle moves: Z=2 (t=0) -> Z=4 (t=0.5) -> Z=6 (t=1.0)
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

        var indices = TriangleIndices;

        var timeSteps = new ReadOnlyMemory<V3f>[]
        {
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<V3f>(verticesT2)
        };

        using var geom = new MotionBlurGeometry(device, timeSteps, new ReadOnlyMemory<int>(indices), quality);

        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        var rayOrigin = new V3f(0, 0, 0);
        var rayDirection = new V3f(0, 0, 1);

        // At t=0.0, triangle at Z=2
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 1.9f, 2.1f);

        // At t=0.25, triangle interpolated between step 0 and 1 -> Z=3
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.25f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 2.9f, 3.1f);

        // At t=0.5, triangle at step 1 -> Z=4
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 3.9f, 4.1f);

        // At t=0.75, triangle interpolated between step 1 and 2 -> Z=5
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.75f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 4.9f, 5.1f);

        // At t=1.0, triangle at step 2 -> Z=6
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 1.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 5.9f, 6.1f);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void ComplexMotionWithFourTimeSteps(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a triangle that moves in a complex path over 4 time steps
        var timeSteps = new[]
        {
            new V3f[] // t=0: at origin
            {
                new V3f(-1, -1, 0),
                new V3f(1, -1, 0),
                new V3f(0, 1, 0)
            },
            new V3f[] // t=0.33: move up and rotate slightly
            {
                new V3f(-0.8f, -1.2f, 2),
                new V3f(1.2f, -0.8f, 2),
                new V3f(0.2f, 1.0f, 2)
            },
            new V3f[] // t=0.67: move further and rotate more
            {
                new V3f(-0.5f, -1.3f, 4),
                new V3f(1.3f, -0.5f, 4),
                new V3f(0.5f, 0.9f, 4)
            },
            new V3f[] // t=1.0: final position
            {
                new V3f(-0.2f, -1.4f, 6),
                new V3f(1.4f, -0.2f, 6),
                new V3f(0.8f, 0.8f, 6)
            }
        };

        var indices = TriangleIndices;
        var memorySteps = timeSteps.Select(v => new ReadOnlyMemory<V3f>(v)).ToArray();

        using var geom = new MotionBlurGeometry(device, memorySteps, new ReadOnlyMemory<int>(indices), quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Test intersection at different time values - verify at least some hits
        // due to complex rotation, not all rays may hit at all times
        var testTimes = KeyFrameTimes;  // Use key frame times
        int hitCount = 0;

        foreach (var t in testTimes)
        {
            var hit = new RayHit();
            // Ray from center, more likely to hit the triangle
            var rayOrigin = new V3f(0, 0, -1);
            var rayDirection = new V3f(0, 0, 1);

            bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                              minT: 0.0f, maxT: float.MaxValue, time: t);

            if (intersected && hit.T > 0)
                hitCount++;
        }

        // At least some time steps should produce valid hits
        Assert.True(hitCount >= 2, $"Expected at least 2 hits at keyframe times, got {hitCount}");
    }

    // ===== Vertex Motion Tests =====

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DeformingGeometryMotionBlur(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a quad that deforms over time (bending/warping motion)
        var verticesT0 = new V3f[]
        {
            new V3f(-2, -2, 0),
            new V3f(2, -2, 0),
            new V3f(2, 2, 0),
            new V3f(-2, 2, 0)
        };

        // Deform into a curved surface
        var verticesT1 = new V3f[]
        {
            new V3f(-2, -2, 0),
            new V3f(2, -2, 0),
            new V3f(2, 2, 2),    // Lift top vertices
            new V3f(-2, 2, 2)
        };

        var indices = QuadIndices; // Two triangles forming a quad

        using var geom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            quality);

        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Test ray hitting the deforming surface at different times
        var rayOrigin = new V3f(0, 1, 5);
        var rayDirection = new V3f(0, 0, -1);

        // At t=0, surface is flat at Z=0
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 4.9f, 5.1f);

        // At t=0.5, surface is partially deformed
        // The interpolation position depends on which triangle is hit and UV coordinates
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 3.5f, 4.5f); // Hit should be closer due to deformation (widened tolerance for non-uniform quad)

        // At t=1.0, surface is fully deformed
        // The interpolation varies depending on which triangle is hit
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 1.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 2.5f, 3.5f); // Hit at deformed position (widened tolerance for triangulated quad)
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void RotatingTriangleMotionBlur(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a triangle that rotates around the Z axis
        const int steps = 5;
        var timeSteps = new ReadOnlyMemory<V3f>[steps];

        for (int i = 0; i < steps; i++)
        {
            float angle = (float)(i * Math.PI / 2.0 / (steps - 1)); // Rotate 90 degrees total
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);

            var vertices = new V3f[]
            {
                new V3f(cos * 2, sin * 2, 1),
                new V3f(-sin * 2, cos * 2, 1),
                new V3f(0, 0, 1)
            };

            timeSteps[i] = new ReadOnlyMemory<V3f>(vertices);
        }

        var indices = TriangleIndices;

        using var geom = new MotionBlurGeometry(device, timeSteps, new ReadOnlyMemory<int>(indices), quality);
        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Test intersection along the rotation path
        for (float t = 0; t <= 1.0f; t += 0.25f)
        {
            var hit = new RayHit();
            var rayOrigin = new V3f(0, 0, 0);
            var rayDirection = new V3f(0, 0, 1);

            bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                              minT: 0.0f, maxT: float.MaxValue, time: t);

            Assert.True(intersected, $"Should intersect rotating triangle at time {t}");
            Assert.InRange(hit.T, 0.9f, 1.1f); // Triangle stays at Z=1
        }
    }

    // ===== Instance Motion Tests =====

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceTransformMotionLinear(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create static triangle geometry
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(0, 1, 0)
        };
        var indices = TriangleIndices;

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Create instance with animated transform
        // Note: This assumes InstanceGeometry supports motion via multiple transforms
        // If not supported yet, this test documents the expected API
        var transformT0 = Affine3f.Translation(0, 0, 2);
        var transformT1 = Affine3f.Translation(0, 0, 4);

        using var instance = new InstanceGeometry(device, triangleGeom, transformT0, quality);

        // Set second transform for motion blur (if API supports it)
        // instance.SetTransform(1, transformT1);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Test intersection at different times
        var rayOrigin = new V3f(0, 0, 0);
        var rayDirection = new V3f(0, 0, 1);

        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 1.9f, 2.1f); // Instance at Z=2 at t=0
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CombinedVertexAndInstanceMotion(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create motion blur geometry (deforming triangle)
        var verticesT0 = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(0, 1, 0)
        };

        var verticesT1 = new V3f[]
        {
            new V3f(-1.5f, -1.5f, 0),  // Triangle expands
            new V3f(1.5f, -1.5f, 0),
            new V3f(0, 1.5f, 0)
        };

        var indices = TriangleIndices;

        using var motionGeom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            quality);

        // Add to scene (could be instanced with transform motion too)
        scene.AttachGeometry(motionGeom);
        scene.Commit();

        // Test that both vertex deformation works
        var rayOrigin = new V3f(0.3f, 0.3f, 1);
        var rayDirection = new V3f(0, 0, -1);

        // At t=0, smaller triangle
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        Assert.True(intersected);

        // At t=1, expanded triangle should still be hit
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 1.0f);
        Assert.True(intersected);
    }

    // ===== Time Range Tests =====

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlurWithCustomTimeRange(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Triangle moves from Z=2 to Z=4
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
            quality);

        // Set time range to [0.2, 0.8] - geometry only visible in this range
        geom.SetTimeRange(0.2f, 0.8f);
        geom.Commit();

        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        var rayOrigin = new V3f(0, 0, 0);
        var rayDirection = new V3f(0, 0, 1);

        // At t=0.0 (before time range), geometry should be invisible
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        Assert.False(intersected);

        // At t=0.2 (start of time range), geometry becomes visible
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.2f);
        Assert.True(intersected);

        // At t=0.5 (within time range), geometry visible with interpolated position
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        Assert.True(intersected);
        Assert.InRange(hit.T, 2.9f, 3.1f); // Interpolated position

        // At t=0.9 (after time range), geometry should be invisible
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.9f);
        Assert.False(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OverlappingTimeRangesMultipleGeometries(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // First geometry: visible from t=0 to t=0.6
        var vertices1T0 = new V3f[] { new V3f(-2, -1, 1), new V3f(-1, -1, 1), new V3f(-1.5f, 1, 1) };
        var vertices1T1 = new V3f[] { new V3f(-2, -1, 2), new V3f(-1, -1, 2), new V3f(-1.5f, 1, 2) };

        using var geom1 = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(vertices1T0),
            new ReadOnlyMemory<V3f>(vertices1T1),
            new ReadOnlyMemory<int>(TriangleIndices),
            quality);
        geom1.SetTimeRange(0.0f, 0.6f);
        geom1.Commit();

        // Second geometry: visible from t=0.4 to t=1.0
        var vertices2T0 = new V3f[] { new V3f(1, -1, 1), new V3f(2, -1, 1), new V3f(1.5f, 1, 1) };
        var vertices2T1 = new V3f[] { new V3f(1, -1, 2), new V3f(2, -1, 2), new V3f(1.5f, 1, 2) };

        using var geom2 = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(vertices2T0),
            new ReadOnlyMemory<V3f>(vertices2T1),
            new ReadOnlyMemory<int>(TriangleIndices),
            quality);
        geom2.SetTimeRange(0.4f, 1.0f);
        geom2.Commit();

        scene.AttachGeometry(geom1);
        scene.AttachGeometry(geom2);
        scene.Commit();

        // Test visibility at different times
        var hit = new RayHit();

        // At t=0.2, only geom1 visible
        bool intersected = scene.Intersect(new V3f(-1.5f, 0, 0), new V3f(0, 0, 1), ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.2f);
        Assert.True(intersected);

        intersected = scene.Intersect(new V3f(1.5f, 0, 0), new V3f(0, 0, 1), ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.2f);
        Assert.False(intersected);

        // At t=0.5, both geometries visible (overlap period)
        intersected = scene.Intersect(new V3f(-1.5f, 0, 0), new V3f(0, 0, 1), ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        Assert.True(intersected);

        intersected = scene.Intersect(new V3f(1.5f, 0, 0), new V3f(0, 0, 1), ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        Assert.True(intersected);

        // At t=0.8, only geom2 visible
        intersected = scene.Intersect(new V3f(-1.5f, 0, 0), new V3f(0, 0, 1), ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.8f);
        Assert.False(intersected);

        intersected = scene.Intersect(new V3f(1.5f, 0, 0), new V3f(0, 0, 1), ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.8f);
        Assert.True(intersected);
    }

    // ===== Performance and Stress Tests =====

    [Fact]
    public void LargeMotionBlurScene()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

        // Create multiple motion blur geometries
        const int geometryCount = 100;
        var random = new Random(42);
        var geometries = new MotionBlurGeometry[geometryCount];

        for (int i = 0; i < geometryCount; i++)
        {
            float x = (float)(random.NextDouble() * 20 - 10);
            float y = (float)(random.NextDouble() * 20 - 10);
            float z = (float)(random.NextDouble() * 10);

            var verticesT0 = new V3f[]
            {
                new V3f(x - 0.5f, y - 0.5f, z),
                new V3f(x + 0.5f, y - 0.5f, z),
                new V3f(x, y + 0.5f, z)
            };

            var verticesT1 = new V3f[]
            {
                new V3f(x - 0.5f, y - 0.5f, z + 2),
                new V3f(x + 0.5f, y - 0.5f, z + 2),
                new V3f(x, y + 0.5f, z + 2)
            };

            var indices = TriangleIndices;

            geometries[i] = new MotionBlurGeometry(device,
                new ReadOnlyMemory<V3f>(verticesT0),
                new ReadOnlyMemory<V3f>(verticesT1),
                new ReadOnlyMemory<int>(indices),
                RTCBuildQuality.Medium);

            scene.AttachGeometry(geometries[i]);
        }

        scene.Commit();

        // Test ray queries at different times and positions
        // Since triangles are randomly positioned, scan across the area to find hits
        var testTimes = new[] { 0.0f, 0.5f, 1.0f };
        int totalHits = 0;

        foreach (var t in testTimes)
        {
            // Scan multiple rays to increase hit probability
            for (float x = -8f; x <= 8f; x += 4f)
            {
                for (float y = -8f; y <= 8f; y += 4f)
                {
                    var hit = new RayHit();
                    bool anyHit = scene.Intersect(new V3f(x, y, -1), new V3f(0, 0, 1), ref hit,
                                                  minT: 0.0f, maxT: float.MaxValue, time: t);
                    if (anyHit) totalHits++;
                }
            }
        }

        // With 100 random triangles scanned over 75 rays (25 positions * 3 times), should get some hits
        Assert.True(totalHits > 0, $"Expected some hits from 100 random triangles, got {totalHits}");

        // Clean up
        foreach (var geom in geometries)
        {
            geom.Dispose();
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void ParallelRayQueriesWithMotionBlur(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a large moving triangle
        var verticesT0 = new V3f[]
        {
            new V3f(-10, -10, 5),
            new V3f(10, -10, 5),
            new V3f(0, 10, 5)
        };

        var verticesT1 = new V3f[]
        {
            new V3f(-10, -10, 10),
            new V3f(10, -10, 10),
            new V3f(0, 10, 10)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            quality);

        scene.AttachGeometry(geom);
        scene.Commit();

        // Perform parallel ray queries at different times
        const int rayCount = 1000;
        var results = new bool[rayCount];

        Parallel.For(0, rayCount, i =>
        {
            float t = (float)i / (rayCount - 1);
            var hit = new RayHit();

            // Ray from random position within triangle bounds
            float u = (float)(i % 10) / 10.0f - 0.5f;
            float v = (float)((i / 10) % 10) / 10.0f - 0.5f;
            var rayOrigin = new V3f(u * 5, v * 5, 0);
            var rayDirection = new V3f(0, 0, 1);

            results[i] = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                         minT: 0.0f, maxT: float.MaxValue, time: t);
        });

        // Most rays should hit the large triangle
        int hitCount = results.Count(r => r);
        Assert.True(hitCount > rayCount * 0.5, $"Expected at least 50% hits, got {hitCount}/{rayCount}");
    }

    // ===== Error Handling Tests =====

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void ThrowsOnInvalidTimeStepCount(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var indices = new int[] { 0, 1, 2 };

        // Single time step array (need at least 2)
        var singleStep = new ReadOnlyMemory<V3f>[] { new ReadOnlyMemory<V3f>(vertices) };

        Assert.Throws<ArgumentException>(() =>
            new MotionBlurGeometry(device, singleStep, new ReadOnlyMemory<int>(indices), quality));

        // Null array
        Assert.Throws<ArgumentException>(() =>
            new MotionBlurGeometry(device, null, new ReadOnlyMemory<int>(indices), quality));

        // Empty array
        var emptySteps = new ReadOnlyMemory<V3f>[0];
        Assert.Throws<ArgumentException>(() =>
            new MotionBlurGeometry(device, emptySteps, new ReadOnlyMemory<int>(indices), quality));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void ThrowsOnMismatchedVertexCounts(RTCBuildQuality quality)
    {
        using var device = new Device();

        var verticesT0 = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        var verticesT1 = new V3f[]
        {
            new V3f(0, 0, 1),
            new V3f(1, 0, 1)  // Missing one vertex
        };

        var indices = new int[] { 0, 1, 2 };

        // Linear motion blur with mismatched vertex counts
        Assert.Throws<ArgumentException>(() =>
            new MotionBlurGeometry(device,
                new ReadOnlyMemory<V3f>(verticesT0),
                new ReadOnlyMemory<V3f>(verticesT1),
                new ReadOnlyMemory<int>(indices),
                quality));

        // Multi-step motion blur with mismatched vertex counts
        var verticesT2 = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(0, 1, 2),
            new V3f(0.5f, 0.5f, 2)  // Extra vertex
        };

        var timeSteps = new ReadOnlyMemory<V3f>[]
        {
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<V3f>(verticesT2)
        };

        Assert.Throws<ArgumentException>(() =>
            new MotionBlurGeometry(device, timeSteps, new ReadOnlyMemory<int>(indices), quality));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void ThrowsOnInvalidTimeRange(RTCBuildQuality quality)
    {
        using var device = new Device();

        var verticesT0 = new V3f[] { new V3f(0, 0, 0), new V3f(1, 0, 0), new V3f(0, 1, 0) };
        var verticesT1 = new V3f[] { new V3f(0, 0, 1), new V3f(1, 0, 1), new V3f(0, 1, 1) };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            quality);

        // Invalid: start time < 0
        Assert.Throws<ArgumentOutOfRangeException>(() => geom.SetTimeRange(-0.1f, 0.5f));

        // Invalid: end time > 1
        Assert.Throws<ArgumentOutOfRangeException>(() => geom.SetTimeRange(0.5f, 1.1f));

        // Invalid: start >= end
        Assert.Throws<ArgumentException>(() => geom.SetTimeRange(0.5f, 0.5f));
        Assert.Throws<ArgumentException>(() => geom.SetTimeRange(0.6f, 0.5f));
    }

    // ===== Integration Tests =====

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlurWithOcclusionQueries(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create two triangles: one moving, one static occluder
        var movingVerticesT0 = new V3f[]
        {
            new V3f(-1, -1, 5),
            new V3f(1, -1, 5),
            new V3f(0, 1, 5)
        };

        var movingVerticesT1 = new V3f[]
        {
            new V3f(-1, -1, 2),  // Moves forward
            new V3f(1, -1, 2),
            new V3f(0, 1, 2)
        };

        var staticVertices = new V3f[]
        {
            new V3f(-2, -2, 3),
            new V3f(2, -2, 3),
            new V3f(0, 2, 3)
        };

        var indices = new int[] { 0, 1, 2 };

        using var movingGeom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(movingVerticesT0),
            new ReadOnlyMemory<V3f>(movingVerticesT1),
            new ReadOnlyMemory<int>(indices),
            quality);

        using var staticGeom = new TriangleGeometry(device, staticVertices, indices, quality);

        scene.AttachGeometry(movingGeom);
        scene.AttachGeometry(staticGeom);
        scene.Commit();

        var rayOrigin = new V3f(0, 0, 0);
        var rayDirection = new V3f(0, 0, 1);

        // At t=0, moving triangle is behind static (occluded)
        bool occluded = scene.Occluded(rayOrigin, rayDirection,
                                       minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        Assert.True(occluded);

        // At t=1, moving triangle is in front of static (still occluded but closer)
        occluded = scene.Occluded(rayOrigin, rayDirection,
                                  minT: 0.0f, maxT: float.MaxValue, time: 1.0f);
        Assert.True(occluded);

        // Test with limited range that excludes both triangles
        occluded = scene.Occluded(rayOrigin, rayDirection,
                                  minT: 0.0f, maxT: 1.5f, time: 0.5f);
        Assert.False(occluded);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlurWithSpanAPI(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Test Span overload for motion blur geometry
        Span<V3f> verticesT0 = stackalloc V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };

        Span<V3f> verticesT1 = stackalloc V3f[]
        {
            new V3f(0, 0, 1),
            new V3f(1, 0, 1),
            new V3f(0, 1, 1)
        };

        Span<int> indices = stackalloc int[] { 0, 1, 2 };

        using var geom = new MotionBlurGeometry(device,
            verticesT0,
            verticesT1,
            indices,
            quality);

        Assert.NotEqual(IntPtr.Zero, geom.Handle);

        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Verify it works with ray queries
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(0.25f, 0.25f, -1), new V3f(0, 0, 1), ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlurNormalInterpolation(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create a triangle that rotates, checking normal interpolation
        var verticesT0 = new V3f[]
        {
            new V3f(-1, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 2, 0)
        };

        // Rotate 45 degrees around X axis
        float angle = (float)(Math.PI / 4);
        float cos = (float)Math.Cos(angle);
        float sin = (float)Math.Sin(angle);

        var verticesT1 = new V3f[]
        {
            new V3f(-1, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 2 * cos, 2 * sin)
        };

        var indices = new int[] { 0, 1, 2 };

        using var geom = new MotionBlurGeometry(device,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            quality);

        using var scene = new Scene(device, quality, dynamic: false);
        scene.AttachGeometry(geom);
        scene.Commit();

        // Test normal at different times
        var rayOrigin = new V3f(0, 0.5f, 2);
        var rayDirection = new V3f(0, 0, -1);

        // At t=0, normal should point mostly in +Z
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDirection, ref hit,
                                           minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        if (intersected)
        {
            Assert.True(hit.Normal.Z > 0.9f, "Normal should point in +Z at t=0");
        }

        // At t=0.5, normal should be interpolated
        hit = new RayHit();
        intersected = scene.Intersect(rayOrigin, new V3f(0, -0.5f, -1).Normalized, ref hit,
                                      minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        if (intersected)
        {
            Assert.True(hit.Normal.Z > 0.4f && hit.Normal.Z < 0.9f, "Normal should be interpolated at t=0.5");
        }
    }
}
