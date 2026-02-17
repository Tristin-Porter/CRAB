# CRAB Compiler Architecture Summary

## Overview

CRAB is a **C# to WebAssembly compiler** that generates WAT (WebAssembly Text format) from C# source code. The compiler is implemented entirely in C# and follows a modular pipeline architecture.

## Table of Contents

1. [Compiler Pipeline](#compiler-pipeline)
2. [Directory Structure](#directory-structure)
3. [WAT/WASM Generation](#watwasm-generation)
4. [PE Format Generation](#pe-format-generation)
5. [Build and Run Instructions](#build-and-run-instructions)
6. [Key Files and Components](#key-files-and-components)
7. [Testing](#testing)

---

## Compiler Pipeline

The CRAB compiler uses a **multi-stage pipeline** built on top of the **CDTk** framework:

```
C# Source Code
    ↓
[1. Tokenization] (Tokens.cs)
    ↓
[2. Parsing] (Rules.cs)
    ↓
[3. AST Construction] (CDTk)
    ↓
[4. Semantic Analysis] (Automatic.cs, Manual.cs)
    ↓
[5. WAT Code Generation] (MapSet.cs, WASM class)
    ↓
[6. Binary Generation] (BADGER)
    ↓
Output: .wasm / .wat / native binary
```

### Pipeline Details

1. **Tokenization**: C# source is tokenized using rules defined in `Compiler/Core/Tokens.cs`
2. **Parsing**: Grammar rules in `Compiler/Core/Rules.cs` construct an AST
3. **Semantic Analysis**: 
   - **Automatic Model** (`Compiler/Models/Automatic.cs`): CTGC memory management
   - **Manual Model** (`Compiler/Models/Manual.cs`): Manual memory verification
   - **Optimization Model** (`Compiler/Models/Optimization.cs`): Safe optimizations
4. **WAT Generation**: The `WASM` MapSet class generates WebAssembly Text format
5. **Binary Generation**: BADGER backend converts WAT to binary formats

---

## Directory Structure

```
CRAB/
├── Program.cs                    # Main entry point
├── CLI/
│   └── Commands/
│       ├── Compile.cs           # compile command - C# to WASM/native
│       ├── Build.cs             # build command - project builds
│       ├── Run.cs               # run command - execute WASM
│       └── Test.cs              # test command - run test suite
├── Compiler/
│   ├── Core/
│   │   ├── MapSet.cs           # WAT code generation (WASM class)
│   │   ├── RuleSet.cs          # C# grammar rules
│   │   └── TokenSet.cs         # C# tokenization rules
│   ├── Models/
│   │   ├── Automatic.cs        # CTGC memory model
│   │   ├── Manual.cs           # Manual memory verification
│   │   └── Optimization.cs     # Code optimization model
│   └── ProjectSystem/          # Solution/project file handling
├── Dependencies/
│   ├── CDTk/                   # Compiler toolkit framework
│   │   └── Boilerplate/
│   │       └── CDTk.cs         # Core compiler infrastructure
│   └── BADGER/                 # Backend for binary generation
│       ├── Program.cs          # BadgerCompiler main class
│       ├── Containers/
│       │   ├── WasmJS.cs      # WAT to WASM binary converter
│       │   ├── PE.cs          # Portable Executable generator
│       │   └── Native.cs      # Native binary container
│       └── Architectures/
│           ├── x86_64.cs      # x86-64 code generation
│           ├── x86_32.cs      # x86-32 code generation
│           ├── ARM64.cs       # ARM64 code generation
│           └── ARM32.cs       # ARM32 code generation
├── Testing/
│   ├── Integration/           # End-to-end tests
│   ├── Unit/                  # Unit tests
│   ├── TestProjects/          # Example C# programs
│   │   ├── HelloWorld.cs
│   │   ├── Calculator.cs
│   │   └── ClassHierarchy.cs
│   └── Utilities/
└── StandardLibrary/           # Standard library implementations
    └── System/
        ├── System.Console.cs  # Console I/O
        └── System.String.cs   # String operations
```

---

## WAT/WASM Generation

### Location: `Compiler/Core/MapSet.cs`

The **WASM MapSet** class (line 1987+) is the core WAT code generator.

### How WAT is Generated

1. **Entry Point**: `WASM.CompilationUnit` map (line 2092)
   - Generates the complete WASM module structure
   - Emits imports, globals, helper functions, and data section

2. **Code Emission**: `WasmEmit` static class (line 100+)
   - `EmitExpression()`: Converts C# expressions to WAT instructions
   - `EmitBinaryExpression()`: Handles operators (+, -, *, /, etc.)
   - `EmitInvocationExpression()`: Method calls
   - `EmitStringLiteral()`: String handling

3. **Module Structure**:
```wat
(module
  ;; Imports (memory, console_log, console_readkey)
  (import "env" "memory" (memory 1))
  (import "env" "console_log" (func $console_log (param i32) (param i32)))
  
  ;; Globals (heap pointer)
  (global $heap_ptr (mut i32) (i32.const <heap_start>))
  
  ;; Helper functions
  (func $alloc ...)              ;; Memory allocation
  (func $string_concat ...)      ;; String concatenation
  (func $int_to_string ...)      ;; Integer to string conversion
  
  ;; Data section (string literals)
  (data (i32.const 0) "Hello World!\00")
  
  ;; User functions
  (func $Main ...)
  
  ;; Exports
  (export "main" (func $Main))
)
```

### Key Features

- **String Registry**: Tracks string literals and their offsets (line 10-48)
- **Local Variable Registry**: Manages function-local variables (line 54-93)
- **Memory Management**: Bump allocator with proper heap initialization
- **Type Mappings**: C# types to WASM types (int → i32, float → f32, etc.)

### Example: C# to WAT

**Input (C#)**:
```csharp
Console.WriteLine("Answer: " + (21 + 21));
```

**Output (WAT)**:
```wat
i32.const 0         ;; ptr to "Answer: "
i32.const 8         ;; length of "Answer: "
i32.const 21        ;; first operand
i32.const 21        ;; second operand
i32.add             ;; compute 42
call $int_to_string ;; convert to string
call $string_concat ;; concatenate strings
call $console_log   ;; output result
```

---

## PE Format Generation

### Location: `Dependencies/BADGER/Containers/PE.cs`

The **PE** class generates **Portable Executable** (Windows) format from machine code.

### Structure

```csharp
public static byte[] Emit(byte[] machineCode)
```

### Generated PE Sections

1. **DOS Header** (64 bytes): Magic "MZ" header
2. **DOS Stub** (64 bytes): "This program cannot be run in DOS mode"
3. **PE Signature** (4 bytes): "PE\0\0"
4. **COFF Header** (20 bytes): Machine type (x86-64), section count
5. **Optional Header** (240 bytes): Entry point, image base, alignment
6. **Section Table** (40 bytes): .text section descriptor
7. **Code Section**: The actual machine code

### Usage

The PE format is used when compiling with the `--to-asm` and `--format pe` flags:

```bash
crab compile program.cs --to-asm --format pe --output program.exe
```

### Pipeline

```
C# Source → WAT → Native Assembly → PE Binary
```

**Note**: The native code generation (WAT → ASM) is currently a stub and returns a placeholder binary.

---

## WASM Binary Generation

### Location: `Dependencies/BADGER/Containers/WasmJS.cs`

The **WasmJS** class converts WAT text to WASM binary format.

### Method

```csharp
public static (byte[] wasm, string javascript) Emit(string watText, string wasmFileName)
```

### Generated Files

1. **.wasm**: Binary WebAssembly module
2. **.js**: JavaScript wrapper for loading and running the WASM
3. **.html**: HTML loader page (generated by Compile.cs)

### WASM Binary Structure

```
Magic Number: 0x00 0x61 0x73 0x6D (\0asm)
Version: 0x01 0x00 0x00 0x00
Sections:
  - Type Section (function signatures)
  - Import Section (imports from env)
  - Function Section (function type indices)
  - Global Section (global variables)
  - Export Section (exported functions)
  - Code Section (function bodies)
  - Data Section (string literals)
```

### Known Issues

⚠️ **WASM Binary Encoder has parsing issues** (see FINAL_STATUS.md):
- Parser doesn't understand S-expression nesting
- Control flow structures (blocks, loops) not handled correctly
- Workaround: Use external tools like `wat2wasm` from WABT

---

## Build and Run Instructions

### Prerequisites

- **.NET 10.0 SDK** or later
- Linux, macOS, or Windows

### Building the Compiler

```bash
# Clone repository
cd /path/to/CRAB

# Restore dependencies
dotnet restore

# Build
dotnet build

# The compiler is now at: bin/Debug/net10.0/CRAB.dll
```

### Running the Compiler

#### Basic Compilation (C# to WASM)

```bash
# Compile a C# file to WASM
dotnet run -- compile program.cs

# With verbose output
dotnet run -- compile program.cs --verbose

# Custom output path
dotnet run -- compile program.cs --output my-program.wasm
```

**Output files**:
- `output.wasm` - Binary WebAssembly module
- `output.js` - JavaScript wrapper
- `output.html` - HTML loader page
- `/tmp/debug_output.wat` - WAT text format (for debugging)

#### Native Compilation (C# to PE/Native)

```bash
# Compile to native x86-64 binary
dotnet run -- compile program.cs --to-asm --arch x86_64 --format native

# Compile to Windows PE executable
dotnet run -- compile program.cs --to-asm --arch x86_64 --format pe --output program.exe
```

**Note**: Native code generation is currently a stub. Use WASM output instead.

#### Building a Project

```bash
# Build a CRAB project
dotnet run -- build ./MyProject

# With configuration
dotnet run -- build ./MyProject --config release --verbose
```

#### Running WASM

```bash
# Option 1: Open HTML in browser
firefox output.html

# Option 2: Use Node.js (requires fixing JS path)
node output.js

# Option 3: Use WABT tools
wat2wasm /tmp/debug_output.wat -o output.wasm
wasmtime output.wasm  # or wasmer run output.wasm
```

### Command Reference

```
crab <command> [options]

Commands:
  compile      - Compile C# source to WebAssembly or native
  build        - Build a CRAB project
  run          - Run a compiled WASM file
  test         - Run test suite
  test-suite   - Run all unit/integration tests
  new          - Create new project
  help         - Display help

Compile flags:
  --input      - Input C# source file or directory
  --output     - Output file path
  --verbose    - Enable verbose output
  --verify     - Run additional verification passes
  --optimize   - Enable optimizations (default: true)
  --to-asm     - Compile to native assembly via BADGER
  --arch       - Target architecture (x86_64, x86_32, arm64, arm32)
  --format     - Output format (native, pe)
```

---

## Key Files and Components

### 1. Main Entry Point

**File**: `Program.cs`

```csharp
class Program
{
    static void Main(string[] args)
    {
        var registry = new Registry()
            .Register(new Help())
            .Register(new Compile())
            .Register(new Build())
            // ... other commands
        
        registry.Run(string.Join(" ", args));
    }
}
```

### 2. Compile Command

**File**: `CLI/Commands/Compile.cs`

The main compilation logic:
1. Reads C# source code
2. Invokes CDTk compiler with WASM target
3. Processes compilation result
4. Applies post-processing fixes
5. Generates output files

Key method:
```csharp
public override void Execute(string[] args, Dictionary<string, string?> flags)
```

### 3. WASM Code Generator

**File**: `Compiler/Core/MapSet.cs`

**Class**: `WASM : MapSet` (line 1987)

The MapSet defines how each AST node type is converted to WAT:

```csharp
public class WASM : MapSet
{
    // Semantic analysis models
    public Automatic AutomaticModel { get; }
    public Manual ManualModel { get; }
    public Optimization OptimizationModel { get; }
    
    // Maps for each AST node type
    public Map<AstNode, string> CompilationUnit = ...
    public Map<AstNode, string> ClassDeclaration = ...
    public Map<AstNode, string> MethodDeclaration = ...
    public Map<AstNode, string> ExpressionStatement = ...
    // ... etc
}
```

### 4. WasmEmit Helper

**File**: `Compiler/Core/MapSet.cs`

**Class**: `WasmEmit` (line 100)

Static helper for WAT code emission:

```csharp
public static class WasmEmit
{
    public static string EmitExpression(object? exprNode)
    public static string EmitBinaryExpression(AstNode node, string defaultOp)
    public static string EmitInvocationExpression(AstNode node)
    public static string EmitStringLiteral(AstNode node)
    // ... etc
}
```

### 5. BADGER Backend

**File**: `Dependencies/BADGER/Program.cs`

**Class**: `BadgerCompiler`

Main interface for backend compilation:

```csharp
public static byte[] Compile(string watText, string architecture, string format)
```

Supported architectures:
- `x86_64` - 64-bit x86
- `x86_32` - 32-bit x86
- `x86_16` - 16-bit x86 (real mode)
- `arm64` - 64-bit ARM
- `arm32` - 32-bit ARM

Supported formats:
- `native` - Raw machine code
- `pe` - Portable Executable (Windows .exe)
- `wasmjs` - WASM binary + JS wrapper

---

## Testing

### Test Structure

```
Testing/
├── Integration/
│   ├── CompilationTests.cs    # End-to-end compilation tests
│   └── AdvancedTests.cs       # Advanced feature tests
├── Unit/
│   └── AdditionalTests.cs     # Unit tests
├── TestProjects/              # Example C# programs
│   ├── HelloWorld.cs
│   ├── Calculator.cs
│   ├── ClassHierarchy.cs
│   └── GenericCollections.cs
└── Utilities/
    └── HtmlGenerator.cs       # Test report generation
```

### Running Tests

```bash
# Run all tests
dotnet run -- test-suite

# Run specific test project
dotnet run -- test Testing/TestProjects/HelloWorld.cs

# Run with verbose output
dotnet run -- test Testing/TestProjects/HelloWorld.cs --verbose
```

### Test Projects

1. **HelloWorld.cs**: Basic console output
   - Tests: Console.WriteLine, Console.ReadKey
   
2. **Calculator.cs**: Arithmetic operations
   - Tests: Integer arithmetic, operator precedence
   
3. **ClassHierarchy.cs**: Object-oriented features
   - Tests: Classes, methods, instantiation
   
4. **GenericCollections.cs**: Generics
   - Tests: Generic types, collections

### Example Test

```csharp
// Testing/TestProjects/HelloWorld.cs
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello World!");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
```

**Compilation**:
```bash
dotnet run -- compile Testing/TestProjects/HelloWorld.cs --verbose
```

**Output**: Generates WAT with correct console calls and string literals.

---

## Implementation Status

### ✅ Working Features

1. **C# Language Support**
   - Classes and methods
   - Integer and string types
   - Arithmetic expressions
   - String operations (concatenation, literals)
   - Console I/O (WriteLine, ReadKey)
   - Variable declarations
   - Method calls

2. **WAT Generation**
   - Complete WASM module structure
   - Function definitions
   - String literals in data section
   - Helper functions (alloc, string_concat, int_to_string)
   - Import/export declarations
   - Memory management

3. **Memory Safety**
   - Bump allocator
   - Proper heap initialization
   - No memory leaks or corruption

### ⚠️ Known Limitations

1. **BADGER Backend Issues**
   - WASM binary encoder has S-expression parsing bugs
   - Native code generation is stub-only
   - Workaround: Use external WAT-to-WASM tools (WABT)

2. **C# Language Features**
   - Limited to basic features
   - No advanced generics
   - No async/await
   - No LINQ

3. **Standard Library**
   - Minimal implementation
   - Only Console.WriteLine and Console.ReadKey

### 📋 Recommendations

For production use:
1. **Use CRAB for WAT generation** - High quality, correct output
2. **Use external tools for binary generation**:
   - WABT: `wat2wasm` for WASM binary
   - Wasmer/Wasmtime: For execution
   - wasm2c: For native compilation
3. **Fix BADGER** if long-term native support is needed

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────┐
│                    CRAB Compiler                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  C# Source Code                                         │
│       ↓                                                 │
│  ┌──────────────────────────────────────┐              │
│  │  CDTk Compiler Framework             │              │
│  │  ┌────────────┐  ┌────────────┐     │              │
│  │  │ Tokens.cs  │→│ Rules.cs   │     │              │
│  │  └────────────┘  └────────────┘     │              │
│  │         ↓              ↓             │              │
│  │    Tokenization   Grammar Rules     │              │
│  └──────────────────────────────────────┘              │
│       ↓                                                 │
│  ┌──────────────────────────────────────┐              │
│  │  Abstract Syntax Tree (AST)          │              │
│  └──────────────────────────────────────┘              │
│       ↓                                                 │
│  ┌──────────────────────────────────────┐              │
│  │  Semantic Analysis                   │              │
│  │  ┌──────────┐ ┌──────────┐          │              │
│  │  │Automatic │ │ Manual   │          │              │
│  │  │  Model   │ │  Model   │          │              │
│  │  └──────────┘ └──────────┘          │              │
│  └──────────────────────────────────────┘              │
│       ↓                                                 │
│  ┌──────────────────────────────────────┐              │
│  │  WASM MapSet (Code Generation)       │              │
│  │  - WasmEmit helper                   │              │
│  │  - String/Variable registries        │              │
│  │  - Type mappings                     │              │
│  └──────────────────────────────────────┘              │
│       ↓                                                 │
│  WAT (WebAssembly Text)                                │
│       ↓                                                 │
├─────────────────────────────────────────────────────────┤
│                   BADGER Backend                        │
├─────────────────────────────────────────────────────────┤
│       ↓                                                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐ │
│  │   WasmJS     │  │   Native     │  │      PE      │ │
│  │  Container   │  │  Container   │  │  Container   │ │
│  └──────────────┘  └──────────────┘  └──────────────┘ │
│       ↓                  ↓                   ↓          │
│  WASM Binary      Machine Code         PE Binary       │
│    + JS                                  (.exe)         │
└─────────────────────────────────────────────────────────┘
```

---

## Glossary

- **AST**: Abstract Syntax Tree - tree representation of source code structure
- **CDTk**: Compiler Development Toolkit - framework for building compilers
- **CTGC**: Compile-Time Garbage Collection - automatic memory management
- **MapSet**: CDTk concept - defines AST → output code mappings
- **PE**: Portable Executable - Windows executable format
- **WAT**: WebAssembly Text format - human-readable WASM
- **WASM**: WebAssembly - binary instruction format for stack-based VM
- **BADGER**: Backend Architecture for Diverse Generation, Emission, and Representation

---

## References

- **CRAB Repository**: `/home/runner/work/CRAB/CRAB`
- **Key Documentation**:
  - `Documentation/ARCHITECTURE.md`
  - `Documentation/FINAL_STATUS.md`
  - `IMPLEMENTATION_COMPLETE.md`
  - `Documentation/WAT_TO_WASM_COMPLETION.md`
- **WebAssembly Spec**: https://webassembly.github.io/spec/
- **WABT Tools**: https://github.com/WebAssembly/wabt

---

*Last Updated: 2024*
