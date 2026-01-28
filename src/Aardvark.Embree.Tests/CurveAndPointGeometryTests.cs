using Aardvark.Base;
using Xunit;
using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree.Tests;

public class CurveGeometryTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateRoundBezierCurve(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 1, 0), 0.1f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f),
            new CurveVertex(new V3f(3, 1, 0), 0.1f)
        };

        var indices = new uint[] { 0 };

        using var curve = new RoundBezierCurveGeometry(device, vertices, indices, quality);

        Assert.NotEqual(IntPtr.Zero, curve.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateFlatBSplineCurve(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 0, 0), 0.1f),
            new CurveVertex(new V3f(2, 1, 0), 0.1f),
            new CurveVertex(new V3f(3, 1, 0), 0.1f)
        };

        var indices = new uint[] { 0 };

        using var curve = new FlatBSplineCurveGeometry(device, vertices, indices, quality);

        Assert.NotEqual(IntPtr.Zero, curve.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CurveGeometryInScene(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.2f),
            new CurveVertex(new V3f(1, 0, 0), 0.2f)
        };

        var indices = new uint[] { 0 };

        using var curve = new RoundLinearCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.5f, 0.0f, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateCurveFromSpan(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new CurveVertex[4]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 1, 0), 0.1f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f),
            new CurveVertex(new V3f(3, 1, 0), 0.1f)
        };

        var indices = new uint[1] { 0 };

        using var curve = new RoundBezierCurveGeometry(device, vertices, indices, quality);

        Assert.NotEqual(IntPtr.Zero, curve.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void RoundBezierCurve_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Simple horizontal Bezier curve with larger radius
        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.5f),
            new CurveVertex(new V3f(1, 0, 0), 0.5f),
            new CurveVertex(new V3f(2, 0, 0), 0.5f),
            new CurveVertex(new V3f(3, 0, 0), 0.5f)
        };

        var indices = new uint[] { 0 };

        using var curve = new RoundBezierCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        // Ray through middle of curve
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(1.5f, 0, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect Bezier curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void FlatBezierCurve_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Flat Bezier curve (ribbon-like)
        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.3f),
            new CurveVertex(new V3f(1, 0.5f, 0), 0.3f),
            new CurveVertex(new V3f(2, 0.5f, 0), 0.3f),
            new CurveVertex(new V3f(3, 0, 0), 0.3f)
        };

        var indices = new uint[] { 0 };

        using var curve = new FlatBezierCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        // Ray through curve
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(1.5f, 0.5f, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect flat Bezier curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void RoundBSplineCurve_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Simple horizontal B-spline curve with larger radius
        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.5f),
            new CurveVertex(new V3f(1, 0, 0), 0.5f),
            new CurveVertex(new V3f(2, 0, 0), 0.5f),
            new CurveVertex(new V3f(3, 0, 0), 0.5f)
        };

        var indices = new uint[] { 0 };

        using var curve = new RoundBSplineCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        // Ray through middle of curve
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(1.5f, 0, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect B-spline curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void FlatBSplineCurve_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.3f),
            new CurveVertex(new V3f(1, 0, 0), 0.3f),
            new CurveVertex(new V3f(2, 1, 0), 0.3f),
            new CurveVertex(new V3f(3, 1, 0), 0.3f)
        };

        var indices = new uint[] { 0 };

        using var curve = new FlatBSplineCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(1.5f, 0.5f, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect flat B-spline curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void RoundCatmullRomCurve_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Catmull-Rom curve with 4 control points
        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.2f),
            new CurveVertex(new V3f(1, 1, 0), 0.2f),
            new CurveVertex(new V3f(2, 0, 0), 0.2f),
            new CurveVertex(new V3f(3, 1, 0), 0.2f)
        };

        var indices = new uint[] { 0 };

        using var curve = new RoundCatmullRomCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(1.5f, 0.5f, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect Catmull-Rom curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void FlatCatmullRomCurve_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.3f),
            new CurveVertex(new V3f(1, 1, 0), 0.3f),
            new CurveVertex(new V3f(2, 0, 0), 0.3f),
            new CurveVertex(new V3f(3, 1, 0), 0.3f)
        };

        var indices = new uint[] { 0 };

        using var curve = new FlatCatmullRomCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(1.5f, 0.5f, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect flat Catmull-Rom curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void FlatLinearCurve_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.3f),
            new CurveVertex(new V3f(2, 0, 0), 0.3f)
        };

        var indices = new uint[] { 0 };

        using var curve = new FlatLinearCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(1.0f, 0, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect flat linear curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void ConeLinearCurve_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.3f),
            new CurveVertex(new V3f(2, 0, 0), 0.1f)  // Cone tapers from 0.3 to 0.1
        };

        var indices = new uint[] { 0 };

        using var curve = new ConeLinearCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(1.0f, 0, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect cone linear curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void RoundLinearCurve_Occlusion(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.3f),
            new CurveVertex(new V3f(1, 0, 0), 0.3f)
        };

        var indices = new uint[] { 0 };

        using var curve = new RoundLinearCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        // Ray should be occluded by curve
        bool occluded = scene.Occluded(
            rayOrigin: new V3f(0.5f, 0, 1.0f),
            rayDirection: new V3f(0, 0, -1)
        );

        Assert.True(occluded, "Ray should be occluded by linear curve");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void Curve_RayMisses(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.1f),
            new CurveVertex(new V3f(1, 0, 0), 0.1f)
        };

        var indices = new uint[] { 0 };

        using var curve = new RoundLinearCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        // Ray far away should miss
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(10, 10, 1.0f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.False(intersected, "Ray far from curve should miss");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void MultipleCurves_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Three linear curves at different positions
        var vertices = new CurveVertex[]
        {
            new CurveVertex(new V3f(0, 0, 0), 0.2f),
            new CurveVertex(new V3f(1, 0, 0), 0.2f),

            new CurveVertex(new V3f(0, 2, 0), 0.2f),
            new CurveVertex(new V3f(1, 2, 0), 0.2f),

            new CurveVertex(new V3f(0, 4, 0), 0.2f),
            new CurveVertex(new V3f(1, 4, 0), 0.2f)
        };

        var indices = new uint[] { 0, 2, 4 };

        using var curve = new RoundLinearCurveGeometry(device, vertices, indices, quality);
        scene.AttachGeometry(curve);
        scene.Commit();

        // Test intersection with each curve
        var hit1 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0.5f, 0, 1), new V3f(0, 0, -1), ref hit1));

        var hit2 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0.5f, 2, 1), new V3f(0, 0, -1), ref hit2));

        var hit3 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0.5f, 4, 1), new V3f(0, 0, -1), ref hit3));
    }
}

public class PointGeometryTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateSpherePointGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(1, 1, 1), 0.3f),
            new Point(new V3f(2, 2, 2), 0.4f)
        };

        using var spherePoints = new SpherePointGeometry(device, points, quality);

        Assert.NotEqual(IntPtr.Zero, spherePoints.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateDiscPointGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(1, 0, 0), 0.5f)
        };

        using var discPoints = new DiscPointGeometry(device, points, quality);

        Assert.NotEqual(IntPtr.Zero, discPoints.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateOrientedDiscPointGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 0.5f, new V3f(0, 0, 1)),
            new OrientedPoint(new V3f(1, 0, 0), 0.5f, new V3f(0, 1, 0))
        };

        using var orientedDiscs = new OrientedDiscPointGeometry(device, points, quality);

        Assert.NotEqual(IntPtr.Zero, orientedDiscs.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SpherePointGeometryIntersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var spherePoints = new SpherePointGeometry(device, points, quality);
        scene.AttachGeometry(spherePoints);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 5),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateSpherePointFromSpan(RTCBuildQuality quality)
    {
        using var device = new Device();

        var points = new Point[3]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(1, 1, 1), 0.3f),
            new Point(new V3f(2, 2, 2), 0.4f)
        };

        using var spherePoints = new SpherePointGeometry(device, points, quality);

        Assert.NotEqual(IntPtr.Zero, spherePoints.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SpherePointGeometry_RayMisses(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var spherePoints = new SpherePointGeometry(device, points, quality);
        scene.AttachGeometry(spherePoints);
        scene.Commit();

        // Ray far away should miss
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(10, 10, 5),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.False(intersected, "Ray far from point should miss");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SpherePointGeometry_Occlusion(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var spherePoints = new SpherePointGeometry(device, points, quality);
        scene.AttachGeometry(spherePoints);
        scene.Commit();

        // Ray should be occluded by sphere point
        bool occluded = scene.Occluded(
            rayOrigin: new V3f(0, 0, 5),
            rayDirection: new V3f(0, 0, -1)
        );

        Assert.True(occluded, "Ray should be occluded by sphere point");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void SpherePointGeometry_MultiplePoints(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f),
            new Point(new V3f(2, 0, 0), 0.5f),
            new Point(new V3f(0, 2, 0), 0.5f)
        };

        using var spherePoints = new SpherePointGeometry(device, points, quality);
        scene.AttachGeometry(spherePoints);
        scene.Commit();

        // Test each point
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
    public void SpherePointGeometry_RespectRadius(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var spherePoints = new SpherePointGeometry(device, points, quality);
        scene.AttachGeometry(spherePoints);
        scene.Commit();

        // Ray inside radius should hit
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(0.3f, 0, 5),
            new V3f(0, 0, -1),
            ref hit1
        );
        Assert.True(intersected1, "Ray inside radius should hit");

        // Ray outside radius should miss
        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            new V3f(0.7f, 0, 5),
            new V3f(0, 0, -1),
            ref hit2
        );
        Assert.False(intersected2, "Ray outside radius should miss");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscPointGeometry_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 1.0f)
        };

        using var discPoints = new DiscPointGeometry(device, points, quality);
        scene.AttachGeometry(discPoints);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 5),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect disc point");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DiscPointGeometry_RayMisses(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.5f)
        };

        using var discPoints = new DiscPointGeometry(device, points, quality);
        scene.AttachGeometry(discPoints);
        scene.Commit();

        // Ray outside bounds should miss
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(10, 10, 5),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.False(intersected, "Ray far from disc point should miss");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OrientedDiscPointGeometry_Intersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 1.0f, new V3f(0, 0, 1))
        };

        using var orientedDiscs = new OrientedDiscPointGeometry(device, points, quality);
        scene.AttachGeometry(orientedDiscs);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 5),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect oriented disc point");
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void OrientedDiscPointGeometry_RespectsOrientation(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        // Disc oriented along Z axis
        var points = new OrientedPoint[]
        {
            new OrientedPoint(new V3f(0, 0, 0), 1.0f, new V3f(0, 0, 1))
        };

        using var orientedDiscs = new OrientedDiscPointGeometry(device, points, quality);
        scene.AttachGeometry(orientedDiscs);
        scene.Commit();

        // Ray perpendicular to disc should hit
        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            new V3f(0, 0, 5),
            new V3f(0, 0, -1),
            ref hit1
        );
        Assert.True(intersected1, "Perpendicular ray should hit");

        // Ray parallel to disc plane should miss
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
    public void PointGeometry_VaryingRadii(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var points = new Point[]
        {
            new Point(new V3f(0, 0, 0), 0.1f),
            new Point(new V3f(1, 0, 0), 0.5f),
            new Point(new V3f(2, 0, 0), 1.0f)
        };

        using var spherePoints = new SpherePointGeometry(device, points, quality);
        scene.AttachGeometry(spherePoints);
        scene.Commit();

        // Small point
        var hit1 = new RayHit();
        Assert.True(scene.Intersect(new V3f(0, 0, 1), new V3f(0, 0, -1), ref hit1));

        // Medium point
        var hit2 = new RayHit();
        Assert.True(scene.Intersect(new V3f(1, 0, 1), new V3f(0, 0, -1), ref hit2));

        // Large point
        var hit3 = new RayHit();
        Assert.True(scene.Intersect(new V3f(2, 0, 1), new V3f(0, 0, -1), ref hit3));
    }
}

public class SubdivisionGeometryTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateSubdivisionGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Simple cube control mesh
        var vertices = new V3f[]
        {
            new V3f(-1, -1, -1), new V3f(1, -1, -1), new V3f(1, 1, -1), new V3f(-1, 1, -1),
            new V3f(-1, -1, 1), new V3f(1, -1, 1), new V3f(1, 1, 1), new V3f(-1, 1, 1)
        };

        // 6 quad faces
        var indices = new uint[]
        {
            0, 1, 2, 3,  // front
            1, 5, 6, 2,  // right
            5, 4, 7, 6,  // back
            4, 0, 3, 7,  // left
            3, 2, 6, 7,  // top
            4, 5, 1, 0   // bottom
        };

        var faces = new uint[] { 4, 4, 4, 4, 4, 4 }; // 6 quads

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces, quality);

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateSubdivisionGeometryFromSpan(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[4]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(1, 1, 0),
            new V3f(-1, 1, 0)
        };

        var indices = new uint[4] { 0, 1, 2, 3 };
        var faces = new uint[1] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces, quality);

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }
}

public class CollisionDetectionTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void CanDetectCollisionBetweenScenes(RTCBuildQuality quality)
    {
        using var device = new Device();

        // NOTE: rtcCollide only works with UserGeometry, not triangle/quad geometries
        // This is an Embree 4.4 API limitation documented in the official docs

        // Bounds function for first geometry: box at (0,0,0) to (2,2,2)
        RTCBoundsFunction bounds1 = (RTCBoundsFunctionArguments* args) =>
        {
            *args->bounds_o = new RTCBounds
            {
                lower = new V3f(0, 0, 0),
                upper = new V3f(2, 2, 2)
            };
        };

        // Intersect function (required but not used for collision detection)
        RTCIntersectFunction intersect1 = (RTCIntersectFunctionNArguments* args) => { };

        // Scene 1: User geometry with bounding box at (0,0,0) to (2,2,2)
        using var scene1 = new Scene(device, quality, dynamic: false);
        using var geom1 = new UserGeometry(device, 1, bounds1, intersect1);
        scene1.AttachGeometry(geom1);
        scene1.Commit();

        // Bounds function for second geometry: box at (1,1,1) to (3,3,3)
        RTCBoundsFunction bounds2 = (RTCBoundsFunctionArguments* args) =>
        {
            *args->bounds_o = new RTCBounds
            {
                lower = new V3f(1, 1, 1),
                upper = new V3f(3, 3, 3)
            };
        };

        // Intersect function for second geometry
        RTCIntersectFunction intersect2 = (RTCIntersectFunctionNArguments* args) => { };

        // Scene 2: User geometry with overlapping bounding box at (1,1,1) to (3,3,3)
        using var scene2 = new Scene(device, quality, dynamic: false);
        using var geom2 = new UserGeometry(device, 1, bounds2, intersect2);
        scene2.AttachGeometry(geom2);
        scene2.Commit();

        var collisions = scene1.Collide(scene2);

        // Should detect collision between the overlapping bounding boxes
        Assert.NotEmpty(collisions);
        Assert.Equal(0u, collisions[0].GeometryId0);
        Assert.Equal(0u, collisions[0].PrimitiveId0);
        Assert.Equal(0u, collisions[0].GeometryId1);
        Assert.Equal(0u, collisions[0].PrimitiveId1);
    }
}
