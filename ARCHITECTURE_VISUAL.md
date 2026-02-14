# CRAB Compiler Architecture - Visual Reference

## Complete Compilation Pipeline

```
┌────────────────────────────────────────────────────────────────────────┐
│                         CRAB Compilation Pipeline                      │
└────────────────────────────────────────────────────────────────────────┘

                              C# Source Code
                                    │
                                    ▼
          ┌─────────────────────────────────────────────┐
          │         CDTk Compiler Framework             │
          │  (Program.cs creates compiler instance)     │
          └─────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
          ┌──────────────────┐          ┌──────────────────┐
          │   Tokens.cs      │          │   Rules.cs       │
          │  (TokenSet)      │          │   (RuleSet)      │
          │  335 lines       │          │   48.9 KB        │
          └──────────────────┘          └──────────────────┘
          │ All C# 13 tokens │          │ Complete C# 13   │
          │ - Keywords       │          │   grammar        │
          │ - Operators      │          │ - AST rules      │
          │ - Literals       │          │ - All constructs │
          └──────────────────┘          └──────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    ▼
                            Abstract Syntax Tree
                                    │
                    ┌───────────────┼───────────────┐
                    │               │               │
                    ▼               ▼               ▼
          ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
          │ Automatic.cs │  │  Manual.cs   │  │Optimization.cs│
          │   (CTGC)     │  │(Manual Mem)  │  │(Safe Opts)   │
          └──────────────┘  └──────────────┘  └──────────────┘
          │ Lifetime     │  │ Abstract     │  │ DCE, CSE     │
          │ inference    │  │ interp.      │  │ Inlining     │
          │ Deallocation │  │ Ownership    │  │ Tail calls   │
          └──────────────┘  └──────────────┘  └──────────────┘
                    │               │               │
                    └───────────────┴───────────────┘
                                    │
                                    ▼
                          ┌──────────────────┐
                          │   MapSet.cs      │
                          │   (WASM)         │
                          │   31.9 KB        │
                          └──────────────────┘
                          │ C# AST → WAT    │
                          │ Template-based  │
                          │ Code generation │
                          └──────────────────┘
                                    │
                                    ▼
                          WebAssembly Text (WAT)
                                    │
                    ┌───────────────┴───────────────┐
                    │  (if --to-asm flag used)      │
                    ▼                               │
          ┌────────────────────┐                    │
          │  BADGER Backend    │                    │
          │  (WAT → Assembly)  │                    │
          └────────────────────┘                    │
                    │                               │
        ┌───────────┼───────────┐                   │
        │           │           │                   │
        ▼           ▼           ▼                   │
    ┌───────┐  ┌───────┐  ┌───────┐                │
    │x86_64 │  │ ARM64 │  │x86_32 │  etc...        │
    └───────┘  └───────┘  └───────┘                │
        │           │           │                   │
        └───────────┴───────────┘                   │
                    │                               │
        ┌───────────┴───────────┐                   │
        │                       │                   │
        ▼                       ▼                   │
    ┌────────┐            ┌────────┐                │
    │ Native │            │   PE   │                │
    └────────┘            └────────┘                │
        │                       │                   │
        └───────────────────────┘                   │
                    │                               │
                    ▼                               ▼
              Binary Output                   WAT Output
```

## Test Command Flow

```
┌────────────────────────────────────────────────────────────────────────┐
│                           test command                                 │
│                      (Test.cs - 326 lines)                             │
└────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
          ┌──────────────────┐          ┌──────────────────┐
          │   --quick mode   │          │ Comprehensive    │
          │                  │          │    mode          │
          └──────────────────┘          └──────────────────┘
          │ Single arch      │          │ 9 combinations   │
          │ Single format    │          │ All archs+formats│
          │ Fast validation  │          │ Full validation  │
          └──────────────────┘          └──────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    ▼
                    ┌───────────────────────────────┐
                    │  1. Generate Test Project     │
                    │     (new console command)     │
                    └───────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  2. Build Project             │
                    │     (build command)           │
                    │     C# → WAT                  │
                    └───────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  3. Compile to Native         │
                    │     (compile --to-asm)        │
                    │     WAT → ASM via BADGER      │
                    └───────────────────────────────┘
                                    │
                                    ▼
                    ┌───────────────────────────────┐
                    │  4. Report Results            │
                    │     ✅ PASS / ❌ FAIL          │
                    └───────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
          ┌──────────────────┐          ┌──────────────────┐
          │   --keep flag    │          │  No --keep       │
          │                  │          │                  │
          └──────────────────┘          └──────────────────┘
          │ Keep test project│          │ Delete everything│
          │ directory        │          │                  │
          └──────────────────┘          └──────────────────┘
                    │
                    ▼
          ┌──────────────────┐
          │   --save flag    │◄──── MISSING! To be implemented
          │   (NOT YET)      │
          └──────────────────┘
          │ Save compiled    │
          │ WAT/binaries     │
          └──────────────────┘
```

## BADGER Architecture Support Matrix

```
┌────────────────────────────────────────────────────────────────────────┐
│                         BADGER Compiler                                │
│           WAT → Native Assembly & Container Formats                    │
└────────────────────────────────────────────────────────────────────────┘

Architecture Support:
┌───────────┬──────────┬──────────┬─────────────────────────────┐
│ Arch      │  Bits    │  Status  │  File                       │
├───────────┼──────────┼──────────┼─────────────────────────────┤
│ x86_64    │  64-bit  │    ✅    │  Architectures/x86_64.cs    │
│ x86_32    │  32-bit  │    ✅    │  Architectures/x86_32.cs    │
│ x86_16    │  16-bit  │    ✅    │  Architectures/x86_16.cs    │
│ ARM64     │  64-bit  │    ✅    │  Architectures/ARM64.cs     │
│ ARM32     │  32-bit  │    ✅    │  Architectures/ARM32.cs     │
└───────────┴──────────┴──────────┴─────────────────────────────┘

Container Format Support:
┌───────────┬──────────┬──────────┬─────────────────────────────┐
│ Format    │  Type    │  Status  │  File                       │
├───────────┼──────────┼──────────┼─────────────────────────────┤
│ native    │  Raw     │    ✅    │  Containers/Native.cs       │
│ PE        │  Windows │    ✅    │  Containers/PE.cs           │
└───────────┴──────────┴──────────┴─────────────────────────────┘

Test Matrix (Comprehensive Mode):
┌──────────┬─────────┬─────────┐
│   Arch   │ native  │   PE    │
├──────────┼─────────┼─────────┤
│ x86_64   │    ✅   │    ✅   │
│ x86_32   │    ✅   │    ✅   │
│ x86_16   │    ✅   │    ❌   │  ← Skipped (incompatible)
│ ARM64    │    ✅   │    ✅   │
│ ARM32    │    ✅   │    ✅   │
└──────────┴─────────┴─────────┘
Total: 9 combinations tested
```

## File Organization by Purpose

```
CRAB/
│
├── 🎯 ENTRY POINT
│   └── Program.cs ..................... Main entry, REPL, command registry
│
├── 📝 COMMAND LINE INTERFACE
│   └── CLI/Commands/
│       ├── Test.cs ................... ⭐ Test infrastructure (326 lines)
│       ├── Compile.cs ................ C# → WAT compilation
│       ├── Build.cs .................. Project build orchestration
│       ├── Run.cs .................... WAT → Native → Execute
│       ├── New.cs .................... Project scaffolding
│       ├── TestSuite.cs .............. Run test suite
│       ├── Help.cs ................... Help system
│       └── Registry.cs ............... Command dispatcher
│
├── 🔧 COMPILER CORE
│   └── Compiler/
│       ├── Core/
│       │   ├── TokenSet.cs ........... ⭐ C# 13 tokenization (335 lines)
│       │   ├── RuleSet.cs ............ ⭐ C# 13 grammar (48.9 KB)
│       │   └── MapSet.cs ............. ⭐ C# → WAT mapping (31.9 KB)
│       └── Models/
│           ├── Automatic.cs .......... CTGC automatic memory
│           ├── Manual.cs ............. Manual memory verification
│           └── Optimization.cs ....... Safe optimizations
│
└── 🏗️ BACKEND (BADGER)
    └── Dependencies/BADGER/
        ├── Program.cs ................ ⭐ WAT → ASM orchestration
        ├── Architectures/
        │   ├── x86_64.cs ............. x86-64 backend
        │   ├── x86_32.cs ............. x86-32 backend
        │   ├── x86_16.cs ............. x86-16 backend
        │   ├── ARM64.cs .............. ARM64 backend
        │   └── ARM32.cs .............. ARM32 backend
        └── Containers/
            ├── Native.cs ............. Raw binary format
            └── PE.cs ................. Windows PE format

⭐ = Key files for understanding the system
```

## Command Flag Reference

```
┌────────────────────────────────────────────────────────────────────────┐
│                         Common Flags                                   │
└────────────────────────────────────────────────────────────────────────┘

Global Flags (most commands):
  --verbose ..................... Enable detailed output
  
Compilation Flags (compile, build, test):
  --to-asm ...................... Compile WAT → native assembly
  --arch <arch> ................. x86_64 | x86_32 | x86_16 | arm64 | arm32
  --format <fmt> ................ native | pe
  --output <path> ............... Output file/directory
  
Test-Specific Flags:
  --name <name> ................. Test project name (default: TestProject)
  --keep ........................ Keep test project directory
  --quick ....................... Single arch test (fast)
  
  --save ........................ ❌ NOT IMPLEMENTED YET
                                   Should save compiled outputs

Run-Specific Flags:
  --input <path> ................ WAT file or project directory
  --args <args> ................. Arguments to pass to program
  --keep-temp ................... Keep temporary executable
```

## Memory Model Integration

```
┌────────────────────────────────────────────────────────────────────────┐
│                    CRAB Memory Safety Models                           │
└────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┐          ┌──────────────────────┐
│  Automatic Model     │          │   Manual Model       │
│  (Automatic.cs)      │          │   (Manual.cs)        │
└──────────────────────┘          └──────────────────────┘
│                                  │
│ Managed Memory                   │ manual{} Blocks
│ ───────────────                  │ ───────────────
│ • Lifetime inference             │ • Abstract interpretation
│ • Region analysis                │ • Symbolic execution
│ • CTGC (Compile-Time GC)         │ • Ownership tracking
│ • Auto deallocation              │ • Safety verification
│                                  │
│ Integrated into MapSet           │ Integrated into MapSet
│ via AutomaticModel property      │ via ManualModel property
│                                  │
└──────────────┬───────────────────┘
               │
               ▼
    ┌────────────────────┐
    │ Optimization Model │
    │ (Optimization.cs)  │
    └────────────────────┘
    │
    │ Safe Optimizations
    │ ──────────────────
    │ • Dead code elimination
    │ • Constant folding
    │ • CSE (Common Subexpression Elimination)
    │ • Inlining (safety-preserving)
    │ • Loop optimizations
    │ • Tail call optimization
    │
    │ All preserve CTGC semantics
    │ and memory safety guarantees
    │
    └────────────────────┘
```

## End-to-End Example: test command

```
$ crab test --verbose --arch x86_64 --format native

Step 1: Generate Test Project
  ┌─────────────────────────────────────┐
  │  TestProject/                       │
  │  ├── Program.cs                     │
  │  └── TestProject.csproj             │
  └─────────────────────────────────────┘
                  │
                  ▼
Step 2: Build Project (C# → WAT)
  ┌─────────────────────────────────────┐
  │  Tokens.cs → RuleSet.cs → MapSet.cs │
  │  C# Source → AST → WAT              │
  └─────────────────────────────────────┘
                  │
                  ▼
  TestProject/bin/output.wasm
                  │
                  ▼
Step 3: Comprehensive Test (9 combinations)
  ┌─────────────────────────────────────┐
  │  x86_64 (native) ................✅ │
  │  x86_64 (pe) ....................✅ │
  │  x86_32 (native) ................✅ │
  │  x86_32 (pe) ....................✅ │
  │  x86_16 (native) ................✅ │
  │  ARM64 (native) .................✅ │
  │  ARM64 (pe) .....................✅ │
  │  ARM32 (native) .................✅ │
  │  ARM32 (pe) .....................✅ │
  └─────────────────────────────────────┘
                  │
                  ▼
Step 4: Cleanup
  ┌─────────────────────────────────────┐
  │  Without --keep:                    │
  │    Delete TestProject/              │
  │    Delete all .bin files            │
  │                                     │
  │  With --keep:                       │
  │    Preserve TestProject/            │
  │    Delete .bin files                │
  │                                     │
  │  With --save (TODO):                │
  │    Save output.wasm                 │
  │    Save all .bin files              │
  └─────────────────────────────────────┘
                  │
                  ▼
✓ Test completed successfully
  Passed: 9/9 (100%)
```

---

**Legend:**
- ✅ = Implemented and working
- ❌ = Not implemented / skipped
- ⭐ = Key file for understanding
- TODO = Needs implementation
