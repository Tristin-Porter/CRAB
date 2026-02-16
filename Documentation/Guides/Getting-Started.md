# Getting Started with CRAB

Welcome to CRAB! This guide will help you start compiling C# to WebAssembly with mathematically proven memory safety.

## Installation

### Prerequisites

- .NET 10.0 SDK or later
- Git (for building from source)

### Building from Source

```bash
git clone https://github.com/Tristin-Porter/CRAB.git
cd CRAB
dotnet build CRAB.sln
```

The `crab` executable will be in `bin/Debug/net10.0/`.

### Add to PATH

```bash
export PATH=$PATH:/path/to/CRAB/bin/Debug/net10.0
```

## Your First Program

### Hello World

Create a file named `Hello.cs`:

```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello from CRAB!");
    }
}
```

### Compile to WASM

```bash
crab compile Hello.cs
```

This creates:
- `output.wasm` - WebAssembly binary
- `output.js` - JavaScript loader
- `index.html` - HTML wrapper

### Run in Browser

Open `index.html` in your browser. You'll see "Hello from CRAB!" in the console.

### Run with Node.js

```bash
node output.js
```

## CLI Commands Overview

### compile
Compile C# to WebAssembly:
```bash
crab compile myfile.cs
crab compile --output program.wasm myfile.cs
```

### build
Build a project or solution:
```bash
crab build
crab build MyProject.csproj
crab build MySolution.sln
```

### run
Compile and run (via Node.js or WASM runtime):
```bash
crab run myfile.cs
```

### test
Run test suite:
```bash
crab test
crab test --name HelloWorld
crab test --save
```

### new
Create new project:
```bash
crab new console MyApp
crab new project MyLib
```

## Project Structure

### Console Application

```
MyApp/
├── Program.cs
├── MyApp.csproj
├── MyApp.sln
└── MyApp.slnx
```

### Library Project

```
MyLib/
├── src/
│   └── MyClass.cs
├── MyLib.csproj
├── MyLib.sln
└── README.md
```

## Basic C# Features

### Variables and Types

```csharp
int number = 42;
string text = "Hello";
bool flag = true;
double pi = 3.14159;
```

### Classes and Methods

```csharp
class Calculator
{
    public int Add(int a, int b)
    {
        return a + b;
    }
    
    public int Multiply(int a, int b)
    {
        return a * b;
    }
}

class Program
{
    static void Main()
    {
        var calc = new Calculator();
        int result = calc.Add(5, 3);
        Console.WriteLine($"Result: {result}");
    }
}
```

### Control Flow

```csharp
// If statements
if (x > 10)
{
    Console.WriteLine("Greater");
}
else
{
    Console.WriteLine("Less or equal");
}

// Loops
for (int i = 0; i < 10; i++)
{
    Console.WriteLine(i);
}

while (condition)
{
    DoWork();
}

// Switch
switch (value)
{
    case 1:
        HandleOne();
        break;
    case 2:
        HandleTwo();
        break;
    default:
        HandleOther();
        break;
}
```

### Collections

```csharp
// Arrays
int[] numbers = new int[10];
numbers[0] = 42;

// Lists (automatic memory management)
var list = new List<string>();
list.Add("one");
list.Add("two");
list.Add("three");

foreach (var item in list)
{
    Console.WriteLine(item);
}
```

## Memory Management

### Automatic (Default)

```csharp
void ProcessData()
{
    var data = new byte[1024];
    // Use data...
    // Automatically freed at end of scope
}
```

### Manual (Explicit Control)

```csharp
manual
{
    var buffer = malloc(1024);
    // Use buffer...
    free(buffer); // Required
}
```

See [Memory Management Guide](Memory-Management.md) for details.

## Standard Library

### Console I/O

```csharp
// Output
Console.WriteLine("Hello!");
Console.Write("No newline");

// Input
string line = Console.ReadLine();
ConsoleKeyInfo key = Console.ReadKey();
```

### String Operations

```csharp
string text = "Hello World";
string upper = text.ToUpper();
string lower = text.ToLower();
string trimmed = text.Trim();
```

### Basic Types

```csharp
// Integers
int i32 = 42;
long i64 = 42L;

// Floating point
float f32 = 3.14f;
double f64 = 3.14159;

// Boolean
bool flag = true;

// Character
char c = 'A';
```

## Compilation Options

### Output Formats

```bash
# WebAssembly
crab compile --format wasm program.cs

# Native binary (x86-64)
crab compile --arch x86_64 --format pe program.cs

# ARM64
crab compile --arch arm64 --format native program.cs
```

### Debug Mode

```bash
crab compile --debug program.cs
```

Enables:
- Symbol information
- Assertions
- Memory debugging

### Release Mode

```bash
crab compile --release program.cs
```

Enables:
- All optimizations
- Dead code elimination
- Minimal output

## Advanced Features

### Generics

```csharp
class Container<T>
{
    private T value;
    
    public Container(T value)
    {
        this.value = value;
    }
    
    public T Get()
    {
        return value;
    }
}

var intContainer = new Container<int>(42);
var stringContainer = new Container<string>("Hello");
```

### Interfaces

```csharp
interface IProcessor
{
    void Process(string data);
}

class Processor : IProcessor
{
    public void Process(string data)
    {
        Console.WriteLine($"Processing: {data}");
    }
}
```

### Inheritance

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
```

## Common Issues

### Problem: "TokenSet not found"

**Solution:** Make sure you're compiling from the correct directory or specify full paths.

### Problem: "Memory safety violation"

**Solution:** Check that manual memory blocks properly free all allocations.

### Problem: "Build failed"

**Solution:** Run `crab build --verbose` for detailed error messages.

## Next Steps

1. **Explore Examples** - See [Examples](../Examples/) for more code
2. **Learn Memory Safety** - Read [Memory Management Guide](Memory-Management.md)
3. **Build a Project** - Create a multi-file application
4. **Study Architecture** - Understand [Compiler Architecture](../Architecture/Compiler-Architecture.md)

## Quick Reference

### Essential Commands

```bash
# Create new project
crab new console MyApp

# Compile single file
crab compile program.cs

# Build project
crab build

# Run program
crab run program.cs

# Test suite
crab test

# Help
crab help
crab help compile
```

### File Extensions

- `.cs` - C# source files
- `.csproj` - Project file
- `.sln` / `.slnx` - Solution files
- `.wasm` - WebAssembly binary
- `.wat` - WebAssembly text
- `.exe` / `.bin` - Native executables

## Resources

- [CLI Reference](CLI-Reference.md) - Complete command documentation
- [Language Features](Language-Features.md) - C# 14 support details
- [API Reference](../API/) - Standard library and APIs
- [Examples](../Examples/) - Working code examples

## Getting Help

1. Check this documentation
2. Review example code in `tests/` folder
3. Run `crab help <command>` for command help
4. See [Contributing Guide](Contributing.md) for how to ask questions

---

**Congratulations!** You're ready to start using CRAB. Happy compiling! 🦀
