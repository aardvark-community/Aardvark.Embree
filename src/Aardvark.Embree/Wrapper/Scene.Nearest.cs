using Aardvark.Base;
using Aardvark.Embree;
using System;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Aardvark.Embree.Scene;

namespace Aardvark.Embree;

public unsafe partial class Scene
{
    // keep a strong ref so the delegate isn't GC'd while native code calls it
    private static readonly RTCPointQueryFunction s_nearestCallback = NearestCallback;

    public NearestHit Nearest(V3f queryPoint, float maxRadius = float.PositiveInfinity)
    {
        // prepare query (time=0)
        var q = new RTCPointQuery()
        {
            p = queryPoint,
            time = 0f,
            radius = float.IsInfinity(maxRadius) ? 1e30f : maxRadius
        };

        //// opaque, sufficiently large, 16B-aligned context buffer
        //// 256 bytes comfortably covers current RTCPointQueryContext
        //IntPtr ctx = Marshal.AllocHGlobal(256);
        //rtcInitPointQueryContext(ctx);

        // NOTE: rtcInitIntersectContext is an inline method -> do manually
        var ctx = new RTCPointQueryContext()
        {
            instID = unchecked((uint)-1), // need to be initialized with RTC_INVALID_GEOMETRY_ID
            inst2world = M44f.Identity,
            world2inst = M44f.Identity,
            instStackSize = 0
        };

        var state = new NearestState(this, queryPoint);
        var gch = GCHandle.Alloc(state, GCHandleType.Normal);

        try
        {
            // stackalloc ensures 16B alignment of RTCPointQuery on x64
            RTCPointQuery* pq = stackalloc RTCPointQuery[1];
            *pq = q;

            EmbreeAPI.rtcPointQuery(Handle, pq, ctx, s_nearestCallback, (IntPtr)gch);

            return state.ToResult();
        }
        finally
        {
            //gch.Free();
            //Marshal.FreeHGlobal(ctx);
        }
    }

    // --------- per-query scratch ------------
    private sealed class NearestState
    {
        public readonly Scene Scene;
        public readonly V3f Query;
        public float BestDistSq = float.PositiveInfinity;
        public V3f BestPoint;
        public uint GeomID = 0xffffffffu, PrimID = 0xffffffffu;

        public NearestState(Scene scene, V3f q) { Scene = scene; Query = q; }
        public NearestHit ToResult()
            => float.IsPositiveInfinity(BestDistSq)
               ? new NearestHit(false, default, float.PositiveInfinity, 0xffffffffu, 0xffffffffu)
               : new NearestHit(true, BestPoint, BestDistSq.Sqrt(), GeomID, PrimID);
    }

    // --------- Embree callback ------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool NearestCallback(ref RTCPointQueryFunctionArguments a)
    {
        var gch = GCHandle.FromIntPtr(a.userPtr);
        var st = (NearestState)gch.Target!;
        var sceneHandle = st.Scene.Handle;

        // fetch geometry
        IntPtr g = EmbreeAPI.rtcGetGeometry(sceneHandle, a.geomID);
        if (g == IntPtr.Zero) return false;

        // get buffers (we assume triangles: indices=int32 triplets, vertices=float3)
        int* idx = (int*)EmbreeAPI.rtcGetGeometryBufferData(g, RTCBufferType.Index, 0);
        float* vtx = (float*)EmbreeAPI.rtcGetGeometryBufferData(g, RTCBufferType.Vertex, 0);
        if (idx == null || vtx == null) return false;

        // triangle indices for this prim
        int i0 = idx[a.primID * 3 + 0];
        int i1 = idx[a.primID * 3 + 1];
        int i2 = idx[a.primID * 3 + 2];

        // load positions (object space == world space unless you use instancing)
        V3f p0 = new V3f(vtx[i0 * 3 + 0], vtx[i0 * 3 + 1], vtx[i0 * 3 + 2]);
        V3f p1 = new V3f(vtx[i1 * 3 + 0], vtx[i1 * 3 + 1], vtx[i1 * 3 + 2]);
        V3f p2 = new V3f(vtx[i2 * 3 + 0], vtx[i2 * 3 + 1], vtx[i2 * 3 + 2]);

        // TODO (optional): if you support Embree instancing, transform p0/p1/p2
        // by the top-of-stack inst2world matrix in a.context.
        // (We keep v1 minimal here; scenes without instances work out of the box.)

        // closest point on triangle and squared distance
        V3f cp = ClosestPointOnTriangle(st.Query, p0, p1, p2, out float d2);

        if (d2 < st.BestDistSq)
        {
            st.BestDistSq = d2;
            st.BestPoint = cp;
            st.GeomID = a.geomID;
            st.PrimID = a.primID;

            // tighten the query radius for the remaining traversal
            var q = (RTCPointQuery*)a.query;
            q->radius = d2.Sqrt();
            return true; // tell Embree to update traversal with the new radius
        }
        return false;
    }

    // Ericson-style closest-point-on-triangle (robust and branch-light)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static V3f ClosestPointOnTriangle(in V3f p, in V3f a, in V3f b, in V3f c, out float distSq)
    {
        var ab = b - a; var ac = c - a; var ap = p - a;
        float d1 = Vec.Dot(ab, ap);
        float d2 = Vec.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) { distSq = ap.LengthSquared; return a; }

        var bp = p - b;
        float d3 = Vec.Dot(ab, bp);
        float d4 = Vec.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) { distSq = bp.LengthSquared; return b; }

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
        {
            float v = d1 / (d1 - d3);
            var q = a + v * ab; distSq = (p - q).LengthSquared; return q;
        }

        var cp2 = p - c;
        float d5 = Vec.Dot(ab, cp2);
        float d6 = Vec.Dot(ac, cp2);
        if (d6 >= 0 && d5 <= d6) { distSq = cp2.LengthSquared; return c; }

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
        {
            float w = d2 / (d2 - d6);
            var q = a + w * ac; distSq = (p - q).LengthSquared; return q;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
        {
            float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
            var q = b + w * (c - b); distSq = (p - q).LengthSquared; return q;
        }

        // inside face
        float denom = 1.0f / (va + vb + vc);
        float v2 = vb * denom, w2 = vc * denom;
        var inside = a + ab * v2 + ac * w2;
        distSq = (p - inside).LengthSquared;
        return inside;
    }
}
