# BADGER Documentation

**BADGER** — Better Assembler for Dependable Generation of Efficient Results

BADGER is the canonical assembler and WAT-lowering backend for the CRAB toolchain. It takes standard WAT (WebAssembly Text format) as input, lowers it to architecture-specific assembly, and assembles that assembly into executable machine code contained in either Native (bare metal) or PE (Windows) binaries.

## Table of Contents

1. [Overview](01-Overview.md)
2. [Architecture](02-Architecture.md)
3. [WAT Parsing](03-WAT-Parsing.md)
4. [Target Architectures](04-Target-Architectures.md)
   - [x86_64](05-x86_64.md)
   - [x86_32](06-x86_32.md)
   - [x86_16](07-x86_16.md)
   - [ARM64](08-ARM64.md)
   - [ARM32](09-ARM32.md)
5. [Assembly Encoding](10-Assembly-Encoding.md)
6. [Container Formats](11-Container-Formats.md)
   - [Native (Bare Metal)](12-Native-Format.md)
   - [PE (Windows)](13-PE-Format.md)
7. [Usage Guide](14-Usage-Guide.md)
8. [Testing](15-Testing.md)
9. [Troubleshooting](16-Troubleshooting.md)
10. [Contributing](17-Contributing.md)

## Quick Start

```bash
# Build BADGER
dotnet build

# Compile a WAT file to native x86_64 binary
dotnet run input.wat -o output.bin --arch x86_64 --format native

# Compile a WAT file to PE executable
dotnet run input.wat -o output.exe --arch x86_64 --format pe
```

## Key Features

- **Standard WAT Input**: Accepts standard WebAssembly Text format (no custom dialect)
- **Multiple Architectures**: x86_64, x86_32, x86_16, ARM64, ARM32
- **Two Container Formats**: Native (bare metal) and PE (Windows)
- **CDTk-Based Parsing**: Uses CDTk for robust WAT parsing
- **Deterministic Output**: Same input always produces same output
- **Comprehensive Testing**: 45+ tests covering all subsystems

## System Requirements

- .NET 10.0 or later
- CDTk package (automatically installed via NuGet)

## License

See [LICENSE.txt](../LICENSE.txt) for license information.

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/Tristin-Porter/BADGER).
