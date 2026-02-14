using System;
using System.IO;

namespace CRAB.Tests;

/// <summary>
/// Main test runner for CRAB compiler test suite.
/// Runs all test categories and reports results.
/// Supports detailed logging and test output saving.
/// </summary>
public class TestRunner
{
    public static void Main(string[] args)
    {
        // Parse command line arguments
        bool saveOutputs = Array.Exists(args, arg => arg == "--save");
        bool verbose = Array.Exists(args, arg => arg == "--verbose");
        bool debugMode = Array.Exists(args, arg => arg == "--debug");
        
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              CRAB Compiler Test Suite                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        if (debugMode)
            Console.WriteLine("[Debug mode enabled - detailed output will be shown]\n");
        
        int passedTests = 0;
        int failedTests = 0;
        
        string? logDir = null;
        if (saveOutputs)
        {
            logDir = Path.Combine(Directory.GetCurrentDirectory(), "tests");
            Directory.CreateDirectory(logDir);
            Console.WriteLine($"Test outputs will be saved to: {logDir}\n");
        }
        
        // Run all test suites
        var testSuites = new (string Name, Action Action)[]
        {
            ("Token/Lexer Tests", () => new TokenTests().RunAll()),
            ("Parser/Grammar Tests", () => new ParserTests().RunAll()),
            ("CTGC Automatic Memory Tests", () => new CTGCTests().RunAll()),
            ("Manual Memory Verification Tests", () => new ManualMemoryTests().RunAll()),
            ("WASM Code Generation Tests", () => new WASMGenerationTests().RunAll()),
            ("End-to-End Integration Tests", () => new IntegrationTests().RunAll()),
            ("Comprehensive Tests", () => RunComprehensiveTests(logDir)),
        };
        
        foreach (var (name, action) in testSuites)
        {
            try
            {
                if (verbose || debugMode)
                    Console.WriteLine($"\n[Starting: {name}]");
                    
                action();
                passedTests++;
                
                if (verbose || debugMode)
                    Console.WriteLine($"[Completed: {name}]");
            }
            catch (Exception ex)
            {
                failedTests++;
                Console.WriteLine($"\n✗ Test suite failed: {name}");
                Console.WriteLine($"  Error: {ex.Message}");
                
                if (debugMode)
                {
                    Console.WriteLine($"  Stack trace:");
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        
        // Print summary
        Console.WriteLine();
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      Test Summary                          ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine($"  Total test suites: {passedTests + failedTests}");
        Console.WriteLine($"  Passed: {passedTests}");
        Console.WriteLine($"  Failed: {failedTests}");
        Console.WriteLine();
        
        if (saveOutputs && logDir != null)
        {
            Console.WriteLine($"  Test outputs saved to: {logDir}");
            Console.WriteLine();
        }
        
        if (failedTests > 0)
        {
            Console.WriteLine("❌ Some tests failed!");
            Environment.Exit(1);
        }
        else
        {
            Console.WriteLine("✅ All tests passed!");
            Environment.Exit(0);
        }
    }
    
    private static void RunComprehensiveTests(string? logDir)
    {
        var comprehensiveTests = new ComprehensiveTests();
        comprehensiveTests.RunAll();
        
        // Save logs if requested
        if (logDir != null)
        {
            var comprehensiveLogDir = Path.Combine(logDir, "comprehensive");
            comprehensiveTests.SaveLogs(comprehensiveLogDir);
        }
    }
}
