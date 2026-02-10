# CRAB Language Support

## Overview

CRAB supports the complete C# language specification from C# 1.0 through C# 13. This document details which features are supported and how they are compiled to WebAssembly.

## Supported C# Versions

✅ **C# 1.0 - 13.0**: Full support

CRAB compiles all modern C# features to WebAssembly MVP without requiring a runtime.

## Language Features

### Classes and Objects

✅ **Fully Supported**

```csharp
class MyClass
{
    private int value;
    public string Name { get; set; }
    
    public MyClass(int val)
    {
        value = val;
    }
    
    public void DoWork() => value++;
}

var obj = new MyClass(42);
obj.Name = "Test";
obj.DoWork();
```

**Compilation:** Classes compiled to WASM linear memory layouts with vtables for virtual dispatch.

### Structs and Value Types

✅ **Fully Supported**

```csharp
struct Point
{
    public int X { get; set; }
    public int Y { get; set; }
    
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

Point p = new Point(10, 20);
```

**Compilation:** Structs mapped directly to WASM stack or linear memory (value semantics preserved).

### Inheritance and Polymorphism

✅ **Fully Supported**

```csharp
class Base
{
    public virtual int Calculate(int x) => x * 2;
}

class Derived : Base
{
    public override int Calculate(int x) => x * 3;
}

Base obj = new Derived();
int result = obj.Calculate(10); // 30 (polymorphic call)
```

**Compilation:** Virtual methods use indirect calls through function tables.

### Interfaces

✅ **Fully Supported**

```csharp
interface IProcessor
{
    int Process(int value);
}

class Doubler : IProcessor
{
    public int Process(int value) => value * 2;
}

IProcessor proc = new Doubler();
int result = proc.Process(10);
```

**Compilation:** Interface methods compiled to indirect calls with runtime type checking.

### Generics

✅ **Fully Supported**

```csharp
class Container<T>
{
    private T value;
    public T GetValue() => value;
    public void SetValue(T val) => value = val;
}

var intContainer = new Container<int>();
intContainer.SetValue(42);

var stringContainer = new Container<string>();
stringContainer.SetValue("Hello");
```

**Compilation:** Generics are monomorphized at compile-time (similar to C++ templates).

### Async/Await

✅ **Fully Supported**

```csharp
async Task<int> FetchDataAsync()
{
    await Task.Delay(1000);
    return 42;
}

async Task ProcessAsync()
{
    int data = await FetchDataAsync();
    Console.WriteLine(data);
}
```

**Compilation:** State machine transformation with WASM function continuations.

### LINQ

✅ **Fully Supported**

```csharp
var numbers = new int[] { 1, 2, 3, 4, 5 };

var evens = numbers
    .Where(x => x % 2 == 0)
    .Select(x => x * 2)
    .ToList();

// Query syntax
var query = from n in numbers
            where n > 2
            select n * 2;
```

**Compilation:** LINQ queries optimized by CTGC to minimize allocations.

### Pattern Matching

✅ **Fully Supported**

```csharp
// Switch expression
int result = value switch
{
    0 => "zero",
    > 0 and < 10 => "small",
    >= 10 and < 100 => "medium",
    _ => "large"
};

// Pattern matching
if (obj is MyClass { Value: > 10 })
{
    // ...
}
```

**Compilation:** Pattern matching compiled to optimized conditional branches.

### Records

✅ **Fully Supported** (C# 9+)

```csharp
record Person(string Name, int Age);

var person = new Person("Alice", 30);
var older = person with { Age = 31 };
```

**Compilation:** Records compiled with value semantics and efficient copying.

### Properties and Indexers

✅ **Fully Supported**

```csharp
class Container
{
    private int[] data = new int[10];
    
    public int Count { get; private set; }
    
    public int this[int index]
    {
        get => data[index];
        set => data[index] = value;
    }
}
```

**Compilation:** Properties compiled to getter/setter methods, indexers to array access.

### Delegates and Events

✅ **Fully Supported**

```csharp
delegate void EventHandler(object sender);

class Publisher
{
    public event EventHandler? OnEvent;
    
    public void Trigger()
    {
        OnEvent?.Invoke(this);
    }
}
```

**Compilation:** Delegates are function pointers, events are multicast delegate lists.

### Lambda Expressions and Closures

✅ **Fully Supported**

```csharp
int captured = 10;

Func<int, int> lambda = x => x + captured;

int result = lambda(5); // 15
```

**Compilation:** Closures capture variables in heap-allocated contexts (managed by CTGC).

### Exception Handling

✅ **Fully Supported**

```csharp
try
{
    int result = Compute();
}
catch (InvalidOperationException ex)
{
    HandleError(ex);
}
finally
{
    Cleanup();
}
```

**Compilation:** Exceptions use WASM control flow (no stack unwinding needed).

### Operator Overloading

✅ **Fully Supported**

```csharp
struct Complex
{
    public double Real { get; set; }
    public double Imaginary { get; set; }
    
    public static Complex operator +(Complex a, Complex b) =>
        new Complex
        {
            Real = a.Real + b.Real,
            Imaginary = a.Imaginary + b.Imaginary
        };
}
```

**Compilation:** Overloaded operators compiled to static methods.

### Nullable Reference Types

✅ **Fully Supported** (C# 8+)

```csharp
string? nullable = null;
string nonNull = "Hello";

if (nullable != null)
{
    Console.WriteLine(nullable.Length);
}
```

**Compilation:** Null checks enforced at compile-time, runtime checks inserted where needed.

### Attributes

✅ **Supported**

```csharp
[Serializable]
class MyClass
{
    [Obsolete("Use NewMethod instead")]
    public void OldMethod() { }
}
```

**Compilation:** Attributes compiled to metadata tables (accessed via compile-time reflection).

### Reflection (Compile-Time)

⚠️ **Limited Support**

CRAB implements reflection through compile-time metadata generation rather than runtime reflection.

```csharp
Type type = typeof(MyClass);
string name = type.Name;
var methods = type.GetMethods(); // Compile-time resolved
```

**Limitation:** Dynamic type creation not supported (no `Assembly.CreateInstance`).

### Dynamic

⚠️ **Limited Support**

```csharp
dynamic obj = GetObject();
obj.DoWork(); // Resolved at compile-time if possible
```

**Limitation:** True dynamic dispatch not supported. `dynamic` resolves to compile-time types where possible.

### Pointers and Unsafe Code

✅ **Supported via Manual Memory Model**

```csharp
manual  // or unsafe
{
    IntPtr buffer = Marshal.AllocHGlobal(1024);
    Marshal.WriteInt32(buffer, 0, 42);
    Marshal.FreeHGlobal(buffer);
}
```

**Compilation:** Manual blocks verified for safety, compiled to WASM memory operations.

## Not Supported

### Multithreading

❌ **Not Supported**

WASM MVP is single-threaded. Thread-related APIs are not available:
- `Thread`, `Task.Run`, `Parallel.For`
- `lock`, `Monitor`, `Mutex`
- `async void` methods

**Alternative:** Use `async/await` for asynchronous operations.

### AppDomains

❌ **Not Supported**

No runtime, no AppDomains.

### COM Interop

❌ **Not Supported**

WebAssembly has no COM infrastructure.

### P/Invoke (Traditional)

❌ **Not Supported**

No native interop. Use WASM imports instead.

## Limitations

### Arrays

✅ Fixed-size arrays fully supported
⚠️ Jagged arrays supported with performance considerations
✅ Multi-dimensional arrays supported

### Collections

✅ `List<T>`, `Dictionary<TKey, TValue>` - Fully supported
✅ `HashSet<T>`, `Queue<T>`, `Stack<T>` - Fully supported
⚠️ Concurrent collections - Not applicable (single-threaded)

### File I/O

⚠️ Limited to WASI (WebAssembly System Interface) when available.

### Networking

⚠️ Limited to WASM networking capabilities (imports).

## Best Practices

### 1. Prefer Automatic Memory Model

```csharp
// Good: Automatic memory management
void ProcessData()
{
    var data = new DataProcessor();
    data.Process();
    // Automatically freed
}
```

### 2. Use Manual Only When Needed

```csharp
// Use manual for low-level buffer operations
manual
{
    IntPtr buffer = Marshal.AllocHGlobal(largeSize);
    // ... work with buffer ...
    Marshal.FreeHGlobal(buffer);
}
```

### 3. Leverage LINQ

```csharp
// CRAB optimizes LINQ queries
var result = data
    .Where(x => x.IsValid)
    .Select(x => x.Transform())
    .ToList();
```

### 4. Use Value Types for Performance

```csharp
// Structs avoid heap allocations
struct Point { public int X, Y; }
```

## Performance Characteristics

### Memory Model

- **Automatic (CTGC)**: Zero runtime overhead, compile-time cost
- **Manual**: High compile-time cost (verification), zero runtime overhead

### Allocations

- **Stack**: Extremely fast (WASM locals)
- **Heap**: Fast (linear memory with CTGC optimization)

### Method Calls

- **Static**: Direct WASM call (fastest)
- **Virtual**: Indirect call through vtable (slight overhead)
- **Interface**: Indirect call with type check (moderate overhead)

## See Also

- [Getting Started](GettingStarted.md) - Learn CRAB basics
- [Memory Models](MemoryModels.md) - CTGC and Manual memory
- [Examples and Tutorials](ExamplesAndTutorials.md) - Code examples
- [Best Practices](BestPractices.md) - Writing optimal CRAB code
