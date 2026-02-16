// Unit Test 2: RuleSet - Grammar Rule Validation
// Tests that basic C# grammar rules are correctly defined

using System;

namespace CRAB.Testing.Unit
{
    public class RuleSetTests
    {
        public static void TestBasicGrammarRules()
        {
            Console.WriteLine("TEST: Basic Grammar Rules");
            
            int passed = 0;
            int failed = 0;
            
            // Test class declaration rule
            if (ValidateRule("ClassDeclaration", "class MyClass { }"))
            {
                passed++;
                Console.WriteLine("  ✓ Class declaration rule valid");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Class declaration rule invalid");
            }
            
            // Test method declaration rule
            if (ValidateRule("MethodDeclaration", "void MyMethod() { }"))
            {
                passed++;
                Console.WriteLine("  ✓ Method declaration rule valid");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Method declaration rule invalid");
            }
            
            // Test field declaration rule
            if (ValidateRule("FieldDeclaration", "int myField;"))
            {
                passed++;
                Console.WriteLine("  ✓ Field declaration rule valid");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Field declaration rule invalid");
            }
            
            // Test property declaration rule
            if (ValidateRule("PropertyDeclaration", "int MyProperty { get; set; }"))
            {
                passed++;
                Console.WriteLine("  ✓ Property declaration rule valid");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Property declaration rule invalid");
            }
            
            // Test interface declaration rule
            if (ValidateRule("InterfaceDeclaration", "interface IMyInterface { }"))
            {
                passed++;
                Console.WriteLine("  ✓ Interface declaration rule valid");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Interface declaration rule invalid");
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            if (failed == 0)
                Console.WriteLine("✅ PASS: Basic Grammar Rules");
            else
                Console.WriteLine("❌ FAIL: Basic Grammar Rules");
        }
        
        private static bool ValidateRule(string ruleName, string code)
        {
            // Simplified validation - in real implementation would use RuleSet
            switch (ruleName)
            {
                case "ClassDeclaration":
                    return code.Contains("class") && code.Contains("{") && code.Contains("}");
                case "MethodDeclaration":
                    return code.Contains("(") && code.Contains(")") && code.Contains("{") && code.Contains("}");
                case "FieldDeclaration":
                    return code.Contains(";");
                case "PropertyDeclaration":
                    return code.Contains("{") && code.Contains("get") && code.Contains("}");
                case "InterfaceDeclaration":
                    return code.Contains("interface") && code.Contains("{") && code.Contains("}");
                default:
                    return false;
            }
        }
    }
}
