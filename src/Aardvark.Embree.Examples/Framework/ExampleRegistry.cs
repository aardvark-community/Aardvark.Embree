using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aardvark.Embree.Examples.Examples;

namespace Aardvark.Embree.Examples.Framework;

/// <summary>
/// Discovery and management of example implementations.
/// </summary>
public static class ExampleRegistry
{
    private static List<ExampleBase>? _cachedExamples;

    /// <summary>Discover all example classes in the assembly.</summary>
    public static List<ExampleBase> DiscoverExamples()
    {
        if (_cachedExamples != null)
            return _cachedExamples;

        var exampleType = typeof(ExampleBase);
        var assembly = Assembly.GetExecutingAssembly();

        var examples = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(exampleType))
            .Select(t => (ExampleBase?)Activator.CreateInstance(t))
            .Where(e => e != null)
            .Cast<ExampleBase>()
            .OrderBy(e => e.OrderIndex)
            .ToList();

        _cachedExamples = examples;
        return examples;
    }

    /// <summary>Get a specific example by its 1-based number.</summary>
    public static ExampleBase? GetByNumber(int number)
    {
        var examples = DiscoverExamples();
        if (number >= 1 && number <= examples.Count)
            return examples[number - 1];
        return null;
    }

    /// <summary>Get the next example after the given one.</summary>
    public static ExampleBase? GetNext(ExampleBase current)
    {
        var examples = DiscoverExamples();
        var index = examples.IndexOf(current);
        if (index >= 0 && index < examples.Count - 1)
            return examples[index + 1];
        return null;
    }

    /// <summary>Get the previous example before the given one.</summary>
    public static ExampleBase? GetPrevious(ExampleBase current)
    {
        var examples = DiscoverExamples();
        var index = examples.IndexOf(current);
        if (index > 0)
            return examples[index - 1];
        return null;
    }

    /// <summary>Print the interactive menu.</summary>
    public static void PrintMenu()
    {
        Console.WriteLine(new string('=', 80));
        Console.WriteLine("                    Aardvark.Embree Example Gallery");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();
        Console.WriteLine("Select an example to run (1-8, or 'q' to quit):");
        Console.WriteLine();

        var examples = DiscoverExamples();
        for (int i = 0; i < examples.Count; i++)
        {
            var ex = examples[i];
            Console.WriteLine($"{i + 1}. {ex.Title}");
            Console.WriteLine($"   {ex.Description}");
            Console.WriteLine($"   Category: {ex.Category} | Lines: ~{ex.ApproximateLineCount}");
            Console.WriteLine();
        }

        Console.WriteLine("Commands:");
        Console.WriteLine("  1-8     Run example");
        Console.WriteLine("  a       Run all examples sequentially");
        Console.WriteLine("  q       Quit");
        Console.WriteLine();
        Console.Write("Your choice: ");
    }

    /// <summary>Print a summary of all examples.</summary>
    public static void PrintSummary()
    {
        var examples = DiscoverExamples();
        Console.WriteLine("\nAvailable Examples:");
        foreach (var ex in examples)
        {
            Console.WriteLine($"{ex.OrderIndex}. {ex.Title}");
        }
    }
}
