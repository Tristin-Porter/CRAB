# CRAB Quick Start Guide

## 📚 Documentation Index

1. **EXPLORATION_SUMMARY.md** - Start here! Executive summary and key findings
2. **REPOSITORY_OVERVIEW.md** - Detailed structural analysis (15 KB)
3. **ARCHITECTURE_VISUAL.md** - Visual diagrams and flowcharts (24 KB)
4. **QUICK_START.md** - This file (quick reference)

---

## 🎯 Key Finding: Missing `--save` Flag

The `test` command needs a `--save` flag to preserve compiled outputs.

**Current behavior**:
- `--keep` preserves source code directory
- Compiled WAT and binaries are **always deleted**

**Needed behavior**:
- `--save` should preserve WAT and binary outputs
- Can be combined with `--keep`

---

## 📁 Critical Files Map

```
Test Infrastructure:
  CLI/Commands/Test.cs ................... ⭐ Test command (326 lines)
  
C# → WAT Compilation:
  Compiler/Core/TokenSet.cs .............. ⭐ Lexer (335 lines)
  Compiler/Core/RuleSet.cs ............... ⭐ Parser (48.9 KB)
  Compiler/Core/MapSet.cs ................ ⭐ Code gen (31.9 KB)
  
WAT → Assembly (BADGER):
  Dependencies/BADGER/Program.cs ......... ⭐ BADGER compiler
  Dependencies/BADGER/Architectures/ ..... 5 arch backends
  Dependencies/BADGER/Containers/ ........ 2 container formats
  
Memory Models:
  Compiler/Models/Automatic.cs ........... CTGC implementation
  Compiler/Models/Manual.cs .............. Manual memory verification
  Compiler/Models/Optimization.cs ........ Safe optimizations
```

---

## 🔧 Test Command Reference

### Current Flags
```bash
crab test [options]

--name <name>      Test project name (default: TestProject)
--keep             Keep test project directory
--verbose          Detailed output
--quick            Single architecture test (fast)
--arch <arch>      Architecture: x86_64|x86_32|x86_16|arm64|arm32
--format <fmt>     Format: native|pe

⚠️  MISSING: --save flag
```

### Test Modes

**Comprehensive Mode** (default):
```bash
crab test
```
- Tests 9 architecture/format combinations
- ~5-10 seconds per combination
- Provides complete validation

**Quick Mode**:
```bash
crab test --quick --arch x86_64 --format native
```
- Tests single combination
- Fast validation (~1 second)
- Good for rapid iteration

---

## 🏗️ Architecture Support

### Backends (5 total)
- **x86_64** - 64-bit Intel/AMD (most common)
- **x86_32** - 32-bit Intel/AMD
- **x86_16** - 16-bit x86 (real mode)
- **ARM64** - 64-bit ARM (Apple Silicon, servers)
- **ARM32** - 32-bit ARM (embedded, mobile)

### Containers (2 total)
- **native** - Raw binary (no headers)
- **PE** - Windows executable format

### Test Matrix (9 combinations)
```
         native    PE
x86_64     ✅      ✅
x86_32     ✅      ✅
x86_16     ✅      ❌  (incompatible)
ARM64      ✅      ✅
ARM32      ✅      ✅
```

---

## 🔬 Compiler Pipeline

```
C# Source
  ↓
TokenSet → Lexical analysis
  ↓
RuleSet → Syntax analysis → AST
  ↓
Models → Semantic analysis (CTGC, manual verification, optimization)
  ↓
MapSet → Code generation → WAT
  ↓
BADGER → Assembly generation → Binary
  ↓
Container → Final output (native or PE)
```

---

## 💾 Memory Safety

CRAB provides **100% memory safety** through:

1. **CTGC** (Compile-Time Garbage Collection)
   - Automatic lifetime inference
   - No runtime overhead
   - Guaranteed deallocation

2. **Manual verification** for `manual{}` blocks
   - Abstract interpretation
   - Ownership tracking
   - Compile-time verification

3. **Safe optimizations**
   - Preserve memory safety
   - DCE, CSE, inlining, tail calls
   - No unsafe transformations

---

## 🚀 Common Commands

```bash
# Create new console project
crab new console MyProject

# Compile C# to WAT
crab compile Program.cs --output output.wasm

# Compile C# to native binary
crab compile Program.cs --to-asm --arch x86_64 --output program.bin

# Build project
crab build --project MyProject

# Run WAT file
crab run output.wasm

# Test (comprehensive)
crab test --verbose

# Test (quick)
crab test --quick --arch arm64
```

---

## 📊 Implementation Checklist for `--save`

- [ ] Add `SupportedFlags["save"]` to Test.cs
- [ ] Parse `--save` flag in Execute() method
- [ ] Decide save location (current dir, ./test-outputs/, or custom)
- [ ] Implement save logic:
  - [ ] Save output.wasm (WAT file)
  - [ ] Save compiled binaries (*.bin)
  - [ ] Optionally save test results summary
- [ ] Modify cleanup logic to preserve saved files
- [ ] Test with `--save` alone
- [ ] Test with `--save --keep` combination
- [ ] Test in quick mode
- [ ] Test in comprehensive mode
- [ ] Update documentation (README, help text)

---

## 🎓 Learning Resources

**Understanding the codebase**:
1. Start with `Program.cs` - entry point
2. Read `CLI/Commands/Test.cs` - test infrastructure
3. Examine `Compiler/Core/MapSet.cs` - code generation
4. Study `Dependencies/BADGER/Program.cs` - backend

**Key concepts**:
- **CDTk**: Compiler Development ToolKit (framework)
- **CTGC**: Compile-Time Garbage Collection
- **WAT**: WebAssembly Text format
- **BADGER**: Better Assembler for Dependable Generation of Efficient Results

---

## ⚡ Pro Tips

1. **Use `--verbose`** for debugging
2. **Use `--quick`** for fast iteration
3. **Architecture detection** is automatic (see Test.cs:289-302)
4. **All files are C#** (no other languages allowed per CRAB spec)
5. **Template-based code gen** in MapSet.cs uses `{placeholder}` syntax
6. **BADGER** handles 5 architectures × 2 formats = 10 combinations

---

**Ready to implement the `--save` flag!** 🎉

See EXPLORATION_SUMMARY.md for detailed implementation plan.
