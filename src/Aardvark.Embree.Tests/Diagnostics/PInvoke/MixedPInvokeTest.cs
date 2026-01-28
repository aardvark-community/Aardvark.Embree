using System;
using System.Runtime.InteropServices;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// DIAGNOSTIC TEST: Mixed approach combining direct P/Invoke with wrapper classes.
///
/// Purpose: Isolates which specific wrapper layer contains the bug by mixing
/// known-working direct P/Invoke code with suspect wrapper code.
///
/// Strategy: Create instance geometry using direct P/Invoke (proven to work),
/// then use Scene wrapper for intersection testing. If this fails, bug is in
/// Scene wrapper, not in InstanceGeometry wrapper or P/Invoke bindings.
///
/// When to use:
/// - After direct P/Invoke test passes but wrapper test fails
/// - To pinpoint exact wrapper class/method causing the issue
/// - To narrow down from "somewhere in wrapper code" to "specific method"
///
/// Historical context:
/// Created during Phase 2C instance geometry investigation (Nov 2025).
/// This test FAILED, proving the bug was specifically in Scene.Intersect() method,
/// not in InstanceGeometry class or instance creation logic.
///
/// Key finding: Direct P/Invoke instance + Scene.Intersect() = FAIL
/// This isolated the bug to Scene.Intersect() implementation.
///
/// Bug found: Scene.Intersect() was using wrong RTCRayQueryFlags.
///
/// See also:
/// - DirectPInvokeInstanceTest.cs for fully working P/Invoke test
/// - docs/DEBUGGING_METHODOLOGY.md Phase 3: Mixed Testing
/// - Scene.cs for the fixed implementation
/// </summary>
public class MixedPInvokeTest
{
    [Fact]
    public void DirectInstance_WithSceneWrapper_ShouldIntersect()
    {
        Console.WriteLine("=== Direct P/Invoke Instance + Scene Wrapper ==>");

        using var device = new Device();

        // Create instance using DIRECT P/Invoke (known to work)
        IntPtr sourceScene = EmbreeAPI.rtcNewScene(device.Handle);
        IntPtr geom = EmbreeAPI.rtcNewGeometry(device.Handle, RTCGeometryType.Triangle);

        // Set vertices
        IntPtr vertexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Vertex, 0, RTCFormat.FLOAT3,
            (nuint)(sizeof(float) * 3), 3
        );
        unsafe
        {
            float* vertices = (float*)vertexBuffer;
            vertices[0] = 0.0f; vertices[1] = 0.0f; vertices[2] = 0.0f;
            vertices[3] = 1.0f; vertices[4] = 0.0f; vertices[5] = 0.0f;
            vertices[6] = 0.0f; vertices[7] = 1.0f; vertices[8] = 0.0f;
        }

        // Set indices
        IntPtr indexBuffer = EmbreeAPI.rtcSetNewGeometryBuffer(
            geom, RTCBufferType.Index, 0, RTCFormat.UINT3,
            (nuint)(sizeof(uint) * 3), 1
        );
        unsafe
        {
            uint* indices = (uint*)indexBuffer;
            indices[0] = 0; indices[1] = 1; indices[2] = 2;
        }

        EmbreeAPI.rtcCommitGeometry(geom);
        EmbreeAPI.rtcAttachGeometry(sourceScene, geom);
        EmbreeAPI.rtcCommitScene(sourceScene);

        // Create instance
        IntPtr instance = EmbreeAPI.rtcNewGeometry(device.Handle, RTCGeometryType.Instance);
        EmbreeAPI.rtcSetGeometryInstancedScene(instance, sourceScene);
        EmbreeAPI.rtcSetGeometryTimeStepCount(instance, 1);

        unsafe
        {
            float* transform = stackalloc float[12];
            transform[0] = 1.0f; transform[1] = 0.0f; transform[2] = 0.0f; transform[3] = 0.0f;
            transform[4] = 0.0f; transform[5] = 1.0f; transform[6] = 0.0f; transform[7] = 0.0f;
            transform[8] = 0.0f; transform[9] = 0.0f; transform[10] = 1.0f; transform[11] = 0.0f;
            EmbreeAPI.rtcSetGeometryTransform(instance, 0, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)transform);
        }

        EmbreeAPI.rtcCommitGeometry(instance);
        Console.WriteLine("Direct P/Invoke instance created");

        // Now use Scene WRAPPER to attach and test
        using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);

        // Attach the instance handle directly
        var instID = EmbreeAPI.rtcAttachGeometry(scene.Handle, instance);
        Console.WriteLine($"Instance attached to scene wrapper (ID={instID})");

        scene.Commit();
        Console.WriteLine("Scene committed");

        // Test intersection using Scene wrapper
        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0.25f, 0.25f, 1f),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Console.WriteLine($"Intersected: {intersected}");
        Console.WriteLine($"Hit T: {hit.T}");

        // Cleanup
        EmbreeAPI.rtcReleaseGeometry(instance);
        EmbreeAPI.rtcReleaseGeometry(geom);
        EmbreeAPI.rtcReleaseScene(sourceScene);

        Assert.True(intersected, "Direct instance + Scene wrapper MUST intersect");
    }
}
