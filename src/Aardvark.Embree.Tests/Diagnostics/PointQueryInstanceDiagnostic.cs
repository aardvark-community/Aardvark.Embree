using Aardvark.Base;
using System;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Aardvark.Embree.Tests.Diagnostics;

/// <summary>
/// Diagnostic test to understand point query behavior with instances.
/// </summary>
public class PointQueryInstanceDiagnostic
{
    private readonly ITestOutputHelper _output;

    public PointQueryInstanceDiagnostic(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(DisplayName = "DIAGNOSTIC: Point query callback behavior with direct geometry")]
    public void PointQuery_DirectGeometry_CallbackInvoked()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        scene.AttachGeometry(triangleGeom);
        scene.Commit();

        var result = scene.GetClosestPoint(new V3f(0.25f, 0.25f, 1f));

        _output.WriteLine($"Direct Geometry Result:");
        _output.WriteLine($"  IsValid: {result.IsValid}");
        _output.WriteLine($"  Point: {result.Point}");
        _output.WriteLine($"  GeomID: {result.GeomID}, PrimID: {result.PrimID}");
        _output.WriteLine($"  DistanceSq: {result.DistanceSquared}");

        Assert.True(result.IsValid, "Direct geometry should work");
    }

    [Fact(DisplayName = "DIAGNOSTIC: Point query callback behavior with instanced geometry")]
    public void PointQuery_InstancedGeometry_CallbackInvoked()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var instance = new InstanceGeometry(device, triangleGeom, Affine3f.Identity, RTCBuildQuality.High);

        scene.AttachGeometry(instance);
        scene.Commit();

        var result = scene.GetClosestPoint(new V3f(0.25f, 0.25f, 1f));

        _output.WriteLine($"Instanced Geometry Result:");
        _output.WriteLine($"  IsValid: {result.IsValid}");
        if (result.IsValid)
        {
            _output.WriteLine($"  Point: {result.Point}");
            _output.WriteLine($"  GeomID: {result.GeomID}, PrimID: {result.PrimID}");
            _output.WriteLine($"  DistanceSq: {result.DistanceSquared}");
            _output.WriteLine($"  SUCCESS: Callback WAS invoked for instanced geometry!");
        }
        else
        {
            _output.WriteLine($"  FAILURE: Callback was NOT invoked for instanced geometry");
            _output.WriteLine($"  This means rtcPointQuery does not traverse instances");
        }

        // Don't assert - this is purely diagnostic
    }

    [Fact(DisplayName = "DIAGNOSTIC: Point query with translated instance")]
    public void PointQuery_TranslatedInstance_TestBehavior()
    {
        using var device = new Device();
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        var vertices = new V3f[]
        {
            new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var triangleGeom = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.High);
        using var instance = new InstanceGeometry(device, triangleGeom, Affine3f.Translation(5f, 5f, 0f), RTCBuildQuality.High);

        scene.AttachGeometry(instance);
        scene.Commit();

        // Query at translated location
        var result = scene.GetClosestPoint(new V3f(5.25f, 5.25f, 1f));

        _output.WriteLine($"Translated Instance Result:");
        _output.WriteLine($"  IsValid: {result.IsValid}");
        if (result.IsValid)
        {
            _output.WriteLine($"  Point: {result.Point}");
            _output.WriteLine($"  Expected Point: ~(5.25, 5.25, 0)");
            _output.WriteLine($"  GeomID: {result.GeomID}, PrimID: {result.PrimID}");
            _output.WriteLine($"  DistanceSq: {result.DistanceSquared}");

            // Check if point is in world space (translated) or object space (not translated)
            bool isWorldSpace = result.Point.X > 4f;
            bool isObjectSpace = result.Point.X < 1f;

            _output.WriteLine($"  Point is in: {(isWorldSpace ? "WORLD SPACE (transformation applied!)" : isObjectSpace ? "OBJECT SPACE (transformation NOT applied)" : "UNKNOWN SPACE")}");
        }
        else
        {
            _output.WriteLine($"  No result found");
        }
    }
}
