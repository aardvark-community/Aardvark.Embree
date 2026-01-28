using Aardvark.Base;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

/// <summary>
/// Helper class for performing collision detection and point queries.
/// Provides high-level wrappers for Embree's collision and point query APIs.
/// </summary>
public static class Collision
{
    /// <summary>
    /// Result of a point query operation.
    /// </summary>
    public readonly struct PointQueryResult
    {
        /// <summary>Whether a valid point was found within the query radius</summary>
        public readonly bool IsValid;
        /// <summary>The closest point on the geometry surface</summary>
        public readonly V3f Point;
        /// <summary>Local UV coordinates on the primitive</summary>
        public readonly V2f UV;
        /// <summary>Squared distance from query point to surface point</summary>
        public readonly float DistanceSquared;
        /// <summary>Geometry ID of the closest primitive</summary>
        public readonly uint GeometryId;
        /// <summary>Primitive ID of the closest primitive</summary>
        public readonly uint PrimitiveId;

        /// <summary>
        /// Creates a new point query result.
        /// </summary>
        public PointQueryResult(bool isValid, V3f point, V2f uv, float distSq, uint geomId, uint primId)
        {
            IsValid = isValid;
            Point = point;
            UV = uv;
            DistanceSquared = distSq;
            GeometryId = geomId;
            PrimitiveId = primId;
        }

        /// <summary>Gets the distance (not squared)</summary>
        public float Distance => IsValid ? (float)Math.Sqrt(DistanceSquared) : float.PositiveInfinity;
    }

    /// <summary>
    /// Performs a single point query to find the closest point on geometry surfaces.
    /// This is a lower-level API that requires manual callback implementation.
    /// For a simpler API, use Scene.GetClosestPoint() instead.
    /// </summary>
    /// <param name="scene">The scene to query</param>
    /// <param name="queryPoint">The query point in world space</param>
    /// <param name="maxRadius">Maximum search radius (default: infinite)</param>
    /// <param name="callback">User callback invoked for each potential closest primitive</param>
    /// <param name="userPtr">User data pointer passed to callback</param>
    /// <returns>True if the query was successful</returns>
    public static unsafe bool PointQuery(
        Scene scene,
        V3f queryPoint,
        float maxRadius,
        RTCPointQueryFunction callback,
        IntPtr userPtr)
    {
        if (scene == null)
            throw new ArgumentNullException(nameof(scene));
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        var query = new RTCPointQuery
        {
            p = queryPoint,
            time = 0f,
            radius = float.IsPositiveInfinity(maxRadius) ? 1e30f : maxRadius
        };

        // Initialize context
        RTCPointQueryContext context = default;
        context.instID = unchecked((uint)-1); // RTC_INVALID_GEOMETRY_ID
        context.instStackSize = 0;

        var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);
        return EmbreeAPI.rtcPointQuery(scene.Handle, &query, &context, callbackPtr, userPtr);
    }

    /// <summary>
    /// Performs a packet of 4 point queries simultaneously.
    /// This can be more efficient than 4 separate queries due to SIMD optimizations.
    /// </summary>
    /// <param name="scene">The scene to query</param>
    /// <param name="points">Array of 4 query points</param>
    /// <param name="maxRadii">Array of 4 maximum search radii</param>
    /// <param name="callback">User callback invoked for each potential closest primitive</param>
    /// <param name="userPtrs">Array of 4 user data pointers passed to callbacks</param>
    /// <returns>True if the query was successful</returns>
    public static unsafe bool PointQuery4(
        Scene scene,
        V3f[] points,
        float[] maxRadii,
        RTCPointQueryFunction callback,
        IntPtr[] userPtrs)
    {
        if (scene == null)
            throw new ArgumentNullException(nameof(scene));
        if (points == null || points.Length != 4)
            throw new ArgumentException("Must provide exactly 4 points", nameof(points));
        if (maxRadii == null || maxRadii.Length != 4)
            throw new ArgumentException("Must provide exactly 4 radii", nameof(maxRadii));
        if (userPtrs == null || userPtrs.Length != 4)
            throw new ArgumentException("Must provide exactly 4 user pointers", nameof(userPtrs));
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        var query = new RTCPointQuery4();
        for (int i = 0; i < 4; i++)
        {
            query.x[i] = points[i].X;
            query.y[i] = points[i].Y;
            query.z[i] = points[i].Z;
            query.time[i] = 0f;
            query.radius[i] = float.IsPositiveInfinity(maxRadii[i]) ? 1e30f : maxRadii[i];
        }

        int* valid = stackalloc int[4];
        for (int i = 0; i < 4; i++)
            valid[i] = -1; // All valid

        RTCPointQueryContext context = default;
        context.instID = unchecked((uint)-1);
        context.instStackSize = 0;

        var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);
        fixed (IntPtr* pUserPtrs = userPtrs)
        {
            return EmbreeAPI.rtcPointQuery4(valid, scene.Handle, &query, &context, callbackPtr, pUserPtrs);
        }
    }

    /// <summary>
    /// Performs collision detection between two scenes.
    /// Returns all pairs of overlapping primitives.
    /// This is a convenience wrapper - Scene.Collide() provides the same functionality.
    /// </summary>
    /// <param name="scene0">First scene</param>
    /// <param name="scene1">Second scene</param>
    /// <returns>List of collision results</returns>
    public static List<Scene.CollisionResult> DetectCollisions(Scene scene0, Scene scene1)
    {
        if (scene0 == null)
            throw new ArgumentNullException(nameof(scene0));
        if (scene1 == null)
            throw new ArgumentNullException(nameof(scene1));

        return scene0.Collide(scene1);
    }

    /// <summary>
    /// Performs collision detection with a custom callback.
    /// This is a convenience wrapper - Scene.Collide() provides the same functionality.
    /// </summary>
    /// <param name="scene0">First scene</param>
    /// <param name="scene1">Second scene</param>
    /// <param name="callback">Callback invoked for each collision pair</param>
    public static void DetectCollisions(Scene scene0, Scene scene1, Action<Scene.CollisionResult> callback)
    {
        if (scene0 == null)
            throw new ArgumentNullException(nameof(scene0));
        if (scene1 == null)
            throw new ArgumentNullException(nameof(scene1));
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        scene0.Collide(scene1, callback);
    }

    /// <summary>
    /// Sets a custom collision callback on a geometry for per-geometry collision filtering.
    /// Note: This function may not be available in all Embree builds.
    /// </summary>
    /// <param name="geometry">The geometry to set the callback on</param>
    /// <param name="callback">The collision callback delegate</param>
    public static void SetGeometryCollisionCallback(EmbreeGeometry geometry, RTCCollideFunc callback)
    {
        if (geometry == null)
            throw new ArgumentNullException(nameof(geometry));
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));

        var funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
        EmbreeAPI.rtcSetGeometryCollideCallback(geometry.Handle, funcPtr);
    }
}

/// <summary>
/// Extension methods for collision detection on Scene objects.
/// These provide additional convenience methods beyond the built-in Scene.Collide methods.
/// </summary>
public static class CollisionExtensions
{
    /// <summary>
    /// Checks if two scenes have any overlapping primitives.
    /// This is more efficient than getting all collisions if you only need to know if there's any collision.
    /// </summary>
    /// <param name="scene">The first scene</param>
    /// <param name="otherScene">The second scene</param>
    /// <returns>True if any primitives overlap</returns>
    public static bool HasCollision(this Scene scene, Scene otherScene)
    {
        if (scene == null)
            throw new ArgumentNullException(nameof(scene));
        if (otherScene == null)
            throw new ArgumentNullException(nameof(otherScene));

        bool foundCollision = false;
        scene.Collide(otherScene, _ => { foundCollision = true; });
        return foundCollision;
    }

    /// <summary>
    /// Counts the number of colliding primitive pairs between two scenes.
    /// </summary>
    /// <param name="scene">The first scene</param>
    /// <param name="otherScene">The second scene</param>
    /// <returns>Number of overlapping primitive pairs</returns>
    public static int CountCollisions(this Scene scene, Scene otherScene)
    {
        if (scene == null)
            throw new ArgumentNullException(nameof(scene));
        if (otherScene == null)
            throw new ArgumentNullException(nameof(otherScene));

        return scene.Collide(otherScene).Count;
    }
}
