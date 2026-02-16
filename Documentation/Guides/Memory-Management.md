# Memory Management Guide

## Introduction

CRAB provides two memory management models that guarantee 100% memory safety:

1. **Automatic Memory (CTGC)** - Compile-Time Garbage Collection (default)
2. **Manual Memory** - Explicit management with verification

Both models are mathematically proven safe and completely isolated from each other.

## Automatic Memory (CTGC)

### Overview

Compile-Time Garbage Collection (CTGC) analyzes your code at compile time and automatically inserts memory allocations and deallocations.

**Benefits:**
- ✅ Zero runtime overhead
- ✅ Deterministic deallocation
- ✅ No GC pauses
- ✅ Provably safe

### Basic Usage

```csharp
void ProcessData()
{
    var data = new byte[1024];    // Allocation
    ProcessBytes(data);
    // Automatic deallocation inserted here
}
```

The compiler analyzes object lifetimes and inserts `deallocate` operations at the appropriate points.

### How CTGC Works

#### 1. Lifetime Analysis

```csharp
void Example()
{
    var obj = new MyClass();  // Line 3: Allocation
    obj.Method1();            // Line 4: Use
    obj.Method2();            // Line 5: Last use
    // Line 6: Deallocation point
}
```

The compiler determines that:
- Object allocated at line 3
- Last use at line 5
- Safe to deallocate after line 5

#### 2. Escape Analysis

```csharp
// No escape - deallocated in function
void NoEscape()
{
    var obj = new MyClass();
    obj.Process();
    // Deallocate here
}

// Escapes - caller owns
MyClass CreateObject()
{
    var obj = new MyClass();
    return obj;  // Ownership transfers to caller
}

// Field assignment - object escapes
class Container
{
    private MyClass field;
    
    void Method()
    {
        var obj = new MyClass();
        field = obj;  // Escapes to field
        // Don't deallocate
    }
}
```

#### 3. Region Analysis

```csharp
void ProcessList()
{
    var list = new List<string>();  // Region starts
    
    for (int i = 0; i < 100; i++)
    {
        list.Add($"Item {i}");      // All items in same region
    }
    
    foreach (var item in list)
    {
        Console.WriteLine(item);
    }
    
    // Entire region deallocated here
}
```

All related allocations (list + all strings) are grouped into a region and deallocated together.

### Working with Collections

```csharp
// List
void ProcessNumbers()
{
    var numbers = new List<int>();
    for (int i = 0; i < 100; i++)
    {
        numbers.Add(i * i);
    }
    
    int sum = numbers.Sum();
    Console.WriteLine(sum);
    // List and all contents deallocated
}

// Dictionary
void ProcessMap()
{
    var map = new Dictionary<string, int>();
    map["one"] = 1;
    map["two"] = 2;
    map["three"] = 3;
    
    foreach (var kvp in map)
    {
        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
    }
    // Dictionary and all entries deallocated
}
```

### Conditional Allocation

```csharp
void ConditionalExample(bool condition)
{
    if (condition)
    {
        var obj = new MyClass();
        obj.Process();
        // Deallocate at end of if block
    }
    else
    {
        var other = new OtherClass();
        other.Process();
        // Deallocate at end of else block
    }
}
```

### Best Practices

1. **Prefer CTGC** - Use automatic memory for most code
2. **Small Scopes** - Keep object lifetimes short when possible
3. **Avoid Circular References** - Not directly supported by CTGC
4. **Use Value Types** - Structs for small data

## Manual Memory

### Overview

For low-level control, use `manual { }` blocks with explicit memory management.

**Benefits:**
- ✅ Full control
- ✅ Verified safety
- ✅ No hidden costs
- ✅ Isolation from automatic code

### Basic Usage

```csharp
manual
{
    var buffer = malloc(1024);  // Explicit allocation
    
    // Use buffer
    for (int i = 0; i < 1024; i++)
    {
        buffer[i] = 0;
    }
    
    free(buffer);  // Required explicit free
}
```

### Verification

The compiler verifies:
1. All allocations are freed
2. No use-after-free
3. No double-free
4. No dangling pointers
5. No memory leaks

### Memory Operations

```csharp
manual
{
    // Allocate
    var ptr = malloc(1024);
    
    // Reallocate
    ptr = realloc(ptr, 2048);
    
    // Copy memory
    var dest = malloc(1024);
    memcpy(dest, ptr, 1024);
    
    // Zero memory
    memset(ptr, 0, 2048);
    
    // Free
    free(dest);
    free(ptr);
}
```

### Conditional Freeing

All paths must free:

```csharp
manual
{
    var ptr = malloc(100);
    
    if (condition)
    {
        ProcessData(ptr);
        free(ptr);  // Free on this path
    }
    else
    {
        LogError();
        free(ptr);  // Free on this path too
    }
    // Compiler verifies all paths free ptr
}
```

### Multiple Allocations

```csharp
manual
{
    var buffer1 = malloc(100);
    var buffer2 = malloc(200);
    var buffer3 = malloc(300);
    
    CombineBuffers(buffer1, buffer2, buffer3);
    
    // Free in any order
    free(buffer3);
    free(buffer1);
    free(buffer2);
}
```

### Error Handling

```csharp
manual
{
    var ptr = malloc(1024);
    
    bool success = ProcessData(ptr);
    
    if (!success)
    {
        free(ptr);
        return;  // Early return OK
    }
    
    MoreProcessing(ptr);
    free(ptr);
}
```

### Best Practices

1. **Minimize Manual Blocks** - Use sparingly
2. **Keep Blocks Small** - Easier to verify
3. **Document Why** - Explain why manual is needed
4. **Test Thoroughly** - Extra testing for manual code
5. **Free Early** - Free as soon as possible

## Model Isolation

### The Isolation Rule

Automatic and manual memory cannot interact:

```csharp
// INVALID: Cannot pass manual pointer to automatic
var autoObj = new MyClass();

manual
{
    var ptr = malloc(100);
    autoObj.SetData(ptr);  // ❌ ERROR: Cannot escape
    free(ptr);
}

// INVALID: Cannot pass automatic object to manual
int[] autoArray = new int[100];

manual
{
    var ptr = malloc(400);
    CopyFrom(ptr, autoArray);  // ❌ ERROR: Cannot use automatic
    free(ptr);
}
```

### Working Around Isolation

#### Copy Values

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
// Process with automatic memory
var result1 = ProcessAutomatic(data);

// Then process with manual memory
manual
{
    var buffer = malloc(1024);
    var result2 = ProcessManual(buffer, dataValue);
    free(buffer);
}

// Combine results
var final = Combine(result1, result2);
```

## Performance Considerations

### CTGC Performance

- **Compilation**: O(n log n) time
- **Runtime**: Zero overhead
- **Memory**: Minimal (immediate deallocation)

### Manual Performance

- **Compilation**: O(n²) worst case (verification)
- **Runtime**: Zero overhead
- **Memory**: Developer controlled

### When to Use Each

**Use Automatic (CTGC) for:**
- Business logic
- String processing
- Collections
- LINQ queries
- Most application code

**Use Manual for:**
- Performance-critical inner loops
- Large buffer manipulation
- Custom memory pools
- Interfacing with C libraries
- Embedded systems

## Debugging

### CTGC Debug Mode

```bash
crab compile --ctgc-stats program.cs
```

Shows:
- All allocation points
- All deallocation points
- Lifetime spans
- Region assignments
- Escape analysis results

### Manual Debug Mode

```bash
crab compile --manual-stats program.cs
```

Shows:
- Ownership graph
- Alias tracking
- Symbolic execution paths
- Verification proof steps

### Common Issues

#### Memory Leak Detection

```csharp
// CTGC detects this is safe
void NoLeak()
{
    var obj = new MyClass();
    obj.Process();
    // Automatically deallocated
}

// Manual requires explicit free
manual
{
    var ptr = malloc(100);
    // ❌ ERROR: Not freed on all paths
}
```

#### Use-After-Free Detection

```csharp
manual
{
    var ptr = malloc(100);
    free(ptr);
    ptr[0] = 42;  // ❌ ERROR: Use after free
}
```

#### Double-Free Detection

```csharp
manual
{
    var ptr = malloc(100);
    free(ptr);
    free(ptr);  // ❌ ERROR: Double free
}
```

## See Also

- [Memory Models Architecture](../Architecture/Memory-Models.md) - Technical details
- [Memory Examples](../Examples/Memory-Management.md) - Working examples
- [Compiler Architecture](../Architecture/Compiler-Architecture.md) - How verification works
