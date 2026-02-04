using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

public class PerformanceOptimizationExampleTests
{
    [Fact]
    public void PerformanceOptimizationExample_SmokeTest()
    {
        using var device = new Device();

        CompareBvhQualities(device);
        TestRayCoherence(device);
        TestDynamicGeometry(device, RTCBuildQuality.Medium, RTCBuildQuality.Refit);
        TestDynamicGeometry(device, RTCBuildQuality.High);
    }

    private static void CompareBvhQualities(Device device)
    {
        var qualities = new[] { RTCBuildQuality.Low, RTCBuildQuality.Medium, RTCBuildQuality.High };
        for (int i = 0; i < qualities.Length; i++)
        {
            using var scene = CreateTestScene(device, qualities[i], dynamic: false);
            TraceCoherentRays(scene, 50);
        }
    }

    private static void TestRayCoherence(Device device)
    {
        using var scene = CreateTestScene(device, RTCBuildQuality.High, dynamic: false);
        TraceCoherentRays(scene, 50);
        TraceIncoherentRays(scene, 50);
    }

    private static void TestDynamicGeometry(Device device, RTCBuildQuality sceneQuality)
        => TestDynamicGeometry(device, sceneQuality, sceneQuality);

    private static void TestDynamicGeometry(Device device, RTCBuildQuality sceneQuality, RTCBuildQuality geometryQuality)
    {
        using var scene = new Scene(device, sceneQuality, dynamic: true);
        using var geometry = CreateTriangleGrid(device, 8, geometryQuality);
        scene.AttachGeometry(geometry);
        scene.Commit();

        for (int frame = 0; frame < 3; frame++)
        {
            DeformGeometry(geometry, 8, frame);
            geometry.UpdateBuffer(RTCBufferType.Vertex);
            geometry.Commit();
            scene.Commit();
            TraceCoherentRays(scene, 25);
        }
    }

    private static Scene CreateTestScene(Device device, RTCBuildQuality quality, bool dynamic)
    {
        var scene = new Scene(device, quality, dynamic);
        using var geometry = CreateTriangleGrid(device, 8, quality);
        scene.AttachGeometry(geometry);
        scene.Commit();
        return scene;
    }

    private static TriangleGeometry CreateTriangleGrid(Device device, int size, RTCBuildQuality quality)
    {
        var vertices = new V3f[(size + 1) * (size + 1)];
        var indices = new int[size * size * 6];

        int vIdx = 0;
        for (int y = 0; y <= size; y++)
        {
            for (int x = 0; x <= size; x++)
            {
                vertices[vIdx++] = new V3f(x, y, 0);
            }
        }

        int iIdx = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int v0 = y * (size + 1) + x;
                int v1 = v0 + 1;
                int v2 = v0 + (size + 1);
                int v3 = v2 + 1;

                indices[iIdx++] = v0;
                indices[iIdx++] = v1;
                indices[iIdx++] = v2;

                indices[iIdx++] = v1;
                indices[iIdx++] = v3;
                indices[iIdx++] = v2;
            }
        }

        return new TriangleGeometry(device, vertices, indices, quality);
    }

    private static unsafe void DeformGeometry(TriangleGeometry geometry, int gridSize, int frame)
    {
        var ptr = geometry.GetVertexDataPointer();
        float time = frame * 0.1f;
        int size = gridSize + 1;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;
                float wave = (float)Math.Sin(x * 0.2f + time) * (float)Math.Cos(y * 0.2f + time);
                ptr[idx] = new V3f(x, y, wave * 0.5f);
            }
        }
    }

    private static void TraceCoherentRays(Scene scene, int count)
    {
        var origin = new V3f(4f, 4f, 20);
        var hit = new RayHit();

        for (int i = 0; i < count; i++)
        {
            float u = (i % 8) / 8f;
            float v = ((i / 8) % 8) / 8f;
            var target = new V3f(u * 8, v * 8, 0);
            var direction = (target - origin).Normalized;
            scene.Intersect(origin, direction, ref hit);
        }
    }

    private static void TraceIncoherentRays(Scene scene, int count)
    {
        var random = new Random(42);
        var hit = new RayHit();

        for (int i = 0; i < count; i++)
        {
            var origin = new V3f(
                (float)random.NextDouble() * 8,
                (float)random.NextDouble() * 8,
                10 + (float)random.NextDouble() * 10
            );

            var direction = new V3f(
                (float)random.NextDouble() * 2 - 1,
                (float)random.NextDouble() * 2 - 1,
                -(float)random.NextDouble()
            ).Normalized;

            scene.Intersect(origin, direction, ref hit);
        }
    }
}
