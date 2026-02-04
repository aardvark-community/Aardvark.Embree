namespace Aardvark.Embree.Examples.Framework;

/// <summary>
/// Base class for all example demonstrations.
/// Provides common lifecycle management, device creation, and output utilities.
/// </summary>
public abstract class ExampleBase
{
    /// <summary>Gets the example title displayed in the menu.</summary>
    public abstract string Title { get; }

    /// <summary>Gets the description of what this example demonstrates.</summary>
    public abstract string Description { get; }

    /// <summary>Gets the category (e.g., "Ray Tracing Queries", "Motion Blur").</summary>
    public abstract string Category { get; }

    /// <summary>Gets the order index (1-8) for sorting in the menu.</summary>
    public abstract int OrderIndex { get; }

    /// <summary>Gets the approximate number of lines of code in this example.</summary>
    public virtual int ApproximateLineCount => 200;

    /// <summary>Shared device instance for this example run.</summary>
    protected Device? Device { get; private set; }

    /// <summary>Performance timer utility.</summary>
    protected Timer Timer { get; }

    protected ExampleBase()
    {
        Timer = new Timer();
    }

    /// <summary>Main entry point - orchestrates setup, execution, and cleanup.</summary>
    public void Run()
    {
        PrintHeader();
        try
        {
            Setup();
            Execute();
        }
        finally
        {
            Cleanup();
        }
        PrintFooter();
    }

    /// <summary>Create the device and initialize resources. Called before Execute().</summary>
    protected virtual void Setup()
    {
        Device = new Device();
    }

    /// <summary>Run the example logic. Must be implemented by derived classes.</summary>
    protected abstract void Execute();

    /// <summary>Clean up resources. Called after Execute() regardless of success/failure.</summary>
    protected virtual void Cleanup()
    {
        Device?.Dispose();
        Device = null;
    }

    /// <summary>Print the example header with title and separator.</summary>
    protected void PrintHeader()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine($"  {Title}");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();
    }

    /// <summary>Print the example footer.</summary>
    protected static void PrintFooter()
    {
        Console.WriteLine();
    }

    /// <summary>Print a section heading with underline.</summary>
    protected static void PrintSection(string title)
    {
        Console.WriteLine($"\n{title}");
        Console.WriteLine(new string('-', title.Length));
    }

    /// <summary>Print a message to console.</summary>
    protected static void Print(string message) => Console.WriteLine(message);

    /// <summary>Print a ray hit result with formatted information.</summary>
    protected static void PrintHit(RayHit hit)
    {
        Console.WriteLine($"  [OK] HIT at distance {hit.T:F2}");
        Console.WriteLine($"    Normal: ({hit.Normal.X:F3}, {hit.Normal.Y:F3}, {hit.Normal.Z:F3})");
        Console.WriteLine($"    Coords: u={hit.Coord.X:F2}, v={hit.Coord.Y:F2}");
    }

    /// <summary>Print performance metrics (ray throughput).</summary>
    protected static void PrintPerformance(string label, int count, double timeMs)
    {
        var throughput = Timer.FormatThroughput(count, timeMs);
        Console.WriteLine($"{label}: {count} in {timeMs:F2}ms ({throughput})");
    }
}
