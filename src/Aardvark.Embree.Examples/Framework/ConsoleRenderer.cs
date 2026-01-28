using System;
using System.Collections.Generic;
using System.Text;
using Aardvark.Base;
using Aardvark.Embree;

namespace Aardvark.Embree.Examples.Framework;

/// <summary>
/// Utilities for rendering visual output in the console.
/// </summary>
public static class ConsoleRenderer
{
    /// <summary>Render ASCII art from a 2D array of ray hits.</summary>
    public static void RenderASCII(RayHit?[,]? hits, int width, int height)
    {
        if (hits == null)
            return;

        Console.WriteLine();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var hit = hits[x, y];
                char c = hit.HasValue ? '#' : ' ';
                Console.Write(c);
            }
            Console.WriteLine();
        }
    }

    /// <summary>Format a V3f vector nicely.</summary>
    public static string Format(V3f v) => $"({v.X:F3}, {v.Y:F3}, {v.Z:F3})";

    /// <summary>Format a M34f matrix nicely.</summary>
    public static string Format(M34f m)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[");
        for (int i = 0; i < 3; i++)
        {
            sb.Append("  ");
            for (int j = 0; j < 4; j++)
            {
                sb.Append($"{m[i, j]:F3:+#.###;-#.###} ");
            }
            sb.AppendLine();
        }
        sb.Append("]");
        return sb.ToString();
    }

    /// <summary>Draw a progress bar for long operations.</summary>
    public static void DrawProgressBar(double percent, int width = 40)
    {
        var filled = (int)(width * percent);
        var bar = new string('█', filled) + new string('░', width - filled);
        Console.Write($"[{bar}] {percent * 100:F1}%\r");
    }

    /// <summary>Draw a formatted table.</summary>
    public static void DrawTable(string[] headers, string[][] rows)
    {
        if (headers.Length == 0 || rows.Length == 0)
            return;

        // Calculate column widths
        var widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
            widths[i] = headers[i].Length;

        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length && i < headers.Length; i++)
                widths[i] = Math.Max(widths[i], row[i].Length);
        }

        // Print header
        for (int i = 0; i < headers.Length; i++)
        {
            Console.Write(headers[i].PadRight(widths[i] + 2));
        }
        Console.WriteLine();

        // Print separator
        for (int i = 0; i < headers.Length; i++)
        {
            Console.Write(new string('-', widths[i] + 2));
        }
        Console.WriteLine();

        // Print rows
        foreach (var row in rows)
        {
            for (int i = 0; i < row.Length && i < headers.Length; i++)
            {
                Console.Write(row[i].PadRight(widths[i] + 2));
            }
            Console.WriteLine();
        }
    }

    /// <summary>Print colored text if console supports it.</summary>
    public static void PrintColored(string text, ConsoleColor color)
    {
        try
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = oldColor;
        }
        catch
        {
            // Fall back to plain text if colors not supported
            Console.Write(text);
        }
    }
}
