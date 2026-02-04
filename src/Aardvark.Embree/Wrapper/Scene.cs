using Aardvark.Base;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Represents a ray-geometry intersection result.
/// </summary>
public struct RayHit
{
    /// <summary>
    /// Gets or sets the ray parameter at the hit point (distance from ray origin).
    /// </summary>
    public float T;

    /// <summary>
    /// Gets or sets the unnormalized geometry normal at the hit point in object space.
    /// </summary>
    public V3f Normal;

    /// <summary>
    /// Gets or sets the local hit coordinates (barycentric coordinates for triangles).
    /// </summary>
    public V2f Coord;

    /// <summary>
    /// Gets or sets the primitive ID of the intersected geometry element.
    /// </summary>
    public uint PrimitiveId;

    /// <summary>
    /// Gets or sets the geometry ID within the scene.
    /// </summary>
    public uint GeometryId;

    /// <summary>
    /// Gets or sets the instance ID for instanced geometries.
    /// </summary>
    public uint InstanceId;
}

/// <summary>
/// Delegate for custom ray filter functions.
/// </summary>
/// <param name="args">Pointer to filter function arguments structure.</param>
public unsafe delegate void RTCFilterFunction(RTCFilterFunctionNArguments* args);

/// <summary>
/// Represents an Embree scene containing geometries for ray tracing.
/// </summary>
/// <remarks>
/// <para>
/// A Scene is a container for geometries and manages the BVH (Bounding Volume Hierarchy) acceleration structure.
/// After attaching geometries, call <see cref="Commit"/> to build or update the BVH.
/// </para>
/// <para>
/// Thread safety: Scene modification (attaching geometries, committing) is not thread-safe.
/// Ray queries (<see cref="Intersect(V3f, V3f, ref RayHit, float, float, RTCFilterFunction)"/> and <see cref="Occluded(V3f, V3f, float, float, RTCFilterFunction)"/>) are thread-safe after <see cref="Commit"/> is called.
/// </para>
/// <para>
/// Must be disposed to release native Embree resources. Dispose the scene before disposing the device.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// using var device = new Device();
/// using var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
///
/// // Attach geometries
/// var triangle = new TriangleGeometry(device, vertices, indices);
/// scene.AttachGeometry(triangle);
///
/// // Build BVH
/// scene.Commit();
///
/// // Perform ray queries
/// var hit = new RayHit();
/// if (scene.Intersect(new V3f(0, 0, 0), new V3f(0, 0, 1), ref hit))
/// {
///     Console.WriteLine($"Hit at distance {hit.T}");
/// }
/// </code>
/// </example>
public partial class Scene : IDisposable
{
    // #define RTC_INVALID_GEOMETRY_ID ((uniform unsigned int)-1)
    private const uint RTC_INVALID_GEOMETRY_ID = unchecked((uint)-1);

    private readonly Device m_device;

    private readonly Dictionary<uint, EmbreeGeometry> m_geometries = [];

    private IntPtr m_handle;
    private bool m_disposed = false;
    private readonly object m_disposeLock = new object();

    /// <summary>
    /// Throws ObjectDisposedException if this scene has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (m_disposed)
            throw new ObjectDisposedException(nameof(Scene));
    }

    /// <summary>
    /// Gets the native Embree scene handle.
    /// </summary>
    /// <remarks>
    /// This handle is required when calling low-level Embree API functions.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    public IntPtr Handle
    {
        get
        {
            ThrowIfDisposed();
            return m_handle;
        }
        private set => m_handle = value;
    }

    /// <summary>
    /// Creates a new Embree scene.
    /// </summary>
    /// <param name="device">The device to create the scene on.</param>
    /// <param name="quality">BVH build quality setting. Higher quality improves ray tracing performance but increases build time.</param>
    /// <param name="dynamic">If true, optimizes the scene for dynamic updates (allows faster rebuild after geometry changes).</param>
    /// <remarks>
    /// <para>
    /// The scene is configured with robust traversal and context filter function support.
    /// Dynamic scenes rebuild the BVH faster but may have slightly lower ray tracing performance.
    /// </para>
    /// <para>
    /// After creating the scene, attach geometries using <see cref="AttachGeometry"/> and call <see cref="Commit"/> to build the BVH.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Static scene with high quality BVH
    /// var scene = new Scene(device, RTCBuildQuality.High, dynamic: false);
    ///
    /// // Dynamic scene for animated geometry
    /// var dynamicScene = new Scene(device, RTCBuildQuality.Medium, dynamic: true);
    /// </code>
    /// </example>
    public Scene(Device device, RTCBuildQuality quality, bool dynamic)
    {
        if (quality == RTCBuildQuality.Refit)
            throw new ArgumentException("RTCBuildQuality.Refit is not valid for scenes. Use Low, Medium, or High for scenes; Refit applies to geometry updates.", nameof(quality));

        m_device = device;
        Handle = EmbreeAPI.rtcNewScene(device.Handle);
        m_device.CheckError("Scene.rtcNewScene");
        var flags = RTCSceneFlags.Robust | RTCSceneFlags.ContextFilterFunction;
        if (dynamic) flags |= RTCSceneFlags.Dynamic;
        EmbreeAPI.rtcSetSceneFlags(Handle, flags);
        m_device.CheckError("Scene.rtcSetSceneFlags");
        EmbreeAPI.rtcSetSceneBuildQuality(Handle, quality);
        m_device.CheckError("Scene.rtcSetSceneBuildQuality");
    }

    /// <summary>
    /// Gets the geometry object for the specified geometry ID.
    /// </summary>
    /// <param name="id">The geometry ID returned by <see cref="AttachGeometry"/>.</param>
    /// <returns>The geometry object associated with the ID.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    /// <exception cref="ArgumentException">Thrown if the ID is not found in the scene.</exception>
    public EmbreeGeometry GetGeometry(uint id)
    {
        ThrowIfDisposed();
        if (m_geometries.TryGetValue(id, out var geo))
            return geo;
        throw new ArgumentException("invalid id");
    }

    /// <summary>
    /// Attaches a geometry to the scene with an automatically assigned geometry ID.
    /// </summary>
    /// <param name="geometry">The geometry to attach.</param>
    /// <returns>The geometry ID assigned by Embree.</returns>
    /// <remarks>
    /// <para>
    /// After attaching all geometries, call <see cref="Commit"/> to build or update the BVH.
    /// The returned geometry ID can be used to retrieve the geometry later with <see cref="GetGeometry"/>.
    /// </para>
    /// <para>
    /// The geometry must not be disposed while attached to the scene.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if geometry is null.</exception>
    /// <example>
    /// <code>
    /// var triangle = new TriangleGeometry(device, vertices, indices);
    /// uint geometryId = scene.AttachGeometry(triangle);
    /// scene.Commit();
    /// </code>
    /// </example>
    public uint AttachGeometry(EmbreeGeometry geometry)
    {
        ThrowIfDisposed();
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        var id = EmbreeAPI.rtcAttachGeometry(Handle, geometry.Handle);
        m_device.CheckError("Scene.AttachGeometry");

        m_geometries.Add(id, geometry);

        return id;
    }

    /// <summary>
    /// Attaches a geometry to the scene with a specific geometry ID.
    /// </summary>
    /// <param name="geometry">The geometry to attach.</param>
    /// <param name="id">The desired geometry ID. Must not already be in use.</param>
    /// <remarks>
    /// <para>
    /// Use this method when you need to control the geometry IDs (e.g., for serialization or external references).
    /// After attaching all geometries, call <see cref="Commit"/> to build or update the BVH.
    /// </para>
    /// <para>
    /// The geometry must not be disposed while attached to the scene.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if geometry is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the ID is already in use.</exception>
    public void Attach(EmbreeGeometry geometry, uint id)
    {
        ThrowIfDisposed();
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        EmbreeAPI.rtcAttachGeometryByID(Handle, geometry.Handle, id);
        m_device.CheckError("Scene.AttachGeometryByID");

        m_geometries.Add(id, geometry);
    }

    /// <summary>
    /// Commits the scene and builds or updates the BVH acceleration structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method must be called after attaching or modifying geometries and before performing ray queries.
    /// Commit builds the BVH, which is required for efficient ray tracing.
    /// </para>
    /// <para>
    /// For dynamic scenes, call Commit after each frame to rebuild the BVH.
    /// For static scenes, call Commit once after initial setup.
    /// </para>
    /// <para>
    /// Performance: BVH build time depends on scene complexity and build quality setting.
    /// High quality builds take longer but produce faster ray queries.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    /// <example>
    /// <code>
    /// scene.AttachGeometry(geometry1);
    /// scene.AttachGeometry(geometry2);
    /// scene.Commit(); // Build BVH before ray tracing
    ///
    /// // Now safe to perform ray queries
    /// scene.Intersect(origin, direction, ref hit);
    /// </code>
    /// </example>
    public void Commit()
    {
        ThrowIfDisposed();
        EmbreeAPI.rtcCommitScene(Handle);
        m_device.CheckError("Scene.Commit");
    }

    /// <summary>
    /// Starts a non-blocking asynchronous commit of the scene to build or update the BVH acceleration structure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// In Embree 4, <c>rtcCommitScene</c> internally performs an asynchronous commit.
    /// This method starts the BVH build in the background, allowing the calling thread to continue other work.
    /// Call <see cref="CommitAsyncWait"/> to wait for the commit to complete before performing ray queries.
    /// </para>
    /// <para>
    /// Thread safety: This method is not thread-safe. Do not call from multiple threads simultaneously.
    /// After <see cref="CommitAsyncWait"/> returns, ray queries are thread-safe.
    /// </para>
    /// <para>
    /// Typical workflow:
    /// 1. Attach geometries to the scene
    /// 2. Call <see cref="CommitAsync"/> to start BVH build
    /// 3. Perform other work while BVH builds in background
    /// 4. Call <see cref="CommitAsyncWait"/> to wait for completion
    /// 5. Perform ray queries
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    /// <example>
    /// <code>
    /// scene.AttachGeometry(geometry1);
    /// scene.AttachGeometry(geometry2);
    /// scene.CommitAsync(); // Start BVH build in background
    ///
    /// // Do other work here while BVH builds
    /// PrepareRenderData();
    ///
    /// // Wait for BVH build to complete
    /// scene.CommitAsyncWait();
    ///
    /// // Now safe to perform ray queries
    /// scene.Intersect(origin, direction, ref hit);
    /// </code>
    /// </example>
    public void CommitAsync()
    {
        ThrowIfDisposed();
        EmbreeAPI.rtcCommitScene(Handle);
        m_device.CheckError("Scene.CommitAsync");
    }

    /// <summary>
    /// Waits for an asynchronous scene commit to complete.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method blocks until the BVH build started by <see cref="CommitAsync"/> completes.
    /// After this method returns, the scene is ready for ray queries.
    /// </para>
    /// <para>
    /// Thread safety: This method is not thread-safe with respect to scene modifications.
    /// After this method returns, ray queries are thread-safe.
    /// </para>
    /// <para>
    /// Must be called after <see cref="CommitAsync"/> and before performing ray queries.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    /// <example>
    /// <code>
    /// scene.CommitAsync(); // Start BVH build
    /// DoOtherWork();
    /// scene.CommitAsyncWait(); // Wait for BVH build to complete
    /// scene.Intersect(origin, direction, ref hit); // Now safe to query
    /// </code>
    /// </example>
    public void CommitAsyncWait()
    {
        ThrowIfDisposed();
        EmbreeAPI.rtcJoinCommitScene(Handle);
        m_device.CheckError("Scene.CommitAsyncWait");
    }

    /// <summary>
    /// Releases all resources used by the scene.
    /// </summary>
    /// <remarks>
    /// Disposes the native Embree scene handle. Dispose the scene before disposing the device.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the scene and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    /// <remarks>
    /// This method is thread-safe and uses double-check locking to ensure the scene is disposed exactly once.
    /// </remarks>
    protected virtual void Dispose(bool disposing)
    {
        lock (m_disposeLock)
        {
            if (m_disposed)
                return;

            if (m_handle != IntPtr.Zero)
            {
                EmbreeAPI.rtcReleaseScene(m_handle);
                m_handle = IntPtr.Zero;
            }

            m_disposed = true;
        }
    }

    /// <summary>
    /// Gets the axis-aligned bounding box of all geometries in the scene.
    /// </summary>
    /// <remarks>
    /// The bounds are computed by Embree and encompass all attached geometries.
    /// Call after <see cref="Commit"/> to get accurate bounds.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    public Box3f Bounds
    {
        get
        {
            ThrowIfDisposed();
            EmbreeAPI.rtcGetSceneBounds(Handle, out var bounds);
            return new Box3f(bounds.lower, bounds.upper);
        }
    }

    /// <summary>
    /// Finds the closest ray-geometry intersection in the scene.
    /// </summary>
    /// <param name="rayOrigin">Ray origin point.</param>
    /// <param name="rayDirection">Ray direction vector (does not need to be normalized).</param>
    /// <param name="hit">Output parameter containing hit information if an intersection is found.</param>
    /// <param name="minT">Minimum ray parameter (distance from origin). Default is 0.</param>
    /// <param name="maxT">Maximum ray parameter (distance from origin). Default is infinity.</param>
    /// <param name="filter">Optional custom filter function to programmatically accept or reject hits.</param>
    /// <returns>True if an intersection was found, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method is thread-safe after <see cref="Commit"/> has been called.
    /// Multiple threads can perform ray queries concurrently.
    /// </para>
    /// <para>
    /// The hit parameter is only modified if an intersection is found. If multiple primitives are hit,
    /// the closest intersection is returned.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    /// <example>
    /// <code>
    /// var hit = new RayHit();
    /// var rayOrigin = new V3f(0, 0, 0);
    /// var rayDirection = new V3f(0, 0, 1);
    ///
    /// if (scene.Intersect(rayOrigin, rayDirection, ref hit))
    /// {
    ///     Console.WriteLine($"Hit at distance {hit.T}");
    ///     Console.WriteLine($"Geometry ID: {hit.GeometryId}");
    ///     Console.WriteLine($"Normal: {hit.Normal}");
    /// }
    /// </code>
    /// </example>
    public bool Intersect(V3f rayOrigin, V3f rayDirection, ref RayHit hit, float minT = 0.0f, float maxT = float.MaxValue, RTCFilterFunction filter = null)
    {
        ThrowIfDisposed();
        return Intersect(rayOrigin, rayDirection, ref hit, minT, maxT, 0.0f, filter);
    }

    /// <summary>
    /// Finds the closest ray-geometry intersection in the scene at a specified time (for motion blur geometries).
    /// </summary>
    /// <param name="rayOrigin">Ray origin point.</param>
    /// <param name="rayDirection">Ray direction vector (does not need to be normalized).</param>
    /// <param name="hit">Output parameter containing hit information if an intersection is found.</param>
    /// <param name="minT">Minimum ray parameter (distance from origin).</param>
    /// <param name="maxT">Maximum ray parameter (distance from origin).</param>
    /// <param name="time">Time value in range [0, 1] for motion blur queries. 0 is start of motion, 1 is end.</param>
    /// <param name="filter">Optional custom filter function to programmatically accept or reject hits.</param>
    /// <returns>True if an intersection was found, false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method is thread-safe after <see cref="Commit"/> has been called.
    /// The time parameter interpolates geometry transforms and vertex positions for motion blur effects.
    /// </para>
    /// <para>
    /// For geometries without motion blur, the time parameter has no effect.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    public bool Intersect(V3f rayOrigin, V3f rayDirection, ref RayHit hit, float minT, float maxT, float time, RTCFilterFunction filter = null)
    {
        ThrowIfDisposed();
        // Allocate 16-byte aligned memory for RTCRayHit (required by Embree 4 SIMD)
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRayHit>() + 15];

        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
                // Align to 16-byte boundary
                IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
                RTCRayHit* rayHitPtr = (RTCRayHit*)alignedPtr;

                // Initialize ray
                rayHitPtr->ray.org = rayOrigin;
                rayHitPtr->ray.dir = rayDirection;
                rayHitPtr->ray.tnear = minT;
                rayHitPtr->ray.tfar = maxT;
                rayHitPtr->ray.flags = 0;
                rayHitPtr->ray.time = time;
                rayHitPtr->ray.mask = unchecked((uint)-1); // 0xFFFFFFFF - required for Embree 4
                rayHitPtr->ray.id = 0;

                // Initialize hit
                rayHitPtr->hit.geomID = unchecked((uint)-1); // RTC_INVALID_GEOMETRY_ID
                rayHitPtr->hit.primID = unchecked((uint)-1);
                rayHitPtr->hit.instID_0 = unchecked((uint)-1); // RTC_INVALID_GEOMETRY_ID

                var args = new RTCIntersectArguments()
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = filter != null ? Marshal.GetFunctionPointerForDelegate(filter) : IntPtr.Zero,
                    intersect = IntPtr.Zero,
                };

                var argsPt = &args;
                EmbreeAPI.rtcIntersect1(Handle, rayHitPtr, argsPt);

                // Check for hit and copy results
                if (rayHitPtr->hit.geomID != unchecked((uint)-1))
                {
                    hit.T = rayHitPtr->ray.tfar;
                    hit.Coord = rayHitPtr->hit.uv;
                    hit.Normal = rayHitPtr->hit.Ng;
                    hit.PrimitiveId = rayHitPtr->hit.primID;
                    hit.GeometryId = rayHitPtr->hit.geomID;
                    hit.InstanceId = rayHitPtr->hit.instID_0;

                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Tests whether a ray is occluded by any geometry in the scene (shadow ray query).
    /// </summary>
    /// <param name="rayOrigin">Ray origin point.</param>
    /// <param name="rayDirection">Ray direction vector (does not need to be normalized).</param>
    /// <param name="minT">Minimum ray parameter (distance from origin). Default is 0.</param>
    /// <param name="maxT">Maximum ray parameter (distance from origin). Default is infinity.</param>
    /// <param name="filter">Optional custom filter function to programmatically accept or reject occlusions.</param>
    /// <returns>True if the ray is occluded (hits any geometry), false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method is thread-safe after <see cref="Commit"/> has been called.
    /// Occlusion queries are faster than intersection queries because they return immediately upon finding any hit.
    /// </para>
    /// <para>
    /// Use this method for shadow rays and visibility tests where detailed hit information is not needed.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    /// <example>
    /// <code>
    /// // Shadow ray test: is the light visible from this point?
    /// var rayOrigin = surfacePoint;
    /// var rayDirection = lightPosition - surfacePoint;
    /// var maxDistance = rayDirection.Length;
    ///
    /// if (!scene.Occluded(rayOrigin, rayDirection, 0.001f, maxDistance))
    /// {
    ///     // Light is visible, add lighting contribution
    /// }
    /// </code>
    /// </example>
    public bool Occluded(V3f rayOrigin, V3f rayDirection, float minT = 0.0f, float maxT = float.MaxValue, RTCFilterFunction filter = null)
    {
        ThrowIfDisposed();
        return Occluded(rayOrigin, rayDirection, minT, maxT, 0.0f, filter);
    }

    /// <summary>
    /// Tests whether a ray is occluded by any geometry in the scene at a specified time (for motion blur geometries).
    /// </summary>
    /// <param name="rayOrigin">Ray origin point.</param>
    /// <param name="rayDirection">Ray direction vector (does not need to be normalized).</param>
    /// <param name="minT">Minimum ray parameter (distance from origin).</param>
    /// <param name="maxT">Maximum ray parameter (distance from origin).</param>
    /// <param name="time">Time value in range [0, 1] for motion blur queries. 0 is start of motion, 1 is end.</param>
    /// <param name="filter">Optional custom filter function to programmatically accept or reject occlusions.</param>
    /// <returns>True if the ray is occluded (hits any geometry), false otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method is thread-safe after <see cref="Commit"/> has been called.
    /// The time parameter interpolates geometry transforms and vertex positions for motion blur effects.
    /// </para>
    /// <para>
    /// For geometries without motion blur, the time parameter has no effect.
    /// </para>
    /// </remarks>
    /// <exception cref="ObjectDisposedException">Thrown if the scene has been disposed.</exception>
    public bool Occluded(V3f rayOrigin, V3f rayDirection, float minT, float maxT, float time, RTCFilterFunction filter = null)
    {
        ThrowIfDisposed();
        // Allocate 16-byte aligned memory for RTCRay (required by Embree 4 SIMD)
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<RTCRay>() + 15];

        unsafe
        {
            fixed (byte* bufferPtr = buffer)
            {
                // Align to 16-byte boundary
                IntPtr alignedPtr = new IntPtr((long)(bufferPtr + 15) & ~15L);
                RTCRay* rayPtr = (RTCRay*)alignedPtr;

                // Initialize ray
                rayPtr->org = rayOrigin;
                rayPtr->dir = rayDirection;
                rayPtr->tnear = minT;
                rayPtr->tfar = maxT;
                rayPtr->flags = 0;
                rayPtr->time = time;
                rayPtr->mask = unchecked((uint)-1); // 0xFFFFFFFF - required for Embree 4
                rayPtr->id = 0;

                var args = new RTCOccludedArguments()
                {
                    flags = RTCRayQueryFlags.None,
                    feature_mask = 0xFFFFFFFF,
                    context = IntPtr.Zero,
                    filter = filter != null ? Marshal.GetFunctionPointerForDelegate(filter) : IntPtr.Zero,
                    occluded = IntPtr.Zero,
                };

                var argsPtr = &args;
                EmbreeAPI.rtcOccluded1(Handle, rayPtr, argsPtr);

                // tfar set to -inf if intersection is found
                return rayPtr->tfar == float.NegativeInfinity;
            }
        }
    }

}
