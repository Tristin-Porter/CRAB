// Test Runner - Executes all tests in the CRAB testing suite

using System;
using System.Diagnostics;
using System.Reflection;

namespace CRAB.Testing.Utilities
{
    public class TestRunner
    {
        private static int totalTests = 0;
        private static int passedTests = 0;
        private static int failedTests = 0;
        
        public static void Main(string[] args)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║         CRAB Compiler - Comprehensive Test Suite             ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            
            var stopwatch = Stopwatch.StartNew();
            
            // Run all test categories
            RunCategory("Unit Tests", RunUnitTests);
            Console.WriteLine();
            
            RunCategory("Integration Tests", RunIntegrationTests);
            Console.WriteLine();
            
            RunCategory("Feature Tests", RunFeatureTests);
            Console.WriteLine();
            
            stopwatch.Stop();
            
            // Print summary
            PrintSummary(stopwatch.ElapsedMilliseconds);
        }
        
        private static void RunCategory(string categoryName, Action testRunner)
        {
            Console.WriteLine($"═══ {categoryName} ═══");
            Console.WriteLine();
            
            testRunner();
        }
        
        private static void RunUnitTests()
        {
            RunTest("TokenSet - Keyword Tokenization", () => Unit.TokenSetTests.TestKeywordTokenization());
            RunTest("RuleSet - Grammar Rules", () => Unit.RuleSetTests.TestBasicGrammarRules());
            RunTest("MapSet - AST Transformation", () => Unit.MapSetTests.TestASTTransformation());
            RunTest("CTGC - Lifetime Analysis", () => Unit.CTGCTests.TestLifetimeAnalysis());
            RunTest("Manual Memory - Verification", () => Unit.ManualMemoryTests.TestManualMemoryVerification());
            RunTest("Project System - Solution Parsing", () => Unit.ProjectSystemTests.TestSolutionParsing());
            RunTest("Type System - Type Inference", () => Unit.TypeSystemTests.TestTypeInference());
            RunTest("Symbol Table - Symbol Resolution", () => Unit.SymbolTableTests.TestSymbolResolution());
            RunTest("Semantic Analysis - Type Checking", () => Unit.SemanticAnalysisTests.TestTypeChecking());
            RunTest("Code Generation - WASM Generation", () => Unit.CodeGenerationTests.TestWASMGeneration());
        }
        
        private static void RunIntegrationTests()
        {
            RunTest("Simple Compilation - Hello World", () => Integration.SimpleCompilationTests.TestHelloWorldCompilation());
            RunTest("Multi-Class Compilation", () => Integration.MultiClassTests.TestMultipleClasses());
            RunTest("Generic Types", () => Integration.GenericTests.TestGenericCompilation());
            RunTest("Interface Implementation", () => Integration.InterfaceTests.TestInterfaceCompilation());
            RunTest("Inheritance", () => Integration.InheritanceTests.TestInheritanceCompilation());
            RunTest("Build System", () => Integration.BuildSystemTests.TestProjectBuild());
            RunTest("Multi-File Projects", () => Integration.MultiFileTests.TestMultiFileProject());
            RunTest("WASM Output Validation", () => Integration.WASMOutputTests.TestWASMOutput());
            RunTest("Native Binary Generation", () => Integration.NativeBinaryTests.TestNativeGeneration());
            RunTest("Error Handling", () => Integration.ErrorHandlingTests.TestCompilationErrors());
        }
        
        private static void RunFeatureTests()
        {
            RunTest("Generics Feature", () => Features.GenericsFeatureTests.TestGenerics());
            RunTest("LINQ Feature", () => Features.LINQFeatureTests.TestLINQ());
            RunTest("Async/Await Feature", () => Features.AsyncFeatureTests.TestAsync());
            RunTest("Pattern Matching", () => Features.PatternMatchingTests.TestPatternMatching());
            RunTest("Nullable References", () => Features.NullableReferenceTests.TestNullableReferences());
            RunTest("Record Types", () => Features.RecordTests.TestRecords());
            RunTest("CTGC Feature", () => Features.CTGCFeatureTests.TestCTGC());
            RunTest("Manual Memory Feature", () => Features.ManualMemoryFeatureTests.TestManualBlocks());
            RunTest("Console I/O", () => Features.ConsoleIOTests.TestConsoleIO());
            RunTest("String Operations", () => Features.StringFeatureTests.TestStringOperations());
            RunTest("Exception Handling", () => Features.ExceptionHandlingTests.TestExceptions());
            RunTest("Delegates and Events", () => Features.DelegatesEventsTests.TestDelegatesAndEvents());
        }
        
        private static void RunTest(string testName, Action testMethod)
        {
            totalTests++;
            
            try
            {
                Console.WriteLine($"Running: {testName}");
                testMethod();
                passedTests++;
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                failedTests++;
                Console.WriteLine($"EXCEPTION: {ex.Message}");
                Console.WriteLine();
            }
        }
        
        private static void PrintSummary(long elapsedMs)
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("                         TEST SUMMARY                          ");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine();
            Console.WriteLine($"  Total Tests:    {totalTests}");
            Console.WriteLine($"  Passed:         {passedTests}  ✅");
            Console.WriteLine($"  Failed:         {failedTests}  {(failedTests > 0 ? "❌" : "")}");
            Console.WriteLine($"  Success Rate:   {(passedTests * 100 / totalTests)}%");
            Console.WriteLine($"  Execution Time: {elapsedMs}ms");
            Console.WriteLine();
            
            if (failedTests == 0)
            {
                Console.WriteLine("  ✅ ALL TESTS PASSED! ✅");
            }
            else
            {
                Console.WriteLine($"  ⚠️  {failedTests} TEST(S) FAILED");
            }
            
            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
        }
    }
}
