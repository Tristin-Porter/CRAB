# CTGC: Compile-Time Garbage Collection

## Overview

CTGC (Compile-Time Garbage Collection) is CRAB's automatic memory management model. It provides the convenience of garbage-collected C# without any runtime overhead by resolving all memory management decisions at compile time.

**Inspiration**: Similar to the Mercury programming language's CTGC system.

## Core Concept

Instead of using a runtime garbage collector that traces and scans memory during program execution, CTGC:
1. **Analyzes** code at compile time to understand object lifetimes
2. **Inserts** deterministic deallocation instructions directly into the code
3. **Proves** mathematically that the program is memory-safe

Result: **Zero runtime overhead** with **guaranteed safety**.

## How It Works

### 1. Lifetime Inference

CTGC uses flow-sensitive analysis to track when every value is created and when it's last used.

#### Example:
```csharp
void Example()
{
    var x = new MyClass();  // x created here
    Use(x);                 // x used here
    Use(x);                 // x last used here
    // x can be deallocated here
}
```

**Analysis**:
- Track all variable assignments
- Track all variable uses
- Identify last use point
- Insert deallocation after last use

### 2. Region Analysis

Objects with similar lifetimes are grouped into memory regions for efficient management.

#### Example:
```csharp
void Example()
{
    var a = new Object();  // Region 1
    var b = new Object();  // Region 1
    var c = new Object();  // Region 1
    
    Use(a, b, c);
    // All deallocated together - bulk operation
}
```

**Benefits**:
- Reduced deallocation overhead
- Better cache locality
- Optimized memory layout

### 3. Allocation Tracking

Every memory allocation is tracked:

```csharp
var obj = new MyClass();          // Object allocation
var arr = new int[100];           // Array allocation
var del = new Action(() => {});   // Delegate allocation
var str = "hello";                // String allocation (immutable)
```

**Each tracked with**:
- Allocation site (source location)
- Type information
- Size requirements
- Lifetime bounds

### 4. Deallocation Placement

CTGC computes optimal deallocation points:

```csharp
void Complex()
{
    var x = new Object();
    
    if (condition)
    {
        Use(x);  // x used here
    }
    else
    {
        Use(x);  // x also used here
    }
    
    // Deallocation placed here (after all possible uses)
}
```

**Placement Rules**:
- After last use in all control flow paths
- Before function returns
- At exception boundaries
- Respects scope boundaries

### 5. Safety Verification

CTGC proves several safety properties:

#### No Memory Leaks
```csharp
void NoLeak()
{
    var x = new Object();
    // CTGC ensures x is deallocated
}
```

Every allocation has a corresponding deallocation.

#### No Use-After-Free
```csharp
void Safe()
{
    var x = new Object();
    Use(x);
    // x deallocated here
    
    // Use(x);  // ERROR: Compiler prevents this
}
```

Compiler prevents any use after deallocation.

#### No Double-Free
```csharp
void Safe()
{
    var x = new Object();
    // CTGC deallocates exactly once
}
```

Each object is deallocated exactly once.

#### No Aliasing Violations
```csharp
void Safe()
{
    var x = new Object();
    var y = x;  // Aliasing detected
    
    Use(x);
    Use(y);
    // Deallocation only after both stop being used
}
```

Aliasing is tracked to prevent premature deallocation.

## Advanced Features

### Lambda Closures

When lambdas capture variables, CTGC analyzes whether the lambda outlives its creation scope:

```csharp
Action Example()
{
    var captured = new Object();
    
    // Lambda captures 'captured'
    // CTGC detects lambda escapes scope
    // 'captured' must be heap-allocated with extended lifetime
    return () => Use(captured);
}
```

**Analysis**:
- Identify captured variables
- Determine lambda lifetime
- Extend captured variable lifetimes accordingly
- Or: Prevent lambda from escaping if unsafe

### Generic Type Instantiations

CTGC handles generic types based on type arguments:

```csharp
T Create<T>() where T : new()
{
    return new T();
}

void Example()
{
    var obj = Create<MyClass>();  // Tracked allocation
    Use(obj);
    // Deallocation inserted
}
```

**Analysis**:
- Track generic instantiations
- Analyze based on actual type arguments
- Value types vs reference types
- Different lifetime patterns per instantiation

### Delegate Allocations

Delegates are objects that require special handling:

```csharp
void Example()
{
    Action del = Method;        // Delegate to static method
    Action del2 = obj.Method;   // Delegate to instance method
    Action del3 = () => {};     // Lambda delegate
    
    // All tracked and deallocated appropriately
}
```

### Exception Handling

CTGC respects exception boundaries:

```csharp
void Example()
{
    var x = new Object();
    try
    {
        var y = new Object();
        MightThrow();
        // y deallocated here on normal path
    }
    catch
    {
        // y also deallocated on exception path
    }
    // x deallocated here
}
```

**Guarantee**: Objects are deallocated on all paths (normal and exceptional).

## Implementation Strategy

CRAB's CTGC is fully implemented with the following components:

### Phase 1: AST Analysis
**AllocationTracker** class traverses the AST to find all allocation sites:
- **Algorithm**: Recursive descent through AST nodes
- **Pattern Matching**:
  ```
  if node.Type contains "NewExpression" or "ObjectCreation":
      create AllocationSite for object
  else if node.Type contains "ArrayCreation":
      create AllocationSite for array
  else if node.Type contains "Lambda" or "Delegate":
      create AllocationSite for closure
  else if node.Type contains "StringLiteral":
      create AllocationSite for string
  ```
- **Metadata Captured**: Type name, estimated size, AST node reference
- **Complexity**: O(n) where n = number of AST nodes

### Phase 2: Lifetime Computation
**LifetimeInferenceVisitor** builds lifetime graph:
- Creates lifetime nodes for each allocation
- Assigns birth points (program point where created)
- Tracks all uses to determine death points (last use)
- Builds dependency edges (outlives, aliases, contains)
- **Complexity**: O(n + e) where n = nodes, e = edges

### Phase 3: Region Assignment
**RegionAnalyzer** groups allocations:
- Groups allocations by scope name
- Computes region start/end points from constituent lifetimes
- Enables bulk deallocation optimization
- **Complexity**: O(n log n) for grouping and sorting

### Phase 4: Deallocation Insertion
**DeallocationComputer** determines optimal deallocation points:
```csharp
for each allocation:
    deallocation_point = allocation.lifetime.death_point + 1
    
    if optimization_enabled and allocation.region exists:
        strategy = Regional  // Bulk deallocation
    else if allocation.is_escaping:
        strategy = Deferred  // Delay until safe
    else:
        strategy = Immediate  // Deallocate ASAP
```
- **Complexity**: O(n)

### Phase 5: Verification
**MemorySafetyVerifier** proves safety through five checks:

1. **No Leaks** (O(n)):
   ```
   allocated_ids = {allocation.id for all allocations}
   deallocated_ids = {dealloc.allocation.id for all deallocations}
   leaks = allocated_ids - deallocated_ids
   assert leaks is empty
   ```

2. **No Use-After-Free** (O(n)):
   ```
   for each deallocation:
       assert deallocation.point > allocation.lifetime.death_point
   ```

3. **No Double-Free** (O(n log n)):
   ```
   seen = set()
   for deallocation in sorted_by_program_point:
       assert deallocation.allocation.id not in seen
       seen.add(deallocation.allocation.id)
   ```

4. **No Dangling Pointers** (O(e)):
   ```
   for edge in lifetime_graph.edges where edge.type == Outlives:
       from = lifetime_graph.get_node(edge.from)
       to = lifetime_graph.get_node(edge.to)
       assert from.death_point <= to.death_point
   ```

5. **No Aliasing Violations** (O(a)):
   ```
   for edge in lifetime_graph.edges where edge.type == Aliases:
       // Verify both aliases have compatible lifetimes
       // Ensure no conflicting accesses
   ```

### Phase 6: Annotation Generation
**AutomaticAnnotator** produces final metadata:
- Original AST preserved
- List of all allocations with metadata
- List of deallocation points with strategies
- Diagnostic messages from verification
- Metadata dictionary with statistics

**Total Complexity**: O(n log n) dominated by region analysis and double-free checking

## Comparison with Runtime GC

| Feature | CTGC (Compile-Time) | Runtime GC |
|---------|---------------------|------------|
| **Overhead** | Zero runtime overhead | Continuous tracing/scanning |
| **Pauses** | No GC pauses | Unpredictable pauses |
| **Determinism** | Fully deterministic | Non-deterministic |
| **Memory Usage** | Optimal (precise lifetimes) | Overheads for tracking |
| **Compile Time** | Higher (analysis cost) | Lower |
| **Safety** | Proven at compile time | Checked at runtime |
| **Performance** | Consistent | Variable |

## Limitations and Trade-offs

### Compile-Time Cost
- CTGC analysis increases compilation time
- Complex programs require more analysis
- Trade-off: Compile once, run fast forever

### Recursion
- Recursive structures need special handling
- Lifetime analysis is more complex
- May require developer annotations (future)

### Separate Compilation
- Cross-module analysis needed
- Interface contracts important
- Whole-program optimization beneficial

## Developer Experience

### No Annotations Required
```csharp
// Just write normal C# code
void Example()
{
    var x = new MyClass();
    x.DoSomething();
    // No need to free, no need to track
}
```

CTGC works invisibly—developers write normal C# code without thinking about memory management.

### Clear Error Messages

When CTGC detects potential issues:
```
Error: Object 'x' may escape function boundary
  at line 42: return x;
  
  Consider:
  - Returning a copy instead
  - Using manual memory model
  - Restructuring object ownership
```

### Performance Transparency

CTGC provides visibility into memory behavior:
```bash
crab compile --verbose MyProgram.cs

Memory Analysis:
  - 47 allocations detected
  - 47 deallocations inserted
  - 12 regions created
  - 0 potential leaks
  - 0 potential use-after-free
  
✓ Memory safety verified
```

## Best Practices

### 1. Prefer Automatic Mode
Use automatic mode (CTGC) by default. It's safe, fast, and easy.

### 2. Keep Lifetimes Simple
Simple, structured code is easier for CTGC to analyze:
```csharp
// Good: Simple lifetime
void Process()
{
    var data = LoadData();
    Transform(data);
    Save(data);
}
```

### 3. Avoid Unnecessary Allocations
```csharp
// Better: Reuse objects when possible
var buffer = new byte[1024];
for (int i = 0; i < 1000; i++)
{
    buffer.Clear();
    Process(buffer);
}
```

### 4. Trust the Compiler
Don't try to outsmart CTGC—it knows best when to deallocate.

## Future Enhancements

- Incremental CTGC analysis
- Cross-module optimization
- Better region heuristics
- Escape analysis improvements
- User-controllable region hints
- Profiling-guided optimization

## References

- Mercury CTGC: https://www.mercurylang.org/
- Region-based memory management research
- Compile-time reference counting techniques
- Linear type systems
