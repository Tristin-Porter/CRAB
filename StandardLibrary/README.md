# CRAB Standard Library

The CRAB Standard Library provides basic functionality for programs compiled with the CRAB compiler.

## Overview

This library contains minimal, essential functions needed for basic C# programs compiled to WebAssembly, including:

- Console I/O operations
- Basic utilities to keep console open
- String operations (future)
- Math operations (future)
- Collections (future)

## Current Features

### Console Operations

The `Console` class provides basic console I/O:

```csharp
using CRAB.StandardLibrary;

// Print to console
Console.Print("Hello");
Console.WriteLine("Hello, World!");
Console.WriteLine();  // Empty line

// Read from console
string input = Console.ReadLine();
ConsoleKeyInfo key = Console.ReadKey();

// Keep console open
Console.WaitForKey();  // Displays "Press any key to continue..."

// Clear console
Console.Clear();
```

## Usage in CRAB Programs

To use the standard library in your CRAB program:

```csharp
using CRAB.StandardLibrary;

class Program
{
    static void Main()
    {
        Console.WriteLine("Welcome to CRAB!");
        Console.WaitForKey();
    }
}
```

## Future Additions

Planned additions to the standard library:

- **String utilities**: String manipulation, formatting, parsing
- **Math functions**: Basic math operations, constants
- **Collections**: List, Dictionary, Set implementations
- **File I/O**: Basic file operations (if supported by WASM environment)
- **Memory utilities**: Memory management helpers
- **Diagnostics**: Debug and assert utilities

## Building

The standard library is built as part of the CRAB solution:

```bash
cd StandardLibrary
dotnet build
```

Or build from the root:

```bash
dotnet build CRAB.sln
```

## Note

This standard library is designed specifically for CRAB's compilation model:

- **Zero runtime**: No .NET runtime required
- **CTGC compatible**: Works with Compile-Time Garbage Collection
- **WASM-safe**: All operations are safe for WebAssembly target
- **Minimal footprint**: Only essential functionality included

Functions in this library are recognized by the CRAB compiler and lowered directly to WebAssembly instructions during compilation.
