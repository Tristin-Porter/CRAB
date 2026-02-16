# CRAB Compiler Architecture

## Overview

CRAB is a C# to WebAssembly/Native compiler with automatic memory management (CTGC). The compiler is implemented entirely in C# and consists of two main components:

1. **CRAB Compiler** - C# to WAT (WebAssembly Text format)
2. **BADGER** - WAT to WASM/Native (Better Assembler for Dependable Generation of Efficient Results)

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         CRAB COMPILER                            │
│                                                                   │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐      │
│  │ TokenSet.cs  │───▶│  RuleSet.cs  │───▶│  MapSet.cs   │      │
│  │   (Tokens)   │    │   (Rules)    │    │    (WASM)    │      │
│  └──────────────┘    └──────────────┘    └──────┬───────┘      │
│        │                    │                    │               │
│        │                    │                    ▼               │
│        │                    │              ┌──────────────┐      │
│        │                    └─────────────▶│  Manual.cs   │      │
│        │                                   │ Automatic.cs │      │
│        └──────────────────────────────────▶│Optimization.cs│     │
│                                            └──────────────┘      │
│                                                   │               │
│                                                   ▼               │
│                                            ┌─────────────┐       │
│                                            │   WAT Text  │       │
│                                            └─────────────┘       │
└───────────────────────────────────────────────────┼──────────────┘
                                                    │
                                                    ▼
┌───────────────────────────────────────────────────────────────────┐
│                            BADGER                                  │
│                                                                    │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐       │
│  │ WATTokens    │───▶│  WATRules    │───▶│  Containers  │       │
│  └──────────────┘    └──────────────┘    └──────┬───────┘       │
│                                                   │               │
│                                                   ▼               │
│                            ┌──────────────────────────────────┐  │
│                            │  Architecture-Specific MapSets   │  │
│                            │  • x86_64 • x86_32 • x86_16     │  │
│                            │  • ARM64  • ARM32               │  │
│                            └──────────────────────────────────┘  │
│                                                   │               │
│                                                   ▼               │
│      ┌───────────────┬────────────────┬──────────────────┐      │
│      │  WASM + JS    │  Native ASM    │  PE/ELF Formats  │      │
│      │   (Web)       │  (Bare Metal)  │   (Executables)  │      │
│      └───────────────┴────────────────┴──────────────────┘      │
└────────────────────────────────────────────────────────────────────┘
```

## Component Details

### 1. CRAB Compiler (C# → WAT)

#### TokenSet.cs (Tokens class)
- **Purpose**: Lexical analysis (tokenization) of C# source code
- **Contains**: 335 token definitions for C# 13 syntax
- **Output**: Token stream
- **Technology**: CDTk TokenSet

#### RuleSet.cs (Rules class)
- **Purpose**: Syntactic analysis (parsing) of C# token stream
- **Contains**: Complete C# 13 grammar rules
- **Output**: Abstract Syntax Tree (AST)
- **Technology**: CDTk RuleSet

#### MapSet.cs (WASM class)
- **Purpose**: Code generation - translates AST to WAT (WebAssembly Text format)
- **Extends**: CDTk MapSet
- **Uses**: TokenSet.cs, RuleSet.cs
- **Integrates**: Manual.cs, Automatic.cs, Optimization.cs
- **Output**: WAT (WebAssembly Text format) as a string
- **Key Features**:
  - Helper classes: StringRegistry, LocalVariableRegistry, WasmEmit
  - Maps C# AST nodes to WAT instructions
  - Integrates memory models for safety verification
  - Generates WebAssembly MVP-compatible output

#### Manual.cs (Memory Model)
- **Purpose**: Verification of manual memory management blocks
- **Features**: 
  - Abstract interpretation
  - Symbolic execution
  - Ownership graph analysis
  - Use-after-free detection
  - Double-free detection
- **Used by**: MapSet.cs during code generation

#### Automatic.cs (CTGC Memory Model)
- **Purpose**: Automatic memory management using Conservative Tracing Garbage Collection
- **Features**:
  - Lifetime inference
  - Region analysis
  - Automatic deallocation point insertion
  - Memory safety guarantees
- **Used by**: MapSet.cs during code generation

#### Optimization.cs (Optimization Model)
- **Purpose**: Safe code transformations that preserve memory safety
- **Optimizations**:
  - Dead code elimination (DCE)
  - Constant folding
  - Common subexpression elimination (CSE)
  - Function inlining
  - Loop optimizations
  - Tail call optimization
  - Peephole optimizations
- **Guarantee**: Preserves CTGC deallocation points and ownership semantics
- **Used by**: MapSet.cs during code generation

### 2. BADGER (WAT → WASM/Native)

**Location**: `/Dependencies/BADGER/`

#### WATTokens (TokenSet)
- **Purpose**: Tokenization of WebAssembly Text format
- **Contains**: Complete WAT instruction set tokens
- **Technology**: CDTk TokenSet

#### WATRules (RuleSet)
- **Purpose**: Parsing of WebAssembly Text format
- **Contains**: Complete WAT grammar
- **Technology**: CDTk RuleSet

#### Architecture-Specific MapSets
Each architecture has its own CDTk MapSet for WAT → Assembly translation:

- **x86_64** - 64-bit x86 architecture
- **x86_32** - 32-bit x86 architecture
- **x86_16** - 16-bit x86 architecture (real mode)
- **ARM64** - 64-bit ARM architecture
- **ARM32** - 32-bit ARM architecture

Each MapSet:
- Translates WAT instructions to native assembly
- Manages stack operations
- Handles calling conventions
- Performs register allocation

#### Container Formats

BADGER supports multiple output container formats:

1. **WasmJS.cs**
   - Converts WAT → WASM binary (WebAssembly MVP format)
   - Generates JavaScript wrapper for browser execution
   - Output: `.wasm` binary + `.js` loader + `.html` runner

2. **Native.cs**
   - Raw machine code output
   - No executable headers
   - For bare-metal/embedded systems

3. **PE.cs** 
   - Windows Portable Executable format
   - Output: `.exe` files
   - For Windows operating systems

4. **ELF.cs**
   - Linux Executable and Linkable Format
   - Output: Linux executables
   - For Unix-like operating systems

## Compilation Pipeline

### Standard Compilation (C# → WAT → WASM + JS)

```bash
crab compile Program.cs --output output.wasm
# or
crab build my_project/
```

**Pipeline**:
1. **Tokenization**: TokenSet.cs reads C# source → tokens
2. **Parsing**: RuleSet.cs parses tokens → AST
3. **Analysis**: 
   - Automatic.cs performs CTGC analysis
   - Manual.cs verifies manual memory blocks
   - Optimization.cs applies safe transformations
4. **Code Generation**: MapSet.cs translates AST → WAT text
5. **WASM Emission**: BADGER WasmJS.Emit() converts WAT → WASM binary + JS wrapper

**Output Files** (in `bin/Debug/crab/Web/`):
- `project.wasm` - WebAssembly binary
- `project.js` - JavaScript loader
- `project.html` - HTML runner
- `project.wat` - Original WAT text (for debugging)

### Native Compilation (C# → WAT → Native Assembly)

```bash
crab compile Program.cs --to-asm --arch x86_64 --format native --output output.bin
```

**Pipeline**:
1. **C# → WAT**: Same as above (steps 1-4)
2. **WAT → Assembly**: BADGER uses architecture-specific MapSet
3. **Binary Generation**: Container emitter creates final binary

**Supported Architectures**:
- `x86_64` - 64-bit x86 (default)
- `x86_32` - 32-bit x86
- `x86_16` - 16-bit x86
- `arm64` - 64-bit ARM
- `arm32` - 32-bit ARM

**Supported Formats**:
- `native` - Raw machine code (default)
- `pe` - Windows executable (.exe)
- `elf` - Linux executable

## Standard Library

**Location**: `/StandardLibrary/System/`

The CRAB standard library provides comprehensive functionality for common programming tasks:

### System.cs (Core Types)
- Value types: Boolean, Int32, Int64, Char
- Reference types: String, Object, Array
- Exception hierarchy

### System.Collections.cs (Data Structures)
- Generic collections: List<T>, Dictionary<TKey,TValue>, Queue<T>, Stack<T>
- Set types: HashSet<T>
- Linked structures: LinkedList<T>
- Interfaces: IEnumerable<T>, ICollection<T>, IList<T>, IDictionary<TKey,TValue>

### System.IO.cs (File System)
- Streams: Stream, MemoryStream, FileStream
- Text I/O: StreamReader, StreamWriter
- File operations: File, Directory, Path

### System.Text.cs (Text Processing)
- StringBuilder - Mutable string builder
- Encoding: UTF8Encoding, ASCIIEncoding, UnicodeEncoding
- Regex - Pattern matching

### System.Web.cs (HTTP/Web)
- HTTP clients: WebClient, HttpClient
- HTTP primitives: HttpWebRequest, HttpWebResponse, Uri
- Content types: StringContent, ByteArrayContent

### System.Data.cs (Database)
- Data structures: DataTable, DataSet, DataColumn, DataRow
- Database interfaces: IDbConnection, IDbCommand, IDataReader
- Provider model: DbConnection, DbCommand

### System.Security.cs (Cryptography)
- Hash algorithms: MD5, SHA1, SHA256
- Symmetric encryption: AES
- Asymmetric encryption: RSA
- Utilities: CryptoStream, SecureString, RandomNumberGenerator

### System.Console.cs (I/O)
- Console operations: WriteLine, Write, ReadLine

## Memory Safety

CRAB provides 100% memory safety through two memory models:

### 1. Automatic Memory Model (CTGC)
- **Default mode** for all C# code
- Conservative Tracing Garbage Collection
- Automatic lifetime inference
- Zero-cost abstractions where possible
- Guarantees:
  - No memory leaks
  - No use-after-free
  - No double-free
  - No dangling pointers

### 2. Manual Memory Model
- **Opt-in** via `manual { }` blocks
- Explicit control for performance-critical code
- Full verification using abstract interpretation
- Same safety guarantees as automatic mode
- Use cases: Real-time systems, embedded systems

## Key Design Principles

1. **Separation of Concerns**
   - CRAB handles C# → WAT (high-level compilation)
   - BADGER handles WAT → WASM/Native (low-level code generation)

2. **CDTk-Based Architecture**
   - Both CRAB and BADGER use CDTk (Compiler Development Toolkit)
   - TokenSet → RuleSet → MapSet pipeline
   - Declarative grammar and code generation

3. **Memory Safety First**
   - All code is memory-safe by default (CTGC)
   - Manual mode provides verified explicit control
   - Optimizations preserve safety guarantees

4. **WebAssembly MVP Target**
   - All WASM output is WebAssembly MVP-compatible
   - No post-MVP features (threads, SIMD, etc.)
   - Maximum browser compatibility

5. **Multi-Architecture Support**
   - Single source → multiple targets
   - Architecture-specific optimizations
   - Portable C# standard library

## Build Outputs

### Debug Build
```
bin/Debug/crab/
├── Web/
│   ├── project.wasm (WASM binary)
│   ├── project.js (JavaScript loader)
│   ├── project.html (HTML runner)
│   └── project.wat (WAT text)
├── Windows/
│   └── output.exe (PE executable)
└── Native/
    └── output.bin (Raw binary)
```

### Release Build
```
bin/Release/crab/
└── (Same structure as Debug)
```

## Command-Line Interface

### Compile Single File
```bash
crab compile Program.cs --output output.wasm
crab compile Program.cs --to-asm --arch x86_64 --format native
```

### Build Project
```bash
crab build .                    # Build current directory
crab build MySolution.sln       # Build solution
crab build MyProject.csproj     # Build project
crab build --config release     # Release configuration
```

### Testing
```bash
crab test                       # Run all tests
crab test-suite                 # Run test suite with detailed output
```

## Performance Characteristics

### Compilation Speed
- **C# → WAT**: Fast (single-pass with integrated analysis)
- **WAT → WASM**: Fast (binary encoding)
- **WAT → Native**: Moderate (architecture-specific translation)

### Generated Code Quality
- **WASM**: Near-native performance in browsers
- **Native**: Competitive with optimizing C compilers
- **Memory**: Zero-overhead abstractions where possible

### Memory Overhead
- **CTGC**: Conservative collection with low overhead
- **Stack**: WebAssembly linear memory model
- **Heap**: Managed by CTGC allocator

## Future Enhancements

Potential areas for expansion:
- SIMD support (post-MVP WebAssembly)
- Threads and shared memory (post-MVP WebAssembly)
- More optimization passes
- Additional architecture targets (RISC-V, MIPS, etc.)
- Just-in-time (JIT) compilation for native targets
- Incremental compilation
- Debug information generation (DWARF, source maps)

## References

- [CRAB Specification](../crab-spec.txt)
- [WebAssembly MVP Specification](https://webassembly.github.io/spec/core/)
- [CDTk Documentation](../Dependencies/CDTk/)
- [BADGER Documentation](../Dependencies/BADGER/)
