# Performance Optimization Guide

CDTk is designed for high performance while maintaining safety and expressiveness. This guide helps you get the most out of CDTk.

## Performance Characteristics

### Lexical Analysis (Tokenization)

**Speed**: 100M+ characters/second

**Characteristics**:
- DFA-based matching (deterministic finite automaton)
- Linear time complexity: O(n) where n is input length
- No backtracking
- Minimal allocations

**Optimization Tips**:
1. **Order tokens by specificity**: More specific tokens first
   ```csharp
   public Token If = "if";           // Specific keyword
   public Token Identifier = @"\w+"; // General pattern
   ```

2. **Use `.Ignore()` for whitespace**: Prevents AST bloat
   ```csharp
   public Token WS = new Token(@"\s+").Ignore();
   ```

3. **Minimize token count**: Combine similar patterns when possible

### Parsing

**Speed**: 5-10M+ AST nodes/second (deterministic grammars)

**Complexity**:
- Deterministic: O(n) - linear time
- Ambiguous: O(n³) worst case, but typically much better with caching
- Large inputs (10K+ items): O(n) with v9.0.0+ optimization

**The AG-LL Advantage**:

1. **ALL(*) Fast Path**: Most grammars use deterministic parsing
2. **DFA Caching**: Results cached for repeated patterns
3. **Lazy SPPF**: Only built for ambiguous regions
4. **Large Input Optimization**: Simple repetitions use iterative matching

### Code Generation

**Speed**: Depends on template complexity

**Optimization Tips**:
1. Keep templates simple
2. Avoid complex logic in templates
3. Use models for heavy computation

## Optimizing Your Grammar

### 1. Prefer Deterministic Patterns

**Slower** (requires GLL):
```csharp
public Rule Expr = new Rule("Expr '+' Expr | Expr '*' Expr | @Number");
```

**Faster** (deterministic with ALL(*)):
```csharp
public Rule Expr = new Rule("Expr '+' Term | Term");
public Rule Term = new Rule("Term '*' Factor | Factor");
public Rule Factor = new Rule("@Number | '(' Expr ')'");
```

### 2. Left Recursion is Fine

CDTk handles left recursion efficiently:

```csharp
// This is efficient - CDTk transforms it automatically
public Rule Expr = new Rule("Expr '+' @Number | @Number");
```

Don't manually transform to right recursion - CDTk does it for you!

### 3. Avoid Deep Nesting

**Problematic**:
```csharp
// Deeply nested input like ((((((1))))))
public Rule Expr = new Rule("@Number | '(' Expr ')'");
```

If you expect deep nesting (>100 levels), consider iterative structures.

### 4. Use Repetition Operators

**Efficient** (uses optimized iteration):
```csharp
public Rule List = new Rule("@Item+");  // O(n) in v9.0.0+
```

**Less Efficient** (requires recursion handling):
```csharp
public Rule List = new Rule("List @Item | @Item");  // O(n) but more overhead
```

## Memory Optimization

### Arena Allocation

CDTk uses arena allocation by default for AST nodes:

```csharp
// Enabled by default - very fast allocation
compiler.Parser.UseArenaAllocation = true;
```

**Benefits**:
- Faster allocation than individual `new` calls
- Better cache locality
- Automatic cleanup

### Managing Large Inputs

For inputs >100K tokens:

1. **Stream processing**: Process and discard ASTs incrementally
2. **Chunking**: Split input into manageable pieces
3. **Selective parsing**: Parse only what you need

Example streaming approach:
```csharp
foreach (var chunk in inputChunks)
{
    var result = compiler.Compile(chunk);
    ProcessResult(result);
    // AST is garbage collected after processing
}
```

## Profiling Your Compiler

### Measuring Performance

```csharp
using System.Diagnostics;

var sw = Stopwatch.StartNew();
var result = compiler.Compile(input);
sw.Stop();

Console.WriteLine($"Parse time: {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"Tokens: {result.Tokens.Count}");
Console.WriteLine($"Throughput: {input.Length / sw.Elapsed.TotalSeconds:N0} chars/sec");
```

### Identifying Bottlenecks

1. **Lexing**: Measure tokenization separately
2. **Parsing**: Test with pre-tokenized input
3. **Code generation**: Measure map evaluation

```csharp
// Measure lexing
var lexer = new Lexer(tokens);
sw.Restart();
var tokenList = lexer.Tokenize(input);
sw.Stop();
Console.WriteLine($"Lexing: {sw.ElapsedMilliseconds}ms");

// Measure parsing (requires internal access)
// See Testing/ for examples
```

## Real-World Performance

### Typical Scenarios

**Expression Calculator** (100 operations):
- Lexing: <1ms
- Parsing: <5ms
- Code gen: <1ms
- Total: <10ms

**JSON Parser** (10KB file):
- Lexing: ~0.1ms (100MB/s)
- Parsing: ~1ms
- Total: ~1ms

**Large Input** (10,000 items with `@Number+`):
- Lexing: ~1ms
- Parsing: ~50ms (v9.0.0+ optimization)
- Total: ~50ms

### Performance Benchmarks

From CDTk test suite (Testing/UnitTests):

- Lexing: 100M+ chars/sec sustained
- Parsing: 5-10M nodes/sec for deterministic grammars
- Integration tests: All complete in <1 second

## When Performance Matters Less

Don't optimize prematurely! For many use cases:

- **Development tools**: User perception is >100ms
- **Small inputs**: (<1000 tokens) parsing is always fast
- **One-time compilation**: Startup cost amortized

Focus on grammar correctness and maintainability first.

## Advanced Optimization

### DFA Caching

CDTk caches GLL results in a DFA. Repeated patterns become deterministic:

```csharp
// First parse: GLL exploration
result1 = compiler.Compile("complex input");

// Second parse: Uses cached DFA
result2 = compiler.Compile("similar input");  // Faster!
```

### Lazy SPPF

For ambiguous grammars, SPPF is built lazily:

- Only for ambiguous regions
- Shared structure (not duplicated)
- Minimal memory overhead

### Future Optimizations

CDTk v9.0.0+ is fully optimized for production use. Future improvements may include:

- Parallel lexing for very large inputs
- Incremental parsing for IDEs
- Custom allocators for specific scenarios

## Performance Checklist

When building a production compiler:

- [ ] Whitespace tokens use `.Ignore()`
- [ ] Token order is specific-to-general
- [ ] Grammar is as deterministic as possible
- [ ] Arena allocation is enabled (default)
- [ ] You've profiled actual usage patterns
- [ ] You're not optimizing prematurely

## Troubleshooting Performance

**Slow lexing**:
- Check regex complexity in token definitions
- Verify token ordering

**Slow parsing**:
- Check for highly ambiguous grammar
- Test with simpler grammar variant
- Measure deterministic vs GLL usage

**High memory**:
- Verify arena allocation is enabled
- Process and discard ASTs incrementally
- Check for retained AST references

## See Also

- [AG-LL Parser](12-AGLLParser.md) - Understanding the parsing strategy
- [Implementation Status](../../IMPLEMENTATION_STATUS.md) - Current performance metrics
- [Best Practices](16-BestPractices.md) - General optimization patterns
- [Internal: Performance Infrastructure](../Internal/14-PerformanceInfrastructure.md) - Implementation details
