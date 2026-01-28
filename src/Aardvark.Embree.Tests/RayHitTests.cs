using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for RayHit struct properties and behavior.
/// </summary>
public class RayHitTests
{
    [Fact]
    public void RayHit4_DefaultConstruction_HasValidLayout()
    {
        unsafe
        {
            _ = new RTCRayHit4();

            Assert.True(sizeof(RTCRayHit4) > 0);
        }
    }

    [Fact]
    public void RayHit4_SetRayOrigin_RetrievesCorrectly()
    {
        unsafe
        {
            var rayhit = new RTCRayHit4();

            for (int i = 0; i < 4; i++)
            {
                rayhit.ray.org_x[i] = i * 1.0f;
                rayhit.ray.org_y[i] = i * 2.0f;
                rayhit.ray.org_z[i] = i * 3.0f;
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i * 1.0f, rayhit.ray.org_x[i]);
                Assert.Equal(i * 2.0f, rayhit.ray.org_y[i]);
                Assert.Equal(i * 3.0f, rayhit.ray.org_z[i]);
            }
        }
    }

    [Fact]
    public void RayHit4_SetHitNormal_RetrievesCorrectly()
    {
        unsafe
        {
            var rayhit = new RTCRayHit4();

            for (int i = 0; i < 4; i++)
            {
                rayhit.hit.Ng_x[i] = i * 0.1f;
                rayhit.hit.Ng_y[i] = i * 0.2f;
                rayhit.hit.Ng_z[i] = i * 0.3f;
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i * 0.1f, rayhit.hit.Ng_x[i]);
                Assert.Equal(i * 0.2f, rayhit.hit.Ng_y[i]);
                Assert.Equal(i * 0.3f, rayhit.hit.Ng_z[i]);
            }
        }
    }

    [Fact]
    public void RayHit4_SetHitUV_RetrievesCorrectly()
    {
        unsafe
        {
            var rayhit = new RTCRayHit4();

            for (int i = 0; i < 4; i++)
            {
                rayhit.hit.u[i] = 0.25f * i;
                rayhit.hit.v[i] = 0.75f * i;
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(0.25f * i, rayhit.hit.u[i]);
                Assert.Equal(0.75f * i, rayhit.hit.v[i]);
            }
        }
    }

    [Fact]
    public void RayHit4_SetHitIds_RetrievesCorrectly()
    {
        unsafe
        {
            var rayhit = new RTCRayHit4();

            for (int i = 0; i < 4; i++)
            {
                rayhit.hit.primID[i] = (uint)(100 + i);
                rayhit.hit.geomID[i] = (uint)(200 + i);
                rayhit.hit.instID[i] = (uint)(300 + i);
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal((uint)(100 + i), rayhit.hit.primID[i]);
                Assert.Equal((uint)(200 + i), rayhit.hit.geomID[i]);
                Assert.Equal((uint)(300 + i), rayhit.hit.instID[i]);
            }
        }
    }

    [Fact]
    public void RayHit4_InitializeInvalidGeomID_SetsCorrectValue()
    {
        unsafe
        {
            var rayhit = new RTCRayHit4();

            for (int i = 0; i < 4; i++)
            {
                rayhit.hit.geomID[i] = unchecked((uint)-1);
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(unchecked((uint)-1), rayhit.hit.geomID[i]);
            }
        }
    }

    [Fact]
    public void RayHit8_DefaultConstruction_HasValidLayout()
    {
        unsafe
        {
            _ = new RTCRayHit8();

            Assert.True(sizeof(RTCRayHit8) > 0);
        }
    }

    [Fact]
    public void RayHit8_SetAllRayFields_RetrievesCorrectly()
    {
        unsafe
        {
            var rayhit = new RTCRayHit8();

            for (int i = 0; i < 8; i++)
            {
                rayhit.ray.org_x[i] = i * 10.0f;
                rayhit.ray.org_y[i] = i * 20.0f;
                rayhit.ray.org_z[i] = i * 30.0f;
                rayhit.ray.tnear[i] = 0.0f;

                rayhit.ray.dir_x[i] = 0.0f;
                rayhit.ray.dir_y[i] = 0.0f;
                rayhit.ray.dir_z[i] = 1.0f;
                rayhit.ray.time[i] = 0.0f;

                rayhit.ray.tfar[i] = float.MaxValue;
                rayhit.ray.mask[i] = 0xFFFFFFFFu;
                rayhit.ray.id[i] = (uint)i;
                rayhit.ray.flags[i] = 0;
            }

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(i * 10.0f, rayhit.ray.org_x[i]);
                Assert.Equal(i * 20.0f, rayhit.ray.org_y[i]);
                Assert.Equal(i * 30.0f, rayhit.ray.org_z[i]);
                Assert.Equal(0.0f, rayhit.ray.dir_x[i]);
                Assert.Equal(0.0f, rayhit.ray.dir_y[i]);
                Assert.Equal(1.0f, rayhit.ray.dir_z[i]);
                Assert.Equal((uint)i, rayhit.ray.id[i]);
            }
        }
    }

    [Fact]
    public void RayHit8_SetAllHitFields_RetrievesCorrectly()
    {
        unsafe
        {
            var rayhit = new RTCRayHit8();

            for (int i = 0; i < 8; i++)
            {
                rayhit.hit.Ng_x[i] = i * 0.5f;
                rayhit.hit.Ng_y[i] = i * 0.6f;
                rayhit.hit.Ng_z[i] = i * 0.7f;
                rayhit.hit.u[i] = 0.1f * i;
                rayhit.hit.v[i] = 0.2f * i;
                rayhit.hit.primID[i] = (uint)(10 + i);
                rayhit.hit.geomID[i] = (uint)(20 + i);
                rayhit.hit.instID[i] = (uint)(30 + i);
            }

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(i * 0.5f, rayhit.hit.Ng_x[i]);
                Assert.Equal(i * 0.6f, rayhit.hit.Ng_y[i]);
                Assert.Equal(i * 0.7f, rayhit.hit.Ng_z[i]);
                Assert.Equal(0.1f * i, rayhit.hit.u[i]);
                Assert.Equal(0.2f * i, rayhit.hit.v[i]);
                Assert.Equal((uint)(10 + i), rayhit.hit.primID[i]);
                Assert.Equal((uint)(20 + i), rayhit.hit.geomID[i]);
                Assert.Equal((uint)(30 + i), rayhit.hit.instID[i]);
            }
        }
    }

    [Fact]
    public void RayHit16_DefaultConstruction_HasValidLayout()
    {
        unsafe
        {
            _ = new RTCRayHit16();

            Assert.True(sizeof(RTCRayHit16) > 0);
        }
    }

    [Fact]
    public void RayHit16_SetAllRayFields_RetrievesCorrectly()
    {
        unsafe
        {
            var rayhit = new RTCRayHit16();

            for (int i = 0; i < 16; i++)
            {
                rayhit.ray.org_x[i] = i * 100.0f;
                rayhit.ray.org_y[i] = i * 200.0f;
                rayhit.ray.org_z[i] = i * 300.0f;
                rayhit.ray.tnear[i] = 0.001f;

                rayhit.ray.dir_x[i] = 1.0f;
                rayhit.ray.dir_y[i] = 0.0f;
                rayhit.ray.dir_z[i] = 0.0f;
                rayhit.ray.time[i] = 0.0f;

                rayhit.ray.tfar[i] = 10000.0f;
                rayhit.ray.mask[i] = 0xFFFFFFFFu;
                rayhit.ray.id[i] = (uint)(500 + i);
                rayhit.ray.flags[i] = 0;
            }

            for (int i = 0; i < 16; i++)
            {
                Assert.Equal(i * 100.0f, rayhit.ray.org_x[i]);
                Assert.Equal(i * 200.0f, rayhit.ray.org_y[i]);
                Assert.Equal(i * 300.0f, rayhit.ray.org_z[i]);
                Assert.Equal(1.0f, rayhit.ray.dir_x[i]);
                Assert.Equal(0.0f, rayhit.ray.dir_y[i]);
                Assert.Equal(0.0f, rayhit.ray.dir_z[i]);
                Assert.Equal((uint)(500 + i), rayhit.ray.id[i]);
            }
        }
    }

    [Fact]
    public void RayHit16_SetAllHitFields_RetrievesCorrectly()
    {
        unsafe
        {
            var rayhit = new RTCRayHit16();

            for (int i = 0; i < 16; i++)
            {
                rayhit.hit.Ng_x[i] = i * 1.0f;
                rayhit.hit.Ng_y[i] = i * 2.0f;
                rayhit.hit.Ng_z[i] = i * 3.0f;
                rayhit.hit.u[i] = 0.05f * i;
                rayhit.hit.v[i] = 0.15f * i;
                rayhit.hit.primID[i] = (uint)(1000 + i);
                rayhit.hit.geomID[i] = (uint)(2000 + i);
                rayhit.hit.instID[i] = (uint)(3000 + i);
            }

            for (int i = 0; i < 16; i++)
            {
                Assert.Equal(i * 1.0f, rayhit.hit.Ng_x[i]);
                Assert.Equal(i * 2.0f, rayhit.hit.Ng_y[i]);
                Assert.Equal(i * 3.0f, rayhit.hit.Ng_z[i]);
                Assert.Equal(0.05f * i, rayhit.hit.u[i]);
                Assert.Equal(0.15f * i, rayhit.hit.v[i]);
                Assert.Equal((uint)(1000 + i), rayhit.hit.primID[i]);
                Assert.Equal((uint)(2000 + i), rayhit.hit.geomID[i]);
                Assert.Equal((uint)(3000 + i), rayhit.hit.instID[i]);
            }
        }
    }

    [Fact]
    public void RayHit_WrapperStruct_DefaultConstruction()
    {
        var hit = new RayHit();

        Assert.Equal(0.0f, hit.T);
        Assert.Equal(V3f.Zero, hit.Normal);
        Assert.Equal(V2f.Zero, hit.Coord);
        Assert.Equal(0u, hit.PrimitiveId);
        Assert.Equal(0u, hit.GeometryId);
        Assert.Equal(0u, hit.InstanceId);
    }

    [Fact]
    public void RayHit_WrapperStruct_SetAllFields_RetrievesCorrectly()
    {
        var hit = new RayHit
        {
            T = 123.456f,
            Normal = new V3f(1.0f, 2.0f, 3.0f),
            Coord = new V2f(0.25f, 0.75f),
            PrimitiveId = 42,
            GeometryId = 7,
            InstanceId = 99
        };

        Assert.Equal(123.456f, hit.T);
        Assert.Equal(new V3f(1.0f, 2.0f, 3.0f), hit.Normal);
        Assert.Equal(new V2f(0.25f, 0.75f), hit.Coord);
        Assert.Equal(42u, hit.PrimitiveId);
        Assert.Equal(7u, hit.GeometryId);
        Assert.Equal(99u, hit.InstanceId);
    }
}
