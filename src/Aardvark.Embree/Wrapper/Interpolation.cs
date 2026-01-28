using Aardvark.Base;
using System;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Extension methods for geometry interpolation using native Embree rtcInterpolate API.
/// </summary>
public static class GeometryInterpolation
{
    /// <summary>
    /// Interpolates vertex position (V3f) at a given primitive and barycentric coordinate.
    /// Uses native Embree rtcInterpolate API for all supported geometry types.
    /// </summary>
    public static V3f InterpolatePosition(this Scene scene, uint geometryId, uint primitiveId, float u, float v)
    {
        var geometry = scene.GetGeometry(geometryId);

        unsafe
        {
            V3f result = default;
            var args = new RTCInterpolateArguments
            {
                geometry = geometry.Handle,
                primID = primitiveId,
                u = u,
                v = v,
                bufferType = RTCBufferType.Vertex,
                bufferSlot = 0,
                P = new IntPtr(&result),
                dPdu = IntPtr.Zero,
                dPdv = IntPtr.Zero,
                ddPdudu = IntPtr.Zero,
                ddPdvdv = IntPtr.Zero,
                ddPdudv = IntPtr.Zero,
                valueCount = 3
            };
            EmbreeAPI.rtcInterpolate(ref args);
            return result;
        }
    }

    /// <summary>
    /// Interpolates with derivatives (position and partial derivatives with respect to u and v).
    /// Uses native Embree rtcInterpolate API with derivative calculation.
    /// </summary>
    public static void InterpolateWithDerivatives(this Scene scene, uint geometryId, uint primitiveId, float u, float v,
                                                  out V3f position, out V3f dPdu, out V3f dPdv)
    {
        var geometry = scene.GetGeometry(geometryId);

        unsafe
        {
            V3f resultPosition = default;
            V3f resultDPdu = default;
            V3f resultDPdv = default;

            var args = new RTCInterpolateArguments
            {
                geometry = geometry.Handle,
                primID = primitiveId,
                u = u,
                v = v,
                bufferType = RTCBufferType.Vertex,
                bufferSlot = 0,
                P = new IntPtr(&resultPosition),
                dPdu = new IntPtr(&resultDPdu),
                dPdv = new IntPtr(&resultDPdv),
                ddPdudu = IntPtr.Zero,
                ddPdvdv = IntPtr.Zero,
                ddPdudv = IntPtr.Zero,
                valueCount = 3
            };
            EmbreeAPI.rtcInterpolate(ref args);

            position = resultPosition;
            dPdu = resultDPdu;
            dPdv = resultDPdv;
        }
    }

    /// <summary>
    /// Batch interpolation for multiple primitives and coordinates.
    /// Uses native Embree rtcInterpolate API for each interpolation.
    /// </summary>
    public static V3f[] InterpolateBatch(this Scene scene, uint geometryId, uint[] primitiveIds, float[] us, float[] vs)
    {
        if (primitiveIds.Length != us.Length || us.Length != vs.Length)
            throw new ArgumentException("Arrays must have same length");

        var geometry = scene.GetGeometry(geometryId);
        int n = primitiveIds.Length;
        var results = new V3f[n];

        unsafe
        {
            for (int i = 0; i < n; i++)
            {
                V3f result = default;
                var args = new RTCInterpolateArguments
                {
                    geometry = geometry.Handle,
                    primID = primitiveIds[i],
                    u = us[i],
                    v = vs[i],
                    bufferType = RTCBufferType.Vertex,
                    bufferSlot = 0,
                    P = new IntPtr(&result),
                    dPdu = IntPtr.Zero,
                    dPdv = IntPtr.Zero,
                    ddPdudu = IntPtr.Zero,
                    ddPdvdv = IntPtr.Zero,
                    ddPdudv = IntPtr.Zero,
                    valueCount = 3
                };
                EmbreeAPI.rtcInterpolate(ref args);
                results[i] = result;
            }
        }

        return results;
    }
}
