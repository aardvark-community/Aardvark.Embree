using System;

namespace Aardvark.Embree;

/// <summary>
/// Configuration settings for motion blur rendering.
/// </summary>
public class MotionBlurSettings
{
    /// <summary>
    /// The start time of the camera shutter opening (default: 0.0).
    /// Must be in range [0, 1].
    /// </summary>
    public float ShutterOpenTime { get; set; } = 0.0f;

    /// <summary>
    /// The end time of the camera shutter closing (default: 1.0).
    /// Must be in range [0, 1] and must be greater than ShutterOpenTime.
    /// </summary>
    public float ShutterCloseTime { get; set; } = 1.0f;

    /// <summary>
    /// Number of time samples to use for motion blur (default: 2).
    /// Higher values provide smoother motion blur but require more memory.
    /// </summary>
    public int TimeSteps { get; set; } = 2;

    /// <summary>
    /// Validates the motion blur settings.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when values are outside valid ranges.</exception>
    /// <exception cref="ArgumentException">Thrown when ShutterCloseTime is not greater than ShutterOpenTime.</exception>
    public void Validate()
    {
        if (ShutterOpenTime < 0.0f || ShutterOpenTime > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(ShutterOpenTime), "Shutter open time must be in range [0, 1]");

        if (ShutterCloseTime < 0.0f || ShutterCloseTime > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(ShutterCloseTime), "Shutter close time must be in range [0, 1]");

        if (ShutterCloseTime <= ShutterOpenTime)
            throw new ArgumentException("Shutter close time must be greater than shutter open time");

        if (TimeSteps < 2)
            throw new ArgumentOutOfRangeException(nameof(TimeSteps), "Time steps must be at least 2 for motion blur");
    }

    /// <summary>
    /// Gets the time value for a specific time step index.
    /// </summary>
    /// <param name="stepIndex">The index of the time step (0 to TimeSteps-1)</param>
    /// <returns>The normalized time value in range [ShutterOpenTime, ShutterCloseTime]</returns>
    public float GetTimeForStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= TimeSteps)
            throw new ArgumentOutOfRangeException(nameof(stepIndex));

        if (TimeSteps == 1)
            return ShutterOpenTime;

        float t = stepIndex / (float)(TimeSteps - 1);
        return ShutterOpenTime + t * (ShutterCloseTime - ShutterOpenTime);
    }

    /// <summary>
    /// Creates default motion blur settings with standard camera shutter.
    /// </summary>
    public static MotionBlurSettings Default => new MotionBlurSettings();

    /// <summary>
    /// Creates motion blur settings for fast motion (shorter shutter time).
    /// </summary>
    public static MotionBlurSettings FastMotion => new MotionBlurSettings
    {
        ShutterOpenTime = 0.25f,
        ShutterCloseTime = 0.75f,
        TimeSteps = 2
    };

    /// <summary>
    /// Creates motion blur settings for smooth motion (more time steps).
    /// </summary>
    public static MotionBlurSettings SmoothMotion => new MotionBlurSettings
    {
        ShutterOpenTime = 0.0f,
        ShutterCloseTime = 1.0f,
        TimeSteps = 8
    };
}