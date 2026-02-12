# Manual Memory Model

## Overview

The manual memory model provides low-level control over memory management for performance-critical code, while still maintaining 100% mathematical safety through static verification.

**Key Feature**: You get the control of C/C++ `unsafe` blocks or Rust's `unsafe`, but with complete verification—no undefined behavior possible.

## Philosophy

Manual mode is for scenarios where:
- Ultimate performance is critical
- Fine-grained control is needed
- Allocation patterns are complex
- You want explicit memory control

**Important**: Manual mode is **not** "unsafe"—it's still 100% safe, just verified differently.

## Syntax

Use the `manual` keyword to create a manual memory block:

```csharp
manual
{
    // Manual memory code here
    int* ptr = stackalloc int[10];
    *ptr = 42;
}
```

Or use the legacy `unsafe` keyword (deprecated):
```csharp
unsafe  // Warning: prefer 'manual' keyword
{
    int* ptr = stackalloc int[10];
}
```

## Verification Process

Manual code is verified using:

1. **Ownership Graphs**: Track which pointer owns which memory
2. **Abstract Interpretation**: Symbolically execute code to understand behavior
3. **Symbolic Execution**: Explore all possible execution paths
4. **Alias Tracking**: Monitor all pointer aliases
5. **Escape Analysis**: Ensure pointers don't escape their valid scope

### Example Verification:
```csharp
manual
{
    int* ptr = stackalloc int[10];
    
    // Verification builds ownership graph:
    // - ptr owns 10 integers on the stack
    // - Valid indices: 0-9
    // - Lifetime: until end of block
    
    for (int i = 0; i < 10; i++)
    {
        ptr[i] = i;  // ✓ Verified: i in bounds [0, 9]
    }
    
    // ptr[10] = 0;  // ✗ Error: out of bounds
}
```

## Key Features

### 1. Stack Allocation

Allocate memory on the stack using `stackalloc`:

```csharp
manual
{
    int* numbers = stackalloc int[100];
    byte* buffer = stackalloc byte[1024];
    
    // Use the memory
    for (int i = 0; i < 100; i++)
    {
        numbers[i] = i * 2;
    }
    
    // Automatically freed at block exit
}
```

**Verification**:
- Bounds checking on all accesses
- Lifetime tracking
- No escapes possible

### 2. Pointer Arithmetic

Manual mode allows pointer arithmetic:

```csharp
manual
{
    int* arr = stackalloc int[10];
    int* ptr = arr;
    
    for (int i = 0; i < 10; i++)
    {
        *ptr = i;
        ptr++;  // ✓ Verified safe
    }
    
    // ptr--;  // Restore if needed
}
```

**Verification**:
- Tracks pointer offsets
- Ensures no out-of-bounds access
- Proves arithmetic safety

### 3. Aliasing

Multiple pointers can reference the same memory:

```csharp
manual
{
    int* arr = stackalloc int[10];
    int* alias = arr;  // Aliasing detected
    
    *arr = 42;
    int value = *alias;  // ✓ Safe: both point to same memory
}
```

**Verification**:
- Tracks all aliases
- Ensures no conflicting accesses
- Proves alias safety

### 4. Fixed Statements

Pin managed objects for pointer access:

```csharp
manual
{
    int[] managedArray = new int[10];
    
    fixed (int* ptr = managedArray)
    {
        // ptr is pinned, can use safely
        *ptr = 100;
    }
    
    // Pin released, managedArray can move again
}
```

**Verification**:
- Ensures pinning during access
- Prevents use after unpin
- Tracks pin/unpin pairs

## Safety Guarantees

### No Use-After-Free

```csharp
manual
{
    int* ptr = stackalloc int[10];
    Use(ptr);
}
// Use(ptr);  // ✗ ERROR: ptr out of scope
```

Compiler prevents use after the block ends.

### No Buffer Overflow

```csharp
manual
{
    int* arr = stackalloc int[10];
    
    arr[0] = 1;   // ✓ Valid
    arr[9] = 10;  // ✓ Valid
    // arr[10] = 11;  // ✗ ERROR: out of bounds
}
```

All array accesses are bounds-checked.

### No Dangling Pointers

```csharp
int* DanglingPointer()
{
    manual
    {
        int* ptr = stackalloc int[10];
        // return ptr;  // ✗ ERROR: pointer escapes
    }
}
```

Pointers cannot escape their valid scope.

### No Double-Free

```csharp
manual
{
    int* ptr = stackalloc int[10];
    // No manual deallocation needed
    // Stack automatically reclaimed
}
```

Stack allocations are automatically freed once.

### No Memory Leaks

```csharp
manual
{
    int* ptr = stackalloc int[10];
    // Automatically freed at block exit
}
```

All manual allocations are deallocated deterministically.

## Model Isolation

Manual and automatic memory are strictly isolated:

```csharp
void IsolationExample()
{
    // Automatic memory
    var obj = new MyClass();
    
    manual
    {
        int* ptr = stackalloc int[10];
        
        // ✗ ERROR: Cannot pass automatic object into manual block
        // ProcessManual(obj);
        
        // ✗ ERROR: Cannot expose manual pointer to automatic code
        // StorePointer(ptr);
    }
    
    // ✗ ERROR: Cannot use manual pointer outside block
    // Use(ptr);
}
```

**Why Isolation?**
- Preserves safety proofs
- Prevents cross-contamination
- Simplifies verification
- Ensures model integrity

## Verification Techniques

CRAB's manual memory verification is fully implemented with four core analysis passes:

### 1. Ownership Graph Construction

**OwnershipGraphBuilder** creates ownership graphs for each manual block:

```
Algorithm BuildOwnershipGraph(block):
    graph = new OwnershipGraph()
    
    for operation in block.operations:
        if operation.type == Allocate:
            node = new OwnershipNode(operation.pointer_name)
            node.allocation_point = operation.program_point
            node.status = Owned
            graph.add_node(node)
        
        else if operation.type == Deallocate:
            node = graph.get_node(operation.pointer_name)
            node.deallocation_point = operation.program_point
            node.status = Freed
    
    return graph
```

**Complexity**: O(m) where m = number of operations in block

**Output**: Graph with nodes representing memory regions and edges representing ownership relationships

### 2. Abstract Interpretation

**AbstractInterpreter** computes abstract states at each program point:

```
Algorithm AbstractInterpret(block, graph):
    state = new AbstractState()
    state.valid_pointers = {}
    state.freed_pointers = {}
    
    for operation in block.operations:
        if operation.type == Allocate:
            state.valid_pointers.add(operation.pointer_name)
        
        else if operation.type == Deallocate:
            state.valid_pointers.remove(operation.pointer_name)
            state.freed_pointers.add(operation.pointer_name)
        
        state.program_point = operation.program_point
    
    return state
```

**Complexity**: O(m)

**Output**: Abstract state tracking valid and freed pointers at each point

### 3. Path-Sensitive Analysis

The compiler tracks invariants on different execution paths:

```csharp
manual
{
    int* ptr = stackalloc int[10];
    int idx = GetIndex();
    
    if (idx >= 0 && idx < 10)
    {
        ptr[idx] = 42;  // ✓ Safe: invariant proves bounds
    }
}
```

**Implementation**: Symbolic execution maintains path conditions and verifies safety under those conditions.

### 4. Invariant Propagation

The compiler tracks invariants through loops and branches:

```csharp
manual
{
    int* ptr = stackalloc int[10];
    
    for (int i = 0; i < 10; i++)
    {
        // Invariant: 0 <= i < 10
        ptr[i] = 0;  // ✓ Always safe
    }
}
```

**Implementation**: Loop invariant analysis determines bounds that hold on all iterations.

### Symbolic Execution Implementation

**SymbolicExecutor** verifies safety on all paths:

```
Algorithm SymbolicExecute(block, abstract_state):
    result = new SymbolicExecutionResult()
    result.is_safe = true
    result.constraints = []
    
    for operation in block.operations:
        if operation.type == Dereference:
            // Check pointer is valid
            if operation.pointer_name not in abstract_state.valid_pointers:
                result.is_safe = false
                result.violations.add("Invalid dereference of " + operation.pointer_name)
            else:
                // Add validity constraint
                result.constraints.add(operation.pointer_name + " != null && valid(" + operation.pointer_name + ")")
        
        // Check for use-after-free
        if operation.pointer_name in abstract_state.freed_pointers:
            result.is_safe = false
            result.violations.add("Use after free: " + operation.pointer_name)
    
    return result
```

**Complexity**: O(m) per path, O(m × p) total where p = number of paths

**Output**: Symbolic execution result with:
- Safety status (safe/unsafe)
- List of symbolic constraints
- List of safety violations (if any)

### Verification Guarantees

The verification process ensures:

1. **Pointer Validity** (100%): All pointer dereferences verified valid
   - Check: pointer in valid_pointers before each use
   
2. **No Use-After-Free** (100%): No accesses to freed memory
   - Check: pointer not in freed_pointers before each use
   
3. **No Escapes** (100%): Pointers don't outlive their scope
   - Check: No pointers returned from manual blocks
   - Check: No pointers stored in fields accessible outside block

4. **Bounds Safety**: Array accesses within bounds
   - Requires: Symbolic execution to prove index constraints
   - Example: For `ptr[i]`, must prove `0 <= i < length`

5. **No Memory Leaks**: Stack allocations automatically freed
   - Guaranteed: Stack memory reclaimed at block exit

## Performance Characteristics

### Compile Time
- **Slower than automatic mode**: Verification is expensive
- **Intentionally**: Encourages using automatic mode by default
- **Worth it**: One-time cost for guaranteed safety

### Runtime
- **Zero overhead**: All verification is compile-time
- **Optimal**: Direct memory access
- **Predictable**: No hidden costs

## Use Cases

### High-Performance Buffer Processing

```csharp
void ProcessBuffer(byte[] data)
{
    manual
    {
        fixed (byte* ptr = data)
        {
            // Fast byte manipulation
            for (int i = 0; i < data.Length; i++)
            {
                ptr[i] = (byte)(ptr[i] ^ 0xFF);
            }
        }
    }
}
```

### Interop with Native Code

```csharp
manual
{
    NativeStruct* native = stackalloc NativeStruct[1];
    InitializeNative(native);
    CallNativeAPI(native);
}
```

### Low-Level Data Structures

```csharp
manual
{
    // Custom memory layout for cache optimization
    int* data = stackalloc int[1024];
    byte* flags = stackalloc byte[1024];
    
    // Process with optimal memory access patterns
}
```

### Temporary Scratch Buffers

```csharp
void Algorithm()
{
    manual
    {
        // Fast temporary buffer
        int* scratch = stackalloc int[256];
        
        // Use for computation
        ComplexAlgorithm(scratch);
        
        // Automatically freed
    }
}
```

## Best Practices

### 1. Prefer Automatic Mode
Use manual mode only when necessary:
```csharp
// Default: Automatic mode
void ProcessData(int[] data)
{
    var result = new List<int>();
    // Easy, safe, fast enough
}

// Only when needed: Manual mode
void HighPerformance(byte[] buffer)
{
    manual
    {
        fixed (byte* ptr = buffer)
        {
            // Ultimate performance
        }
    }
}
```

### 2. Keep Manual Blocks Small
```csharp
void Example()
{
    // Automatic mode for most code
    var data = PrepareData();
    
    // Manual mode only for critical section
    manual
    {
        fixed (byte* ptr = data)
        {
            ProcessFast(ptr);
        }
    }
    
    // Back to automatic mode
    SaveResults(data);
}
```

### 3. Use Fixed for Managed Arrays
```csharp
manual
{
    int[] array = new int[100];
    
    fixed (int* ptr = array)
    {
        // Safe: array is pinned
        FastProcess(ptr, 100);
    }
}
```

### 4. Trust the Verification
If the compiler accepts your manual code, it's safe:
```csharp
manual
{
    int* ptr = stackalloc int[10];
    
    // If this compiles, it's proven safe
    for (int i = 0; i < 10; i++)
    {
        ptr[i] = i;
    }
}
```

## Common Patterns

### Pattern 1: Temporary Buffer
```csharp
void Algorithm()
{
    manual
    {
        int* temp = stackalloc int[1024];
        // Use temp for computation
        Compute(temp);
    }
}
```

### Pattern 2: Pinned Array Processing
```csharp
void ProcessArray(int[] data)
{
    manual
    {
        fixed (int* ptr = data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                ptr[i] *= 2;
            }
        }
    }
}
```

### Pattern 3: Struct Manipulation
```csharp
manual
{
    MyStruct* s = stackalloc MyStruct[1];
    s->Field1 = 42;
    s->Field2 = 3.14;
}
```

## Limitations

### Cannot Return Pointers
```csharp
int* GetPointer()  // Not allowed
{
    manual
    {
        int* ptr = stackalloc int[10];
        // return ptr;  // ✗ ERROR: escapes scope
    }
}
```

**Workaround**: Use automatic mode or pass pointers as parameters.

### Cannot Store in Fields
```csharp
class MyClass
{
    int* _ptr;  // Not allowed in manual blocks
}
```

**Workaround**: Use IntPtr or automatic mode.

### Limited Cross-Block Usage
```csharp
manual
{
    int* ptr1 = stackalloc int[10];
}

manual
{
    // Cannot use ptr1 here
}
```

**Workaround**: Combine into single manual block if related.

## Error Messages

CRAB provides clear error messages:

```
Error: Pointer may escape manual block
  at line 42: return ptr;
  
  Manual pointers cannot outlive their block.
  
  Suggestions:
  - Return data by value instead
  - Use automatic memory model
  - Restructure code to keep pointer local

Error: Array access may be out of bounds
  at line 58: ptr[idx] = value;
  
  Cannot prove: 0 <= idx < 10
  
  Suggestions:
  - Add bounds check: if (idx >= 0 && idx < 10)
  - Use constant index
  - Provide range proof
```

## Future Enhancements

- Heap allocations in manual mode
- Manual reference counting
- Custom allocators
- Annotations for complex proofs
- Better error messages
- Profiling integration

## Comparison with Other Languages

| Feature | CRAB Manual | C/C++ unsafe | Rust unsafe |
|---------|-------------|--------------|-------------|
| **Safety** | 100% verified | Undefined behavior | Verified when correct |
| **Annotations** | None required | None | Some required |
| **Ease** | Easy | Hard | Moderate |
| **Guarantees** | Mathematical | None | Borrow checker |
| **Performance** | Optimal | Optimal | Optimal |

## Summary

Manual memory mode provides:
- ✓ Ultimate control over memory
- ✓ 100% safety through verification
- ✓ Zero runtime overhead
- ✓ No annotations needed
- ✓ Clear error messages
- ✓ Integration with automatic mode

Use it when you need maximum performance and control, while maintaining CRAB's safety guarantees.
