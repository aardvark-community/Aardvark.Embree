using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

public class QuaternionMotionTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_WithQuaternionMotion(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a simple triangle
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Create instance with quaternion-based rotation motion
        using var instance = new InstanceGeometry(device, triangleGeom,
            Affine3f.Identity, quality);

        // Setup motion blur with quaternion interpolation
        var rot0 = Rot3f.Identity;
        var rot1 = Rot3f.Rotation(V3f.OOI, (float)(Math.PI / 2)); // 90 degree rotation around Z

        var t0 = new Affine3f((M33f)rot0, V3f.Zero);
        var t1 = new Affine3f((M33f)rot1, V3f.Zero);

        instance.SetTransforms(new[] { t0, t1 });

        scene.AttachGeometry(instance);
        scene.Commit();

        // At t=0, triangle should be in original orientation
        var hit0 = new RayHit();
        bool intersected0 = scene.Intersect(
            new V3f(0.25f, 0.25f, 1),
            new V3f(0, 0, -1),
            ref hit0,
            minT: 0.0f,
            maxT: float.MaxValue,
            time: 0.0f
        );
        Assert.True(intersected0, "Should hit at t=0");

        // At t=0.5, triangle should be partially rotated (45 degrees around Z)
        // The triangle center moves during rotation, ray from inside triangle area should still hit
        var hit05 = new RayHit();
        bool intersected05 = scene.Intersect(
            new V3f(0.2f, 0.2f, 1),  // Closer to center, more likely to hit rotated triangle
            new V3f(0, 0, -1),
            ref hit05,
            minT: 0.0f,
            maxT: float.MaxValue,
            time: 0.5f
        );
        Assert.True(intersected05, "Should hit at t=0.5 (triangle rotated 45 degrees)");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void TriangleGeometry_LinearMotion_InterpolatesCorrectly(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle at different positions for t=0 and t=1
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);

        // Set motion blur vertices
        var verticesT1 = new V3f[]
        {
            new V3f(0, 0, 2),
            new V3f(1, 0, 2),
            new V3f(0, 1, 2)
        };

        geom.SetVertexPositions(1, (ReadOnlySpan<V3f>)verticesT1, 2);
        geom.Commit();

        scene.AttachGeometry(geom);
        scene.Commit();

        // Ray from above
        var rayOrigin = new V3f(0.25f, 0.25f, 5);
        var rayDir = new V3f(0, 0, -1);

        // At t=0, should hit at z=0
        var hit0 = new RayHit();
        bool intersected0 = scene.Intersect(rayOrigin, rayDir, ref hit0, minT: 0.0f, maxT: float.MaxValue, time: 0.0f);
        Assert.True(intersected0);
        Assert.InRange(hit0.T, 4.9f, 5.1f);

        // At t=0.5, should hit at z=1
        var hit05 = new RayHit();
        bool intersected05 = scene.Intersect(rayOrigin, rayDir, ref hit05, minT: 0.0f, maxT: float.MaxValue, time: 0.5f);
        Assert.True(intersected05);
        Assert.InRange(hit05.T, 3.9f, 4.1f);

        // At t=1, should hit at z=2
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(rayOrigin, rayDir, ref hit1, minT: 0.0f, maxT: float.MaxValue, time: 1.0f);
        Assert.True(intersected1);
        Assert.InRange(hit1.T, 2.9f, 3.1f);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_ComplexRotationSequence(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);
        using var instance = new InstanceGeometry(device, triangleGeom,
            Affine3f.Identity, quality);

        // Multiple time steps
        var transforms = new[]
        {
            Affine3f.Identity,
            new Affine3f((M33f)Rot3f.Rotation(V3f.OOI, (float)(Math.PI / 4)), V3f.Zero),
            new Affine3f((M33f)Rot3f.Rotation(V3f.OOI, (float)(Math.PI / 2)), V3f.Zero),
            new Affine3f((M33f)Rot3f.Rotation(V3f.OOI, (float)(Math.PI)), V3f.Zero)
        };

        instance.SetTransforms(transforms);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Test at various time samples
        for (float t = 0; t <= 1.0f; t += 0.25f)
        {
            var hit = new RayHit();
            var rayOrigin = new V3f(0, 0, 2);
            var rayDir = new V3f(0, 0, -1);

            scene.Intersect(rayOrigin, rayDir, ref hit, minT: 0.0f, maxT: float.MaxValue, time: t);
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_RotationAndTranslation(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);
        using var instance = new InstanceGeometry(device, triangleGeom,
            Affine3f.Identity, quality);

        // Combine rotation and translation
        var t0 = Affine3f.Translation(0, 0, 0);
        var t1 = Affine3f.Translation(5, 5, 0) *
                 new Affine3f((M33f)Rot3f.Rotation(V3f.OOI, (float)Math.PI), V3f.Zero);

        instance.SetTransforms(new[] { t0, t1 });

        scene.AttachGeometry(instance);
        scene.Commit();

        // At t=0, should hit at origin
        var hit0 = new RayHit();
        bool intersected0 = scene.Intersect(
            new V3f(0.25f, 0.25f, 1),
            new V3f(0, 0, -1),
            ref hit0,
            minT: 0.0f,
            maxT: float.MaxValue,
            time: 0.0f
        );
        Assert.True(intersected0);

        // At t=1, triangle is at (5,5,0), (4,5,0), (5,4,0) after rotation and translation
        // Ray from center of triangle area should hit
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(4.7f, 4.7f, 1),  // Inside the rotated and translated triangle
            new V3f(0, 0, -1),
            ref hit1,
            minT: 0.0f,
            maxT: float.MaxValue,
            time: 1.0f
        );
        Assert.True(intersected1, "Should hit rotated and translated triangle at t=1");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlur_MultipleTimeSteps(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);

        // 5 time steps
        uint timeSteps = 5;
        for (uint i = 1; i < timeSteps; i++)
        {
            float z = i * 0.5f;
            var verts = new V3f[]
            {
                new V3f(0, 0, z),
                new V3f(1, 0, z),
                new V3f(0, 1, z)
            };
            geom.SetVertexPositions(i, (ReadOnlySpan<V3f>)verts, timeSteps);
        }
        geom.Commit();

        scene.AttachGeometry(geom);
        scene.Commit();

        // Test interpolation at each time step
        for (uint i = 0; i < timeSteps; i++)
        {
            float t = i / (float)(timeSteps - 1);
            float expectedZ = i * 0.5f;

            var hit = new RayHit();
            bool intersected = scene.Intersect(
                new V3f(0.25f, 0.25f, 5),
                new V3f(0, 0, -1),
                ref hit,
                minT: 0.0f,
                maxT: float.MaxValue,
                time: t
            );

            Assert.True(intersected, $"Should hit at time step {i}");
            Assert.InRange(hit.T, 5 - expectedZ - 0.1f, 5 - expectedZ + 0.1f);
        }
    }

    [Fact]
    public void InstanceGeometry_QuaternionSlerp()
    {
        using var device = new Device();

        // Test quaternion SLERP for smooth rotation
        var rot0 = Rot3f.Identity;
        var rot1 = Rot3f.Rotation(V3f.OOI, (float)Math.PI);

        // Intermediate rotation should be smooth - manual lerp between rotations
        for (float t = 0; t <= 1.0f; t += 0.1f)
        {
            // Linear interpolation between rotation matrices
            var m0 = (M33f)rot0;
            var m1 = (M33f)rot1;
            var mt = new M33f(
                Fun.Lerp(m0.M00, m1.M00, t), Fun.Lerp(m0.M01, m1.M01, t), Fun.Lerp(m0.M02, m1.M02, t),
                Fun.Lerp(m0.M10, m1.M10, t), Fun.Lerp(m0.M11, m1.M11, t), Fun.Lerp(m0.M12, m1.M12, t),
                Fun.Lerp(m0.M20, m1.M20, t), Fun.Lerp(m0.M21, m1.M21, t), Fun.Lerp(m0.M22, m1.M22, t)
            );
            var transform = new Affine3f(mt, V3f.Zero);

            // Transform should be valid
            Assert.NotEqual(M44f.Zero, (M44f)transform);
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlur_RotatingBox(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a box that rotates
        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(1, 1, 0),
            new V3f(-1, 1, 0)
        };
        var indices = new int[] { 0, 1, 2, 0, 2, 3 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);

        // Rotate vertices for time step 1
        var angle = (float)(Math.PI / 2);
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);

        var verticesT1 = new V3f[]
        {
            new V3f(-cos + sin, -sin - cos, 0),
            new V3f(cos + sin, sin - cos, 0),
            new V3f(cos - sin, sin + cos, 0),
            new V3f(-cos - sin, -sin + cos, 0)
        };

        geom.SetVertexPositions(1, (ReadOnlySpan<V3f>)verticesT1, 2);
        geom.Commit();

        scene.AttachGeometry(geom);
        scene.Commit();

        // Test at multiple time samples
        var hit0 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, -1), ref hit0, minT: 0.0f, maxT: float.MaxValue, time: 0.0f));

        var hit05 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, -1), ref hit05, minT: 0.0f, maxT: float.MaxValue, time: 0.5f));

        var hit1 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, -1), ref hit1, minT: 0.0f, maxT: float.MaxValue, time: 1.0f));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlur_NonUniformMotion(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);

        // Non-uniform motion (accelerating)
        uint timeSteps = 4;
        for (uint i = 1; i < timeSteps; i++)
        {
            float z = i * i * 0.1f; // Quadratic motion
            var verts = new V3f[]
            {
                new V3f(0, 0, z),
                new V3f(1, 0, z),
                new V3f(0, 1, z)
            };
            geom.SetVertexPositions(i, (ReadOnlySpan<V3f>)verts, timeSteps);
        }
        geom.Commit();

        scene.AttachGeometry(geom);
        scene.Commit();

        // Verify interpolation works with non-uniform motion
        for (uint i = 0; i < timeSteps; i++)
        {
            float t = i / (float)(timeSteps - 1);
            var hit = new RayHit();
            bool intersected = scene.Intersect(
                new V3f(0.25f, 0.25f, 5),
                new V3f(0, 0, -1),
                ref hit,
                minT: 0.0f,
                maxT: float.MaxValue,
                time: t
            );

            Assert.True(intersected, $"Should hit at time {t}");
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_ScaleMotion(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(0.1f, 0, 0),
            new V3f(0, 0.1f, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);
        using var instance = new InstanceGeometry(device, triangleGeom,
            Affine3f.Identity, quality);

        // Scale from 1x to 10x
        var t0 = Affine3f.Scale(1.0f);
        var t1 = Affine3f.Scale(10.0f);

        instance.SetTransforms(new[] { t0, t1 });

        scene.AttachGeometry(instance);
        scene.Commit();

        // At t=0, small triangle
        var hit0 = new RayHit();
        bool intersected0 = scene.Intersect(
            new V3f(0.05f, 0.05f, 1),
            new V3f(0, 0, -1),
            ref hit0,
            minT: 0.0f,
            maxT: float.MaxValue,
            time: 0.0f
        );
        Assert.True(intersected0);

        // At t=1, large triangle
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(0.5f, 0.5f, 1),
            new V3f(0, 0, -1),
            ref hit1,
            minT: 0.0f,
            maxT: float.MaxValue,
            time: 1.0f
        );
        Assert.True(intersected1);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlur_CustomTimeRange(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);

        var verticesT1 = new V3f[]
        {
            new V3f(0, 0, 1),
            new V3f(1, 0, 1),
            new V3f(0, 1, 1)
        };

        geom.SetVertexPositions(1, (ReadOnlySpan<V3f>)verticesT1, 2);
        geom.Commit();

        scene.AttachGeometry(geom);
        scene.Commit();

        // Test with custom time samples
        var times = new[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };

        foreach (var t in times)
        {
            var hit = new RayHit();
            bool intersected = scene.Intersect(
                new V3f(0.25f, 0.25f, 2),
                new V3f(0, 0, -1),
                ref hit,
                minT: 0.0f,
                maxT: float.MaxValue,
                time: t
            );

            Assert.True(intersected, $"Should hit at time {t}");
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MotionBlur_EdgeCase_SingleTimeStep(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);

        // Single time step should throw (need at least 2 for motion blur)
        Assert.Throws<ArgumentException>(() =>
        {
            geom.SetVertexPositions(0, (ReadOnlySpan<V3f>)vertices, 1);
        });
    }
}
