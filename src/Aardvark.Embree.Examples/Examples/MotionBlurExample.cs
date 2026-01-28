using System;
using Aardvark.Base;
using Aardvark.Embree;
using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples.Examples;

/// <summary>
/// Motion Blur Example - Demonstrates temporal effects and animation
///
/// This example shows:
/// - Linear motion blur with 2 time steps (t=0 and t=1)
/// - Multi-segment motion blur with N time steps
/// - Vertex-based motion (deforming geometry)
/// - Transform-based motion (rigid body animation)
/// - Temporal sampling across shutter time
/// - Linear and quaternion interpolation concepts
/// - Frame-by-frame animation rendering
///
/// Motion blur is essential for:
/// - Realistic rendering of fast-moving objects
/// - Film and video production (simulates camera shutter)
/// - Physics simulation visualization
/// - Temporal antialiasing
/// - VFX and animation pipelines
/// </summary>
public class MotionBlurExample : ExampleBase
{
    public override string Title => "Motion Blur Animation";
    public override string Description => "Temporal effects, vertex motion, and transform animation";
    public override string Category => "Motion Blur";
    public override int OrderIndex => 5;
    public override int ApproximateLineCount => 400;

    protected override void Execute()
    {
        PrintSection("Overview");
        Print("Motion blur simulates the camera shutter being open over a time interval.");
        Print("Objects moving during this interval appear blurred along their motion path.");
        Print("");
        Print("Two types of motion blur:");
        Print("  1. Vertex Motion Blur: Deforming geometry (vertices move independently)");
        Print("  2. Transform Motion Blur: Rigid body motion (entire object moves/rotates)");
        Print("");

        // Demo 1: Simple bouncing ball with linear motion blur
        Demo1_BouncingBall();

        // Demo 2: Rotating cube with multi-segment vertex motion blur
        Demo2_RotatingCube();

        // Demo 3: Multiple objects at different time samples
        Demo3_TemporalSampling();

        // Demo 4: Motion vectors and speed analysis
        Demo4_MotionAnalysis();

        PrintSection("Summary");
        Print("Motion blur adds realism by capturing temporal information:");
        Print("  * Shutter time controls blur length (0.0 to 1.0 range)");
        Print("  * More time steps = smoother but more memory");
        Print("  * Use rtcSetGeometryTimeStepCount() for motion blur");
        Print("  * Query at specific times with ray.time parameter");
        Print("  * Essential for film rendering, VFX, and physics visualization");
    }

    /// <summary>
    /// Demo 1: Bouncing Ball with Linear Motion Blur
    ///
    /// Demonstrates:
    /// - MotionBlurGeometry with 2 time steps
    /// - Sphere moving from bottom to top (bounce motion)
    /// - Ray queries at different time values (t = 0.0, 0.5, 1.0)
    /// - How geometry position changes over time
    /// </summary>
    private void Demo1_BouncingBall()
    {
        PrintSection("Demo 1: Bouncing Ball (Linear Motion Blur)");
        Print("Creating a sphere that bounces from y=0 to y=3...");
        Print("");

        var (verticesT0, indices) = CreateSphere(center: new V3f(0, 0, 0), radius: 0.5f, segments: 16);
        var (verticesT1, _) = CreateSphere(center: new V3f(0, 3, 0), radius: 0.5f, segments: 16);

        Print($"Sphere: {verticesT0.Length} vertices, {indices.Length / 3} triangles");
        Print($"  Time 0: center at (0, 0, 0)");
        Print($"  Time 1: center at (0, 3, 0)");
        Print("");

        using var scene = new Scene(Device!, RTCBuildQuality.High, dynamic: false);
        using var motionBlurSphere = new MotionBlurGeometry(
            Device!,
            new ReadOnlyMemory<V3f>(verticesT0),
            new ReadOnlyMemory<V3f>(verticesT1),
            new ReadOnlyMemory<int>(indices),
            RTCBuildQuality.High);

        scene.AttachGeometry(motionBlurSphere);
        // Time step count must be set before geometry commit—Embree builds interpolated BVH structure at commit time
        scene.Commit();

        Print("Shooting rays at different time values:");
        Print("");

        var rayOrigin = new V3f(-5, 1.5f, 0);
        var rayDirection = new V3f(1, 0, 0).Normalized;

        var timeValues = new[] { 0.0f, 0.5f, 1.0f };

        foreach (var time in timeValues)
        {
            var hit = new RayHit();
            bool didHit = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time);

            if (didHit)
            {
                var hitPos = rayOrigin + rayDirection * hit.T;
                Print($"  Time {time:F1}: HIT at distance {hit.T:F3}");
                Print($"    Hit position: ({hitPos.X:F2}, {hitPos.Y:F2}, {hitPos.Z:F2})");
                Print($"    Expected Y: {InterpolateFloat(0f, 3f, time):F2}");
            }
            else
            {
                Print($"  Time {time:F1}: MISS");
            }
        }

        Print("");
        Print("Notice: The hit position Y-coordinate matches the interpolated sphere center.");
        Print("This demonstrates how Embree interpolates vertex positions across time.");
        Print("");
    }

    /// <summary>
    /// Demo 2: Rotating Cube with Multi-Segment Motion Blur
    ///
    /// Demonstrates:
    /// - Multi-segment motion blur (8 time steps)
    /// - Rotation motion captured at multiple keyframes
    /// - Smoother interpolation with more time steps
    /// - Vertex generation for intermediate frames
    /// </summary>
    private void Demo2_RotatingCube()
    {
        PrintSection("Demo 2: Rotating Cube (Multi-Segment Motion Blur)");
        Print("Creating a cube that rotates 180 degrees around the Y-axis...");
        Print("");

        const int timeSteps = 8;
        Print($"Using {timeSteps} time steps for smooth rotation");
        Print("");

        var cubeTimeSteps = new ReadOnlyMemory<V3f>[timeSteps];
        var indices = CreateCubeIndices();

        for (int i = 0; i < timeSteps; i++)
        {
            float t = i / (float)(timeSteps - 1);
            float angle = t * (float)Math.PI;

            var transform = Affine3f.RotationY(angle);
            var baseVertices = CreateCubeVertices(size: 1.0f);
            var rotatedVertices = new V3f[baseVertices.Length];

            for (int v = 0; v < baseVertices.Length; v++)
            {
                rotatedVertices[v] = transform.TransformPos(baseVertices[v]);
            }

            cubeTimeSteps[i] = new ReadOnlyMemory<V3f>(rotatedVertices);
        }

        Print("Time step rotations:");
        for (int i = 0; i < timeSteps; i++)
        {
            float t = i / (float)(timeSteps - 1);
            float angle = t * 180f;
            Print($"  Step {i}: {angle:F1}° rotation (time={t:F3})");
        }
        Print("");

        using var scene = new Scene(Device!, RTCBuildQuality.High, dynamic: false);
        using var rotatingCube = new MotionBlurGeometry(
            Device!,
            cubeTimeSteps,
            new ReadOnlyMemory<int>(indices),
            RTCBuildQuality.High);

        scene.AttachGeometry(rotatingCube);
        scene.Commit();

        Print("Sampling cube at specific times:");
        Print("");

        var rayOrigin = new V3f(0, 0, 5);
        var rayDirection = new V3f(0, 0, -1);

        var sampleTimes = new[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };

        foreach (var time in sampleTimes)
        {
            var hit = new RayHit();
            bool didHit = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time);
            float expectedAngle = time * 180f;

            Print($"  Time {time:F2} (angle {expectedAngle:F1}°): {(didHit ? "HIT" : "MISS")} distance={(didHit ? hit.T.ToString("F2") : "N/A")}");
        }

        Print("");
        Print("The cube visibility changes as it rotates - demonstrating temporal sampling.");
        Print("");
    }

    /// <summary>
    /// Demo 3: Temporal Sampling - Frame-by-Frame Animation
    ///
    /// Demonstrates:
    /// - Rendering multiple frames across shutter time
    /// - Creating an animated sequence
    /// - Visualizing motion trails
    /// - MotionBlurSettings configuration
    /// </summary>
    private void Demo3_TemporalSampling()
    {
        PrintSection("Demo 3: Temporal Sampling (Frame-by-Frame Animation)");
        Print("Simulating an animation sequence with a moving sphere...");
        Print("");

        // 16 time steps captures circular path without aliasing
        const int timeSteps = 16;
        var radius = 3.0f;
        var sphereTimeSteps = new ReadOnlyMemory<V3f>[timeSteps];
        var indices = CreateSphereSimple(segments: 12);

        for (int i = 0; i < timeSteps; i++)
        {
            float t = i / (float)(timeSteps - 1);
            float angle = t * 2.0f * (float)Math.PI; // Full circle

            var center = new V3f(
                radius * (float)Math.Cos(angle),
                0,
                radius * (float)Math.Sin(angle)
            );

            var (vertices, _) = CreateSphere(center, radius: 0.5f, segments: 12);
            sphereTimeSteps[i] = new ReadOnlyMemory<V3f>(vertices);
        }

        Print($"Created circular motion path with {timeSteps} time steps");
        Print($"Orbit radius: {radius:F1} units");
        Print("");

        using var scene = new Scene(Device!, RTCBuildQuality.High, dynamic: false);
        using var orbitingSphere = new MotionBlurGeometry(
            Device!,
            sphereTimeSteps,
            new ReadOnlyMemory<int>(indices),
            RTCBuildQuality.High);

        scene.AttachGeometry(orbitingSphere);
        scene.Commit();

        const int frameCount = 8;
        Print($"Rendering {frameCount} animation frames:");
        Print("");

        for (int frame = 0; frame < frameCount; frame++)
        {
            float time = frame / (float)(frameCount - 1);
            float angle = time * 360f;

            var rayOrigin = new V3f(0, 5, 0);
            var rayDirection = new V3f(0, -1, 0);

            var hit = new RayHit();
            bool didHit = scene.Intersect(rayOrigin, rayDirection, ref hit, 0.0f, float.MaxValue, time);

            var expectedX = radius * (float)Math.Cos(time * 2 * Math.PI);
            var expectedZ = radius * (float)Math.Sin(time * 2 * Math.PI);

            Print($"  Frame {frame + 1}/8 (time={time:F3}, angle={angle:F1}°):");
            if (didHit)
            {
                var hitPos = rayOrigin + rayDirection * hit.T;
                Print($"    Hit at ({hitPos.X:F2}, {hitPos.Y:F2}, {hitPos.Z:F2})");
                Print($"    Expected position: ({expectedX:F2}, 0.00, {expectedZ:F2})");
            }
            else
            {
                Print($"    MISS (sphere not under camera at this time)");
            }
        }

        Print("");
        Print("This simulates motion blur in animation rendering.");
        Print("In production: sample multiple times per frame and average for smooth blur.");
        Print("");
    }

    /// <summary>
    /// Demo 4: Motion Vectors and Speed Analysis
    ///
    /// Demonstrates:
    /// - Calculating motion vectors between time steps
    /// - Computing velocity and speed
    /// - Understanding temporal derivatives
    /// - MotionBlurInterpolation helper utilities
    /// </summary>
    private void Demo4_MotionAnalysis()
    {
        PrintSection("Demo 4: Motion Analysis (Vectors and Speed)");
        Print("Analyzing motion characteristics of a bouncing triangle...");
        Print("");

        var triangleT0 = new V3f[]
        {
            new V3f(0, 0, 0),
            new V3f(1, 0, 0),
            new V3f(0.5f, 1, 0)
        };

        var triangleT1 = new V3f[]
        {
            new V3f(2, 1, 0),
            new V3f(3, 1, 0),
            new V3f(2.5f, 2, 0)
        };

        Print("Triangle positions:");
        Print("  Vertex 0: (0.0, 0.0, 0.0) -> (2.0, 1.0, 0.0)");
        Print("  Vertex 1: (1.0, 0.0, 0.0) -> (3.0, 1.0, 0.0)");
        Print("  Vertex 2: (0.5, 1.0, 0.0) -> (2.5, 2.0, 0.0)");
        Print("");

        var motionVectors = MotionBlurInterpolation.CalculateMotionVectors(
            triangleT0,
            triangleT1
        );

        Print("Motion vectors (displacement from t=0 to t=1):");
        for (int i = 0; i < motionVectors.Length; i++)
        {
            var v = motionVectors[i];
            Print($"  Vertex {i}: ({v.X:F2}, {v.Y:F2}, {v.Z:F2})");
        }
        Print("");

        var speeds = MotionBlurInterpolation.CalculateMotionSpeed(
            triangleT0,
            triangleT1,
            deltaTime: 1.0f
        );

        Print("Motion speed (units per second):");
        for (int i = 0; i < speeds.Length; i++)
        {
            Print($"  Vertex {i}: {speeds[i]:F3} units/sec");
        }
        Print("");

        Print("Interpolated positions at t=0.5:");
        var interpolated = MotionBlurInterpolation.InterpolateVertices(
            triangleT0,
            triangleT1,
            t: 0.5f
        );

        for (int i = 0; i < interpolated.Length; i++)
        {
            var v = interpolated[i];
            Print($"  Vertex {i}: ({v.X:F2}, {v.Y:F2}, {v.Z:F2})");
        }
        Print("");

        Print("Motion vectors are crucial for:");
        Print("  * Temporal reprojection in rendering");
        Print("  * Motion-compensated temporal antialiasing (TAA)");
        Print("  * Optical flow and tracking");
        Print("  * Post-process motion blur effects");
        Print("");
    }

    private (V3f[] vertices, int[] indices) CreateSphere(V3f center, float radius, int segments)
    {
        var vertices = new System.Collections.Generic.List<V3f>();
        var indices = new System.Collections.Generic.List<int>();

        for (int lat = 0; lat <= segments; lat++)
        {
            float theta = lat * (float)Math.PI / segments;
            float sinTheta = (float)Math.Sin(theta);
            float cosTheta = (float)Math.Cos(theta);

            for (int lon = 0; lon <= segments; lon++)
            {
                float phi = lon * 2 * (float)Math.PI / segments;
                float sinPhi = (float)Math.Sin(phi);
                float cosPhi = (float)Math.Cos(phi);

                var x = cosPhi * sinTheta;
                var y = cosTheta;
                var z = sinPhi * sinTheta;

                vertices.Add(center + new V3f(x, y, z) * radius);
            }
        }

        for (int lat = 0; lat < segments; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int first = lat * (segments + 1) + lon;
                int second = first + segments + 1;

                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);

                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }

        return (vertices.ToArray(), indices.ToArray());
    }

    private int[] CreateSphereSimple(int segments)
    {
        var indices = new System.Collections.Generic.List<int>();

        for (int lat = 0; lat < segments; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int first = lat * (segments + 1) + lon;
                int second = first + segments + 1;

                indices.Add(first);
                indices.Add(second);
                indices.Add(first + 1);

                indices.Add(second);
                indices.Add(second + 1);
                indices.Add(first + 1);
            }
        }

        return indices.ToArray();
    }

    private V3f[] CreateCubeVertices(float size)
    {
        float h = size / 2;
        return new V3f[]
        {
            new V3f(-h, -h,  h), new V3f( h, -h,  h), new V3f( h,  h,  h), new V3f(-h,  h,  h),
            new V3f(-h, -h, -h), new V3f(-h,  h, -h), new V3f( h,  h, -h), new V3f( h, -h, -h),
            new V3f(-h,  h, -h), new V3f(-h,  h,  h), new V3f( h,  h,  h), new V3f( h,  h, -h),
            new V3f(-h, -h, -h), new V3f( h, -h, -h), new V3f( h, -h,  h), new V3f(-h, -h,  h),
            new V3f( h, -h, -h), new V3f( h,  h, -h), new V3f( h,  h,  h), new V3f( h, -h,  h),
            new V3f(-h, -h, -h), new V3f(-h, -h,  h), new V3f(-h,  h,  h), new V3f(-h,  h, -h)
        };
    }

    private int[] CreateCubeIndices()
    {
        return new int[]
        {
            0, 1, 2,  0, 2, 3,
            4, 5, 6,  4, 6, 7,
            8, 9, 10,  8, 10, 11,
            12, 13, 14,  12, 14, 15,
            16, 17, 18,  16, 18, 19,
            20, 21, 22,  20, 22, 23
        };
    }

    private float InterpolateFloat(float a, float b, float t)
    {
        return a * (1.0f - t) + b * t;
    }
}
