# CRAB Test Files

This folder contains test files for the CRAB compiler. Each test file contains embedded C# code, .sln, and .slnx content that is automatically extracted and used to generate a complete project when the test command is run.

## Test File Format

Test files follow this structure:

```csharp
// CRAB Test File: ProjectName
// Description of what this test validates

// === BEGIN CSHARP ===
using System;

namespace ProjectName
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Hello from CRAB!");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
// === END CSHARP ===

// === BEGIN SLN ===
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
...
// === END SLN ===

// === BEGIN SLNX ===
<?xml version="1.0" encoding="utf-8"?>
<Solution Version="1.0">
...
</Solution>
// === END SLNX ===
```

## Running Tests

### Run All Tests
```bash
crab test
```

### Run a Specific Test
```bash
crab test --name HelloWorld
```

### Keep Generated Projects
```bash
crab test --keep
```

### Save All Outputs
```bash
crab test --save
```

This will generate:
- `tests/ProjectName/binaries/` - All compiled binaries
  - `ProjectName.x86-64.exe`, `ProjectName.x86-32.exe`
  - `ProjectName.ARM64.exe`, `ProjectName.ARM32.exe`
  - `ProjectName.x86-64.bin`, `ProjectName.x86-32.bin`, `ProjectName.x86-16.bin`
  - `ProjectName.ARM64.bin`, `ProjectName.ARM32.bin`
- `tests/ProjectName/wasm/` - WebAssembly outputs
  - `ProjectName.wasm`
  - `ProjectName.js`
  - `ProjectName.html`
- `tests/ProjectName/logs/` - Debug and info logs

### Quick Test (Single Architecture)
```bash
crab test --quick --arch x86_64 --format pe
```

### Verbose Output
```bash
crab test --verbose
```

### Debug Mode
```bash
crab test --debug
```

## Available Tests

- **HelloWorld.cs** - Basic Hello World example with Console I/O
- **Calculator.cs** - Simple calculator class with arithmetic operations
- **ClassHierarchy.cs** - Class hierarchy and inheritance test
- **GenericCollections.cs** - Generic collections test

## Adding New Tests

To add a new test:

1. Create a new `.cs` file in this directory
2. Follow the test file format above
3. Include the three sections: CSHARP, SLN, and SLNX
4. Run `crab test --name YourTestName` to test it

## Standard Library Support

Tests can use the CRAB standard library, including:

### System.Console
- `Console.WriteLine()` - Write to console
- `Console.ReadKey()` - Read a key press (prevents PE window auto-close)
- `Console.ReadLine()` - Read a line of input

### System Types
- `Boolean`, `Int32`, `Int64`, `Char`
- `String`, `Object`, `Array`
- `Exception`, `NullReferenceException`, `IndexOutOfRangeException`
- `Guid`, `DateTime`

## Notes

- The test command automatically generates projects from test files
- Generated projects include .csproj, .sln, and .slnx files
- All architectures are tested by default (x86_64, x86_32, x86_16, ARM64, ARM32)
- Both native and PE formats are tested where supported
- WebAssembly output is always generated
