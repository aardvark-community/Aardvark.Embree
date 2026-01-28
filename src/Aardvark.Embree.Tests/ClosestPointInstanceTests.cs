using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests documenting the Embree 4 API limitation that RTC_GEOMETRY_TYPE_INSTANCE does not support point queries.
/// These tests verify that point queries correctly return no results for instance geometries,
/// while ray queries (Intersect/Occluded) work correctly with instances.
/// </summary>
/// <remarks>
/// EMBREE 4 API LIMITATION:
/// rtcSetGeometryPointQueryFunction is NOT supported for RTC_GEOMETRY_TYPE_INSTANCE.
/// Only RTC_GEOMETRY_TYPE_USER geometries support point query callbacks.
/// This is documented in the Embree API reference and verified by these tests.
///
/// Reference: https://man.archlinux.org/man/RTC_GEOMETRY_TYPE_INSTANCE.3embree3.en
/// Supported functions for instance geometries:
/// - rtcSetGeometryInstancedScene
/// - rtcSetGeometryTransform
/// - rtcSetGeometryTimeStepCount
/// (rtcSetGeometryPointQueryFunction is NOT listed)
/// </remarks>
public class ClosestPointInstanceTests
{
    [Theory(DisplayName = "Point query on instance returns invalid (Embree limitation), but ray query works")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_InstancedTriangle_Identity_ReturnsInvalid(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);
        using var instance = new InstanceGeometry(device, triangleGeom, Affine3f.Identity, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Point query - does NOT work with instances
        var queryPoint = new V3f(0.25f, 0.25f, 1f);
        var pointResult = scene.GetClosestPoint(queryPoint);
        Assert.False(pointResult.IsValid,
            "Point queries do not traverse RTC_GEOMETRY_TYPE_INSTANCE (Embree API limitation)");

        // Ray query - DOES work with instances
        var rayHit = new RayHit();
        bool intersected = scene.Intersect(queryPoint, new V3f(0, 0, -1), ref rayHit);
        Assert.True(intersected, "Ray queries work correctly with instances");
        Assert.InRange(rayHit.T, 0.9f, 1.1f); // Should hit at approximately t=1
    }

    [Theory(DisplayName = "Point query on translated instance returns invalid, but ray query works")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_InstancedTriangle_Translation_ReturnsInvalid(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Translate by (5, 5, 0)
        var transform = Affine3f.Translation(5f, 5f, 0f);
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Point query at translated location - does NOT work
        var queryPoint = new V3f(5.25f, 5.25f, 1f);
        var pointResult = scene.GetClosestPoint(queryPoint);
        Assert.False(pointResult.IsValid,
            "Point queries do not traverse instances regardless of transform");

        // Ray query at translated location - DOES work
        var rayHit = new RayHit();
        bool intersected = scene.Intersect(queryPoint, new V3f(0, 0, -1), ref rayHit);
        Assert.True(intersected, "Ray queries correctly apply instance transforms");

        // Verify hit point is in world space (transformed)
        var hitPoint = queryPoint + new V3f(0, 0, -1) * rayHit.T;
        Assert.InRange(hitPoint.X, 5.2f, 5.3f);
        Assert.InRange(hitPoint.Y, 5.2f, 5.3f);
    }

    [Theory(DisplayName = "Point query on scaled instance returns invalid, but ray query works")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_InstancedTriangle_Scale_ReturnsInvalid(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Scale by 2x
        var transform = Affine3f.Scale(2f);
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Point query on scaled triangle - does NOT work
        var queryPoint = new V3f(1f, 1f, 1f);
        var pointResult = scene.GetClosestPoint(queryPoint);
        Assert.False(pointResult.IsValid, "Point queries do not work with scaled instances");

        // Ray query on scaled triangle - DOES work
        var rayHit = new RayHit();
        bool intersected = scene.Intersect(queryPoint, new V3f(0, 0, -1), ref rayHit);
        Assert.True(intersected, "Ray queries correctly handle scaled instances");
    }

    [Theory(DisplayName = "Verify point query limitation vs ray query functionality")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_VsIntersect_InstancedGeometry_ShowsLimitation(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Translate triangle
        var transform = Affine3f.Translation(5f, 5f, 0f);
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Ray from above pointing down
        var rayOrigin = new V3f(5.25f, 5.25f, 1f);
        var rayDir = new V3f(0, 0, -1);

        // Ray query works
        var hit = new RayHit();
        bool intersected = scene.Intersect(rayOrigin, rayDir, ref hit);
        Assert.True(intersected, "Ray queries work with instances");

        // Point query does not work
        var closestResult = scene.GetClosestPoint(rayOrigin);
        Assert.False(closestResult.IsValid,
            "Point queries do not work with instances - this demonstrates the API limitation");
    }

    [Theory(DisplayName = "Multiple instances - point queries return invalid for all")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_MultipleInstances_AllReturnInvalid(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Create two instances at different locations
        var transform1 = Affine3f.Translation(0f, 0f, 0f);
        var transform2 = Affine3f.Translation(5f, 0f, 0f);

        using var instance1 = new InstanceGeometry(device, triangleGeom, transform1, quality);
        using var instance2 = new InstanceGeometry(device, triangleGeom, transform2, quality);

        scene.AttachGeometry(instance1);
        scene.AttachGeometry(instance2);
        scene.Commit();

        // Point queries don't work for either instance
        var result1 = scene.GetClosestPoint(new V3f(0.5f, 0.5f, 1f));
        Assert.False(result1.IsValid, "Point query does not find first instance");

        var result2 = scene.GetClosestPoint(new V3f(5.5f, 0.5f, 1f));
        Assert.False(result2.IsValid, "Point query does not find second instance");

        // But ray queries work for both
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(new V3f(0.5f, 0.5f, 1f), new V3f(0, 0, -1), ref hit1);
        Assert.True(intersected1, "Ray query finds first instance");

        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(new V3f(5.5f, 0.5f, 1f), new V3f(0, 0, -1), ref hit2);
        Assert.True(intersected2, "Ray query finds second instance");
    }

    [Theory(DisplayName = "Point query with max radius on instance - still returns invalid")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_WithMaxRadius_InstancedGeometry_ReturnsInvalid(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        var transform = Affine3f.Translation(10f, 10f, 0f);
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Point queries don't work regardless of max radius
        var result1 = scene.GetClosestPoint(new V3f(0, 0, 0), maxRadius: 1f);
        Assert.False(result1.IsValid, "Point query with small radius does not find instance");

        var result2 = scene.GetClosestPoint(new V3f(0, 0, 0), maxRadius: 20f);
        Assert.False(result2.IsValid, "Point query with large radius does not find instance (API limitation)");

        // But ray queries still work
        var hit = new RayHit();
        bool intersected = scene.Intersect(new V3f(10.25f, 10.25f, 1f), new V3f(0, 0, -1), ref hit);
        Assert.True(intersected, "Ray queries work with translated instances");
    }

    [Theory(DisplayName = "Point query on rotated instance returns invalid")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_InstancedTriangle_Rotation_ReturnsInvalid(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Triangle in XY plane
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Rotate 90 degrees around Z axis
        var transform = Affine3f.Rotation(V3f.OOI, (float)Constant.PiHalf);
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Point query does not work
        var queryPoint = new V3f(-0.25f, 0.25f, 1f);
        var pointResult = scene.GetClosestPoint(queryPoint);
        Assert.False(pointResult.IsValid, "Point query does not work with rotated instances");

        // Ray query works
        var rayHit = new RayHit();
        bool intersected = scene.Intersect(queryPoint, new V3f(0, 0, -1), ref rayHit);
        Assert.True(intersected, "Ray query works with rotated instances");
    }

    [Theory(DisplayName = "Point query on instance with combined transform returns invalid")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void GetClosestPoint_InstancedTriangle_CombinedTransform_ReturnsInvalid(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, quality);

        // Combine scale and translation (simpler than including rotation)
        var transform = Affine3f.Translation(5f, 5f, 0f) * Affine3f.Scale(2f);
        using var instance = new InstanceGeometry(device, triangleGeom, transform, quality);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Point query does not work
        // Triangle center after scale(2x) then translate(5,5): center at (0.33,0.33) -> (0.66,0.66) -> (5.66,5.66)
        var queryPoint = new V3f(5.66f, 5.66f, 1f);
        var pointResult = scene.GetClosestPoint(queryPoint);
        Assert.False(pointResult.IsValid,
            "Point query does not work even with complex transforms (API limitation)");

        // Ray query works
        var rayHit = new RayHit();
        bool intersected = scene.Intersect(queryPoint, new V3f(0, 0, -1), ref rayHit);
        Assert.True(intersected, "Ray query correctly handles complex transforms");
    }
}
