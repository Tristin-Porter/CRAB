# StandardLibrary and HTML Test Generation - Implementation Summary

## Overview

This implementation adds a new StandardLibrary to the CRAB compiler and enhances the test command to generate HTML files for easy browser-based testing.

## What Was Added

### 1. StandardLibrary Folder Structure

Created a new top-level `StandardLibrary/` directory with the following structure:

```
StandardLibrary/
├── Console.cs                    # Basic console I/O utilities
├── StandardLibrary.csproj        # Project file
├── README.md                     # Documentation
└── Examples/                     # Example programs
    ├── HelloWorld.cs            # Basic console output example
    ├── InteractiveConsole.cs    # Interactive I/O example
    └── README.md                # Examples documentation
```

### 2. Console.cs - Standard Library Features

The `CRAB.StandardLibrary.Console` class provides essential console operations:

**Output Functions:**
- `Print(string text)` - Print without newline
- `WriteLine(string text)` - Print with newline
- `WriteLine()` - Print empty line

**Input Functions:**
- `ReadLine()` - Read line from console
- `ReadKey()` - Read single key
- `ReadKey(bool intercept)` - Read key with optional intercept

**Utility Functions:**
- `WaitForKey()` - Display "Press any key..." and wait
- `Clear()` - Clear console output

### 3. Enhanced Test Command

Modified `CLI/Commands/Test.cs` to automatically save HTML and JS files when using the `--save` flag:

**Before:**
- Only saved WASM/WAT files to `tests/{project}/wasm/`

**After:**
- Saves WASM/WAT files to `tests/{project}/wasm/`
- Saves JS wrapper to `tests/{project}/wasm/output.js`
- Saves HTML runner to `tests/{project}/wasm/index.html`

### 4. Build System Fixes

Created `Directory.Build.props` to resolve .NET 10 SDK duplicate attribute issues:

```xml
<Project>
  <PropertyGroup>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
  </PropertyGroup>
</Project>
```

Updated `CRAB.csproj` to exclude StandardLibrary from compilation:
```xml
<Compile Remove="StandardLibrary/**/*.cs" />
```

## Usage Examples

### Using StandardLibrary in Your Code

```csharp
using CRAB.StandardLibrary;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello from CRAB!");
        Console.Print("Enter your name: ");
        string name = Console.ReadLine();
        Console.WriteLine($"Welcome, {name}!");
        Console.WaitForKey();
    }
}
```

### Running Tests with HTML Generation

```bash
# Run quick test and save all outputs including HTML
crab test --quick --save

# Run comprehensive test with HTML generation
crab test --save

# Test specific project with verbose output
crab test --name HelloWorld --save --verbose
```

### Test Output Structure

When using `--save`, tests generate the following structure:

```
tests/
└── ProjectName/
    ├── wasm/
    │   ├── output.wasm      # WebAssembly binary
    │   ├── output.js        # JavaScript wrapper
    │   └── index.html       # HTML test runner
    ├── binaries/
    │   └── (native binaries for each architecture)
    └── logs/
        ├── debug.log
        └── info.log
```

## How HTML Generation Works

1. **Build Command** generates the files:
   - `Build.cs` calls `HtmlGenerator.GenerateHtml()` 
   - Creates `index.html`, `output.js`, and `output.wasm` in the bin folder

2. **Test Command** copies the files:
   - When `--save` flag is used
   - Copies all three files (HTML, JS, WASM) to `tests/{project}/wasm/`
   - Previous behavior only copied WASM/WAT

3. **HTML File** provides:
   - Styled interface for running WASM in browser
   - Console output display
   - Error handling and status indicators
   - Instructions for local server setup

## Testing the HTML Files

To test a generated HTML file in the browser:

```bash
# After running test with --save
cd tests/HelloWorld/wasm
python -m http.server 8000

# Open browser to http://localhost:8000/index.html
```

## Files Modified

1. **Created:**
   - `StandardLibrary/Console.cs`
   - `StandardLibrary/StandardLibrary.csproj`
   - `StandardLibrary/README.md`
   - `StandardLibrary/Examples/HelloWorld.cs`
   - `StandardLibrary/Examples/InteractiveConsole.cs`
   - `StandardLibrary/Examples/README.md`
   - `Directory.Build.props`

2. **Modified:**
   - `CLI/Commands/Test.cs` - Added HTML/JS file copying
   - `CRAB.csproj` - Excluded StandardLibrary from build

3. **Unchanged (already working):**
   - `Compiler/Core/HtmlGenerator.cs` - Already generates HTML
   - `CLI/Commands/Build.cs` - Already calls HtmlGenerator

## Benefits

1. **StandardLibrary:**
   - Provides basic console I/O for CRAB programs
   - Easy to use, familiar C# Console API
   - Foundation for future standard library additions
   - Well-documented with examples

2. **HTML Test Generation:**
   - Tests now produce browser-ready files
   - Easy to verify WASM output in browser
   - Better debugging with visual output
   - Complete test artifacts saved for inspection

3. **Build System:**
   - Fixed .NET 10 SDK compatibility issues
   - Clean separation between main project and StandardLibrary
   - Proper exclusion of examples and test files

## Future Enhancements

The StandardLibrary can be extended with:
- String utilities (formatting, parsing)
- Math functions
- Collections (List, Dictionary, etc.)
- File I/O (when supported by WASM environment)
- Memory management utilities
- Diagnostics and assertions

## Verification

All changes have been tested and verified:
- ✅ StandardLibrary builds successfully
- ✅ Main CRAB project builds without errors
- ✅ Tests generate HTML, JS, and WASM files
- ✅ HTML files are properly formatted and functional
- ✅ `--save` flag copies all browser files to tests folder
- ✅ Examples compile and demonstrate StandardLibrary usage
- ✅ No build system conflicts or duplicate attribute errors
