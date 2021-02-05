using Aardvark.Base;
using Aardvark.Embree;
using System;
using System.Diagnostics;
using System.IO;
using Aardvark.SceneGraph;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Aardvark.Base.Aardvark.Init(); // PixImage DevIL init

            var device = new Device();

            Report.Line("Version={0}", device.Version);

            var geos = new[]
            {
                new EmbreeIndexedGeometry(device,
                    IndexedGeometryPrimitives.Sphere.solidPhiThetaSphere(new Sphere3d(V3d.OOO, 0.5), 1200, new C4b(160, 120, 190))),
                //new EmbreeIndexedGeometry(device,
                //    IndexedGeometryPrimitives.Quad.solidQuadrangle(V3d.OOO, V3d.IOO, V3d.IOI, V3d.OOI, new C4b(255, 255, 255), V3d.OIO)),
                //new EmbreeIndexedGeometry(device,
                //    IndexedGeometryPrimitives.Box.solidBox(new Box3d(V3d.OOO, V3d.III), new C4b(240, 150, 220))),
                //new EmbreeIndexedGeometry(device,
                //    IndexedGeometryPrimitives.Cone.solidCone(V3d.OOO, V3d.OOI, 1.0, 0.25, 12, new C4b(120, 130, 170))),
                //new EmbreeIndexedGeometry(device,
                //    IndexedGeometryPrimitives.Cylinder.solidCylinder(V3d.OOO, V3d.OOI, 1.0, 0.25, 0.15, 16, new C4b(250, 100, 140))),
                //new EmbreeIndexedGeometry(device,
                //    IndexedGeometryPrimitives.Torus.solidTorus(new Torus3d(V3d.OOO, V3d.OOI, 0.8, 0.2), new C4b(200, 180, 255), 12, 8)),
            };

            Report.Line("TriangleCount: {0}", geos.Sum(x => x.IndexedGeometry.FaceCount));
            
            var scene = new Scene(device, RTCBuildQuality.High, false);

            Report.BeginTimed("Create Scene");
            var geoMap = new Dictionary<uint, EmbreeIndexedGeometry>();

            // geometries with absolute transformation
            foreach (var g in geos)
                geoMap.Add(scene.Attach(g.EmbreeGeometry), g);

            //// instanced geometries with transformation matrix
            //foreach (var g in geos)
            //{
            //    var gi = new GeometryInstance(device, g.EmbreeGeometry, Affine3f.Identity);
            //    geoMap.Add(scene.Attach(gi), g);
            //}

            scene.Commit();
            Report.End();

            var cam = new CameraViewWithSky();
            cam.Location = V3d.III * 0.8;
            cam.LookAt(V3d.OOO);

            var vpSize = new V2i(3840, 2160);
            var proj = new CameraProjectionPerspective(60, 0.1, 10, vpSize.X / (double)vpSize.Y);

            var pix = RenderSimple(scene, vpSize.X, vpSize.Y, cam.ViewTrafo, proj.ProjectionTrafo, geoMap);

            device.Dispose();

            var file = "C:\\Debug\\Embree.png";
            pix.SaveAsImage(file);

            //Process.Start(file);
        }

        static PixImage RenderSimple(Scene scene, int width, int height, Trafo3d view, Trafo3d proj, Dictionary<uint, EmbreeIndexedGeometry> geos)
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

            for (int i = 0; i < 20; i++)
            {
                var sw = Stopwatch.StartNew();
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
                var rayCount = width * height;
                var raysPerSecond = rayCount / sw.Elapsed.TotalSeconds / 1e6;
                Report.Line("{0:0.00}MRay/s", raysPerSecond);
            }

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
    }

    public class EmbreeIndexedGeometry: IDisposable
    {
        public IndexedGeometry IndexedGeometry { get; }
        public TriangleGeometry EmbreeGeometry { get; private set; }

        public EmbreeIndexedGeometry(Device device, IndexedGeometry ig)
        {
            var pa = (V3f[])ig.IndexedAttributes[DefaultSemantic.Positions];
            var ia = (int[])ig.IndexArray;

            EmbreeGeometry = new TriangleGeometry(device, pa, ia, RTCBuildQuality.High);
            IndexedGeometry = ig;
        }

        public void Dispose()
        {
            EmbreeGeometry.Dispose();
            EmbreeGeometry = null;
        }
    }
}
