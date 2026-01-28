using Aardvark.Base;
using System;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for displacement mapping on subdivision surfaces.
/// </summary>
public class DisplacementMappingTests
{
    [Theory(DisplayName = "Displacement function can be set on subdivision geometry")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void DisplacementFunction_CanBeSet(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        void DisplacementFunc(RTCDisplacementFunctionNArguments* args)
        {
            for (uint i = 0; i < args->N; i++)
            {
                args->P_x[i] = args->P_x[i];
                args->P_y[i] = args->P_y[i];
                args->P_z[i] = args->P_z[i];
            }
        }

        subdiv.SetDisplacementFunction(DisplacementFunc);
        subdiv.Commit();

        Assert.NotEqual(IntPtr.Zero, subdiv.Handle);
    }

    [Theory(DisplayName = "Displacement function is called during intersection")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void DisplacementFunction_CalledDuringIntersection(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(1, 1, 0),
            new V3f(-1, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.SmoothBoundary, 4.0f);

        bool displacementCalled = false;

        void DisplacementFunc(RTCDisplacementFunctionNArguments* args)
        {
            displacementCalled = true;
            for (uint i = 0; i < args->N; i++)
            {
                args->P_x[i] = args->P_x[i];
                args->P_y[i] = args->P_y[i];
                args->P_z[i] = args->P_z[i];
            }
        }

        subdiv.SetDisplacementFunction(DisplacementFunc);
        subdiv.Commit();

        scene.AttachGeometry(subdiv);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect subdivision surface");
        Assert.True(displacementCalled, "Displacement function should have been called");
    }

    [Theory(DisplayName = "Displacement function modifies geometry")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void DisplacementFunction_ModifiesGeometry(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(1, 1, 0),
            new V3f(-1, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.SmoothBoundary, 4.0f);

        float displacementAmount = 0.5f;

        void DisplacementFunc(RTCDisplacementFunctionNArguments* args)
        {
            for (uint i = 0; i < args->N; i++)
            {
                var nx = args->Ng_x[i];
                var ny = args->Ng_y[i];
                var nz = args->Ng_z[i];

                var length = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (length > 0)
                {
                    nx /= length;
                    ny /= length;
                    nz /= length;
                }

                args->P_x[i] = args->P_x[i] + nx * displacementAmount;
                args->P_y[i] = args->P_y[i] + ny * displacementAmount;
                args->P_z[i] = args->P_z[i] + nz * displacementAmount;
            }
        }

        subdiv.SetDisplacementFunction(DisplacementFunc);
        subdiv.Commit();

        scene.AttachGeometry(subdiv);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect displaced subdivision surface");
        Assert.True(hit.T > 0, "Hit distance should be positive");
    }

    [Theory(DisplayName = "Displacement function with height map pattern")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void DisplacementFunction_WithHeightMap(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices = new V3f[]
        {
            new V3f(-1, -1, 0),
            new V3f(1, -1, 0),
            new V3f(1, 1, 0),
            new V3f(-1, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality, RTCSubdivisionMode.SmoothBoundary, 8.0f);

        void HeightMapDisplacement(RTCDisplacementFunctionNArguments* args)
        {
            for (uint i = 0; i < args->N; i++)
            {
                float u = args->u[i];
                float v = args->v[i];

                float height = (float)(Math.Sin(u * Math.PI * 4) * Math.Cos(v * Math.PI * 4) * 0.1);

                var nx = args->Ng_x[i];
                var ny = args->Ng_y[i];
                var nz = args->Ng_z[i];

                var length = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (length > 0)
                {
                    nx /= length;
                    ny /= length;
                    nz /= length;
                }

                args->P_x[i] = args->P_x[i] + nx * height;
                args->P_y[i] = args->P_y[i] + ny * height;
                args->P_z[i] = args->P_z[i] + nz * height;
            }
        }

        subdiv.SetDisplacementFunction(HeightMapDisplacement);
        subdiv.Commit();

        scene.AttachGeometry(subdiv);
        scene.Commit();

        var hit = new RayHit();
        bool intersected = scene.Intersect(
            rayOrigin: new V3f(0, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit
        );

        Assert.True(intersected, "Ray should intersect height-mapped subdivision surface");
    }

    [Theory(DisplayName = "Displacement function throws on null")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void DisplacementFunction_ThrowsOnNull(RTCBuildQuality quality)
    {
        using var device = new Device();

        var vertices = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(1, 1, 0),
            new V3f(0, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv = new SubdivisionGeometry(device, vertices, indices, faces,
            quality);

        Assert.Throws<ArgumentNullException>(() =>
            subdiv.SetDisplacementFunction(null));
    }

    [Theory(DisplayName = "Multiple subdivisions with different displacement functions")]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public unsafe void MultipleSubdivisions_DifferentDisplacements(RTCBuildQuality quality)
    {
        using var device = new Device();
        using var scene = new Scene(device, quality, dynamic: false);

        var vertices1 = new V3f[]
        {
            new V3f(-2, -1, 0),
            new V3f(-1, -1, 0),
            new V3f(-1, 1, 0),
            new V3f(-2, 1, 0)
        };

        var vertices2 = new V3f[]
        {
            new V3f(1, -1, 0),
            new V3f(2, -1, 0),
            new V3f(2, 1, 0),
            new V3f(1, 1, 0)
        };

        var indices = new uint[] { 0, 1, 2, 3 };
        var faces = new uint[] { 4 };

        using var subdiv1 = new SubdivisionGeometry(device, vertices1, indices, faces,
            quality);
        using var subdiv2 = new SubdivisionGeometry(device, vertices2, indices, faces,
            quality);

        void Displacement1(RTCDisplacementFunctionNArguments* args)
        {
            for (uint i = 0; i < args->N; i++)
            {
                var nx = args->Ng_x[i];
                var ny = args->Ng_y[i];
                var nz = args->Ng_z[i];
                var length = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (length > 0)
                {
                    nx /= length;
                    ny /= length;
                    nz /= length;
                }
                args->P_x[i] = args->P_x[i] + nx * 0.2f;
                args->P_y[i] = args->P_y[i] + ny * 0.2f;
                args->P_z[i] = args->P_z[i] + nz * 0.2f;
            }
        }

        void Displacement2(RTCDisplacementFunctionNArguments* args)
        {
            for (uint i = 0; i < args->N; i++)
            {
                var nx = args->Ng_x[i];
                var ny = args->Ng_y[i];
                var nz = args->Ng_z[i];
                var length = (float)Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (length > 0)
                {
                    nx /= length;
                    ny /= length;
                    nz /= length;
                }
                args->P_x[i] = args->P_x[i] + nx * 0.5f;
                args->P_y[i] = args->P_y[i] + ny * 0.5f;
                args->P_z[i] = args->P_z[i] + nz * 0.5f;
            }
        }

        subdiv1.SetDisplacementFunction(Displacement1);
        subdiv1.Commit();

        subdiv2.SetDisplacementFunction(Displacement2);
        subdiv2.Commit();

        scene.AttachGeometry(subdiv1);
        scene.AttachGeometry(subdiv2);
        scene.Commit();

        var hit1 = new RayHit();
        bool intersected1 = scene.Intersect(
            rayOrigin: new V3f(-1.5f, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit1
        );

        var hit2 = new RayHit();
        bool intersected2 = scene.Intersect(
            rayOrigin: new V3f(1.5f, 0, 1),
            rayDirection: new V3f(0, 0, -1),
            ref hit2
        );

        Assert.True(intersected1, "Ray should intersect first subdivision surface");
        Assert.True(intersected2, "Ray should intersect second subdivision surface");
    }
}
