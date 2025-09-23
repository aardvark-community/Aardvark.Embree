using Aardvark.Base;
using Aardvark.Embree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Test;

internal static class BvhTest
{
    public static void Run()
    {
        Aardvark.Base.Aardvark.Init(); // PixImage DevIL init

        Report.Line("=============================================================================================)");

        // generate triangles
        Report.BeginTimed("generating 1M random triangles");
        var random = new Random(42);
        var triangles = new Triangle3d[1_000_000];
        for (int i = 0; i < triangles.Length; i++)
        {
            var center = new V3d(
                (random.NextDouble() - 0.5) * 100,
                (random.NextDouble() - 0.5) * 100,
                (random.NextDouble() - 0.5) * 100
            );

            // Generate a small random triangle around the center
            var size = 0.1 + random.NextDouble() * 1.9;
            var p0 = center + new V3d((random.NextDouble() - 0.5) * size, (random.NextDouble() - 0.5) * size, (random.NextDouble() - 0.5) * size);
            var p1 = center + new V3d((random.NextDouble() - 0.5) * size, (random.NextDouble() - 0.5) * size, (random.NextDouble() - 0.5) * size);
            var p2 = center + new V3d((random.NextDouble() - 0.5) * size, (random.NextDouble() - 0.5) * size, (random.NextDouble() - 0.5) * size);

            triangles[i] = new Triangle3d(p0, p1, p2);
        }
        Report.EndTimed();

        // generate rays

        Report.BeginTimed("generating 1M random rays");
        var rays = new Ray3d[1000000];
        var raysF = new Ray3f[rays.Length];
        {
            for (var i = 0; i < rays.Length; i++)
            {
                rays[i] = new Ray3d(
                    new V3d((random.NextDouble() - 0.5) * 200, (random.NextDouble() - 0.5) * 200, (random.NextDouble() - 0.5) * 200),
                    new V3d(random.NextDouble() - 0.5, random.NextDouble() - 0.5, random.NextDouble() - 0.5).Normalized
                );

                raysF[i] = new Ray3f((V3f)rays[i].Origin, (V3f)rays[i].Direction);
            }
        }

        // for nearest distance queries
        var qs = new V3f[1000000];
        {
            for (var i = 0; i < qs.Length; i++)
            {
                qs[i] = new V3f(
                    (random.NextDouble() - 0.5) * 100,
                    (random.NextDouble() - 0.5) * 100,
                    (random.NextDouble() - 0.5) * 100
                );
            }
        }

        Report.EndTimed();

        {
            Report.Line("=============================================================================================)");
            Report.BeginTimed("building scene");

            var device = new Device();

            Report.Line("Version={0}", device.Version);
            Report.Line("RayMaskSupported={0}", device.RayMaskSupported);
            Report.Line("FilterFunctionSupported={0}", device.FilterFunctionSupported);


            var pa = MemoryMarshal.Cast<Triangle3d, V3d>(triangles).ToArray().Map(p => (V3f)p);
            var ia = new int[pa.Length]; for (var i = 0; i < pa.Length; i++) ia[i] = i;
            var geometry = new TriangleGeometry(device, pa, ia, RTCBuildQuality.High);

            var scene = new Scene(device, RTCBuildQuality.High, false);
            var id = scene.Attach(geometry);
            scene.Commit();

            Report.EndTimed();

            Report.Line("=============================================================================================)");
            Report.BeginTimed("compute ray intersections");
            var hitCount = 0;
            //for (var j = 0; j < 10; j++)
            {
                for (var i = 0; i < raysF.Length; i++)
                {
                    var rayF = raysF[i];
                    var hit = new RayHit();
                    if (scene.Intersect(rayF.Origin, rayF.Direction, ref hit, 0, float.MaxValue, filter: default))
                    {
                        hitCount++;
                        //Report.Line($"Ray {i}: t = {hit.T}");
                    }
                }
            }
            var totalSeconds = Report.EndTimed();
            Report.Line($"avg {qs.Length / totalSeconds:N0} intersections/sec");
            Report.Line($"hits {hitCount:N0}/{rays.Length}");

            Report.Line("=============================================================================================)");
            Report.BeginTimed("compute nearest distance");
            for (var i = 0; i < qs.Length; i++)
            {
                    var foo = scene.Nearest(qs[i]);
                    //Report.Line($"{foo.PointWS}");
            }
            totalSeconds = Report.EndTimed();
            Report.Line($"avg {qs.Length / totalSeconds:N0} queries/sec");
        }


        Report.Line("=============================================================================================)");

    }
}
