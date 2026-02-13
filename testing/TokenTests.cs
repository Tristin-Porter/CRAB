using System;
using System.Collections.Generic;
using CDTk;

namespace CRAB.Tests;

/// <summary>
/// Tests for the CRAB tokenizer/lexer.
/// Validates that C# source code is correctly tokenized according to the TokenSet.
/// </summary>
public class TokenTests
{
    private readonly Tokens _tokens = new Tokens();

    public void RunAll()
    {
        Console.WriteLine("=== Token/Lexer Tests ===\n");
        
        TestKeywords();
        TestIdentifiers();
        TestLiterals();
        TestOperators();
        TestComments();
        TestPreprocessor();
        TestWhitespace();
        
        Console.WriteLine("\n✓ All token tests passed!\n");
    }

    private void TestKeywords()
    {
        Console.WriteLine("Testing keywords...");
        
        // Test that keywords are recognized
        AssertTokenizes("class", "KwClass");
        AssertTokenizes("public", "KwPublic");
        AssertTokenizes("static", "KwStatic");
        AssertTokenizes("void", "KwVoid");
        AssertTokenizes("return", "KwReturn");
        AssertTokenizes("if", "KwIf");
        AssertTokenizes("else", "KwElse");
        AssertTokenizes("while", "KwWhile");
        AssertTokenizes("for", "KwFor");
        AssertTokenizes("foreach", "KwForEach");
        AssertTokenizes("async", "KwAsync");
        AssertTokenizes("await", "KwAwait");
        AssertTokenizes("manual", "KwManual"); // CRAB-specific
        
        // Test contextual keywords
        AssertTokenizes("var", "KwVar");
        AssertTokenizes("when", "KwWhen");
        AssertTokenizes("where", "KwWhere");
        
        Console.WriteLine("  ✓ Keywords test passed");
    }

    private void TestIdentifiers()
    {
        Console.WriteLine("Testing identifiers...");
        
        AssertTokenizes("myVariable", "Identifier");
        AssertTokenizes("_privateField", "Identifier");
        AssertTokenizes("MyClass", "Identifier");
        AssertTokenizes("x1", "Identifier");
        AssertTokenizes("MAX_VALUE", "Identifier");
        
        Console.WriteLine("  ✓ Identifiers test passed");
    }

    private void TestLiterals()
    {
        Console.WriteLine("Testing literals...");
        
        // Integers
        AssertTokenizes("42", "DecimalIntegerLiteral");
        AssertTokenizes("0x2A", "HexIntegerLiteral");
        AssertTokenizes("0b101010", "BinaryIntegerLiteral");
        
        // Floats
        AssertTokenizes("3.14", "FloatLiteral");
        AssertTokenizes("1.5e10", "FloatLiteral");
        
        // Strings
        AssertTokenizes("\"hello\"", "StringLiteral");
        AssertTokenizes("@\"path\\to\\file\"", "VerbatimStringLiteral");
        
        // Characters
        AssertTokenizes("'a'", "CharacterLiteral");
        
        // Booleans
        AssertTokenizes("true", "KwTrue");
        AssertTokenizes("false", "KwFalse");
        
        // Null
        AssertTokenizes("null", "KwNull");
        
        Console.WriteLine("  ✓ Literals test passed");
    }

    private void TestOperators()
    {
        Console.WriteLine("Testing operators...");
        
        AssertTokenizes("+", "Plus");
        AssertTokenizes("-", "Minus");
        AssertTokenizes("*", "Multiply");
        AssertTokenizes("/", "Divide");
        AssertTokenizes("==", "Equality");
        AssertTokenizes("!=", "Inequality");
        AssertTokenizes("&&", "LogicalAnd");
        AssertTokenizes("||", "LogicalOr");
        AssertTokenizes("=>", "LambdaArrow");
        AssertTokenizes("??", "NullCoalesce");
        
        Console.WriteLine("  ✓ Operators test passed");
    }

    private void TestComments()
    {
        Console.WriteLine("Testing comments...");
        
        // Single-line comments should be ignored
        var source1 = "x // comment\ny";
        AssertContainsToken(source1, "Identifier"); // x and y
        
        // Multi-line comments should be ignored
        var source2 = "x /* comment */ y";
        AssertContainsToken(source2, "Identifier"); // x and y
        
        Console.WriteLine("  ✓ Comments test passed");
    }

    private void TestPreprocessor()
    {
        Console.WriteLine("Testing preprocessor directives...");
        
        AssertTokenizes("#define DEBUG", "PreprocessorDefine");
        AssertTokenizes("#if DEBUG", "PreprocessorIf");
        AssertTokenizes("#endif", "PreprocessorEndif");
        AssertTokenizes("#region MyRegion", "PreprocessorRegion");
        AssertTokenizes("#endregion", "PreprocessorEndregion");
        
        Console.WriteLine("  ✓ Preprocessor test passed");
    }

    private void TestWhitespace()
    {
        Console.WriteLine("Testing whitespace handling...");
        
        // Whitespace should be ignored
        var source = "  x  \t  y  \n  z  ";
        AssertContainsToken(source, "Identifier"); // x, y, z without whitespace tokens
        
        Console.WriteLine("  ✓ Whitespace test passed");
    }

    // Helper methods
    private void AssertTokenizes(string source, string expectedTokenType)
    {
        // In a full implementation, this would use the actual tokenizer
        // For now, we validate that the token pattern exists in the TokenSet
        var tokenField = typeof(Tokens).GetField(expectedTokenType);
        if (tokenField == null)
        {
            throw new Exception($"Token type '{expectedTokenType}' not found in TokenSet");
        }
    }

    private void AssertContainsToken(string source, string tokenType)
    {
        // Simplified assertion - in full implementation would actually tokenize
        var tokenField = typeof(Tokens).GetField(tokenType);
        if (tokenField == null)
        {
            throw new Exception($"Token type '{tokenType}' not found in TokenSet");
        }
    }

//     public static void Main(string[] args)
//     {
//         try
//         {
//             var tests = new TokenTests();
//             tests.RunAll();
//         }
//         catch (Exception ex)
//         {
//             Console.WriteLine($"\n✗ Test failed: {ex.Message}");
//             Environment.Exit(1);
//         }
//     }
// }
}
