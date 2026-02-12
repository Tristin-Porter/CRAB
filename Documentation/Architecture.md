# CRAB Architecture Documentation

## Table of Contents
- [Overview](#overview)
- [Core Principles](#core-principles)
- [Architectural Components](#architectural-components)
- [Compilation Pipeline](#compilation-pipeline)
- [Design Decisions](#design-decisions)

## Overview

CRAB (C# to Reliable Assembly Builder) is a sovereign, zero-runtime C# to WebAssembly compiler that compiles the entire C# language into pure WASM MVP while guaranteeing mathematically provable memory safety.

**Key Innovation**: CRAB is not a new language—it's the same C# syntax and semantics developers already know, but compiled through a radically safer and more predictable architecture.

## Core Principles

### 1. Zero Runtime
- No garbage collector
- No metadata tables
- No hidden execution model
- No .NET runtime dependency
- All behavior resolved at compile time

### 2. Memory Safety Guarantees
- No use-after-free
- No double-free
- No dangling pointers
- No memory leaks
- No buffer overflows
- No invalid aliasing
- No data races (WASM MVP is single-threaded)

### 3. Full C# Compatibility
- Complete C# 13 language support
- All keywords and operators
- Generics, async/await, LINQ
- Reflection (compile-time)
- Dynamic (compile-time specialization)
- Pattern matching, delegates, lambdas
- Events, attributes, interfaces
- Inheritance, operator overloading

### 4. Deterministic Execution
- Predictable performance
- No runtime surprises
- All costs at compile time
- Transparent memory model

## Architectural Components

### Frontend: CDTk-based Parser
```
Source Code (.cs)
      ↓
[CDTk Lexer] → Tokens
      ↓
[CDTk Parser] → AST (Abstract Syntax Tree)
      ↓
[Semantic Analysis]
```

**CDTk Features**:
- AG-LL predictive parsing
- Fallback to GLL for ambiguities
- SPPF construction
- DFA caching for performance
- Complete C# 13 grammar

### Memory Verification Pipeline

The heart of CRAB's safety guarantees:

```
AST
 ↓
[CTGC Analysis] ← Automatic Memory Model
 ↓
[Manual Verification] ← Manual Memory Model
 ↓
[Safety Proofs]
 ↓
Verified AST
```

#### Automatic Memory (CTGC)
- Compile-Time Garbage Collection
- Lifetime inference
- Region analysis
- Deterministic deallocation
- Similar to Mercury's CTGC

#### Manual Memory
- Abstract interpretation
- Symbolic execution
- Ownership graphs
- Alias tracking
- Escape analysis
- 100% verified, no undefined behavior

### Backend: WASM MVP Generator

```
Verified AST
      ↓
[Lowering Pass]
      ↓
[WASM Map Generation]
      ↓
WebAssembly Text (WAT)
      ↓
[Optional: BADGER]
      ↓
Native Assembly (x86, ARM, etc.)
```

## Compilation Pipeline

### Phase 1: Lexing & Parsing
1. **Tokenization**: Source → Token Stream
   - Keywords, identifiers, literals
   - Operators, delimiters
   - Comments (ignored), preprocessor directives

2. **Parsing**: Tokens → AST
   - Grammar validation
   - Syntax tree construction
   - Error recovery

### Phase 2: Semantic Analysis
1. **Type Checking**
   - Type inference
   - Generic instantiation
   - Constraint validation

2. **Symbol Resolution**
   - Name binding
   - Scope analysis
   - Overload resolution

### Phase 3: Memory Model Analysis

#### For Automatic Code:
1. **Lifetime Inference**
   - Flow-sensitive analysis
   - Last-use tracking
   - Escape detection

2. **Region Analysis**
   - Group allocations by lifetime
   - Optimize memory layout
   - Enable bulk deallocation

3. **Allocation Tracking**
   - Track all `new` expressions
   - Array allocations
   - Delegate/lambda captures
   - String literals

4. **Deallocation Placement**
   - Compute optimal free points
   - Preserve reachability
   - Minimize fragmentation

5. **Safety Verification**
   - Prove no leaks
   - Prove no use-after-free
   - Prove no aliasing violations

#### For Manual Code:
1. **Block Extraction**
   - Identify `manual { }` blocks
   - Isolate from automatic code

2. **Ownership Graph Construction**
   - Build pointer relationships
   - Track ownership transfer
   - Detect aliasing

3. **Abstract Interpretation**
   - Symbolic execution
   - Path-sensitive analysis
   - Invariant propagation

4. **Verification**
   - Prove pointer safety
   - Prove no escapes
   - Prove bounds safety

5. **Isolation Enforcement**
   - No cross-model references
   - Strict boundary checking

### Phase 4: Optimization
- Dead code elimination
- Constant folding
- Common subexpression elimination
- Inlining
- Loop optimizations
- Tail call optimization
- Peephole optimizations

**Constraint**: All optimizations must preserve safety guarantees.

### Phase 5: WASM Generation
1. **Module Structure**
   - Function exports
   - Memory declarations
   - Type definitions

2. **Function Lowering**
   - Convert methods to WASM functions
   - Map locals to WASM locals
   - Generate parameter/return handling

3. **Memory Management**
   - Insert allocation calls
   - Insert deallocation calls (from CTGC)
   - Linear memory layout

4. **Control Flow**
   - if/then/else blocks
   - loop/br/br_if
   - block/end structures

5. **Expression Evaluation**
   - Stack-based evaluation
   - Operator lowering
   - Function calls

## Design Decisions

### Why CDTk?
- Mature, proven parser framework
- Handles full C# grammar complexity
- Predictive + GLL = fast + complete
- Excellent error recovery
- Clean AST generation

### Why Two Memory Models?
- **Automatic (CTGC)**: Ease of use, no manual management
- **Manual**: Ultimate control when needed
- **Isolation**: Safety proofs remain sound
- Developer choice based on requirements

### Why WASM MVP?
- Maximum portability
- No runtime dependencies
- Deterministic execution
- Browser-compatible
- Future-proof

### Why No Runtime?
- Predictable performance
- Zero startup overhead
- Smaller binaries
- Easier deployment
- Complete transparency

### Why Compile-Time Analysis?
- Catches bugs early
- No runtime failures
- Better optimizations
- Mathematical guarantees
- Performance without risk

## Performance Characteristics

### Compile Time
- **Automatic Mode**: Fast (similar to C#)
- **Manual Mode**: Slower (verification overhead)
- Trade-off: Compile once, run forever

### Runtime
- **Zero overhead**: No GC pauses
- **Predictable**: No JIT compilation
- **Competitive**: Matches/exceeds .NET AOT
- **Target**: Match Rust/C++ to WASM

### Memory Usage
- **Efficient**: CTGC minimizes allocations
- **Deterministic**: Predictable layout
- **Compact**: No metadata overhead

## Invariants

These invariants must NEVER be violated:

1. **Memory Safety**: 100% proven safe, no undefined behavior
2. **Type Safety**: All types verified at compile time
3. **Model Isolation**: Automatic and manual never mix
4. **WASM Compliance**: Only MVP features, no extensions
5. **C# Compatibility**: Full language support
6. **Determinism**: Identical execution every time
7. **Zero Runtime**: No hidden dependencies

## Future Directions

While maintaining all invariants:
- Incremental compilation
- Better optimization passes
- Parallel compilation
- Enhanced diagnostics
- Tooling integration
- Standard library development
