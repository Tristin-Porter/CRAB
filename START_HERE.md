# 🚀 CRAB Compiler - START HERE

**You want to make CRAB compile real programs. This is where you start.**

---

## ⚡ The 60-Second Summary

CRAB has a **solid foundation** but **3 critical gaps** prevent it from compiling real programs.

**What's broken**:
1. String literals don't work (line 320)
2. If/for/while don't work (line 872)
3. Functions are incomplete (lines 4125-4161)

**Fix these 3 things = working compiler in ~22 hours**

---

## 🎯 Your Mission (If You Choose to Accept)

### Priority #1: String Literals (START HERE) ⚠️

**File**: `Compiler/Core/MapSet.cs`  
**Line**: 320  
**Time**: 6 hours  
**Impact**: CRITICAL - blocks all I/O

**Current code**:
```csharp
// Line 320 - generates comment instead of WASM
return $";; TODO: string literal \"{str}\"";
```

**What to do**:
```csharp
private static string EmitStringLiteral(AstNode node)
{
    // 1. Extract string text from node
    var text = /* parse node to get string value */;
    
    // 2. Register in StringRegistry (already exists!)
    int stringId = StringRegistry.RegisterString(text);
    int offset = StringRegistry.GetStringOffset(stringId);
    
    // 3. Return pointer to string in linear memory
    return $"i32.const {offset}  ;; string literal \"{text}\"";
}
```

**Also need**: Generate data section in module
```csharp
// In WasmModule.ToWat() method (around line 590):
if (StringRegistry.GetAllStrings().Any())
{
    sb.AppendLine("  (data (i32.const 0)");
    foreach (var str in StringRegistry.GetAllStrings())
    {
        sb.Append($"    \"{str.Value}\\00\"");
    }
    sb.AppendLine("  )");
}
```

**Test**:
```csharp
// Create test.cs:
void Hello() {
    Console.WriteLine("Hello, CRAB!");
}

// Compile:
dotnet run compile test.cs --output test.wat --verbose

// Should generate WAT with data section:
// (data (i32.const 0) "Hello, CRAB!\00")
```

---

### Priority #2: Control Flow (After strings work)

**File**: `Compiler/Core/MapSet.cs`  
**Line**: 872  
**Time**: 12 hours  
**Impact**: CRITICAL - can't write logic

**What to do**: Add cases to `EmitStatement()` switch

**If/Else pattern**:
```csharp
case "IfStatement":
    return EmitIfStatement(node);

private static string EmitIfStatement(AstNode node)
{
    var condition = EmitExpression(node["condition"]);
    var thenStmt = EmitStatement(node["thenStmt"]);
    var elseStmt = node.Fields.ContainsKey("elseStmt") 
        ? EmitStatement(node["elseStmt"]) 
        : "";
    
    return $@"
{condition}
(if
  (then
    {thenStmt}
  )
  {(elseStmt != "" ? $"(else\n    {elseStmt}\n  )" : "")}
)";
}
```

**While loop pattern**:
```csharp
case "WhileStatement":
    return EmitWhileStatement(node);

private static string EmitWhileStatement(AstNode node)
{
    var condition = EmitExpression(node["condition"]);
    var body = EmitStatement(node["body"]);
    
    return $@"
(block $break
  (loop $continue
    {condition}
    i32.eqz
    br_if $break
    {body}
    br $continue
  )
)";
}
```

**For loop pattern**:
```csharp
case "ForStatement":
    return EmitForStatement(node);

private static string EmitForStatement(AstNode node)
{
    var init = EmitStatement(node["initializer"]);
    var condition = EmitExpression(node["condition"]);
    var increment = EmitStatement(node["increment"]);
    var body = EmitStatement(node["body"]);
    
    return $@"
{init}
(block $break
  (loop $continue
    {condition}
    i32.eqz
    br_if $break
    {body}
    {increment}
    br $continue
  )
)";
}
```

---

### Priority #3: Function Emission (After control flow works)

**File**: `Compiler/Core/MapSet.cs`  
**Lines**: 4125-4161  
**Time**: 4 hours  
**Impact**: CRITICAL - functions don't work

**What to do**:

1. **Parse parameters** (line 4149):
```csharp
// Replace TODO comment with:
if (node["parameters"] is AstNode paramsNode)
{
    // Parameters are in a list
    if (paramsNode["params"] is List<AstNode> paramList)
    {
        foreach (var param in paramList)
        {
            var paramType = param["type"] as string ?? "int";
            var wasmType = WasmTypeExtensions.FromCSharpType(paramType);
            func.Type.Parameters.Add(wasmType);
        }
    }
}
```

2. **Emit function body** (line 4156):
```csharp
// Replace TODO with:
if (node["body"] is AstNode bodyNode)
{
    // Extract statements from body
    if (bodyNode["stmts"] is List<AstNode> stmts)
    {
        foreach (var stmt in stmts)
        {
            var instructions = EmitStatement(stmt);
            // Parse WAT string back to instructions (temporary)
            // Or better: refactor EmitStatement to return WasmInstructionSequence
            func.Body.Add(new WasmInstruction(OpCode.Nop) 
                { Comment = instructions }); // Temporary
        }
    }
}
```

**Better approach**: Refactor `EmitStatement()` to return `WasmInstructionSequence` instead of string (more work but cleaner).

---

## 🧪 Testing Your Changes

### Step 1: Build
```bash
cd /home/runner/work/CRAB/CRAB
dotnet build
```

### Step 2: Create Test File
```bash
cat > test.cs << 'EOL'
int Add(int a, int b) {
    return a + b;
}
EOL
```

### Step 3: Compile
```bash
dotnet run compile test.cs --output test.wat --verbose
```

### Step 4: Inspect Output
```bash
cat test.wat
```

Should see:
```wat
(module
  (func $Add (param i32) (param i32) (result i32)
    local.get 0
    local.get 1
    i32.add
    return
  )
  (export "Add" (func $Add))
)
```

### Step 5: Validate
```bash
# If you have wat2wasm installed:
wat2wasm test.wat -o test.wasm

# Or try BADGER compilation:
dotnet run compile test.cs --to-asm --arch x86_64
```

---

## 📁 Key Files

```
/home/runner/work/CRAB/CRAB/
├── Compiler/Core/MapSet.cs          ⚠️ PRIMARY WORK HERE
│   ├── Line 320   - String literals
│   ├── Line 872   - Control flow
│   └── Line 4125  - Function emission
│
├── Compiler/Core/WasmIR.cs          📖 Reference for IR types
│   ├── WasmModule
│   ├── WasmFunction
│   └── WasmInstruction
│
├── CLI/Commands/Compile.cs          📖 Reference for pipeline
│
└── Testing/Unit/MapSetTests.cs      📖 Reference for tests
```

---

## 🎯 Success Checklist

### After String Literals:
- [ ] Can compile: `void Hello() { Console.WriteLine("Hi"); }`
- [ ] Generated WAT has data section
- [ ] String appears at memory offset 0

### After Control Flow:
- [ ] Can compile: `if (x > 5) { return 10; }`
- [ ] Can compile: `while (x < 10) { x++; }`
- [ ] Can compile: `for (int i = 0; i < 5; i++) { }`

### After Function Emission:
- [ ] Can compile: `int Add(int a, int b) { return a + b; }`
- [ ] Parameters appear in function signature
- [ ] Function body has real instructions (not nop)

### MVP Complete:
- [ ] All above work
- [ ] Hello World compiles
- [ ] FizzBuzz compiles
- [ ] Factorial compiles
- [ ] No "TODO" comments in generated WAT

---

## 💡 Pro Tips

### Tip 1: Debug with Console.WriteLine
```csharp
System.Console.WriteLine($"DEBUG: Processing {node.Type}");
System.Console.WriteLine($"DEBUG: Fields = {string.Join(", ", node.Fields.Keys)}");
```

### Tip 2: Look at Working Code
Before implementing new feature, find similar working feature:
- String literals → Look at integer literals (line 86)
- If statements → Look at return statements (line 879)
- Loops → Look at blocks (line 892)

### Tip 3: Test Incrementally
Don't implement all 3 priorities at once. Do:
1. Implement string literals
2. Test thoroughly
3. Move to control flow
4. Test thoroughly
5. Move to functions

### Tip 4: Use WAT Reference
Keep WebAssembly text format reference open:
https://webassembly.github.io/spec/core/text/

### Tip 5: Start Simple
For if/else, implement simple case first:
```csharp
if (condition) {
    statement;
}
```

Then add else:
```csharp
if (condition) {
    thenStatement;
} else {
    elseStatement;
}
```

Then add else-if chains later.

---

## 🆘 Common Issues

### Issue: "Cannot find StringRegistry"
**Solution**: It's at line 9 in MapSet.cs. Use `StringRegistry.RegisterString(text)`

### Issue: "Node doesn't have expected field"
**Solution**: Check what fields exist:
```csharp
Console.WriteLine($"Fields: {string.Join(", ", node.Fields.Keys)}");
```

### Issue: "Generated WAT fails validation"
**Solution**: 
1. Check parentheses are balanced
2. Check instruction order (operands before operation)
3. Use wat2wasm to see exact error

### Issue: "Don't know what node type to handle"
**Solution**: Add debug print to see what types appear:
```csharp
Console.WriteLine($"Unhandled node type: {node.Type}");
```

---

## 📚 More Documentation

- **Quick overview**: `EXECUTIVE_SUMMARY.md`
- **Developer guide**: `IMPLEMENTATION_ROADMAP.md`
- **Detailed analysis**: `CRAB_IMPLEMENTATION_ANALYSIS.md`
- **Visual status**: `IMPLEMENTATION_STATUS.txt`

---

## 🏁 Ready?

1. Open `Compiler/Core/MapSet.cs`
2. Go to line 320
3. Start implementing string literals
4. Test with Hello World
5. Move to next priority

**Good luck! You've got this! 🚀**

---

**Last Updated**: February 2025  
**Estimated Time to MVP**: 22 hours  
**Files to Edit**: Primarily MapSet.cs
