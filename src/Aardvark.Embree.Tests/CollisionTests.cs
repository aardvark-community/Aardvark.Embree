using Aardvark.Base;
using System;
using System.Collections.Generic;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for Scene.Collide collision detection API.
/// Validates collision detection between scenes, callback invocation, and edge cases.
/// </summary>
public class CollisionTests
{
    /// <summary>
    /// Creates a simple scene with a single triangle on the specified device.
    /// </summary>
    private (Scene, TriangleGeometry) CreateTriangleScene(Device device, V3f[] vertices, int[] indices)
    {
        var geometry = new TriangleGeometry(device, vertices, indices, RTCBuildQuality.Medium);
        var scene = new Scene(device, RTCBuildQuality.Medium, dynamic: false);
        scene.AttachGeometry(geometry);
        scene.Commit();
        return (scene, geometry);
    }

    [Fact(DisplayName = "BasicCollisionDetection - Two overlapping geometries detect collision", Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void BasicCollisionDetection_OverlappingGeometries_DetectsCollision()
    {
        var device = new Device();
        try
        {
            // Create first scene with triangle in XY plane at Z=0
            var vertices1 = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
            var indices1 = new[] { 0, 1, 2 };
            var (scene1, geometry1) = CreateTriangleScene(device, vertices1, indices1);

            // Create second scene with overlapping triangle
            var vertices2 = new[] { new V3f(-0.5f, -0.5f, 0), new V3f(0.5f, -0.5f, 0), new V3f(0, 0.5f, 0) };
            var indices2 = new[] { 0, 1, 2 };
            var (scene2, geometry2) = CreateTriangleScene(device, vertices2, indices2);

            try
            {
                // Perform collision detection
                var collisions = scene1.Collide(scene2);

                // Verify collision was detected
                Assert.NotEmpty(collisions);
                Assert.True(collisions.Count > 0);

                // Verify collision result contains valid geometry and primitive IDs
                var firstCollision = collisions[0];
                Assert.True(firstCollision.GeometryId0 == 0 || firstCollision.GeometryId1 == 0);
                Assert.True(firstCollision.PrimitiveId0 == 0 || firstCollision.PrimitiveId1 == 0);
            }
            finally
            {
                scene2.Dispose();
                geometry2.Dispose();
                scene1.Dispose();
                geometry1.Dispose();
            }
        }
        finally
        {
            device.Dispose();
        }
    }

    [Fact(DisplayName = "NoCollision - Two non-overlapping geometries return no collision", Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void NoCollision_NonOverlappingGeometries_ReturnsEmpty()
    {
        var device = new Device();
        try
        {
            // Create first scene with triangle at Z=0
            var vertices1 = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
            var indices1 = new[] { 0, 1, 2 };
            var (scene1, geometry1) = CreateTriangleScene(device, vertices1, indices1);

            // Create second scene with triangle far away at X=10
            var vertices2 = new[] { new V3f(10, -1, 0), new V3f(12, -1, 0), new V3f(11, 1, 0) };
            var indices2 = new[] { 0, 1, 2 };
            var (scene2, geometry2) = CreateTriangleScene(device, vertices2, indices2);

            try
            {
                // Perform collision detection
                var collisions = scene1.Collide(scene2);

                // Verify no collisions detected
                Assert.Empty(collisions);
            }
            finally
            {
                scene2.Dispose();
                geometry2.Dispose();
                scene1.Dispose();
                geometry1.Dispose();
            }
        }
        finally
        {
            device.Dispose();
        }
    }

    [Fact(DisplayName = "CollisionWithEmptyScene - Handle empty scene gracefully", Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void CollisionWithEmptyScene_EmptyScene_ReturnsEmpty()
    {
        var device = new Device();
        try
        {
            // Create first scene with triangle
            var vertices1 = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
            var indices1 = new[] { 0, 1, 2 };
            var (scene1, geometry1) = CreateTriangleScene(device, vertices1, indices1);

            // Create empty scene on same device
            var scene2 = new Scene(device, RTCBuildQuality.Medium, dynamic: false);
            scene2.Commit();

            try
            {
                // Perform collision detection with empty scene
                var collisions = scene1.Collide(scene2);

                // Verify no collisions with empty scene
                Assert.Empty(collisions);
            }
            finally
            {
                scene2.Dispose();
                scene1.Dispose();
                geometry1.Dispose();
            }
        }
        finally
        {
            device.Dispose();
        }
    }

    [Fact(DisplayName = "CollisionCallback - Verify callback is invoked with correct data", Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void CollisionCallback_OverlappingGeometries_InvokesCallback()
    {
        var device = new Device();
        try
        {
            // Create first scene with triangle in XY plane at Z=0
            var vertices1 = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
            var indices1 = new[] { 0, 1, 2 };
            var (scene1, geometry1) = CreateTriangleScene(device, vertices1, indices1);

            // Create second scene with overlapping triangle
            var vertices2 = new[] { new V3f(-0.5f, -0.5f, 0), new V3f(0.5f, -0.5f, 0), new V3f(0, 0.5f, 0) };
            var indices2 = new[] { 0, 1, 2 };
            var (scene2, geometry2) = CreateTriangleScene(device, vertices2, indices2);

            try
            {
                var callbackInvoked = false;
                var collisionResults = new List<Scene.CollisionResult>();

                // Perform collision detection with callback
                scene1.Collide(scene2, collision =>
                {
                    callbackInvoked = true;
                    collisionResults.Add(collision);
                });

                // Verify callback was invoked
                Assert.True(callbackInvoked);
                Assert.NotEmpty(collisionResults);

                // Verify callback received valid collision data
                var firstCollision = collisionResults[0];
                Assert.True(firstCollision.GeometryId0 == 0 || firstCollision.GeometryId1 == 0);
                Assert.True(firstCollision.PrimitiveId0 == 0 || firstCollision.PrimitiveId1 == 0);
            }
            finally
            {
                scene2.Dispose();
                geometry2.Dispose();
                scene1.Dispose();
                geometry1.Dispose();
            }
        }
        finally
        {
            device.Dispose();
        }
    }

    [Fact(DisplayName = "MultipleCollisionPairs - Multiple overlapping pairs all detected", Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void MultipleCollisionPairs_MultipleOverlaps_DetectsAll()
    {
        var device = new Device();
        try
        {
            var scene1 = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

            // Create first scene with two triangles
            var vertices1a = new[] { new V3f(-2, -1, 0), new V3f(-1, -1, 0), new V3f(-1.5f, 1, 0) };
            var indices1a = new[] { 0, 1, 2 };
            var geometry1a = new TriangleGeometry(device, vertices1a, indices1a, RTCBuildQuality.Medium);
            scene1.AttachGeometry(geometry1a);

            var vertices1b = new[] { new V3f(1, -1, 0), new V3f(2, -1, 0), new V3f(1.5f, 1, 0) };
            var indices1b = new[] { 0, 1, 2 };
            var geometry1b = new TriangleGeometry(device, vertices1b, indices1b, RTCBuildQuality.Medium);
            scene1.AttachGeometry(geometry1b);

            scene1.Commit();

            var scene2 = new Scene(device, RTCBuildQuality.Medium, dynamic: false);

            // Create second scene with two overlapping triangles
            var vertices2a = new[] { new V3f(-2.5f, -0.5f, 0), new V3f(-1.5f, -0.5f, 0), new V3f(-2, 0.5f, 0) };
            var indices2a = new[] { 0, 1, 2 };
            var geometry2a = new TriangleGeometry(device, vertices2a, indices2a, RTCBuildQuality.Medium);
            scene2.AttachGeometry(geometry2a);

            var vertices2b = new[] { new V3f(0.5f, -0.5f, 0), new V3f(1.5f, -0.5f, 0), new V3f(1, 0.5f, 0) };
            var indices2b = new[] { 0, 1, 2 };
            var geometry2b = new TriangleGeometry(device, vertices2b, indices2b, RTCBuildQuality.Medium);
            scene2.AttachGeometry(geometry2b);

            scene2.Commit();

            try
            {
                // Perform collision detection
                var collisions = scene1.Collide(scene2);

                // Verify multiple collisions detected
                // Note: Embree may report multiple collision pairs depending on BVH traversal
                Assert.NotEmpty(collisions);

                // Verify all collisions have valid geometry and primitive IDs
                foreach (var collision in collisions)
                {
                    Assert.True(collision.GeometryId0 <= 1);
                    Assert.True(collision.GeometryId1 <= 1);
                    Assert.True(collision.PrimitiveId0 == 0);
                    Assert.True(collision.PrimitiveId1 == 0);
                }
            }
            finally
            {
                geometry2b.Dispose();
                geometry2a.Dispose();
                scene2.Dispose();
                geometry1b.Dispose();
                geometry1a.Dispose();
                scene1.Dispose();
            }
        }
        finally
        {
            device.Dispose();
        }
    }

    [Fact(DisplayName = "CollisionWithIdenticalScenes - Same scene collides with itself", Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void CollisionWithIdenticalScenes_SameGeometry_DetectsCollision()
    {
        var device = new Device();
        try
        {
            // Create scene with triangle
            var vertices = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
            var indices = new[] { 0, 1, 2 };
            var (scene, geometry) = CreateTriangleScene(device, vertices, indices);

            try
            {
                // Collide scene with itself
                var collisions = scene.Collide(scene);

                // Scene should collide with itself
                Assert.NotEmpty(collisions);
            }
            finally
            {
                scene.Dispose();
                geometry.Dispose();
            }
        }
        finally
        {
            device.Dispose();
        }
    }

    [Fact(DisplayName = "CollisionResult structure - Validates fields are populated correctly", Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void CollisionResult_Structure_ValidatesFieldsPopulated()
    {
        var device = new Device();
        try
        {
            // Create two overlapping scenes
            var vertices1 = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
            var indices1 = new[] { 0, 1, 2 };
            var (scene1, geometry1) = CreateTriangleScene(device, vertices1, indices1);

            var vertices2 = new[] { new V3f(-0.5f, -0.5f, 0), new V3f(0.5f, -0.5f, 0), new V3f(0, 0.5f, 0) };
            var indices2 = new[] { 0, 1, 2 };
            var (scene2, geometry2) = CreateTriangleScene(device, vertices2, indices2);

            try
            {
                var collisions = scene1.Collide(scene2);

                Assert.NotEmpty(collisions);

                var collision = collisions[0];

                // Verify all fields are accessible and have reasonable values
                Assert.True(collision.GeometryId0 >= 0);
                Assert.True(collision.PrimitiveId0 >= 0);
                Assert.True(collision.GeometryId1 >= 0);
                Assert.True(collision.PrimitiveId1 >= 0);
            }
            finally
            {
                scene2.Dispose();
                geometry2.Dispose();
                scene1.Dispose();
                geometry1.Dispose();
            }
        }
        finally
        {
            device.Dispose();
        }
    }

    [Fact(DisplayName = "CollisionCallback not invoked when no collision", Skip = "rtcCollide not available in Embree 4.4.0 build")]
    public void CollisionCallback_NoOverlap_CallbackNotInvoked()
    {
        var device = new Device();
        try
        {
            // Create two non-overlapping scenes
            var vertices1 = new[] { new V3f(-1, -1, 0), new V3f(1, -1, 0), new V3f(0, 1, 0) };
            var indices1 = new[] { 0, 1, 2 };
            var (scene1, geometry1) = CreateTriangleScene(device, vertices1, indices1);

            var vertices2 = new[] { new V3f(10, -1, 0), new V3f(12, -1, 0), new V3f(11, 1, 0) };
            var indices2 = new[] { 0, 1, 2 };
            var (scene2, geometry2) = CreateTriangleScene(device, vertices2, indices2);

            try
            {
                var callbackInvoked = false;

                scene1.Collide(scene2, collision =>
                {
                    callbackInvoked = true;
                });

                // Verify callback was not invoked
                Assert.False(callbackInvoked);
            }
            finally
            {
                scene2.Dispose();
                geometry2.Dispose();
                scene1.Dispose();
                geometry1.Dispose();
            }
        }
        finally
        {
            device.Dispose();
        }
    }
}
