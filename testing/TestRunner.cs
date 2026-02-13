using System;

namespace CRAB.Tests;

/// <summary>
/// Main test runner for CRAB compiler test suite.
/// Runs all test categories and reports results.
/// </summary>
public class TestRunner
{
    public static void Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              CRAB Compiler Test Suite                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        int passedTests = 0;
        int failedTests = 0;
        
        // Run all test suites
        var testSuites = new Action[]
        {
            () => new TokenTests().RunAll(),
            () => new ParserTests().RunAll(),
            () => new CTGCTests().RunAll(),
            () => new ManualMemoryTests().RunAll(),
            () => new WASMGenerationTests().RunAll(),
            () => new IntegrationTests().RunAll(),
        };
        
        foreach (var testSuite in testSuites)
        {
            try
            {
                testSuite();
                passedTests++;
            }
            catch (Exception ex)
            {
                failedTests++;
                Console.WriteLine($"\n✗ Test suite failed: {ex.Message}");
                Console.WriteLine($"  Stack trace: {ex.StackTrace}");
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
}
