using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for ray intersection queries, hit detection, barycentric coordinates, and occlusion tests.
/// </summary>
public class IntersectionTests
{
    private static (Device, Scene) CreateSimpleScene(RTCBuildQuality quality = RTCBuildQuality.High)
    {
        var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };

        var indices = new int[] { 0, 1, 2 };

        var geometry = new TriangleGeometry(device, vertices, indices, quality);

        var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        return (device, scene);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect_RayHitsTriangle_ReturnsTrue(RTCBuildQuality quality)
    {
        var (device, scene) = CreateSimpleScene(quality);

        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);

        Assert.True(result);
        Assert.True(hit.T > 0.0f);
        Assert.Equal(0u, hit.GeometryId);

        scene.Dispose();
        device.Dispose();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect_RayMissesTriangle_ReturnsFalse(RTCBuildQuality quality)
    {
        var (device, scene) = CreateSimpleScene(quality);

        var hit = new RayHit();
        var rayOrigin = new V3f(10.0f, 10.0f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);

        Assert.False(result);

        scene.Dispose();
        device.Dispose();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect_RayWithTnearBeyondHit_ReturnsFalse(RTCBuildQuality quality)
    {
        var (device, scene) = CreateSimpleScene(quality);

        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 10.0f, float.MaxValue);

        Assert.False(result);

        scene.Dispose();
        device.Dispose();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Intersect_RayWithTfarBeforeHit_ReturnsFalse(RTCBuildQuality quality)
    {
        var (device, scene) = CreateSimpleScene(quality);

        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, 0.5f);

        Assert.False(result);

        scene.Dispose();
        device.Dispose();
    }

    [Fact]
    public void Intersect_ValidHit_ReturnsCorrectGeomID()
    {
        var device = new Device();

        var vertices1 = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };

        var vertices2 = new V3f[]
        {
            new V3f(2.0f, 0.0f, 0.0f),
            new V3f(3.0f, 0.0f, 0.0f),
            new V3f(2.0f, 1.0f, 0.0f)
        };

        var indices = new int[] { 0, 1, 2 };

        var geometry1 = new TriangleGeometry(device, vertices1, indices, RTCBuildQuality.High);
        var geometry2 = new TriangleGeometry(device, vertices2, indices, RTCBuildQuality.High);

        var scene = new Scene(device, RTCBuildQuality.High, false);
        var geomId1 = scene.AttachGeometry(geometry1);
        var geomId2 = scene.AttachGeometry(geometry2);
        scene.Commit();

        var hit1 = new RayHit();
        var result1 = scene.Intersect(new V3f(0.25f, 0.25f, 1.0f), new V3f(0.0f, 0.0f, -1.0f), ref hit1);

        Assert.True(result1);
        Assert.Equal(geomId1, hit1.GeometryId);

        var hit2 = new RayHit();
        var result2 = scene.Intersect(new V3f(2.25f, 0.25f, 1.0f), new V3f(0.0f, 0.0f, -1.0f), ref hit2);

        Assert.True(result2);
        Assert.Equal(geomId2, hit2.GeometryId);

        scene.Dispose();
        device.Dispose();
    }

    [Fact]
    public void Occluded_RayHitsTriangle_ReturnsTrue()
    {
        var (device, scene) = CreateSimpleScene();

        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Occluded(rayOrigin, rayDirection, 0.0f, float.MaxValue);

        Assert.True(result);

        scene.Dispose();
        device.Dispose();
    }

    [Fact]
    public void Occluded_RayMissesTriangle_ReturnsFalse()
    {
        var (device, scene) = CreateSimpleScene();

        var rayOrigin = new V3f(10.0f, 10.0f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Occluded(rayOrigin, rayDirection, 0.0f, float.MaxValue);

        Assert.False(result);

        scene.Dispose();
        device.Dispose();
    }

    [Fact]
    public void Occluded_RayWithTnearBeyondHit_ReturnsFalse()
    {
        var (device, scene) = CreateSimpleScene();

        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Occluded(rayOrigin, rayDirection, 10.0f, float.MaxValue);

        Assert.False(result);

        scene.Dispose();
        device.Dispose();
    }

    [Fact]
    public void Occluded_RayWithTfarBeforeHit_ReturnsFalse()
    {
        var (device, scene) = CreateSimpleScene();

        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Occluded(rayOrigin, rayDirection, 0.0f, 0.5f);

        Assert.False(result);

        scene.Dispose();
        device.Dispose();
    }

    [Fact]
    public void Intersect_MultipleRays_AllReturnExpectedResults()
    {
        var (device, scene) = CreateSimpleScene();

        var testCases = new[]
        {
            (Origin: new V3f(0.25f, 0.25f, 1.0f), Dir: new V3f(0.0f, 0.0f, -1.0f), ExpectHit: true),
            (Origin: new V3f(0.1f, 0.1f, 1.0f), Dir: new V3f(0.0f, 0.0f, -1.0f), ExpectHit: true),
            (Origin: new V3f(10.0f, 10.0f, 1.0f), Dir: new V3f(0.0f, 0.0f, -1.0f), ExpectHit: false),
            (Origin: new V3f(-1.0f, -1.0f, 1.0f), Dir: new V3f(0.0f, 0.0f, -1.0f), ExpectHit: false)
        };

        foreach (var testCase in testCases)
        {
            var hit = new RayHit();
            var result = scene.Intersect(testCase.Origin, testCase.Dir, ref hit, 0.0f, float.MaxValue);

            Assert.Equal(testCase.ExpectHit, result);
        }

        scene.Dispose();
        device.Dispose();
    }
}
