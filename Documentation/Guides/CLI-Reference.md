# CLI Reference - Complete Command-Line Interface Documentation

## Overview

CRAB provides a comprehensive command-line interface for compiling C# to WebAssembly and native binaries.

## Global Options

Available for all commands:

```bash
--help, -h        Show help information
--version, -v     Show version information
--verbose         Enable verbose output
--debug           Enable debug mode
```

## Commands

### compile

Compile C# source files to WebAssembly or native code.

**Syntax:**
```bash
crab compile [options] <source-files>
```

**Options:**
- `--output <file>` - Output file name (default: output.wasm)
- `--format <format>` - Output format: wasm, wat, pe, elf, bin (default: wasm)
- `--arch <arch>` - Target architecture: x86_64, x86_32, x86_16, arm64, arm32
- `--optimize` - Enable optimizations
- `--debug` - Include debug symbols
- `--no-verify` - Skip memory verification (not recommended)
- `--ctgc-stats` - Show CTGC statistics
- `--manual-stats` - Show manual memory verification statistics

**Examples:**
```bash
# Compile to WASM
crab compile program.cs

# Compile to native x86-64 PE
crab compile --arch x86_64 --format pe program.cs

# Compile with optimizations
crab compile --optimize --output optimized.wasm program.cs

# Compile to WAT (WebAssembly Text)
crab compile --format wat program.cs
```

### build

Build a project or solution.

**Syntax:**
```bash
crab build [options] [project-or-solution]
```

**Options:**
- `--project <file>` - Project file (.csproj)
- `--solution <file>` - Solution file (.sln or .slnx)
- `--configuration <config>` - Build configuration: Debug or Release (default: Debug)
- `--verbosity <level>` - Verbosity level: quiet, minimal, normal, detailed
- `--no-restore` - Skip package restore
- `--clean` - Clean before building

**Examples:**
```bash
# Build current directory project
crab build

# Build specific project
crab build MyProject.csproj

# Build solution
crab build MySolution.sln

# Release build
crab build --configuration Release

# Build with detailed output
crab build --verbosity detailed
```

### run

Compile and run a C# program.

**Syntax:**
```bash
crab run [options] <source-file>
```

**Options:**
- `--input <file>` - Input WASM or WAT file (if already compiled)
- `--arch <arch>` - Target architecture for native execution
- `--format <format>` - Output format
- `--args <arguments>` - Arguments to pass to the program

**Examples:**
```bash
# Compile and run
crab run program.cs

# Run pre-compiled WASM
crab run --input program.wasm

# Run with arguments
crab run program.cs --args "arg1 arg2"
```

### test

Run test suite.

**Syntax:**
```bash
crab test [options] [test-name]
```

**Options:**
- `--name <name>` - Run specific test
- `--keep` - Keep generated test projects
- `--save` - Save compiled outputs
- `--verbose` - Verbose test output
- `--debug` - Debug test execution
- `--quick` - Quick test (single architecture)
- `--arch <arch>` - Architecture for quick test
- `--format <format>` - Format for quick test

**Examples:**
```bash
# Run all tests
crab test

# Run specific test
crab test --name HelloWorld

# Save test outputs
crab test --save

# Quick test
crab test --quick --arch x86_64 --format pe
```

### new

Create a new project.

**Syntax:**
```bash
crab new <template> <name> [options]
```

**Templates:**
- `console` - Console application
- `project` - Empty project/library

**Options:**
- `--name <name>` - Project name
- `--output <directory>` - Output directory

**Examples:**
```bash
# Create console application
crab new console MyApp

# Create library project
crab new project MyLib

# Create in specific directory
crab new console MyApp --output ./src
```

### help

Show help information.

**Syntax:**
```bash
crab help [command]
```

**Examples:**
```bash
# General help
crab help

# Command-specific help
crab help compile
crab help build
crab help test
```

## Architecture Support

### Supported Architectures

| Architecture | Code | Formats |
|--------------|------|---------|
| x86-64 | `x86_64` | pe, elf, bin |
| x86-32 | `x86_32` | pe, elf, bin |
| x86-16 | `x86_16` | bin |
| ARM64 | `arm64` | pe, elf, bin |
| ARM32 | `arm32` | pe, elf, bin |
| WebAssembly | `wasm` | wasm, wat |

### Output Formats

| Format | Extension | Description |
|--------|-----------|-------------|
| wasm | `.wasm` | WebAssembly binary |
| wat | `.wat` | WebAssembly text |
| pe | `.exe` | Windows PE executable |
| elf | `.bin` | ELF binary (Linux) |
| bin | `.bin` | Raw binary |
| native | `.bin` | Platform-specific binary |

## Configuration Files

### Project File (.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>MyApp</RootNamespace>
  </PropertyGroup>
</Project>
```

### Solution File (.sln)

```
Microsoft Visual Studio Solution File, Format Version 12.00
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "MyProject", "MyProject.csproj", "{GUID}"
EndProject
```

### Solution File (.slnx)

```xml
<?xml version="1.0" encoding="utf-8"?>
<Solution Version="1.0">
  <Properties>
    <Name>MySolution</Name>
  </Properties>
  <Project Path="MyProject.csproj" Name="MyProject" Type="C#" Id="{GUID}" />
</Solution>
```

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `CRAB_HOME` | CRAB installation directory | - |
| `CRAB_CONFIG` | Configuration file path | - |
| `CRAB_CACHE` | Compilation cache directory | `~/.crab/cache` |
| `CRAB_DEBUG` | Enable debug mode | `false` |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Compilation error |
| 2 | Syntax error |
| 3 | Type error |
| 4 | Memory safety violation |
| 5 | Build error |
| 6 | Runtime error |
| 255 | Internal error |

## Common Workflows

### Develop and Test

```bash
# Create project
crab new console MyApp
cd MyApp

# Edit Program.cs

# Build
crab build

# Run
crab run bin/output.wasm

# Test
crab test
```

### Compile for Multiple Platforms

```bash
# x86-64 Windows
crab compile --arch x86_64 --format pe --output app.exe program.cs

# x86-64 Linux
crab compile --arch x86_64 --format elf --output app program.cs

# ARM64
crab compile --arch arm64 --format bin --output app.bin program.cs

# WebAssembly
crab compile --format wasm --output app.wasm program.cs
```

### Optimize for Production

```bash
crab build --configuration Release
crab compile --optimize --no-debug --output optimized.wasm program.cs
```

## Troubleshooting

### Build Fails

```bash
# Verbose build
crab build --verbosity detailed

# Clean and rebuild
crab build --clean
```

### Memory Safety Errors

```bash
# Show CTGC statistics
crab compile --ctgc-stats program.cs

# Show manual verification details
crab compile --manual-stats program.cs

# Debug mode
crab compile --debug program.cs
```

### Test Failures

```bash
# Verbose test output
crab test --verbose

# Keep test projects for inspection
crab test --keep

# Debug specific test
crab test --name MyTest --debug
```

## See Also

- [Getting Started](Getting-Started.md) - Quick start guide
- [Building Projects](Building-Projects.md) - Project system details
- [Compiler Architecture](../Architecture/Compiler-Architecture.md) - Compiler internals
- [Memory Management](Memory-Management.md) - Memory safety guide
