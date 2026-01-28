using Aardvark.Embree.Examples.Framework;

namespace Aardvark.Embree.Examples;

class Program
{
    static void Main(string[] args)
    {
        // Extracts embedded native DLLs (embree4.dll, tbb12.dll) to disk and registers the loader
        Aardvark.Base.Aardvark.Init();

        var examples = ExampleRegistry.DiscoverExamples();

        while (true)
        {
            Console.Clear();
            ExampleRegistry.PrintMenu();

            var input = Console.ReadLine()?.Trim().ToLower();

            if (input == "q")
                break;

            if (input == "a")
            {
                RunAllExamples(examples);
                continue;
            }

            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= examples.Count)
            {
                RunExample(examples[choice - 1]);
            }
            else
            {
                Console.WriteLine($"Invalid choice: {input}");
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }

        Console.WriteLine("\nThank you for exploring Aardvark.Embree!");
    }

    static void RunExample(ExampleBase example)
    {
        Console.Clear();
        try
        {
            example.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] Error running example: {ex.Message}");
            Console.WriteLine($"{ex.GetType().Name}: {ex}");
        }

        ShowPostExampleMenu(example);
    }

    static void RunAllExamples(List<ExampleBase> examples)
    {
        Console.Clear();
        Console.WriteLine("Running all examples sequentially...\n");

        foreach (var example in examples)
        {
            try
            {
                example.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[ERROR] Error running {example.Title}: {ex.Message}");
            }

            Console.WriteLine("\nPress Enter to continue to next example...");
            Console.ReadLine();
            Console.Clear();
        }

        Console.WriteLine("\nAll examples completed!");
        Console.WriteLine("Press Enter to return to main menu...");
        Console.ReadLine();
    }

    static void ShowPostExampleMenu(ExampleBase example)
    {
        var examples = ExampleRegistry.DiscoverExamples();
        var currentIndex = examples.IndexOf(example);

        while (true)
        {
            Console.WriteLine("\n" + new string('=', 80));
            Console.WriteLine("Example completed!");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("\nOptions:");
            Console.WriteLine("  [Enter]  Return to main menu");
            Console.WriteLine("  r        Run this example again");

            if (currentIndex > 0)
                Console.WriteLine($"  p        Run previous example ({examples[currentIndex - 1].OrderIndex}. {examples[currentIndex - 1].Title})");

            if (currentIndex < examples.Count - 1)
                Console.WriteLine($"  n        Run next example ({examples[currentIndex + 1].OrderIndex}. {examples[currentIndex + 1].Title})");

            Console.WriteLine("  q        Quit");
            Console.WriteLine();
            Console.Write("Your choice: ");

            var input = Console.ReadLine()?.Trim().ToLower();

            switch (input)
            {
                case "":
                    return;

                case "r":
                    RunExample(example);
                    return;

                case "n" when currentIndex < examples.Count - 1:
                    RunExample(examples[currentIndex + 1]);
                    return;

                case "p" when currentIndex > 0:
                    RunExample(examples[currentIndex - 1]);
                    return;

                case "q":
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine($"Invalid choice: {input}");
                    break;
            }
        }
    }
}
