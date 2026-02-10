# CRAB CLI Reference

Complete command-line interface reference for the CRAB compiler.

## Synopsis

```bash
crab [command] [options] [files...]
```

## Global Options

Options that apply to all commands:

### `--help, -h`
Display help information.

```bash
crab --help
crab compile --help
```

### `--version, -v`
Display CRAB version and build information.

```bash
crab --version
# Output: CRAB Compiler v1.0.0
```

### `--verbose`
Enable verbose output for debugging.

```bash
crab --verbose compile program.cs
```

### `--quiet, -q`
Suppress non-error output.

```bash
crab --quiet compile program.cs
```

## Commands

### `compile`

Compile C# source files to WebAssembly.

**Syntax:**
```bash
crab compile [options] <files...>
```

**Options:**

#### `-o, --output <file>`
Specify output file name.

```bash
crab compile program.cs -o myapp.wasm
```

#### `-m, --memory-model <model>`
Choose memory model: `automatic` (default) or `manual`.

```bash
crab compile program.cs -m automatic
```

#### `-O, --optimize <level>`
Set optimization level: `0`, `1`, `2`, `3`.

```bash
crab compile program.cs -O 3
```

#### `--emit-ir`
Emit intermediate representation to file.

```bash
crab compile program.cs --emit-ir -o program.ir
```

#### `--emit-text`
Emit WASM text format (.wat) instead of binary.

```bash
crab compile program.cs --emit-text -o program.wat
```

#### `--no-verify`
Skip verification passes (not recommended).

```bash
crab compile program.cs --no-verify
```

#### `--target <target>`
Specify WASM target (default: mvp).

```bash
crab compile program.cs --target mvp
```

**Examples:**

```bash
# Basic compilation
crab compile hello.cs

# With optimization
crab compile hello.cs -O 3 -o hello.wasm

# Multiple files
crab compile file1.cs file2.cs file3.cs -o app.wasm

# Emit IR for debugging
crab compile program.cs --emit-ir --emit-text
```

### `check`

Check source code for errors without compiling.

**Syntax:**
```bash
crab check [options] <files...>
```

**Options:**

#### `--warnings-as-errors`
Treat warnings as errors.

```bash
crab check program.cs --warnings-as-errors
```

**Examples:**

```bash
# Check syntax and types
crab check program.cs

# Check multiple files
crab check src/*.cs
```

### `analyze`

Perform memory safety analysis and report.

**Syntax:**
```bash
crab analyze [options] <files...>
```

**Options:**

#### `--report <format>`
Output format: `text`, `json`, `html`.

```bash
crab analyze program.cs --report html -o report.html
```

#### `--show-graphs`
Include ownership/lifetime graphs in report.

```bash
crab analyze program.cs --show-graphs
```

**Examples:**

```bash
# Analyze memory safety
crab analyze program.cs

# Generate HTML report
crab analyze program.cs --report html --show-graphs
```

### `new`

Create a new CRAB project from template.

**Syntax:**
```bash
crab new <template> <name>
```

**Templates:**
- `console` - Console application
- `library` - Class library
- `empty` - Empty project

**Examples:**

```bash
# Create console app
crab new console MyApp

# Create library
crab new library MyLib
```

### `run`

Compile and run program (requires WASM runtime).

**Syntax:**
```bash
crab run [options] <file>
```

**Options:**

#### `--runtime <runtime>`
WASM runtime to use: `wasmtime`, `node`, `browser`.

```bash
crab run program.cs --runtime wasmtime
```

**Examples:**

```bash
# Compile and run
crab run hello.cs

# Run with specific runtime
crab run hello.cs --runtime node
```

### `test`

Run test suite (requires test framework).

**Syntax:**
```bash
crab test [options] [test-name-pattern]
```

**Options:**

#### `--filter <pattern>`
Run tests matching pattern.

```bash
crab test --filter "TestName*"
```

#### `--parallel`
Run tests in parallel.

```bash
crab test --parallel
```

**Examples:**

```bash
# Run all tests
crab test

# Run specific tests
crab test --filter "Memory*"
```

### `clean`

Remove build artifacts.

**Syntax:**
```bash
crab clean [options]
```

**Examples:**

```bash
# Clean output directory
crab clean
```

### `info`

Display compiler and system information.

**Syntax:**
```bash
crab info [options]
```

**Options:**

#### `--diagnostics`
Include diagnostic information.

```bash
crab info --diagnostics
```

**Examples:**

```bash
# Show system info
crab info

# Show diagnostics
crab info --diagnostics
```

## Configuration File

CRAB can be configured via `crab.config.json`:

```json
{
  "compilation": {
    "targetWasm": "MVP",
    "memoryModel": "automatic",
    "optimization": "O2",
    "emitIR": false,
    "emitText": false
  },
  "diagnostics": {
    "warningsAsErrors": false,
    "verbose": false,
    "suppressWarnings": []
  },
  "output": {
    "directory": "./bin",
    "preserveIR": false,
    "preserveText": false
  },
  "runtime": {
    "defaultRuntime": "wasmtime",
    "runtimePath": null
  }
}
```

## Environment Variables

### `CRAB_HOME`
CRAB installation directory.

```bash
export CRAB_HOME=/usr/local/crab
```

### `CRAB_CONFIG`
Path to configuration file.

```bash
export CRAB_CONFIG=/path/to/crab.config.json
```

### `CRAB_MEMORY_MODEL`
Default memory model (`automatic` or `manual`).

```bash
export CRAB_MEMORY_MODEL=automatic
```

### `CRAB_OPTIMIZATION`
Default optimization level (`O0`, `O1`, `O2`, `O3`).

```bash
export CRAB_OPTIMIZATION=O2
```

### `CRAB_RUNTIME`
Default WASM runtime.

```bash
export CRAB_RUNTIME=wasmtime
```

## Exit Codes

- `0` - Success
- `1` - Compilation error
- `2` - Invalid arguments
- `3` - File not found
- `4` - Verification failed
- `5` - Runtime error

## Examples

### Complete Workflow

```bash
# Create new project
crab new console MyApp
cd MyApp

# Check for errors
crab check Program.cs

# Analyze memory safety
crab analyze Program.cs --report html

# Compile with optimization
crab compile Program.cs -O 3 -o MyApp.wasm

# Run the program
crab run MyApp.wasm --runtime wasmtime
```

### Advanced Compilation

```bash
# Compile with all options
crab compile \
  --memory-model automatic \
  --optimize 3 \
  --emit-ir \
  --emit-text \
  --output app.wasm \
  --verbose \
  src/**/*.cs
```

### CI/CD Integration

```bash
#!/bin/bash
# Build script for CI

set -e

# Check code
crab check src/*.cs --warnings-as-errors

# Run tests
crab test --parallel

# Build release
crab compile src/*.cs -O 3 -o release/app.wasm

# Analyze
crab analyze src/*.cs --report json -o analysis.json
```

## See Also

- [Getting Started](GettingStarted.md) - Introduction to CRAB
- [Language Support](LanguageSupport.md) - Supported C# features
- [Memory Models](MemoryModels.md) - Understanding CTGC and Manual
- [Troubleshooting](Troubleshooting.md) - Common issues and solutions
