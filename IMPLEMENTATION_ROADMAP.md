# CRAB Compiler - Implementation Roadmap

Quick reference for developers working on CRAB implementation.

---

## 🎯 Top 3 Critical Priorities

### 1. String Literals (CRITICAL) ⚠️
**File**: `Compiler/Core/MapSet.cs:320`  
**Status**: TODO comment, no implementation  
**Impact**: Blocks most real programs (Console.WriteLine, etc.)

**What to do**:
```csharp
// Current (line 320):
return $";; TODO: string literal \"{str}\"";

// Need to implement:
private static string EmitStringLiteral(AstNode node)
{
    var text = ExtractStringText(node);
    int stringId = StringRegistry.RegisterString(text);
    int offset = StringRegistry.GetStringOffset(stringId);
    return $"i32.const {offset}  ;; string \"{text}\"";
}
```

**Also need**:
- Add data section to WasmModule output
- Format: `(data (i32.const 0) "string1\00string2\00")`

**Files to modify**:
- `MapSet.cs` line 320 (EmitStringLiteral)
- `MapSet.cs` line ~4000 (add data section generation)

---

### 2. Control Flow Statements (CRITICAL) ⚠️
**File**: `Compiler/Core/MapSet.cs:872`  
**Status**: Fallback to nop  
**Impact**: Can't compile loops or if statements

**Missing**:
- `if (condition) { }` / `if { } else { }`
- `while (condition) { }`
- `for (init; condition; increment) { }`
- `break` / `continue`

**What to do**:
Add cases to EmitStatement() switch:
```csharp
case "IfStatement":
    return EmitIfStatement(node);
case "WhileStatement":
    return EmitWhileStatement(node);
case "ForStatement":
    return EmitForStatement(node);
```

**WASM patterns**:
```wat
;; if-else
(if (result i32)
  (i32.const 1)  ;; condition
  (then
    ;; true branch
  )
  (else
    ;; false branch
  )
)

;; while loop
(block $break
  (loop $continue
    ;; condition check
    br_if $break
    ;; loop body
    br $continue
  )
)
```

---

### 3. Function Emission (CRITICAL) ⚠️
**File**: `Compiler/Core/MapSet.cs:4125-4161`  
**Status**: Stub with TODOs  
**Impact**: Functions don't work properly

**Current issues**:
- Parameters not parsed (line 4149)
- Body is just a placeholder (line 4161)
- Local variables not extracted

**What to do**:
```csharp
// Parse parameters (line 4149)
if (node["parameters"] is AstNode paramsNode)
{
    foreach (var param in ExtractParameters(paramsNode))
    {
        func.Type.Parameters.Add(
            WasmTypeExtensions.FromCSharpType(param.Type)
        );
    }
}

// Parse body (line 4156)
if (node["body"] is AstNode bodyNode)
{
    var bodyStmts = EmitStatementList(bodyNode);
    func.Body.AddRange(bodyStmts.Instructions);
}
```

---

## 📋 Implementation Checklist

### Phase 1: MVP (1 week)
- [ ] String literal emission (6 hrs)
- [ ] Data section generation (2 hrs)
- [ ] If/else statements (4 hrs)
- [ ] While loops (3 hrs)
- [ ] For loops (4 hrs)
- [ ] Function parameter parsing (2 hrs)
- [ ] Function body emission (3 hrs)
- [ ] Test: Hello World
- [ ] Test: Factorial
- [ ] Test: FizzBuzz

### Phase 2: Arrays & Objects (1 week)
- [ ] Array creation: `new int[10]`
- [ ] Array access: `arr[i]`
- [ ] Array assignment: `arr[i] = value`
- [ ] Object creation: `new ClassName()`
- [ ] Field access: `obj.field`
- [ ] Method calls: `obj.Method()`

### Phase 3: Binary Encoding (3 days)
- [ ] Complete opcode byte mapping (WasmIR.cs:821)
- [ ] Test .wasm file generation
- [ ] Verify with wat2wasm
- [ ] Load in browser

### Phase 4: Advanced Features (2-3 weeks)
- [ ] Properties
- [ ] Indexers
- [ ] Foreach loops
- [ ] Switch statements
- [ ] Try/catch
- [ ] Using statements

---

## 🔍 Where Things Are

### Code Generation
- **Main file**: `Compiler/Core/MapSet.cs` (4178 lines)
- **Expression emission**: Lines 60-144 (EmitExpression)
- **Statement emission**: Lines 848-873 (EmitStatement)
- **Function emission**: Lines 4120-4165 (EmitFunction)
- **String registry**: Lines 9-47 (StringRegistry)

### IR & Binary Encoding
- **Main file**: `Compiler/Core/WasmIR.cs` (903 lines)
- **OpCode enum**: Lines 130-327
- **Binary encoder**: Lines 619-902
- **Opcode mapping**: Lines 803-825 (GetOpCodeByte) ⚠️ INCOMPLETE

### Memory Models (Low Priority)
- **Automatic**: `Compiler/Models/Automatic.cs` (1243 lines) - Skeleton
- **Manual**: `Compiler/Models/Manual.cs` (1142 lines) - Skeleton
- **Optimization**: `Compiler/Models/Optimization.cs` (1243 lines) - Skeleton

### Compilation Pipeline
- **CLI command**: `CLI/Commands/Compile.cs` (392 lines)
- **Pipeline**: C# → CDTk → AST → MapSet → WAT → BADGER → ASM

---

## 🐛 Common Issues

### Issue 1: "TODO: string literal"
**Symptom**: String literals generate comments instead of WASM  
**Location**: MapSet.cs:320  
**Fix**: Implement EmitStringLiteral() with StringRegistry

### Issue 2: "TODO: Emit {NodeType}"
**Symptom**: Statement/expression generates nop  
**Location**: MapSet.cs:142, 872  
**Fix**: Add case to switch statement in EmitExpression/EmitStatement

### Issue 3: Functions have no body
**Symptom**: Functions compile but do nothing  
**Location**: MapSet.cs:4161  
**Fix**: Implement recursive body emission

### Issue 4: Binary WASM validation fails
**Symptom**: .wasm file rejected by runtime  
**Location**: WasmIR.cs:821  
**Fix**: Complete opcode byte mapping

---

## 🧪 Testing

### Quick Test Commands
```bash
# Build
dotnet build

# Run test suite
dotnet run test

# Compile single file
dotnet run compile input.cs --output output.wat

# Compile to native
dotnet run compile input.cs --to-asm --arch x86_64
```

### Test Programs

**Test 1: Literals**
```csharp
int Add() { return 5 + 3; }
```
Expected WAT:
```wat
(func $Add (result i32)
  i32.const 5
  i32.const 3
  i32.add
  return
)
```

**Test 2: Strings** (currently broken)
```csharp
void Hello() {
    Console.WriteLine("Hello!");
}
```

**Test 3: Loop** (currently broken)
```csharp
int Sum(int n) {
    int total = 0;
    for (int i = 0; i < n; i++) {
        total += i;
    }
    return total;
}
```

---

## 📚 Resources

### WebAssembly Spec
- **MVP Instructions**: https://webassembly.github.io/spec/core/binary/instructions.html
- **Binary Format**: https://webassembly.github.io/spec/core/binary/
- **Text Format**: https://webassembly.github.io/spec/core/text/

### Tools
- **wat2wasm**: Convert WAT to WASM binary
- **wasm-validate**: Validate WASM files
- **wasm-objdump**: Inspect WASM binaries
- **wasmtime**: Run WASM from command line

### CDTk Documentation
- CDTk integration: `__AllRules`, `__Ast`, `AstNode`
- MapSet base class provides compilation context

---

## 💡 Implementation Tips

### Tip 1: Test WAT Output First
Generate WAT text and validate with wat2wasm before worrying about binary encoding.

### Tip 2: Use Debug Prints
```csharp
System.Console.WriteLine($"DEBUG: node type={node.Type}, fields={string.Join(",", node.Fields.Keys)}");
```

### Tip 3: Reference Existing Implementations
Look at working cases:
- Integer literals (line 86): `EmitIntegerLiteral`
- Binary operations (line 901): `EmitBinaryExpression`
- Return statements (line 879): `EmitReturnStatement`

### Tip 4: Handle CDTk Field Names
CDTk may use different field names:
- `node["expr"]` or `node.Fields["expr"]`
- `node.Fields.ContainsKey("expr")` to check
- Cast with: `node.Fields["expr"] as AstNode`

### Tip 5: Memory Models Can Wait
The automatic/manual memory models are aspirational. They're called but failures are caught and ignored. Focus on code generation first.

---

## 🎓 Understanding the Architecture

### Compilation Flow
```
C# source
  ↓
CDTk Tokenizer → TokenSet
  ↓
CDTk Parser → RuleSet
  ↓
AST (AstNode tree)
  ↓
MapSet (WASM target)
  ├→ Automatic Model (optional)
  ├→ Manual Model (optional)
  └→ Optimization Model (optional)
  ↓
WAT text format
  ↓
Option 1: Save as .wat
Option 2: WasmBinaryEncoder → .wasm
Option 3: BADGER → native ASM
```

### Key Classes
- **MapSet**: Base class for target emission
- **WASM**: Concrete MapSet implementation for WebAssembly
- **WasmModule**: IR for complete WASM module
- **WasmFunction**: IR for single function
- **WasmInstruction**: IR for single instruction
- **Model**: Base class for semantic analysis (Automatic, Manual, Optimization)

---

## 📞 Getting Help

### Debug Compilation
```bash
# Verbose output
dotnet run compile input.cs --verbose

# See AST
dotnet run compile input.cs --verbose 2>&1 | grep "DEBUG:"
```

### Check What's Generated
```bash
# Compile and inspect
dotnet run compile test.cs --output test.wat
cat test.wat

# Validate WAT
wat2wasm test.wat

# Run in browser
# Create HTML with <script src="test.js"></script>
# test.js loads test.wasm
```

---

## ✅ Success Criteria

### MVP Complete When:
- ✅ Can compile: int Add(int a, int b) { return a + b; }
- ✅ Can compile: string literals and Console.WriteLine
- ✅ Can compile: if/else statements
- ✅ Can compile: for/while loops
- ✅ Can compile: arrays
- ✅ Generated WAT passes wat2wasm validation
- ✅ Generated code runs in browser/Node.js

### Full V1 Complete When:
- ✅ MVP criteria met
- ✅ Can generate binary .wasm files
- ✅ Can compile realistic C# programs
- ✅ All C# basic types supported
- ✅ Classes and objects work
- ✅ Standard library basics implemented

---

**Document Version**: 1.0  
**For Questions**: See CRAB_IMPLEMENTATION_ANALYSIS.md for detailed analysis  
**Last Updated**: February 2025
