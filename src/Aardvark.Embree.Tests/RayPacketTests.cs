using Xunit;

namespace Aardvark.Embree.Tests;

/// <summary>
/// Tests for ray packet intersection queries for improved performance.
/// </summary>
public class RayPacketTests
{
    [Fact]
    public void Ray4_DefaultConstruction_HasValidLayout()
    {
        unsafe
        {
            _ = new RTCRay4();

            // Verify structure can be instantiated
            Assert.True(sizeof(RTCRay4) > 0);
        }
    }

    [Fact]
    public void Ray4_SetOriginValues_RetrievesCorrectly()
    {
        unsafe
        {
            var ray = new RTCRay4();

            ray.org_x[0] = 1.0f;
            ray.org_y[0] = 2.0f;
            ray.org_z[0] = 3.0f;

            ray.org_x[1] = 4.0f;
            ray.org_y[1] = 5.0f;
            ray.org_z[1] = 6.0f;

            Assert.Equal(1.0f, ray.org_x[0]);
            Assert.Equal(2.0f, ray.org_y[0]);
            Assert.Equal(3.0f, ray.org_z[0]);

            Assert.Equal(4.0f, ray.org_x[1]);
            Assert.Equal(5.0f, ray.org_y[1]);
            Assert.Equal(6.0f, ray.org_z[1]);
        }
    }

    [Fact]
    public void Ray4_SetDirectionValues_RetrievesCorrectly()
    {
        unsafe
        {
            var ray = new RTCRay4();

            for (int i = 0; i < 4; i++)
            {
                ray.dir_x[i] = i * 1.0f;
                ray.dir_y[i] = i * 2.0f;
                ray.dir_z[i] = i * 3.0f;
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(i * 1.0f, ray.dir_x[i]);
                Assert.Equal(i * 2.0f, ray.dir_y[i]);
                Assert.Equal(i * 3.0f, ray.dir_z[i]);
            }
        }
    }

    [Fact]
    public void Ray4_SetRayParameters_TnearTfarTimeMaskIdFlags()
    {
        unsafe
        {
            var ray = new RTCRay4();

            for (int i = 0; i < 4; i++)
            {
                ray.tnear[i] = 0.001f * i;
                ray.tfar[i] = 1000.0f + i;
                ray.time[i] = 0.5f * i;
                ray.mask[i] = 0xFF000000u + (uint)i;
                ray.id[i] = 100u + (uint)i;
                ray.flags[i] = (uint)i;
            }

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(0.001f * i, ray.tnear[i]);
                Assert.Equal(1000.0f + i, ray.tfar[i]);
                Assert.Equal(0.5f * i, ray.time[i]);
                Assert.Equal(0xFF000000u + (uint)i, ray.mask[i]);
                Assert.Equal(100u + (uint)i, ray.id[i]);
                Assert.Equal((uint)i, ray.flags[i]);
            }
        }
    }

    [Fact]
    public void Ray8_DefaultConstruction_HasValidLayout()
    {
        unsafe
        {
            _ = new RTCRay8();

            Assert.True(sizeof(RTCRay8) > 0);
        }
    }

    [Fact]
    public void Ray8_SetAllFields_RetrievesCorrectly()
    {
        unsafe
        {
            var ray = new RTCRay8();

            for (int i = 0; i < 8; i++)
            {
                ray.org_x[i] = i * 1.0f;
                ray.org_y[i] = i * 2.0f;
                ray.org_z[i] = i * 3.0f;
                ray.tnear[i] = 0.0f;

                ray.dir_x[i] = i * 0.1f;
                ray.dir_y[i] = i * 0.2f;
                ray.dir_z[i] = i * 0.3f;
                ray.time[i] = 0.0f;

                ray.tfar[i] = float.MaxValue;
                ray.mask[i] = 0xFFFFFFFFu;
                ray.id[i] = (uint)i;
                ray.flags[i] = 0;
            }

            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(i * 1.0f, ray.org_x[i]);
                Assert.Equal(i * 2.0f, ray.org_y[i]);
                Assert.Equal(i * 3.0f, ray.org_z[i]);
                Assert.Equal(i * 0.1f, ray.dir_x[i]);
                Assert.Equal(i * 0.2f, ray.dir_y[i]);
                Assert.Equal(i * 0.3f, ray.dir_z[i]);
                Assert.Equal((uint)i, ray.id[i]);
            }
        }
    }

    [Fact]
    public void Ray16_DefaultConstruction_HasValidLayout()
    {
        unsafe
        {
            _ = new RTCRay16();

            Assert.True(sizeof(RTCRay16) > 0);
        }
    }

    [Fact]
    public void Ray16_SetAllFields_RetrievesCorrectly()
    {
        unsafe
        {
            var ray = new RTCRay16();

            for (int i = 0; i < 16; i++)
            {
                ray.org_x[i] = i * 10.0f;
                ray.org_y[i] = i * 20.0f;
                ray.org_z[i] = i * 30.0f;
                ray.tnear[i] = 0.001f;

                ray.dir_x[i] = 0.0f;
                ray.dir_y[i] = 0.0f;
                ray.dir_z[i] = -1.0f;
                ray.time[i] = 0.0f;

                ray.tfar[i] = 1000.0f;
                ray.mask[i] = 0xFFFFFFFFu;
                ray.id[i] = (uint)(1000 + i);
                ray.flags[i] = 0;
            }

            for (int i = 0; i < 16; i++)
            {
                Assert.Equal(i * 10.0f, ray.org_x[i]);
                Assert.Equal(i * 20.0f, ray.org_y[i]);
                Assert.Equal(i * 30.0f, ray.org_z[i]);
                Assert.Equal(0.0f, ray.dir_x[i]);
                Assert.Equal(0.0f, ray.dir_y[i]);
                Assert.Equal(-1.0f, ray.dir_z[i]);
                Assert.Equal((uint)(1000 + i), ray.id[i]);
            }
        }
    }
}
