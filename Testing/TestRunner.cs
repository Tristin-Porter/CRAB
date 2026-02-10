// CRAB Test Runner Infrastructure
// Provides utilities for running and validating CRAB compiler tests

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CRAB.Testing
{
    /// <summary>
    /// Test result status
    /// </summary>
    public enum TestStatus
    {
        Passed,
        Failed,
        Skipped,
        Error
    }

    /// <summary>
    /// Result of a single test execution
    /// </summary>
    public class TestResult
    {
        public string TestName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public TestStatus Status { get; set; }
        public string? Message { get; set; }
        public TimeSpan Duration { get; set; }
        public Exception? Exception { get; set; }

        public bool Success => Status == TestStatus.Passed;
    }

    /// <summary>
    /// Collection of test results
    /// </summary>
    public class TestResults
    {
        public List<TestResult> Results { get; } = new List<TestResult>();
        
        public int Total => Results.Count;
        public int Passed => Results.Count(r => r.Status == TestStatus.Passed);
        public int Failed => Results.Count(r => r.Status == TestStatus.Failed);
        public int Skipped => Results.Count(r => r.Status == TestStatus.Skipped);
        public int Errors => Results.Count(r => r.Status == TestStatus.Error);
        
        public TimeSpan TotalDuration => TimeSpan.FromTicks(Results.Sum(r => r.Duration.Ticks));

        public void Print()
        {
            Console.WriteLine("\n" + new string('=', 60));
            Console.WriteLine("CRAB Test Results");
            Console.WriteLine(new string('=', 60));
            
            foreach (var result in Results)
            {
                var statusSymbol = result.Status switch
                {
                    TestStatus.Passed => "✓",
                    TestStatus.Failed => "✗",
                    TestStatus.Skipped => "○",
                    TestStatus.Error => "⚠",
                    _ => "?"
                };
                
                Console.WriteLine($"{statusSymbol} [{result.Category}] {result.TestName} ({result.Duration.TotalMilliseconds:F2}ms)");
                
                if (!string.IsNullOrEmpty(result.Message))
                {
                    Console.WriteLine($"  {result.Message}");
                }
                
                if (result.Exception != null)
                {
                    Console.WriteLine($"  Exception: {result.Exception.Message}");
                }
            }
            
            Console.WriteLine(new string('-', 60));
            Console.WriteLine($"Total: {Total} | Passed: {Passed} | Failed: {Failed} | Skipped: {Skipped} | Errors: {Errors}");
            Console.WriteLine($"Duration: {TotalDuration.TotalSeconds:F2}s");
            Console.WriteLine(new string('=', 60));
        }
    }

    /// <summary>
    /// Base class for all CRAB tests
    /// </summary>
    public abstract class CRABTest
    {
        public abstract string TestName { get; }
        public abstract string Category { get; }
        
        public virtual bool Skip => false;
        public virtual string? SkipReason => null;
        
        public abstract TestResult Run();
        
        protected TestResult Success(string? message = null, TimeSpan? duration = null)
        {
            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Status = TestStatus.Passed,
                Message = message,
                Duration = duration ?? TimeSpan.Zero
            };
        }
        
        protected TestResult Failure(string message, TimeSpan? duration = null)
        {
            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Status = TestStatus.Failed,
                Message = message,
                Duration = duration ?? TimeSpan.Zero
            };
        }
        
        protected TestResult Skipped(string reason)
        {
            return new TestResult
            {
                TestName = TestName,
                Category = Category,
                Status = TestStatus.Skipped,
                Message = reason
            };
        }
    }

    /// <summary>
    /// Main test runner for CRAB
    /// </summary>
    public class TestRunner
    {
        private readonly List<CRABTest> tests = new List<CRABTest>();
        
        public void RegisterTest(CRABTest test)
        {
            tests.Add(test);
        }
        
        public TestResults RunAll()
        {
            var results = new TestResults();
            
            foreach (var test in tests)
            {
                if (test.Skip)
                {
                    results.Results.Add(test.Skipped(test.SkipReason ?? "Test skipped"));
                    continue;
                }
                
                try
                {
                    var result = test.Run();
                    results.Results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Results.Add(new TestResult
                    {
                        TestName = test.TestName,
                        Category = test.Category,
                        Status = TestStatus.Error,
                        Message = "Unhandled exception during test execution",
                        Exception = ex
                    });
                }
            }
            
            return results;
        }
        
        public TestResults RunCategory(string category)
        {
            var results = new TestResults();
            var categoryTests = tests.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            
            foreach (var test in categoryTests)
            {
                if (test.Skip)
                {
                    results.Results.Add(test.Skipped(test.SkipReason ?? "Test skipped"));
                    continue;
                }
                
                try
                {
                    var result = test.Run();
                    results.Results.Add(result);
                }
                catch (Exception ex)
                {
                    results.Results.Add(new TestResult
                    {
                        TestName = test.TestName,
                        Category = test.Category,
                        Status = TestStatus.Error,
                        Message = "Unhandled exception during test execution",
                        Exception = ex
                    });
                }
            }
            
            return results;
        }
    }

    /// <summary>
    /// Assertion utilities for tests
    /// </summary>
    public static class Assert
    {
        public static void IsTrue(bool condition, string? message = null)
        {
            if (!condition)
                throw new AssertionException(message ?? "Expected true but was false");
        }
        
        public static void IsFalse(bool condition, string? message = null)
        {
            if (condition)
                throw new AssertionException(message ?? "Expected false but was true");
        }
        
        public static void AreEqual<T>(T expected, T actual, string? message = null)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new AssertionException(message ?? $"Expected {expected} but was {actual}");
        }
        
        public static void AreNotEqual<T>(T notExpected, T actual, string? message = null)
        {
            if (EqualityComparer<T>.Default.Equals(notExpected, actual))
                throw new AssertionException(message ?? $"Expected not {notExpected} but was {actual}");
        }
        
        public static void IsNull(object? value, string? message = null)
        {
            if (value != null)
                throw new AssertionException(message ?? $"Expected null but was {value}");
        }
        
        public static void IsNotNull(object? value, string? message = null)
        {
            if (value == null)
                throw new AssertionException(message ?? "Expected non-null value but was null");
        }
        
        public static void Throws<TException>(Action action, string? message = null) where TException : Exception
        {
            try
            {
                action();
                throw new AssertionException(message ?? $"Expected exception {typeof(TException).Name} but none was thrown");
            }
            catch (TException)
            {
                // Expected
            }
            catch (Exception ex)
            {
                throw new AssertionException(message ?? $"Expected exception {typeof(TException).Name} but got {ex.GetType().Name}");
            }
        }
        
        public static void Contains(string text, string substring, string? message = null)
        {
            if (!text.Contains(substring))
                throw new AssertionException(message ?? $"Expected text to contain '{substring}' but it didn't");
        }
    }

    /// <summary>
    /// Exception thrown when an assertion fails
    /// </summary>
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}
