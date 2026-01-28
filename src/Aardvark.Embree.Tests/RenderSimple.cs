using Aardvark.Base;
using Aardvark.Rendering;
using Aardvark.SceneGraph;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Aardvark.Embree.Tests;

public class RenderSimpleTests
{
    [Theory]
    [InlineData(RTCBuildQuality.Low)]
    [InlineData(RTCBuildQuality.Medium)]
    [InlineData(RTCBuildQuality.High)]
    public void CanRenderSphereWithDifferentQuality(RTCBuildQuality quality)
    {
        var device = new Device();

        var geos = new[]
        {
            new EmbreeIndexedGeometry(device, quality,
                IndexedGeometryPrimitives.Sphere.solidPhiThetaSphere(new Sphere3d(V3d.OOO, 0.5), 1200, new C4b(160, 120, 190))),
        };

        var scene = new Scene(device, quality, false);

        var geoMap = new Dictionary<uint, EmbreeIndexedGeometry>();

        foreach (var g in geos)
            geoMap.Add(scene.AttachGeometry(g.EmbreeGeometry), g);

        scene.Commit();

        var cam = new CameraViewWithSky
        {
            Location = V3d.III * 0.8
        };
        cam.LookAt(V3d.OOO);

        var vpSize = new V2i(100, 100); // Small size for fast test
        var proj = new CameraProjectionPerspective(60, 0.1, 10, vpSize.X / (double)vpSize.Y);

        var pix = RenderSimpleImage(scene, vpSize.X, vpSize.Y, cam.ViewTrafo, proj.ProjectionTrafo, geoMap);

        Assert.NotNull(pix);
        Assert.Equal(100, pix.Size.X);
        Assert.Equal(100, pix.Size.Y);

        device.Dispose();
    }

    static PixImage RenderSimpleImage(Scene scene, int width, int height, Trafo3d view, Trafo3d proj, Dictionary<uint, EmbreeIndexedGeometry> geos)
    { 
        var img = new PixImage<byte>(width, height, 4);
        var mtx = img.GetMatrix<C4b>();

        var viewProj = view * proj;
        var invViewProj = viewProj.Backward;

        RTCFilterFunction filter = null;
        //unsafe
        //{
        //    filter = new RTCFilterFunction(ptr =>
        //    {
        //        //((uint*)ptr->valid)[0] = 0;
        //    });
        //}

        Parallel.For(0, height, new ParallelOptions(), y =>
        {
            for (int x = 0; x < width; x++)
            {
                var uv = (new V2d(x, y) + 0.5) / new V2d(width, height);

                var ray = GetCameraRay(uv, invViewProj);

                var color = GetColor(scene, ray, geos, filter);

                mtx[x, y] = color;
            }
        });

        return img;
    }

    static C4b GetColor(Scene scene, Ray3d ray, Dictionary<uint, EmbreeIndexedGeometry> geos, RTCFilterFunction filter = null)
    {
        var hit = new RayHit();
        if (scene.Intersect((V3f)ray.Origin, (V3f)ray.Direction, ref hit, 0, float.MaxValue, filter))
        {
            if (geos.TryGetValue(hit.GeometryId, out var geo))
            {
                var ca = (C4b[])geo.IndexedGeometry.IndexedAttributes[DefaultSemantic.Colors];
                return ca[0];
            }
        }
        return C4b.Black;
    }

    static Ray3d GetCameraRay(V2d uv, M44d invViewProj)
    {
        var deviceCoord = new V2d(uv.X * 2 - 1, -uv.Y * 2 + 1);

        var nearPoint = invViewProj.TransformPosProj(deviceCoord.XYO);
        var farPoint = invViewProj.TransformPosProj(deviceCoord.XYI);
        
        return new Ray3d(nearPoint, (farPoint - nearPoint).Normalized);
    }

    public class EmbreeIndexedGeometry : IDisposable
    {
        public IndexedGeometry IndexedGeometry { get; }
        public TriangleGeometry EmbreeGeometry { get; private set; }

        public EmbreeIndexedGeometry(Device device, RTCBuildQuality quality, IndexedGeometry ig)
        {
            var pa = (V3f[])ig.IndexedAttributes[DefaultSemantic.Positions];
            var ia = (int[])ig.IndexArray;

            EmbreeGeometry = new TriangleGeometry(device, pa, ia, quality);
            IndexedGeometry = ig;
        }

        public void Dispose()
        {
            EmbreeGeometry.Dispose();
            EmbreeGeometry = null;
            GC.SuppressFinalize(this);
        }
    }
}
