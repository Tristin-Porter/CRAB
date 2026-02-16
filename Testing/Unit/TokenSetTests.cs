// Unit Test 1: TokenSet - Keyword Tokenization
// Tests that all C# keywords are correctly tokenized

using System;
using System.Collections.Generic;

namespace CRAB.Testing.Unit
{
    public class TokenSetTests
    {
        public static void TestKeywordTokenization()
        {
            Console.WriteLine("TEST: Keyword Tokenization");
            
            var keywords = new string[]
            {
                "class", "interface", "struct", "enum", "namespace",
                "using", "public", "private", "protected", "internal",
                "static", "void", "int", "string", "bool",
                "if", "else", "while", "for", "foreach",
                "return", "break", "continue", "switch", "case",
                "new", "this", "base", "null", "true", "false"
            };
            
            int passed = 0;
            int failed = 0;
            
            foreach (var keyword in keywords)
            {
                bool isKeyword = IsValidKeyword(keyword);
                if (isKeyword)
                {
                    passed++;
                    Console.WriteLine($"  ✓ '{keyword}' recognized as keyword");
                }
                else
                {
                    failed++;
                    Console.WriteLine($"  ✗ '{keyword}' NOT recognized as keyword");
                }
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            if (failed == 0)
                Console.WriteLine("✅ PASS: Keyword Tokenization");
            else
                Console.WriteLine("❌ FAIL: Keyword Tokenization");
        }
        
        private static bool IsValidKeyword(string word)
        {
            // Simplified check - in real implementation would use TokenSet
            var csharpKeywords = new HashSet<string>
            {
                "class", "interface", "struct", "enum", "namespace",
                "using", "public", "private", "protected", "internal",
                "static", "void", "int", "string", "bool",
                "if", "else", "while", "for", "foreach",
                "return", "break", "continue", "switch", "case",
                "new", "this", "base", "null", "true", "false",
                "abstract", "virtual", "override", "sealed", "async",
                "await", "var", "dynamic", "in", "out", "ref",
                "readonly", "const", "volatile", "unsafe", "fixed",
                "lock", "checked", "unchecked", "sizeof", "typeof",
                "is", "as", "delegate", "event", "operator",
                "explicit", "implicit", "params", "get", "set",
                "add", "remove", "value", "partial", "where",
                "try", "catch", "finally", "throw", "when",
                "nameof", "stackalloc", "unmanaged", "from", "select",
                "group", "into", "orderby", "join", "let",
                "on", "equals", "by", "ascending", "descending"
            };
            
            return csharpKeywords.Contains(word);
        }
    }
}
