using Aardvark.Base;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public class GeometryInstance : EmbreeGeometry
{
    IntPtr m_scene;
    Affine3f m_transform;

    public Affine3f Transform
    {
        get { return m_transform; }
        set
        {
            unsafe
            {
                m_transform = value;
                var mtx = (M34f)value;
                unsafe
                {
                    float* ptr = (float*)&mtx;
                    EmbreeAPI.rtcSetGeometryTransform(Handle, 0, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)ptr);
                }
            }
        }
    }

    /// <summary>
    /// Single geometry instance
    /// </summary>
    public GeometryInstance(Device device, EmbreeGeometry geometry, Affine3f transform)
        : base(device, RTCGeometryType.Instance)
    {
        m_scene = EmbreeAPI.rtcNewScene(device.Handle);
        EmbreeAPI.rtcSetSceneFlags(m_scene, RTCSceneFlags.Robust | RTCSceneFlags.ContextFilterFunction);
        EmbreeAPI.rtcSetSceneBuildQuality(m_scene, RTCBuildQuality.High);
        EmbreeAPI.rtcAttachGeometry(m_scene, geometry.Handle);
        EmbreeAPI.rtcCommitScene(m_scene);

        EmbreeAPI.rtcSetGeometryInstancedScene(Handle, m_scene);
        EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, 1);
        Transform = transform;

        Commit();
    }

    public override void Dispose()
    {
        EmbreeAPI.rtcReleaseScene(m_scene);
        m_scene = IntPtr.Zero;
        base.Dispose();
    }
}

public struct RayHit
{
    public float T;
    public V3f Normal; // unnormalized geometry normal in object space
    public V2f Coord; // local hit coordinates
    public uint PrimitiveId;
    public uint GeometryId;
    public uint InstanceId;
}

public unsafe delegate void RTCFilterFunction(RTCFilterFunctionNArguments* args);

public partial class Scene : IDisposable
{
    // #define RTC_INVALID_GEOMETRY_ID ((uniform unsigned int)-1)
    private const uint RTC_INVALID_GEOMETRY_ID = unchecked((uint)-1);

    private readonly Device m_device;

    private readonly Dictionary<uint, EmbreeGeometry> m_geometries = [];

    public IntPtr Handle { get; private set; }

    public Scene(Device device, RTCBuildQuality quality, bool dynamic)
    {
        m_device = device;
        Handle = EmbreeAPI.rtcNewScene(device.Handle);
        var flags = RTCSceneFlags.Robust | RTCSceneFlags.ContextFilterFunction;
        if (dynamic) flags |= RTCSceneFlags.Dynamic;
        EmbreeAPI.rtcSetSceneFlags(Handle, flags);
        EmbreeAPI.rtcSetSceneBuildQuality(Handle, quality);
    }

    public EmbreeGeometry GetGeometry(uint id)
    {
        if (m_geometries.TryGetValue(id, out var geo))
            return geo;
        throw new ArgumentException("invalid id");
    }

    [Obsolete("Use AttachGeometry instead.")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Attach(EmbreeGeometry geometry) => AttachGeometry(geometry);

    public uint AttachGeometry(EmbreeGeometry geometry)
    {
        var id = EmbreeAPI.rtcAttachGeometry(Handle, geometry.Handle);
        m_device.CheckError("AddGeometry");

        m_geometries.Add(id, geometry);

        return id;
    }

    public void Attach(EmbreeGeometry geometry, uint id)
    {
        EmbreeAPI.rtcAttachGeometryByID(Handle, geometry.Handle, id);
        m_device.CheckError("AddGeometry");

        m_geometries.Add(id, geometry);
    }

    public void Commit()
    {
        EmbreeAPI.rtcCommitScene(Handle);
    }

    public void Dispose()
    {
        EmbreeAPI.rtcReleaseScene(Handle);
        Handle = IntPtr.Zero;
    }

    /// <summary>
    /// Gets the scene bounds
    /// </summary>
    public Box3f Bounds
    {
        get
        {
            EmbreeAPI.rtcGetSceneBounds(Handle, out var bounds);
            return new Box3f(bounds.lower, bounds.upper);
        }
    }

    public bool Intersect(V3f rayOrigin, V3f rayDirection, ref RayHit hit, float minT = 0.0f, float maxT = float.MaxValue, RTCFilterFunction filter = null)
    {
        // NOTE: rtcInitIntersectContext is an inline method -> do manually
        var ctx = new RTCIntersectContext()
        {
            instID = RTC_INVALID_GEOMETRY_ID, // need to be initialized with RTC_INVALID_GEOMETRY_ID
            filter = filter != null ? Marshal.GetFunctionPointerForDelegate(filter) : IntPtr.Zero,
        };

        var rayHit = new RTCRayHit()
        {
            ray = new RTCRay()
            {
                org = rayOrigin,
                dir = rayDirection,
                tnear = minT,
                tfar = maxT,
                flags = 0, // must be initialized with 0
                time = 0,
                mask = 0,
                id = 0,
            },
            hit = new RTCHit()
            {
                geomID = unchecked((uint)-1), // must be initialized to RTC_INVALID_GEOMETRY_ID
            }
        };

        unsafe
        {
            var ctxPt = &ctx;
            var rayHitPt = &rayHit;
            EmbreeAPI.rtcIntersect1(Handle, ctxPt, rayHitPt);
        }

        if (rayHit.hit.geomID != unchecked((uint)-1))
        {
            hit.T = rayHit.ray.tfar;
            hit.Coord = rayHit.hit.uv;
            hit.Normal = rayHit.hit.Ng;
            hit.PrimitiveId = rayHit.hit.primID;
            hit.GeometryId = rayHit.hit.geomID;
            hit.InstanceId = rayHit.hit.instID;

            return true;
        }

        return false;
    }

    public bool Occluded(V3f rayOrigin, V3f rayDirection, float minT = 0.0f, float maxT = float.MaxValue, RTCFilterFunction filter = null)
    {
        // NOTE: rtcInitIntersectContext is an inline method -> do manually
        var ctx = new RTCIntersectContext()
        {
            instID = RTC_INVALID_GEOMETRY_ID,
            filter = filter != null ? Marshal.GetFunctionPointerForDelegate(filter) : IntPtr.Zero,
        };

        var rtRay = new RTCRay()
        {
            org = rayOrigin,
            dir = rayDirection,
            tnear = minT,
            tfar = maxT,
            flags = 0, // must be initialized with 0
            time = 0,
            mask = 0,
            id = 0,
        };

        unsafe
        {
            var ctxPtr = &ctx;
            var rayPtr = &rtRay;
            EmbreeAPI.rtcOccluded1(Handle, ctxPtr, rayPtr);
        }

        // tfar set to -inf if intersection is found
        return rtRay.tfar == float.NegativeInfinity;
    }

}
