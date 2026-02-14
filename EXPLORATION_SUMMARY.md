# CRAB Repository Exploration Summary

**Date**: 2024  
**Purpose**: Understanding CRAB compiler architecture before implementing enhancements

---

## Quick Reference

### Key Discovery: `--save` Flag is Missing
The `test` command has a `--keep` flag that preserves the test project directory, but **lacks a `--save` flag** to preserve compiled WAT and binary outputs.

### Repository Statistics
- **Language**: 100% C# (as per CRAB spec requirements)
- **Total CLI Commands**: 8 (new, compile, build, run, test, test-suite, help, registry)
- **Supported Architectures**: 5 (x86_64, x86_32, x86_16, ARM64, ARM32)
- **Supported Containers**: 2 (native, PE)
- **Test Combinations**: 9 (comprehensive mode)

---

## Critical Files Identified

### 1. Test Command Infrastructure
- **`/CLI/Commands/Test.cs`** (326 lines)
  - Current flags: `--name`, `--keep`, `--verbose`, `--quick`, `--arch`, `--format`
  - Missing: `--save` flag
  - Two modes: quick (single arch) and comprehensive (9 combinations)

### 2. C# → WAT Mapping
- **`/Compiler/Core/TokenSet.cs`** (335 lines) - Complete C# 13 tokenization
- **`/Compiler/Core/RuleSet.cs`** (48.9 KB) - Complete C# 13 grammar
- **`/Compiler/Core/MapSet.cs`** (31.9 KB) - AST → WAT template-based generation

### 3. WAT → Assembly (BADGER)
- **`/Dependencies/BADGER/Program.cs`** - Main BADGER compiler
- **`/Dependencies/BADGER/Architectures/*.cs`** - 5 architecture backends
- **`/Dependencies/BADGER/Containers/*.cs`** - 2 container formats

---

## Architecture Overview

```
C# Source
    ↓
TokenSet (Tokens.cs) → Lexical Analysis
    ↓
RuleSet (Rules.cs) → Syntax Analysis → AST
    ↓
Models (Automatic.cs, Manual.cs, Optimization.cs) → Semantic Analysis
    ↓
MapSet (MapSet.cs) → Code Generation → WAT
    ↓
BADGER (Program.cs) → Assembly Generation → Binary
    ↓
Container (Native.cs, PE.cs) → Final Output
```

---

## Current Test Command Behavior

### Quick Mode (`--quick`)
```bash
crab test --quick --arch x86_64 --format native
```
1. Generate test project (TestProject/)
2. Build project (C# → WAT)
3. Compile to single architecture (WAT → x86_64 native)
4. Run test
5. Cleanup (delete everything unless --keep)

### Comprehensive Mode (default)
```bash
crab test --verbose
```
1. Generate test project (TestProject/)
2. Build project (C# → WAT)
3. Compile to ALL 9 combinations:
   - x86_64 × (native, PE)
   - x86_32 × (native, PE)
   - x86_16 × (native)
   - ARM64 × (native, PE)
   - ARM32 × (native, PE)
4. Run tests (detect platform, execute on compatible arch)
5. Cleanup (delete everything unless --keep)

### Current Flags
| Flag | Effect |
|------|--------|
| `--keep` | Keeps TestProject/ directory (source code) |
| `--verbose` | Shows detailed progress |
| `--quick` | Single architecture test |
| `--arch` | Specify architecture (with --quick) |
| `--format` | Specify format (with --quick) |
| `--name` | Test project name |

### Missing: `--save` Flag
**Should preserve**:
- output.wasm (WAT file)
- All compiled binaries (*.bin files)
- Optionally: test results summary

---

## Supported Architectures & Containers

### Architecture Support
| Architecture | File | Description |
|--------------|------|-------------|
| x86_64 | Architectures/x86_64.cs | 64-bit x86 (AMD64, Intel 64) |
| x86_32 | Architectures/x86_32.cs | 32-bit x86 (IA-32) |
| x86_16 | Architectures/x86_16.cs | 16-bit x86 (real mode) |
| ARM64 | Architectures/ARM64.cs | 64-bit ARM (AArch64) |
| ARM32 | Architectures/ARM32.cs | 32-bit ARM |

### Container Support
| Format | File | Description |
|--------|------|-------------|
| native | Containers/Native.cs | Raw binary (no headers) |
| PE | Containers/PE.cs | Windows PE/COFF format |

### Test Matrix
```
         native    PE
x86_64     ✅      ✅
x86_32     ✅      ✅
x86_16     ✅      ❌  (skipped - incompatible)
ARM64      ✅      ✅
ARM32      ✅      ✅
```

---

## Memory Safety Models

### Automatic Model (`/Compiler/Models/Automatic.cs`)
- **CTGC** (Compile-Time Garbage Collection)
- Lifetime inference
- Region analysis
- Automatic deallocation insertion

### Manual Model (`/Compiler/Models/Manual.cs`)
- Abstract interpretation of `manual{}` blocks
- Symbolic execution
- Ownership graph analysis
- Safety verification

### Optimization Model (`/Compiler/Models/Optimization.cs`)
- Dead code elimination
- Constant folding
- Common subexpression elimination (CSE)
- Inlining, loop optimizations, tail call optimization
- All preserve CTGC semantics and memory safety

---

## Next Steps for `--save` Implementation

### Implementation Plan
1. **Add flag to Test.cs**
   ```csharp
   SupportedFlags["save"] = "Save compiled WAT and binary outputs.";
   ```

2. **Parse flag in Execute() method**
   ```csharp
   bool saveOutputs = flags.ContainsKey("save");
   ```

3. **Modify cleanup logic**
   - If `--save`: Copy outputs before cleanup
   - If `--keep`: Preserve test project (existing)
   - Otherwise: Delete everything (existing)

4. **Decide what to save**:
   - output.wasm (WAT file)
   - All *.bin files (9 in comprehensive mode, 1 in quick mode)
   - test_results.txt (summary report)

5. **Decide where to save**:
   - Option A: Current directory with timestamp
   - Option B: `./test-outputs/` directory
   - Option C: User-specified via `--save <path>`

### Example Usage (after implementation)
```bash
# Save outputs to default location
crab test --save

# Save outputs and keep project
crab test --save --keep

# Save with custom name
crab test --name MyTest --save

# Quick test with save
crab test --quick --arch arm64 --save
```

---

## Documentation References

The following markdown files were created during exploration:
1. **REPOSITORY_OVERVIEW.md** - Detailed structural analysis
2. **ARCHITECTURE_VISUAL.md** - Visual diagrams and flowcharts
3. **EXPLORATION_SUMMARY.md** - This file (executive summary)

All documentation preserves CRAB's architecture requirements:
- C#-only implementation ✅
- CDTk framework integration ✅
- BADGER backend for native compilation ✅
- CTGC automatic memory management ✅
- Manual memory verification ✅

---

## Conclusion

The CRAB repository has:
- ✅ Well-organized structure
- ✅ Complete C# 13 support
- ✅ Comprehensive test infrastructure
- ✅ 5 architecture backends via BADGER
- ✅ Memory safety guarantees via CTGC
- ❌ **Missing**: `--save` flag for test outputs

**Ready to proceed with implementation.**

