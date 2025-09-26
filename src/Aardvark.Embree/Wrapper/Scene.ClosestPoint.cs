using Aardvark.Base;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public unsafe partial class Scene
{
    // keep a strong ref so the delegate isn't GC'd while native code calls it
    private static readonly RTCPointQueryFunction s_nearestCallback = NearestCallback;

    public readonly struct ClosestPointInfo
    {
        public readonly bool IsValid;
        public readonly V3f Point;
        public readonly V2f UV;
        public readonly float DistanceSquared;
        public readonly uint GeomID;
        public readonly uint PrimID;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ClosestPointInfo(bool isValid, in V3f p, in V2f uv, float d2, uint gid, uint pid)
            => (IsValid, Point, UV, DistanceSquared, GeomID, PrimID) = (isValid, p, uv, d2, gid, pid);
    }

    public ClosestPointInfo GetClosestPoint(V3f queryPoint, float maxRadius = float.PositiveInfinity)
    {
        // prepare query (time=0)
        var q = new RTCPointQuery()
        {
            p = queryPoint,
            time = 0f,
            radius = float.IsInfinity(maxRadius) ? 1e30f : maxRadius
        };

        // NOTE: rtcInitPointQueryContext is an inline method -> do manually
        var ctx = new RTCPointQueryContext()
        {
            instID = RTC_INVALID_GEOMETRY_ID,   // need to be initialized with RTC_INVALID_GEOMETRY_ID
            instStackSize = 0                   // 
            //inst2world = M44f.Identity,       // NOTE: inline rtcInitPointQueryContext does not set this
            //world2inst = M44f.Identity,       // NOTE: inline rtcInitPointQueryContext does not set this
        };

        var state = new NearestState(this, queryPoint);
        var gch = GCHandle.Alloc(state, GCHandleType.Normal);

        // stackalloc ensures 16B alignment of RTCPointQuery on x64
        RTCPointQuery* pq = stackalloc RTCPointQuery[1];
        *pq = q;

        EmbreeAPI.rtcPointQuery(Handle, pq, ctx, s_nearestCallback, (IntPtr)gch);

        return state.ToResult();
    }

    // --------- per-query scratch ------------
    private sealed class NearestState(Scene scene, V3f q)
    {
        public readonly Scene Scene = scene;
        public readonly V3f Query = q;
        public float BestDistSq = float.PositiveInfinity;
        public V3f BestPoint;
        public V2f BestUV;
        public uint GeomID = 0xffffffffu, PrimID = 0xffffffffu;

        public ClosestPointInfo ToResult()
            => float.IsPositiveInfinity(BestDistSq)
               ? new ClosestPointInfo(false, default, default, float.PositiveInfinity, 0xffffffffu, 0xffffffffu)
               : new ClosestPointInfo(true, BestPoint, BestUV, BestDistSq, GeomID, PrimID);
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
        //int* iPtr = idx + a.primID * 3;
        //int i0 = *iPtr++;
        //int i1 = *iPtr++;
        //int i2 = *iPtr;
        int i0 = idx[a.primID * 3 + 0];
        int i1 = idx[a.primID * 3 + 1];
        int i2 = idx[a.primID * 3 + 2];

        // load positions (object space == world space unless you use instancing)
        //V3f p0 = *(V3f*)(vtx + i0 * 3);
        //V3f p1 = *(V3f*)(vtx + i1 * 3);
        //V3f p2 = *(V3f*)(vtx + i2 * 3);
        var triangle = new Triangle3f(
            new V3f(vtx[i0 * 3 + 0], vtx[i0 * 3 + 1], vtx[i0 * 3 + 2]),
            new V3f(vtx[i1 * 3 + 0], vtx[i1 * 3 + 1], vtx[i1 * 3 + 2]),
            new V3f(vtx[i2 * 3 + 0], vtx[i2 * 3 + 1], vtx[i2 * 3 + 2])
            );

        // TODO (optional): if you support Embree instancing, transform p0/p1/p2
        // by the top-of-stack inst2world matrix in a.context.
        // (We keep v1 minimal here; scenes without instances work out of the box.)

        // closest point on triangle and squared distance
        V3f cp = ClosestPointOnTriangle(st.Query, triangle.P0, triangle.P1, triangle.P2, out float d2, out float u, out float v);
        //V3f cp = triangle.GetClosestPointOn(st.Query);
        //var d2 = (cp - st.Query).LengthSquared;

        if (d2 < st.BestDistSq)
        {
            st.BestDistSq = d2;
            st.BestPoint = cp;
            st.BestUV = new V2f(u, v);
            st.GeomID = a.geomID;
            st.PrimID = a.primID;

            // tighten the query radius for the remaining traversal
            var q = (RTCPointQuery*)a.query;
            q->radius = d2.Sqrt();
            return true; // tell Embree to update traversal with the new radius
        }
        return false;
    }

    // Returns closest point + squared distance + (u,v) such that
    // q = a + u*(b - a) + v*(c - a).  (Note: barycentrics = (1-u-v, u, v))
    private static V3f ClosestPointOnTriangle(
        in V3f p, in V3f a, in V3f b, in V3f c,
        out float distSq, out float u, out float v)
    {
        var ab = b - a; var ac = c - a; var ap = p - a;
        float d1 = Vec.Dot(ab, ap);
        float d2 = Vec.Dot(ac, ap);
        if (d1 <= 0 && d2 <= 0) { distSq = ap.LengthSquared; u = 0f; v = 0f; return a; }

        var bp = p - b;
        float d3 = Vec.Dot(ab, bp);
        float d4 = Vec.Dot(ac, bp);
        if (d3 >= 0 && d4 <= d3) { distSq = bp.LengthSquared; u = 1f; v = 0f; return b; }

        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0 && d1 >= 0 && d3 <= 0)
        {
            float t = d1 / (d1 - d3);          // along AB
            var q = a + t * ab;
            distSq = (p - q).LengthSquared;
            u = t; v = 0f;                      // q = a + u*ab
            return q;
        }

        var cp2 = p - c;
        float d5 = Vec.Dot(ab, cp2);
        float d6 = Vec.Dot(ac, cp2);
        if (d6 >= 0 && d5 <= d6) { distSq = cp2.LengthSquared; u = 0f; v = 1f; return c; }

        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0 && d2 >= 0 && d6 <= 0)
        {
            float t = d2 / (d2 - d6);          // along AC
            var q = a + t * ac;
            distSq = (p - q).LengthSquared;
            u = 0f; v = t;                      // q = a + v*ac
            return q;
        }

        float va = d3 * d6 - d5 * d4;
        if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
        {
            float t = (d4 - d3) / ((d4 - d3) + (d5 - d6)); // along BC
            var q = b + t * (c - b);
            distSq = (p - q).LengthSquared;
            u = 1f - t; v = t;                 // since q = (1-t)B + tC = a + u*ab + v*ac
            return q;
        }

        // inside face
        float denom = 1.0f / (va + vb + vc);
        float uBary = vb * denom;   // weight of B
        float vBary = vc * denom;   // weight of C
        var inside = a + ab * uBary + ac * vBary;
        distSq = (p - inside).LengthSquared;
        u = uBary; v = vBary;       // exactly the requested (u,v)
        return inside;
    }

}
