using Aardvark.Base;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.EdgeCases;

/// <summary>
/// Tests to verify actual behavior when ray origin contains NaN values.
///
/// Claim from AdvancedFeaturesExample.cs:42:
/// "NaN in origin -> tfar becomes NaN (miss)"
///
/// This test suite documents the ACTUAL behavior observed.
/// </summary>
public class NaNOriginTests
{
    private readonly ITestOutputHelper _output;

    public NaNOriginTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "Ray with NaN in origin X component")]
    public void RayWithNaNOriginX_ActualBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray with NaN in origin.X
        var origin = new V3f(float.NaN, 0.5f, -1.0f);
        var direction = new V3f(0, 0, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        // Document actual behavior
        _output.WriteLine($"=== NaN in Origin.X ===");
        _output.WriteLine($"Ray Origin: ({origin.X}, {origin.Y}, {origin.Z})");
        _output.WriteLine($"Ray Direction: ({direction.X}, {direction.Y}, {direction.Z})");
        _output.WriteLine($"Has Hit: {hasHit}");
        _output.WriteLine($"Hit.T: {hit.T}");
        _output.WriteLine($"Hit.T is NaN: {float.IsNaN(hit.T)}");
        _output.WriteLine($"Hit.T is Infinity: {float.IsInfinity(hit.T)}");
        if (hasHit)
        {
            _output.WriteLine($"Hit Normal: ({hit.Normal.X}, {hit.Normal.Y}, {hit.Normal.Z})");
            _output.WriteLine($"Hit GeometryId: {hit.GeometryId}");
        }

        // The claim states: "NaN in origin -> tfar becomes NaN (miss)"
        // Verify if tfar (hit.T) is NaN and we get a miss
        Assert.False(hasHit, "Expected miss when origin contains NaN");
    }

    [Fact(DisplayName = "Ray with NaN in origin Y component")]
    public void RayWithNaNOriginY_ActualBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray with NaN in origin.Y
        var origin = new V3f(0.5f, float.NaN, -1.0f);
        var direction = new V3f(0, 0, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        _output.WriteLine($"=== NaN in Origin.Y ===");
        _output.WriteLine($"Ray Origin: ({origin.X}, {origin.Y}, {origin.Z})");
        _output.WriteLine($"Ray Direction: ({direction.X}, {direction.Y}, {direction.Z})");
        _output.WriteLine($"Has Hit: {hasHit}");
        _output.WriteLine($"Hit.T: {hit.T}");
        _output.WriteLine($"Hit.T is NaN: {float.IsNaN(hit.T)}");
        _output.WriteLine($"Hit.T is Infinity: {float.IsInfinity(hit.T)}");
        if (hasHit)
        {
            _output.WriteLine($"Hit Normal: ({hit.Normal.X}, {hit.Normal.Y}, {hit.Normal.Z})");
            _output.WriteLine($"Hit GeometryId: {hit.GeometryId}");
        }

        Assert.False(hasHit, "Expected miss when origin contains NaN");
    }

    [Fact(DisplayName = "Ray with NaN in origin Z component")]
    public void RayWithNaNOriginZ_ActualBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray with NaN in origin.Z
        var origin = new V3f(0.5f, 0.5f, float.NaN);
        var direction = new V3f(0, 0, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        _output.WriteLine($"=== NaN in Origin.Z ===");
        _output.WriteLine($"Ray Origin: ({origin.X}, {origin.Y}, {origin.Z})");
        _output.WriteLine($"Ray Direction: ({direction.X}, {direction.Y}, {direction.Z})");
        _output.WriteLine($"Has Hit: {hasHit}");
        _output.WriteLine($"Hit.T: {hit.T}");
        _output.WriteLine($"Hit.T is NaN: {float.IsNaN(hit.T)}");
        _output.WriteLine($"Hit.T is Infinity: {float.IsInfinity(hit.T)}");
        if (hasHit)
        {
            _output.WriteLine($"Hit Normal: ({hit.Normal.X}, {hit.Normal.Y}, {hit.Normal.Z})");
            _output.WriteLine($"Hit GeometryId: {hit.GeometryId}");
        }

        Assert.False(hasHit, "Expected miss when origin contains NaN");
    }

    [Fact(DisplayName = "Ray with all NaN origin components")]
    public void RayWithAllNaNOrigin_ActualBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Ray with all NaN components in origin
        var origin = new V3f(float.NaN, float.NaN, float.NaN);
        var direction = new V3f(0, 0, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        _output.WriteLine($"=== All NaN in Origin ===");
        _output.WriteLine($"Ray Origin: ({origin.X}, {origin.Y}, {origin.Z})");
        _output.WriteLine($"Ray Direction: ({direction.X}, {direction.Y}, {direction.Z})");
        _output.WriteLine($"Has Hit: {hasHit}");
        _output.WriteLine($"Hit.T: {hit.T}");
        _output.WriteLine($"Hit.T is NaN: {float.IsNaN(hit.T)}");
        _output.WriteLine($"Hit.T is Infinity: {float.IsInfinity(hit.T)}");
        if (hasHit)
        {
            _output.WriteLine($"Hit Normal: ({hit.Normal.X}, {hit.Normal.Y}, {hit.Normal.Z})");
            _output.WriteLine($"Hit GeometryId: {hit.GeometryId}");
        }

        Assert.False(hasHit, "Expected miss when origin contains NaN");
    }

    [Fact(DisplayName = "Control test - valid ray should hit triangle")]
    public void ValidRay_ShouldHitTriangle()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Valid ray that should hit the triangle
        var origin = new V3f(0.5f, 0.5f, -1.0f);
        var direction = new V3f(0, 0, 1);
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        _output.WriteLine($"=== Control Test - Valid Ray ===");
        _output.WriteLine($"Ray Origin: ({origin.X}, {origin.Y}, {origin.Z})");
        _output.WriteLine($"Ray Direction: ({direction.X}, {direction.Y}, {direction.Z})");
        _output.WriteLine($"Has Hit: {hasHit}");
        _output.WriteLine($"Hit.T: {hit.T}");
        _output.WriteLine($"Hit.T is NaN: {float.IsNaN(hit.T)}");
        _output.WriteLine($"Hit Normal: ({hit.Normal.X}, {hit.Normal.Y}, {hit.Normal.Z})");
        _output.WriteLine($"Hit GeometryId: {hit.GeometryId}");

        Assert.True(hasHit, "Valid ray should hit the triangle");
        Assert.False(float.IsNaN(hit.T), "Valid hit should have non-NaN tfar");
        Assert.True(hit.T > 0, "Valid hit should have positive distance");
    }

    [Fact(DisplayName = "NaN origin with different direction")]
    public void NaNOrigin_DifferentDirection_ActualBehavior()
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var scene = new Scene(device, RTCBuildQuality.High, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // NaN origin with non-axis-aligned direction
        var origin = new V3f(float.NaN, 0.5f, -1.0f);
        var direction = new V3f(0.1f, 0.1f, 1.0f).Normalized;
        var hit = new RayHit();

        bool hasHit = scene.Intersect(origin, direction, ref hit);

        _output.WriteLine($"=== NaN Origin with Non-Axis-Aligned Direction ===");
        _output.WriteLine($"Ray Origin: ({origin.X}, {origin.Y}, {origin.Z})");
        _output.WriteLine($"Ray Direction: ({direction.X}, {direction.Y}, {direction.Z})");
        _output.WriteLine($"Has Hit: {hasHit}");
        _output.WriteLine($"Hit.T: {hit.T}");
        _output.WriteLine($"Hit.T is NaN: {float.IsNaN(hit.T)}");
        if (hasHit)
        {
            _output.WriteLine($"Hit Normal: ({hit.Normal.X}, {hit.Normal.Y}, {hit.Normal.Z})");
            _output.WriteLine($"Hit GeometryId: {hit.GeometryId}");
        }

        Assert.False(hasHit, "Expected miss when origin contains NaN, regardless of direction");
    }
}
