using System;
using Aardvark.Base;
using Xunit;

namespace Aardvark.Embree.Tests;

public class MatrixFormatTest
{
    [Fact]
    public void PrintM34fLayout()
    {
        Console.WriteLine("=== M34f from Affine3f.Identity ===");

        var affine = Affine3f.Identity;
        var m34 = (M34f)affine;

        unsafe
        {
            float* ptr = (float*)&m34;
            Console.WriteLine("M34f memory layout (12 floats):");
            for (int i = 0; i < 12; i++)
            {
                Console.WriteLine($"  [{i,2}] = {ptr[i]}");
            }

            Console.WriteLine("\nAs 3x4 matrix:");
            Console.WriteLine($"  Row 0: {ptr[0]}, {ptr[1]}, {ptr[2]}, {ptr[3]}");
            Console.WriteLine($"  Row 1: {ptr[4]}, {ptr[5]}, {ptr[6]}, {ptr[7]}");
            Console.WriteLine($"  Row 2: {ptr[8]}, {ptr[9]}, {ptr[10]}, {ptr[11]}");
        }

        Console.WriteLine("\n=== Expected identity (row-major 3x4) ===");
        Console.WriteLine("  Row 0: 1, 0, 0, 0");
        Console.WriteLine("  Row 1: 0, 1, 0, 0");
        Console.WriteLine("  Row 2: 0, 0, 1, 0");

        Console.WriteLine("\n=== M34f structure fields ===");
        Console.WriteLine($"  M00={m34.M00}, M01={m34.M01}, M02={m34.M02}, M03={m34.M03}");
        Console.WriteLine($"  M10={m34.M10}, M11={m34.M11}, M12={m34.M12}, M13={m34.M13}");
        Console.WriteLine($"  M20={m34.M20}, M21={m34.M21}, M22={m34.M22}, M23={m34.M23}");
    }
}
