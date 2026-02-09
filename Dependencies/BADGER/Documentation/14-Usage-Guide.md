# Usage Guide

## Installation

### Prerequisites
- .NET 10.0 SDK or later
- Git (for cloning repository)

### Clone Repository

```bash
git clone https://github.com/Tristin-Porter/BADGER.git
cd BADGER
```

### Build

```bash
dotnet build
```

This will:
- Restore NuGet packages (including CDTk)
- Compile BADGER
- Run the test suite (45 tests)

## Basic Usage

### Command Line Syntax

```bash
dotnet run <input.wat> [options]
```

### Options

- `-o <output>` - Output file path (default: `output.bin`)
- `--arch <arch>` - Target architecture (default: `x86_64`)
  - `x86_64` - 64-bit x86
  - `x86_32` - 32-bit x86
  - `x86_16` - 16-bit x86
  - `arm64` - 64-bit ARM
  - `arm32` - 32-bit ARM
- `--format <fmt>` - Output format (default: `native`)
  - `native` - Flat binary (bare metal)
  - `pe` - Windows executable

## Examples

### Compile WAT to x86_64 Native Binary

```bash
dotnet run program.wat -o program.bin --arch x86_64 --format native
```

### Compile WAT to Windows Executable

```bash
dotnet run program.wat -o program.exe --arch x86_64 --format pe
```

### Compile WAT to ARM64 Native Binary

```bash
dotnet run program.wat -o program.bin --arch arm64 --format native
```

### Compile WAT to 16-bit x86 Boot Sector

```bash
dotnet run bootloader.wat -o boot.bin --arch x86_16 --format native
```

## WAT Input Format

BADGER accepts standard WebAssembly Text format:

### Simple Function

```wasm
(module
  (func $add (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.add
  )
)
```

### Function with Locals

```wasm
(module
  (func $fibonacci (param $n i32) (result i32)
    (local $a i32)
    (local $b i32)
    (local $temp i32)
    
    i32.const 0
    local.set $a
    
    i32.const 1
    local.set $b
    
    ;; ... implementation
  )
)
```

### Control Flow

```wasm
(module
  (func $abs (param $x i32) (result i32)
    local.get $x
    i32.const 0
    i32.ge_s
    if (result i32)
      local.get $x
    else
      local.get $x
      i32.const -1
      i32.mul
    end
  )
)
```

## Running Tests

BADGER includes a comprehensive test suite that runs automatically at startup:

```bash
dotnet run
```

Output:
```
================================================================================
BADGER Test Suite
================================================================================

WAT Parser Tests:
----------------
  ✓ WAT tokens are defined
  ✓ WAT grammar rules are defined
  ...

Test Results: 45/45 passed, 0 failed
================================================================================
```

To run without tests, modify `Program.cs` to comment out the test runner.

## Development Workflow

### 1. Write WAT Code

Create a `.wat` file with your WebAssembly program:

```wasm
(module
  (func $main (result i32)
    i32.const 42
  )
)
```

### 2. Compile

```bash
dotnet run program.wat -o program.bin --arch x86_64 --format native
```

### 3. Verify Output

```bash
# Check file was created
ls -lh program.bin

# View hex dump
xxd program.bin | head
```

### 4. Test (if bare metal)

```bash
# Run in QEMU
qemu-system-x86_64 -kernel program.bin

# Or run in SHARK (CRAB runtime)
shark run program.bin
```

### 5. Test (if PE)

```bash
# On Windows
program.exe

# On Linux with Wine
wine program.exe
```

## Output Files

### Native Binary (.bin)

- Raw machine code
- No headers
- Load at fixed address
- Run in emulator or bare metal

### PE Executable (.exe)

- Windows executable
- DOS stub + PE headers
- Run directly on Windows
- Debug with Windows tools

## Troubleshooting

See: [Troubleshooting](16-Troubleshooting.md)

## Advanced Usage

### Custom Architecture

To compile for a specific architecture version:

```bash
# Modern x86_64 with all extensions
dotnet run program.wat --arch x86_64 -o modern.bin

# Legacy 16-bit x86 for old hardware
dotnet run program.wat --arch x86_16 -o legacy.bin
```

### Pipeline Stages

BADGER's pipeline:

```
WAT Input
   ↓
[Parse] (CDTk)
   ↓
WAT AST
   ↓
[Lower] (Architecture-specific)
   ↓
Assembly Text
   ↓
[Assemble] (Architecture-specific)
   ↓
Machine Code
   ↓
[Emit] (Container-specific)
   ↓
Binary File
```

## Integration with CRAB

BADGER is designed as the backend for CRAB:

```
CRAB Source → CRAB Compiler → WAT → BADGER → Binary
```

CRAB will automatically invoke BADGER with appropriate options.

## Best Practices

1. **Start with x86_64/native** - Easiest to test and debug
2. **Test incrementally** - Compile and verify often
3. **Use standard WAT** - No custom extensions
4. **Check test suite** - Ensure all tests pass before deployment
5. **Verify binaries** - Use hexdump to check output

## Common Patterns

### Hello World (Bare Metal)

```wasm
(module
  (func $start
    ;; VGA text mode write
    i32.const 0xB8000
    i32.const 72  ;; 'H'
    i32.store8
  )
)
```

### Simple Calculation

```wasm
(module
  (func $calculate (result i32)
    i32.const 10
    i32.const 20
    i32.add
    i32.const 2
    i32.mul
  )
)
```

## Getting Help

- Check [Troubleshooting](16-Troubleshooting.md)
- Review [Testing](15-Testing.md) for examples
- See architecture-specific docs for details
- Open an issue on GitHub
