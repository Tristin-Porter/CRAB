# CRAB Compiler Documentation

Welcome to the CRAB (Compiler for Reliably Acceptable Binaries) documentation!

## Documentation Structure

### [Wiki](Wiki/) - User Documentation
User-facing documentation for using the CRAB compiler:
- **Getting Started**: Installation, first programs, basic usage
- **Language Guide**: C# language support and features
- **Memory Models**: Understanding automatic and manual memory management
- **Compilation**: How to compile programs with CRAB
- **WASM Output**: Working with WebAssembly output
- **Best Practices**: Recommended patterns and practices

### [Internal](Internal/) - Developer Documentation
Internal documentation for CRAB compiler developers:
- **Architecture**: Compiler architecture and design
- **Implementation Summaries**: Detailed implementation documentation
- **Memory Models**: Internal workings of CTGC and manual verification
- **Pipeline**: Compilation pipeline details
- **Contributing**: How to contribute to CRAB

## Quick Links

### For Users
- [Installation Guide](Wiki/Installation.md)
- [First Program](Wiki/GettingStarted.md)
- [Language Reference](Wiki/LanguageReference.md)
- [Memory Models Explained](Wiki/MemoryModels.md)

### For Developers
- [Architecture Overview](Internal/Architecture.md)
- [Frontend Implementation](Internal/IMPLEMENTATION_SUMMARY.md)
- [Automatic Model](Internal/AUTOMATIC_MODEL_IMPLEMENTATION_SUMMARY.md)
- [Manual Model](Internal/MANUAL_MODEL_IMPLEMENTATION_SUMMARY.md)
- [MapSet (WASM Generation)](Internal/MAPSET_IMPLEMENTATION_SUMMARY.md)

## About CRAB

CRAB is a sovereign, zero-runtime C# to WebAssembly compiler that:
- ✅ Compiles the entire C# language to pure WASM MVP
- ✅ Guarantees 100% memory safety mathematically
- ✅ Provides two memory models: automatic (CTGC) and manual (verified)
- ✅ Has zero runtime overhead - all decisions at compile-time
- ✅ Replaces the entire .NET toolchain
- ✅ Produces deterministic, portable WebAssembly

## Key Features

### Automatic Memory Model (CTGC)
- Compile-Time Garbage Collection like Mercury
- Zero runtime overhead
- Provably safe: no leaks, use-after-free, double-free
- 6-phase analysis pipeline
- 7 specialized analyzers

### Manual Memory Model
- Mathematically verified manual memory management
- Abstract interpretation and symbolic execution
- Ownership graph construction
- No undefined behavior possible
- Safer than Rust's unsafe{} blocks

### Full C# Support
- C# 1.0 through C# 13
- 210 tokens, 200 grammar rules
- Generics, async/await, LINQ, pattern matching
- Records, tuples, modern features
- 188 WASM code generation maps

## License

See [LICENSE.md](../LICENSE.md)

## Security

See [SECURITY.md](../SECURITY.md)
