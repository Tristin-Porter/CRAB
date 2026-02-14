# CRAB Repository Structure Overview

## Executive Summary

This document provides a comprehensive overview of the CRAB compiler repository structure, focusing on:
1. Test command infrastructure and flag implementation
2. C# → WAT mapping implementation  
3. Supported architectures and containers
4. Location of relevant source files

---

## 1. Test Command Infrastructure

### Location
- **Primary File**: `/CLI/Commands/Test.cs` (326 lines)
- **Supporting Files**: 
  - `/CLI/Commands/TestSuite.cs` (101 lines)
  - `/Testing/` directory (various test files)

### Current Flags
The `Test` command currently supports these flags:

| Flag | Description | Default |
|------|-------------|---------|
| `--name` | Name of the test project | `TestProject` |
| `--keep` | Keep the generated test project after execution | `false` (deletes after run) |
| `--verbose` | Enable verbose output | `false` |
| `--quick` | Run quick test (single architecture only) | `false` |
| `--arch` | Single architecture to test (use with --quick) | `x86_64` |
| `--format` | Output format (use with --quick) | `native` |

### Test Command Workflow
1. **Generate test project**: Creates a new console project with given name
2. **Build project**: Compiles C# → WAT using CRAB compiler
3. **Run tests**:
   - **Quick mode**: Single arch/format combination
   - **Comprehensive mode**: All 9 arch/format combinations (5 architectures × 2 formats, minus x86_16+PE)
4. **Cleanup**: Deletes test project unless `--keep` flag is used

### **MISSING: `--save` Flag**
Currently **NOT IMPLEMENTED**. The test command only has `--keep` which preserves the generated test project directory, but does **not** save the compiled WAT/assembly outputs.

---

## 2. C# → WAT Mapping Implementation

### Architecture Overview

The CRAB compiler uses **CDTk** (Compiler Development ToolKit) framework with these components:

```
┌─────────────────────────────────────────────────────────┐
│                     CDTk Pipeline                       │
│  Tokens → Syntax → Structure → Semantics → Emission    │
└─────────────────────────────────────────────────────────┘
         ↓           ↓            ↓            ↓
    TokenSet    RuleSet      MapSet       Models
```

### Key Files

#### 1. **Tokenization** (`/Compiler/Core/TokenSet.cs`)
- **335 lines** of complete C# 13 token definitions
- Includes:
  - All keywords (reserved + contextual)
  - String/numeric literals (including C# 11 raw strings, UTF-8 strings)
  - Operators (including C# 11 unsigned right shift `>>>`)
  - Preprocessor directives
  - CRAB-specific: `manual` keyword for manual memory blocks

#### 2. **Grammar Rules** (`/Compiler/Core/RuleSet.cs`)
- **48.9 KB** - Complete C# 13 grammar
- Defines AST structure for:
  - Compilation units, namespaces, types
  - Class/struct/interface/enum/delegate/record declarations
  - All member types (methods, fields, properties, events, indexers, operators)
  - All statements (if, while, for, foreach, switch, try-catch, using, etc.)
  - All expressions (operators, literals, calls, lambdas, LINQ, etc.)
  - C# 9-13 features (records, file-scoped namespaces, required members, etc.)

#### 3. **WAT Generation** (`/Compiler/Core/MapSet.cs`)
- **31.9 KB** - C# AST → WebAssembly Text mapping
- **Class**: `WASM : MapSet`
- **Location**: Lines 16+

**Key Features**:
- Each public `Map` field corresponds to an AST node from `RuleSet`
- Template-based code generation with placeholders like `{name}`, `{body}`, `{members}`
- Integrates with semantic analysis models:
  - `AutomaticModel` (CTGC automatic memory management)
  - `ManualModel` (manual{} block verification)
  - `OptimizationModel` (safe code optimizations)

**Example Mappings** (from MapSet.cs):
```csharp
// Module structure
public Map CompilationUnit = @"(module
  ;; Imports
  (import ""env"" ""memory"" (memory 1))
  
  ;; Generated members
{members}
)";

// Method declaration with CTGC annotations
public Map MethodDeclaration = @"(func ${name}
  (param {parameters})
  (result {returnType})
  ;; Method body with CTGC-inserted deallocations
{body}
)";

// Class mapped to WASM struct
public Map ClassDeclaration = @";; class {name}
(type ${name} (struct
{body}
))";
```

### Semantic Analysis Models

Located in `/Compiler/Models/`:

1. **Automatic.cs** - CTGC (Compile-Time Garbage Collection)
   - Lifetime inference
   - Region analysis  
   - Memory safety verification
   - Automatic deallocation insertion

2. **Manual.cs** - Manual memory verification
   - Abstract interpretation of manual{} blocks
   - Symbolic execution
   - Ownership graph analysis

3. **Optimization.cs** - Safe optimizations
   - Dead code elimination
   - Constant folding
   - Common subexpression elimination (CSE)
   - Inlining, loop optimizations, tail call optimization

---

## 3. Supported Architectures and Containers

### BADGER Backend
**BADGER** (Better Assembler for Dependable Generation of Efficient Results) is the WAT → native assembly compiler.

**Location**: `/Dependencies/BADGER/`

### Supported Architectures

| Architecture | File | Status | Description |
|--------------|------|--------|-------------|
| **x86_64** | `/Dependencies/BADGER/Architectures/x86_64.cs` | ✅ Implemented | 64-bit x86 (AMD64, Intel 64) |
| **x86_32** | `/Dependencies/BADGER/Architectures/x86_32.cs` | ✅ Implemented | 32-bit x86 (IA-32) |
| **x86_16** | `/Dependencies/BADGER/Architectures/x86_16.cs` | ✅ Implemented | 16-bit x86 (real mode) |
| **ARM64** | `/Dependencies/BADGER/Architectures/ARM64.cs` | ✅ Implemented | 64-bit ARM (AArch64) |
| **ARM32** | `/Dependencies/BADGER/Architectures/ARM32.cs` | ✅ Implemented | 32-bit ARM |

### Supported Container Formats

| Format | File | Status | Description |
|--------|------|--------|-------------|
| **native** | `/Dependencies/BADGER/Containers/Native.cs` | ✅ Implemented | Raw binary (no container) |
| **PE** | `/Dependencies/BADGER/Containers/PE.cs` | ✅ Implemented | Windows PE/COFF format |

**Note**: x86_16 + PE combination is skipped in comprehensive tests (line 181-182 of Test.cs)

### WAT → Assembly Pipeline

**Entry Point**: `/Dependencies/BADGER/Program.cs`
- **Class**: `BadgerCompiler`
- **Main Method**: `Compile(string watInput, string architecture, string format)`

**Pipeline**:
```
WAT Text Input
     ↓
CDTk Tokenization (WATTokens)
     ↓
CDTk Parsing (WATRules)
     ↓
Template Expansion (TemplateExpander)
     ↓
Architecture-Specific MapSet (e.g., WATToX86_64MapSet)
     ↓
Assembly Text Generation
     ↓
Architecture Assembler (e.g., x86_64.Assembler)
     ↓
Machine Code
     ↓
Container Emitter (Native or PE)
     ↓
Binary Output
```

### Key BADGER Components

1. **WATTokens** (lines 10-246): Complete WAT token set
   - All WASM MVP instructions (numeric, control, memory, variable)
   - Value types (i32, i64, f32, f64)
   - Module structure elements

2. **WATRules** (lines 248-346): Complete WAT grammar
   - Module, functions, imports, exports
   - Memory, tables, globals, data segments
   - Control flow, instructions

3. **TemplateExpander** (lines 348-423): Template system
   - Architecture-specific code generation
   - Stack simulation helpers (push, pop, pop2)
   - Context-aware expansion

4. **Architecture MapSets** (e.g., `WATToX86_64MapSet`):
   - Complete WAT → Assembly lowering
   - Stack simulation (registers + memory spilling)
   - Control flow (labels, blocks, loops)
   - Calling conventions
   - Full instruction selection

---

## 4. CLI Command Structure

### Command Registry
**Location**: `/CLI/Commands/Registry.cs` (248 lines)

All commands registered in `Program.cs`:
```csharp
var registry = new Registry()
    .Register(new New().AddSub(new Console()).AddSub(new Project()))
    .Register(new Compile())
    .Register(new Build())
    .Register(new Run())
    .Register(new Test())
    .Register(new TestSuite())
    .Register(new Help());
```

### Command Files

| Command | File | Lines | Description |
|---------|------|-------|-------------|
| **compile** | `/CLI/Commands/Compile.cs` | 264 | C# → WAT (or C# → ASM with --to-asm) |
| **build** | `/CLI/Commands/Build.cs` | 150 | Build CRAB project |
| **run** | `/CLI/Commands/Run.cs` | 252 | WAT → Native → Execute |
| **test** | `/CLI/Commands/Test.cs` | 326 | Generate, build, test project |
| **test-suite** | `/CLI/Commands/TestSuite.cs` | 101 | Run test suite |
| **new** | `/CLI/Commands/New.cs` | 198 | Create new project |
| **help** | `/CLI/Commands/Help.cs` | 46 | Display help |

### Common Flags Across Commands

| Flag | Commands | Description |
|------|----------|-------------|
| `--verbose` | All | Enable verbose output |
| `--output` | compile, build | Output path |
| `--to-asm` | compile, build, test | Compile to native assembly (WAT → ASM) |
| `--arch` | compile, build, run, test | Target architecture |
| `--format` | compile, build, run, test | Container format (native, pe) |
| `--input` | compile, run | Input file/directory |
| `--keep` | test | Keep test project |
| `--keep-temp` | run | Keep temporary executable |

---

## 5. Source File Organization

```
/home/runner/work/CRAB/CRAB/
│
├── Program.cs                   # Entry point, REPL
├── CRAB.csproj                  # Project file
├── CRAB.sln                     # Solution file
│
├── CLI/                         # Command-line interface
│   ├── Formatting.cs            # Colored input rendering
│   └── Commands/
│       ├── Registry.cs          # Command registry
│       ├── Compile.cs           # compile command
│       ├── Build.cs             # build command
│       ├── Run.cs               # run command
│       ├── Test.cs              # test command ⭐
│       ├── TestSuite.cs         # test-suite command
│       ├── New.cs               # new command (subcommands: console, project)
│       └── Help.cs              # help command
│
├── Compiler/                    # Core compiler
│   ├── Core/
│   │   ├── TokenSet.cs          # C# 13 tokens ⭐
│   │   ├── RuleSet.cs           # C# 13 grammar ⭐
│   │   └── MapSet.cs            # C# → WAT mapping ⭐
│   └── Models/
│       ├── Automatic.cs         # CTGC automatic memory model
│       ├── Manual.cs            # Manual memory verification
│       └── Optimization.cs      # Safe optimizations
│
├── Dependencies/
│   ├── CDTk/                    # Compiler Development ToolKit
│   │   └── Boilerplate/
│   │       └── CDTk.cs          # CDTk framework
│   └── BADGER/                  # WAT → Assembly compiler ⭐
│       ├── Program.cs           # BADGER entry point, templates
│       ├── Badger.csproj        # BADGER project file
│       ├── Architectures/
│       │   ├── x86_64.cs        # x86-64 backend
│       │   ├── x86_32.cs        # x86-32 backend
│       │   ├── x86_16.cs        # x86-16 backend
│       │   ├── ARM64.cs         # ARM64 backend
│       │   └── ARM32.cs         # ARM32 backend
│       └── Containers/
│           ├── Native.cs        # Raw binary container
│           └── PE.cs            # Windows PE container
│
├── Testing/                     # Test files (excluded from build)
│   ├── IntegrationTests.cs
│   ├── WASMGenerationTests.cs
│   ├── TokenTests.cs
│   ├── ManualMemoryTests.cs
│   ├── ParserTests.cs
│   └── TestRunner.cs
│
└── Documentation/               # Documentation files
```

---

## 6. Key Insights for Implementation

### Test Command `--save` Flag Implementation

**Current Behavior**:
- Test command creates temporary project
- Builds it (generates WAT/assembly)
- Runs tests (9 arch/format combinations in comprehensive mode)
- **Deletes everything** unless `--keep` is specified

**`--keep` vs `--save` distinction**:
- `--keep`: Preserves the **test project directory** (source code)
- `--save` (missing): Should preserve **compiled outputs** (WAT/binary files)

**Suggested Implementation** (in Test.cs):
1. Add `SupportedFlags["save"]` with description
2. Parse flag in `Execute()` method
3. Modify cleanup logic to:
   - If `--save`: Copy compiled outputs (WAT/bins) to current directory or specified location
   - If `--keep`: Preserve test project directory (existing behavior)
   - Otherwise: Delete everything (existing behavior)

### Architecture Detection
The `Test` command includes architecture detection (line 289-302):
```csharp
private string DetectCurrentArchitecture()
{
    var arch = RuntimeInformation.ProcessArchitecture;
    return arch switch
    {
        Architecture.X64 => "x86_64",
        Architecture.X86 => "x86_32",
        Architecture.Arm64 => "arm64",
        Architecture.Arm => "arm32",
        _ => "x86_64" // default fallback
    };
}
```

### Comprehensive Test Matrix
The comprehensive test runs **9 combinations** (lines 169-183):
- x86_64 × (native, PE) = 2
- x86_32 × (native, PE) = 2  
- x86_16 × (native) = 1  *(PE skipped)*
- ARM64 × (native, PE) = 2
- ARM32 × (native, PE) = 2

Each test:
1. Compiles WAT → Architecture assembly using BADGER
2. Generates binary in specified container format
3. Reports pass/fail with detailed error messages

---

## 7. Next Steps

To implement the `--save` flag functionality:

1. **Modify `/CLI/Commands/Test.cs`**:
   - Add flag to `SupportedFlags` dictionary
   - Parse flag in `Execute()` method
   - Implement save logic in `CleanupProject()` or new method
   - Save both WAT and compiled binaries

2. **Consider save location**:
   - Current directory with timestamped names?
   - Specified output directory via flag value?
   - Standard location like `./test-outputs/`?

3. **What to save**:
   - WAT file (intermediate representation)
   - All compiled binaries (9 files in comprehensive mode)
   - Test project source (if `--keep` also specified)
   - Test results summary (pass/fail report)

4. **Update documentation**:
   - README.md
   - QUICK_REFERENCE.md
   - Help text in Test.cs

---

## Summary

The CRAB compiler has a well-structured architecture:
- **Complete C# 13 support** via comprehensive TokenSet and RuleSet
- **Clean separation** between compilation stages (CDTk pipeline)
- **5 target architectures** via BADGER backend
- **2 container formats** (native, PE)
- **Robust testing infrastructure** with comprehensive and quick modes
- **Missing**: `--save` flag to preserve compiled test outputs

All source files are **C#-only** as specified in CRAB architecture requirements.
