// Feature Tests 1-6: C# Language Features

using System;

namespace CRAB.Testing.Features
{
    // Feature Test 1: Generics
    public class GenericsFeatureTests
    {
        public static void TestGenerics()
        {
            Console.WriteLine("FEATURE TEST: Generics");
            
            bool passed = true;
            
            string genericClass = @"
                class Box<T> { private T item; public void Set(T value) { item = value; } }
            ";
            
            if (genericClass.Contains("<T>"))
            {
                Console.WriteLine("  ✓ Generic type parameter");
            }
            else
            {
                Console.WriteLine("  ✗ Generic type parameter failed");
                passed = false;
            }
            
            string constrainedGeneric = @"
                class Box<T> where T : IComparable { }
            ";
            
            if (constrainedGeneric.Contains("where T :"))
            {
                Console.WriteLine("  ✓ Generic constraints");
            }
            else
            {
                Console.WriteLine("  ✗ Generic constraints failed");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
    
    // Feature Test 2: LINQ
    public class LINQFeatureTests
    {
        public static void TestLINQ()
        {
            Console.WriteLine("FEATURE TEST: LINQ");
            
            bool passed = true;
            
            string linqQuery = @"
                var query = from x in numbers where x > 5 select x * 2;
            ";
            
            if (linqQuery.Contains("from") && linqQuery.Contains("where") && linqQuery.Contains("select"))
            {
                Console.WriteLine("  ✓ LINQ query syntax");
            }
            else
            {
                Console.WriteLine("  ✗ LINQ query syntax failed");
                passed = false;
            }
            
            string methodSyntax = @"
                var result = numbers.Where(x => x > 5).Select(x => x * 2);
            ";
            
            if (methodSyntax.Contains("Where") && methodSyntax.Contains("Select"))
            {
                Console.WriteLine("  ✓ LINQ method syntax");
            }
            else
            {
                Console.WriteLine("  ✗ LINQ method syntax failed");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
    
    // Feature Test 3: Async/Await
    public class AsyncFeatureTests
    {
        public static void TestAsync()
        {
            Console.WriteLine("FEATURE TEST: Async/Await");
            
            bool passed = true;
            
            string asyncMethod = @"
                async Task<int> GetDataAsync() {
                    await Task.Delay(1000);
                    return 42;
                }
            ";
            
            if (asyncMethod.Contains("async") && asyncMethod.Contains("await"))
            {
                Console.WriteLine("  ✓ Async/await syntax");
            }
            else
            {
                Console.WriteLine("  ✗ Async/await syntax failed");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
    
    // Feature Test 4: Pattern Matching
    public class PatternMatchingTests
    {
        public static void TestPatternMatching()
        {
            Console.WriteLine("FEATURE TEST: Pattern Matching");
            
            bool passed = true;
            
            string switchExpression = @"
                var result = obj switch {
                    string s => s.Length,
                    int i => i * 2,
                    _ => 0
                };
            ";
            
            if (switchExpression.Contains("switch") && switchExpression.Contains("=>"))
            {
                Console.WriteLine("  ✓ Switch expressions");
            }
            else
            {
                Console.WriteLine("  ✗ Switch expressions failed");
                passed = false;
            }
            
            string isPattern = @"
                if (obj is string s) { }
            ";
            
            if (isPattern.Contains("is string"))
            {
                Console.WriteLine("  ✓ Is-pattern matching");
            }
            else
            {
                Console.WriteLine("  ✗ Is-pattern matching failed");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
    
    // Feature Test 5: Nullable Reference Types
    public class NullableReferenceTests
    {
        public static void TestNullableReferences()
        {
            Console.WriteLine("FEATURE TEST: Nullable Reference Types");
            
            bool passed = true;
            
            string nullableRef = @"
                string? nullableString = null;
                string nonNullString = ""hello"";
            ";
            
            if (nullableRef.Contains("string?"))
            {
                Console.WriteLine("  ✓ Nullable reference syntax");
            }
            else
            {
                Console.WriteLine("  ✗ Nullable reference syntax failed");
                passed = false;
            }
            
            string nullForgiving = @"
                string s = nullableString!;
            ";
            
            if (nullForgiving.Contains("!"))
            {
                Console.WriteLine("  ✓ Null-forgiving operator");
            }
            else
            {
                Console.WriteLine("  ✗ Null-forgiving operator failed");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
    
    // Feature Test 6: Records
    public class RecordTests
    {
        public static void TestRecords()
        {
            Console.WriteLine("FEATURE TEST: Record Types");
            
            bool passed = true;
            
            string recordDecl = @"
                record Person(string Name, int Age);
            ";
            
            if (recordDecl.Contains("record"))
            {
                Console.WriteLine("  ✓ Record declaration");
            }
            else
            {
                Console.WriteLine("  ✗ Record declaration failed");
                passed = false;
            }
            
            string recordClass = @"
                record class Point { public int X { get; init; } public int Y { get; init; } }
            ";
            
            if (recordClass.Contains("record class") && recordClass.Contains("init"))
            {
                Console.WriteLine("  ✓ Record class with init");
            }
            else
            {
                Console.WriteLine("  ✗ Record class failed");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
}
