# CRAB Compiler - Critical Missing Pieces (Executive Summary)

**Date**: February 2025  
**Status**: Pre-Alpha - Builds successfully, core gaps identified

---

## TL;DR - What's Broken

Three critical areas prevent CRAB from compiling real programs:

1. **String literals don't work** (MapSet.cs:320)
2. **Control flow (if/for/while) doesn't work** (MapSet.cs:872)
3. **Function bodies are stubs** (MapSet.cs:4125-4161)

**Fix these 3 things → working compiler in ~20 hours of work**

---

## 1. MapSet.cs Critical TODOs

### 🔴 CRITICAL: String Literal Emission (Line 320)

**Problem**:
```csharp
// Generates comment instead of WASM code:
return $";; TODO: string literal \"{str}\"";
```

**Blocks**: Console.WriteLine, any string operations, 90% of test programs

**Solution** (6 hours work):
1. Use existing StringRegistry class (lines 9-47)
2. Register string in data section
3. Return pointer to string data
4. Generate data section in module output

**Code needed**:
```csharp
int stringId = StringRegistry.RegisterString(text);
int offset = StringRegistry.GetStringOffset(stringId);
return $"i32.const {offset}";
```

---

### 🔴 CRITICAL: Control Flow Statements (Line 872)

**Problem**:
```csharp
// Most statements generate nop:
_ => $";; TODO: Emit {node.Type}\nnop"
```

**Missing**:
- if/else statements
- while loops
- for loops
- break/continue
- switch statements

**Blocks**: Any program with logic

**Solution** (12 hours work):
Add cases to `EmitStatement()` switch for each statement type.

WASM if/else pattern:
```wat
(if (result i32)
  condition
  (then true-branch)
  (else false-branch)
)
```

WASM loop pattern:
```wat
(block $break
  (loop $continue
    br_if $break  ;; exit condition
    ;; body
    br $continue  ;; loop back
  )
)
```

---

### 🔴 CRITICAL: Function Emission Incomplete (Lines 4125-4161)

**Problem**:
```csharp
// TODO: Parse parameters (line 4149)
// TODO: Parse and emit body instructions (line 4156)
func.Body.Add(new WasmInstruction(OpCode.Nop) { 
    Comment = "TODO: emit function body" 
});
```

**Blocks**: Any function with parameters or real body

**Solution** (4 hours work):
1. Parse parameter list from AST
2. Add parameters to function signature
3. Recursively emit body statements
4. Extract and declare local variables

---

### 🟡 MEDIUM: Expression Emission (Line 142)

**Problem**:
```csharp
// Unknown expressions become placeholders:
_ => $";; TODO: Emit expression {node.Type}\ni32.const 0"
```

**Missing**:
- Array creation/access
- Object creation
- Member access
- Type casts
- Lambdas
- LINQ

**Impact**: Medium - basic programs work, complex ones don't

**Priority**: Phase 2 (after critical fixes)

---

## 2. Memory Models Status

**Files**: 
- `Compiler/Models/Automatic.cs` (1243 lines)
- `Compiler/Models/Manual.cs` (1142 lines)  
- `Compiler/Models/Optimization.cs` (1243 lines)

**Status**: Skeleton implementations

**What exists**:
- ✅ Class structure
- ✅ Data structures
- ✅ API design
- ✅ Phase organization

**What's missing**:
- ❌ Actual algorithms (all are stubs)
- ❌ Lifetime inference
- ❌ Region analysis
- ❌ Ownership tracking
- ❌ Abstract interpretation
- ❌ All verification passes

**Impact**: **LOW** - Models are called but failures are caught and ignored

**Reason to defer**:
1. WASM doesn't have CTGC anyway (future feature)
2. Manual blocks are rare
3. Optimization is optional
4. Doesn't block MVP

**Timeline**: Month 2-3, after code generation works

---

## 3. WAT→WASM Binary Encoding

**File**: `Compiler/Core/WasmIR.cs` (903 lines)

**Status**: Partial implementation

### 🟡 MEDIUM: Incomplete Opcode Mapping (Line 821)

**Problem**:
```csharp
// Only ~10 opcodes mapped, rest return 0x00:
_ => 0x00  // Unreachable - causes WASM validation failure
```

**Current coverage**:
- ✅ Basic control flow: nop, return, end
- ✅ Variables: local.get, local.set
- ✅ Constants: i32.const
- ✅ Basic arithmetic: i32.add
- ❌ Missing: 100+ other opcodes

**Impact**: Can't generate binary .wasm files

**But**: WAT text generation works fine!

**Why this may not matter**:
- Compiler generates WAT text first
- BADGER converts WAT → native assembly
- .wasm binary only needed for browser deployment
- For MVP: WAT output is sufficient

**Solution** (12 hours): Map all 130+ opcodes to bytes using WebAssembly spec

**Priority**: Phase 3 (after basic code generation works)

---

## 4. Priority Implementation Order

### Phase 1: MVP (Week 1) - 20 hours

**Goal**: Compile simple C# programs

**Tasks**:
1. ✅ String literals (6h) - **START HERE**
2. ✅ If/else statements (4h)
3. ✅ While loops (3h)
4. ✅ For loops (4h)
5. ✅ Function parameters (2h)
6. ✅ Function bodies (3h)

**Deliverable**: 
- Compile Hello World
- Compile FizzBuzz
- Compile Factorial

---

### Phase 2: Essential Features (Week 2) - 24 hours

**Goal**: Support arrays and objects

**Tasks**:
1. Array creation/access (8h)
2. Object creation (6h)
3. Member access (4h)
4. More expression types (6h)

**Deliverable**:
- Compile programs with data structures
- Real algorithms (sorting, searching)

---

### Phase 3: Binary Output (Week 3) - 12 hours

**Goal**: Generate .wasm files

**Tasks**:
1. Complete opcode mapping (12h)

**Deliverable**:
- Generate valid .wasm files
- Run in browser
- Deploy to web

---

### Phase 4: Memory Models (Month 2-3) - 120 hours

**Goal**: Implement CTGC and verification

**Tasks**:
1. Automatic model (40h)
2. Manual model (50h)
3. Optimization model (30h)

**Deliverable**:
- Full memory safety verification
- Optimized code generation
- Production-ready compiler

---

## 5. What's Working

### ✅ Strong Foundation

- **Build system**: Clean builds, 0 errors, 0 warnings
- **CDTk integration**: Parser, tokenizer, AST generation work
- **Architecture**: Follows CRAB spec exactly
- **WasmIR design**: Excellent typed IR, clean API
- **BADGER integration**: WAT→ASM works for 6 architectures
- **WAT generation**: Text format output works

### ✅ Partial Implementation

- **Basic expressions**: Literals, arithmetic, simple operations
- **Simple functions**: No-param functions with simple returns
- **Type mapping**: C# types → WASM types works

---

## 6. Quick Verification

### Test Current State

```bash
# Build (should succeed)
cd /home/runner/work/CRAB/CRAB
dotnet build

# Try compiling (will partially work)
dotnet run compile test.cs --output test.wat --verbose
```

### What Works Now

```csharp
// This compiles:
int Add() {
    return 5 + 3;
}
```

### What's Broken Now

```csharp
// This doesn't work (strings):
void Hello() {
    Console.WriteLine("Hello!");
}

// This doesn't work (loops):
int Sum(int n) {
    int total = 0;
    for (int i = 0; i < n; i++) {
        total += i;
    }
    return total;
}

// This doesn't work (parameters):
int Add(int a, int b) {
    return a + b;
}
```

---

## 7. Recommendations

### Immediate Focus (This Week)

**Priority 1**: String literals
- Most blocking issue
- Needed for any I/O
- ~6 hours of work
- **Start here**

**Priority 2**: Control flow
- Needed for any logic
- ~12 hours of work
- If/else + loops

**Priority 3**: Functions
- Parameters + bodies
- ~4 hours of work

**Total**: ~22 hours → working compiler

### Defer to Later

- Memory models (stubs work fine)
- Binary encoding (WAT is enough)
- Advanced features (generics, async, etc.)
- Optimizations (correctness first)

---

## 8. Files Reference

```
Critical files:
  Compiler/Core/MapSet.cs          - Code generation (4178 lines)
    Line 320  - String literals TODO
    Line 872  - Statement emission TODO
    Line 4125 - Function emission TODO
  
  Compiler/Core/WasmIR.cs          - Binary encoding (903 lines)
    Line 821  - Opcode mapping TODO

Lower priority:
  Compiler/Models/Automatic.cs     - Memory model skeleton (1243 lines)
  Compiler/Models/Manual.cs        - Memory model skeleton (1142 lines)
  Compiler/Models/Optimization.cs  - Optimization skeleton (1243 lines)
```

---

## 9. Success Metrics

### MVP Success (Week 1)
- ✅ Strings work
- ✅ If/else works
- ✅ Loops work
- ✅ Functions with params work
- ✅ Can compile Hello World
- ✅ Can compile FizzBuzz

### V1 Success (Month 1)
- ✅ MVP complete
- ✅ Arrays work
- ✅ Objects work
- ✅ Binary .wasm generation works
- ✅ Can compile realistic programs

### V2 Success (Month 2-3)
- ✅ V1 complete
- ✅ Memory models implemented
- ✅ Optimizations work
- ✅ Production-ready

---

## 10. Bottom Line

**Good news**: CRAB has solid architecture and working infrastructure

**Challenge**: Need ~20-30 hours of focused code generation work

**Path forward**: Crystal clear - fix 3 critical TODOs in MapSet.cs

**Timeline**: 
- MVP: 1 week
- V1: 1 month  
- V2: 2-3 months

**Recommendation**: Start with string literals (highest impact, most blocking)

---

**See Also**:
- `CRAB_IMPLEMENTATION_ANALYSIS.md` - Detailed technical analysis
- `IMPLEMENTATION_ROADMAP.md` - Developer reference guide

---

**Document Version**: 1.0  
**Last Updated**: February 2025
