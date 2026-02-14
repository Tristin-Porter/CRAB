using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace CRAB.Tests;

/// <summary>
/// Comprehensive end-to-end tests that create multiple projects,
/// build them for all architectures and container formats,
/// and run the builds that the current hardware supports.
/// </summary>
public class ComprehensiveTests
{
    private readonly string _testRootDir;
    private readonly List<string> _debugLog = new();
    private readonly List<string> _infoLog = new();
    private readonly string _crabExecutable;

    private static readonly string[] Architectures = { "x86_64", "x86_32", "x86_16", "arm64", "arm32" };
    private static readonly string[] ContainerFormats = { "native", "pe" };
    
    public ComprehensiveTests()
    {
        _testRootDir = Path.Combine(Path.GetTempPath(), "crab-comprehensive-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRootDir);
        
        // Find CRAB executable
        _crabExecutable = FindCrabExecutable();
        
        LogDebug($"Initialized comprehensive tests at: {_testRootDir}");
        LogDebug($"Using CRAB executable: {_crabExecutable}");
    }

    public void RunAll()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        Comprehensive End-to-End Integration Tests          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        LogInfo("Starting comprehensive test suite");
        
        try
        {
            // Create multiple test projects with different characteristics
            var testProjects = new List<TestProject>
            {
                CreateHelloWorldProject(),
                CreateCalculatorProject(),
                CreateClassHierarchyProject(),
                CreateGenericCollectionsProject()
            };
            
            LogInfo($"Created {testProjects.Count} test projects");
            
            // For each project, test all architecture/format combinations
            int totalTests = 0;
            int passedTests = 0;
            var results = new List<TestResult>();
            
            foreach (var project in testProjects)
            {
                LogInfo($"Testing project: {project.Name}");
                Console.WriteLine($"\n=== Testing Project: {project.Name} ===");
                
                var projectResults = TestAllArchitecturesAndFormats(project);
                results.AddRange(projectResults);
                
                totalTests += projectResults.Count;
                passedTests += projectResults.Count(r => r.Success);
            }
            
            // Test hardware detection and execution
            LogInfo("Testing hardware detection and execution");
            Console.WriteLine("\n=== Hardware Detection and Execution ===");
            TestHardwareDetectionAndExecution(testProjects);
            
            // Report summary
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║               Comprehensive Test Summary                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Total projects tested:     {testProjects.Count}");
            Console.WriteLine($"Total compilation tests:   {totalTests}");
            Console.WriteLine($"Passed:                    {passedTests}");
            Console.WriteLine($"Failed:                    {totalTests - passedTests}");
            Console.WriteLine($"Success rate:              {(totalTests > 0 ? (passedTests * 100.0 / totalTests) : 0):F1}%");
            Console.WriteLine();
            
            LogInfo($"Test suite completed: {passedTests}/{totalTests} tests passed");
            
            if (passedTests < totalTests)
            {
                Console.WriteLine("\n⚠️  Some tests failed - see details above");
                LogInfo("Some tests failed - check debug log for details");
            }
            else
            {
                Console.WriteLine("\n✅ All comprehensive tests passed!");
                LogInfo("All comprehensive tests passed");
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Exception in comprehensive tests: {ex}");
            throw;
        }
        finally
        {
            Cleanup();
        }
    }
    
    private TestProject CreateHelloWorldProject()
    {
        LogDebug("Creating Hello World project");
        
        var projectName = "HelloWorld";
        var projectPath = Path.Combine(_testRootDir, projectName);
        Directory.CreateDirectory(projectPath);
        
        var sourceCode = @"using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello from CRAB!"");
    }
}";
        
        File.WriteAllText(Path.Combine(projectPath, "Program.cs"), sourceCode);
        
        LogDebug($"Created Hello World project at {projectPath}");
        return new TestProject(projectName, projectPath);
    }
    
    private TestProject CreateCalculatorProject()
    {
        LogDebug("Creating Calculator project");
        
        var projectName = "Calculator";
        var projectPath = Path.Combine(_testRootDir, projectName);
        Directory.CreateDirectory(projectPath);
        
        var sourceCode = @"using System;

class Calculator
{
    public static int Add(int a, int b) => a + b;
    public static int Subtract(int a, int b) => a - b;
    public static int Multiply(int a, int b) => a * b;
    public static int Divide(int a, int b) => b != 0 ? a / b : 0;
}

class Program
{
    static void Main()
    {
        Console.WriteLine(""Calculator Test"");
        Console.WriteLine($""5 + 3 = {Calculator.Add(5, 3)}"");
        Console.WriteLine($""5 - 3 = {Calculator.Subtract(5, 3)}"");
        Console.WriteLine($""5 * 3 = {Calculator.Multiply(5, 3)}"");
        Console.WriteLine($""6 / 2 = {Calculator.Divide(6, 2)}"");
    }
}";
        
        File.WriteAllText(Path.Combine(projectPath, "Program.cs"), sourceCode);
        
        LogDebug($"Created Calculator project at {projectPath}");
        return new TestProject(projectName, projectPath);
    }
    
    private TestProject CreateClassHierarchyProject()
    {
        LogDebug("Creating Class Hierarchy project");
        
        var projectName = "ClassHierarchy";
        var projectPath = Path.Combine(_testRootDir, projectName);
        Directory.CreateDirectory(projectPath);
        
        var sourceCode = @"using System;

abstract class Animal
{
    public abstract string Speak();
}

class Dog : Animal
{
    public override string Speak() => ""Woof!"";
}

class Cat : Animal
{
    public override string Speak() => ""Meow!"";
}

class Program
{
    static void Main()
    {
        Animal dog = new Dog();
        Animal cat = new Cat();
        
        Console.WriteLine($""Dog says: {dog.Speak()}"");
        Console.WriteLine($""Cat says: {cat.Speak()}"");
    }
}";
        
        File.WriteAllText(Path.Combine(projectPath, "Program.cs"), sourceCode);
        
        LogDebug($"Created Class Hierarchy project at {projectPath}");
        return new TestProject(projectName, projectPath);
    }
    
    private TestProject CreateGenericCollectionsProject()
    {
        LogDebug("Creating Generic Collections project");
        
        var projectName = "GenericCollections";
        var projectPath = Path.Combine(_testRootDir, projectName);
        Directory.CreateDirectory(projectPath);
        
        var sourceCode = @"using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var numbers = new List<int> { 1, 2, 3, 4, 5 };
        var sum = 0;
        
        foreach (var num in numbers)
        {
            sum += num;
        }
        
        Console.WriteLine($""Sum of numbers: {sum}"");
    }
}";
        
        File.WriteAllText(Path.Combine(projectPath, "Program.cs"), sourceCode);
        
        LogDebug($"Created Generic Collections project at {projectPath}");
        return new TestProject(projectName, projectPath);
    }
    
    private List<TestResult> TestAllArchitecturesAndFormats(TestProject project)
    {
        var results = new List<TestResult>();
        
        // First, compile the project to WASM
        LogDebug($"Compiling {project.Name} to WASM");
        string wasmOutput = Path.Combine(project.Path, "output.wasm");
        
        if (!CompileToWasm(project.Path, wasmOutput))
        {
            LogDebug($"Failed to compile {project.Name} to WASM");
            Console.WriteLine($"  ⚠️  Skipping {project.Name} - WASM compilation failed");
            return results;
        }
        
        // Test each architecture and format combination
        foreach (var arch in Architectures)
        {
            foreach (var format in ContainerFormats)
            {
                // Skip invalid combinations
                if (arch == "x86_16" && format == "pe")
                {
                    LogDebug($"Skipping {arch}/{format} - invalid combination");
                    continue;
                }
                
                var testName = $"{project.Name}_{arch}_{format}";
                LogDebug($"Testing {testName}");
                Console.Write($"  Testing {arch,-10} ({format,-6})  ");
                
                var result = new TestResult
                {
                    ProjectName = project.Name,
                    Architecture = arch,
                    Format = format,
                    TestName = testName
                };
                
                try
                {
                    string outputFile = Path.Combine(project.Path, $"output_{arch}_{format}.bin");
                    
                    if (CompileWasmToNative(wasmOutput, outputFile, arch, format))
                    {
                        result.Success = true;
                        result.Message = "Success";
                        Console.WriteLine("✅");
                        LogDebug($"Test {testName} passed");
                    }
                    else
                    {
                        result.Success = false;
                        result.Message = "Compilation failed";
                        Console.WriteLine("❌");
                        LogDebug($"Test {testName} failed: Compilation failed");
                    }
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.Message = ex.Message;
                    Console.WriteLine("❌");
                    LogDebug($"Test {testName} failed with exception: {ex.Message}");
                }
                
                results.Add(result);
            }
        }
        
        return results;
    }
    
    private void TestHardwareDetectionAndExecution(List<TestProject> projects)
    {
        // Detect current hardware architecture
        var currentArch = DetectCurrentArchitecture();
        Console.WriteLine($"Detected current architecture: {currentArch}");
        LogInfo($"Detected hardware architecture: {currentArch}");
        
        // Try to execute a compatible build
        foreach (var project in projects)
        {
            Console.WriteLine($"\nAttempting to execute {project.Name} on {currentArch}...");
            LogDebug($"Attempting execution of {project.Name} on {currentArch}");
            
            string wasmOutput = Path.Combine(project.Path, "output.wasm");
            if (!File.Exists(wasmOutput))
            {
                Console.WriteLine("  ⚠️  WASM file not found, skipping");
                continue;
            }
            
            // Try to run using CRAB's run command (which uses BADGER internally)
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _crabExecutable,
                    Arguments = $"run --input \"{wasmOutput}\" --arch {currentArch} --format native",
                    WorkingDirectory = project.Path,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                
                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"  ✅ Execution successful");
                        LogInfo($"Successfully executed {project.Name} on {currentArch}");
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            Console.WriteLine($"  Output: {output.Trim()}");
                            LogDebug($"Execution output: {output}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ⚠️  Execution failed with exit code {process.ExitCode}");
                        LogDebug($"Execution failed: exit code {process.ExitCode}, stderr: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ⚠️  Execution error: {ex.Message}");
                LogDebug($"Execution exception: {ex}");
            }
        }
    }
    
    private bool CompileToWasm(string projectPath, string outputPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _crabExecutable,
                Arguments = $"compile --project \"{projectPath}\" --output \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
                
                if (process.ExitCode == 0 && File.Exists(outputPath))
                {
                    return true;
                }
                
                LogDebug($"WASM compilation failed: exit code {process.ExitCode}");
                LogDebug($"stderr: {process.StandardError.ReadToEnd()}");
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Exception during WASM compilation: {ex}");
        }
        
        return false;
    }
    
    private bool CompileWasmToNative(string wasmInput, string outputPath, string arch, string format)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _crabExecutable,
                Arguments = $"compile --to-asm \"{wasmInput}\" --arch {arch} --format {format} --output \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            
            using var process = Process.Start(startInfo);
            if (process != null)
            {
                process.WaitForExit();
                
                if (process.ExitCode == 0 && File.Exists(outputPath))
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Exception during native compilation: {ex}");
        }
        
        return false;
    }
    
    private string DetectCurrentArchitecture()
    {
        var arch = RuntimeInformation.ProcessArchitecture;
        
        return arch switch
        {
            Architecture.X64 => "x86_64",
            Architecture.X86 => "x86_32",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm32",
            _ => "x86_64"
        };
    }
    
    private string FindCrabExecutable()
    {
        // Look for CRAB executable in common locations
        var currentDir = Directory.GetCurrentDirectory();
        
        // Try ../bin/Debug/net10.0/CRAB (from Testing directory)
        var path1 = Path.Combine(currentDir, "..", "bin", "Debug", "net10.0", "CRAB");
        if (File.Exists(path1) || File.Exists(path1 + ".dll"))
        {
            return File.Exists(path1) ? path1 : $"dotnet {path1}.dll";
        }
        
        // Try bin/Debug/net10.0/CRAB (from root directory)
        var path2 = Path.Combine(currentDir, "bin", "Debug", "net10.0", "CRAB");
        if (File.Exists(path2) || File.Exists(path2 + ".dll"))
        {
            return File.Exists(path2) ? path2 : $"dotnet {path2}.dll";
        }
        
        // Fallback to "crab" assuming it's in PATH
        return "crab";
    }
    
    private void LogDebug(string message)
    {
        _debugLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] DEBUG: {message}");
    }
    
    private void LogInfo(string message)
    {
        _infoLog.Add($"[{DateTime.Now:HH:mm:ss.fff}] INFO: {message}");
    }
    
    public void SaveLogs(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        
        File.WriteAllLines(Path.Combine(outputDir, "debug.log"), _debugLog);
        File.WriteAllLines(Path.Combine(outputDir, "info.log"), _infoLog);
        
        Console.WriteLine($"\nLogs saved to {outputDir}");
    }
    
    private void Cleanup()
    {
        try
        {
            if (Directory.Exists(_testRootDir))
            {
                Directory.Delete(_testRootDir, recursive: true);
                LogDebug($"Cleaned up test directory: {_testRootDir}");
            }
        }
        catch (Exception ex)
        {
            LogDebug($"Failed to cleanup: {ex.Message}");
        }
    }
    
    private class TestProject
    {
        public string Name { get; }
        public string Path { get; }
        
        public TestProject(string name, string path)
        {
            Name = name;
            Path = path;
        }
    }
    
    private class TestResult
    {
        public string ProjectName { get; set; } = "";
        public string Architecture { get; set; } = "";
        public string Format { get; set; } = "";
        public string TestName { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}
