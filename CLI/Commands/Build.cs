using System.IO;
using System.Diagnostics;
using CRAB.ProjectSystem;
using CRAB.WebAssembly;
using Badger.Containers;

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
        
        // Parse output path
        string outputPath = Path.Combine(baseDir, "bin");
        if (flags.TryGetValue("output", out var flagOutput) && !string.IsNullOrWhiteSpace(flagOutput))
            outputPath = flagOutput;

        // Parse configuration
        string config = "debug";
        if (flags.TryGetValue("config", out var flagConfig) && !string.IsNullOrWhiteSpace(flagConfig))
            config = flagConfig.ToLower();

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
            
            bool toAsm = flags.ContainsKey("to-asm");
            string outputFile = Path.Combine(outputPath, toAsm ? "output.bin" : "output.wasm");

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
            
            // Pass through BADGER flags if building to assembly
            if (toAsm)
            {
                compileFlags["to-asm"] = null;
                
                if (flags.TryGetValue("arch", out var arch))
                    compileFlags["arch"] = arch;
                    
                if (flags.TryGetValue("format", out var fmt))
                    compileFlags["format"] = fmt;
            }

            compileCommand.Execute(Array.Empty<string>(), compileFlags);

            if (!File.Exists(outputFile))
            {
                System.Console.WriteLine("Error: Build failed - output file was not created.");
                return;
            }

            // For WAT/WASM output, also generate WASM binary, JS wrapper, and HTML files
            if (!toAsm && outputFile.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (verbose) System.Console.WriteLine("\n[4/6] Generating WebAssembly binary and browser files...");
                    
                    // Read the WAT text that was generated
                    string watText = File.ReadAllText(outputFile);
                    if (verbose) System.Console.WriteLine($"      Read {watText.Length} characters of WAT text");
                    
                    // Generate WASM binary and JS wrapper using BADGER
                    if (verbose) System.Console.WriteLine("      Calling BADGER WasmJS.Emit()...");
                    var (wasmBinary, jsWrapper) = WasmJS.Emit(watText);
                    if (verbose) System.Console.WriteLine($"      BADGER generated {wasmBinary.Length} bytes of WASM binary");
                    
                    // Determine project name for file naming
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
                    
                    // Save WASM binary
                    string wasmBinaryPath = Path.Combine(outputPath, $"{projectNameForFiles}.wasm");
                    File.WriteAllBytes(wasmBinaryPath, wasmBinary);
                    if (verbose) System.Console.WriteLine($"      Saved {wasmBinaryPath} ({wasmBinary.Length} bytes)");
                    
                    // Save JS wrapper
                    string jsPath = Path.Combine(outputPath, $"{projectNameForFiles}.js");
                    File.WriteAllText(jsPath, jsWrapper);
                    if (verbose) System.Console.WriteLine($"      Saved {jsPath}");
                    
                    // Generate HTML file with project-specific filenames
                    string projectNameForTitle = projectNameForFiles;
                    string htmlContent = HtmlGenerator.GenerateHtml(
                        projectNameForTitle, 
                        $"{projectNameForFiles}.wasm", 
                        $"{projectNameForFiles}.js");
                    string htmlPath = Path.Combine(outputPath, $"{projectNameForFiles}.html");
                    File.WriteAllText(htmlPath, htmlContent);
                    if (verbose) System.Console.WriteLine($"      Saved {htmlPath}");
                    
                    // Also save the original WAT for debugging
                    string watPath = Path.Combine(outputPath, $"{projectNameForFiles}.wat");
                    File.WriteAllText(watPath, watText);
                    if (verbose) System.Console.WriteLine($"      Saved {watPath} (WebAssembly text format)");
                    
                    if (verbose)
                    {
                        System.Console.WriteLine("\n[5/6] Browser files generated.");
                        System.Console.WriteLine($"      Open {projectNameForFiles}.html in a browser to run the WebAssembly module.");
                    }
                }
                catch (IOException ioEx)
                {
                    System.Console.WriteLine($"Warning: Failed to write browser files to disk - {ioEx.Message}");
                    if (verbose)
                    {
                        System.Console.WriteLine("This may be a file permissions or disk space issue.");
                        System.Console.WriteLine("Stack trace:");
                        System.Console.WriteLine(ioEx.StackTrace);
                    }
                }
                catch (Exception wasmEx)
                {
                    System.Console.WriteLine($"Warning: Failed to generate WASM binary/browser files - {wasmEx.Message}");
                    System.Console.WriteLine("This may be due to invalid WAT format or BADGER compilation error.");
                    if (verbose)
                    {
                        System.Console.WriteLine("Stack trace:");
                        System.Console.WriteLine(wasmEx.StackTrace);
                    }
                }
            }

            if (verbose)
            {
                System.Console.WriteLine($"\n[{(toAsm ? "4/4" : "6/6")}] Build complete.");
                System.Console.WriteLine("=".PadRight(60, '='));
            }

            System.Console.WriteLine($"✓ Build successful: {outputFile}");
            System.Console.WriteLine($"  Output size: {new FileInfo(outputFile).Length} bytes");
            
            // Show additional outputs if generated
            if (!toAsm && outputFile.EndsWith(".wasm", StringComparison.OrdinalIgnoreCase))
            {
                // Determine project name for file naming (same logic as above)
                string projectNameForFiles = "output";  // Default fallback
                try
                {
                    var fileDiscovery = ProjectDiscovery.Discover(projectPath);
                    if (fileDiscovery.ProjectFiles.Count > 0)
                    {
                        projectNameForFiles = Path.GetFileNameWithoutExtension(fileDiscovery.ProjectFiles[0]);
                    }
                    else if (!string.IsNullOrEmpty(fileDiscovery.SolutionFile))
                    {
                        projectNameForFiles = Path.GetFileNameWithoutExtension(fileDiscovery.SolutionFile);
                    }
                    else if (Directory.Exists(projectPath))
                    {
                        projectNameForFiles = Path.GetFileName(Path.GetFullPath(projectPath));
                    }
                }
                catch
                {
                    // If discovery fails, use default
                }
                
                string wasmBinaryPath = Path.Combine(outputPath, $"{projectNameForFiles}.wasm");
                string jsPath = Path.Combine(outputPath, $"{projectNameForFiles}.js");
                string htmlPath = Path.Combine(outputPath, $"{projectNameForFiles}.html");
                string watPath = Path.Combine(outputPath, $"{projectNameForFiles}.wat");
                
                if (File.Exists(wasmBinaryPath))
                    System.Console.WriteLine($"  WASM binary: {wasmBinaryPath} ({new FileInfo(wasmBinaryPath).Length} bytes)");
                if (File.Exists(jsPath))
                    System.Console.WriteLine($"  JS wrapper: {jsPath}");
                if (File.Exists(htmlPath))
                    System.Console.WriteLine($"  HTML runner: {htmlPath}");
                if (File.Exists(watPath))
                    System.Console.WriteLine($"  WAT text: {watPath}");
                    
                System.Console.WriteLine();
                System.Console.WriteLine("To run in browser:");
                System.Console.WriteLine($"  1. Open {htmlPath} in your web browser");
                System.Console.WriteLine("  2. Or serve with: python -m http.server 8000");
            }
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
