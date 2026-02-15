# CRAB Project System Support

## Overview

CRAB now supports standard MSBuild project files instead of relying on `.crab` files. You can use:
- `.sln` - Visual Studio Solution files
- `.slnx` - XML-based solution files (Visual Studio 2022+)
- `.csproj` - C# project files

## Supported Build Inputs

The CRAB compiler can now build from:

1. **Solution files (.sln)**
   ```bash
   crab build MySolution.sln
   ```

2. **XML Solution files (.slnx)**
   ```bash
   crab build MySolution.slnx
   ```

3. **Project files (.csproj)**
   ```bash
   crab build MyProject.csproj
   ```

4. **Directories** (auto-discovers .sln, .slnx, or .csproj)
   ```bash
   crab build ./MyProject
   ```

5. **Single C# files**
   ```bash
   crab compile Program.cs
   ```

## How It Works

### Solution File Parsing (.sln)

The CRAB compiler parses Visual Studio solution files to extract project references:

```
Microsoft Visual Studio Solution File, Format Version 12.00
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyApp", "MyApp.csproj", "{GUID}"
EndProject
```

Features:
- Extracts all C# projects from the solution
- Follows project references recursively
- Respects project dependencies

### XML Solution File Parsing (.slnx)

CRAB also supports the newer XML-based solution format:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Solution Version="1.0">
  <Project Path="MyApp.csproj" Name="MyApp" Type="C#" />
</Solution>
```

### Project File Parsing (.csproj)

CRAB parses C# project files to discover source files:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

Features:
- **SDK-style projects**: Automatically includes all `.cs` files
- **Exclusions**: Respects `<Compile Remove="..." />` elements
- **References**: Follows `<ProjectReference>` elements
- **Auto-discovery**: Finds files matching SDK conventions

### Exclusion Examples

CRAB respects project file exclusions:

```xml
<ItemGroup>
  <Compile Remove="Dependencies\**" />
  <Compile Remove="Testing/**/*.cs" />
</ItemGroup>
```

This prevents Dependencies and Testing folders from being compiled.

## Project Discovery Algorithm

When you run `crab build <path>`, CRAB:

1. **Checks if path is a file**:
   - `.sln` → Parse solution, extract C# projects
   - `.slnx` → Parse XML solution, extract C# projects
   - `.csproj` → Parse project, extract source files
   - `.cs` → Compile single file

2. **If path is a directory**:
   - Look for `.sln` files (priority 1)
   - Look for `.slnx` files (priority 2)
   - Look for `.csproj` files (priority 3)
   - Fallback: Gather all `.cs` files

3. **Extract source files**:
   - Parse each project file
   - Apply exclusions from `<Compile Remove>`
   - Discover files (SDK-style auto-includes)
   - Follow project references

4. **Compile**:
   - Concatenate all source files
   - Run through CRAB compiler
   - Generate WASM output

## Usage Examples

### Build from Solution

```bash
# Build entire solution
crab build CRAB.sln --verbose

# Build to native assembly
crab build CRAB.sln --to-asm --arch x86_64
```

### Build from Project

```bash
# Build specific project
crab build MyApp.csproj

# Build with custom output
crab build MyApp.csproj --output ./dist/output.wasm
```

### Build from Directory

```bash
# Auto-discover solution/project
crab build ./src

# Current directory
crab build .
```

## Implementation Details

### Components

1. **SolutionFile.cs** - Parses `.sln` files
   - Extracts project entries
   - Parses GUIDs and paths
   - Handles Visual Studio format

2. **SlnxFile.cs** - Parses `.slnx` files
   - XML-based parsing
   - Extracts project paths and metadata

3. **ProjectFile.cs** - Parses `.csproj` files
   - XML-based parsing
   - SDK-style project detection
   - Exclusion pattern matching
   - Auto-discovery of source files

4. **ProjectDiscovery.cs** - Unified discovery system
   - Dispatches to appropriate parser
   - Handles all input types
   - Recursive project reference following

### Parser Features

All parsers support:
- ✅ **Relative path resolution** - Converts to absolute paths
- ✅ **Error handling** - Graceful degradation on parse errors
- ✅ **Recursive discovery** - Follows project references
- ✅ **SDK-style conventions** - Auto-includes .cs files
- ✅ **Exclusion patterns** - Glob pattern matching

### CDTk Integration

While CDTk is used for tokenization support (SolutionTokens.cs), the actual parsing uses:
- **Line-based parsing** for .sln files (traditional format)
- **XML parsing** for .slnx and .csproj files (using System.Xml.Linq)

This pragmatic approach balances:
- **Performance** - Fast parsing without full grammar
- **Maintainability** - Simple code that's easy to understand
- **Compatibility** - Works with all MSBuild formats

## Migration from .crab Files

Previously, CRAB used `.crab` files (which were just C# files with a different extension). Now:

**Before:**
```bash
crab compile mycode.crab
```

**After:**
```bash
# Use standard project files
crab build MyProject.csproj

# Or compile C# directly
crab compile mycode.cs
```

## Limitations

Current limitations:
- **Multi-targeting**: Only uses first target framework
- **Conditional compilation**: Doesn't evaluate MSBuild conditions
- **Complex glob patterns**: Basic pattern matching only
- **NuGet packages**: Not resolved (CRAB has zero dependencies by design)

These are intentional - CRAB is a zero-runtime compiler that doesn't use external dependencies.

## Testing

Test the new system:

```bash
# Test with CRAB's own solution
cd /path/to/CRAB
crab build CRAB.sln --verbose

# Test with XML solution
crab build CRAB.slnx --verbose

# Test with project
crab build CRAB.csproj --verbose
```

## Future Enhancements

Possible future additions:
- Full glob pattern matching (use library or implement)
- MSBuild condition evaluation
- Better multi-targeting support
- Configuration-specific builds

## Technical Notes

### Why Not Full CDTk Parsing?

While we created `SolutionTokens.cs` for CDTk integration, we opted for simpler parsing because:
1. **.sln format** is line-based and irregular - full grammar would be complex
2. **.slnx and .csproj** are XML - better handled by XML libraries
3. **Performance** - Direct parsing is faster
4. **Maintainability** - Less code, easier to debug

The CDTk tokenizer is available if needed for future enhancements.
