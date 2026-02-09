# CRAB Manual Memory Model - Verified Manual Memory Management

## Overview

The Manual Memory Model provides **mathematically verified low-level memory control** for CRAB. It applies to code inside `manual{}` or `unsafe{}` blocks, offering the expressive power of Rust's unsafe blocks but with full mathematical verification and invisible ownership inference—no annotations required.

**Key Innovation**: Manual memory management with automatic verification. Developers get low-level control without the burden of manual proof or lifetime annotations.

## Architecture

The manual model processes code through a comprehensive 10-phase verification pipeline:

### Phase 1: Manual Block Extraction
- Identifies all `manual{}` and `unsafe{}` blocks in the AST
- Issues warning for `unsafe{}` recommending `manual{}` instead (per CRAB spec)
- Extracts pointer operations within each block

### Phase 2: Ownership Graph Construction
- Builds ownership graphs tracking which values own which memory regions
- **Ownership graph** has nodes (memory regions/pointers) and edges (ownership relationships)
- Tracks ownership status: Owned, Borrowed, Moved, Freed
- Records allocation and deallocation points

### Phase 3: Abstract Interpretation
- Models all possible program states at each program point
- Provides sound over-approximation of runtime behavior
- Tracks valid pointers and freed pointers at each point
- Uses abstract values to represent possible value sets

### Phase 4: Symbolic Execution
- Executes symbolically on all possible paths through the code
- Generates symbolic constraints for each path
- Verifies safety properties across all paths
- Detects potential violations before they can occur

### Phase 5: Alias Tracking
- Tracks all pointer aliases throughout the program
- Groups aliasing pointers together
- Detects conflicting mutable aliases
- Ensures safe aliasing patterns

### Phase 6: Escape Analysis
- Analyzes whether pointers escape manual blocks
- **Critical**: Manual pointers must NOT escape into automatic code
- Detects return values, assignments to outer scope, captures
- Enforces strict containment

### Phase 7: Memory Safety Verification
Mathematically proves the following properties:

1. **No Invalid Pointer Usage** - All dereferences are valid
2. **No Memory Leaks** - All allocated memory is freed
3. **No Use-After-Free** - No uses occur after deallocation
4. **No Double-Free** - Each pointer freed exactly once
5. **No Undefined Behavior** - All operations are well-defined
6. **Safe Aliasing** - No conflicting mutable aliases
7. **No Escapes** - Pointers stay within manual blocks

### Phase 8: Model Isolation Enforcement
- Enforces complete isolation between manual and automatic models
- **No cross-model aliasing** - Manual pointers can't alias automatic references
- **No ownership transfer** - Can't pass ownership between models
- **No lifetime dependencies** - Lifetimes are model-local
- This isolation is **mandatory** for global safety guarantees

### Phase 9: IR Generation
- Generates verified ManualIR with:
  - Original AST
  - Manual blocks with annotations
  - Ownership graphs
  - Verification metadata
- Ready for WASM lowering

## Core Components

### Ownership Graphs

Ownership graphs are the foundation of manual memory verification:

```
OwnershipGraph
├── Nodes (OwnershipNode)
│   ├── Id, Name
│   ├── Status (Owned/Borrowed/Moved/Freed)
│   ├── AllocationPoint
│   └── DeallocationPoint
└── Edges (OwnershipEdge)
    ├── Owns - Ownership relationship
    ├── Borrows - Non-owning reference
    └── Aliases - Alias relationship
```

**Example**:
```csharp
manual {
    var ptr = Marshal.AllocHGlobal(1024);  // Node: ptr (Owned)
    var alias = ptr;                        // Edge: alias Aliases ptr
    // use ptr and alias...
    Marshal.FreeHGlobal(ptr);              // ptr status → Freed
}
```

### Abstract Interpretation

Abstract interpretation tracks possible program states:

```
AbstractState (at each program point)
├── ProgramPoint (location in code)
├── Values (AbstractValue per variable)
│   ├── Name
│   ├── Kind (Integer/Pointer/Boolean/Unknown)
│   └── PossibleValues (set of possible values)
├── ValidPointers (currently valid)
└── FreedPointers (already deallocated)
```

Abstract interpretation provides **sound over-approximation**: if the analysis says a property holds, it definitely holds at runtime.

### Symbolic Execution

Symbolic execution explores all paths:

```
SymbolicExecutionResult (per path)
├── PathId (unique path identifier)
├── Constraints (SymbolicConstraint list)
│   ├── Expression (symbolic expression)
│   └── MustBeTrue (constraint polarity)
├── IsSafe (overall path safety)
└── SafetyViolations (detected issues)
```

**Example constraints**:
- `ptr != null` must be true before dereference
- `ptr not in FreedPointers` must be true before use
- `index >= 0 && index < length` for array access

### Alias Tracking

Alias tracking prevents conflicting aliases:

```
AliasInfo
├── AliasGroups (pointer → aliases mapping)
└── Violations (AliasingViolation list)
    ├── Pointer1, Pointer2
    └── Reason
```

**Violation example**: Two mutable pointers to the same memory without synchronization.

### Escape Analysis

Escape analysis enforces containment:

```
EscapeAnalysisResult
├── Violations (EscapeViolation list)
│   ├── PointerName
│   └── EscapeLocation
└── AllPointersContained (boolean)
```

Pointers escape through:
- Return statements
- Assignment to outer scope variables
- Closure capture
- Passing to functions that may store them

## Verification Guarantees

The manual model provides **mathematical proofs** of the following guarantees:

### 1. No Invalid Pointer Usage

**Property**: All pointer dereferences are valid

**Verification**:
- Check ownership graph: pointer must be in Owned or Borrowed state
- Check abstract state: pointer must be in ValidPointers set
- Check symbolic constraints: pointer != null on all paths
- Verify pointer hasn't been moved or freed

**Example Safe**:
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    Marshal.WriteInt32(ptr, 42);  // ✓ Valid: ptr is Owned
    Marshal.FreeHGlobal(ptr);
}
```

**Example Unsafe** (rejected):
```csharp
manual {
    IntPtr ptr = IntPtr.Zero;
    Marshal.WriteInt32(ptr, 42);  // ✗ Error: null pointer dereference
}
```

### 2. No Memory Leaks

**Property**: All allocated memory is freed

**Verification**:
- For each OwnershipNode with allocation
- Verify DeallocationPoint is set
- Trace all paths ensure deallocation is reachable

**Example Safe**:
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    // ... use ptr ...
    Marshal.FreeHGlobal(ptr);  // ✓ Freed before block exit
}
```

**Example Unsafe** (rejected):
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    // ... use ptr ...
    // ✗ Error: Memory leak - ptr never freed
}
```

### 3. No Use-After-Free

**Property**: No uses occur after deallocation

**Verification**:
- Track freed pointers in abstract state
- Symbolic execution verifies no dereference after free
- Check ownership graph: no operations when Status == Freed

**Example Safe**:
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    Marshal.WriteInt32(ptr, 42);  // ✓ Use before free
    Marshal.FreeHGlobal(ptr);
    // No further use
}
```

**Example Unsafe** (rejected):
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    Marshal.FreeHGlobal(ptr);
    Marshal.WriteInt32(ptr, 42);  // ✗ Error: Use after free
}
```

### 4. No Double-Free

**Property**: Each pointer freed at most once

**Verification**:
- Track freed pointers across all paths
- Verify no pointer appears in freed set twice
- Symbolic execution confirms single free per allocation

**Example Safe**:
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    Marshal.FreeHGlobal(ptr);  // ✓ Single free
}
```

**Example Unsafe** (rejected):
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    Marshal.FreeHGlobal(ptr);
    Marshal.FreeHGlobal(ptr);  // ✗ Error: Double free
}
```

### 5. No Undefined Behavior

**Property**: All operations are well-defined

**Verification**:
- Pointer arithmetic: verify in-bounds
- Type casts: verify safe conversions
- Buffer access: verify bounds checks
- Symbolic execution explores all paths

### 6. Safe Aliasing

**Property**: No conflicting mutable aliases

**Verification**:
- Alias tracking identifies all aliases
- Check for mutable aliases to same memory
- Ensure proper synchronization if needed

**Example Safe**:
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    IntPtr alias = ptr;
    // Read-only use of both - OK
    int val1 = Marshal.ReadInt32(ptr);
    int val2 = Marshal.ReadInt32(alias);
    Marshal.FreeHGlobal(ptr);
}
```

**Example Unsafe** (rejected):
```csharp
manual {
    IntPtr ptr = Marshal.AllocHGlobal(100);
    IntPtr alias = ptr;
    Marshal.WriteInt32(ptr, 42);    // Mutable use
    Marshal.WriteInt32(alias, 99);  // ✗ Error: Conflicting mutable alias
    Marshal.FreeHGlobal(ptr);
}
```

### 7. No Escapes

**Property**: Manual pointers don't escape manual blocks

**Verification**:
- Escape analysis tracks all pointer flows
- Verify no pointer escapes block boundary
- Critical for model isolation

**Example Safe**:
```csharp
void ProcessData() {
    manual {
        IntPtr buffer = Marshal.AllocHGlobal(1024);
        // ... process data in buffer ...
        Marshal.FreeHGlobal(buffer);
    }  // ✓ buffer doesn't escape
}
```

**Example Unsafe** (rejected):
```csharp
IntPtr escapedPtr;

void ProcessData() {
    manual {
        IntPtr buffer = Marshal.AllocHGlobal(1024);
        escapedPtr = buffer;  // ✗ Error: Pointer escapes manual block
        Marshal.FreeHGlobal(buffer);
    }
}
```

## Model Isolation

**Critical requirement**: Manual and automatic memory models **never interoperate**.

### Isolation Rules

1. **Manual pointers cannot escape manual blocks**
   - Prevents manual pointers from entering automatic code
   - Enforced by escape analysis

2. **Automatic references cannot enter manual blocks**
   - Prevents automatic references from being manually managed
   - Enforced by block boundary analysis

3. **No cross-model aliasing**
   - Manual pointer can't alias automatic reference
   - Automatic reference can't alias manual pointer
   - Verified by alias tracker

4. **No ownership transfer**
   - Can't transfer ownership between models
   - Each model maintains separate ownership domains

5. **No lifetime dependencies**
   - Manual lifetimes independent of automatic lifetimes
   - Prevents coupling between models

### Why Isolation Matters

Isolation ensures both models' safety proofs remain sound:
- **Automatic model** assumes CTGC manages all memory
- **Manual model** assumes explicit control
- **Mixing them** would invalidate both proofs

## Comparison to Other Systems

### vs. Rust's unsafe{}

| Aspect | Rust unsafe{} | CRAB manual{} |
|--------|---------------|---------------|
| Safety | Programmer responsibility | Mathematically verified |
| Annotations | Manual lifetime annotations | Automatic inference |
| Learning curve | High (borrow checker) | None (same C#) |
| Compile time | Fast | Slower (by design) |
| Verification | Unchecked | Full verification |

**CRAB advantage**: Safety without learning burden

### vs. C/C++ Manual Memory

| Aspect | C/C++ | CRAB manual{} |
|--------|-------|---------------|
| Safety | None | Mathematically proven |
| Leaks | Possible | Impossible (verified) |
| Use-after-free | Possible | Impossible (verified) |
| Double-free | Possible | Impossible (verified) |
| Undefined behavior | Common | Impossible (verified) |

**CRAB advantage**: Manual control with guaranteed safety

### vs. CRAB Automatic

| Aspect | CRAB Automatic | CRAB Manual |
|--------|----------------|-------------|
| Use case | Default (most code) | Low-level control |
| Compile time | Moderate | Slow (intentional) |
| Developer effort | None | None (verified) |
| Safety | Proven | Proven |
| Performance | Zero overhead | Zero overhead |
| Control | High-level | Low-level |

**Both guaranteed safe**, choose based on control needs.

## Performance Characteristics

### Compile-Time Complexity

- **Ownership Graph Construction**: O(n) where n = pointer operations
- **Abstract Interpretation**: O(n × d) where d = domain depth
- **Symbolic Execution**: O(2^b) where b = branches (path explosion)
  - Mitigated by MaxSymbolicPathDepth limit
  - Typically O(n × p) where p = number of paths
- **Alias Tracking**: O(n²) worst case for n pointers
- **Escape Analysis**: O(n × e) where e = escape points

**Total**: Intentionally slower than automatic model (by design)
**Goal**: Encourage developers to use automatic model for most code

### Runtime Performance

- **Zero overhead**: All verification at compile-time
- **No runtime checks**: Safety proven statically
- **Optimal code generation**: Direct WASM without indirection
- **Deterministic**: No garbage collection pauses

**Expected**: Match or exceed Rust and C++ manual memory performance

## Integration with CRAB

### Workflow

1. **Parse**: CDTk parses C# source including manual{} blocks
2. **Semantic Analysis**: Type checking, name resolution
3. **Memory Model Selection**:
   - Automatic model for code outside manual blocks
   - Manual model for code inside manual{} blocks
4. **Verification**: Each model verifies its code
5. **Isolation Check**: Verify no cross-model interactions
6. **IR Generation**: Both models produce verified IR
7. **WASM Lowering**: IR lowered to WASM MVP

### CDTk Integration

Manual model inherits from `Model` base class:
- `Build(object input)` processes AST
- Returns verified ManualIR
- Integrates seamlessly with compiler pipeline

### Diagnostics

Manual model provides detailed error messages:

```
Error: Memory leak detected
  Pointer 'buffer' allocated at line 15 but never freed
  
Error: Use-after-free detected
  Pointer 'ptr' freed at line 20 but used at line 22
  
Error: Escape violation
  Pointer 'data' escapes manual block at line 30
  Cannot return manual pointer from block
  
Error: Aliasing violation
  Pointers 'ptr1' and 'ptr2' are mutable aliases
  Conflicting write operations detected
```

## Example Scenarios

### Safe Manual Memory

```csharp
void ProcessLargeBuffer() {
    manual {
        // Allocate buffer
        IntPtr buffer = Marshal.AllocHGlobal(1024 * 1024);
        
        // Write data
        for (int i = 0; i < 1024; i++) {
            Marshal.WriteInt32(buffer, i * 4, i);
        }
        
        // Read data
        int sum = 0;
        for (int i = 0; i < 1024; i++) {
            sum += Marshal.ReadInt32(buffer, i * 4);
        }
        
        // Free buffer
        Marshal.FreeHGlobal(buffer);
    }
    // ✓ Verified safe: allocation, use, deallocation all correct
}
```

### Complex Pointer Operations

```csharp
void ComplexPointerManipulation() {
    manual {
        // Multiple allocations
        IntPtr buf1 = Marshal.AllocHGlobal(100);
        IntPtr buf2 = Marshal.AllocHGlobal(200);
        
        // Pointer arithmetic
        IntPtr offset = IntPtr.Add(buf1, 50);
        
        // Conditional free
        bool useBuf1 = SomeCondition();
        if (useBuf1) {
            Marshal.FreeHGlobal(buf1);
        } else {
            Marshal.FreeHGlobal(buf2);
        }
        
        // Free the other one
        if (!useBuf1) {
            Marshal.FreeHGlobal(buf1);
        } else {
            Marshal.FreeHGlobal(buf2);
        }
    }
    // ✓ Verified safe: symbolic execution proves both freed on all paths
}
```

### Legacy unsafe{} Support

```csharp
void LegacyCode() {
    unsafe {  // Legacy keyword
        // Warning: Use 'manual' keyword instead of 'unsafe'
        fixed (byte* ptr = buffer) {
            // ... pointer operations ...
        }
    }
    // ✓ Still verified, but warning issued
}
```

## Future Enhancements

Potential improvements:
1. **Region-based verification**: Group related pointers for faster verification
2. **Incremental verification**: Re-verify only changed code
3. **Counterexample generation**: Show concrete violation scenarios
4. **Automated repair**: Suggest fixes for common violations
5. **Performance profiling**: Identify slow verification paths

## Conclusion

The CRAB Manual Memory Model provides **verified manual memory management**: the low-level control of manual memory with the safety of automatic verification. Developers can write powerful low-level code without the burden of proof, annotations, or fear of undefined behavior.

**Three guarantees**:
1. ✅ **Safe**: Mathematically proven memory safety
2. ✅ **Powerful**: Full manual memory control
3. ✅ **Easy**: No annotations, automatic inference

The manual model represents a breakthrough in safe systems programming, offering expressive power beyond Rust's unsafe{} with guaranteed safety beyond any existing system.
