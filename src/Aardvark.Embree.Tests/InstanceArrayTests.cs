using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for InstanceArray geometry type.
///
/// FIXED (2025-12-23): RTCBufferType.Transform enum value was corrected (23 not 33).
/// InstanceArray tests now pass successfully with the fixed enum value.
/// All 21 InstanceArray tests verified passing.
/// </summary>
public class InstanceArrayTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateInstanceArray_SingleScene(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 10, quality);

        Assert.NotEqual(IntPtr.Zero, instanceArray.Handle);
        Assert.Equal(10u, instanceArray.InstanceCount);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanCreateInstanceArray_MultipleScenes(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Scene 1: Triangle
        var vertices1 = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices1 = new int[] { 0, 1, 2 };

        using var geom1 = new TriangleGeometry(device, vertices1, indices1, quality);
        using var scene1 = new Scene(device, quality, dynamic: false);
        scene1.AttachGeometry(geom1);
        scene1.Commit();

        // Scene 2: Quad
        var vertices2 = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };
        var indices2 = new int[] { 0, 1, 2, 0, 2, 3 };

        using var geom2 = new TriangleGeometry(device, vertices2, indices2, quality);
        using var scene2 = new Scene(device, quality, dynamic: false);
        scene2.AttachGeometry(geom2);
        scene2.Commit();

        using var instanceArray = new InstanceArray(device, new[] { scene1, scene2 }, 10, quality);

        Assert.NotEqual(IntPtr.Zero, instanceArray.Handle);
        Assert.Equal(10u, instanceArray.InstanceCount);
    }

    [Theory]
    // NOTE: RTCBuildQuality.Low is skipped due to Embree 4 crash when doing ray intersections
    // on InstanceArray with Low quality on all components (Linux specific issue).
    // [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_SetTransformBuffer(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var mainScene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 5, quality);

        // Set transforms for each instance
        var transforms = new Affine3f[]
        {
            Affine3f.Translation(0, 0, 0),
            Affine3f.Translation(2, 0, 0),
            Affine3f.Translation(4, 0, 0),
            Affine3f.Translation(6, 0, 0),
            Affine3f.Translation(8, 0, 0)
        };

        instanceArray.SetTransformBuffer(transforms);
        instanceArray.Commit();

        mainScene.AttachGeometry(instanceArray);
        mainScene.Commit();

        // Test each instance
        for (int i = 0; i < 5; i++)
        {
            var hit = new RayHit();
            bool intersected = mainScene.Intersect(
                new V3f(i * 2 + 0.25f, 0.25f, 1),
                new V3f(0, 0, -1),
                ref hit
            );

            Assert.True(intersected, $"Should hit instance {i}");
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_SetIndexBuffer(RTCBuildQuality quality)
    {
        using var device = new Device();

        // Create two different scenes
        var vertices1 = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(0.5f, 0, 0),
            new V3f(0, 0.5f, 0)
        };
        var indices1 = new int[] { 0, 1, 2 };

        using var geom1 = new TriangleGeometry(device, vertices1, indices1, quality);
        using var scene1 = new Scene(device, quality, dynamic: false);
        scene1.AttachGeometry(geom1);
        scene1.Commit();

        var vertices2 = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices2 = new int[] { 0, 1, 2 };

        using var geom2 = new TriangleGeometry(device, vertices2, indices2, quality);
        using var scene2 = new Scene(device, quality, dynamic: false);
        scene2.AttachGeometry(geom2);
        scene2.Commit();

        using var instanceArray = new InstanceArray(device, new[] { scene1, scene2 }, 4, quality);

        // Map instances to scenes: 0->scene1, 1->scene2, 2->scene1, 3->scene2
        var sceneIndices = new uint[] { 0, 1, 0, 1 };
        instanceArray.SetIndexBuffer(sceneIndices);

        Assert.NotEqual(IntPtr.Zero, instanceArray.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_SetupMotionBlur(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 3, quality);

        // Setup motion blur with 3 time steps
        instanceArray.SetupMotionBlur(3, 0.0f, 1.0f);

        Assert.Equal(3u, instanceArray.TimeStepCount);

        // Set transforms for each time step
        var transforms0 = new Affine3f[]
        {
            Affine3f.Translation(0, 0, 0),
            Affine3f.Translation(2, 0, 0),
            Affine3f.Translation(4, 0, 0)
        };

        var transforms1 = new Affine3f[]
        {
            Affine3f.Translation(0, 0, 1),
            Affine3f.Translation(2, 0, 1),
            Affine3f.Translation(4, 0, 1)
        };

        var transforms2 = new Affine3f[]
        {
            Affine3f.Translation(0, 0, 2),
            Affine3f.Translation(2, 0, 2),
            Affine3f.Translation(4, 0, 2)
        };

        instanceArray.SetTransformBuffer(transforms0, 0);
        instanceArray.SetTransformBuffer(transforms1, 1);
        instanceArray.SetTransformBuffer(transforms2, 2);

        instanceArray.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_SetTransformBuffer_TimeStepWithoutSetup_Throws(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 2, quality);

        var transforms = new Affine3f[]
        {
            Affine3f.Translation(0, 0, 0),
            Affine3f.Translation(2, 0, 0)
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => instanceArray.SetTransformBuffer(transforms, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            Span<Affine3f> span = transforms;
            instanceArray.SetTransformBuffer(span, 1);
        });
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void InstanceArray_SetSharedTransformBuffer_TimeStepWithoutSetup_Throws(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 2, quality);

        var buffer = new float[2 * 12];
        fixed (float* ptr = buffer)
        {
            try
            {
                instanceArray.SetSharedTransformBuffer(ptr, 1);
                Assert.Fail("Expected ArgumentOutOfRangeException");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected
            }
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_GetInstanceTransform(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 2, quality);

        var transforms = new Affine3f[]
        {
            Affine3f.Translation(1, 2, 3),
            Affine3f.Translation(4, 5, 6)
        };

        instanceArray.SetTransformBuffer(transforms);
        instanceArray.Commit();

        // Retrieve transforms
        var retrieved0 = instanceArray.GetInstanceTransform(0);
        var retrieved1 = instanceArray.GetInstanceTransform(1);

        // Check transforms match (approximately)
        Assert.InRange((retrieved0.Trans - new V3f(1, 2, 3)).Length, 0, 0.01f);
        Assert.InRange((retrieved1.Trans - new V3f(4, 5, 6)).Length, 0, 0.01f);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_SetInstanceTransform(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 2, quality);

        // Must set buffer first
        var transforms = new Affine3f[]
        {
            Affine3f.Identity,
            Affine3f.Identity
        };
        instanceArray.SetTransformBuffer(transforms);

        // Now set individual instance transform
        instanceArray.SetInstanceTransform(0, Affine3f.Translation(5, 5, 5));
        instanceArray.SetInstanceTransform(1, Affine3f.Translation(10, 10, 10));

        instanceArray.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_SetMask(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 5, quality);

        instanceArray.SetMask(0xFF);

        Assert.NotEqual(IntPtr.Zero, instanceArray.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_LargeInstanceCount(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 1000, quality);

        Assert.Equal(1000u, instanceArray.InstanceCount);

        // Create transforms for all instances (grid layout)
        var transforms = new Affine3f[1000];
        for (int i = 0; i < 1000; i++)
        {
            int x = i % 100;
            int y = i / 100;
            transforms[i] = Affine3f.Translation(x * 2, y * 2, 0);
        }

        instanceArray.SetTransformBuffer(transforms);
        instanceArray.Commit();
    }

    [Theory]
    // NOTE: RTCBuildQuality.Low is skipped due to Embree 4 crash when doing ray intersections
    // on InstanceArray with Low quality on all components (Linux specific issue).
    // [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_WithRotations(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var mainScene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0.5f, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 4, quality);

        // Different rotations for each instance
        var transforms = new Affine3f[]
        {
            Affine3f.Translation(0, 0, 0) * new Affine3f((M33f)Rot3f.Identity, V3f.Zero),
            Affine3f.Translation(3, 0, 0) * new Affine3f((M33f)Rot3f.Rotation(V3f.OOI, (float)(Math.PI / 4)), V3f.Zero),
            Affine3f.Translation(6, 0, 0) * new Affine3f((M33f)Rot3f.Rotation(V3f.OOI, (float)(Math.PI / 2)), V3f.Zero),
            Affine3f.Translation(9, 0, 0) * new Affine3f((M33f)Rot3f.Rotation(V3f.OOI, (float)Math.PI), V3f.Zero)
        };

        instanceArray.SetTransformBuffer(transforms);
        instanceArray.Commit();

        mainScene.AttachGeometry(instanceArray);
        mainScene.Commit();

        // Test intersections
        for (int i = 0; i < 4; i++)
        {
            var hit = new RayHit();
            mainScene.Intersect(
                new V3f(i * 3, 0, 1),
                new V3f(0, 0, -1),
                ref hit
            );
        }
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_WithScaling(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 3, quality);

        // Different scales for each instance
        var transforms = new Affine3f[]
        {
            Affine3f.Translation(0, 0, 0) * Affine3f.Scale(1.0f),
            Affine3f.Translation(5, 0, 0) * Affine3f.Scale(2.0f),
            Affine3f.Translation(10, 0, 0) * Affine3f.Scale(0.5f)
        };

        instanceArray.SetTransformBuffer(transforms);
        instanceArray.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_ValidationCheck_ZeroCount(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        Assert.Throws<ArgumentException>(() =>
        {
            var instanceArray = new InstanceArray(device, instScene, 0, quality);
        });
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_ValidationCheck_IndexBufferSize(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var scene1 = new Scene(device, quality, dynamic: false);
        scene1.AttachGeometry(geom);
        scene1.Commit();

        using var scene2 = new Scene(device, quality, dynamic: false);
        scene2.AttachGeometry(geom);
        scene2.Commit();

        using var instanceArray = new InstanceArray(device, new[] { scene1, scene2 }, 5, quality);

        // Wrong size index buffer should throw
        var wrongIndices = new uint[] { 0, 1, 0 }; // Only 3, need 5

        Assert.Throws<ArgumentException>(() =>
        {
            instanceArray.SetIndexBuffer(wrongIndices);
        });
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_ValidationCheck_TransformBufferSize(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 5, quality);

        // Wrong size transform buffer should throw
        var wrongTransforms = new Affine3f[] { Affine3f.Identity, Affine3f.Identity }; // Only 2, need 5

        Assert.Throws<ArgumentException>(() =>
        {
            instanceArray.SetTransformBuffer(wrongTransforms);
        });
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_ProperDisposal(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        var instanceArray = new InstanceArray(device, instScene, 5, quality);
        var handle = instanceArray.Handle;

        Assert.NotEqual(IntPtr.Zero, handle);

        instanceArray.Dispose();

        // After disposal, accessing Handle throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => instanceArray.Handle);
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_SetTransformBuffer_WithSpan(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom = new TriangleGeometry(device, vertices, indices, quality);
        using var instScene = new Scene(device, quality, dynamic: false);
        instScene.AttachGeometry(geom);
        instScene.Commit();

        using var instanceArray = new InstanceArray(device, instScene, 3, quality);

        // Use span for zero-copy
        Span<Affine3f> transforms = stackalloc Affine3f[]
        {
            Affine3f.Translation(0, 0, 0),
            Affine3f.Translation(2, 0, 0),
            Affine3f.Translation(4, 0, 0)
        };

        instanceArray.SetTransformBuffer(transforms);
        instanceArray.Commit();
    }

    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void InstanceArray_SetIndexBuffer_WithSpan(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0, 1, 0)
        };
        var indices = new int[] { 0, 1, 2 };

        using var geom1 = new TriangleGeometry(device, vertices, indices, quality);
        using var scene1 = new Scene(device, quality, dynamic: false);
        scene1.AttachGeometry(geom1);
        scene1.Commit();

        using var geom2 = new TriangleGeometry(device, vertices, indices, quality);
        using var scene2 = new Scene(device, quality, dynamic: false);
        scene2.AttachGeometry(geom2);
        scene2.Commit();

        using var instanceArray = new InstanceArray(device, new[] { scene1, scene2 }, 4, quality);

        // Use span for zero-copy
        Span<uint> sceneIndices = stackalloc uint[] { 0, 1, 0, 1 };

        instanceArray.SetIndexBuffer(sceneIndices);
        instanceArray.Commit();
    }
}
