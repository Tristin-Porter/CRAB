# CRAB Architecture Overview

## Introduction

This document provides a comprehensive overview of CRAB's compiler architecture, design decisions, and implementation strategy.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        Source Code (C#)                      │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   Frontend (CDTk-based)                      │
│  ┌──────────────┐  ┌─────────────┐  ┌──────────────────┐   │
│  │   Lexer      │→ │   Parser    │→ │  Semantic        │   │
│  │   (Tokens)   │  │   (AST)     │  │  Analysis        │   │
│  └──────────────┘  └─────────────┘  └──────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Memory Verification Pipeline                    │
│  ┌──────────────────────────┐  ┌────────────────────────┐  │
│  │  Automatic Model (CTGC)  │  │  Manual Model          │  │
│  │  • Lifetime inference    │  │  • Ownership graphs    │  │
│  │  • Region analysis       │  │  • Symbolic execution  │  │
│  │  • Escape analysis       │  │  • Safety verification │  │
│  │  → AST Annotations       │  │  → AST Annotations     │  │
│  └──────────────────────────┘  └────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              WASM Code Generation (MapSet)                   │
│  • Direct AST → WASM translation                             │
│  • Uses annotations for memory management                    │
│  • No intermediate representation                            │
│  • Produces pure WASM MVP                                    │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                   WebAssembly Output                         │
│                      (.wasm file)                            │
└─────────────────────────────────────────────────────────────┘
```

## Core Components

### 1. Frontend (CDTk-based)

**Purpose:** Parse C# source code into Abstract Syntax Trees (ASTs).

**Technology:** CDTk.cs (Compiler Development Toolkit)

**Key Features:**
- **AG-LL Parsing**: Attribute Grammar LL predictive parser with GLL fallback
- **Token Model**: Lexical analysis with comprehensive C# token set
- **Rule Model**: Grammar rules for full C# syntax
- **MapSet Integration**: Efficient set operations for parsing
- **Diagnostic System**: Detailed error reporting

**Components:**

#### Lexer
- **Input**: Raw C# source text
- **Output**: Token stream
- **Features**:
  - Unicode support
  - Multi-line tokens (strings, comments)
  - Preprocessor directives
  - Context-sensitive keywords

#### Parser
- **Input**: Token stream
- **Output**: Untyped AST
- **Strategy**:
  - Predictive parsing (LL) for common cases
  - GLL fallback for ambiguities
  - SPPF construction for ambiguous parses
  - Conflict resolution via MapSet analysis

#### Semantic Analysis
- **Input**: Untyped AST
- **Output**: Typed AST with symbol tables
- **Features**:
  - Type checking and inference
  - Name resolution
  - Scope management
  - Generic instantiation
  - Overload resolution

### 2. Memory Verification Pipeline

**Purpose:** Ensure 100% memory safety for all code.

**Strategy:** Dual memory model with compile-time verification.

#### Automatic Memory Model (CTGC)

**Algorithm:** Compile-Time Garbage Collection (inspired by Mercury language)

**Phases:**

1. **Lifetime Inference**
   ```
   Input: Typed AST
   Output: Lifetime-annotated AST
   
   Algorithm:
   - Build def-use chains
   - Compute reaching definitions
   - Infer lifetime bounds
   - Propagate constraints
   ```

2. **Region Analysis**
   ```
   Input: Lifetime-annotated AST
   Output: Region-annotated AST
   
   Algorithm:
   - Partition program into regions
   - Assign allocations to regions
   - Compute region nesting
   - Optimize region boundaries
   ```

3. **Deallocation Point Computation**
   ```
   Input: Region-annotated AST
   Output: AST with deallocation instructions
   
   Algorithm:
   - Identify last use of each object
   - Insert deallocation at last use or region exit
   - Handle exception paths
   - Optimize for early deallocation
   ```

4. **Escape Analysis**
   ```
   Input: AST with allocations
   Output: Escape information
   
   Algorithm:
   - Track object flow through program
   - Identify escaping allocations
   - Mark stack-allocatable objects
   - Optimize based on escape info
   ```

#### Manual Memory Model

**Algorithm:** Abstract Interpretation with Symbolic Execution

**Phases:**

1. **Ownership Graph Construction**
   ```
   Input: Manual block AST
   Output: Ownership graph
   
   Algorithm:
   - Build pointer aliasing graph
   - Identify ownership relationships
   - Track ownership transfers
   - Detect sharing violations
   ```

2. **Symbolic Execution**
   ```
   Input: Manual block AST + Ownership graph
   Output: Symbolic execution trace
   
   Algorithm:
   - Execute program symbolically
   - Track symbolic state at each point
   - Explore all paths
   - Merge states at join points
   ```

3. **Safety Verification**
   ```
   Input: Symbolic execution trace
   Output: Safety proof or error
   
   Algorithm:
   - Verify no use-after-free
   - Verify no double-free
   - Verify no memory leaks
   - Verify bounds safety
   - Generate proof obligations
   - Discharge or report violations
   ```

### 3. WASM Code Generation (MapSet)

**Purpose:** Directly translate C# AST to WASM MVP with no intermediate representation.

**Technology:** CDTk MapSet with memory model annotations

**Key Features:**
- Direct AST → WASM translation
- Memory annotations guide code generation
- No intermediate representation layer
- Declarative mapping approach
- Type information from AST
- Memory safety enforced by annotations

**How It Works:**

1. **MapSet Receives**:
   - C# AST from frontend
   - Automatic model annotations (allocations, deallocations, lifetimes)
   - Manual model annotations (ownership proofs, verification results)

2. **MapSet Generates**:
   - WASM module structure
   - Function definitions with memory management
   - Linear memory layout
   - Proper deallocation instructions
   - Verified manual memory operations

3. **Translation Process**:
   ```
   C# AST Node          Memory Annotation         WASM Output
   -----------          -----------------         -----------
   new MyClass()   →    Alloc(site_1)        →   call $malloc + initialize
   obj.Method()    →    Use(site_1)          →   call $method
   } end scope    →    Dealloc(site_1)       →   call $free
   
   manual { ... }  →    Verified(proof)      →   verified WASM code
   ```

### 4. Optimization

**Purpose:** Generate efficient WASM while preserving safety.

**Note:** Optimizations are performed during MapSet translation, not as separate passes.

**Optimization Strategies:**

#### Allocation Optimization
- Stack allocation when possible
- Allocation coalescing
- Lifetime-based optimization

#### LINQ Optimization  
- Query fusion in memory model
- Eliminate intermediate allocations
- Direct WASM generation for common patterns

#### Control Flow Optimization
- Pattern matching optimization
- Branch elimination
- Tail call optimization

## Design Decisions

### Why CDTk for Frontend?

**Decision:** Use CDTk.cs as the parsing framework.

**Rationale:**
- Designed specifically for C# parsing
- AG-LL architecture handles C# complexity
- GLL fallback for ambiguous constructs
- Excellent error recovery
- MapSet-based optimization
- Already scaffolded into repository

### Why Dual Memory Model?

**Decision:** Provide both Automatic (CTGC) and Manual verified memory.

**Rationale:**
- **Automatic**: Ease of use for 99% of code
- **Manual**: Low-level control when needed
- **Isolation**: Maintains safety guarantees
- **Flexibility**: Best of both worlds

### Why WASM MVP Only?

**Decision:** Target pure WASM MVP, no extensions.

**Rationale:**
- **Universal compatibility**: Works everywhere
- **No runtime dependencies**: True sovereignty
- **Predictable behavior**: Well-defined semantics
- **Future-proof**: MVP is stable baseline

### Why Compile-Time Verification?

**Decision:** All verification at compile-time, zero runtime checks.

**Rationale:**
- **Performance**: No runtime overhead
- **Predictability**: Deterministic execution
- **Safety**: Guaranteed before deployment
- **Size**: No verification code in output

## Implementation Strategy

### Phase 1: Core Infrastructure ✅
- CDTk integration
- Basic IR design
- MapSet implementation
- Token and rule models

### Phase 2: Automatic Memory Model ✅
- Lifetime inference
- Region analysis
- Deallocation computation
- Basic optimization

### Phase 3: Manual Memory Model ✅
- Ownership graphs
- Symbolic execution
- Safety verification
- Model isolation

### Phase 4: Backend Development 🔨
- WASM lowering
- Memory layout
- Function generation
- Module emission

### Phase 5: Optimization ⏳
- Dead code elimination
- LINQ optimization
- Inlining
- Advanced passes

### Phase 6: Language Features ⏳
- Generics (monomorphization)
- Async/await (state machines)
- LINQ (deforestation)
- Pattern matching

### Phase 7: Testing & Documentation ✅
- Comprehensive test suite
- User documentation
- Developer documentation
- Examples and tutorials

## Performance Characteristics

### Compile-Time

| Component | Time Complexity | Space Complexity |
|-----------|-----------------|------------------|
| Lexing | O(n) | O(n) |
| Parsing | O(n) - O(n³) | O(n) - O(n²) |
| CTGC | O(n²) | O(n) |
| Manual Verification | O(2ⁿ) worst, O(n²) typical | O(n) |
| Optimization | O(n²) | O(n) |
| Code Generation | O(n) | O(n) |

### Runtime

| Operation | Time | Notes |
|-----------|------|-------|
| Allocation (auto) | O(1) | Pre-computed at compile-time |
| Deallocation (auto) | O(1) | Deterministic points |
| Method call (static) | O(1) | Direct WASM call |
| Method call (virtual) | O(1) | Indirect call |
| Array access | O(1) | Bounds check when needed |

## Safety Guarantees

### Automatic Model Guarantees

1. **No Use-After-Free**: Lifetime analysis ensures objects are not accessed after deallocation
2. **No Double-Free**: Each allocation freed exactly once
3. **No Memory Leaks**: All allocations have corresponding deallocations
4. **No Dangling Pointers**: References cannot outlive referenced objects
5. **No Data Races**: Single-threaded execution (WASM MVP)

### Manual Model Guarantees

1. **No Use-After-Free**: Symbolic execution verifies no access after free
2. **No Double-Free**: Ownership tracking prevents multiple frees
3. **No Memory Leaks**: Verification ensures all paths free allocations
4. **Bounds Safety**: Symbolic execution verifies array bounds
5. **No Pointer Escapes**: Isolation prevents pointers leaving manual blocks

## Future Enhancements

### Short-Term
- Incremental compilation
- Better error messages
- IDE integration
- Performance profiling

### Medium-Term
- WASI support
- Advanced optimizations
- Debugger integration
- Package ecosystem

### Long-Term
- Multi-module support
- Link-time optimization
- Alternative backends (native)
- Research into novel safety analyses

## References

- CDTk Documentation (Dependencies/CDTk.cs)
- WASM MVP Specification
- Mercury Language (CTGC inspiration)
- Rust Borrow Checker (safety model comparison)

## See Also

- [Compiler Pipeline](CompilerPipeline.md) - Detailed pipeline description
- [Frontend CDTk](FrontendCDTk.md) - CDTk integration details
- [Testing Guide](TestingGuide.md) - How to test CRAB
- [Contributing Guide](ContributingGuide.md) - Contributing to CRAB
