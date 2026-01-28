using Aardvark.Base;
using System;
using System.Collections.Generic;

namespace Aardvark.Embree;

/// <summary>
/// Helper class for building scenes with motion blur support.
/// </summary>
public class MotionBlurSceneBuilder
{
    private readonly Device m_device;
    private readonly Scene m_scene;
    private readonly MotionBlurSettings m_settings;
    private readonly List<EmbreeGeometry> m_geometries = new List<EmbreeGeometry>();

    /// <summary>
    /// Gets the motion blur settings for this scene.
    /// </summary>
    public MotionBlurSettings Settings => m_settings;

    /// <summary>
    /// Gets the scene being built.
    /// </summary>
    public Scene Scene => m_scene;

    /// <summary>
    /// Creates a new motion blur scene builder.
    /// </summary>
    /// <param name="device">The device to create the scene on</param>
    /// <param name="settings">Motion blur settings (null for default)</param>
    /// <param name="quality">Build quality for the scene</param>
    /// <param name="dynamic">Whether the scene is dynamic (supports updates)</param>
    public MotionBlurSceneBuilder(Device device, MotionBlurSettings settings = null, RTCBuildQuality quality = RTCBuildQuality.High, bool dynamic = false)
    {
        m_device = device ?? throw new ArgumentNullException(nameof(device));
        m_settings = settings ?? MotionBlurSettings.Default;
        m_settings.Validate();

        m_scene = new Scene(device, quality, dynamic);
    }

    /// <summary>
    /// Adds a static geometry (no motion blur) to the scene.
    /// </summary>
    /// <param name="geometry">The geometry to add</param>
    /// <returns>The geometry ID in the scene</returns>
    public uint AddStaticGeometry(EmbreeGeometry geometry)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));

        m_geometries.Add(geometry);
        return m_scene.AttachGeometry(geometry);
    }

    /// <summary>
    /// Creates and adds a triangle geometry with motion blur.
    /// </summary>
    /// <param name="vertexTimeSteps">Vertex positions for each time step</param>
    /// <param name="triangleIndices">Triangle indices (shared across all time steps)</param>
    /// <param name="quality">Build quality for the geometry</param>
    /// <returns>The geometry ID in the scene</returns>
    public uint AddMotionBlurTriangles(ReadOnlyMemory<V3f>[] vertexTimeSteps, ReadOnlyMemory<int> triangleIndices, RTCBuildQuality quality = RTCBuildQuality.High)
    {
        if (vertexTimeSteps == null || vertexTimeSteps.Length < 2)
            throw new ArgumentException("At least 2 time steps required for motion blur", nameof(vertexTimeSteps));

        var geometry = new MotionBlurGeometry(m_device, vertexTimeSteps, triangleIndices, quality);

        // Set time range based on settings
        geometry.SetTimeRange(m_settings.ShutterOpenTime, m_settings.ShutterCloseTime);

        m_geometries.Add(geometry);
        return m_scene.AttachGeometry(geometry);
    }

    /// <summary>
    /// Creates and adds a triangle geometry with linear motion blur (2 time steps).
    /// </summary>
    /// <param name="verticesT0">Vertices at time 0</param>
    /// <param name="verticesT1">Vertices at time 1</param>
    /// <param name="triangleIndices">Triangle indices</param>
    /// <param name="quality">Build quality for the geometry</param>
    /// <returns>The geometry ID in the scene</returns>
    public uint AddLinearMotionBlurTriangles(ReadOnlyMemory<V3f> verticesT0, ReadOnlyMemory<V3f> verticesT1,
        ReadOnlyMemory<int> triangleIndices, RTCBuildQuality quality = RTCBuildQuality.High)
    {
        var geometry = new MotionBlurGeometry(m_device, verticesT0, verticesT1, triangleIndices, quality);

        // Set time range based on settings
        geometry.SetTimeRange(m_settings.ShutterOpenTime, m_settings.ShutterCloseTime);

        m_geometries.Add(geometry);
        return m_scene.AttachGeometry(geometry);
    }

    /// <summary>
    /// Creates and adds an instance geometry with motion blur transforms.
    /// </summary>
    /// <param name="sourceGeometry">The geometry to instance</param>
    /// <param name="transforms">Transforms for each time step</param>
    /// <param name="quality">Build quality for the instance</param>
    /// <returns>The geometry ID in the scene</returns>
    public uint AddMotionBlurInstance(EmbreeGeometry sourceGeometry, Affine3f[] transforms, RTCBuildQuality quality = RTCBuildQuality.High)
    {
        if (transforms == null || transforms.Length < 2)
            throw new ArgumentException("At least 2 transforms required for motion blur", nameof(transforms));

        var instance = new InstanceGeometry(m_device, sourceGeometry, transforms[0], quality);
        instance.SetTransforms(transforms);

        m_geometries.Add(instance);
        return m_scene.AttachGeometry(instance);
    }

    /// <summary>
    /// Commits the scene for rendering.
    /// Must be called after all geometries have been added.
    /// </summary>
    public void Commit()
    {
        m_scene.Commit();
    }

    /// <summary>
    /// Performs a ray intersection query at the specified time.
    /// </summary>
    /// <param name="rayOrigin">Ray origin</param>
    /// <param name="rayDirection">Ray direction</param>
    /// <param name="hit">Hit result</param>
    /// <param name="time">Time value in range [0, 1], or -1 to use middle of shutter time</param>
    /// <param name="minT">Minimum ray distance</param>
    /// <param name="maxT">Maximum ray distance</param>
    /// <returns>True if intersection found</returns>
    public bool Intersect(V3f rayOrigin, V3f rayDirection, ref RayHit hit, float time = -1.0f,
        float minT = 0.0f, float maxT = float.MaxValue)
    {
        // Use middle of shutter time if not specified
        if (time < 0)
        {
            time = (m_settings.ShutterOpenTime + m_settings.ShutterCloseTime) * 0.5f;
        }

        return m_scene.Intersect(rayOrigin, rayDirection, ref hit, minT, maxT, time);
    }

    /// <summary>
    /// Performs an occlusion query at the specified time.
    /// </summary>
    /// <param name="rayOrigin">Ray origin</param>
    /// <param name="rayDirection">Ray direction</param>
    /// <param name="time">Time value in range [0, 1], or -1 to use middle of shutter time</param>
    /// <param name="minT">Minimum ray distance</param>
    /// <param name="maxT">Maximum ray distance</param>
    /// <returns>True if ray is occluded</returns>
    public bool Occluded(V3f rayOrigin, V3f rayDirection, float time = -1.0f,
        float minT = 0.0f, float maxT = float.MaxValue)
    {
        // Use middle of shutter time if not specified
        if (time < 0)
        {
            time = (m_settings.ShutterOpenTime + m_settings.ShutterCloseTime) * 0.5f;
        }

        return m_scene.Occluded(rayOrigin, rayDirection, minT, maxT, time);
    }

    /// <summary>
    /// Disposes all geometries and the scene.
    /// </summary>
    public void Dispose()
    {
        foreach (var geometry in m_geometries)
        {
            geometry.Dispose();
        }
        m_geometries.Clear();

        m_scene.Dispose();
    }
}