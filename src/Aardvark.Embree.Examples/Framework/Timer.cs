using System.Diagnostics;

namespace Aardvark.Embree.Examples.Framework;

/// <summary>
/// High-resolution performance timer for measuring example execution times.
/// </summary>
public class Timer
{
    private readonly Stopwatch _stopwatch = new();
    private string? _currentLabel;

    /// <summary>Start a named timer.</summary>
    public void Start(string label)
    {
        _currentLabel = label;
        _stopwatch.Restart();
    }

    /// <summary>Stop the timer and return elapsed time in milliseconds.</summary>
    public double Stop()
    {
        _stopwatch.Stop();
        return _stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>Get elapsed time without stopping.</summary>
    public double Elapsed => _stopwatch.Elapsed.TotalMilliseconds;

    /// <summary>Format throughput for human-readable display.</summary>
    public static string FormatThroughput(int count, double timeMs)
    {
        if (timeMs <= 0)
            return "∞";

        var raysPerMs = count / timeMs;
        var raysPerSec = raysPerMs * 1000;

        if (raysPerSec < 1_000)
            return $"{raysPerSec:F1} rays/sec";
        if (raysPerSec < 1_000_000)
            return $"{raysPerSec / 1000:F2}K rays/sec";
        if (raysPerSec < 1_000_000_000)
            return $"{raysPerSec / 1_000_000:F2}M rays/sec";

        return $"{raysPerSec / 1_000_000_000:F2}B rays/sec";
    }
}
