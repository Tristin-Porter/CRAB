# Getting Started with CRAB

Welcome to CRAB - the Compiler for Reliably Acceptable Binaries!

## What is CRAB?

CRAB is a revolutionary C# to WebAssembly compiler that provides:
- **100% Memory Safety** - Mathematically proven at compile-time
- **Zero Runtime** - No garbage collector, no JIT, pure WASM MVP
- **Full C# Support** - C# 1.0 through C# 13
- **Two Memory Models** - Automatic (CTGC) and Manual (verified)

## Installation

### Prerequisites
- .NET 10 SDK or later
- Git (for cloning the repository)

### Building CRAB

```bash
# Clone the repository
git clone https://github.com/yourusername/CRAB.git
cd CRAB

# Build the compiler
dotnet build

# Run the CRAB CLI
dotnet run
```

## Your First Program

Create a file called `hello.cs`:

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

### Compile with CRAB

```bash
# In the CRAB CLI
> new console hello
> compile hello.cs

# Output: hello.wasm
```

## Understanding Memory Models

CRAB provides two memory models:

### Automatic Model (Default)
Uses Compile-Time Garbage Collection (CTGC) - like normal C# but with zero runtime overhead:

```csharp
void Example()
{
    var obj = new MyClass();  // Automatically managed
    obj.DoWork();
    // Deallocation inserted automatically at optimal point
}
```

### Manual Model
For low-level control with mathematical verification:

```csharp
void LowLevel()
{
    manual {
        IntPtr buffer = Marshal.AllocHGlobal(1024);
        // ... use buffer ...
        Marshal.FreeHGlobal(buffer);
    }
    // ✓ Compiler verifies: no leaks, no use-after-free, no double-free
}
```

## Next Steps

- [Language Reference](LanguageReference.md) - Full C# language support
- [Memory Models](MemoryModels.md) - Deep dive into CTGC and manual memory
- [Compilation Guide](Compilation.md) - Advanced compilation options
- [WASM Output](WASM.md) - Working with generated WebAssembly

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/yourusername/CRAB/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/CRAB/discussions)
- **Documentation**: [Full Documentation](../README.md)

## Example: Automatic Memory Model

```csharp
using System;
using System.Linq;

class AutomaticExample
{
    static void Main()
    {
        // CTGC automatically manages all allocations
        var numbers = new int[] { 1, 2, 3, 4, 5 };
        
        var evens = numbers
            .Where(x => x % 2 == 0)
            .Select(x => x * 2)
            .ToList();
        
        foreach (var n in evens)
        {
            Console.WriteLine(n);
        }
        
        // All memory deallocated automatically at optimal points
        // Zero runtime overhead, provably safe
    }
}
```

## Example: Manual Memory Model

```csharp
using System;
using System.Runtime.InteropServices;

class ManualExample
{
    static void ProcessBuffer()
    {
        manual {
            // Allocate 1KB buffer
            IntPtr buffer = Marshal.AllocHGlobal(1024);
            
            // Use buffer...
            for (int i = 0; i < 1024; i++)
            {
                Marshal.WriteByte(buffer, i, (byte)i);
            }
            
            // Free buffer
            Marshal.FreeHGlobal(buffer);
        }
        
        // ✓ Compiler proved:
        // - No memory leaks
        // - No use-after-free
        // - No double-free
        // - No pointer escapes
        // - 100% safe
    }
}
```

## Welcome to the Future of C# Compilation!

CRAB represents a new paradigm in compiler design - combining the convenience of automatic memory management with the performance and safety of manual control, all while targeting pure WebAssembly without any runtime.

Happy compiling! 🦀
