using Aardvark.Base;
using System;

namespace Aardvark.Embree;

/// <summary>
/// Helper class for interpolating motion blur data.
/// </summary>
public static class MotionBlurInterpolation
{
    /// <summary>
    /// Interpolates vertex positions linearly between two time steps.
    /// </summary>
    /// <param name="verticesT0">Vertices at time 0</param>
    /// <param name="verticesT1">Vertices at time 1</param>
    /// <param name="t">Interpolation parameter in range [0, 1]</param>
    /// <returns>Interpolated vertices</returns>
    public static V3f[] InterpolateVertices(ReadOnlySpan<V3f> verticesT0, ReadOnlySpan<V3f> verticesT1, float t)
    {
        if (verticesT0.Length != verticesT1.Length)
            throw new ArgumentException("Vertex counts must match");

        if (t < 0.0f || t > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(t), "Interpolation parameter must be in range [0, 1]");

        var result = new V3f[verticesT0.Length];
        var oneMinusT = 1.0f - t;

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = verticesT0[i] * oneMinusT + verticesT1[i] * t;
        }

        return result;
    }

    /// <summary>
    /// Interpolates a transform linearly between two time steps.
    /// </summary>
    /// <param name="transformT0">Transform at time 0</param>
    /// <param name="transformT1">Transform at time 1</param>
    /// <param name="t">Interpolation parameter in range [0, 1]</param>
    /// <returns>Interpolated transform</returns>
    public static Affine3f InterpolateTransform(Affine3f transformT0, Affine3f transformT1, float t)
    {
        if (t < 0.0f || t > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(t), "Interpolation parameter must be in range [0, 1]");

        // Decompose transforms into rotation and translation
        var rot0 = (M33f)transformT0;
        var rot1 = (M33f)transformT1;
        var trans0 = transformT0.Trans;
        var trans1 = transformT1.Trans;

        // Linearly interpolate translation
        var trans = trans0 * (1.0f - t) + trans1 * t;

        // Spherical linear interpolation (slerp) for rotation
        var rot = SlerpRotation(rot0, rot1, t);

        return new Affine3f(rot, trans);
    }

    /// <summary>
    /// Generates intermediate vertex positions for multi-segment motion blur.
    /// </summary>
    /// <param name="verticesStart">Vertices at start time</param>
    /// <param name="verticesEnd">Vertices at end time</param>
    /// <param name="timeSteps">Number of time steps to generate</param>
    /// <returns>Array of vertex positions for each time step</returns>
    public static ReadOnlyMemory<V3f>[] GenerateVertexTimeSteps(ReadOnlyMemory<V3f> verticesStart, ReadOnlyMemory<V3f> verticesEnd, int timeSteps)
    {
        if (timeSteps < 2)
            throw new ArgumentException("At least 2 time steps required", nameof(timeSteps));

        var result = new ReadOnlyMemory<V3f>[timeSteps];
        var startSpan = verticesStart.Span;
        var endSpan = verticesEnd.Span;

        for (int i = 0; i < timeSteps; i++)
        {
            float t = i / (float)(timeSteps - 1);
            result[i] = new ReadOnlyMemory<V3f>(InterpolateVertices(startSpan, endSpan, t));
        }

        return result;
    }

    /// <summary>
    /// Generates intermediate transforms for multi-segment motion blur.
    /// </summary>
    /// <param name="transformStart">Transform at start time</param>
    /// <param name="transformEnd">Transform at end time</param>
    /// <param name="timeSteps">Number of time steps to generate</param>
    /// <returns>Array of transforms for each time step</returns>
    public static Affine3f[] GenerateTransformTimeSteps(Affine3f transformStart, Affine3f transformEnd, int timeSteps)
    {
        if (timeSteps < 2)
            throw new ArgumentException("At least 2 time steps required", nameof(timeSteps));

        var result = new Affine3f[timeSteps];

        for (int i = 0; i < timeSteps; i++)
        {
            float t = i / (float)(timeSteps - 1);
            result[i] = InterpolateTransform(transformStart, transformEnd, t);
        }

        return result;
    }

    /// <summary>
    /// Calculates motion vectors for vertices between two time steps.
    /// </summary>
    /// <param name="verticesT0">Vertices at time 0</param>
    /// <param name="verticesT1">Vertices at time 1</param>
    /// <returns>Motion vectors (velocity) for each vertex</returns>
    public static V3f[] CalculateMotionVectors(ReadOnlySpan<V3f> verticesT0, ReadOnlySpan<V3f> verticesT1)
    {
        if (verticesT0.Length != verticesT1.Length)
            throw new ArgumentException("Vertex counts must match");

        var result = new V3f[verticesT0.Length];

        for (int i = 0; i < result.Length; i++)
        {
            result[i] = verticesT1[i] - verticesT0[i];
        }

        return result;
    }

    /// <summary>
    /// Calculates the speed of motion for each vertex.
    /// </summary>
    /// <param name="verticesT0">Vertices at time 0</param>
    /// <param name="verticesT1">Vertices at time 1</param>
    /// <param name="deltaTime">Time difference between the two states</param>
    /// <returns>Speed (magnitude of velocity) for each vertex</returns>
    public static float[] CalculateMotionSpeed(ReadOnlySpan<V3f> verticesT0, ReadOnlySpan<V3f> verticesT1, float deltaTime = 1.0f)
    {
        if (verticesT0.Length != verticesT1.Length)
            throw new ArgumentException("Vertex counts must match");

        if (deltaTime <= 0)
            throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time must be positive");

        var result = new float[verticesT0.Length];

        for (int i = 0; i < result.Length; i++)
        {
            var motion = verticesT1[i] - verticesT0[i];
            result[i] = motion.Length / deltaTime;
        }

        return result;
    }

    /// <summary>
    /// Performs spherical linear interpolation between two rotation matrices.
    /// </summary>
    private static M33f SlerpRotation(M33f rot0, M33f rot1, float t)
    {
        // Convert rotation matrices to quaternions for slerp
        var q0 = QuaternionFromMatrix(rot0);
        var q1 = QuaternionFromMatrix(rot1);

        // Perform quaternion slerp
        var qResult = SlerpQuaternion(q0, q1, t);

        // Convert back to rotation matrix
        return MatrixFromQuaternion(qResult);
    }

    private static V4f QuaternionFromMatrix(M33f m)
    {
        // Simple conversion from rotation matrix to quaternion
        float trace = m.M00 + m.M11 + m.M22;
        float w, x, y, z;

        if (trace > 0)
        {
            float s = 0.5f / (float)Math.Sqrt(trace + 1.0f);
            w = 0.25f / s;
            x = (m.M21 - m.M12) * s;
            y = (m.M02 - m.M20) * s;
            z = (m.M10 - m.M01) * s;
        }
        else if (m.M00 > m.M11 && m.M00 > m.M22)
        {
            float s = 2.0f * (float)Math.Sqrt(1.0f + m.M00 - m.M11 - m.M22);
            w = (m.M21 - m.M12) / s;
            x = 0.25f * s;
            y = (m.M01 + m.M10) / s;
            z = (m.M02 + m.M20) / s;
        }
        else if (m.M11 > m.M22)
        {
            float s = 2.0f * (float)Math.Sqrt(1.0f + m.M11 - m.M00 - m.M22);
            w = (m.M02 - m.M20) / s;
            x = (m.M01 + m.M10) / s;
            y = 0.25f * s;
            z = (m.M12 + m.M21) / s;
        }
        else
        {
            float s = 2.0f * (float)Math.Sqrt(1.0f + m.M22 - m.M00 - m.M11);
            w = (m.M10 - m.M01) / s;
            x = (m.M02 + m.M20) / s;
            y = (m.M12 + m.M21) / s;
            z = 0.25f * s;
        }

        return new V4f(x, y, z, w);
    }

    private static M33f MatrixFromQuaternion(V4f q)
    {
        float x = q.X, y = q.Y, z = q.Z, w = q.W;
        float xx = x * x, yy = y * y, zz = z * z;
        float xy = x * y, xz = x * z, yz = y * z;
        float wx = w * x, wy = w * y, wz = w * z;

        return new M33f(
            1.0f - 2.0f * (yy + zz), 2.0f * (xy - wz), 2.0f * (xz + wy),
            2.0f * (xy + wz), 1.0f - 2.0f * (xx + zz), 2.0f * (yz - wx),
            2.0f * (xz - wy), 2.0f * (yz + wx), 1.0f - 2.0f * (xx + yy)
        );
    }

    private static V4f SlerpQuaternion(V4f q0, V4f q1, float t)
    {
        // Ensure shortest path
        float dot = q0.X * q1.X + q0.Y * q1.Y + q0.Z * q1.Z + q0.W * q1.W;
        if (dot < 0)
        {
            q1 = -q1;
            dot = -dot;
        }

        // Use linear interpolation for very similar quaternions
        if (dot > 0.9995f)
        {
            var result = q0 * (1.0f - t) + q1 * t;
            float len = (float)Math.Sqrt(result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W);
            return result / len;
        }

        // Slerp
        float theta = (float)Math.Acos(dot);
        float sinTheta = (float)Math.Sin(theta);
        float w0 = (float)Math.Sin((1.0f - t) * theta) / sinTheta;
        float w1 = (float)Math.Sin(t * theta) / sinTheta;

        return q0 * w0 + q1 * w1;
    }
}