# CRAB Standard Library Examples

This directory contains example programs demonstrating how to use the CRAB Standard Library.

## Examples

### HelloWorld.cs
A simple example showing basic console output with the CRAB Standard Library.

```bash
# Compile and run
crab compile HelloWorld.cs
crab run output.wat
```

### InteractiveConsole.cs
Demonstrates interactive console operations including:
- Reading user input
- Printing with and without newlines
- Waiting for key press
- Clearing the console

```bash
# Compile and run
crab compile InteractiveConsole.cs
crab run output.wat
```

## Running Examples

All examples can be compiled using the CRAB compiler:

```bash
# Compile to WebAssembly
crab compile <example>.cs

# Run the compiled output
crab run output.wat

# Or run in browser
crab compile <example>.cs
cd bin
python -m http.server 8000
# Open http://localhost:8000/index.html
```

## Creating Your Own Examples

To create your own example using the Standard Library:

1. Create a new `.cs` file
2. Add `using CRAB.StandardLibrary;` at the top
3. Use the Console class for I/O operations
4. Compile with CRAB

Example:

```csharp
using CRAB.StandardLibrary;

class Program
{
    static void Main()
    {
        Console.WriteLine("My CRAB Program");
        Console.WaitForKey();
    }
}
```
