using CDTk;
using System;

namespace CRAB;

/// <summary>
/// Simple diagnostic program to test the CRAB compiler's tokenization and parsing.
/// </summary>
class DiagnosticTest
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== CRAB Compiler Diagnostic Test ===\n");
        
        // Test 1: Simple tokenization
        Console.WriteLine("[Test 1] Testing Tokenization...");
        var tokens = new Tokens();
        var lexer = new LexicalAnalysis();
        
        // Build the lexer
        try
        {
            lexer = lexer.WithTokens(tokens);
            Console.WriteLine("✓ Tokens registered successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to register tokens: {ex.Message}");
            return;
        }
        
        // Test 2: Try to lex a simple program
        Console.WriteLine("\n[Test 2] Testing Lexical Analysis...");
        string testCode = "using System;";
        var diags = new Diagnostics();
        
        try
        {
            var tokenResult = lexer.Lex(testCode, diags);
            Console.WriteLine($"✓ Lexed {tokenResult.Count} tokens from \"{testCode}\"");
            
            foreach (var tok in tokenResult)
            {
                Console.WriteLine($"    {tok.Type}: '{tok.Lexeme}'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Lexing failed: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
        }
        
        // Test 3: Try full compilation
        Console.WriteLine("\n[Test 3] Testing Full Compilation Pipeline...");
        string simpleProgram = @"using System;

class Hello
{
    static void Main()
    {
        Console.WriteLine(""Hello"");
    }
}";
        
        try
        {
            var compiler = new Compiler()
                .WithTokens(new Tokens())
                .WithRules(new Rules())
                .WithTarget(new WASM())
                .Build();
            
            Console.WriteLine("✓ Compiler built successfully");
            
            var result = compiler.Compile(simpleProgram);
            
            if (result.Diagnostics.HasErrors)
            {
                Console.WriteLine("✗ Compilation failed with errors:");
                foreach (var diag in result.Diagnostics.Items)
                {
                    Console.WriteLine($"   {diag.Level}: {diag.Message}");
                }
            }
            else if (result.Ast != null)
            {
                Console.WriteLine($"✓ Compilation succeeded! AST type: {result.Ast.GetType().Name}");
                Console.WriteLine($"  Output length: {result.Output?.Length ?? 0} characters");
            }
            else
            {
                Console.WriteLine("? Compilation completed but no AST or output generated");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Compilation failed with exception: {ex.Message}");
            Console.WriteLine($"   Stack: {ex.StackTrace}");
        }
        
        Console.WriteLine("\n=== Diagnostic Test Complete ===");
    }
}
