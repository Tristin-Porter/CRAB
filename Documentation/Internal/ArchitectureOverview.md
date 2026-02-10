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
│  └──────────────────────────┘  └────────────────────────┘  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              Intermediate Representation (IR)                │
│  • Memory-annotated IR                                       │
│  • Control flow graphs                                       │
│  • Data flow information                                     │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                    Optimization Passes                       │
│  • Dead code elimination                                     │
│  • Constant folding                                          │
│  • Inlining                                                  │
│  • LINQ optimization                                         │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│                  WASM MVP Backend                            │
│  • Function lowering                                         │
│  • Memory layout                                             │
│  • Control flow lowering                                     │
│  • WASM emission                                             │
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

### 3. Intermediate Representation (IR)

**Purpose:** WASM-friendly IR that preserves memory annotations.

**Design:**

```
IR ::= Program(modules)
Module ::= Function* + Global* + Data*
Function ::= Param* + Local* + Block
Block ::= Instruction*

Instruction ::=
  | Alloc(size, lifetime, region)
  | Free(ptr, proof)
  | Load(ptr, offset, type)
  | Store(ptr, offset, value, type)
  | Call(func, args)
  | Br(label, condition?)
  | Return(value?)
  | ...
```

**Key Features:**
- Memory annotations (lifetime, region, ownership)
- Control flow graph (CFG) embedded
- Data flow information
- Type information preserved
- Optimization-friendly

### 4. Optimization Passes

**Purpose:** Generate efficient WASM while preserving safety.

**Passes:**

#### Dead Code Elimination
```
Algorithm: Mark-and-sweep
1. Mark all reachable instructions
2. Sweep unmarked instructions
3. Update control flow
```

#### Constant Folding
```
Algorithm: Symbolic evaluation
1. Evaluate compile-time constants
2. Replace operations with results
3. Simplify control flow
```

#### Inlining
```
Algorithm: Heuristic-based
1. Identify inline candidates
2. Cost-benefit analysis
3. Inline small/hot functions
4. Update call sites
```

#### LINQ Optimization
```
Algorithm: Deforestation
1. Identify LINQ query chains
2. Fuse operations
3. Eliminate intermediate allocations
4. Generate optimized code
```

### 5. WASM Backend

**Purpose:** Lower IR to WASM MVP bytecode.

**Phases:**

#### Function Lowering
```
1. Map IR functions to WASM functions
2. Generate function signatures
3. Allocate WASM locals
4. Lower function bodies
```

#### Memory Layout
```
1. Compute struct layouts
2. Generate linear memory layout
3. Emit memory initialization
4. Create vtables for virtual dispatch
```

#### Control Flow Lowering
```
IR                  WASM
---                 ----
if-then-else    →   block + br_if
while-loop      →   loop + br_if
for-loop        →   loop + br_if
try-catch       →   block nesting
```

#### Code Generation
```
IR Instruction      WASM Instruction(s)
--------------      -------------------
Alloc(size)     →   i32.const size + call $malloc
Free(ptr)       →   local.get ptr + call $free
Load(ptr, off)  →   local.get ptr + i32.load offset=off
Store(ptr, v)   →   local.get ptr + local.get v + i32.store
Call(f, args)   →   [args] + call $f
Br(label)       →   br $label
Return(val)     →   local.get val + return
```

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
