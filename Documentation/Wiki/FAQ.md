# CRAB Frequently Asked Questions (FAQ)

## General Questions

### What is CRAB?

CRAB (Compiler for Reliably Acceptable Binaries) is a sovereign C# to WebAssembly compiler that provides:
- **100% Memory Safety** - Mathematically proven at compile-time
- **Zero Runtime** - No garbage collector, no JIT, pure WASM MVP
- **Full C# Support** - C# 1.0 through C# 13
- **Two Memory Models** - Automatic (CTGC) and Manual (verified)

### How is CRAB different from Blazor WebAssembly?

| Feature | CRAB | Blazor WASM |
|---------|------|-------------|
| Runtime | Zero runtime | Full .NET runtime (2MB+) |
| Garbage Collection | Compile-time (CTGC) | Runtime GC |
| Memory Safety | Mathematically proven | Runtime checked |
| WASM Output | Pure MVP | WASM + .NET runtime |
| Startup Time | Instant | Several seconds |
| File Size | Minimal | Large (runtime overhead) |

### Is CRAB a new language?

No! CRAB compiles standard C# code. You write normal C# and CRAB compiles it to WebAssembly.

### Why should I use CRAB?

Use CRAB when you need:
- **Maximum Performance**: Zero runtime overhead
- **Minimal Size**: No runtime bloat
- **Memory Safety**: Mathematically proven safety guarantees
- **WebAssembly**: Native WASM without runtime dependencies
- **Predictability**: Deterministic compilation and execution

## Memory Models

### What is CTGC (Compile-Time Garbage Collection)?

CTGC is CRAB's automatic memory model that:
- Analyzes your code at compile-time
- Infers object lifetimes
- Inserts optimal deallocation points
- Guarantees zero leaks and zero use-after-free
- Has **zero runtime overhead**

It's like having a garbage collector that runs during compilation instead of runtime.

### When should I use the Automatic memory model?

**Always**, unless you need low-level control. The automatic model:
- Is easier to use (write normal C#)
- Has zero runtime overhead
- Is provably safe
- Works for 99% of use cases

### When should I use the Manual memory model?

Use manual memory when you need:
- Direct memory control (large buffers)
- Interfacing with low-level APIs
- Specific memory layout requirements
- Performance-critical sections

### Can I mix Automatic and Manual memory?

**No.** CRAB enforces strict isolation:
- Manual pointers cannot escape manual blocks
- Automatic references cannot enter manual blocks
- This isolation is enforced at compile-time

This ensures the safety proofs for each model remain sound.

## Compilation

### How long does compilation take?

- **Automatic mode**: Fast (similar to normal C# compilation)
- **Manual mode**: Slower (mathematical verification takes time)

Manual mode's compile-time cost is intentional—it encourages using automatic mode as the default.

### What optimizations does CRAB perform?

CRAB performs:
- **Lifetime optimization**: Early deallocation when safe
- **LINQ optimization**: Minimal allocations for queries
- **Escape analysis**: Stack allocation when possible
- **Dead code elimination**: Removes unused code
- **Constant folding**: Compile-time computation
- **Inlining**: Function inlining when beneficial

### Can I control optimization level?

Yes, use the `-O` flag:
```bash
crab compile program.cs -O 0  # No optimization
crab compile program.cs -O 1  # Basic optimization
crab compile program.cs -O 2  # Standard (default)
crab compile program.cs -O 3  # Aggressive
```

### Does CRAB support incremental compilation?

Not yet, but it's planned for future releases.

## Language Support

### Does CRAB support all C# features?

CRAB supports C# 1.0 through C# 13, with some limitations:
- ✅ Classes, structs, interfaces, generics
- ✅ Async/await, LINQ, pattern matching
- ✅ Properties, events, delegates, lambdas
- ✅ Records, nullable reference types
- ⚠️ Reflection (compile-time only)
- ⚠️ Dynamic (resolved at compile-time)
- ❌ Multithreading (WASM MVP is single-threaded)

See [Language Support](LanguageSupport.md) for details.

### Can I use async/await?

Yes! CRAB fully supports async/await. It's compiled to state machines that work with WASM continuations.

```csharp
async Task<int> FetchDataAsync()
{
    await Task.Delay(1000);
    return 42;
}
```

### Can I use LINQ?

Yes! CRAB optimizes LINQ queries to minimize allocations:

```csharp
var result = numbers
    .Where(x => x > 10)
    .Select(x => x * 2)
    .ToList();
```

### Does CRAB support generics?

Yes! Generics are monomorphized (like C++ templates), generating specialized code for each type used.

### Can I use third-party NuGet packages?

Not directly. NuGet packages often depend on .NET runtime features. You can:
- Use source-only packages
- Port libraries to CRAB (usually straightforward)
- Request CRAB-compatible versions from library authors

## WASM Output

### What WASM features does CRAB target?

CRAB targets **WASM MVP** (Minimum Viable Product):
- No WASM GC required
- No threads required
- No exceptions proposal required
- Works in all WASM runtimes

### How big are the generated WASM files?

Very small! Since there's no runtime:
- **Hello World**: ~2KB
- **Simple App**: 10-50KB
- **Complex App**: 100-500KB

Compare to Blazor WASM: 2MB+ for minimal app.

### Can I inspect the generated WASM?

Yes! Use `--emit-text` to generate WAT (WASM Text):

```bash
crab compile program.cs --emit-text -o program.wat
```

You can also emit the IR:

```bash
crab compile program.cs --emit-ir -o program.ir
```

### Where can I run the generated WASM?

Anywhere that supports WASM MVP:
- **Browsers**: Chrome, Firefox, Safari, Edge
- **Standalone**: Wasmtime, Wasmer, WAMR
- **Node.js**: Built-in WASM support
- **Cloud**: Cloudflare Workers, Fastly Compute@Edge

## Debugging

### How do I debug CRAB programs?

Use these techniques:
1. **Compiler errors**: CRAB provides detailed diagnostics
2. **IR inspection**: Examine intermediate representation
3. **WAT output**: Read WASM text format
4. **WASM debuggers**: Use browser DevTools or wasmtime debugging

### What if my program doesn't compile?

Check:
1. **Error messages**: CRAB provides detailed errors
2. **Language support**: Verify feature is supported
3. **Memory model isolation**: Check for cross-model violations
4. **Troubleshooting guide**: See [Troubleshooting.md](Troubleshooting.md)

### How do I report a bug?

File an issue on GitHub:
1. Include minimal reproduction
2. Provide compiler version (`crab --version`)
3. Include error messages
4. Describe expected vs actual behavior

## Performance

### How fast is CRAB-compiled code?

CRAB-compiled code is typically:
- **Faster than .NET AOT**: No GC overhead
- **Comparable to Rust/C++**: Similar performance characteristics
- **Deterministic**: No GC pauses or JIT delays

### Does CRAB have garbage collection pauses?

**No.** CRAB has zero runtime overhead. All memory management happens at compile-time.

### How does CRAB compare to Rust?

| Aspect | CRAB | Rust |
|--------|------|------|
| Language | C# (familiar) | Rust (learning curve) |
| Memory Safety | Compile-time proven | Compile-time proven |
| Memory Model | Automatic + Manual | Manual (borrow checker) |
| Runtime | Zero | Zero |
| Performance | Comparable | Comparable |
| Ease of Use | High (automatic mode) | Medium (borrow checker) |

## Future Plans

### Is CRAB production-ready?

CRAB is in active development. It's suitable for:
- ✅ Experimental projects
- ✅ Performance-critical components
- ✅ Learning WASM and memory safety
- ⚠️ Production use (evaluate based on your needs)

### What features are planned?

Upcoming features:
- Incremental compilation
- Better debugger integration
- WASI support
- Additional optimizations
- IDE plugins

### Can I contribute to CRAB?

Yes! CRAB welcomes contributions:
- Code contributions (see [ContributingGuide.md](../Internal/ContributingGuide.md))
- Bug reports
- Documentation improvements
- Example programs
- Community support

## Getting Help

### Where can I get help?

- **Documentation**: [Full Documentation](../README.md)
- **Issues**: [GitHub Issues](https://github.com/yourusername/CRAB/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/CRAB/discussions)
- **Examples**: [Examples and Tutorials](ExamplesAndTutorials.md)

### How do I stay updated?

- Watch the GitHub repository
- Follow release notes
- Join GitHub Discussions
- Check the changelog

## Licensing

### What license does CRAB use?

CRAB is released under the MIT License. See [LICENSE.md](../../LICENSE.md).

### Can I use CRAB commercially?

Yes! The MIT License allows commercial use.

### Do I need to open-source my CRAB programs?

No. The MIT License does not require you to open-source programs compiled with CRAB.

## Troubleshooting

### "Memory model isolation violation" error

You're trying to mix automatic and manual memory. Solutions:
- Keep automatic code outside manual blocks
- Don't pass manual pointers to automatic code
- Use appropriate memory model for each section

See [Memory Models](MemoryModels.md) for details.

### "Lifetime inference failed" error

CRAB couldn't infer object lifetime. Try:
- Simplifying control flow
- Breaking complex functions into smaller ones
- Using manual memory model for complex cases

### Compilation is slow

Manual memory verification is intentionally slower. Solutions:
- Use automatic mode where possible
- Profile with `--verbose` to identify slow passes
- Break large manual blocks into smaller ones

### Generated WASM is large

Check:
- Are you including unused code? (Dead code elimination should help)
- Use `-O 3` for maximum optimization
- Profile with `--emit-ir` to see what's being compiled

See [Troubleshooting.md](Troubleshooting.md) for more solutions.
