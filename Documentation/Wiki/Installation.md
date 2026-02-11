# CRAB Installation Guide

## Overview

This guide walks you through installing and setting up the CRAB compiler on your system.

## Prerequisites

Before installing CRAB, ensure you have the following:

### Required
- **.NET 10 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Git** for cloning the repository ([Download](https://git-scm.com/))
- **Minimum 4GB RAM** (8GB recommended for large projects)
- **500MB free disk space**

### Optional
- **WebAssembly runtime** (e.g., Wasmtime, Node.js with WASM support) for testing
- **Visual Studio Code** with C# extension for development
- **WASM tools** for inspecting generated binaries

## Installation Methods

### Method 1: Build from Source (Recommended)

#### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/CRAB.git
cd CRAB
```

#### 2. Build the Compiler

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build --configuration Release

# Verify installation
dotnet run -- --version
```

#### 3. Add to PATH (Optional)

To use CRAB from anywhere:

**Linux/macOS:**
```bash
# Add to ~/.bashrc or ~/.zshrc
export PATH="$PATH:/path/to/CRAB/bin/Release/net10.0"

# Reload shell configuration
source ~/.bashrc  # or source ~/.zshrc
```

**Windows:**
```powershell
# Add to PATH environment variable
$env:PATH += ";C:\path\to\CRAB\bin\Release\net10.0"

# Make permanent via System Properties > Environment Variables
```

### Method 2: NuGet Package (Future)

```bash
# When available
dotnet tool install --global CRAB.Compiler
```

## Verifying Installation

After installation, verify CRAB is working:

```bash
# Check version
dotnet run -- --version

# Expected output:
# CRAB Compiler v1.0.0
# Target: WebAssembly MVP
# Memory Models: Automatic (CTGC) + Manual
```

## Quick Start

### 1. Create Your First Program

Create a file named `hello.cs`:

```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello from CRAB!");
    }
}
```

### 2. Compile to WebAssembly

```bash
# From CRAB directory
dotnet run -- compile hello.cs

# Or if added to PATH
crab compile hello.cs
```

### 3. Run the Generated WASM

```bash
# Using Wasmtime
wasmtime hello.wasm

# Using Node.js
node run-wasm.js hello.wasm
```

## Configuration

### Compiler Configuration File

Create `crab.config.json` in your project directory:

```json
{
  "compilation": {
    "targetWasm": "MVP",
    "memoryModel": "automatic",
    "optimization": "O2"
  },
  "diagnostics": {
    "warningsAsErrors": false,
    "verbose": false
  },
  "output": {
    "directory": "./bin",
    "emitText": false,
    "optimize": "release"
  }
}
```

### Environment Variables

CRAB respects these environment variables:

- `CRAB_HOME`: Installation directory
- `CRAB_CONFIG`: Path to configuration file
- `CRAB_MEMORY_MODEL`: Default memory model (automatic/manual)
- `CRAB_OPTIMIZATION`: Default optimization level (O0/O1/O2/O3)

## IDE Integration

### Visual Studio Code

1. Install the C# extension
2. Create `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "CRAB: Compile",
      "type": "shell",
      "command": "dotnet",
      "args": ["run", "--", "compile", "${file}"],
      "group": {
        "kind": "build",
        "isDefault": true
      }
    }
  ]
}
```

### Visual Studio

1. Add CRAB as an external tool:
   - Tools > External Tools > Add
   - Title: "CRAB Compile"
   - Command: `dotnet`
   - Arguments: `run -- compile $(ItemPath)`

## Updating CRAB

### From Source

```bash
cd CRAB
git pull origin main
dotnet build --configuration Release
```

### From NuGet (Future)

```bash
dotnet tool update --global CRAB.Compiler
```

## Troubleshooting

### "dotnet: command not found"

- Ensure .NET 10 SDK is installed
- Verify PATH includes .NET installation directory
- Restart terminal after installation

### Build Errors

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### Permission Denied (Linux/macOS)

```bash
# Make executable
chmod +x /path/to/CRAB/bin/Release/net10.0/CRAB
```

### WASM Runtime Not Found

Install a WASM runtime:

**Wasmtime:**
```bash
curl https://wasmtime.dev/install.sh -sSf | bash
```

**Node.js:**
```bash
# Install Node.js (includes WASM support)
# See: https://nodejs.org/
```

## Uninstallation

### From Source

```bash
# Remove CRAB directory
rm -rf /path/to/CRAB

# Remove from PATH (edit shell configuration file)
```

### From NuGet (Future)

```bash
dotnet tool uninstall --global CRAB.Compiler
```

## Next Steps

- [Getting Started Guide](GettingStarted.md) - Learn CRAB basics
- [CLI Reference](CLIReference.md) - Command-line options
- [Language Support](LanguageSupport.md) - Supported C# features
- [Examples and Tutorials](ExamplesAndTutorials.md) - Hands-on examples

## Getting Help

- **Issues**: [GitHub Issues](https://github.com/yourusername/CRAB/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/CRAB/discussions)
- **Documentation**: [Full Documentation](../README.md)
- **FAQ**: [Frequently Asked Questions](FAQ.md)

## Platform-Specific Notes

### Windows

- Use PowerShell or Command Prompt
- Path separators use backslash (`\`)
- May require running as Administrator for PATH changes

### Linux

- All major distributions supported
- Requires glibc 2.17+ or musl
- ARM64 supported

### macOS

- macOS 10.15 (Catalina) or later
- Both Intel and Apple Silicon supported
- May require approving in System Preferences > Security

## License

CRAB is released under the MIT License. See [LICENSE.md](../../LICENSE.md) for details.
