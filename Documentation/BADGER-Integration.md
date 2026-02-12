# BADGER Integration

This document describes the integration of BADGER into the CRAB compiler pipeline.

## Pipeline Overview

CRAB now supports a complete compilation pipeline from C# source code to native assembly:

```
C# Source Code
    ↓
[CRAB Compiler]
    ↓
WebAssembly Text (WAT)
    ↓
[BADGER Assembler]
    ↓
Native Assembly Binary
```

## Components

### CRAB
- Input: C# source files
- Output: WebAssembly Text format (WAT)
- Handles: Parsing, semantic analysis, memory verification, WASM generation

### BADGER
- Input: WebAssembly Text format (WAT)
- Output: Native assembly binary
- Handles: WAT parsing, architecture-specific lowering, assembly encoding, binary emission

## Dependencies

Both CRAB and BADGER depend on **CDTk** (Compiler Development Toolkit):
- CRAB uses CDTk for C# parsing
- BADGER uses CDTk for WAT parsing

## Usage

### Compile C# to WAT (default behavior)

```bash
# Compile to WebAssembly text format
crab compile input.cs --output output.wasm

# Or using the build command
crab build --project ./MyProject
```

### Compile C# to Native Assembly

```bash
# Compile directly to x86_64 native binary
crab compile input.cs --to-asm --output output.bin

# Specify architecture and format
crab compile input.cs --to-asm --arch x86_64 --format native --output program.bin

# Compile to Windows PE executable
crab compile input.cs --to-asm --arch x86_64 --format pe --output program.exe

# Build entire project to native assembly
crab build --project ./MyProject --to-asm --arch x86_64 --format native
```

### Supported Architectures

- `x86_64` - 64-bit x86 (default)
- `x86_32` - 32-bit x86
- `x86_16` - 16-bit x86 (real mode)
- `arm64` - 64-bit ARM
- `arm32` - 32-bit ARM

### Output Formats

- `native` - Flat binary with no headers (bare metal)
- `pe` - Windows Portable Executable format

## Command Reference

### compile

```bash
crab compile <input.cs> [options]

Options:
  --output <file>     Output file path
  --verbose           Enable verbose output
  --verify            Run additional verification passes
  --optimize          Enable optimizations (default: true)
  --to-asm            Compile to native assembly using BADGER
  --arch <arch>       Target architecture (when using --to-asm)
  --format <fmt>      Output format (when using --to-asm)
```

### build

```bash
crab build [options]

Options:
  --project <dir>     Project directory (default: current directory)
  --output <dir>      Output directory (default: ./bin)
  --config <cfg>      Build configuration (debug or release)
  --verbose           Enable verbose output
  --to-asm            Build to native assembly using BADGER
  --arch <arch>       Target architecture (when using --to-asm)
  --format <fmt>      Output format (when using --to-asm)
```

## Architecture

### Project Structure

```
CRAB/
├── CRAB.csproj              # Main CRAB compiler project
├── Dependencies/
│   ├── CDTk/
│   │   └── CDTk.csproj      # Compiler toolkit library
│   └── BADGER/
│       └── Badger.csproj    # WAT assembler library
```

### Project References

CRAB.csproj references both dependencies as project references:

```xml
<ItemGroup>
  <ProjectReference Include="Dependencies/CDTk/CDTk.csproj" />
  <ProjectReference Include="Dependencies/BADGER/Badger.csproj" />
</ItemGroup>
```

### BADGER API

BADGER is now a library (not an executable) and exposes the following API:

```csharp
namespace Badger;

public class BadgerCompiler
{
    // Compile WAT text to binary
    public static byte[] Compile(
        string watInput, 
        string architecture = "x86_64", 
        string format = "native"
    );
    
    // Compile WAT file to binary file
    public static void CompileFile(
        string inputFile, 
        string outputFile, 
        string architecture = "x86_64", 
        string format = "native"
    );
}
```

## Examples

### Example 1: Simple Hello World to x86_64

```bash
# Create a simple C# program
echo 'Console.WriteLine("Hello World");' > hello.cs

# Compile to native x86_64 binary
crab compile hello.cs --to-asm --arch x86_64 --format native --output hello.bin

# File hello.bin is now a native x86_64 executable binary
```

### Example 2: Build Project to Windows PE

```bash
# Build entire project to Windows executable
crab build --project ./MyApp --to-asm --arch x86_64 --format pe

# Output will be in ./MyApp/bin/output.exe
```

### Example 3: Cross-compile to ARM64

```bash
# Compile C# to ARM64 bare metal binary
crab compile app.cs --to-asm --arch arm64 --format native --output app-arm64.bin
```

### Example 4: Verbose Compilation

```bash
# See detailed pipeline steps
crab compile program.cs --to-asm --verbose

# Output will show:
# [1/6] Reading source files...
# [2/6] Compiling with CDTk pipeline...
# [3/6] Memory analysis complete...
# [4/6] Manual memory verification complete...
# [5/6] WebAssembly generation complete...
# [6/6] Writing output...
#       Invoking BADGER to compile WAT to x86_64 assembly...
#       BADGER compiled XXX bytes...
# ✓ Compilation successful: C# -> WAT -> X86_64 ASM
```

## Pipeline Details

When `--to-asm` is specified:

1. **CRAB Frontend**: Parses C# source using CDTk
2. **CRAB Semantic Analysis**: Verifies memory safety (CTGC, manual verification)
3. **CRAB Backend**: Generates WebAssembly Text (WAT) format
4. **BADGER Frontend**: Parses WAT using CDTk
5. **BADGER Backend**: 
   - Lowers WAT to architecture-specific assembly
   - Assembles to machine code
   - Emits binary in specified format

## Benefits

- **End-to-end compilation**: Single command from C# to native binary
- **Memory safety**: All CRAB memory guarantees preserved through the pipeline
- **Cross-platform**: Generate binaries for multiple architectures
- **Flexible output**: Choose between bare metal or OS-specific formats
- **Transparent pipeline**: Each stage can be inspected independently

## Future Enhancements

- Additional architectures (RISC-V, MIPS, etc.)
- Additional output formats (ELF, Mach-O, etc.)
- Optimization passes in BADGER
- Debug symbol generation
- Link-time optimization
