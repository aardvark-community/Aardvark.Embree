using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Modern geometry instancing class that efficiently reuses geometry with different transforms.
/// This is the PREFERRED API for creating geometry instances.
/// </summary>
/// <remarks>
/// <b>When to use InstanceGeometry:</b>
/// <list type="bullet">
/// <item>Reusing the same geometry multiple times with different transforms (e.g., trees in a forest)</item>
/// <item>Creating hierarchical scenes (instances of instances)</item>
/// <item>Implementing motion blur with moving instances</item>
/// <item>Memory-efficient scene composition</item>
/// </list>
/// <para>
/// <b>Key advantages over legacy geometry instancing:</b>
/// - Motion blur support with multiple time steps via <see cref="SetTransforms"/>
/// - Time range control via <see cref="SetMotionBlurTimeRange"/>
/// - Transform interpolation queries via <see cref="GetTransformAtTime"/>
/// - Explicit build quality control
/// - Comprehensive documentation and examples
/// </para>
/// <para>
/// <b>Memory Efficiency:</b>
/// Instancing stores one copy of geometry data plus N transforms (48 bytes each).
/// For 1000 instances of a 10,000 triangle mesh:
/// - Direct duplication: ~458 MB
/// - Instancing: ~458 KB + 47 KB = ~505 KB (99.9% reduction)
/// </para>
/// <para>
/// <b>Usage:</b>
/// Create once, attach to scene, and commit. The internal scene is managed automatically.
/// See Examples/InstancingExample.cs for comprehensive usage patterns.
/// </para>
/// </remarks>
public class InstanceGeometry : EmbreeGeometry
{
    private IntPtr m_scene;
    private Affine3f m_transform;
    private readonly object m_sceneDisposeLock = new object();

    /// <summary>
    /// The transform applied to the instanced geometry.
    /// </summary>
    public Affine3f Transform
    {
        get
        {
            ThrowIfDisposed();
            return m_transform;
        }
        set
        {
            ThrowIfDisposed();
            m_transform = value;
            var mtx = (M34f)value;
            unsafe
            {
                float* ptr = (float*)&mtx;
                EmbreeAPI.rtcSetGeometryTransform(Handle, 0, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)ptr);
            }
            Commit();
        }
    }

    /// <summary>
    /// Creates a geometry instance with the specified transform.
    /// </summary>
    /// <param name="device">Device to create the instance on</param>
    /// <param name="geometry">Geometry to instance</param>
    /// <param name="transform">Transform to apply to the instance</param>
    /// <param name="quality">Build quality for the internal scene</param>
    /// <param name="sceneFlags">Optional scene flags for advanced configuration (default: None)</param>
    public InstanceGeometry(Device device, EmbreeGeometry geometry, Affine3f transform, RTCBuildQuality quality = RTCBuildQuality.High, RTCSceneFlags sceneFlags = RTCSceneFlags.None)
        : base(device, RTCGeometryType.Instance)
    {
        // Create internal scene to hold the instanced geometry
        m_scene = EmbreeAPI.rtcNewScene(device.Handle);
        device.CheckError("InstanceGeometry.rtcNewScene");

        // Configure scene
        EmbreeAPI.rtcSetSceneFlags(m_scene, sceneFlags);
        EmbreeAPI.rtcSetSceneBuildQuality(m_scene, quality);

        // Attach geometry to internal scene
        EmbreeAPI.rtcAttachGeometry(m_scene, geometry.Handle);
        device.CheckError("InstanceGeometry.rtcAttachGeometry");

        // Commit internal scene
        EmbreeAPI.rtcCommitScene(m_scene);
        device.CheckError("InstanceGeometry.rtcCommitScene");

        // Set this geometry as an instance of the scene
        EmbreeAPI.rtcSetGeometryInstancedScene(Handle, m_scene);

        // Set single time step (no motion blur)
        EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, 1);

        // Set transform
        Transform = transform;

        // Enable the instance geometry
        EmbreeAPI.rtcEnableGeometry(Handle);

        // Commit the instance geometry
        Commit();
    }

    /// <summary>
    /// Sets the transform for a specific time step (for motion blur).
    /// This enables motion blur for the instance geometry.
    /// </summary>
    /// <param name="timeStep">The time step index (0 to totalTimeSteps-1)</param>
    /// <param name="transform">The transform for this time step</param>
    /// <param name="totalTimeSteps">Total number of time steps (must be at least 2 for motion blur)</param>
    public void SetTransform(uint timeStep, Affine3f transform, uint totalTimeSteps)
    {
        ThrowIfDisposed();
        if (totalTimeSteps < 2)
            throw new ArgumentException("Motion blur requires at least 2 time steps", nameof(totalTimeSteps));

        if (timeStep >= totalTimeSteps)
            throw new ArgumentOutOfRangeException(nameof(timeStep), "Time step index must be less than total time steps");

        // Set the time step count if this is the first call
        if (timeStep == 0)
        {
            EmbreeAPI.rtcSetGeometryTimeStepCount(Handle, totalTimeSteps);
            m_device.CheckError("InstanceGeometry.rtcSetGeometryTimeStepCount");
        }

        // Set the transform for this time step
        var mtx = (M34f)transform;
        unsafe
        {
            float* ptr = (float*)&mtx;
            EmbreeAPI.rtcSetGeometryTransform(Handle, timeStep, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)ptr);
            m_device.CheckError($"InstanceGeometry.rtcSetGeometryTransform(timeStep={timeStep})");
        }

        // Store the transform if it's for time step 0
        if (timeStep == 0)
        {
            m_transform = transform;
        }
    }

    /// <summary>
    /// Sets transforms for all time steps (for motion blur).
    /// </summary>
    /// <param name="transforms">Array of transforms, one for each time step</param>
    public void SetTransforms(Affine3f[] transforms)
    {
        ThrowIfDisposed();
        if (transforms == null || transforms.Length < 2)
            throw new ArgumentException("At least 2 transforms required for motion blur", nameof(transforms));

        for (uint i = 0; i < transforms.Length; i++)
        {
            SetTransform(i, transforms[i], (uint)transforms.Length);
        }

        // Commit after all transforms are set
        Commit();
    }

    /// <summary>
    /// Sets the motion blur time range for this instance.
    /// Specifies the time interval over which motion blur is evaluated.
    /// </summary>
    /// <param name="startTime">Start time of motion blur range</param>
    /// <param name="endTime">End time of motion blur range</param>
    public void SetMotionBlurTimeRange(float startTime, float endTime)
    {
        ThrowIfDisposed();
        EmbreeAPI.rtcSetInstanceMotionBlurTimeRange(Handle, startTime, endTime);
        m_device.CheckError("InstanceGeometry.rtcSetInstanceMotionBlurTimeRange");
    }

    /// <summary>
    /// Gets the interpolated transformation at a specific time.
    /// Useful for querying motion blur transformations.
    /// </summary>
    /// <param name="time">Time at which to query the transformation</param>
    /// <returns>Interpolated transformation at the specified time</returns>
    public Affine3f GetTransformAtTime(float time)
    {
        ThrowIfDisposed();
        unsafe
        {
            M34f mtx = default;
            EmbreeAPI.rtcGetGeometryTransform(Handle, time, RTCFormat.FLOAT3X4_ROW_MAJOR, (IntPtr)(&mtx));
            m_device.CheckError($"InstanceGeometry.rtcGetGeometryTransform(time={time})");
            // M34f is row-major: [R00 R01 R02 T0], [R10 R11 R12 T1], [R20 R21 R22 T2]
            var rot = new M33f(mtx.M00, mtx.M01, mtx.M02,
                               mtx.M10, mtx.M11, mtx.M12,
                               mtx.M20, mtx.M21, mtx.M22);
            var trans = new V3f(mtx.M03, mtx.M13, mtx.M23);
            return new Affine3f(rot, trans);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (m_sceneDisposeLock)
            {
                var scene = m_scene;
                m_scene = IntPtr.Zero;  // Clear FIRST to prevent double-free
                if (scene != IntPtr.Zero)
                {
                    EmbreeAPI.rtcReleaseScene(scene);
                }
            }
        }
        base.Dispose(disposing);
    }
}
