using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

public class DiscPrimitiveTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateDiscGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(1, 0, 0), 0.3f),
            new Point(new V3f(0, 1, 0), 0.4f)
        };

        using var disc = new DiscGeometry(device, points, quality);

        Assert.NotEqual(IntPtr.Zero, disc.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscGeometry_CanIntersect(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var disc = new DiscGeometry(device, points, quality);

        scene.AttachGeometry(disc);
        scene.Commit();

        // Ray from above pointing down, slightly off-center to avoid center singularity
        // Disc points in Embree are view-oriented (billboarded) flat surfaces
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.1f, 0.1f, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect disc");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscGeometry_RespectRadius(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var disc = new DiscGeometry(device, points, quality);

        scene.AttachGeometry(disc);
        scene.Commit();

        // Ray inside radius should hit
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(0.3f, 0, 1),
            new V3f(0, 0, -1),
            ref hit1
        );
        Assert.True(intersected1, "Ray inside radius should hit");

        // Ray outside radius should miss
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            new V3f(0.6f, 0, 1),
            new V3f(0, 0, -1),
            ref hit2
        );
        Assert.False(intersected2, "Ray outside radius should miss");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateSphereGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(1, 0, 0), 0.3f),
            new Point(new V3f(0, 1, 0), 0.4f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        Assert.NotEqual(IntPtr.Zero, sphere.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_CanIntersect(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Ray from any direction should hit sphere
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 2),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect sphere");
        Assert.InRange(hit.T, 0.9f, 1.1f); // Should hit at distance ~1 (2 - radius)
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_IntersectsFromAllDirections(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Test from +Z
        var hit1 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, 2), new V3f(0, 0, -1), ref hit1));

        // Test from -Z
        var hit2 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, -2), new V3f(0, 0, 1), ref hit2));

        // Test from +X
        var hit3 = new RayHit();
        Assert.True(scene.Intersect(new V3f(2, 0, 0), new V3f(-1, 0, 0), ref hit3));

        // Test from -X
        var hit4 = new RayHit();
        Assert.True(scene.Intersect(new V3f(-2, 0, 0), new V3f(1, 0, 0), ref hit4));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateOrientedDiscGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 0.5f, new V3f(0, 0, 1)),
            new OrientedPoint(new V3f(1, 0, 0), 0.3f, new V3f(1, 0, 0)),
            new OrientedPoint(new V3f(0, 1, 0), 0.4f, new V3f(0, 1, 0))
        };

        using var orientedDisc = new OrientedDiscGeometry(device, points, quality);

        Assert.NotEqual(IntPtr.Zero, orientedDisc.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OrientedDiscGeometry_CanIntersect(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 1.0f, new V3f(0, 0, 1))
        };

        using var orientedDisc = new OrientedDiscGeometry(device, points, quality);

        scene.AttachGeometry(orientedDisc);
        scene.Commit();

        // Ray from above pointing down should hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect oriented disc");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OrientedDiscGeometry_RespectsOrientation(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Disc oriented in XY plane (normal along Z)
        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 1.0f, new V3f(0, 0, 1))
        };

        using var orientedDisc = new OrientedDiscGeometry(device, points, quality);

        scene.AttachGeometry(orientedDisc);
        scene.Commit();

        // Ray perpendicular to disc should hit
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(0, 0, 1),
            new V3f(0, 0, -1),
            ref hit1
        );
        Assert.True(intersected1, "Perpendicular ray should hit");

        // Ray parallel to disc should miss (or graze edge)
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            new V3f(-2, 0, 0),
            new V3f(1, 0, 0),
            ref hit2
        );
        Assert.False(intersected2, "Parallel ray should miss");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscGeometry_MultipleDiscs(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(2, 0, 0), 0.5f),
            new Point(new V3f(0, 2, 0), 0.5f)
        };

        using var disc = new DiscGeometry(device, points, quality);

        scene.AttachGeometry(disc);
        scene.Commit();

        // Test each disc
        var hit1 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, -1), ref hit1));

        var hit2 = new RayHit();
        Assert.True(scene.Intersect(new V3f(2, 0, 1), new V3f(0, 0, -1), ref hit2));

        var hit3 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 2, 1), new V3f(0, 0, -1), ref hit3));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_MultipleSpheres(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(2, 0, 0), 0.5f),
            new Point(new V3f(0, 2, 0), 0.5f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Test each sphere
        var hit1 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, -1), ref hit1));

        var hit2 = new RayHit();
        Assert.True(scene.Intersect(new V3f(2, 0, 1), new V3f(0, 0, -1), ref hit2));

        var hit3 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 2, 1), new V3f(0, 0, -1), ref hit3));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_TangentRay(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Ray grazing the top of the sphere (tangent at Y=1)
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(-2, 1, 0),
            rayDirection: new V3f(1, 0, 0).Normalized,
            ref hit
        );

        Assert.True(intersected, "Tangent ray should intersect sphere");
        Assert.InRange(hit.T, 1.9f, 2.1f); // Should hit at distance ~2
        var hitPoint = new V3f(-2, 1, 0) + new V3f(1, 0, 0) * hit.T;
        Assert.InRange(hitPoint.Y, 0.99f, 1.01f); // Hit point should be at top of sphere
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_InteriorOrigin(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 2.0f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Ray starting inside the sphere
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 0),
            rayDirection: new V3f(1, 0, 0),
            ref hit
        );

        Assert.True(intersected, "Ray from sphere interior should intersect");
        Assert.InRange(hit.T, 1.9f, 2.1f); // Should hit at radius distance
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_NearMiss(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Ray passing just above the sphere (should miss)
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            rayOrigin: new V3f(-2, 1.01f, 0),
            rayDirection: new V3f(1, 0, 0),
            ref hit1
        );
        Assert.False(intersected1, "Ray just above sphere should miss");

        // Ray passing just below the tangent (should hit)
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            rayOrigin: new V3f(-2, 0.99f, 0),
            rayDirection: new V3f(1, 0, 0),
            ref hit2
        );
        Assert.True(intersected2, "Ray just below tangent should hit");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_VariableRadii(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(2, 0, 0), 1.0f),
            new Point(new V3f(5, 0, 0), 1.5f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Test small sphere (radius 0.5)
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(0, 0, 2),
            new V3f(0, 0, -1),
            ref hit1
        );
        Assert.True(intersected1);
        Assert.InRange(hit1.T, 1.4f, 1.6f); // 2 - 0.5 = 1.5

        // Test medium sphere (radius 1.0)
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            new V3f(2, 0, 2),
            new V3f(0, 0, -1),
            ref hit2
        );
        Assert.True(intersected2);
        Assert.InRange(hit2.T, 0.9f, 1.1f); // 2 - 1.0 = 1.0

        // Test large sphere (radius 1.5)
        var hit3 = new RayHit();
        bool intersected3 = scene.Intersect(
            new V3f(5, 0, 2),
            new V3f(0, 0, -1),
            ref hit3
        );
        Assert.True(intersected3);
        Assert.InRange(hit3.T, 0.4f, 0.6f); // 2 - 1.5 = 0.5
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_EdgeDistanceCalculation(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Ray from exactly distance 3 away
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 3),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect sphere");
        Assert.InRange(hit.T, 1.9f, 2.1f); // Distance to surface: 3 - radius(1) = 2

        // Validate hit point is on sphere surface
        var hitPoint = new V3f(0, 0, 3) + new V3f(0, 0, -1) * hit.T;
        var distFromCenter = hitPoint.Length;
        Assert.InRange(distFromCenter, 0.99f, 1.01f); // Should be at radius 1.0
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_NormalValidationAtTangent(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Tangent ray at the top
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(-2, 1, 0),
            rayDirection: new V3f(1, 0, 0),
            ref hit
        );

        Assert.True(intersected, "Tangent ray should intersect");

        // Normal at tangent point should point upward (Y direction)
        var normal = hit.Normal.Normalized;
        Assert.InRange(normal.Y, 0.99f, 1.01f); // Y component should be ~1
        Assert.InRange(Math.Abs(normal.X), 0f, 0.1f); // X component should be ~0
        Assert.InRange(Math.Abs(normal.Z), 0f, 0.1f); // Z component should be ~0
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_DiagonalRayIntersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Diagonal ray through center
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(-2, -2, -2),
            rayDirection: new V3f(1, 1, 1).Normalized,
            ref hit
        );

        Assert.True(intersected, "Diagonal ray should intersect sphere");

        // Validate hit point is on sphere surface
        var rayDir = new V3f(1, 1, 1).Normalized;
        var hitPoint = new V3f(-2, -2, -2) + rayDir * hit.T;
        var distFromCenter = hitPoint.Length;
        Assert.InRange(distFromCenter, 0.99f, 1.01f);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscGeometry_CanUpdate(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: true);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var disc = new DiscGeometry(device, points, quality);

        scene.AttachGeometry(disc);
        scene.Commit();

        // Initial position
        var hit1 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, -1), ref hit1));

        // Update position
        var updatedPoints = new Point[]
        {
            new Point(new V3f(5, 0, 0), 0.5f)
        };

        disc.UpdatePoints((ReadOnlySpan<Point>)updatedPoints);
        disc.UpdateBuffer(RTCBufferType.Vertex);
        disc.Commit();
        scene.Commit();

        // New position
        var hit2 = new RayHit();
        Assert.True(scene.Intersect(new V3f(5, 0, 1), new V3f(0, 0, -1), ref hit2));

        // Old position should miss
        var hit3 = new RayHit();
        Assert.False(scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, -1), ref hit3));
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_CanUpdate(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: true);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        scene.AttachGeometry(sphere);
        scene.Commit();

        // Update radius
        var updatedPoints = new Point[]
        {
            new Point(new V3f(0, 0, 0), 2.0f)
        };

        sphere.UpdatePoints((ReadOnlySpan<Point>)updatedPoints);
        sphere.UpdateBuffer(RTCBufferType.Vertex);
        sphere.Commit();
        scene.Commit();

        // Ray that was outside old radius should now hit
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            new V3f(1.5f, 0, 0),
            new V3f(-1, 0, 0),
            ref hit
        );

        Assert.True(intersected, "Ray should hit expanded sphere");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OrientedDiscGeometry_CanUpdate(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 0.5f, new V3f(0, 0, 1))
        };

        using var orientedDisc = new OrientedDiscGeometry(device, points, quality);

        // Update positions
        var updatedPositions = new Point[]
        {
            new Point(new V3f(1, 1, 1), 0.7f)
        };

        orientedDisc.UpdatePositions((ReadOnlySpan<Point>)updatedPositions);

        // Update normals
        var updatedNormals = new V3f[]
        {
            new V3f(1, 0, 0).Normalized
        };

        orientedDisc.UpdateNormals((ReadOnlySpan<V3f>)updatedNormals);

        orientedDisc.UpdateBuffer(RTCBufferType.Vertex);
        orientedDisc.UpdateBuffer(RTCBufferType.Normal);
        orientedDisc.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscGeometry_SetMask(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var disc = new DiscGeometry(device, points, quality);

        disc.SetMask(0xFF);

        Assert.NotEqual(IntPtr.Zero, disc.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_SetMask(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        sphere.SetMask(0xFF);

        Assert.NotEqual(IntPtr.Zero, sphere.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OrientedDiscGeometry_SetMask(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 0.5f, new V3f(0, 0, 1))
        };

        using var orientedDisc = new OrientedDiscGeometry(device, points, quality);

        orientedDisc.SetMask(0xFF);

        Assert.NotEqual(IntPtr.Zero, orientedDisc.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscGeometry_DirectPointerAccess(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var disc = new DiscGeometry(device, points, quality);

        unsafe
        {
            var ptr = disc.GetPointDataPointer();
            Assert.True(ptr != null);

            // Modify first point
            ptr[0] = new Point(new V3f(1, 1, 1), 1.0f);
        }

        disc.UpdateBuffer(RTCBufferType.Vertex);
        disc.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_DirectPointerAccess(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var sphere = new SphereGeometry(device, points, quality);

        unsafe
        {
            var ptr = sphere.GetPointDataPointer();
            Assert.True(ptr != null);

            // Modify first point
            ptr[0] = new Point(new V3f(1, 1, 1), 1.0f);
        }

        sphere.UpdateBuffer(RTCBufferType.Vertex);
        sphere.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OrientedDiscGeometry_DirectPointerAccess(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 0.5f, new V3f(0, 0, 1))
        };

        using var orientedDisc = new OrientedDiscGeometry(device, points, quality);

        unsafe
        {
            var posPtr = orientedDisc.GetPositionDataPointer();
            var normPtr = orientedDisc.GetNormalDataPointer();

            Assert.True(posPtr != null);
            Assert.True(normPtr != null);

            // Modify
            posPtr[0] = new Point(new V3f(1, 1, 1), 1.0f);
            normPtr[0] = new V3f(1, 0, 0);
        }

        orientedDisc.UpdateBuffer(RTCBufferType.Vertex);
        orientedDisc.UpdateBuffer(RTCBufferType.Normal);
        orientedDisc.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscGeometry_ProperDisposal(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        var disc = new DiscGeometry(device, points, quality);
        var handle = disc.Handle;

        Assert.NotEqual(IntPtr.Zero, handle);

        disc.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => disc.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SphereGeometry_ProperDisposal(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        var sphere = new SphereGeometry(device, points, quality);
        var handle = sphere.Handle;

        Assert.NotEqual(IntPtr.Zero, handle);

        sphere.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => sphere.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OrientedDiscGeometry_ProperDisposal(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 0.5f, new V3f(0, 0, 1))
        };

        var orientedDisc = new OrientedDiscGeometry(device, points, quality);
        var handle = orientedDisc.Handle;

        Assert.NotEqual(IntPtr.Zero, handle);

        orientedDisc.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => orientedDisc.Handle);
    }
}
