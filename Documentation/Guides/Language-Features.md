# Language Features - C# 14 Support in CRAB

## Overview

CRAB provides comprehensive support for C# 14 language features, compiling them to memory-safe WebAssembly and native code.

## Supported Features

### Classes and Objects

```csharp
class MyClass
{
    // Fields
    private int field;
    
    // Properties
    public int Property { get; set; }
    
    // Constructor
    public MyClass(int value)
    {
        field = value;
    }
    
    // Methods
    public int GetValue()
    {
        return field;
    }
}
```

### Interfaces

```csharp
interface IProcessor
{
    void Process(string data);
    int GetResult();
}

class Processor : IProcessor
{
    public void Process(string data)
    {
        // Implementation
    }
    
    public int GetResult()
    {
        return 42;
    }
}
```

### Inheritance and Polymorphism

```csharp
class Base
{
    public virtual void Method()
    {
        Console.WriteLine("Base");
    }
}

class Derived : Base
{
    public override void Method()
    {
        Console.WriteLine("Derived");
    }
}

// Usage
Base obj = new Derived();
obj.Method(); // Calls Derived.Method()
```

### Generics

#### Generic Classes

```csharp
class Container<T>
{
    private T value;
    
    public void Set(T item)
    {
        value = item;
    }
    
    public T Get()
    {
        return value;
    }
}

// Usage
var intContainer = new Container<int>();
intContainer.Set(42);

var stringContainer = new Container<string>();
stringContainer.Set("hello");
```

#### Generic Methods

```csharp
class Utilities
{
    public static T Max<T>(T a, T b) where T : IComparable<T>
    {
        return a.CompareTo(b) > 0 ? a : b;
    }
}

int maxInt = Utilities.Max(5, 10);
string maxString = Utilities.Max("apple", "banana");
```

#### Generic Constraints

```csharp
// Class constraint
class Box<T> where T : class { }

// Struct constraint
class Number<T> where T : struct { }

// Interface constraint
class Processor<T> where T : IProcessor { }

// Constructor constraint
class Factory<T> where T : new() { }

// Multiple constraints
class Advanced<T> where T : class, IProcessor, new() { }
```

### Properties

#### Auto-Properties

```csharp
class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}
```

#### Property with Backing Field

```csharp
class Person
{
    private string _name;
    
    public string Name
    {
        get { return _name; }
        set { _name = value?.Trim(); }
    }
}
```

#### Init-Only Properties (C# 9+)

```csharp
class Person
{
    public string Name { get; init; }
    public int Age { get; init; }
}

var person = new Person { Name = "Alice", Age = 30 };
// person.Name = "Bob"; // Error: init-only
```

### Records (C# 9+)

```csharp
// Positional record
record Person(string Name, int Age);

var person = new Person("Alice", 30);
Console.WriteLine(person.Name);

// Record with methods
record Point(int X, int Y)
{
    public double DistanceFromOrigin()
    {
        return Math.Sqrt(X * X + Y * Y);
    }
}

// Record inheritance
record Student(string Name, int Age, string School) : Person(Name, Age);
```

### Pattern Matching

#### Is-Pattern

```csharp
if (obj is string s)
{
    Console.WriteLine($"String: {s}");
}

if (obj is int i && i > 0)
{
    Console.WriteLine($"Positive int: {i}");
}
```

#### Switch Expressions

```csharp
var result = shape switch
{
    Circle c => c.Radius * c.Radius * Math.PI,
    Rectangle r => r.Width * r.Height,
    Triangle t => t.Base * t.Height / 2,
    _ => 0
};
```

#### Property Patterns

```csharp
var message = person switch
{
    { Age: < 18 } => "Minor",
    { Age: >= 18, Age: < 65 } => "Adult",
    { Age: >= 65 } => "Senior",
    _ => "Unknown"
};
```

### Nullable Reference Types (C# 8+)

```csharp
#nullable enable

string? nullableString = null; // Can be null
string nonNullString = "hello"; // Cannot be null

void ProcessString(string? input)
{
    if (input != null)
    {
        Console.WriteLine(input.Length);
    }
}

// Null-forgiving operator
string text = nullableString!;
```

### Async/Await

```csharp
async Task<int> FetchDataAsync()
{
    await Task.Delay(1000);
    return 42;
}

async Task ProcessAsync()
{
    int result = await FetchDataAsync();
    Console.WriteLine($"Result: {result}");
}
```

### LINQ

#### Query Syntax

```csharp
var query = from n in numbers
            where n > 5
            orderby n descending
            select n * 2;
```

#### Method Syntax

```csharp
var query = numbers
    .Where(n => n > 5)
    .OrderByDescending(n => n)
    .Select(n => n * 2);
```

#### Common Operations

```csharp
// Filtering
var evens = numbers.Where(n => n % 2 == 0);

// Projection
var lengths = words.Select(w => w.Length);

// Aggregation
int sum = numbers.Sum();
int max = numbers.Max();
double average = numbers.Average();

// Grouping
var groups = people.GroupBy(p => p.Age);

// Joining
var joined = customers.Join(
    orders,
    c => c.Id,
    o => o.CustomerId,
    (c, o) => new { c.Name, o.Total }
);
```

### Lambda Expressions

```csharp
// Expression lambda
Func<int, int> square = x => x * x;

// Statement lambda
Func<int, int> abs = x =>
{
    if (x < 0)
        return -x;
    return x;
};

// Multiple parameters
Func<int, int, int> add = (a, b) => a + b;
```

### Delegates and Events

#### Delegates

```csharp
delegate void MessageHandler(string message);

MessageHandler handler = Console.WriteLine;
handler("Hello");

// Multicast
handler += LogMessage;
handler("World");
```

#### Events

```csharp
class Button
{
    public event EventHandler? Clicked;
    
    public void Click()
    {
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}

var button = new Button();
button.Clicked += (sender, args) => Console.WriteLine("Clicked!");
button.Click();
```

### Exception Handling

```csharp
try
{
    int result = Divide(10, 0);
}
catch (DivideByZeroException ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected: {ex.Message}");
}
finally
{
    Console.WriteLine("Cleanup");
}

// Throw
throw new InvalidOperationException("Invalid state");

// Rethrow
catch (Exception)
{
    // Handle
    throw; // Rethrow original exception
}
```

### Local Functions

```csharp
int Calculate(int a, int b)
{
    int Add(int x, int y) => x + y;
    int Multiply(int x, int y) => x * y;
    
    return Add(a, b) + Multiply(a, b);
}
```

### Tuples

```csharp
// Tuple creation
(int, string) tuple = (42, "hello");

// Named tuples
(int Age, string Name) person = (30, "Alice");
Console.WriteLine($"{person.Name} is {person.Age}");

// Tuple deconstruction
var (age, name) = person;
Console.WriteLine($"{name} is {age}");

// Tuple return
(int Sum, int Product) Calculate(int a, int b)
{
    return (a + b, a * b);
}
```

### Deconstruction

```csharp
class Point
{
    public int X { get; set; }
    public int Y { get; set; }
    
    public void Deconstruct(out int x, out int y)
    {
        x = X;
        y = Y;
    }
}

var point = new Point { X = 10, Y = 20 };
var (x, y) = point;
```

### String Interpolation

```csharp
string name = "Alice";
int age = 30;

// Basic interpolation
string message = $"Hello, {name}!";

// Expression interpolation
string info = $"{name} is {age} years old";

// Format specifiers
double pi = 3.14159;
string formatted = $"Pi: {pi:F2}";

// Verbatim interpolated
string path = $@"C:\Users\{name}\Documents";
```

### Collection Initializers

```csharp
// List initializer
var numbers = new List<int> { 1, 2, 3, 4, 5 };

// Dictionary initializer
var map = new Dictionary<string, int>
{
    ["one"] = 1,
    ["two"] = 2,
    ["three"] = 3
};

// Collection expressions (C# 12+)
int[] array = [1, 2, 3, 4, 5];
```

### Index and Range (C# 8+)

```csharp
string text = "Hello, World!";

// Index from end
char last = text[^1];      // '!'
char secondLast = text[^2]; // 'd'

// Range
string hello = text[0..5];   // "Hello"
string world = text[7..12];  // "World"
string all = text[..];       // entire string
string fromFifth = text[5..]; // ", World!"
```

### Default Interface Methods (C# 8+)

```csharp
interface ILogger
{
    void Log(string message);
    
    // Default implementation
    void LogInfo(string message)
    {
        Log($"INFO: {message}");
    }
}
```

### Operators

#### Arithmetic
```csharp
int sum = a + b;
int diff = a - b;
int product = a * b;
int quotient = a / b;
int remainder = a % b;
```

#### Comparison
```csharp
bool equal = a == b;
bool notEqual = a != b;
bool greater = a > b;
bool less = a < b;
bool greaterOrEqual = a >= b;
bool lessOrEqual = a <= b;
```

#### Logical
```csharp
bool and = a && b;
bool or = a || b;
bool not = !a;
```

#### Null-Coalescing
```csharp
string result = nullableString ?? "default";
nullableString ??= "value"; // Assign if null
```

#### Conditional
```csharp
int result = condition ? trueValue : falseValue;
```

## Limitations

While CRAB supports the full C# language, certain features have WebAssembly-specific considerations:

1. **Reflection** - Compile-time only, no runtime reflection
2. **Dynamic** - Resolved at compile time
3. **Threading** - Single-threaded WASM MVP
4. **Assembly Loading** - All assemblies resolved at compile time

## See Also

- [Getting Started](Getting-Started.md) - Quick start guide
- [Memory Management](Memory-Management.md) - CTGC and manual memory
- [Advanced Examples](../Examples/Advanced-Features.md) - Complex feature examples
