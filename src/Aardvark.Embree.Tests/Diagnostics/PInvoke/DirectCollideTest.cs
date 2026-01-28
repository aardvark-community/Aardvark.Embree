using System;
using System.Runtime.InteropServices;
using Xunit;
using static Aardvark.Embree.EmbreeAPI;

namespace Aardvark.Embree.Tests.Diagnostics.PInvoke;

/// <summary>
/// Direct P/Invoke test for rtcCollide to verify the function exists and works at the lowest level.
/// </summary>
public class DirectCollideTest
{
    [Fact(Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void DirectCollide_SimplestPossibleTest()
    {
        // Create device
        var device = rtcNewDevice(null);
        Assert.NotEqual(IntPtr.Zero, device);

        try
        {
            // Create two scenes
            var scene1 = rtcNewScene(device);
            var scene2 = rtcNewScene(device);

            try
            {
                // Create simple triangle geometry in scene1
                var geom1 = rtcNewGeometry(device, RTCGeometryType.Triangle);
                unsafe
                {
                    float* vertices1 = (float*)rtcSetNewGeometryBuffer(
                        geom1,
                        RTCBufferType.Vertex,
                        0,
                        RTCFormat.FLOAT3,
                        3 * sizeof(float),
                        3);

                    // Triangle 1: simple triangle at origin
                    vertices1[0] = -1; vertices1[1] = -1; vertices1[2] = 0;
                    vertices1[3] =  1; vertices1[4] = -1; vertices1[5] = 0;
                    vertices1[6] =  0; vertices1[7] =  1; vertices1[8] = 0;

                    uint* indices1 = (uint*)rtcSetNewGeometryBuffer(
                        geom1,
                        RTCBufferType.Index,
                        0,
                        RTCFormat.UINT3,
                        3 * sizeof(uint),
                        1);

                    indices1[0] = 0; indices1[1] = 1; indices1[2] = 2;
                }

                rtcCommitGeometry(geom1);
                rtcAttachGeometry(scene1, geom1);
                rtcReleaseGeometry(geom1);
                rtcCommitScene(scene1);

                // Create simple triangle geometry in scene2 (non-overlapping)
                var geom2 = rtcNewGeometry(device, RTCGeometryType.Triangle);
                unsafe
                {
                    float* vertices2 = (float*)rtcSetNewGeometryBuffer(
                        geom2,
                        RTCBufferType.Vertex,
                        0,
                        RTCFormat.FLOAT3,
                        3 * sizeof(float),
                        3);

                    // Triangle 2: far away
                    vertices2[0] = 10; vertices2[1] = -1; vertices2[2] = 0;
                    vertices2[3] = 12; vertices2[4] = -1; vertices2[5] = 0;
                    vertices2[6] = 11; vertices2[7] =  1; vertices2[8] = 0;

                    uint* indices2 = (uint*)rtcSetNewGeometryBuffer(
                        geom2,
                        RTCBufferType.Index,
                        0,
                        RTCFormat.UINT3,
                        3 * sizeof(uint),
                        1);

                    indices2[0] = 0; indices2[1] = 1; indices2[2] = 2;
                }

                rtcCommitGeometry(geom2);
                rtcAttachGeometry(scene2, geom2);
                rtcReleaseGeometry(geom2);
                rtcCommitScene(scene2);

                // Try to call rtcCollide
                int callbackCount = 0;
                var gcHandle = GCHandle.Alloc(callbackCount, GCHandleType.Pinned);

                try
                {
                    unsafe
                    {
                        RTCCollideFunc callback = (userPtr, collisions, numCollisions) =>
                        {
                            callbackCount++;
                        };

                        var callbackHandle = GCHandle.Alloc(callback, GCHandleType.Normal);
                        try
                        {
                            var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);
                            rtcCollide(scene1, scene2, callbackPtr, GCHandle.ToIntPtr(gcHandle));
                        }
                        finally
                        {
                            callbackHandle.Free();
                        }
                    }

                    // If we got here without crashing, rtcCollide exists and works
                    // For non-overlapping triangles, callback should not have been invoked
                    Assert.Equal(0, callbackCount);
                }
                finally
                {
                    gcHandle.Free();
                }
            }
            finally
            {
                rtcReleaseScene(scene2);
                rtcReleaseScene(scene1);
            }
        }
        finally
        {
            rtcReleaseDevice(device);
        }
    }
}
