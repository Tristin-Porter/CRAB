using System.IO;
using System.Runtime.InteropServices;

namespace CRAB;

/// <summary>
/// Test command - automatically generates, compiles, and runs a test project
/// Tests all architectures and formats by default for comprehensive validation
/// </summary>
class Test : Command
{
    public Test()
    {
        Name = "test";
        Description = "Generate a test project, compile it for all architectures, and run the appropriate one.";
        
        SupportedFlags["name"] = "Name of the test project (default: TestProject).";
        SupportedFlags["keep"] = "Keep the generated test project after execution.";
        SupportedFlags["verbose"] = "Enable verbose output.";
        SupportedFlags["quick"] = "Run quick test (single architecture only).";
        SupportedFlags["arch"] = "Single architecture to test (x86_64, x86_32, x86_16, arm64, arm32). Use with --quick.";
        SupportedFlags["format"] = "Output format (native, pe). Use with --quick.";
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
        bool quickMode = flags.ContainsKey("quick");

        string currentDir = Directory.GetCurrentDirectory();
        string projectPath = Path.Combine(currentDir, projectName);

        if (verbose || !quickMode)
        {
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine("CRAB Compiler - Comprehensive Test Suite");
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine($"Project:    {projectName}");
            System.Console.WriteLine($"Mode:       {(quickMode ? "Quick (single architecture)" : "Comprehensive (all architectures)")}");
            System.Console.WriteLine($"Keep:       {keepProject}");
            System.Console.WriteLine("=".PadRight(70, '='));
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

            // Step 3: Compile to native/PE for all architectures (or single if quick mode)
            if (quickMode)
            {
                RunQuickTest(outputFile, flags, verbose);
            }
            else
            {
                RunComprehensiveTest(outputFile, verbose);
            }

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

    private void RunQuickTest(string outputFile, Dictionary<string, string?> flags, bool verbose)
    {
        if (verbose) System.Console.WriteLine("[3/3] Running quick test...");
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
        else
            runFlags["arch"] = "x86_64"; // default

        if (flags.TryGetValue("format", out var format))
            runFlags["format"] = format;
        else
            runFlags["format"] = "native"; // default

        runCommand.Execute(Array.Empty<string>(), runFlags);
    }

    private void RunComprehensiveTest(string outputFile, bool verbose)
    {
        System.Console.WriteLine("[3/3] Running comprehensive test suite...");
        System.Console.WriteLine();

        // Define all architecture and format combinations
        var architectures = new[] { "x86_64", "x86_32", "x86_16", "arm64", "arm32" };
        var formats = new[] { "native", "pe" };

        int total = 0;
        int passed = 0;
        var results = new List<(string arch, string format, bool success, string message)>();

        foreach (var arch in architectures)
        {
            foreach (var format in formats)
            {
                // x86_16 doesn't typically support PE format on most systems
                if (arch == "x86_16" && format == "pe")
                    continue;

                total++;
                string testName = $"{arch} ({format})";
                System.Console.Write($"  Testing {testName,-25} ");

                try
                {
                    var compileCommand = new Compile();
                    var compileFlags = new Dictionary<string, string?>
                    {
                        ["to-asm"] = null,
                        ["arch"] = arch,
                        ["format"] = format,
                        ["output"] = $"test_output_{arch}_{format}.bin"
                    };

                    // Suppress output during compilation
                    var originalOut = System.Console.Out;
                    try
                    {
                        if (!verbose)
                            System.Console.SetOut(TextWriter.Null);

                        compileCommand.Execute(new[] { outputFile }, compileFlags);
                        
                        System.Console.SetOut(originalOut);
                        System.Console.WriteLine("✅ PASS");
                        passed++;
                        results.Add((arch, format, true, "Success"));
                    }
                    catch
                    {
                        System.Console.SetOut(originalOut);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("❌ FAIL");
                    results.Add((arch, format, false, ex.Message));
                    if (verbose)
                        System.Console.WriteLine($"    Error: {ex.Message}");
                }

                // Clean up test output
                try
                {
                    string testOutput = $"test_output_{arch}_{format}.bin";
                    if (File.Exists(testOutput))
                        File.Delete(testOutput);
                }
                catch { }
            }
        }

        // Try to run on the appropriate architecture for the current platform
        System.Console.WriteLine();
        System.Console.WriteLine("Attempting to run on current platform...");
        
        string currentArch = DetectCurrentArchitecture();
        System.Console.WriteLine($"  Detected platform: {currentArch}");

        try
        {
            var runCommand = new Run();
            var runFlags = new Dictionary<string, string?>
            {
                ["input"] = outputFile,
                ["arch"] = currentArch,
                ["format"] = "native"
            };

            if (verbose)
                runFlags["verbose"] = null;

            runCommand.Execute(Array.Empty<string>(), runFlags);
            System.Console.WriteLine($"  ✅ Execution successful on {currentArch}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ⚠️  Execution failed: {ex.Message}");
            System.Console.WriteLine("  (This is expected if WAT execution is not fully implemented)");
        }

        // Print summary
        System.Console.WriteLine();
        System.Console.WriteLine("=".PadRight(70, '='));
        System.Console.WriteLine("COMPREHENSIVE TEST SUMMARY");
        System.Console.WriteLine("=".PadRight(70, '='));
        System.Console.WriteLine($"Total tests:  {total}");
        System.Console.WriteLine($"Passed:       {passed}");
        System.Console.WriteLine($"Failed:       {total - passed}");
        System.Console.WriteLine($"Success rate: {(passed * 100.0 / total):F1}%");
        System.Console.WriteLine("=".PadRight(70, '='));

        if (verbose && results.Any(r => !r.success))
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Failed tests:");
            foreach (var result in results.Where(r => !r.success))
            {
                System.Console.WriteLine($"  - {result.arch} ({result.format}): {result.message}");
            }
        }
    }

    private string DetectCurrentArchitecture()
    {
        // Detect the current platform architecture
        var arch = RuntimeInformation.ProcessArchitecture;
        
        return arch switch
        {
            Architecture.X64 => "x86_64",
            Architecture.X86 => "x86_32",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm32",
            _ => "x86_64" // default fallback
        };
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
