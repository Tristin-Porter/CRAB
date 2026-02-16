# CRAB Compiler Architecture

## Overview

CRAB (C# to Reliable Assembly Builder) is a sovereign, zero-runtime C# to WebAssembly compiler that provides mathematically provable memory safety through compile-time analysis and verification.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      CRAB Compiler                          │
│                                                             │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌─────────┐ │
│  │  Source  │ → │ Frontend │ → │  Memory  │ → │ Backend │ │
│  │  Files   │   │  (CDTk)  │   │ Verifier │   │ (BADGER)│ │
│  └──────────┘   └──────────┘   └──────────┘   └─────────┘ │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## Pipeline Stages

### 1. Frontend (Parsing)

**Technology**: CDTk (Compiler Development Toolkit)

The frontend handles lexical analysis, syntax parsing, and AST construction.

#### Components

- **TokenSet** - Defines all C# 14 tokens (keywords, operators, literals)
- **RuleSet** - Defines complete C# 14 grammar
- **Parser** - AG-LL predictive parser with GLL fallback
- **AST Builder** - Constructs abstract syntax tree

#### Process Flow

```
C# Source → Lexer → Tokens → Parser → AST
```

**Example:**
```csharp
// Input
int x = 42;

// Tokens
[KEYWORD:int] [IDENTIFIER:x] [OPERATOR:=] [LITERAL:42] [SEMICOLON]

// AST
LocalDeclaration
├─ Type: Int32
├─ Identifier: "x"
└─ Initializer: Literal(42)
```

### 2. Semantic Analysis

Validates program semantics and builds symbol tables.

#### Tasks

1. **Type Checking** - Verify type correctness
2. **Name Resolution** - Resolve all identifiers
3. **Scope Analysis** - Build scope hierarchies
4. **Symbol Tables** - Track all declarations
5. **Attribute Processing** - Handle metadata

### 3. Memory Verification Pipeline

CRAB's unique contribution: compile-time memory safety verification.

#### Automatic Memory (CTGC)

**Compile-Time Garbage Collection** similar to Mercury language.

```
AST → Lifetime Analysis → Region Analysis → Deallocation Insertion → Safe AST
```

**Key Algorithms:**
- **Lifetime Inference** - Determines object lifetimes
- **Region Analysis** - Groups related allocations
- **Escape Analysis** - Identifies allocations that escape scope
- **Deallocation Insertion** - Inserts deterministic free operations

**Guarantees:**
- ✅ No memory leaks
- ✅ No use-after-free
- ✅ No double-free
- ✅ O(n log n) compilation time

#### Manual Memory Verification

For code in `manual { }` blocks, uses symbolic execution.

```
Manual Block → Ownership Graph → Alias Analysis → Symbolic Execution → Safety Proof
```

**Verification Steps:**
1. **Ownership Graph Construction** - Build ownership relationships
2. **Alias Tracking** - Track all pointer aliases
3. **Escape Analysis** - Ensure pointers don't escape manual blocks
4. **Symbolic Execution** - Prove safety properties
5. **Invariant Checking** - Verify all safety invariants

**Guarantees:**
- ✅ No invalid pointer usage
- ✅ No memory leaks
- ✅ No dangling pointers
- ✅ Complete isolation from automatic memory

### 4. Optimization Models

Three semantic models provide optimizations:

#### AutomaticModel
- CTGC transformations
- Allocation pooling
- Deallocation coalescing

#### ManualModel
- Manual memory verification
- Pointer analysis
- Safety proofs

#### OptimizationModel
- Dead code elimination
- Constant folding
- Expression normalization
- Loop optimizations

### 5. IR Lowering

Transforms verified AST to WebAssembly IR.

**MapSet System:**
```csharp
// Each AST node type maps to WASM IR
public Map<AstNode, WasmInstruction> BinaryOperation = TypedMap.For<WasmInstruction>()
    .Using(model => ((OptimizationModel)model).NormalizeExpression)
    .Emit(node => {
        var op = node["operator"];
        return new WasmInstruction(
            op == "+" ? OpCode.I32Add :
            op == "-" ? OpCode.I32Sub :
            op == "*" ? OpCode.I32Mul :
            OpCode.I32Div
        );
    });
```

### 6. Backend (Code Generation)

**Technology**: BADGER (Binary Assembly Generator)

Generates final output in multiple formats.

#### Output Formats

1. **WebAssembly Text (WAT)**
   ```wat
   (module
     (func $add (param i32 i32) (result i32)
       local.get 0
       local.get 1
       i32.add
     )
   )
   ```

2. **WebAssembly Binary (.wasm)**
   - Compact binary format
   - Direct browser execution
   - Standard WASM MVP

3. **Native Executables**
   - x86-64, x86-32, x86-16
   - ARM64, ARM32
   - PE (.exe) and ELF (.bin) formats

4. **HTML/JS Wrapper**
   - Browser-ready HTML
   - JavaScript loader
   - Console integration

## Project System

Handles C# project and solution files.

### Components

- **ProjectDiscovery** - Finds and parses .csproj files
- **SolutionFile** - Parses .sln and .slnx files
- **NuGetResolver** - Resolves NuGet dependencies
- **AssemblyMetadataReader** - Reads assembly references

### Build Process

```
Solution File → Project Discovery → Dependency Resolution → Compilation Order → Build
```

## Memory Models in Detail

### Automatic Memory (Default)

**Code:**
```csharp
void Example()
{
    var obj = new MyClass(); // Allocation
    obj.DoWork();
    // Automatic deallocation inserted here
}
```

**Compiler Transformation:**
```csharp
void Example()
{
    var obj = allocate(MyClass);
    obj.DoWork();
    deallocate(obj); // Inserted by CTGC
}
```

### Manual Memory

**Code:**
```csharp
manual
{
    var ptr = malloc(1024);
    // Use pointer
    free(ptr); // Must be explicit
}
```

**Verification:**
- Ownership graph proves `ptr` is freed
- Alias analysis ensures no dangling references
- Escape analysis prevents leaking to automatic code

## Compiler Invariants

### Safety Invariants

1. **Memory Safety**
   - All memory operations are safe
   - No undefined behavior possible

2. **Type Safety**
   - All operations are type-correct
   - No type confusion

3. **Determinism**
   - Identical input → identical output
   - Reproducible builds

### Performance Invariants

1. **Zero Runtime**
   - No garbage collector
   - No runtime library (except minimal WASM startup)

2. **Compilation Time**
   - CTGC: O(n log n) where n = AST nodes
   - Manual verification: O(n²) worst case (acceptable for slower mode)

3. **Output Size**
   - Minimal WASM output
   - No metadata tables
   - No reflection overhead

## Error Handling

### Compile-Time Errors

1. **Syntax Errors** - From parser
2. **Type Errors** - From semantic analysis
3. **Memory Safety Errors** - From verification pipeline
4. **Unsupported Features** - Clear diagnostic messages

### Error Message Quality

```
Error: Memory safety violation at line 42
  |
42 | return localPtr; // Error: pointer escapes manual block
  |        ^^^^^^^^
  |
Help: Pointers allocated in manual blocks cannot escape to automatic code.
Consider: Copy the data or restructure your code.
```

## Diagnostics System

### Phases

1. **Lexical Diagnostics** - Token errors
2. **Syntax Diagnostics** - Parse errors
3. **Semantic Diagnostics** - Type/name errors
4. **Verification Diagnostics** - Memory safety errors

### Diagnostic Levels

- **Error** - Compilation cannot proceed
- **Warning** - Potential issue
- **Info** - Helpful information

## Build Configuration

### Debug Mode
- Full symbol information
- Assertions enabled
- Optimization level 0

### Release Mode
- Minimal symbols
- All optimizations
- Dead code elimination

## Performance Characteristics

### Compilation Speed
- Small projects (< 1000 LOC): < 1 second
- Medium projects (1000-10000 LOC): 1-10 seconds
- Large projects (> 10000 LOC): 10-60 seconds

### Runtime Performance
- Matches/exceeds .NET AOT
- Comparable to Rust/C++ compiled to WASM

### Memory Usage (Compiler)
- Proportional to AST size
- ~100MB for typical projects

## Extension Points

### Custom Transformations
Add new optimization passes:
```csharp
public class CustomOptimization : Model
{
    public WasmInstruction OptimizePattern(AstNode node)
    {
        // Custom transformation logic
    }
}
```

### Custom Targets
Add new output formats via BADGER.

### Custom Standard Library
Extend `StandardLibrary/` with new namespaces.

## Technical Specifications

| Aspect | Specification |
|--------|--------------|
| Target | WASM MVP (1.0) |
| Input Language | C# 14 |
| Memory Model | CTGC + Verified Manual |
| Runtime Dependencies | None (zero-runtime) |
| Compilation Model | Ahead-of-Time (AOT) |
| Type System | Fully type-safe |
| Safety | Mathematically proven |

## See Also

- [Memory Models](Memory-Models.md) - Detailed memory system documentation
- [WASM Backend](WASM-Backend.md) - Code generation details
- [Project System](Project-System.md) - Build system documentation
