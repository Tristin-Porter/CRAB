# CRAB Compiler - TODO Checklist

Quick reference for implementation tasks. Check off as you complete them.

---

## 🔴 CRITICAL: Phase 1 - MVP (Week 1)

### Task 1: String Literals (6 hours)

**File**: `Compiler/Core/MapSet.cs:320`

- [ ] Extract string text from AstNode in `EmitStringLiteral()`
- [ ] Call `StringRegistry.RegisterString(text)` to register string
- [ ] Get offset with `StringRegistry.GetStringOffset(stringId)`
- [ ] Return `i32.const {offset}` instead of TODO comment
- [ ] Add data section generation in `WasmModule.ToWat()` (line ~590)
- [ ] Test with: `void Hello() { Console.WriteLine("Hi"); }`
- [ ] Verify WAT has `(data ...)` section
- [ ] Verify no more "TODO: string literal" comments

**Success**: Hello World program compiles

---

### Task 2: If/Else Statements (4 hours)

**File**: `Compiler/Core/MapSet.cs:872`

- [ ] Add `case "IfStatement":` to `EmitStatement()` switch
- [ ] Implement `EmitIfStatement(AstNode node)` method
- [ ] Extract condition expression from node
- [ ] Extract then-statement from node
- [ ] Extract optional else-statement from node
- [ ] Generate WAT if-block structure
- [ ] Test with: `if (x > 5) { return 10; }`
- [ ] Test with: `if (x > 5) { return 10; } else { return 0; }`

**Success**: Simple if/else compiles

---

### Task 3: While Loops (3 hours)

**File**: `Compiler/Core/MapSet.cs:872`

- [ ] Add `case "WhileStatement":` to `EmitStatement()` switch
- [ ] Implement `EmitWhileStatement(AstNode node)` method
- [ ] Extract condition expression
- [ ] Extract loop body statement
- [ ] Generate WAT block/loop structure with labels
- [ ] Use `br_if $break` for exit condition
- [ ] Use `br $continue` for loop back
- [ ] Test with: `while (x < 10) { x++; }`

**Success**: Simple while loop compiles

---

### Task 4: For Loops (4 hours)

**File**: `Compiler/Core/MapSet.cs:872`

- [ ] Add `case "ForStatement":` to `EmitStatement()` switch
- [ ] Implement `EmitForStatement(AstNode node)` method
- [ ] Extract initializer statement
- [ ] Extract condition expression
- [ ] Extract increment statement
- [ ] Extract loop body
- [ ] Generate WAT structure: init, block/loop with condition/body/increment
- [ ] Test with: `for (int i = 0; i < 5; i++) { }`
- [ ] Test FizzBuzz program

**Success**: For loops work, FizzBuzz compiles

---

### Task 5: Function Parameters (2 hours)

**File**: `Compiler/Core/MapSet.cs:4149`

- [ ] Replace TODO comment with parameter parsing code
- [ ] Extract parameter list from AST node
- [ ] Parse each parameter's type
- [ ] Map C# type to WASM type with `WasmTypeExtensions.FromCSharpType()`
- [ ] Add parameter types to `func.Type.Parameters`
- [ ] Test with: `int Add(int a, int b) { ... }`
- [ ] Verify WAT has `(param i32) (param i32)`

**Success**: Function signatures include parameters

---

### Task 6: Function Bodies (3 hours)

**File**: `Compiler/Core/MapSet.cs:4156-4161`

- [ ] Replace TODO comment with body emission code
- [ ] Extract statements from body node
- [ ] Call `EmitStatement()` for each statement
- [ ] Add instructions to `func.Body`
- [ ] Handle local variable declarations
- [ ] Remove placeholder nop instruction
- [ ] Test with: `int Add(int a, int b) { return a + b; }`
- [ ] Test factorial function

**Success**: Function bodies have real instructions

---

## ⚠️ MEDIUM: Phase 2 - Essential Features (Week 2)

### Task 7: Array Creation (4 hours)

**File**: `Compiler/Core/MapSet.cs:142`

- [ ] Add `case "ArrayCreationExpression":` to `EmitExpression()`
- [ ] Implement array allocation in linear memory
- [ ] Return pointer to array
- [ ] Handle array size expression
- [ ] Test with: `int[] arr = new int[10];`

---

### Task 8: Array Access (4 hours)

**File**: `Compiler/Core/MapSet.cs:142`

- [ ] Add `case "ElementAccessExpression":` to `EmitExpression()`
- [ ] Calculate array element offset (base + index * element_size)
- [ ] Generate memory load instruction
- [ ] Test with: `int x = arr[5];`
- [ ] Test with: `arr[3] = 42;`

---

### Task 9: Object Creation (6 hours)

**File**: `Compiler/Core/MapSet.cs:142`

- [ ] Add `case "ObjectCreationExpression":` to `EmitExpression()`
- [ ] Implement struct/object allocation
- [ ] Handle constructor calls
- [ ] Generate field offset calculations
- [ ] Test with: `var obj = new MyClass();`

---

### Task 10: Member Access (4 hours)

**File**: `Compiler/Core/MapSet.cs:142`

- [ ] Enhance existing `EmitMemberAccessExpression()`
- [ ] Calculate field offsets from base pointer
- [ ] Generate memory load for field access
- [ ] Test with: `int x = obj.field;`

---

### Task 11: More Expressions (6 hours)

**File**: `Compiler/Core/MapSet.cs:142`

- [ ] Add missing expression types (type casts, etc.)
- [ ] Add comparison operators (==, !=, <, >, <=, >=)
- [ ] Add logical operators (&&, ||, !)
- [ ] Add unary operators (++, --, !)
- [ ] Test comprehensive expressions

---

## ⚠️ MEDIUM: Phase 3 - Binary WASM (Week 3)

### Task 12: Complete Opcode Mapping (12 hours)

**File**: `Compiler/Core/WasmIR.cs:803-825`

Reference: https://webassembly.github.io/spec/core/binary/instructions.html

- [ ] Map control flow opcodes (block, loop, if, br, etc.)
- [ ] Map all i32 operations (30+ opcodes)
- [ ] Map all i64 operations (30+ opcodes)
- [ ] Map all f32 operations (20+ opcodes)
- [ ] Map all f64 operations (20+ opcodes)
- [ ] Map memory operations (loads, stores)
- [ ] Map conversion operations (trunc, extend, etc.)
- [ ] Test binary .wasm generation
- [ ] Validate with wat2wasm
- [ ] Test loading in browser

---

## ⏸️ LOW: Phase 4 - Memory Models (Month 2-3)

### Task 13: Automatic Memory Model (40 hours)

**File**: `Compiler/Models/Automatic.cs`

- [ ] Implement lifetime inference algorithm
- [ ] Implement region analysis
- [ ] Implement allocation tracking
- [ ] Implement deallocation point computation
- [ ] Implement memory safety verification
- [ ] Add comprehensive tests

---

### Task 14: Manual Memory Model (50 hours)

**File**: `Compiler/Models/Manual.cs`

- [ ] Implement ownership graph construction
- [ ] Implement abstract interpretation
- [ ] Implement symbolic execution
- [ ] Implement alias tracking
- [ ] Implement escape analysis
- [ ] Implement model isolation checks

---

### Task 15: Optimization Model (30 hours)

**File**: `Compiler/Models/Optimization.cs`

- [ ] Implement dead code elimination
- [ ] Implement constant folding
- [ ] Implement common subexpression elimination
- [ ] Implement inlining analysis
- [ ] Implement loop optimizations
- [ ] Implement tail call optimization
- [ ] Implement peephole optimizations

---

## 📊 Progress Tracking

### MVP Milestone (Week 1)
Progress: [___________] 0/6 tasks

- [ ] Task 1: String literals
- [ ] Task 2: If/else
- [ ] Task 3: While loops
- [ ] Task 4: For loops
- [ ] Task 5: Function parameters
- [ ] Task 6: Function bodies

**MVP Complete when**:
- Hello World compiles
- FizzBuzz compiles
- Factorial compiles
- No "TODO" comments in generated WAT

---

### V1 Milestone (Month 1)
Progress: [___________] 0/5 tasks

- [ ] MVP complete
- [ ] Task 7: Array creation
- [ ] Task 8: Array access
- [ ] Task 9: Object creation
- [ ] Task 10: Member access
- [ ] Task 11: More expressions

**V1 Complete when**:
- Can compile programs with arrays
- Can compile programs with objects
- Can generate valid .wasm files

---

### V2 Milestone (Month 2-3)
Progress: [___________] 0/3 tasks

- [ ] V1 complete
- [ ] Task 13: Automatic model
- [ ] Task 14: Manual model
- [ ] Task 15: Optimization model

**V2 Complete when**:
- Full CTGC implementation
- Memory safety verification working
- Optimizations functional
- Production ready

---

## 📝 Notes & Discoveries

Use this section to track issues, workarounds, or insights as you implement:

```
Date: ___________
Task: ___________
Issue: _____________________________________________________
Solution: __________________________________________________

Date: ___________
Task: ___________
Issue: _____________________________________________________
Solution: __________________________________________________

Date: ___________
Task: ___________
Issue: _____________________________________________________
Solution: __________________________________________________
```

---

## ✅ Verification Tests

### Test Programs

Create these test programs to verify each phase:

**test1_hello.cs** (Tests: strings)
```csharp
void Main() {
    Console.WriteLine("Hello, CRAB!");
}
```

**test2_params.cs** (Tests: parameters)
```csharp
int Add(int a, int b) {
    return a + b;
}
```

**test3_if.cs** (Tests: if/else)
```csharp
int Max(int a, int b) {
    if (a > b) {
        return a;
    } else {
        return b;
    }
}
```

**test4_while.cs** (Tests: while loop)
```csharp
int Sum(int n) {
    int total = 0;
    int i = 1;
    while (i <= n) {
        total += i;
        i++;
    }
    return total;
}
```

**test5_for.cs** (Tests: for loop)
```csharp
int Factorial(int n) {
    int result = 1;
    for (int i = 1; i <= n; i++) {
        result *= i;
    }
    return result;
}
```

**test6_fizzbuzz.cs** (Tests: combination)
```csharp
void FizzBuzz(int n) {
    for (int i = 1; i <= n; i++) {
        if (i % 15 == 0) {
            Console.WriteLine("FizzBuzz");
        } else if (i % 3 == 0) {
            Console.WriteLine("Fizz");
        } else if (i % 5 == 0) {
            Console.WriteLine("Buzz");
        } else {
            Console.WriteLine(i);
        }
    }
}
```

---

**Document Version**: 1.0  
**Last Updated**: February 2025  
**Total Tasks**: 15 (6 critical, 5 medium, 3 low, 1 deferred)
