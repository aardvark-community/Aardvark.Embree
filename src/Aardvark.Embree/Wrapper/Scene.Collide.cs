using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Aardvark.Embree;

public partial class Scene
{
    /// <summary>
    /// Result of a collision query between two geometries.
    /// </summary>
    public readonly struct CollisionResult
    {
        /// <summary>Geometry ID in first scene</summary>
        public readonly uint GeometryId0;
        /// <summary>Primitive ID in first geometry</summary>
        public readonly uint PrimitiveId0;
        /// <summary>Geometry ID in second scene</summary>
        public readonly uint GeometryId1;
        /// <summary>Primitive ID in second geometry</summary>
        public readonly uint PrimitiveId1;

        /// <summary>
        /// Creates a new collision result.
        /// </summary>
        public CollisionResult(uint geomId0, uint primId0, uint geomId1, uint primId1)
        {
            GeometryId0 = geomId0;
            PrimitiveId0 = primId0;
            GeometryId1 = geomId1;
            PrimitiveId1 = primId1;
        }
    }

    /// <summary>
    /// Performs collision detection between this scene and another scene.
    /// Returns all pairs of overlapping primitives between the two scenes.
    /// </summary>
    /// <param name="otherScene">The other scene to test collision against</param>
    /// <returns>List of collision results</returns>
    public unsafe List<CollisionResult> Collide(Scene otherScene)
    {
        ThrowIfDisposed();
        if (otherScene == null)
            throw new ArgumentNullException(nameof(otherScene));
        var results = new List<CollisionResult>();
        var resultsHandle = GCHandle.Alloc(results, GCHandleType.Normal);

        try
        {
            // Create callback delegate and pin it to prevent GC during native call
            // The delegate must stay alive for the entire duration of rtcCollide
            var callback = new RTCCollideFunc(CollideCallback);
            var callbackHandle = GCHandle.Alloc(callback, GCHandleType.Normal);

            try
            {
                var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);
                EmbreeAPI.rtcCollide(Handle, otherScene.Handle, callbackPtr, GCHandle.ToIntPtr(resultsHandle));
            }
            finally
            {
                callbackHandle.Free();
            }

            return results;
        }
        finally
        {
            resultsHandle.Free();
        }
    }

    /// <summary>
    /// Performs collision detection with a custom callback for each collision.
    /// </summary>
    /// <param name="otherScene">The other scene to test collision against</param>
    /// <param name="callback">Callback invoked for each overlapping primitive pair</param>
    public unsafe void Collide(Scene otherScene, Action<CollisionResult> callback)
    {
        ThrowIfDisposed();
        if (otherScene == null)
            throw new ArgumentNullException(nameof(otherScene));
        if (callback == null)
            throw new ArgumentNullException(nameof(callback));
        var callbackHandle = GCHandle.Alloc(callback, GCHandleType.Normal);

        try
        {
            // Create and pin the native callback delegate
            var collideFunc = new RTCCollideFunc(CollideCallbackAction);
            var funcHandle = GCHandle.Alloc(collideFunc, GCHandleType.Normal);

            try
            {
                var funcPtr = Marshal.GetFunctionPointerForDelegate(collideFunc);
                EmbreeAPI.rtcCollide(Handle, otherScene.Handle, funcPtr, GCHandle.ToIntPtr(callbackHandle));
            }
            finally
            {
                funcHandle.Free();
            }
        }
        finally
        {
            callbackHandle.Free();
        }
    }

    /// <summary>
    /// Native callback for collecting collisions into a list.
    /// Receives an array of RTCCollision structs from Embree.
    /// </summary>
    private static unsafe void CollideCallback(IntPtr userPtr, RTCCollision* collisions, UIntPtr numCollisions)
    {
        try
        {
            var gch = GCHandle.FromIntPtr(userPtr);
            var results = (List<CollisionResult>)gch.Target;

            // Process all collisions in the array
            var count = (int)numCollisions;
            for (int i = 0; i < count; i++)
            {
                var collision = collisions[i];
                results.Add(new CollisionResult(
                    collision.geomID0,
                    collision.primID0,
                    collision.geomID1,
                    collision.primID1
                ));
            }
        }
        catch (Exception ex)
        {
            // Log exception but don't propagate to native code
            Aardvark.Base.Report.Warn($"[Embree] Exception in CollideCallback: {ex}");
        }
    }

    /// <summary>
    /// Native callback for invoking user-provided action for each collision.
    /// Receives an array of RTCCollision structs from Embree.
    /// </summary>
    private static unsafe void CollideCallbackAction(IntPtr userPtr, RTCCollision* collisions, UIntPtr numCollisions)
    {
        try
        {
            var gch = GCHandle.FromIntPtr(userPtr);
            var callback = (Action<CollisionResult>)gch.Target;

            // Invoke callback for each collision in the array
            var count = (int)numCollisions;
            for (int i = 0; i < count; i++)
            {
                var collision = collisions[i];
                var result = new CollisionResult(
                    collision.geomID0,
                    collision.primID0,
                    collision.geomID1,
                    collision.primID1
                );
                callback(result);
            }
        }
        catch (Exception ex)
        {
            // Log exception but don't propagate to native code
            Aardvark.Base.Report.Warn($"[Embree] Exception in CollideCallbackAction: {ex}");
        }
    }
}
