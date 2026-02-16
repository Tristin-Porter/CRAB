# Memory Management Examples

## CTGC Automatic Memory Examples

### Basic Allocation

```csharp
void SimpleAllocation()
{
    var data = new byte[1024];
    
    // Use data
    for (int i = 0; i < 1024; i++)
    {
        data[i] = (byte)i;
    }
    
    // Automatically deallocated here
}
```

### Object Creation

```csharp
class MyClass
{
    private int value;
    
    public MyClass(int v)
    {
        value = v;
    }
    
    public void Process()
    {
        Console.WriteLine(value);
    }
}

void CreateObject()
{
    var obj = new MyClass(42);
    obj.Process();
    // obj automatically deallocated
}
```

### Collection Management

```csharp
void ProcessList()
{
    var numbers = new List<int>();
    
    for (int i = 0; i < 100; i++)
    {
        numbers.Add(i * i);
    }
    
    foreach (var num in numbers)
    {
        Console.WriteLine(num);
    }
    
    // List and all items deallocated together
}
```

### String Processing

```csharp
void ProcessStrings()
{
    var text = "Hello, World!";
    var upper = text.ToUpper();
    var lower = text.ToLower();
    var trimmed = text.Trim();
    
    Console.WriteLine($"{upper}, {lower}, {trimmed}");
    
    // All strings deallocated
}
```

### Escaping Objects

```csharp
// Object escapes - caller owns
string CreateString()
{
    var text = new StringBuilder();
    text.Append("Hello");
    text.Append(" World");
    return text.ToString();  // Ownership transfers
    // StringBuilder deallocated, string escapes
}

void UseEscaping()
{
    var result = CreateString();
    Console.WriteLine(result);
    // result deallocated here
}
```

### Conditional Allocation

```csharp
void ConditionalExample(bool condition)
{
    if (condition)
    {
        var obj1 = new MyClass(1);
        obj1.Process();
        // obj1 deallocated
    }
    else
    {
        var obj2 = new MyClass(2);
        obj2.Process();
        // obj2 deallocated
    }
}
```

### Loop Allocation

```csharp
void LoopExample()
{
    for (int i = 0; i < 10; i++)
    {
        var temp = new MyClass(i);
        temp.Process();
        // temp deallocated each iteration
    }
}
```

## Manual Memory Examples

### Basic Manual Memory

```csharp
manual
{
    var buffer = malloc(1024);
    
    // Initialize
    for (int i = 0; i < 1024; i++)
    {
        buffer[i] = 0;
    }
    
    // Use buffer
    ProcessData(buffer);
    
    // Required explicit free
    free(buffer);
}
```

### Multiple Allocations

```csharp
manual
{
    var buffer1 = malloc(100);
    var buffer2 = malloc(200);
    var buffer3 = malloc(300);
    
    // Use buffers
    CombineData(buffer1, buffer2, buffer3);
    
    // Free all
    free(buffer1);
    free(buffer2);
    free(buffer3);
}
```

### Conditional Freeing

```csharp
manual
{
    var buffer = malloc(1024);
    
    bool success = ProcessBuffer(buffer);
    
    if (success)
    {
        SaveResult(buffer);
        free(buffer);
    }
    else
    {
        LogError();
        free(buffer);  // Must free on all paths
    }
}
```

### Reallocation

```csharp
manual
{
    var buffer = malloc(1024);
    
    // Use initial buffer
    FillData(buffer, 1024);
    
    // Need more space
    buffer = realloc(buffer, 2048);
    
    // Use expanded buffer
    FillMoreData(buffer, 1024, 2048);
    
    free(buffer);
}
```

### Memory Operations

```csharp
manual
{
    var src = malloc(1024);
    var dest = malloc(1024);
    
    // Fill source
    for (int i = 0; i < 1024; i++)
    {
        src[i] = (byte)i;
    }
    
    // Copy memory
    memcpy(dest, src, 1024);
    
    // Zero memory
    memset(src, 0, 1024);
    
    // Cleanup
    free(src);
    free(dest);
}
```

### Buffer Processing

```csharp
manual
{
    var inputBuffer = malloc(4096);
    var outputBuffer = malloc(4096);
    
    // Read data
    int bytesRead = ReadData(inputBuffer, 4096);
    
    // Transform
    int bytesWritten = Transform(inputBuffer, bytesRead, outputBuffer);
    
    // Write result
    WriteData(outputBuffer, bytesWritten);
    
    // Cleanup
    free(inputBuffer);
    free(outputBuffer);
}
```

## Memory Isolation Examples

### Invalid: Crossing Boundaries

```csharp
// ❌ INVALID - Cannot pass manual pointer to automatic
var autoObj = new MyClass();

manual
{
    var ptr = malloc(100);
    autoObj.SetPointer(ptr);  // ERROR
    free(ptr);
}
```

### Valid: Copying Values

```csharp
// ✅ VALID - Copy values between models
int[] autoData = new int[100];

// Fill automatic data
for (int i = 0; i < 100; i++)
{
    autoData[i] = i;
}

manual
{
    var manualData = malloc(400);  // 100 * sizeof(int)
    
    // Copy values
    for (int i = 0; i < 100; i++)
    {
        manualData[i] = autoData[i];
    }
    
    // Process in manual memory
    ProcessManual(manualData);
    
    free(manualData);
}
```

### Valid: Separate Processing

```csharp
// ✅ VALID - Process separately
void ProcessData(int[] data)
{
    // Automatic processing
    var sorted = data.OrderBy(x => x).ToArray();
    int average = (int)sorted.Average();
    
    // Manual processing
    manual
    {
        var buffer = malloc(1024);
        
        // Use average value (primitive, not pointer)
        FillBuffer(buffer, average);
        
        free(buffer);
    }
    
    // Continue automatic processing
    var result = sorted.Take(10).ToList();
}
```

## Performance Patterns

### CTGC-Friendly Pattern

```csharp
void EfficientCTGC()
{
    // Keep allocations in tight scopes
    {
        var temp1 = new MyClass();
        temp1.Process();
        // temp1 freed immediately
    }
    
    {
        var temp2 = new MyClass();
        temp2.Process();
        // temp2 freed immediately
    }
}
```

### Manual for Performance

```csharp
void PerformanceCritical()
{
    manual
    {
        // Pre-allocate buffer once
        var scratch = malloc(4096);
        
        // Reuse in tight loop
        for (int i = 0; i < 10000; i++)
        {
            ProcessChunk(scratch, data, i);
            // No allocation/deallocation overhead
        }
        
        free(scratch);
    }
}
```

### Hybrid Approach

```csharp
void HybridExample()
{
    // High-level logic with CTGC
    var config = LoadConfiguration();
    var inputs = GatherInputs();
    
    // Performance-critical section with manual
    manual
    {
        var workBuffer = malloc(1048576);  // 1MB
        
        foreach (var input in inputs)
        {
            ProcessWithBuffer(workBuffer, input, config);
        }
        
        free(workBuffer);
    }
    
    // Back to CTGC for results
    var results = FormatResults();
    SaveResults(results);
}
```

## Common Patterns

### Builder Pattern (CTGC)

```csharp
class StringBuilder
{
    private List<string> parts = new List<string>();
    
    public void Append(string text)
    {
        parts.Add(text);
    }
    
    public string Build()
    {
        return string.Join("", parts);
    }
}

void UseBuilder()
{
    var builder = new StringBuilder();
    builder.Append("Hello");
    builder.Append(" ");
    builder.Append("World");
    var result = builder.Build();
    
    Console.WriteLine(result);
    // builder deallocated, result escapes
}
```

### Resource Management

```csharp
void ManageResource()
{
    var file = OpenFile("data.txt");
    
    try
    {
        var data = file.ReadAll();
        ProcessData(data);
    }
    finally
    {
        file.Close();
        // CTGC ensures file is deallocated
    }
}
```

## See Also

- [Memory Models](../Architecture/Memory-Models.md) - Architecture details
- [Memory Management Guide](../Guides/Memory-Management.md) - User guide
- [Advanced Features](Advanced-Features.md) - Complex examples
