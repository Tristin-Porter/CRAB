# BADGER

**Better Assembler for Dependable Generation of Efficient Results**

BADGER is a specialized assembler and WAT-lowering backend designed specifically for the CRAB toolchain. It transforms standard WebAssembly Text format (WAT) into executable machine code for multiple architectures, emitting either bare metal binaries or Windows PE executables.

## Features

- **Standard WAT Input** - Accepts standard WebAssembly Text format (no custom dialect)
- **Multi-Architecture Support** - x86_64, x86_32, x86_16, ARM64, ARM32
- **Dual Container Formats** - Native (bare metal) and PE (Windows) binaries
- **CDTk-Based Parsing** - Robust WAT parsing using CDTk grammar framework
- **Deterministic Output** - Same input always produces identical output
- **Comprehensive Testing** - 45+ tests covering all subsystems
- **Pure C# Implementation** - Entirely written in C#

## Quick Start

```bash
# Clone and build
git clone https://github.com/Tristin-Porter/BADGER.git
cd BADGER
dotnet build

# Compile WAT to native x86_64 binary
dotnet run input.wat -o output.bin --arch x86_64 --format native

# Compile WAT to Windows executable
dotnet run input.wat -o output.exe --arch x86_64 --format pe
```

## Supported Architectures

| Architecture | Description | Status |
|-------------|-------------|--------|
| **x86_64** | 64-bit x86 (primary) | ✓ Implemented |
| **x86_32** | 32-bit x86 | ✓ Implemented |
| **x86_16** | 16-bit x86 (real mode) | ✓ Implemented |
| **ARM64** | 64-bit ARM | ✓ Implemented |
| **ARM32** | 32-bit ARM | ✓ Implemented |

## Container Formats

- **Native**: Flat binary with no headers, suitable for bare metal execution, bootloaders, or emulators like QEMU
- **PE**: Minimal Portable Executable format for Windows

## Usage

### Command Line

```bash
dotnet run <input.wat> [options]
```

### Options

- `-o <output>` - Output file path (default: `output.bin`)
- `--arch <arch>` - Target architecture: `x86_64`, `x86_32`, `x86_16`, `arm64`, `arm32` (default: `x86_64`)
- `--format <fmt>` - Output format: `native`, `pe` (default: `native`)

### Examples

**Compile for x86_64 bare metal:**
```bash
dotnet run program.wat -o program.bin --arch x86_64 --format native
```

**Compile for Windows:**
```bash
dotnet run program.wat -o program.exe --arch x86_64 --format pe
```

**Compile for ARM64:**
```bash
dotnet run program.wat -o program.bin --arch arm64 --format native
```

**Compile for 16-bit x86 boot sector:**
```bash
dotnet run bootloader.wat -o boot.bin --arch x86_16 --format native
```

## WAT Input Format

BADGER accepts standard WebAssembly Text format. Here's a simple example:

```wasm
(module
  (func $add (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.add
  )
)
```

More complex example with control flow:

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

## Architecture

BADGER follows a four-stage compilation pipeline:

```
WAT Input
   ↓
[1. Parse] (CDTk)
   ↓
WAT AST
   ↓
[2. Lower] (Architecture-specific)
   ↓
Assembly Text
   ↓
[3. Assemble] (Architecture-specific)
   ↓
Machine Code
   ↓
[4. Emit] (Container-specific)
   ↓
Binary File
```

### 1. WAT Parsing
- Uses CDTk grammar framework
- Parses standard WAT into AST
- Supports modules, functions, locals, blocks, and all standard instructions

### 2. Architecture Lowering
- Transforms stack-based WAT to register/stack-based architectures
- Generates canonical assembly dialect
- Handles control flow, function calls, and local variables

### 3. Assembly Encoding
- Two-pass assembler (label resolution + encoding)
- Architecture-specific instruction encoding
- Resolves labels and computes offsets

### 4. Container Emission
- Native: Flat binary with entrypoint at offset 0
- PE: Minimal Windows executable with code section

## Design Principles

### Sovereignty and Minimalism
BADGER is not a general-purpose assembler. It only supports:
- WAT instructions and patterns that CRAB emits
- Minimal, canonical assembly dialect for each architecture
- Two container formats: Native and PE

### Predictability
- Completely deterministic behavior
- Same WAT input always produces identical output
- No hidden optimizations or transformations
- Explicit, traceable lowering rules

### Modularity
- Each architecture is isolated and self-contained
- Adding new architectures requires no changes to existing ones
- Minimal shared infrastructure

## System Requirements

- .NET 10.0 SDK or later
- CDTk package (automatically installed via NuGet)

## Testing

BADGER includes a comprehensive test suite with 45+ tests. The test suite **automatically runs on every startup** before any compilation:

```bash
# Run without arguments to see tests and usage
dotnet run

# Tests run before compilation even with arguments
dotnet run input.wat -o output.bin
```

The test suite covers:
- WAT parsing and grammar
- WAT to assembly lowering for each architecture
- Assembly encoding and instruction generation
- Label resolution and branch correctness
- Container emission (Native and PE)
- End-to-end integration tests

## Documentation

Comprehensive documentation is available in the [`Documentation/`](Documentation/) directory:

- [Overview](Documentation/01-Overview.md) - Project overview and design principles
- [Architecture](Documentation/02-Architecture.md) - System architecture and pipeline
- [WAT Parsing](Documentation/03-WAT-Parsing.md) - WAT parsing with CDTk
- [Target Architectures](Documentation/04-Target-Architectures.md) - Supported architectures
  - [x86_64](Documentation/05-x86_64.md)
  - [x86_32](Documentation/06-x86_32.md)
  - [x86_16](Documentation/07-x86_16.md)
  - [ARM64](Documentation/08-ARM64.md)
  - [ARM32](Documentation/09-ARM32.md)
- [Assembly Encoding](Documentation/10-Assembly-Encoding.md) - Instruction encoding
- [Container Formats](Documentation/11-Container-Formats.md) - Binary formats
  - [PE Format](Documentation/13-PE-Format.md)
- [Usage Guide](Documentation/14-Usage-Guide.md) - Detailed usage examples
- [Testing](Documentation/15-Testing.md) - Test suite documentation
- [Troubleshooting](Documentation/16-Troubleshooting.md) - Common issues and solutions
- [Contributing](Documentation/17-Contributing.md) - Contribution guidelines

## Non-Goals

BADGER explicitly does **not**:
- Support arbitrary user-written assembly
- Emit ELF, .deb, or other Linux-specific formats
- Implement its own WAT parser (uses CDTk)
- Support custom WAT dialects
- Act as a general-purpose assembler
- Perform complex optimizations (optimization happens in CRAB)

## Integration with CRAB

BADGER serves as the backend for the CRAB toolchain:

```
CRAB Source → CRAB Compiler → WAT → BADGER → Binary
```

CRAB automatically invokes BADGER with appropriate options for the target platform.

## Project Structure

```
BADGER/
├── Program.cs              # Main entry point and CLI
├── Architectures/          # Architecture-specific lowering and assembly
│   ├── x86_64.cs
│   ├── x86_32.cs
│   ├── x86_16.cs
│   ├── ARM64.cs
│   └── ARM32.cs
├── Containers/             # Container format emitters
│   ├── Native.cs
│   └── PE.cs
└── Documentation/          # Detailed documentation
```

## Contributing

We welcome contributions! Please see [Contributing](Documentation/17-Contributing.md) for guidelines.

### Development Workflow

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Ensure all tests pass (`dotnet run`)
5. Submit a pull request

### Adding a New Architecture

Each architecture is modular and isolated:

1. Create `Architectures/YourArch.cs`
2. Implement `Lowering` class (WAT → assembly)
3. Implement `Assembler` class (assembly → machine code)
4. Add tests in `Testing/`
5. Update documentation

## License

See [LICENSE.txt](LICENSE.txt) for license information.

## Authors

Created as part of the CRAB toolchain project.

## Support

- **Issues**: [GitHub Issues](https://github.com/Tristin-Porter/BADGER/issues)
- **Documentation**: See [`Documentation/`](Documentation/) directory
- **Questions**: Open a GitHub Discussion

## Acknowledgments

- **CDTk** - Grammar toolkit for WAT parsing
- **CRAB** - The parent toolchain project
