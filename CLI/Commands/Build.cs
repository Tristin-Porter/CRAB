using System.IO;
using System.Diagnostics;

namespace CRAB;

/// <summary>
/// Build command - builds a CRAB project
/// </summary>
class Build : Command
{
    public Build()
    {
        Name = "build";
        Description = "Build a CRAB project.";
        
        SupportedFlags["project"] = "Path to project directory (default: current directory).";
        SupportedFlags["output"] = "Output directory (default: ./bin).";
        SupportedFlags["config"] = "Build configuration: debug or release (default: debug).";
        SupportedFlags["verbose"] = "Enable verbose build output.";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        // Parse project path
        string projectPath = ".";
        if (flags.TryGetValue("project", out var flagProject) && !string.IsNullOrWhiteSpace(flagProject))
            projectPath = flagProject;
        else if (args.Length > 0)
            projectPath = args[0];

        // Parse output path
        string outputPath = Path.Combine(projectPath, "bin");
        if (flags.TryGetValue("output", out var flagOutput) && !string.IsNullOrWhiteSpace(flagOutput))
            outputPath = flagOutput;

        // Parse configuration
        string config = "debug";
        if (flags.TryGetValue("config", out var flagConfig) && !string.IsNullOrWhiteSpace(flagConfig))
            config = flagConfig.ToLower();

        bool verbose = flags.ContainsKey("verbose");

        // Validate project path
        if (!Directory.Exists(projectPath))
        {
            System.Console.WriteLine($"Error: Project directory '{projectPath}' does not exist.");
            return;
        }

        if (verbose)
        {
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine("CRAB Build System");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine($"Project:    {Path.GetFullPath(projectPath)}");
            System.Console.WriteLine($"Output:     {Path.GetFullPath(outputPath)}");
            System.Console.WriteLine($"Config:     {config}");
            System.Console.WriteLine("=".PadRight(60, '='));
        }

        try
        {
            // Find all .cs files
            if (verbose) System.Console.WriteLine("\n[1/4] Discovering source files...");
            var csFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("/obj/") && !f.Contains("/bin/"))
                .ToArray();

            if (csFiles.Length == 0)
            {
                System.Console.WriteLine($"Error: No .cs files found in project directory: {projectPath}");
                return;
            }

            if (verbose)
            {
                System.Console.WriteLine($"      Found {csFiles.Length} source files:");
                foreach (var file in csFiles.Take(10))
                {
                    System.Console.WriteLine($"        {Path.GetFileName(file)}");
                }
                if (csFiles.Length > 10)
                {
                    System.Console.WriteLine($"        ... and {csFiles.Length - 10} more");
                }
            }

            // Create output directory
            if (verbose) System.Console.WriteLine("\n[2/4] Preparing output directory...");
            Directory.CreateDirectory(outputPath);
            string outputFile = Path.Combine(outputPath, "output.wasm");

            // Compile each file or concatenate them
            if (verbose) System.Console.WriteLine("\n[3/4] Compiling project...");
            
            var compileCommand = new Compile();
            var compileFlags = new Dictionary<string, string?>
            {
                ["input"] = projectPath,
                ["output"] = outputFile
            };
            
            if (verbose)
                compileFlags["verbose"] = null;

            compileCommand.Execute(Array.Empty<string>(), compileFlags);

            if (!File.Exists(outputFile))
            {
                System.Console.WriteLine("Error: Build failed - output file was not created.");
                return;
            }

            if (verbose)
            {
                System.Console.WriteLine("\n[4/4] Build complete.");
                System.Console.WriteLine("=".PadRight(60, '='));
            }

            System.Console.WriteLine($"✓ Build successful: {outputFile}");
            System.Console.WriteLine($"  Output size: {new FileInfo(outputFile).Length} bytes");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: Build failed - {ex.Message}");
            if (verbose)
            {
                System.Console.WriteLine("\nStack trace:");
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
