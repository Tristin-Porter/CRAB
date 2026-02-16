# CRAB Memory Models

## Overview

CRAB provides two distinct memory management models that are completely isolated from each other:

1. **Automatic Memory (CTGC)** - Default mode, compile-time garbage collection
2. **Manual Memory** - Explicit memory management with verification

## Automatic Memory (CTGC)

### Concept

Compile-Time Garbage Collection (CTGC) is inspired by Mercury's memory management. All allocations and deallocations are determined at compile time and inserted directly into the code.

### Key Features

- **No Runtime GC** - All memory management is compile-time
- **Deterministic** - Same execution pattern every time
- **Zero Overhead** - No GC pauses or tracing
- **Provably Safe** - Mathematical guarantees

### How It Works

#### 1. Lifetime Analysis

The compiler analyzes object lifetimes:

```csharp
void Example()
{
    var obj = new MyClass(); // Allocation point
    obj.Method1();
    obj.Method2();
    // Deallocation point automatically determined
}
```

**Compiler Analysis:**
- Object created at line 3
- Last use at line 5
- Safe to deallocate after line 5

#### 2. Region Analysis

Groups related allocations:

```csharp
void ProcessData()
{
    var list = new List<int>();    // Region A
    for (int i = 0; i < 100; i++)
    {
        list.Add(i);               // All items in Region A
    }
    // Entire Region A deallocated here
}
```

#### 3. Escape Analysis

Determines if objects escape their scope:

```csharp
MyClass CreateObject()
{
    var obj = new MyClass();
    return obj; // Escapes! Caller owns deallocation
}

void NoEscape()
{
    var obj = new MyClass();
    obj.DoWork();
    // Doesn't escape, deallocate here
}
```

#### 4. Deallocation Insertion

The compiler inserts explicit deallocation:

```csharp
// Original code
void Method()
{
    var data = new byte[1024];
    Process(data);
}

// After CTGC transformation
void Method()
{
    var data = allocate_bytes(1024);
    Process(data);
    deallocate(data); // Inserted by compiler
}
```

### CTGC Guarantees

✅ **No Memory Leaks** - All allocations are freed  
✅ **No Use-After-Free** - Objects only accessed while live  
✅ **No Double-Free** - Each allocation freed exactly once  
✅ **Deterministic** - Same deallocation points every run  

### CTGC Limitations

- No cyclic data structures without special handling
- Reference counting for shared ownership
- Some patterns require manual intervention

### CTGC Examples

#### Simple Allocation

```csharp
void SimpleExample()
{
    var number = new int[100];
    for (int i = 0; i < 100; i++)
    {
        number[i] = i * i;
    }
    int sum = number.Sum();
    Console.WriteLine(sum);
    // Compiler inserts: deallocate(number)
}
```

#### Method Call with Return

```csharp
string ProcessString(string input)
{
    var upper = input.ToUpper();
    var trimmed = upper.Trim();
    return trimmed; // Escapes to caller
    // Compiler inserts: deallocate(input), deallocate(upper)
    // 'trimmed' ownership transfers to caller
}
```

#### Collection Processing

```csharp
void ProcessList()
{
    var items = new List<string>();
    items.Add("one");
    items.Add("two");
    items.Add("three");
    
    foreach (var item in items)
    {
        Console.WriteLine(item);
    }
    // Compiler inserts: deallocate(items and all contained strings)
}
```

## Manual Memory

### Concept

For low-level code requiring explicit control, CRAB provides `manual { }` blocks with full verification.

### Key Features

- **Explicit Control** - You manage allocations
- **Verified Safety** - Compiler proves correctness
- **Isolation** - Cannot interact with automatic code
- **No Borrow Checker** - Invisible verification

### Syntax

```csharp
manual
{
    // Manual memory code here
    var ptr = malloc(1024);
    // ...
    free(ptr);
}
```

### How Verification Works

#### 1. Ownership Graph Construction

```csharp
manual
{
    var ptr1 = malloc(100);  // ptr1 owns memory
    var ptr2 = ptr1;         // ptr2 aliases ptr1
    free(ptr1);              // Ownership released
    // Using ptr2 here would be ERROR
}
```

**Ownership Graph:**
```
malloc(100) ──owns──> ptr1
                      ├──aliases──> ptr2
                      └──freed at line 5
```

#### 2. Alias Tracking

The compiler tracks all pointer aliases:

```csharp
manual
{
    var ptr = malloc(100);
    var alias1 = ptr;
    var alias2 = alias1;
    
    // Compiler knows: ptr, alias1, alias2 all point to same memory
    
    free(ptr);
    
    // All aliases are now invalid
    // Using alias1 or alias2 is an ERROR
}
```

#### 3. Escape Analysis

Pointers cannot escape manual blocks:

```csharp
void* globalPtr;

manual
{
    var ptr = malloc(100);
    globalPtr = ptr; // ERROR: Cannot escape manual block
}
```

#### 4. Symbolic Execution

The compiler symbolically executes all paths:

```csharp
manual
{
    var ptr = malloc(100);
    
    if (condition)
    {
        free(ptr);
    }
    else
    {
        free(ptr);
    }
    // Both paths free 'ptr', OK
}

manual
{
    var ptr = malloc(100);
    
    if (condition)
    {
        free(ptr);
    }
    // ERROR: Path exists where 'ptr' is not freed
}
```

### Manual Memory Guarantees

✅ **No Invalid Pointers** - All pointer uses are verified safe  
✅ **No Memory Leaks** - All allocations proven to be freed  
✅ **No Dangling Pointers** - Use-after-free impossible  
✅ **Complete Isolation** - Cannot affect automatic memory  

### Manual vs Unsafe

```csharp
// Old C# unsafe - NOT VERIFIED
unsafe
{
    int* ptr = stackalloc int[100];
    ptr[200] = 42; // Buffer overflow! No safety checking
}

// CRAB manual - FULLY VERIFIED
manual
{
    var ptr = malloc(400); // 100 * sizeof(int)
    ptr[200] = 42; // ERROR: Compiler proves this is out of bounds
    free(ptr);
}
```

### Manual Memory Examples

#### Basic Allocation

```csharp
manual
{
    var buffer = malloc(1024);
    
    // Use buffer...
    for (int i = 0; i < 1024; i++)
    {
        buffer[i] = 0;
    }
    
    free(buffer); // Required
}
```

#### Conditional Freeing

```csharp
manual
{
    var ptr = malloc(100);
    
    bool success = ProcessData(ptr);
    
    if (success)
    {
        StoreResult(ptr);
        free(ptr);
    }
    else
    {
        LogError();
        free(ptr); // Must free on all paths
    }
}
```

#### Multiple Allocations

```csharp
manual
{
    var ptr1 = malloc(100);
    var ptr2 = malloc(200);
    var ptr3 = malloc(300);
    
    CombineData(ptr1, ptr2, ptr3);
    
    free(ptr3);
    free(ptr2);
    free(ptr1);
}
```

## Model Isolation

### The Isolation Rule

**Automatic and manual memory CANNOT interact.**

```csharp
// AUTOMATIC CODE
var autoObj = new MyClass();

manual
{
    var ptr = malloc(100);
    
    autoObj.field = ptr; // ERROR: Cannot pass manual ptr to automatic
    
    free(ptr);
}

var result = new int[100];
manual
{
    var ptr = malloc(400);
    
    // ERROR: Cannot pass automatic 'result' to manual block
    CopyMemory(ptr, result);
    
    free(ptr);
}
```

### Why Isolation?

1. **Safety Proof Soundness** - Each model has different verification
2. **No Undefined Behavior** - Prevents unsafe interactions
3. **Clear Ownership** - No ambiguity about who owns what

### Working Around Isolation

#### Copy Data

```csharp
int[] autoData = new int[100];

manual
{
    var manualData = malloc(400);
    
    // Copy values, not pointers
    for (int i = 0; i < 100; i++)
    {
        manualData[i] = autoData[i];
    }
    
    ProcessManual(manualData);
    
    free(manualData);
}
```

#### Separate Processing

```csharp
// Process in automatic mode
var result1 = ProcessAutomatic(data);

// Then process in manual mode
manual
{
    var buffer = malloc(1024);
    var result2 = ProcessManual(buffer, dataValue);
    free(buffer);
}

// Combine results
var final = Combine(result1, result2);
```

## Performance Characteristics

### CTGC Performance

| Aspect | Performance |
|--------|-------------|
| Compilation Time | O(n log n) |
| Runtime Overhead | Zero |
| Memory Usage | Minimal |
| Determinism | 100% |

### Manual Performance

| Aspect | Performance |
|--------|-------------|
| Compilation Time | O(n²) worst case |
| Runtime Overhead | Zero |
| Memory Usage | Developer controlled |
| Determinism | 100% |

## Best Practices

### When to Use Automatic (CTGC)

✅ **Default choice** - Use for most code  
✅ **Business logic** - Application code  
✅ **String processing** - Text manipulation  
✅ **Collections** - Lists, dictionaries, etc.  
✅ **LINQ queries** - Data transformations  

### When to Use Manual

✅ **Low-level code** - Systems programming  
✅ **Performance critical** - Inner loops  
✅ **Interop** - C library integration  
✅ **Custom allocators** - Memory pools  
✅ **Embedded systems** - Resource constraints  

### Guidelines

1. **Prefer Automatic** - Default to CTGC
2. **Minimize Manual** - Use sparingly
3. **Document Manual** - Explain why needed
4. **Test Thoroughly** - Manual blocks need extra testing
5. **Profile First** - Verify manual is actually faster

## Debugging Memory Issues

### CTGC Debug Mode

```bash
crab compile --debug-memory program.cs
```

Shows:
- All allocation points
- All deallocation points
- Lifetime spans
- Region assignments

### Manual Verification Output

```bash
crab compile --verify-manual program.cs
```

Shows:
- Ownership graph
- All pointer aliases
- Symbolic execution paths
- Safety proof steps

## Common Patterns

### Resource Management (Automatic)

```csharp
void ProcessFile(string path)
{
    var file = OpenFile(path);
    var data = file.ReadAll();
    ProcessData(data);
    file.Close();
    // Compiler ensures: file and data are deallocated
}
```

### Temporary Buffers (Manual)

```csharp
manual
{
    var scratch = malloc(4096);
    
    for (int i = 0; i < iterations; i++)
    {
        ProcessChunk(scratch, data, i);
    }
    
    free(scratch);
}
```

### Builder Pattern (Automatic)

```csharp
var builder = new StringBuilder();
builder.Append("Hello");
builder.Append(" ");
builder.Append("World");
var result = builder.ToString();
// Compiler deallocates builder but result escapes
```

## See Also

- [Compiler Architecture](Compiler-Architecture.md) - Overall compiler design
- [Memory Management Guide](../Guides/Memory-Management.md) - User guide
- [Memory Examples](../Examples/Memory-Management.md) - Working examples
