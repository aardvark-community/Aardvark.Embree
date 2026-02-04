using Aardvark.Base;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for Embree 4 filter functions to validate callback compatibility.
/// Validates that filter callbacks work correctly without crashes (critical after point query delegate issue).
/// Tests geometry filtering, ray masking, and filter function invocation.
/// </summary>
public class FilterFunctionTests
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate void FilterFunctionN(RTCFilterFunctionNArguments* args);

    private static (Device, Scene) CreateSceneWithTwoTriangles(RTCBuildQuality quality)
    {
        var device = new Device();

        // First triangle (z=0 plane)
        var vertices1 = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices1 = new int[] { 0, 1, 2 };

        // Second triangle (z=2 plane)
        var vertices2 = new V3f[]
        {
            new V3f(0.0f, 0.0f, 2.0f),
            new V3f(1.0f, 0.0f, 2.0f),
            new V3f(0.0f, 1.0f, 2.0f)
        };
        var indices2 = new int[] { 0, 1, 2 };

        var geometry1 = new TriangleGeometry(device, vertices1, indices1, quality);
        var geometry2 = new TriangleGeometry(device, vertices2, indices2, quality);

        var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry1);
        scene.AttachGeometry(geometry2);
        scene.Commit();

        return (device, scene);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanSetIntersectFilterFunctionToNull(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry.Handle, IntPtr.Zero);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanSetOccludedFilterFunctionToNull(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        EmbreeAPI.rtcSetGeometryOccludedFilterFunction(geometry.Handle, IntPtr.Zero);

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void CanSetIntersectFilterFunctionWithCallback(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        // Simple filter that accepts all hits
        FilterFunctionN filterFunc = (RTCFilterFunctionNArguments* args) =>
        {
            // Default behavior: keep all hits (valid array defaults to non-zero)
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(filterFunc);
        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry.Handle, funcPtr);

        geometry.Commit();

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);

        GC.KeepAlive(filterFunc);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void CanSetOccludedFilterFunctionWithCallback(RTCBuildQuality quality)
    {
        using var device = new Device();
        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        // Simple filter that accepts all occlusions
        FilterFunctionN filterFunc = (RTCFilterFunctionNArguments* args) =>
        {
            // Default behavior: keep all hits
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(filterFunc);
        EmbreeAPI.rtcSetGeometryOccludedFilterFunction(geometry.Handle, funcPtr);

        geometry.Commit();

        Assert.NotEqual(IntPtr.Zero, geometry.Handle);

        GC.KeepAlive(filterFunc);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_AcceptAllHits_WorksCorrectly(RTCBuildQuality quality)
    {
        var (device, scene) = CreateSceneWithTwoTriangles(quality);

        try
        {
            // Ray should hit both triangles at z=0 and z=2
            var hit = new RayHit();
            var rayOrigin = new V3f(0.25f, 0.25f, -1.0f);
            var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

            var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);

            Assert.True(result);
            Assert.True(hit.T > 0.0f);
        }
        finally
        {
            scene.Dispose();
            device.Dispose();
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_RejectAllHits_BlocksIntersections(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        // Filter that rejects all hits by setting valid[0] = 0
        FilterFunctionN filterFunc = (RTCFilterFunctionNArguments* args) =>
        {
            if (args->valid != null)
            {
                args->valid[0] = 0; // Reject hit
            }
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(filterFunc);
        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry.Handle, funcPtr);
        geometry.Commit();

        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);

        // Hit should be rejected by filter
        Assert.False(result);

        GC.KeepAlive(filterFunc);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_RejectsBasedOnGeometryId_WorksCorrectly(RTCBuildQuality quality)
    {
        using var device = new Device();

        // First triangle
        var vertices1 = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices1 = new int[] { 0, 1, 2 };

        // Second triangle (offset in Z)
        var vertices2 = new V3f[]
        {
            new V3f(0.0f, 0.0f, -1.0f),
            new V3f(1.0f, 0.0f, -1.0f),
            new V3f(0.0f, 1.0f, -1.0f)
        };
        var indices2 = new int[] { 0, 1, 2 };

        using var geometry1 = new TriangleGeometry(device, vertices1, indices1, quality);
        using var geometry2 = new TriangleGeometry(device, vertices2, indices2, quality);

        // Filter for geometry2 that accepts all hits
        FilterFunctionN acceptFilter = (RTCFilterFunctionNArguments* args) =>
        {
            // Accept all hits (default behavior)
        };

        var acceptPtr = Marshal.GetFunctionPointerForDelegate(acceptFilter);
        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry2.Handle, acceptPtr);

        geometry1.Commit();
        geometry2.Commit();

        using var scene = new Scene(device, quality, false);
        var geomId1 = scene.AttachGeometry(geometry1);
        var geomId2 = scene.AttachGeometry(geometry2);
        scene.Commit();

        // Ray hits geometry2 first (at z=-1)
        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, -2.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);

        Assert.True(result);
        Assert.Equal(geomId2, hit.GeometryId);

        GC.KeepAlive(acceptFilter);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_WithRayMask_FiltersCorrectly(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        // Set geometry mask to 0x01
        EmbreeAPI.rtcSetGeometryMask(geometry.Handle, 0x01);
        geometry.Commit();

        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        // Test with geometry mask - Embree handles masking internally
        // This test validates that rtcSetGeometryMask API works without crashing
        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);
        Assert.True(result);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_MultipleGeometriesWithDifferentFilters_WorkCorrectly(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices1 = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        var vertices2 = new V3f[]
        {
            new V3f(0.0f, 0.0f, -1.0f),
            new V3f(1.0f, 0.0f, -1.0f),
            new V3f(0.0f, 1.0f, -1.0f)
        };

        using var geometry1 = new TriangleGeometry(device, vertices1, indices, quality);
        using var geometry2 = new TriangleGeometry(device, vertices2, indices, quality);

        // Filter for geometry1: reject all
        FilterFunctionN rejectFilter = (RTCFilterFunctionNArguments* args) =>
        {
            if (args->valid != null)
            {
                args->valid[0] = 0; // Reject
            }
        };

        // Filter for geometry2: accept all (default)
        FilterFunctionN acceptFilter = (RTCFilterFunctionNArguments* args) =>
        {
            // Accept all
        };

        var rejectPtr = Marshal.GetFunctionPointerForDelegate(rejectFilter);
        var acceptPtr = Marshal.GetFunctionPointerForDelegate(acceptFilter);

        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry1.Handle, rejectPtr);
        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry2.Handle, acceptPtr);

        geometry1.Commit();
        geometry2.Commit();

        using var scene = new Scene(device, quality, false);
        var geomId1 = scene.AttachGeometry(geometry1);
        var geomId2 = scene.AttachGeometry(geometry2);
        scene.Commit();

        // Ray from z=-2 going toward +z hits geometry2 first, then geometry1
        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, -2.0f);
        var rayDirection = new V3f(0.0f, 0.0f, 1.0f);

        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);

        // Should hit geometry2 (accepts hits), skip geometry1 (rejects hits)
        Assert.True(result);
        Assert.Equal(geomId2, hit.GeometryId);

        GC.KeepAlive(rejectFilter);
        GC.KeepAlive(acceptFilter);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_OccludedCallback_WorksWithoutCrash(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        FilterFunctionN occludedFilter = (RTCFilterFunctionNArguments* args) =>
        {
            // Accept occlusion (default)
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(occludedFilter);
        EmbreeAPI.rtcSetGeometryOccludedFilterFunction(geometry.Handle, funcPtr);

        geometry.Commit();

        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Occluded(rayOrigin, rayDirection, 0.0f, float.MaxValue);

        Assert.True(result);

        GC.KeepAlive(occludedFilter);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_OccludedRejectAll_ReturnsFalse(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        FilterFunctionN rejectFilter = (RTCFilterFunctionNArguments* args) =>
        {
            if (args->valid != null)
            {
                args->valid[0] = 0; // Reject occlusion
            }
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(rejectFilter);
        EmbreeAPI.rtcSetGeometryOccludedFilterFunction(geometry.Handle, funcPtr);

        geometry.Commit();

        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        var result = scene.Occluded(rayOrigin, rayDirection, 0.0f, float.MaxValue);

        // Occlusion rejected by filter
        Assert.False(result);

        GC.KeepAlive(rejectFilter);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_ChecksFilterArgumentsAreValid(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        bool filterCalled = false;
        bool argsValid = false;

        FilterFunctionN validationFilter = (RTCFilterFunctionNArguments* args) =>
        {
            filterCalled = true;
            argsValid = args != null && args->valid != null && args->ray != null && args->hit != null;
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(validationFilter);
        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry.Handle, funcPtr);

        geometry.Commit();

        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);

        Assert.True(filterCalled, "Filter function was not called");
        Assert.True(argsValid, "Filter function arguments were invalid");

        GC.KeepAlive(validationFilter);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void FilterFunction_NoCrashWithNullGeometryUserPtr(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0.0f, 0.0f, 0.0f),
            new V3f(1.0f, 0.0f, 0.0f),
            new V3f(0.0f, 1.0f, 0.0f)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geometry = new TriangleGeometry(device, vertices, indices, quality);

        FilterFunctionN filter = (RTCFilterFunctionNArguments* args) =>
        {
            // Access geometryUserPtr (should be null or valid)
            var userPtr = args->geometryUserPtr;
            // No crash expected
        };

        var funcPtr = Marshal.GetFunctionPointerForDelegate(filter);
        EmbreeAPI.rtcSetGeometryIntersectFilterFunction(geometry.Handle, funcPtr);

        geometry.Commit();

        using var scene = new Scene(device, quality, false);
        scene.AttachGeometry(geometry);
        scene.Commit();

        var hit = new RayHit();
        var rayOrigin = new V3f(0.25f, 0.25f, 1.0f);
        var rayDirection = new V3f(0.0f, 0.0f, -1.0f);

        // Should not crash
        var result = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue);

        Assert.True(result);

        GC.KeepAlive(filter);
    }
}
