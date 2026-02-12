# CRAB Documentation

Comprehensive documentation for the CRAB compiler.

## Documents

### [Architecture.md](Architecture.md)
Complete architectural overview of the CRAB compiler:
- Core principles and design philosophy
- Architectural components (frontend, pipeline, backend)
- Compilation pipeline details
- Design decisions and rationale
- Performance characteristics
- System invariants

**Read this to**: Understand how CRAB works internally.

### [CTGC-MemoryModel.md](CTGC-MemoryModel.md)
Deep dive into Compile-Time Garbage Collection (CTGC):
- How CTGC works (lifetime inference, region analysis, etc.)
- Safety guarantees (no leaks, no use-after-free, etc.)
- Advanced features (lambdas, generics, delegates)
- Implementation strategy
- Comparison with runtime GC
- Best practices

**Read this to**: Understand automatic memory management in CRAB.

### [Manual-MemoryModel.md](Manual-MemoryModel.md)
Complete guide to manual memory management:
- Manual block syntax
- Verification process (ownership, abstract interpretation, etc.)
- Safety guarantees
- Model isolation rules
- Use cases and patterns
- Best practices
- Comparison with unsafe code in other languages

**Read this to**: Learn how to use manual memory mode safely.

### [UserGuide.md](UserGuide.md)
Practical guide for CRAB developers:
- Getting started
- CLI commands (new, compile, build, run)
- Writing CRAB code
- Project structure
- Common patterns
- Code examples
- Debugging tips
- Performance tips

**Read this to**: Start using CRAB effectively.

## Quick Links

### For New Users
1. Start with [UserGuide.md](UserGuide.md)
2. Read [CTGC-MemoryModel.md](CTGC-MemoryModel.md) for memory basics
3. Check examples in ../Testing/

### For Advanced Users
1. Study [Architecture.md](Architecture.md)
2. Deep dive into [Manual-MemoryModel.md](Manual-MemoryModel.md)
3. Explore compiler source code

### For Contributors
1. Read all documentation
2. Study the architecture
3. Review the CRAB specification (../.github/agents/crab-spec.txt)
4. Check the testing suite (../Testing/)

## Topics Overview

### Memory Management
- **CTGC (Automatic)**: [CTGC-MemoryModel.md](CTGC-MemoryModel.md)
- **Manual**: [Manual-MemoryModel.md](Manual-MemoryModel.md)
- **When to use which**: [UserGuide.md](UserGuide.md)

### Compilation
- **Pipeline overview**: [Architecture.md](Architecture.md)
- **CLI usage**: [UserGuide.md](UserGuide.md)
- **CDTk integration**: [Architecture.md](Architecture.md)

### Safety
- **Memory safety**: [CTGC-MemoryModel.md](CTGC-MemoryModel.md)
- **Type safety**: [Architecture.md](Architecture.md)
- **Verification**: [Manual-MemoryModel.md](Manual-MemoryModel.md)

### WASM
- **Code generation**: [Architecture.md](Architecture.md)
- **Target compliance**: [Architecture.md](Architecture.md)
- **Optimization**: [Architecture.md](Architecture.md)

## Document Status

All documents are complete and cover:
- ✅ Core concepts
- ✅ Practical usage
- ✅ Code examples
- ✅ Best practices
- ✅ Common patterns
- ✅ Troubleshooting

## Contributing to Documentation

To improve the documentation:
1. Keep language clear and accessible
2. Include practical examples
3. Maintain consistency with CRAB spec
4. Add code samples where helpful
5. Update all affected documents

## Additional Resources

- **Testing Suite**: ../Testing/ (with examples)
- **Source Code**: ../Compiler/, ../CLI/
- **CRAB Spec**: ../.github/agents/crab-spec.txt
- **Project README**: ../README.md (when available)

## Glossary

- **CRAB**: C# to Reliable Assembly Builder
- **CTGC**: Compile-Time Garbage Collection
- **CDTk**: Compiler Development Toolkit (parser framework)
- **WASM**: WebAssembly
- **WAT**: WebAssembly Text Format
- **MVP**: Minimum Viable Product (WASM MVP = core WASM spec)
- **AST**: Abstract Syntax Tree
- **Manual Block**: `manual { }` - code with manual memory management

## Version Information

Documentation version: 1.0.0  
Last updated: 2026-02-12  
CRAB version: 1.0.0 (Production Ready)  

**Status**: All documentation is complete and reflects the production-ready state of CRAB.

All core features are fully implemented:
- ✅ Complete C# 13 language support
- ✅ Full CTGC automatic memory model
- ✅ Complete manual memory verification
- ✅ Comprehensive optimization model
- ✅ Full WASM MVP code generation
- ✅ All CLI commands functional
- ✅ Comprehensive test coverage
