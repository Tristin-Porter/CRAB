# CRAB Memory Models

CRAB provides two distinct memory models, each with different trade-offs and guarantees.

## Automatic Memory Model - CTGC

**Compile-Time Garbage Collection (CTGC)** is CRAB's default memory model, inspired by the Mercury programming language.

### How It Works

1. **Lifetime Inference**: Compiler determines when each value is created and last used
2. **Region Analysis**: Groups allocations with similar lifetimes
3. **Allocation Tracking**: Identifies all memory allocations in the program
4. **Deallocation Computation**: Calculates optimal deallocation points
5. **Safety Verification**: Mathematically proves memory safety
6. **AST Annotation**: Annotates AST with deallocation point markers for MapSet

### Safety Guarantees

✅ **No Memory Leaks** - Every allocation has exactly one deallocation  
✅ **No Use-After-Free** - No uses occur after deallocation  
✅ **No Double-Free** - Each allocation freed exactly once  
✅ **No Dangling Pointers** - Pointers never outlive their pointees  
✅ **No Aliasing Violations** - Mutable aliases tracked and verified

### Performance

- **Compile-Time**: O(n log n) typical, O(n²) worst case
- **Runtime**: Zero overhead - all decisions at compile-time
- **Memory**: Minimal - deallocations at earliest safe point

### Example

```csharp
void ProcessData()
{
    // Automatic allocation
    var data = new DataProcessor();
    
    // Use data
    var result = data.Process();
    
    // Deallocation inserted here automatically
    // Compiler proved: no leaks, no use-after-free
}
```

### Advanced Features

**Escape Analysis**: Detects when allocations escape their scope
```csharp
DataProcessor CreateProcessor()
{
    var proc = new DataProcessor();
    return proc;  // Escapes - caller becomes owner
}
```

**LINQ Optimization**: Efficient intermediate allocation management
```csharp
var result = numbers
    .Where(x => x > 0)      // Intermediate 1
    .Select(x => x * 2)     // Intermediate 2
    .ToList();              // Final materialization
// Intermediates deallocated, result remains
```

**Async/Await**: Safe across suspension points
```csharp
async Task<Data> FetchAsync()
{
    var client = new HttpClient();  // Becomes state field
    await Task.Delay(100);          // Safe across suspension
    return await client.GetAsync(); // Safe after resumption
}
```

## Manual Memory Model

**Manual Memory** allows low-level memory control with mathematical verification.

### How It Works

1. **Block Extraction**: Identifies manual{} and unsafe{} blocks
2. **Ownership Graph**: Tracks ownership relationships
3. **Abstract Interpretation**: Models all possible program states
4. **Symbolic Execution**: Verifies properties on all paths
5. **Alias Tracking**: Identifies and verifies pointer aliases
6. **Escape Analysis**: Ensures pointers don't escape blocks
7. **Safety Verification**: Mathematically proves 7 safety properties
8. **Model Isolation**: Enforces separation from automatic model

### Safety Guarantees

✅ **No Invalid Pointer Usage** - All dereferences proven valid  
✅ **No Memory Leaks** - All allocations freed on all paths  
✅ **No Use-After-Free** - No uses after deallocation  
✅ **No Double-Free** - Each pointer freed at most once  
✅ **No Undefined Behavior** - All operations well-defined  
✅ **Safe Aliasing** - No conflicting mutable aliases  
✅ **No Escapes** - Manual pointers don't escape blocks

### Performance

- **Compile-Time**: Slower by design (O(2^b) mitigated to O(n×p))
- **Runtime**: Zero overhead - optimal memory operations
- **Memory**: Matches hand-written C/C++

### Example

```csharp
void ProcessBuffer()
{
    manual {
        // Allocate buffer
        IntPtr buffer = Marshal.AllocHGlobal(1024);
        
        // Process data
        ProcessRawData(buffer, 1024);
        
        // Free buffer
        Marshal.FreeHGlobal(buffer);
    }
    // ✓ Compiler verified all 7 safety properties
}
```

### Advanced Verification

**Ownership Graphs**: Track pointer relationships
```
buffer (Owned) ──owns──> memory region
buffer (Freed)  ──X──>  (invalid)
```

**Symbolic Execution**: Verify all paths
```csharp
manual {
    IntPtr buf = Marshal.AllocHGlobal(100);
    
    if (condition) {
        Process1(buf);
        Marshal.FreeHGlobal(buf);
    } else {
        Process2(buf);
        Marshal.FreeHGlobal(buf);
    }
    // ✓ Verified: freed on both paths
}
```

**Escape Prevention**:
```csharp
IntPtr escaped;

void TryEscape() {
    manual {
        IntPtr ptr = Marshal.AllocHGlobal(100);
        escaped = ptr;  // ✗ Error: Pointer escapes manual block
        Marshal.FreeHGlobal(ptr);
    }
}
```

## Model Isolation

**Critical Rule**: Automatic and manual memory NEVER interoperate.

### Isolation Rules

1. Manual pointers cannot escape manual blocks
2. Automatic references cannot enter manual blocks
3. No cross-model aliasing
4. No ownership transfer between models
5. No lifetime dependencies

### Why Isolation Matters

- Automatic model assumes CTGC manages everything
- Manual model assumes explicit verification
- Mixing them would invalidate both proofs
- Isolation ensures both models remain sound

## Choosing a Model

### Use Automatic (CTGC) When:
- ✅ Writing normal application code
- ✅ Using LINQ, async/await, collections
- ✅ Prioritizing development speed
- ✅ Want zero-cost garbage collection

### Use Manual When:
- ✅ Need low-level memory control
- ✅ Interfacing with C libraries
- ✅ Performance-critical hotspots
- ✅ Custom memory layouts

## Best Practices

1. **Default to Automatic**: Use CTGC for 95% of code
2. **Manual for Hotspots**: Use manual{} sparingly
3. **Isolate Manual**: Keep manual blocks small and focused
4. **Document Assumptions**: Comment manual memory contracts
5. **Test Thoroughly**: Both models provide safety, but test behavior

## Comparison

| Feature | Automatic (CTGC) | Manual |
|---------|------------------|---------|
| **Ease of Use** | Very Easy | Moderate |
| **Compile Time** | Fast | Slower |
| **Runtime** | Zero overhead | Zero overhead |
| **Safety** | Proven | Proven |
| **Control** | Automatic | Manual |
| **Use Case** | General code | Low-level code |

## Example: Hybrid Approach

```csharp
class DataProcessor
{
    // Automatic memory for high-level logic
    private List<Record> records = new List<Record>();
    
    public void Process()
    {
        foreach (var record in records)
        {
            ProcessRecord(record);
        }
    }
    
    // Manual memory for performance-critical buffer
    private void ProcessRecord(Record record)
    {
        manual {
            IntPtr buffer = Marshal.AllocHGlobal(4096);
            // Fast low-level processing
            FastProcess(buffer, record.Data);
            Marshal.FreeHGlobal(buffer);
        }
    }
}
```

## Learn More

- [Getting Started](GettingStarted.md) - First programs
- [Language Reference](LanguageReference.md) - Full C# support
- [Internal Documentation](../Internal/README.md) - How it works internally

---

**Both models guarantee 100% memory safety. Choose the right tool for each job!**
