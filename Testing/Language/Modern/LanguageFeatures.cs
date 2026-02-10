// Test C# language features compilation
// Generics, delegates, LINQ, async/await

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LanguageFeatures
{
    // Generic class
    class Container<T>
    {
        private T value;
        
        public Container(T initialValue)
        {
            value = initialValue;
        }
        
        public T GetValue() => value;
        public void SetValue(T newValue) => value = newValue;
    }
    
    // Delegates and lambdas
    class DelegateTest
    {
        delegate int Operation(int x, int y);
        
        static void TestDelegates()
        {
            Operation add = (x, y) => x + y;
            Operation multiply = (x, y) => x * y;
            
            int result1 = add(5, 3);
            int result2 = multiply(5, 3);
        }
    }
    
    // LINQ
    class LinqTest
    {
        static void TestLinq()
        {
            var numbers = new List<int> { 1, 2, 3, 4, 5 };
            
            var evens = numbers.Where(n => n % 2 == 0).ToList();
            var doubled = numbers.Select(n => n * 2).ToList();
            var sum = numbers.Sum();
        }
    }
    
    // Async/await
    class AsyncTest
    {
        static async Task<int> ComputeAsync(int value)
        {
            await Task.Delay(100);
            return value * 2;
        }
        
        static async Task TestAsync()
        {
            int result = await ComputeAsync(21);
            Console.WriteLine($"Result: {result}");
        }
    }
    
    // Pattern matching
    class PatternTest
    {
        static string Describe(object obj)
        {
            return obj switch
            {
                int i => $"Integer: {i}",
                string s => $"String: {s}",
                null => "Null",
                _ => "Unknown"
            };
        }
    }
}
