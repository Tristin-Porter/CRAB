using System.IO;
using System.Diagnostics;
using CRAB.ProjectSystem;

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
        SupportedFlags["to-asm"] = "Build all the way to native assembly using BADGER (WAT -> ASM).";
        SupportedFlags["arch"] = "Target architecture when using --to-asm (x86_64, x86_32, x86_16, arm64, arm32, default: x86_64).";
        SupportedFlags["format"] = "Output format when using --to-asm (native, pe, default: native).";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        // Parse project path
        string projectPath = ".";
        if (flags.TryGetValue("project", out var flagProject) && !string.IsNullOrWhiteSpace(flagProject))
            projectPath = flagProject;
        else if (args.Length > 0)
            projectPath = args[0];

        // Determine base directory for output
        string baseDir = projectPath;
        if (File.Exists(projectPath))
        {
            // If a file is provided (sln, csproj, etc.), use its directory
            baseDir = Path.GetDirectoryName(projectPath) ?? ".";
        }
        
        // Parse configuration
        string config = "debug";
        if (flags.TryGetValue("config", out var flagConfig) && !string.IsNullOrWhiteSpace(flagConfig))
            config = flagConfig.ToLower();

        // Capitalize first letter for path (Debug/Release)
        string configCapitalized = config.Length > 0 
            ? char.ToUpper(config[0]) + config.Substring(1) 
            : "Debug";

        // Parse output path - default to bin/Debug/crab1.0 or bin/Release/crab1.0
        string outputPath = Path.Combine(baseDir, "bin", configCapitalized, "crab1.0");
        if (flags.TryGetValue("output", out var flagOutput) && !string.IsNullOrWhiteSpace(flagOutput))
            outputPath = flagOutput;

        bool verbose = flags.ContainsKey("verbose");

        // Validate project path exists (can be file or directory)
        if (!Directory.Exists(projectPath) && !File.Exists(projectPath))
        {
            System.Console.WriteLine($"Error: Path '{projectPath}' does not exist.");
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
            // Discover projects and source files using the new project system
            if (verbose) System.Console.WriteLine("\n[1/4] Discovering projects and source files...");
            
            var discovery = ProjectDiscovery.Discover(projectPath);
            
            if (verbose)
            {
                System.Console.WriteLine($"      Project type: {discovery.ProjectType}");
                if (!string.IsNullOrEmpty(discovery.SolutionFile))
                {
                    System.Console.WriteLine($"      Solution: {Path.GetFileName(discovery.SolutionFile)}");
                }
                if (discovery.ProjectFiles.Count > 0)
                {
                    System.Console.WriteLine($"      Projects: {discovery.ProjectFiles.Count}");
                    foreach (var proj in discovery.ProjectFiles.Take(3))
                    {
                        System.Console.WriteLine($"        {Path.GetFileName(proj)}");
                    }
                    if (discovery.ProjectFiles.Count > 3)
                    {
                        System.Console.WriteLine($"        ... and {discovery.ProjectFiles.Count - 3} more");
                    }
                }
            }
            
            var csFiles = discovery.SourceFiles.ToArray();

            if (csFiles.Length == 0)
            {
                System.Console.WriteLine($"Error: No .cs files found in: {projectPath}");
                if (discovery.HasProjects)
                {
                    System.Console.WriteLine("       Check that the project files are valid and contain source files.");
                }
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
            
            // Determine project name for output file
            string projectNameForFiles;
            if (discovery.ProjectFiles.Count > 0)
            {
                // Use project file name
                projectNameForFiles = Path.GetFileNameWithoutExtension(discovery.ProjectFiles[0]);
            }
            else if (!string.IsNullOrEmpty(discovery.SolutionFile))
            {
                // Use solution file name
                projectNameForFiles = Path.GetFileNameWithoutExtension(discovery.SolutionFile);
            }
            else if (Directory.Exists(projectPath))
            {
                // Use directory name
                projectNameForFiles = Path.GetFileName(Path.GetFullPath(projectPath));
            }
            else
            {
                // Fallback to "output"
                projectNameForFiles = "output";
            }
            
            // Output file is ProjectName.wat in the output directory
            string outputFile = Path.Combine(outputPath, $"{projectNameForFiles}.wat");

            // Compile source files
            if (verbose) System.Console.WriteLine("\n[3/4] Compiling project...");
            
            // Create a temporary file with all source code concatenated
            var tempSourceFile = Path.Combine(outputPath, "temp_combined_source.cs");
            var combinedSource = new System.Text.StringBuilder();
            
            foreach (var file in csFiles)
            {
                if (File.Exists(file))
                {
                    combinedSource.AppendLine($"// Source: {Path.GetFileName(file)}");
                    combinedSource.AppendLine(File.ReadAllText(file));
                    combinedSource.AppendLine();
                }
            }
            
            File.WriteAllText(tempSourceFile, combinedSource.ToString());
            
            var compileCommand = new Compile();
            var compileFlags = new Dictionary<string, string?>
            {
                ["input"] = tempSourceFile,
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

            System.Console.WriteLine($"\n✓ Build successful!");
            System.Console.WriteLine($"  Output: {outputFile}");
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
