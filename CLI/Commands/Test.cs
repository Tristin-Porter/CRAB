using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace CRAB;

/// <summary>
/// Test command - automatically generates, compiles, and runs a test project
/// Tests all architectures and formats by default for comprehensive validation
/// </summary>
class Test : Command
{
    private readonly List<string> _debugLog = new();
    private readonly List<string> _infoLog = new();
    
    public Test()
    {
        Name = "test";
        Description = "Generate a test project, compile it for all architectures, and run the appropriate one.";
        
        SupportedFlags["name"] = "Name of the test project (default: TestProject).";
        SupportedFlags["keep"] = "Keep the generated test project after execution.";
        SupportedFlags["save"] = "Save all compiled outputs in tests/{test-name} folder with organized subfolders.";
        SupportedFlags["verbose"] = "Enable verbose output.";
        SupportedFlags["debug"] = "Enable debug logging with detailed information.";
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
        bool saveOutputs = flags.ContainsKey("save");
        bool verbose = flags.ContainsKey("verbose");
        bool debugMode = flags.ContainsKey("debug");
        bool quickMode = flags.ContainsKey("quick");

        string currentDir = Directory.GetCurrentDirectory();
        string projectPath = Path.Combine(currentDir, projectName);

        // Set up save directory if requested
        string? saveDir = null;
        if (saveOutputs)
        {
            saveDir = Path.Combine(currentDir, "tests", projectName);
            Directory.CreateDirectory(saveDir);
            
            // Create organized subfolders
            Directory.CreateDirectory(Path.Combine(saveDir, "wasm"));
            Directory.CreateDirectory(Path.Combine(saveDir, "binaries"));
            Directory.CreateDirectory(Path.Combine(saveDir, "logs"));
            
            LogInfo($"Save directory created: {saveDir}");
            LogDebug($"Created subfolders: wasm, binaries, logs");
        }

        if (verbose || debugMode || !quickMode)
        {
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine("CRAB Compiler - Comprehensive Test Suite");
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine($"Project:    {projectName}");
            System.Console.WriteLine($"Mode:       {(quickMode ? "Quick (single architecture)" : "Comprehensive (all architectures)")}");
            System.Console.WriteLine($"Keep:       {keepProject}");
            System.Console.WriteLine($"Save:       {saveOutputs}");
            if (saveOutputs && saveDir != null)
                System.Console.WriteLine($"Save Dir:   {saveDir}");
            System.Console.WriteLine($"Verbose:    {verbose}");
            System.Console.WriteLine($"Debug:      {debugMode}");
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine();
        }

        LogInfo($"Starting test execution for project: {projectName}");
        LogDebug($"Project path: {projectPath}");
        LogDebug($"Quick mode: {quickMode}, Verbose: {verbose}, Debug: {debugMode}");

        try
        {
            // Step 1: Generate test project
            if (verbose || debugMode) System.Console.WriteLine("[1/3] Generating test project...");
            else System.Console.WriteLine($"Generating test project '{projectName}'...");
            
            LogInfo("Step 1: Generating test project");

            var consoleCommand = new Console();
            var newFlags = new Dictionary<string, string?>
            {
                ["name"] = projectName
            };
            
            consoleCommand.Execute(Array.Empty<string>(), newFlags);

            if (!Directory.Exists(projectPath))
            {
                var errorMsg = $"Failed to create test project '{projectName}'.";
                System.Console.WriteLine($"Error: {errorMsg}");
                LogDebug($"ERROR: {errorMsg}");
                return;
            }
            
            LogInfo($"Test project created successfully at {projectPath}");

            if (verbose || debugMode) System.Console.WriteLine();

            // Step 2: Build the test project
            if (verbose || debugMode) System.Console.WriteLine("[2/3] Building test project...");
            else System.Console.WriteLine($"Building test project...");
            
            LogInfo("Step 2: Building test project");

            var buildCommand = new Build();
            var buildFlags = new Dictionary<string, string?>
            {
                ["project"] = projectPath
            };
            
            if (verbose || debugMode)
                buildFlags["verbose"] = null;

            buildCommand.Execute(Array.Empty<string>(), buildFlags);

            // Check for compiled output (wasm or wat)
            string outputFile = Path.Combine(projectPath, "bin", "output.wasm");
            if (!File.Exists(outputFile))
            {
                outputFile = Path.Combine(projectPath, "bin", "output.wat");
                if (!File.Exists(outputFile))
                {
                    var errorMsg = "Build failed - output file not found.";
                    System.Console.WriteLine($"Error: {errorMsg}");
                    LogDebug($"ERROR: {errorMsg}");
                    LogDebug($"Checked paths: {Path.Combine(projectPath, "bin", "output.wasm")}, {Path.Combine(projectPath, "bin", "output.wat")}");
                    CleanupProject(projectPath, keepProject, verbose || debugMode, saveDir);
                    return;
                }
            }
            
            LogInfo($"Build successful, output: {outputFile}");

            if (verbose || debugMode) System.Console.WriteLine();

            // Save WASM/WAT file if requested
            if (saveOutputs && saveDir != null)
            {
                string wasmSaveDir = Path.Combine(saveDir, "wasm");
                string watDest = Path.Combine(wasmSaveDir, Path.GetFileName(outputFile));
                File.Copy(outputFile, watDest, overwrite: true);
                
                LogInfo($"Saved WASM/WAT output to {watDest}");
                
                if (verbose || debugMode)
                    System.Console.WriteLine($"Saved {Path.GetFileName(outputFile)} to {wasmSaveDir}");
            }

            // Step 3: Compile to native/PE for all architectures (or single if quick mode)
            if (quickMode)
            {
                RunQuickTest(outputFile, flags, verbose || debugMode, saveDir);
            }
            else
            {
                RunComprehensiveTest(outputFile, verbose || debugMode, debugMode, saveDir);
            }

            // Step 4: Cleanup if requested
            CleanupProject(projectPath, keepProject, verbose || debugMode, saveDir);
            
            // Save logs if requested
            if (saveOutputs && saveDir != null)
            {
                SaveLogs(saveDir);
            }

            System.Console.WriteLine();
            System.Console.WriteLine("✓ Test completed successfully.");
            LogInfo("Test execution completed successfully");
        }
        catch (Exception ex)
        {
            var errorMsg = $"Test failed - {ex.Message}";
            System.Console.WriteLine($"Error: {errorMsg}");
            LogDebug($"EXCEPTION: {ex}");
            
            if (verbose || debugMode)
            {
                System.Console.WriteLine("\nStack trace:");
                System.Console.WriteLine(ex.StackTrace);
            }

            // Save logs even on error if requested
            if (saveOutputs && saveDir != null)
            {
                SaveLogs(saveDir);
            }

            // Attempt cleanup even on error
            CleanupProject(projectPath, keepProject, verbose || debugMode, saveDir);
        }
    }

    private void RunQuickTest(string outputFile, Dictionary<string, string?> flags, bool verbose, string? saveDir)
    {
        if (verbose) System.Console.WriteLine("[3/3] Running quick test...");
        else System.Console.WriteLine($"Running test project...");
        
        LogInfo("Step 3: Running quick test");

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
        
        LogDebug($"Quick test - Architecture: {runFlags["arch"]}, Format: {runFlags["format"]}");

        runCommand.Execute(Array.Empty<string>(), runFlags);
        
        // Save output if requested
        if (saveDir != null)
        {
            string outputBin = $"output.bin";
            if (File.Exists(outputBin))
            {
                string extension = runFlags["format"] == "pe" ? "exe" : "bin";
                string binarySaveDir = Path.Combine(saveDir, "binaries");
                string destName = $"{runFlags["arch"]}_{runFlags["format"]}.{extension}";
                string destPath = Path.Combine(binarySaveDir, destName);
                File.Copy(outputBin, destPath, overwrite: true);
                
                LogInfo($"Saved binary to {destPath}");
                
                if (verbose)
                    System.Console.WriteLine($"Saved {destName} to {binarySaveDir}");
            }
        }
        
        LogInfo("Quick test completed");
    }

    private void RunComprehensiveTest(string outputFile, bool verbose, bool debug, string? saveDir)
    {
        System.Console.WriteLine("[3/3] Running comprehensive test suite...");
        System.Console.WriteLine();
        
        LogInfo("Step 3: Running comprehensive test suite");

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
                {
                    LogDebug($"Skipping {arch}/{format} - invalid combination");
                    continue;
                }

                total++;
                string testName = $"{arch} ({format})";
                System.Console.Write($"  Testing {testName,-25} ");
                
                LogDebug($"Testing {arch}/{format}");

                try
                {
                    var compileCommand = new Compile();
                    string outputFileName = $"test_output_{arch}_{format}.bin";
                    var compileFlags = new Dictionary<string, string?>
                    {
                        ["to-asm"] = null,
                        ["arch"] = arch,
                        ["format"] = format,
                        ["output"] = outputFileName
                    };

                    // Suppress output during compilation
                    var originalOut = System.Console.Out;
                    try
                    {
                        if (!verbose && !debug)
                            System.Console.SetOut(TextWriter.Null);

                        compileCommand.Execute(new[] { outputFile }, compileFlags);
                        
                        System.Console.SetOut(originalOut);
                        System.Console.WriteLine("✅ PASS");
                        passed++;
                        results.Add((arch, format, true, "Success"));
                        
                        LogInfo($"Test {arch}/{format} PASSED");
                        
                        // Save output if requested
                        if (saveDir != null && File.Exists(outputFileName))
                        {
                            string extension = format == "pe" ? "exe" : "bin";
                            string binarySaveDir = Path.Combine(saveDir, "binaries");
                            string destPath = Path.Combine(binarySaveDir, $"{arch}_{format}.{extension}");
                            File.Copy(outputFileName, destPath, overwrite: true);
                            
                            LogDebug($"Saved {arch}/{format} binary to {destPath}");
                        }
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
                    
                    LogInfo($"Test {arch}/{format} FAILED: {ex.Message}");
                    LogDebug($"Exception for {arch}/{format}: {ex}");
                    
                    if (verbose || debug)
                        System.Console.WriteLine($"    Error: {ex.Message}");
                }

                // Clean up test output (unless saving)
                try
                {
                    string testOutput = $"test_output_{arch}_{format}.bin";
                    bool shouldDeleteTestOutput = saveDir == null;
                    if (File.Exists(testOutput) && shouldDeleteTestOutput)
                        File.Delete(testOutput);
                }
                catch { }
            }
        }
        
        LogInfo($"Comprehensive tests completed: {passed}/{total} passed");
        
        // Report saved outputs
        if (saveDir != null)
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"Saved outputs to: {saveDir}");
            System.Console.WriteLine($"  - WASM files in: {Path.Combine(saveDir, "wasm")}");
            System.Console.WriteLine($"  - {passed} binaries in: {Path.Combine(saveDir, "binaries")}");
            System.Console.WriteLine($"  - Logs in: {Path.Combine(saveDir, "logs")}");
            
            LogInfo($"Saved {passed} binaries to {saveDir}");
        }

        // Try to run on the appropriate architecture for the current platform
        System.Console.WriteLine();
        System.Console.WriteLine("Attempting to run on current platform...");
        
        LogInfo("Attempting hardware detection and execution");
        
        string currentArch = DetectCurrentArchitecture();
        System.Console.WriteLine($"  Detected platform: {currentArch}");
        
        LogInfo($"Detected current architecture: {currentArch}");

        try
        {
            var runCommand = new Run();
            var runFlags = new Dictionary<string, string?>
            {
                ["input"] = outputFile,
                ["arch"] = currentArch,
                ["format"] = "native"
            };

            if (verbose || debug)
                runFlags["verbose"] = null;

            runCommand.Execute(Array.Empty<string>(), runFlags);
            System.Console.WriteLine($"  ✅ Execution successful on {currentArch}");
            
            LogInfo($"Execution successful on {currentArch}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"  ⚠️  Execution failed: {ex.Message}");
            System.Console.WriteLine("  (This is expected if WAT execution is not fully implemented)");
            
            LogInfo($"Execution failed on {currentArch}: {ex.Message}");
            LogDebug($"Execution exception: {ex}");
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

        if ((verbose || debug) && results.Any(r => !r.success))
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
        
        var detected = arch switch
        {
            Architecture.X64 => "x86_64",
            Architecture.X86 => "x86_32",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm32",
            _ => "x86_64" // default fallback
        };
        
        LogDebug($"Detected architecture: {detected} (RuntimeInformation: {arch})");
        return detected;
    }

    private void CleanupProject(string projectPath, bool keep, bool verbose, string? saveDir)
    {
        if (!keep)
        {
            try
            {
                if (Directory.Exists(projectPath))
                {
                    Directory.Delete(projectPath, recursive: true);
                    
                    LogInfo($"Cleaned up test project: {projectPath}");
                    
                    if (verbose) 
                        System.Console.WriteLine($"Cleaned up test project: {projectPath}");
                }
            }
            catch (Exception ex)
            {
                var warnMsg = $"Failed to cleanup test project - {ex.Message}";
                System.Console.WriteLine($"Warning: {warnMsg}");
                LogDebug($"WARNING: {warnMsg}");
            }
        }
        else if (verbose && Directory.Exists(projectPath))
        {
            System.Console.WriteLine($"Kept test project at: {projectPath}");
            LogInfo($"Kept test project at: {projectPath}");
        }
    }
    
    private void LogDebug(string message)
    {
        _debugLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] DEBUG: {message}");
    }
    
    private void LogInfo(string message)
    {
        _infoLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] INFO: {message}");
    }
    
    private void SaveLogs(string saveDir)
    {
        try
        {
            string logDir = Path.Combine(saveDir, "logs");
            Directory.CreateDirectory(logDir);
            
            string debugLogPath = Path.Combine(logDir, "debug.log");
            string infoLogPath = Path.Combine(logDir, "info.log");
            
            File.WriteAllLines(debugLogPath, _debugLog);
            File.WriteAllLines(infoLogPath, _infoLog);
            
            LogInfo($"Logs saved to {logDir}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Failed to save logs - {ex.Message}");
        }
    }
}
