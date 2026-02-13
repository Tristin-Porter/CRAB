# Frequently Asked Questions (FAQ)

## General Questions

### What is CDTk?

CDTk (Compiler Description Toolkit) is a modern, high-performance compiler framework for .NET (C#). It provides a complete pipeline for building compilers and interpreters: lexical analysis, parsing, semantic analysis, and code generation—all in one memory-safe, declarative framework.

### Is CDTk production-ready?

**Yes!** CDTk v9.0.0+ is 100% feature-complete with:
- ✅ All 43 tests passing (100% coverage)
- ✅ Zero known issues
- ✅ Full support for ANY context-free grammar
- ✅ Production-ready performance and stability

### What makes CDTk different from other parser frameworks?

1. **Complete Pipeline**: Lexing → Parsing → Semantic Analysis → Code Generation in one framework
2. **AG-LL Parser**: Supports ANY CFG including left-recursive and ambiguous grammars
3. **100% Safe Code**: No unsafe constructs, fully memory-safe
4. **Declarative API**: Define WHAT your language looks like, not HOW to parse it
5. **Type-Safe**: Strongly-typed C# APIs with compile-time validation

## Grammar Support

### Does CDTk support left-recursive grammars?

**Yes!** CDTk fully supports all forms of left recursion:
- Direct left recursion: `Expr -> Expr '+' Number`
- Mutual left recursion: `A -> B 'x'`, `B -> A 'y'`
- Indirect left recursion: Multi-step cycles

The AG-LL parser handles left recursion automatically via transformation or GLL fallback.

### Can CDTk handle ambiguous grammars?

**Yes!** CDTk's GLL component builds a Shared Packed Parse Forest (SPPF) to represent all possible parse trees for ambiguous input. You can detect and analyze ambiguity in your grammars.

### What is the largest input CDTk can handle?

CDTk has been tested with 10,000+ items successfully. The v9.0.0+ optimization enables O(N) performance for simple repetition patterns. For very large inputs (100K+), performance depends on grammar complexity.

### What grammars does CDTk support?

**ANY context-free grammar (CFG)!** This includes:
- Deterministic grammars
- Non-deterministic grammars
- Left-recursive grammars
- Right-recursive grammars
- Ambiguous grammars
- Deeply nested structures

## Performance

### How fast is CDTk?

**Lexing**: 100M+ characters/second  
**Parsing**: 5-10M+ AST nodes/second (deterministic grammars)  
**Complex Grammars**: Varies but remains practical for real-world use

### Is CDTk thread-safe?

The `Compiler` object is thread-safe for parsing after building. Multiple threads can call `Compile()` simultaneously on the same compiler instance.

## API & Usage

### Do I need to write unsafe code?

**No!** CDTk is 100% safe managed C# code. No `unsafe` blocks, no pointers, no manual memory management.

### Can I use CDTk with .NET Framework?

CDTk targets .NET 10.0+. For earlier .NET versions, you may need to adjust the target framework in the .csproj file, but some features may not be available.

### How do I handle errors?

CDTk provides comprehensive diagnostics through the `Diagnostics` system. All errors, warnings, and info messages are collected during compilation with source spans for precise error reporting. See [Diagnostics & Error Handling](13-Diagnostics.md).

### Can I generate code for multiple target languages?

**Yes!** The `MapSet` system allows you to define multiple code generators for different target languages. You can create separate MapSets for C, Python, JavaScript, etc.

## Common Issues

### My grammar isn't parsing correctly

1. Check diagnostics for grammar validation errors
2. Verify token definitions match your input
3. Test with simple input first
4. Enable diagnostic output to see what's happening
5. See [Troubleshooting Guide](18-Troubleshooting.md)

### How do I handle operator precedence?

Use multiple rules with proper structure:

```csharp
public Rule Expr = new Rule("Expr '+' Term | Term");
public Rule Term = new Rule("Term '*' Factor | Factor");
public Rule Factor = new Rule("@Number | '(' Expr ')'");
```

This naturally encodes precedence: `*` binds tighter than `+`.

### Can I match empty input?

By default, CDTk disallows nullable start rules (rules that can match empty input). You can disable this with:

```csharp
compiler.Parser.DisallowNullableStartRule = false;
```

## Getting Help

### Where can I find examples?

- [Real-World Examples](15-Examples.md) - Complete language implementations
- [Quick Start Tutorial](02-QuickStart.md) - Step-by-step guide
- Main [README.md](../../README.md) - Quick examples

### Where do I report bugs?

[GitHub Issues](https://github.com/Tristin-Porter/CDTk/issues) - Please include:
- CDTk version
- Minimal reproduction code
- Expected vs actual behavior
- Any error messages

### How can I contribute?

See the [Contributing Guide](https://github.com/Tristin-Porter/CDTk/wiki/Contributing) for information on:
- Code contributions
- Documentation improvements
- Bug reports
- Feature suggestions

## Advanced Topics

### What is the AG-LL parser?

AG-LL (Adaptive Generalized LL) combines ALL(*) predictive parsing with GLL fallback. It provides the performance of predictive parsing for deterministic grammars while supporting ANY CFG via GLL. See [AG-LL Parser Explained](12-AGLLParser.md).

### How does SPPF work?

SPPF (Shared Packed Parse Forest) is a compact representation of multiple parse trees for ambiguous input. Instead of exponentially duplicating nodes, SPPF shares common subtrees. See [SPPF Documentation](../Internal/09-SPPF.md).

### Can I customize the AST structure?

Yes! Use the `.Returns()` method on rules to specify which parts of the match to include in the AST:

```csharp
public Rule Expr = new Rule("left:@Number '+' right:@Number")
    .Returns("left", "right");
```

See [AST Construction](07-AST.md) for details.

## See Also

- [Getting Started](01-GettingStarted.md) - Start here
- [Troubleshooting](18-Troubleshooting.md) - Common issues and solutions
- [Best Practices](16-BestPractices.md) - Patterns for success
- [Implementation Status](../../IMPLEMENTATION_STATUS.md) - Feature status and test results
