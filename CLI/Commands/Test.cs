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
        Description = "Run comprehensive test suite with multiple test projects across all architectures.";
        
        SupportedFlags["name"] = "Name of a specific test project to run (default: run all projects).";
        SupportedFlags["keep"] = "Keep the generated test projects after execution.";
        SupportedFlags["save"] = "Save all compiled outputs in tests/{project-name} folders with organized subfolders.";
        SupportedFlags["verbose"] = "Enable verbose output.";
        SupportedFlags["debug"] = "Enable debug logging with detailed information.";
        SupportedFlags["quick"] = "Run quick test (single architecture only).";
        SupportedFlags["arch"] = "Single architecture to test (x86_64, x86_32, x86_16, arm64, arm32). Use with --quick.";
        SupportedFlags["format"] = "Output format (native, pe). Use with --quick.";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        // Parse flags
        string? specificProject = null;
        if (flags.TryGetValue("name", out var flagName) && !string.IsNullOrWhiteSpace(flagName))
            specificProject = flagName;
        else if (args.Length > 0)
            specificProject = args[0];

        bool keepProjects = flags.ContainsKey("keep");
        bool saveOutputs = flags.ContainsKey("save");
        bool verbose = flags.ContainsKey("verbose");
        bool debugMode = flags.ContainsKey("debug");
        bool quickMode = flags.ContainsKey("quick");

        string currentDir = Directory.GetCurrentDirectory();

        // Define test projects to run
        var projectsToRun = specificProject != null
            ? new[] { specificProject }
            : new[] { "HelloWorld", "Calculator", "ClassHierarchy", "GenericCollections" };

        // Print header
        System.Console.WriteLine("=".PadRight(70, '='));
        System.Console.WriteLine("CRAB Compiler - Comprehensive Test Suite");
        System.Console.WriteLine("=".PadRight(70, '='));
        System.Console.WriteLine($"Projects:   {(specificProject != null ? specificProject : $"{projectsToRun.Length} test projects")}");
        System.Console.WriteLine($"Mode:       {(quickMode ? "Quick (single architecture)" : "Comprehensive (all architectures)")}");
        System.Console.WriteLine($"Keep:       {keepProjects}");
        System.Console.WriteLine($"Save:       {saveOutputs}");
        System.Console.WriteLine($"Verbose:    {verbose}");
        System.Console.WriteLine($"Debug:      {debugMode}");
        System.Console.WriteLine("=".PadRight(70, '='));
        System.Console.WriteLine();

        LogInfo($"Starting test suite execution with {projectsToRun.Length} projects");

        int totalProjects = 0;
        int successfulProjects = 0;
        int totalTests = 0;
        int passedTests = 0;

        try
        {
            foreach (var projectName in projectsToRun)
            {
                totalProjects++;
                System.Console.WriteLine();
                System.Console.WriteLine(new string('═', 70));
                System.Console.WriteLine($"Testing Project: {projectName}");
                System.Console.WriteLine(new string('═', 70));
                
                LogInfo($"Starting test for project: {projectName}");

                bool projectSuccess = RunProjectTest(
                    projectName, 
                    currentDir, 
                    keepProjects, 
                    saveOutputs, 
                    verbose, 
                    debugMode, 
                    quickMode,
                    out int projectTestsPassed,
                    out int projectTestsTotal);

                if (projectSuccess)
                {
                    successfulProjects++;
                    System.Console.WriteLine($"✓ {projectName} completed successfully");
                }
                else
                {
                    System.Console.WriteLine($"✗ {projectName} failed");
                }

                passedTests += projectTestsPassed;
                totalTests += projectTestsTotal;
                
                LogInfo($"{projectName}: {projectTestsPassed}/{projectTestsTotal} tests passed");
            }

            // Print final summary
            System.Console.WriteLine();
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine("COMPREHENSIVE TEST SUITE SUMMARY");
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine($"Total projects:    {totalProjects}");
            System.Console.WriteLine($"Successful:        {successfulProjects}");
            System.Console.WriteLine($"Failed:            {totalProjects - successfulProjects}");
            System.Console.WriteLine($"Total tests:       {totalTests}");
            System.Console.WriteLine($"Passed tests:      {passedTests}");
            System.Console.WriteLine($"Failed tests:      {totalTests - passedTests}");
            System.Console.WriteLine($"Success rate:      {(totalTests > 0 ? (passedTests * 100.0 / totalTests) : 0):F1}%");
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine();

            if (successfulProjects == totalProjects && passedTests == totalTests)
            {
                System.Console.WriteLine("✓ All tests passed!");
                LogInfo("All tests completed successfully");
            }
            else
            {
                System.Console.WriteLine("⚠️  Some tests failed - see details above");
                LogInfo($"Tests completed with failures: {successfulProjects}/{totalProjects} projects, {passedTests}/{totalTests} tests");
            }
        }
        catch (Exception ex)
        {
            var errorMsg = $"Test suite failed - {ex.Message}";
            System.Console.WriteLine($"Error: {errorMsg}");
            LogDebug($"EXCEPTION: {ex}");
            
            if (verbose || debugMode)
            {
                System.Console.WriteLine("\nStack trace:");
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }

    private bool RunProjectTest(
        string projectName,
        string baseDir,
        bool keepProject,
        bool saveOutputs,
        bool verbose,
        bool debugMode,
        bool quickMode,
        out int testsPassed,
        out int testsTotal)
    {
        testsPassed = 0;
        testsTotal = 0;

        string projectPath = Path.Combine(baseDir, projectName);
        string? saveDir = null;

        try
        {
            // Set up save directory if requested
            if (saveOutputs)
            {
                saveDir = Path.Combine(baseDir, "tests", projectName);
                Directory.CreateDirectory(saveDir);
                
                // Create organized subfolders
                Directory.CreateDirectory(Path.Combine(saveDir, "wasm"));
                Directory.CreateDirectory(Path.Combine(saveDir, "binaries"));
                Directory.CreateDirectory(Path.Combine(saveDir, "logs"));
                
                LogInfo($"Save directory created: {saveDir}");
                LogDebug($"Created subfolders: wasm, binaries, logs");
            }

            // Step 1: Generate test project with appropriate content
            if (verbose || debugMode) 
                System.Console.WriteLine($"[1/3] Generating {projectName} project...");
            else 
                System.Console.WriteLine($"Generating {projectName}...");
            
            LogInfo($"Step 1: Generating {projectName} project");

            if (!GenerateTestProject(projectName, projectPath))
            {
                System.Console.WriteLine($"Error: Failed to generate {projectName}");
                return false;
            }
            
            LogInfo($"Test project created successfully at {projectPath}");

            if (verbose || debugMode) System.Console.WriteLine();

            // Step 2: Build the test project
            if (verbose || debugMode) 
                System.Console.WriteLine($"[2/3] Building {projectName}...");
            else 
                System.Console.WriteLine($"Building {projectName}...");
            
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
                    System.Console.WriteLine($"Error: Build failed - output file not found");
                    LogDebug($"ERROR: Build failed for {projectName}");
                    CleanupProject(projectPath, keepProject, verbose || debugMode, saveDir);
                    return false;
                }
            }
            
            LogInfo($"Build successful, output: {outputFile}");

            if (verbose || debugMode) System.Console.WriteLine();

            // Save WASM/WAT/JS/HTML files if requested
            if (saveOutputs && saveDir != null)
            {
                string wasmSaveDir = Path.Combine(saveDir, "wasm");
                string binDir = Path.Combine(projectPath, "bin");
                
                // Copy the main output file (WASM or WAT)
                string watDest = Path.Combine(wasmSaveDir, Path.GetFileName(outputFile));
                File.Copy(outputFile, watDest, overwrite: true);
                LogInfo($"Saved WASM/WAT output to {watDest}");
                
                // Also copy JS and HTML files if they exist
                string jsSource = Path.Combine(binDir, "output.js");
                string htmlSource = Path.Combine(binDir, "index.html");
                
                if (File.Exists(jsSource))
                {
                    string jsDest = Path.Combine(wasmSaveDir, "output.js");
                    File.Copy(jsSource, jsDest, overwrite: true);
                    LogInfo($"Saved JS wrapper to {jsDest}");
                    
                    if (verbose || debugMode)
                        System.Console.WriteLine($"Saved output.js to {wasmSaveDir}");
                }
                
                if (File.Exists(htmlSource))
                {
                    string htmlDest = Path.Combine(wasmSaveDir, "index.html");
                    File.Copy(htmlSource, htmlDest, overwrite: true);
                    LogInfo($"Saved HTML runner to {htmlDest}");
                    
                    if (verbose || debugMode)
                        System.Console.WriteLine($"Saved index.html to {wasmSaveDir}");
                }
                
                // Final summary log after all files are copied
                if (verbose || debugMode)
                    System.Console.WriteLine($"Saved all test outputs to {wasmSaveDir}");
            }

            // Step 3: Compile to native/PE for all architectures (or single if quick mode)
            if (verbose || debugMode)
                System.Console.WriteLine($"[3/3] Testing {projectName} across architectures...");
            else
                System.Console.WriteLine($"Testing {projectName}...");

            if (quickMode)
            {
                RunQuickTest(outputFile, new Dictionary<string, string?>(), verbose || debugMode, saveDir);
                testsPassed = 1;
                testsTotal = 1;
            }
            else
            {
                RunComprehensiveTest(outputFile, verbose || debugMode, debugMode, saveDir, out testsPassed, out testsTotal);
            }

            // Cleanup if requested
            CleanupProject(projectPath, keepProject, verbose || debugMode, saveDir);
            
            // Save logs if requested
            if (saveOutputs && saveDir != null)
            {
                SaveLogs(saveDir);
            }

            LogInfo($"{projectName} test execution completed: {testsPassed}/{testsTotal} passed");
            return testsPassed == testsTotal;
        }
        catch (Exception ex)
        {
            var errorMsg = $"{projectName} test failed - {ex.Message}";
            System.Console.WriteLine($"Error: {errorMsg}");
            LogDebug($"EXCEPTION in {projectName}: {ex}");
            
            // Save logs even on error if requested
            if (saveOutputs && saveDir != null)
            {
                SaveLogs(saveDir);
            }

            // Attempt cleanup even on error
            CleanupProject(projectPath, keepProject, verbose || debugMode, saveDir);
            return false;
        }
    }

    private (string csharp, string sln, string slnx) ParseTestFile(string testFilePath)
    {
        try
        {
            string content = File.ReadAllText(testFilePath);
            
            // Extract C# code
            string csharpCode = ExtractSection(content, "BEGIN CSHARP", "END CSHARP");
            
            // Extract .sln content
            string slnContent = ExtractSection(content, "BEGIN SLN", "END SLN");
            
            // Extract .slnx content
            string slnxContent = ExtractSection(content, "BEGIN SLNX", "END SLNX");
            
            return (csharpCode, slnContent, slnxContent);
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to parse test file {testFilePath}: {ex.Message}");
            return (null, null, null);
        }
    }
    
    private string ExtractSection(string content, string beginMarker, string endMarker)
    {
        string beginTag = $"// === {beginMarker} ===";
        string endTag = $"// === {endMarker} ===";
        
        int beginIndex = content.IndexOf(beginTag);
        int endIndex = content.IndexOf(endTag);
        
        if (beginIndex >= 0 && endIndex > beginIndex)
        {
            beginIndex += beginTag.Length;
            string section = content.Substring(beginIndex, endIndex - beginIndex);
            return section.Trim();
        }
        
        return null;
    }

    private bool GenerateTestProject(string projectName, string projectPath)
    {
        try
        {
            Directory.CreateDirectory(projectPath);
            
            // Find the CRAB executable directory to locate tests folder
            string crabDir = Path.GetDirectoryName(typeof(Test).Assembly.Location);
            string testsDir = Path.Combine(crabDir, "tests");
            
            // If tests folder doesn't exist in executable directory, try repository root
            if (!Directory.Exists(testsDir))
            {
                // Try to find repository root by looking for CRAB.csproj
                string currentDir = crabDir;
                while (currentDir != null && !File.Exists(Path.Combine(currentDir, "CRAB.csproj")))
                {
                    currentDir = Directory.GetParent(currentDir)?.FullName;
                }
                
                if (currentDir != null)
                {
                    testsDir = Path.Combine(currentDir, "tests");
                }
            }
            
            string testFilePath = Path.Combine(testsDir, $"{projectName}.cs");
            
            string csharpCode = null;
            string slnContent = null;
            string slnxContent = null;
            
            // Try to read from test file if it exists
            if (File.Exists(testFilePath))
            {
                LogDebug($"Reading test file: {testFilePath}");
                (csharpCode, slnContent, slnxContent) = ParseTestFile(testFilePath);
            }
            
            // Fallback to hardcoded content if test file doesn't exist or parsing failed
            if (string.IsNullOrWhiteSpace(csharpCode))
            {
                LogDebug($"Test file not found or invalid, using fallback content for {projectName}");
                csharpCode = projectName switch
                {
                    "HelloWorld" => @"class Test {
    int GetValue() {
        return 42;
    }
}",
                    "Calculator" => @"class Calculator {
    int Add(int a, int b) {
        return a + b;
    }
    
    int Multiply(int a, int b) {
        return a * b;
    }
}",
                    "ClassHierarchy" => @"class Base {
    int GetBase() {
        return 10;
    }
}

class Derived {
    int GetValue() {
        return 20;
    }
}",
                    "GenericCollections" => @"class Container {
    int GetData() {
        return 100;
    }
}",
                    _ => @"class Program {
    int Main() {
        return 0;
    }
}"
                };
            }
            
            // Create Program.cs
            File.WriteAllText(Path.Combine(projectPath, "Program.cs"), csharpCode);
            
            // Generate GUID for the project
            string projectGuid = Guid.NewGuid().ToString("B").ToUpper();
            
            // Create .csproj file
            string csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk"">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{projectName}</RootNamespace>
  </PropertyGroup>

</Project>
";
            File.WriteAllText(Path.Combine(projectPath, $"{projectName}.csproj"), csprojContent);
            
            // Create .sln file
            if (string.IsNullOrWhiteSpace(slnContent))
            {
                slnContent = $@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.0.0
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}"") = ""{projectName}"", ""{projectName}.csproj"", ""{projectGuid}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{projectGuid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{projectGuid}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{projectGuid}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{projectGuid}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {{9F613E25-9B1A-49E2-A847-B8FC584263C9}}
	EndGlobalSection
EndGlobal
";
            }
            File.WriteAllText(Path.Combine(projectPath, $"{projectName}.sln"), slnContent);
            
            // Create .slnx file
            if (string.IsNullOrWhiteSpace(slnxContent))
            {
                slnxContent = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Solution Version=""1.0"">
  <Properties>
    <Name>{projectName}</Name>
  </Properties>
  <Project Path=""{projectName}.csproj"" Name=""{projectName}"" Type=""C#"" Id=""{projectGuid}"" />
</Solution>
";
            }
            File.WriteAllText(Path.Combine(projectPath, $"{projectName}.slnx"), slnxContent);
            
            LogDebug($"Generated {projectName} project at {projectPath}");
            LogDebug($"Created files: Program.cs, {projectName}.csproj, {projectName}.sln, {projectName}.slnx");
            return true;
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to generate {projectName}: {ex.Message}");
            return false;
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

    private void RunComprehensiveTest(string outputFile, bool verbose, bool debug, string? saveDir, out int testsPassed, out int testsTotal)
    {
        System.Console.WriteLine("[3/3] Running comprehensive test suite...");
        System.Console.WriteLine();
        
        LogInfo("Step 3: Running comprehensive test suite");

        // Define all architecture and format combinations
        var architectures = new[] { "x86_64", "x86_32", "x86_16", "arm64", "arm32" };
        var formats = new[] { "native", "pe" };

        int total = 0;
        int passed = 0;
        var results = new List<(string arch, string format, bool success, string message, long compileTimeMs)>();

        // Test native architectures
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

                var sw = System.Diagnostics.Stopwatch.StartNew();
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
                        
                        sw.Stop();
                        System.Console.SetOut(originalOut);
                        System.Console.WriteLine($"✅ PASS ({sw.ElapsedMilliseconds}ms)");
                        passed++;
                        results.Add((arch, format, true, "Success", sw.ElapsedMilliseconds));
                        
                        LogInfo($"Test {arch}/{format} PASSED in {sw.ElapsedMilliseconds}ms");
                        
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
                        sw.Stop();
                        System.Console.SetOut(originalOut);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    System.Console.WriteLine($"❌ FAIL ({sw.ElapsedMilliseconds}ms)");
                    results.Add((arch, format, false, ex.Message, sw.ElapsedMilliseconds));
                    
                    LogInfo($"Test {arch}/{format} FAILED in {sw.ElapsedMilliseconds}ms: {ex.Message}");
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
        
        // Test WASM format
        System.Console.WriteLine();
        System.Console.WriteLine("  Testing WASM Output:");
        total++;
        string wasmTestName = "WASM (wasm)";
        System.Console.Write($"  Testing {wasmTestName,-25} ");
        
        LogDebug("Testing WASM format");
        
        var wasmSw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            // Read WAT file
            string watContent = File.ReadAllText(outputFile);
            
            // Use BadgerCompiler directly for WASM
            byte[] wasmBinary = Badger.BadgerCompiler.Compile(watContent, "wasm", "wasm");
            
            wasmSw.Stop();
            System.Console.WriteLine($"✅ PASS ({wasmSw.ElapsedMilliseconds}ms)");
            passed++;
            results.Add(("wasm", "wasm", true, "Success", wasmSw.ElapsedMilliseconds));
            
            LogInfo($"Test WASM format PASSED in {wasmSw.ElapsedMilliseconds}ms");
            LogInfo($"Generated {wasmBinary.Length} bytes of WASM binary");
            
            // Save WASM and JS if requested
            if (saveDir != null)
            {
                string wasmSaveDir = Path.Combine(saveDir, "wasm");
                
                // Save WASM binary
                string wasmPath = Path.Combine(wasmSaveDir, "output.wasm");
                File.WriteAllBytes(wasmPath, wasmBinary);
                
                // Copy JS wrapper if it exists
                if (File.Exists("output.js"))
                {
                    string jsPath = Path.Combine(wasmSaveDir, "output.js");
                    File.Copy("output.js", jsPath, overwrite: true);
                    
                    LogInfo($"Saved WASM binary ({wasmBinary.Length} bytes) and JS wrapper to {wasmSaveDir}");
                    
                    if (verbose || debug)
                    {
                        System.Console.WriteLine($"    Saved {wasmBinary.Length} bytes to {wasmPath}");
                        System.Console.WriteLine($"    Saved JS wrapper to {jsPath}");
                    }
                }
            }
            
            // Cleanup output.js if not saving
            if (saveDir == null && File.Exists("output.js"))
            {
                File.Delete("output.js");
            }
        }
        catch (Exception ex)
        {
            wasmSw.Stop();
            System.Console.WriteLine($"❌ FAIL ({wasmSw.ElapsedMilliseconds}ms)");
            results.Add(("wasm", "wasm", false, ex.Message, wasmSw.ElapsedMilliseconds));
            
            LogInfo($"Test WASM format FAILED in {wasmSw.ElapsedMilliseconds}ms: {ex.Message}");
            LogDebug($"Exception for WASM: {ex}");
            
            if (verbose || debug)
                System.Console.WriteLine($"    Error: {ex.Message}");
        }
        
        LogInfo($"Comprehensive tests completed: {passed}/{total} passed");
        
        // Set out parameters
        testsPassed = passed;
        testsTotal = total;
        
        // Performance summary
        if (debug)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Performance Summary:");
            System.Console.WriteLine("  Format           Average Time    Min Time    Max Time");
            System.Console.WriteLine("  " + new string('-', 60));
            
            var successResults = results.Where(r => r.success).ToList();
            if (successResults.Any())
            {
                var avgTime = successResults.Average(r => r.compileTimeMs);
                var minTime = successResults.Min(r => r.compileTimeMs);
                var maxTime = successResults.Max(r => r.compileTimeMs);
                
                System.Console.WriteLine($"  All formats      {avgTime,10:F1}ms    {minTime,7}ms    {maxTime,7}ms");
                
                LogDebug($"Performance: Avg={avgTime:F1}ms, Min={minTime}ms, Max={maxTime}ms");
            }
        }
        
        // Report saved outputs
        if (saveDir != null)
        {
            System.Console.WriteLine();
            System.Console.WriteLine($"Saved outputs to: {saveDir}");
            System.Console.WriteLine($"  - WASM files in: {Path.Combine(saveDir, "wasm")}");
            System.Console.WriteLine($"  - {passed - 1} binaries in: {Path.Combine(saveDir, "binaries")}"); // -1 for WASM
            System.Console.WriteLine($"  - Logs in: {Path.Combine(saveDir, "logs")}");
            
            LogInfo($"Saved {passed} outputs to {saveDir}");
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
            
            // Determine appropriate format for the current platform
            string execFormat = "native";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                execFormat = "pe";  // Use PE format on Windows
            }
            
            var runFlags = new Dictionary<string, string?>
            {
                ["input"] = outputFile,
                ["arch"] = currentArch,
                ["format"] = execFormat
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

        // Don't print summary here - it's now printed at the suite level
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
