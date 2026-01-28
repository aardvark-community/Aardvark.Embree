using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

public class InstanceGeometryTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_IdentityTransform_IntersectsAtOrigin(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a triangle at origin in XY plane
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Create instance with identity transform
        using var instance = new InstanceGeometry(device, triangleGeom, Affine3f.Identity, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Ray from above pointing down at (0.25, 0.25)
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.25f, 0.25f, 1f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect instanced triangle");
        Assert.Equal(1f, hit.T, 2); // Hit at z=0, distance=1
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_TranslationTransform_IntersectsAtTranslatedPosition(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a triangle at origin
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Create instance with translation (2, 3, 0)
        var transform = Affine3f.Translation(2f, 3f, 0f);
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Ray should hit translated triangle at (2.25, 3.25)
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(2.25f, 3.25f, 1f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect translated instanced triangle");

        // Ray should miss at original position
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            rayOrigin: new V3f(0.25f, 0.25f, 1f),
            rayDirection: new V3f(0, 0, -1),
            ref hit2
        );

        Assert.False(intersected2, "Ray should NOT intersect at original position");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_ScaleTransform_ScalesGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a small triangle
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(0.1f, 0, 0),
            new V3f(0, 0.1f, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Scale by 10x
        var transform = Affine3f.Scale(10f);
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Ray should hit scaled triangle at (0.5, 0.5) which is inside scaled bounds
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.5f, 0.5f, 1f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect scaled instanced triangle");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_RotationTransform_RotatesGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create a triangle along X axis
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0.5f, 0.5f, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Rotate 90 degrees around Z axis
        var transform = Affine3f.Rotation(V3f.OOI, (float)(Math.PI / 2));
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // After rotation, triangle should be along Y axis
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(-0.25f, 0.5f, 1f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect rotated instanced triangle");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_MultipleInstances_EachIntersectsIndependently(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Create base triangle
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Create three instances at different positions
        using var instance1 = new InstanceGeometry(device, triangleGeom, Affine3f.Translation(0, 0, 0), quality);
        using var instance2 = new InstanceGeometry(device, triangleGeom, Affine3f.Translation(2, 0, 0), quality);
        using var instance3 = new InstanceGeometry(device, triangleGeom, Affine3f.Translation(0, 2, 0), quality);

        scene.AttachGeometry(instance1);
        scene.AttachGeometry(instance2);
        scene.AttachGeometry(instance3);
        scene.Commit();

        // Test each instance
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(new V3f(0.25f, 0.25f, 1f), new V3f(0, 0, -1), ref hit1);
        Assert.True(intersected1, "Should hit instance 1");

        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(2.25f, 0.25f, 1f), new V3f(0, 0, -1), ref hit2);
        Assert.True(intersected2, "Should hit instance 2");

        var hit3 = new RayHit();
        bool intersected3 = scene.Intersect(new V3f(0.25f, 2.25f, 1f), new V3f(0, 0, -1), ref hit3);
        Assert.True(intersected3, "Should hit instance 3");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceGeometry_CanUpdateTransform(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: true);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);
        using var instance = new InstanceGeometry(device, triangleGeom, Affine3f.Identity, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Initial position
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(new V3f(0.25f, 0.25f, 1f), new V3f(0, 0, -1), ref hit1);
        Assert.True(intersected1, "Should hit at origin");

        // Update transform
        instance.Transform = Affine3f.Translation(5f, 5f, 0f);
        scene.Commit();

        // New position
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(5.25f, 5.25f, 1f), new V3f(0, 0, -1), ref hit2);
        Assert.True(intersected2, "Should hit at new position");

        // Old position should miss
        var hit3 = new RayHit();
        bool intersected3 = scene.Intersect(new V3f(0.25f, 0.25f, 1f), new V3f(0, 0, -1), ref hit3);
        Assert.False(intersected3, "Should NOT hit at old position");
    }

    [Fact]
    public void InstanceGeometry_CanCreateWithDifferentBuildQualities()
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);

        using var instanceHigh = new InstanceGeometry(device, triangleGeom, Affine3f.Identity, RTCBuildQuality.High);
        using var instanceMedium = new InstanceGeometry(device, triangleGeom, Affine3f.Identity, RTCBuildQuality.Medium);
        using var instanceLow = new InstanceGeometry(device, triangleGeom, Affine3f.Identity, RTCBuildQuality.Low);

        Assert.NotEqual(IntPtr.Zero, instanceHigh.Handle);
        Assert.NotEqual(IntPtr.Zero, instanceMedium.Handle);
        Assert.NotEqual(IntPtr.Zero, instanceLow.Handle);
    }
}
