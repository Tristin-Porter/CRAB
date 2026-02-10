# CRAB Automatic Memory Model (CTGC)

## Overview

The Automatic Memory Model implements **Compile-Time Garbage Collection (CTGC)** for CRAB. CTGC is a static analysis and transformation system that resolves all memory allocations, deallocations, and lifetimes during compilation—completely eliminating the need for runtime garbage collection.

**Key Principle**: What would normally happen at runtime in a garbage-collected language (like C#) is instead proven and inserted at compile-time, resulting in zero runtime overhead while maintaining complete memory safety.

## Architecture

The automatic model processes code through a multi-phase pipeline:

### Phase 1: Lifetime Inference
- Analyzes data flow to determine when every value is created (birth) and last used (death)
- Builds a **LifetimeGraph** that captures lifetime dependencies between values
- Uses flow-sensitive analysis to track values through the program

### Phase 2: Region Analysis
- Groups allocations with similar lifetimes into **MemoryRegions**
- Enables bulk deallocation and memory layout optimization
- Similar to region-based memory management in Mercury and MLKit

### Phase 3: Allocation Tracking
- Identifies every allocation site in the program:
  - `new` expressions
  - Array allocations
  - Delegate/lambda captures
  - Boxing operations
  - String concatenations
  - LINQ intermediate collections
- Tracks metadata: type, size, scope, escaping behavior

### Phase 4: Deallocation Computation
- Computes the optimal point to deallocate each allocation
- Uses liveness analysis to place deallocations as early as safely possible
- Chooses deallocation strategy:
  - **Immediate**: Right after last use (most efficient)
  - **Regional**: Bulk deallocation with region (optimizes locality)
  - **Deferred**: At scope exit (safest for complex patterns)

### Phase 5: Safety Verification
Mathematically proves the following guarantees:

1. **No Memory Leaks**: Every allocation has a corresponding deallocation
2. **No Use-After-Free**: No uses occur after deallocation
3. **No Double-Free**: Each allocation is deallocated exactly once
4. **No Dangling Pointers**: No pointers outlive their pointees
5. **No Aliasing Violations**: Mutable aliases are tracked and verified safe

### Phase 6: AST Annotation
- Annotates AST with memory management metadata
- Original AST structure + allocation metadata + deallocation point markers
- Output is used by MapSet for direct WASM generation with deterministic memory behavior

## Advanced Features

### Escape Analysis
Determines if allocations escape their creation scope:
- Returned from functions → escapes
- Assigned to fields/globals → escapes
- Captured by lambdas/delegates → may escape
- Passed to functions that store them → may escape

Escaped allocations require different deallocation strategies.

### Inter-Procedural Analysis
- Builds function summaries describing memory behavior
- Tracks whether parameters escape
- Determines return value lifetime relationships
- Enables precise analysis across function boundaries

### Async/Await Analysis
- Handles memory across `await` suspension points
- Identifies variables that become state machine fields
- Ensures no dangling references when async operations complete
- Critical for async-heavy modern C# code

### LINQ Analysis
- Tracks intermediate collections created by LINQ operators
- Distinguishes deferred vs. immediate execution
- Optimizes deallocation of intermediate enumerables
- Example: `list.Where(x => x > 0).Select(x => x * 2).ToList()`
  - Tracks intermediate `IEnumerable` instances
  - Deallocates after materialization

### Delegate/Lambda Capture Analysis
- Identifies variables captured by closures
- Determines if captured variables must be heap-allocated
- Extends lifetime of captured values to match delegate lifetime
- Handles complex scenarios like nested lambdas

### Generics Analysis
- Handles different allocation patterns for value vs. reference type arguments
- `List<int>` allocates differently than `List<string>`
- Ensures correctness across generic instantiations

## Memory Safety Guarantees

The automatic model provides **mathematical proofs** of memory safety, verified at compile-time:

### Leak Freedom
```csharp
void Example() {
    var obj = new MyClass();  // Allocation tracked
    obj.DoWork();
    // Deallocation inserted here (after last use)
}
// CTGC proves: obj is deallocated exactly once, no leak
```

### Use-After-Free Prevention
```csharp
void Example() {
    var obj = new MyClass();
    var ptr = obj;
    obj.DoWork();
    // Deallocation of obj here
    // ptr.DoWork();  // ERROR: Use after free detected at compile-time
}
```

### Lifetime Constraints
```csharp
MyClass GetObject() {
    var obj = new MyClass();
    return obj;  // Escapes - deallocation deferred to caller
}

void Use() {
    var obj = GetObject();
    obj.DoWork();
    // Deallocation here
}
```

## Comparison to Traditional GC

| Aspect | Traditional GC | CRAB CTGC |
|--------|---------------|-----------|
| When deallocations happen | Runtime (unpredictable) | Compile-time determined |
| Performance overhead | Scanning, marking, sweeping | Zero runtime overhead |
| Pause times | Unpredictable GC pauses | No pauses (deterministic) |
| Memory leaks | Possible if references held | Impossible (proven) |
| Safety guarantees | Best effort | Mathematical proof |
| Use-after-free | Prevented by GC | Impossible (proven) |

## Comparison to Manual Memory Management

| Aspect | Manual (C/C++/Rust) | CRAB CTGC |
|--------|---------------------|-----------|
| Developer burden | High (explicit free/drop) | None (automatic) |
| Safety | Depends on developer | Guaranteed (proven) |
| Lifetime annotations | Required (Rust) | Inferred automatically |
| Learning curve | Steep | None (same C# syntax) |
| Compile time | Fast | Slower (analysis overhead) |

## Performance Characteristics

### Compile Time
- **Automatic mode**: Moderate analysis cost
  - Lifetime inference: O(n log n) where n = variables
  - Region analysis: O(n²) worst case for complex programs
  - Safety verification: O(n + e) where e = edges in lifetime graph

**Trade-off**: Slower compilation for zero runtime overhead

### Runtime
- **Zero overhead**: All decisions made at compile time
- Deterministic deallocation timing
- No GC pauses
- Predictable memory usage
- Expected to match or exceed Rust/C++ performance

## Integration with CRAB

The automatic model is the **default** memory model for CRAB:
- Applies to all code outside `manual { }` blocks
- Transparent to developers (same C# syntax)
- Integrates with CDTk's Model system
- Annotates AST for MapSet to generate WASM with memory management

## Isolation from Manual Model

Per CRAB specification, automatic and manual memory **never interoperate**:
- Manual pointers cannot escape manual blocks
- Automatic references cannot enter manual blocks
- No cross-model aliasing or ownership transfer
- Ensures both models' safety proofs remain sound

## Future Enhancements

Planned improvements:
1. **Ownership inference**: More precise tracking of unique ownership
2. **Move semantics**: Optimize by moving instead of copying
3. **Pool allocation**: Group similar-sized allocations
4. **Whole-program optimization**: Cross-module analysis
5. **Parallel compilation**: Parallelize analysis phases

## Technical Details

### Data Structures

#### LifetimeGraph
- Nodes: Values with birth/death points
- Edges: Lifetime dependencies (outlives, aliases, contains)
- Enables precise liveness computation

#### MemoryRegion
- Groups allocations with overlapping lifetimes
- Start/end points define region scope
- Enables bulk deallocation

#### AllocationSite
- Tracks individual allocations
- Metadata: type, size, region, lifetime, escape status
- Links to deallocation point

#### DeallocationPoint
- Computed insertion point for deallocation
- Strategy: immediate, regional, or deferred
- Links back to allocation

### Algorithms

#### Lifetime Inference
Uses a forward data-flow analysis:
1. Initialize all values with unknown lifetimes
2. Propagate birth points from declarations
3. Propagate death points from last uses
4. Iterate until fixed point

#### Region Analysis
Uses scope-based grouping:
1. Partition values by lexical scope
2. Merge overlapping scopes
3. Compute region boundaries
4. Assign allocations to regions

#### Safety Verification
Uses constraint solving:
1. Generate constraints from lifetime graph
2. Check for violations:
   - ∀ alloc: ∃ dealloc (no leaks)
   - ∀ use, dealloc: use < dealloc (no use-after-free)
   - ∀ dealloc₁, dealloc₂: dealloc₁ ≠ dealloc₂ (no double-free)
3. Emit diagnostics for violations

## Examples

### Simple Automatic Memory
```csharp
void SimpleExample() {
    var list = new List<int>();  // Allocation
    list.Add(1);
    list.Add(2);
    Console.WriteLine(list.Count);
    // Automatic deallocation inserted here
}
```

### Escaping Allocation
```csharp
List<int> CreateList() {
    var list = new List<int>();  // Escapes via return
    list.Add(1);
    return list;  // Caller responsible for deallocation
}
```

### LINQ with Intermediates
```csharp
void LinqExample(List<int> numbers) {
    var result = numbers
        .Where(x => x > 0)      // Intermediate 1
        .Select(x => x * 2)     // Intermediate 2
        .ToList();              // Final materialization
    // Intermediates deallocated here
    Console.WriteLine(result.Count);
    // Result deallocated here
}
```

### Async/Await
```csharp
async Task AsyncExample() {
    var data = new MyData();       // Becomes state machine field
    await Task.Delay(100);         // Suspension point
    Console.WriteLine(data.Value); // Safe after resumption
    // Deallocation after async completion
}
```

### Lambda Captures
```csharp
Action CreateClosure() {
    var captured = new MyClass();  // Heap-allocated for capture
    return () => captured.DoWork(); // Captured by lambda
    // Deallocation when Action is disposed
}
```

## Diagnostics

The automatic model provides detailed compile-time diagnostics:

### Leak Detection
```
Error: Memory leak detected: 3 allocation(s) without deallocation
  at Foo.cs:15 - 'new MyClass()' never deallocated
  at Foo.cs:23 - 'new Buffer()' never deallocated  
  at Foo.cs:31 - 'new List<int>()' never deallocated
```

### Use-After-Free
```
Error: Use-after-free detected
  at Foo.cs:42 - deallocation at program point 15
              but last use at program point 18
  Variable: 'myObject'
```

### Double-Free
```
Error: Double-free detected
  Allocation 'buffer' (Foo.cs:50) deallocated multiple times:
    - First deallocation at Foo.cs:55
    - Second deallocation at Foo.cs:58
```

## Conclusion

The CRAB Automatic Memory Model (CTGC) provides the convenience of garbage-collected C# with the performance and predictability of manual memory management—all with **zero runtime overhead** and **mathematical safety guarantees**.

It represents a third way between traditional garbage collection and manual memory management, offering the best of both worlds for modern WebAssembly development.
