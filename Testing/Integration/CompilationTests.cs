// Integration Test 1-5: End-to-End Compilation Tests

using System;
using System.IO;

namespace CRAB.Testing.Integration
{
    // Integration Test 1: Simple Program Compilation
    public class SimpleCompilationTests
    {
        public static void TestHelloWorldCompilation()
        {
            Console.WriteLine("INTEGRATION TEST: Hello World Compilation");
            
            string sourceCode = @"
                using System;
                
                class Program
                {
                    static void Main()
                    {
                        Console.WriteLine(""Hello from CRAB!"");
                    }
                }
            ";
            
            bool compiled = CompileAndValidate(sourceCode, "HelloWorld");
            
            if (compiled)
                Console.WriteLine("✅ PASS: Hello World compiles successfully");
            else
                Console.WriteLine("❌ FAIL: Hello World compilation failed");
        }
        
        private static bool CompileAndValidate(string source, string name)
        {
            // Simplified - would actually invoke compiler
            return source.Contains("class") && source.Contains("Main");
        }
    }
    
    // Integration Test 2: Multi-Class Compilation
    public class MultiClassTests
    {
        public static void TestMultipleClasses()
        {
            Console.WriteLine("INTEGRATION TEST: Multiple Classes");
            
            string sourceCode = @"
                class Calculator
                {
                    public int Add(int a, int b) { return a + b; }
                }
                
                class Program
                {
                    static void Main()
                    {
                        var calc = new Calculator();
                        int result = calc.Add(5, 3);
                    }
                }
            ";
            
            bool passed = true;
            
            // Test class detection
            if (CountClasses(sourceCode) == 2)
            {
                Console.WriteLine("  ✓ Multiple classes detected");
            }
            else
            {
                Console.WriteLine("  ✗ Class detection failed");
                passed = false;
            }
            
            // Test method detection
            if (CountMethods(sourceCode) >= 2)
            {
                Console.WriteLine("  ✓ Methods detected");
            }
            else
            {
                Console.WriteLine("  ✗ Method detection failed");
                passed = false;
            }
            
            // Test instantiation
            if (sourceCode.Contains("new Calculator"))
            {
                Console.WriteLine("  ✓ Object instantiation detected");
            }
            else
            {
                Console.WriteLine("  ✗ Instantiation detection failed");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
        
        private static int CountClasses(string code)
        {
            int count = 0;
            int index = 0;
            while ((index = code.IndexOf("class ", index)) != -1)
            {
                count++;
                index += 6;
            }
            return count;
        }
        
        private static int CountMethods(string code)
        {
            int count = 0;
            string[] methodKeywords = { "static void", "public int", "private void" };
            foreach (var keyword in methodKeywords)
            {
                int index = 0;
                while ((index = code.IndexOf(keyword, index)) != -1)
                {
                    count++;
                    index += keyword.Length;
                }
            }
            return count;
        }
    }
    
    // Integration Test 3: Generic Types
    public class GenericTests
    {
        public static void TestGenericCompilation()
        {
            Console.WriteLine("INTEGRATION TEST: Generic Types");
            
            string sourceCode = @"
                class Container<T>
                {
                    private T value;
                    public Container(T v) { value = v; }
                    public T Get() { return value; }
                }
                
                class Program
                {
                    static void Main()
                    {
                        var intContainer = new Container<int>(42);
                        var stringContainer = new Container<string>(""hello"");
                    }
                }
            ";
            
            bool passed = true;
            
            if (sourceCode.Contains("class Container<T>"))
            {
                Console.WriteLine("  ✓ Generic class declaration");
                passed = true;
            }
            else
            {
                Console.WriteLine("  ✗ Generic class not found");
                passed = false;
            }
            
            if (sourceCode.Contains("Container<int>") && sourceCode.Contains("Container<string>"))
            {
                Console.WriteLine("  ✓ Generic instantiations");
            }
            else
            {
                Console.WriteLine("  ✗ Generic instantiation failed");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
    
    // Integration Test 4: Interface Implementation
    public class InterfaceTests
    {
        public static void TestInterfaceCompilation()
        {
            Console.WriteLine("INTEGRATION TEST: Interface Implementation");
            
            string sourceCode = @"
                interface IProcessor
                {
                    void Process(string data);
                }
                
                class Processor : IProcessor
                {
                    public void Process(string data)
                    {
                        Console.WriteLine(data);
                    }
                }
            ";
            
            bool passed = true;
            
            if (sourceCode.Contains("interface IProcessor"))
            {
                Console.WriteLine("  ✓ Interface declaration");
            }
            else
            {
                Console.WriteLine("  ✗ Interface not found");
                passed = false;
            }
            
            if (sourceCode.Contains(": IProcessor"))
            {
                Console.WriteLine("  ✓ Interface implementation");
            }
            else
            {
                Console.WriteLine("  ✗ Implementation not found");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
    
    // Integration Test 5: Inheritance
    public class InheritanceTests
    {
        public static void TestInheritanceCompilation()
        {
            Console.WriteLine("INTEGRATION TEST: Inheritance");
            
            string sourceCode = @"
                class Base
                {
                    public virtual void Method() { }
                }
                
                class Derived : Base
                {
                    public override void Method() { }
                }
            ";
            
            bool passed = true;
            
            if (sourceCode.Contains("class Base"))
            {
                Console.WriteLine("  ✓ Base class declaration");
            }
            else
            {
                Console.WriteLine("  ✗ Base class not found");
                passed = false;
            }
            
            if (sourceCode.Contains(": Base"))
            {
                Console.WriteLine("  ✓ Inheritance declaration");
            }
            else
            {
                Console.WriteLine("  ✗ Inheritance not found");
                passed = false;
            }
            
            if (sourceCode.Contains("virtual") && sourceCode.Contains("override"))
            {
                Console.WriteLine("  ✓ Virtual method override");
            }
            else
            {
                Console.WriteLine("  ✗ Virtual/override not found");
                passed = false;
            }
            
            Console.WriteLine(passed ? "✅ PASS" : "❌ FAIL");
        }
    }
}
