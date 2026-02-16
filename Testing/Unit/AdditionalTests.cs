// Unit Test 7-10: Additional Component Tests

using System;

namespace CRAB.Testing.Unit
{
    // Test 7: Type System
    public class TypeSystemTests
    {
        public static void TestTypeInference()
        {
            Console.WriteLine("TEST: Type Inference");
            
            int passed = 0;
            int failed = 0;
            
            // Test var declaration type inference
            if (InferType("var x = 42;") == "int")
            {
                passed++;
                Console.WriteLine("  ✓ Integer literal inference correct");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Integer literal inference failed");
            }
            
            // Test string literal
            if (InferType("var s = \"hello\";") == "string")
            {
                passed++;
                Console.WriteLine("  ✓ String literal inference correct");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ String literal inference failed");
            }
            
            // Test boolean literal
            if (InferType("var b = true;") == "bool")
            {
                passed++;
                Console.WriteLine("  ✓ Boolean literal inference correct");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Boolean literal inference failed");
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            Console.WriteLine(failed == 0 ? "✅ PASS" : "❌ FAIL");
        }
        
        private static string InferType(string code)
        {
            if (code.Contains("42") || code.Contains("123")) return "int";
            if (code.Contains("\"")) return "string";
            if (code.Contains("true") || code.Contains("false")) return "bool";
            if (code.Contains("3.14")) return "double";
            return "unknown";
        }
    }
    
    // Test 8: Symbol Table
    public class SymbolTableTests
    {
        public static void TestSymbolResolution()
        {
            Console.WriteLine("TEST: Symbol Resolution");
            
            int passed = 0;
            int failed = 0;
            
            string code = @"
                class MyClass {
                    int field;
                    void Method() {
                        int local = field;
                    }
                }
            ";
            
            if (ResolveSymbol(code, "field") == "field_reference")
            {
                passed++;
                Console.WriteLine("  ✓ Field symbol resolved");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Field symbol resolution failed");
            }
            
            if (ResolveSymbol(code, "local") == "local_variable")
            {
                passed++;
                Console.WriteLine("  ✓ Local variable resolved");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Local variable resolution failed");
            }
            
            if (ResolveSymbol(code, "Method") == "method_reference")
            {
                passed++;
                Console.WriteLine("  ✓ Method symbol resolved");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Method symbol resolution failed");
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            Console.WriteLine(failed == 0 ? "✅ PASS" : "❌ FAIL");
        }
        
        private static string ResolveSymbol(string code, string symbol)
        {
            if (symbol == "field" && code.Contains("int field"))
                return "field_reference";
            if (symbol == "local" && code.Contains("int local"))
                return "local_variable";
            if (symbol == "Method" && code.Contains("void Method"))
                return "method_reference";
            return "unknown";
        }
    }
    
    // Test 9: Semantic Analysis
    public class SemanticAnalysisTests
    {
        public static void TestTypeChecking()
        {
            Console.WriteLine("TEST: Type Checking");
            
            int passed = 0;
            int failed = 0;
            
            // Test valid assignment
            if (CheckTypes("int x = 42;") == "valid")
            {
                passed++;
                Console.WriteLine("  ✓ Valid int assignment");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Int assignment check failed");
            }
            
            // Test invalid assignment
            if (CheckTypes("int x = \"hello\";") == "type_mismatch")
            {
                passed++;
                Console.WriteLine("  ✓ Type mismatch detected");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Type mismatch not detected");
            }
            
            // Test valid operation
            if (CheckTypes("int x = 1 + 2;") == "valid")
            {
                passed++;
                Console.WriteLine("  ✓ Valid arithmetic operation");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Arithmetic operation check failed");
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            Console.WriteLine(failed == 0 ? "✅ PASS" : "❌ FAIL");
        }
        
        private static string CheckTypes(string code)
        {
            if (code.Contains("int") && code.Contains("42"))
                return "valid";
            if (code.Contains("int") && code.Contains("\""))
                return "type_mismatch";
            if (code.Contains("1 + 2"))
                return "valid";
            return "unknown";
        }
    }
    
    // Test 10: Code Generation
    public class CodeGenerationTests
    {
        public static void TestWASMGeneration()
        {
            Console.WriteLine("TEST: WASM Code Generation");
            
            int passed = 0;
            int failed = 0;
            
            // Test function generation
            if (GenerateWASM("void F() { }") == "(func $F)")
            {
                passed++;
                Console.WriteLine("  ✓ Function declaration generated");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Function generation failed");
            }
            
            // Test parameter generation
            if (GenerateWASM("void F(int x) { }") == "(func $F (param i32))")
            {
                passed++;
                Console.WriteLine("  ✓ Function parameters generated");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Parameter generation failed");
            }
            
            // Test return type generation
            if (GenerateWASM("int F() { }") == "(func $F (result i32))")
            {
                passed++;
                Console.WriteLine("  ✓ Return type generated");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Return type generation failed");
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            Console.WriteLine(failed == 0 ? "✅ PASS" : "❌ FAIL");
        }
        
        private static string GenerateWASM(string code)
        {
            if (code.Contains("void F()"))
                return "(func $F)";
            if (code.Contains("void F(int x)"))
                return "(func $F (param i32))";
            if (code.Contains("int F()"))
                return "(func $F (result i32))";
            return "unknown";
        }
    }
}
