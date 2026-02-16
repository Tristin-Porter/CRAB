# CRAB Compiler - Complete Documentation Index

Welcome to the comprehensive documentation for the CRAB (C# to Reliable Assembly Builder) compiler.

## 📚 Documentation Organization

### 🏗️ Architecture Documentation
Learn about CRAB's internal structure and design.

- **[Compiler Architecture](Architecture/Compiler-Architecture.md)** - Complete compiler pipeline overview
- **[Memory Models](Architecture/Memory-Models.md)** - CTGC automatic and manual memory management
- **[Project System](Architecture/Project-System.md)** - Solution and project file handling
- **[WASM Backend](Architecture/WASM-Backend.md)** - WebAssembly generation and optimization

### 📖 User Guides
Step-by-step guides for using CRAB.

- **[Getting Started](Guides/Getting-Started.md)** - Quick start guide for new users
- **[CLI Reference](Guides/CLI-Reference.md)** - Complete command-line interface documentation
- **[Language Features](Guides/Language-Features.md)** - C# 14 language support details
- **[Memory Management](Guides/Memory-Management.md)** - Using CTGC and manual memory blocks
- **[Building Projects](Guides/Building-Projects.md)** - Working with solutions and projects
- **[Standard Library](Guides/Standard-Library.md)** - Using CRAB's standard library

### 🔧 Developer Guides
For contributors and advanced users.

- **[Contributing](Guides/Contributing.md)** - How to contribute to CRAB
- **[Building from Source](Guides/Building-from-Source.md)** - Compiling CRAB itself
- **[Extending the Compiler](Guides/Extending-Compiler.md)** - Adding new features
- **[Testing Guide](Guides/Testing-Guide.md)** - Writing and running tests

### 📚 API Reference
Detailed API documentation.

- **[MapSet API](API/MapSet-API.md)** - AST to code transformation API
- **[WasmIR API](API/WasmIR-API.md)** - WASM intermediate representation
- **[CDTk Integration](API/CDTk-Integration.md)** - Parser framework integration
- **[Standard Library API](API/Standard-Library-API.md)** - System types and console I/O

### 💡 Examples
Working code examples.

- **[Hello World Examples](Examples/Hello-World.md)** - Basic programs
- **[Memory Management Examples](Examples/Memory-Management.md)** - CTGC and manual memory
- **[Advanced Features](Examples/Advanced-Features.md)** - Generics, async, LINQ
- **[Multi-Project Solutions](Examples/Multi-Project.md)** - Complex project structures

---

## 🚀 Quick Start Paths

### I'm new to CRAB
1. [Getting Started](Guides/Getting-Started.md)
2. [Hello World Examples](Examples/Hello-World.md)
3. [CLI Reference](Guides/CLI-Reference.md)

### I want to understand memory safety
1. [Memory Models](Architecture/Memory-Models.md)
2. [Memory Management Guide](Guides/Memory-Management.md)
3. [Memory Management Examples](Examples/Memory-Management.md)

### I want to contribute
1. [Contributing Guide](Guides/Contributing.md)
2. [Compiler Architecture](Architecture/Compiler-Architecture.md)
3. [Building from Source](Guides/Building-from-Source.md)

### I need API reference
1. [MapSet API](API/MapSet-API.md)
2. [WasmIR API](API/WasmIR-API.md)
3. [Standard Library API](API/Standard-Library-API.md)

---

## 📊 Documentation Status

| Category | Files | Status |
|----------|-------|--------|
| Architecture | 4 | ✅ Complete |
| User Guides | 6 | ✅ Complete |
| Developer Guides | 4 | ✅ Complete |
| API Reference | 4 | ✅ Complete |
| Examples | 4 | ✅ Complete |

**Total**: 22 documentation files

---

## 🔍 Search by Topic

### Compilation
- [Compiler Architecture](Architecture/Compiler-Architecture.md)
- [Building Projects](Guides/Building-Projects.md)
- [CLI Reference](Guides/CLI-Reference.md)

### Memory Safety
- [Memory Models](Architecture/Memory-Models.md)
- [Memory Management](Guides/Memory-Management.md)
- [Memory Examples](Examples/Memory-Management.md)

### WebAssembly
- [WASM Backend](Architecture/WASM-Backend.md)
- [WasmIR API](API/WasmIR-API.md)

### Language Features
- [Language Features](Guides/Language-Features.md)
- [Advanced Features](Examples/Advanced-Features.md)

### Standard Library
- [Standard Library Guide](Guides/Standard-Library.md)
- [Standard Library API](API/Standard-Library-API.md)

---

## 📞 Additional Resources

- **Main README**: [README.md](../README.md)
- **Quick Reference**: [QUICK_REFERENCE.md](../QUICK_REFERENCE.md)
- **Test Suite**: [Testing/README.md](../Testing/README.md)
- **CDTk Documentation**: [Dependencies/CDTk/Documentation/](../Dependencies/CDTk/Documentation/)
- **BADGER Documentation**: [Dependencies/BADGER/Documentation/](../Dependencies/BADGER/Documentation/)

---

## 🆘 Getting Help

1. Check the relevant documentation section above
2. Review the [Examples](Examples/) for working code
3. See [Contributing Guide](Guides/Contributing.md) for how to ask questions
4. Check the test suite in [Testing/](../Testing/) for usage examples

---

**Last Updated**: 2026-02-16  
**Version**: 1.0.0  
**Status**: Complete and Production-Ready
