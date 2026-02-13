using System.IO;

namespace CRAB;

/// <summary>
/// Test command - automatically generates, compiles, and runs a test project
/// </summary>
class Test : Command
{
    public Test()
    {
        Name = "test";
        Description = "Generate a test project, compile it, and run it.";
        
        SupportedFlags["name"] = "Name of the test project (default: TestProject).";
        SupportedFlags["keep"] = "Keep the generated test project after execution.";
        SupportedFlags["verbose"] = "Enable verbose output.";
        SupportedFlags["arch"] = "Target architecture (x86_64, x86_32, x86_16, arm64, arm32, default: x86_64).";
        SupportedFlags["format"] = "Output format (native, pe, default: native).";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        // Parse flags
        string projectName = "TestProject";
        if (flags.TryGetValue("name", out var flagName) && !string.IsNullOrWhiteSpace(flagName))
            projectName = flagName;
        else if (args.Length > 0)
            projectName = args[0];

        bool keepProject = flags.ContainsKey("keep");
        bool verbose = flags.ContainsKey("verbose");

        string currentDir = Directory.GetCurrentDirectory();
        string projectPath = Path.Combine(currentDir, projectName);

        if (verbose)
        {
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine("CRAB Test Command");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine($"Project:    {projectName}");
            System.Console.WriteLine($"Keep:       {keepProject}");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine();
        }

        try
        {
            // Step 1: Generate test project
            if (verbose) System.Console.WriteLine("[1/3] Generating test project...");
            else System.Console.WriteLine($"Generating test project '{projectName}'...");

            var consoleCommand = new Console();
            var newFlags = new Dictionary<string, string?>
            {
                ["name"] = projectName
            };
            
            consoleCommand.Execute(Array.Empty<string>(), newFlags);

            if (!Directory.Exists(projectPath))
            {
                System.Console.WriteLine($"Error: Failed to create test project '{projectName}'.");
                return;
            }

            if (verbose) System.Console.WriteLine();

            // Step 2: Build the test project
            if (verbose) System.Console.WriteLine("[2/3] Building test project...");
            else System.Console.WriteLine($"Building test project...");

            var buildCommand = new Build();
            var buildFlags = new Dictionary<string, string?>
            {
                ["project"] = projectPath
            };
            
            if (verbose)
                buildFlags["verbose"] = null;

            buildCommand.Execute(Array.Empty<string>(), buildFlags);

            // Check for compiled output (wasm or wat)
            string outputFile = Path.Combine(projectPath, "bin", "output.wasm");
            if (!File.Exists(outputFile))
            {
                outputFile = Path.Combine(projectPath, "bin", "output.wat");
                if (!File.Exists(outputFile))
                {
                    System.Console.WriteLine($"Error: Build failed - output file not found.");
                    CleanupProject(projectPath, keepProject, verbose);
                    return;
                }
            }

            if (verbose) System.Console.WriteLine();

            // Step 3: Run the compiled output
            if (verbose) System.Console.WriteLine("[3/3] Running test project...");
            else System.Console.WriteLine($"Running test project...");

            var runCommand = new Run();
            var runFlags = new Dictionary<string, string?>
            {
                ["input"] = outputFile
            };

            if (verbose)
                runFlags["verbose"] = null;

            // Pass arch and format flags to Run command for BADGER compilation
            if (flags.TryGetValue("arch", out var arch))
                runFlags["arch"] = arch;

            if (flags.TryGetValue("format", out var format))
                runFlags["format"] = format;

            runCommand.Execute(Array.Empty<string>(), runFlags);

            if (verbose) System.Console.WriteLine();

            // Step 4: Cleanup if requested
            CleanupProject(projectPath, keepProject, verbose);

            System.Console.WriteLine();
            System.Console.WriteLine("✓ Test completed successfully.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: Test failed - {ex.Message}");
            if (verbose)
            {
                System.Console.WriteLine("\nStack trace:");
                System.Console.WriteLine(ex.StackTrace);
            }

            // Attempt cleanup even on error
            CleanupProject(projectPath, keepProject, verbose);
        }
    }

    private void CleanupProject(string projectPath, bool keep, bool verbose)
    {
        if (!keep)
        {
            try
            {
                if (Directory.Exists(projectPath))
                {
                    Directory.Delete(projectPath, recursive: true);
                    if (verbose) System.Console.WriteLine($"Cleaned up test project: {projectPath}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Warning: Failed to cleanup test project - {ex.Message}");
            }
        }
        else if (verbose && Directory.Exists(projectPath))
        {
            System.Console.WriteLine($"Kept test project at: {projectPath}");
        }
    }
}
