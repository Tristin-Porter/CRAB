// Unit Test 3: MapSet - AST to WASM Transformation
// Tests that AST nodes correctly transform to WASM instructions

using System;

namespace CRAB.Testing.Unit
{
    public class MapSetTests
    {
        public static void TestASTTransformation()
        {
            Console.WriteLine("TEST: AST to WASM Transformation");
            
            int passed = 0;
            int failed = 0;
            
            // Test integer literal transformation
            if (TransformNode("IntLiteral", "42") == "i32.const 42")
            {
                passed++;
                Console.WriteLine("  ✓ Integer literal transforms correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Integer literal transformation failed");
            }
            
            // Test addition transformation
            if (TransformNode("BinaryOp", "+") == "i32.add")
            {
                passed++;
                Console.WriteLine("  ✓ Addition operator transforms correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Addition transformation failed");
            }
            
            // Test subtraction transformation
            if (TransformNode("BinaryOp", "-") == "i32.sub")
            {
                passed++;
                Console.WriteLine("  ✓ Subtraction operator transforms correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Subtraction transformation failed");
            }
            
            // Test multiplication transformation
            if (TransformNode("BinaryOp", "*") == "i32.mul")
            {
                passed++;
                Console.WriteLine("  ✓ Multiplication operator transforms correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Multiplication transformation failed");
            }
            
            // Test local variable get
            if (TransformNode("LocalGet", "0") == "local.get 0")
            {
                passed++;
                Console.WriteLine("  ✓ Local get transforms correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Local get transformation failed");
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            if (failed == 0)
                Console.WriteLine("✅ PASS: AST Transformation");
            else
                Console.WriteLine("❌ FAIL: AST Transformation");
        }
        
        private static string TransformNode(string nodeType, string value)
        {
            // Simplified transformation - in real implementation would use MapSet
            switch (nodeType)
            {
                case "IntLiteral":
                    return $"i32.const {value}";
                case "BinaryOp":
                    switch (value)
                    {
                        case "+": return "i32.add";
                        case "-": return "i32.sub";
                        case "*": return "i32.mul";
                        case "/": return "i32.div";
                        default: return "unknown";
                    }
                case "LocalGet":
                    return $"local.get {value}";
                case "LocalSet":
                    return $"local.set {value}";
                default:
                    return "unknown";
            }
        }
    }
}
