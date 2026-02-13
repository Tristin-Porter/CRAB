# Troubleshooting Guide

This guide helps you resolve common issues when working with CDTk.

## Grammar Issues

### Grammar won't compile

**Symptoms**: Error messages during `Build()`

**Common Causes**:

1. **Undefined token types**
   ```
   Error: Grammar references token type '@Number', but no token definition exists
   ```
   **Solution**: Define the token in your TokenSet:
   ```csharp
   public Token Number = @"\d+";
   ```

2. **Undefined rules**
   ```
   Error: Grammar references rule 'Term', but no such rule exists
   ```
   **Solution**: Define the rule in your RuleSet:
   ```csharp
   public Rule Term = new Rule("@Number");
   ```

3. **Invalid grammar syntax**
   ```
   Error: Invalid grammar pattern for rule 'Expr': Expected identifier
   ```
   **Solution**: Check grammar syntax. Use `@TokenName` for tokens, `RuleName` for rules.

### Left recursion warnings

**Symptoms**: Info message about left recursion cycle

**This is normal!** CDTk detects and handles left recursion automatically. The message is informational only. Your grammar will work correctly.

Example:
```
Info: Left recursion cycle detected: Expr -> Expr
  Note: This is fully supported by the AG-LL parser's GLL component.
```

### Nullable start rule error

**Symptoms**: 
```
Error: Start rule 'Root' is nullable (can match empty input)
```

**Solution 1** (Recommended): Modify grammar to require at least one token:
```csharp
// Before (nullable)
public Rule Root = new Rule("@Number*");

// After (not nullable)
public Rule Root = new Rule("@Number+");
```

**Solution 2**: Disable the check if empty input is intentional:
```csharp
compiler.Parser.DisallowNullableStartRule = false;
```

## Parsing Issues

### Input doesn't match

**Symptoms**: No errors but output is null or unexpected

**Debugging Steps**:

1. **Enable diagnostic output**:
   ```csharp
   var compiler = new Compiler()
       .WithTokens(new Tokens())
       .WithRules(new Rules())
       .WithTarget(new Maps())
       .Build();
       
   var result = compiler.Compile("your input");
   foreach (var diag in result.Diagnostics.Items)
   {
       Console.WriteLine($"{diag.Level}: {diag.Message}");
   }
   ```

2. **Test lexing separately**:
   ```csharp
   var lexer = new Lexer(new Tokens());
   var tokens = lexer.Tokenize("your input");
   foreach (var token in tokens)
   {
       Console.WriteLine($"{token.Type}: '{token.Lexeme}'");
   }
   ```

3. **Start with simple input**: Test with minimal input first, then build up.

### Unexpected parse results

**Common Causes**:

1. **Token priority**: Longer or more specific tokens should come first in TokenSet
   ```csharp
   public Token If = "if";           // More specific
   public Token Identifier = @"\w+"; // Less specific
   ```

2. **Greedy matching**: Tokens match as much as possible
   ```csharp
   // If you want to match individual digits:
   public Token Digit = @"\d";  // NOT @"\d+"
   ```

3. **Whitespace not ignored**:
   ```csharp
   public Token WS = new Token(@"\s+").Ignore();  // Don't forget .Ignore()!
   ```

## Performance Issues

### Slow parsing

**For small inputs (<1000 tokens)**: This is unusual. Check for:
- Extremely ambiguous grammars
- Deep nesting levels (>100)
- Diagnostic output being written to slow console

**For large inputs (>10,000 tokens)**: 
- This is expected for complex grammars
- Simple repetitions are optimized in v9.0.0+
- Consider chunking very large inputs

### High memory usage

**Causes**:
1. **Large input**: Proportional to input size
2. **Ambiguous grammar**: SPPF can be large for highly ambiguous grammars
3. **AST retention**: If you keep all ASTs in memory

**Solutions**:
- Process and discard ASTs incrementally
- Use arena allocation (enabled by default)
- Consider streaming for very large inputs

## Code Generation Issues

### Mapping not found

**Symptoms**:
```
Warning: No map found for AST node 'SomeRule'
```

**Solutions**:

1. **Add the map**:
   ```csharp
   public Map SomeRule = "code template";
   ```

2. **Use fallback mapping**:
   ```csharp
   class Maps : MapSet
   {
       public Map __fallback = "{value}";  // Default for unmapped nodes
   }
   ```

### Wrong output generated

**Debugging**:

1. **Check AST structure**:
   ```csharp
   var result = compiler.Compile("input");
   Console.WriteLine(result.Ast);  // Inspect the AST
   ```

2. **Verify map patterns**: Ensure template variables match AST field names

3. **Test maps individually**: Build minimal test cases for each map

## Build/Compilation Errors

### NuGet package not found

CDTk is not yet published to NuGet. Use project reference:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/CDTk/CDTk.csproj" />
</ItemGroup>
```

### Version compatibility

CDTk targets .NET 10.0+. For earlier versions:

1. Edit CDTk.csproj:
   ```xml
   <TargetFramework>net8.0</TargetFramework>
   ```

2. Some features may not be available

### Build errors in CDTk itself

If you encounter build errors when compiling CDTk:

1. Ensure you have .NET 10.0 SDK installed
2. Run `dotnet restore` in the CDTk directory
3. Check [GitHub Issues](https://github.com/Tristin-Porter/CDTk/issues)

## Common Mistakes

### Using strings for token/rule references

**Wrong**:
```csharp
public Rule Expr = new Rule("Number + Number");  // ❌
```

**Right**:
```csharp
public Rule Expr = new Rule("@Number '+' @Number");  // ✅
```

Use `@TokenName` for tokens, `RuleName` for non-terminals.

### Forgetting to ignore whitespace

**Wrong**:
```csharp
public Token WS = @"\s+";  // ❌ Whitespace becomes part of AST
```

**Right**:
```csharp
public Token WS = new Token(@"\s+").Ignore();  // ✅
```

### Modifying compiler after Build()

**Wrong**:
```csharp
var compiler = new Compiler().WithTokens(tokens).Build();
compiler.WithRules(rules);  // ❌ Too late!
```

**Right**:
```csharp
var compiler = new Compiler()
    .WithTokens(tokens)
    .WithRules(rules)
    .Build();  // ✅ Build() comes last
```

### Not checking diagnostics

**Wrong**:
```csharp
var result = compiler.Compile(input);
var output = result.Output;  // ❌ Might be null if there were errors
```

**Right**:
```csharp
var result = compiler.Compile(input);
if (result.Diagnostics.HasErrors)
{
    foreach (var error in result.Diagnostics.Items)
        Console.WriteLine(error.Message);
    return;
}
var output = result.Output;  // ✅ Safe to use
```

## Getting More Help

### Still stuck?

1. **Check examples**: [Real-World Examples](15-Examples.md)
2. **Read documentation**: [Core Concepts](04-CoreConcepts.md)
3. **Check FAQ**: [Frequently Asked Questions](17-FAQ.md)
4. **Report issue**: [GitHub Issues](https://github.com/Tristin-Porter/CDTk/issues)

### When reporting issues

Include:
- CDTk version (check IMPLEMENTATION_STATUS.md)
- Minimal code to reproduce the problem
- Expected vs actual behavior
- Full error messages
- Input that causes the issue

### Quick Checklist

Before asking for help, verify:
- [ ] All tokens are defined in TokenSet
- [ ] All rules are defined in RuleSet
- [ ] Whitespace token has `.Ignore()`
- [ ] Grammar syntax is correct (`@Token` vs `Rule`)
- [ ] You called `Build()` before `Compile()`
- [ ] You checked `result.Diagnostics.HasErrors`
- [ ] You tested with simple input first

## See Also

- [FAQ](17-FAQ.md) - Frequently asked questions
- [Diagnostics & Error Handling](13-Diagnostics.md) - Understanding error messages
- [Best Practices](16-BestPractices.md) - Avoiding common pitfalls
- [Getting Started](01-GettingStarted.md) - Basic tutorial
