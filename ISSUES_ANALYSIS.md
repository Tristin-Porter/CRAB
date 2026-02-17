# CRAB Compiler Issues - Root Cause Analysis

## Executive Summary

The CRAB compiler has critical bugs in its WASM code generation that cause:
1. Stack underflow errors when compiling expressions with string concatenation
2. Non-functional portable executables that close immediately
3. Calculator and other examples failing due to these issues

All issues stem from incorrect handling of expressions that mix strings with other types, and missing implementations of Console I/O backends.

---

## Issue #1: WASM Stack Underflow - "not enough arguments on the stack for call"

### Error Message
```
not enough arguments on the stack for call (need 1, got 0) @+72
OR
function index 6 out of bounds (5 entries) @+110
```

### Test Case
```csharp
Console.WriteLine("Hello World!" + "\nThis is MATH: 1 + 2 = " + (1+2));
```

### Generated WASM (BUGGY)
```wat
(func $Main
  i32.const 0     ; Push "Hello World!" ptr
  i32.const 12    ; Push "Hello World!" len
  i32.add         ; ❌ BUG: Adds ptr+len, consumes both, leaves 1 value
  i32.const 13    ; Push second string ptr
  i32.const 24    ; Push second string len
  i32.add         ; ❌ BUG: Adds ptr+len again
  i32.add         ; ❌ BUG: Adds the two previous sums
  call $console_log  ; ❌ CRASH: Expects (i32, i32) but stack has only 1 value!
)
```

### Root Cause

The bug is in `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs` in the `EmitBinaryExpression` function (lines 869-966).

**The Problem:**
1. When processing nested string concatenation like `"A" + "B" + (1+2)`, the parser creates nested `AdditiveExpression` nodes
2. The string concatenation detection (lines 931-954) correctly identifies when BOTH operands are strings
3. **BUT** when the left operand is itself an `AdditiveExpression` (from a previous concatenation), `IsStringExpression()` returns FALSE for it
4. This causes the code to fall through to the integer addition path (lines 956-965)
5. Line 963 calls `MapOperator("+")` which returns `"i32.add"` (line 975)
6. This inserts `i32.add` instructions that consume the (ptr, len) pairs, corrupting the stack

**Visual Flow:**
```
Expression: "A" + "B" + (1+2)

AST:
  AdditiveExpression("+")                    ← Outer expression
  ├── left: AdditiveExpression("+")         ← Inner expression (evaluated first)
  │   ├── left: StringLiteral("A")
  │   └── right: StringLiteral("B")
  └── right: ParenthesizedExpression
      └── AdditiveExpression("+")
          ├── left: IntegerLiteral(1)
          └── right: IntegerLiteral(2)

Processing:
1. Inner AdditiveExpression("A" + "B"):
   - IsStringExpression(left=StringLiteral) → TRUE
   - IsStringExpression(right=StringLiteral) → TRUE  
   - ✓ Emits: i32.const 0, i32.const N (ptr, len for both strings)
   - ✓ Returns string concatenation code

2. Outer AdditiveExpression((inner) + (1+2)):
   - IsStringExpression(left=AdditiveExpression) → FALSE ❌
   - IsStringExpression(right=ParenthesizedExpression) → FALSE ❌
   - ✗ Falls through to integer addition path
   - ✗ Calls MapOperator("+") → "i32.add"
   - ✗ Emits: (left code) (right code) i32.add
   - ✗ This inserts i32.add which consumes the (ptr, len) pairs!
```

### Why `IsStringExpression` Fails

Located at lines 528-562 in MapSet.cs:

```csharp
private static bool IsStringExpression(object? node)
{
    if (node is AstNode astNode)
    {
        // Only checks for StringLiteral or AdditiveExpression with string operands
        if (astNode.Type == "StringLiteral")
            return true;
        
        if (astNode.Type == "AdditiveExpression" && 
            astNode.Fields.ContainsKey("left") && 
            astNode.Fields.ContainsKey("right"))
        {
            var left = astNode.Fields["left"];
            var right = astNode.Fields["right"];
            return IsStringExpression(left) || IsStringExpression(right);
        }
        
        // ... tries to unwrap some wrappers ...
    }
    return false;
}
```

**The Issue:** This function recursively checks AdditiveExpression, but when processing the OUTER expression, the INNER AdditiveExpression has already been processed and its code emitted. The recursion works, but the problem is the recursive call at line 543 happens BEFORE the outer expression emits its code, so both expressions try to emit operators.

**The Real Bug:** Lines 956-965 should NEVER execute when either operand is a string expression, but the check at line 932 only catches cases where AT LEAST ONE operand is directly identifiable as a string. When both operands are complex expressions that evaluate to strings, the check fails.

---

## Issue #2: Incorrect String Concatenation with Integer Conversion

### Problem
When concatenating strings with integers like `"Result: " + (1+2)`, the code calls `$int_to_string` but this function is NOT implemented (lines 2072-2077 in MapSet.cs):

```wat
(func $int_to_string (param $value i32) (result i32) (result i32)
  ;; TODO: Implement integer to string conversion
  i32.const 0  ;; ptr - RETURNS NULL!
  i32.const 0  ;; len - RETURNS 0!
)
```

This causes:
1. Integer to string conversion returns (ptr=0, len=0)
2. String concatenation receives invalid pointers
3. Console.WriteLine receives corrupted string data
4. Output is incomplete or missing

---

## Issue #3: Portable Executables Close Immediately

### Problem
PE (Portable Executable) files generated by CRAB open a console window and immediately close without displaying output.

### Root Cause
The PE backend in BADGER (at `/home/runner/work/CRAB/CRAB/Dependencies/BADGER/Containers/PE.cs`) needs to:
1. Implement Windows Console API calls for `Console.WriteLine` (WriteConsoleA/W)
2. Implement blocking wait for `Console.ReadKey` (ReadConsoleInput)
3. Keep the console window open until user input is received

Currently, the PE container likely emits placeholder code that doesn't actually call Windows APIs, so:
- Text is not written to the console
- No blocking wait occurs
- The program exits immediately

### Expected Behavior
When `Console.ReadKey()` is called at the end of a program, it should:
1. Call Windows API `ReadConsoleInput` to wait for a key press
2. Block execution until a key is pressed
3. Return the key information
4. This prevents the console from closing

---

## Issue #4: Calculator Example Doesn't Work

### File: `/home/runner/work/CRAB/CRAB/Testing/TestProjects/Calculator.cs`

The calculator uses:
```csharp
Console.WriteLine("Simple Calculator");
Console.WriteLine("================");
Console.WriteLine("");
int num1 = 5;
int num2 = 3;
int result = num1 + num2;
Console.WriteLine("Calculating: 5 + 3");
Console.WriteLine("Result: 8");
Console.WriteLine("");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();
```

**Problems:**
1. String literals in WriteLine work fine for simple cases
2. But string concatenation with integers (like `"Result: " + result`) would fail due to Issue #1 and #2
3. `Console.ReadKey()` doesn't block in PE due to Issue #3
4. In WASM, `console_readkey` import returns 0 immediately (non-blocking)

The comment in the file says "For now using hardcoded values until Console.ReadLine() is implemented" - this was a workaround attempt, but even with hardcoded values, the display issues prevent it from working.

---

## Issue #5: Calculator Should Take Input

### Requirement
The calculator should allow users to:
1. Input a first number
2. Input a second number  
3. Display the sum

### Missing Implementation
`Console.ReadLine()` is declared in `/home/runner/work/CRAB/CRAB/StandardLibrary/System/System.Console.cs` but is a stub:

```csharp
public static string ReadLine()
{
    // Read a line of text from console
    // For PE, this will use Windows console API and wait for user input
    // This prevents the window from closing
    return "";  // ❌ Always returns empty string
}
```

**Required Implementation:**
1. WASM backend: Import a JavaScript function that uses `prompt()` or creates an input field
2. PE backend: Call Windows API `ReadConsoleA` or `ReadConsoleW` with proper buffer handling
3. Parse the input string and return it

---

## Summary of Fixes Needed

### Priority 1: Fix WASM Stack Corruption (Issue #1)
**Location:** `Compiler/Core/MapSet.cs`, function `EmitBinaryExpression` (lines 869-966)

**Solution:** 
The core issue is that when we detect string concatenation, we emit the string concatenation code, but then the parent expression also emits code. We need to:

1. **Option A (Recommended):** Make `IsStringExpression` detect when an expression EVALUATES to a string, even if it's not a literal:
   - Track expression types through semantic analysis
   - Mark `AdditiveExpression` nodes that produce strings
   - Check this metadata in `IsStringExpression`

2. **Option B (Quick Fix):** Improve the heuristic in `IsStringExpression`:
   - If node is `AdditiveExpression` AND recursion says either operand is a string, mark the whole expression as string
   - Currently line 550 checks `||` (OR) which is correct
   - But the check at line 932 in the OUTER function needs to trust this

3. **Option C (Defensive):** In `EmitBinaryExpression`, after checking for string concat (line 932), add early return:
   ```csharp
   if (op == "+" && (IsStringExpression(left) || IsStringExpression(right)))
   {
       // ... existing string concat code ...
       return $"{leftStrCode}\n{rightStrCode}\ncall $string_concat";
       // NO FALLTHROUGH - this is the bug!
   }
   ```
   Then remove lines 956-965 as they would never execute for strings.

**The actual bug is that there's NO early return after the string concatenation code!** Lines 956-965 always execute, even after string concatenation was handled. This is the smoking gun.

### Priority 2: Implement `$int_to_string` (Issue #2)
**Location:** `Compiler/Core/MapSet.cs`, lines 2072-2077

**Solution:**
Implement integer to string conversion in WASM:
```wat
(func $int_to_string (param $value i32) (result i32) (result i32)
  (local $ptr i32)
  (local $len i32)
  (local $temp i32)
  (local $digit i32)
  
  ;; Allocate 12 bytes for string (max digits for i32: 11 + null)
  global.get $heap_ptr
  local.set $ptr
  
  ;; Handle negative numbers
  (if (i32.lt_s (local.get $value) (i32.const 0))
    (then
      ;; Store '-' character
      (i32.store8 (local.get $ptr) (i32.const 45))
      local.get $ptr
      i32.const 1
      i32.add
      local.set $ptr
      
      ;; Negate value
      (local.set $value (i32.sub (i32.const 0) (local.get $value)))
    )
  )
  
  ;; Convert digits (reverse order, then flip)
  ;; ... implementation details ...
  
  ;; Return (ptr, len)
  local.get $ptr
  local.get $len
)
```

Alternatively, import from JavaScript:
```wat
(import "env" "int_to_string" (func $int_to_string (param i32) (result i32) (result i32)))
```

### Priority 3: Fix PE Console I/O (Issue #3)
**Location:** `Dependencies/BADGER/Containers/PE.cs`

**Solution:**
Implement Windows Console API calls:
1. For `Console.WriteLine`: Call `WriteConsoleA` or `WriteConsoleW`
2. For `Console.ReadKey`: Call `ReadConsoleInput` with `INPUT_RECORD` structure
3. For `Console.ReadLine`: Call `ReadConsoleA` or `ReadConsoleW`

See Windows API documentation for:
- `kernel32.dll!WriteConsoleA` - Write text to console
- `kernel32.dll!ReadConsoleInput` - Read keyboard events
- `kernel32.dll!ReadConsoleA` - Read line of text

### Priority 4: Implement Console.ReadLine (Issue #5)
**Location:** 
- `StandardLibrary/System/System.Console.cs` (declaration)
- `Compiler/Core/MapSet.cs` (code generation)

**Solution:**
1. Add import in WASM module template (line 2055):
   ```wat
   (import "env" "console_readline" (func $console_readline (result i32) (result i32)))
   ```

2. Generate call in WasmEmit.EmitInvocationExpression (around line 590):
   ```csharp
   if (targetStr == "Console.ReadLine" || targetStr.EndsWith(".ReadLine"))
   {
       return ";; Console.ReadLine\ncall $console_readline";
   }
   ```

3. Implement in JavaScript wrapper:
   ```javascript
   console_readline: () => {
       const input = prompt("Enter text:");
       if (!input) return [0, 0];  // null input
       
       // Store string in memory
       const bytes = new TextEncoder().encode(input);
       const ptr = allocateMemory(bytes.length);
       new Uint8Array(imports.env.memory.buffer).set(bytes, ptr);
       
       return [ptr, bytes.length];
   }
   ```

4. For PE: Use `ReadConsoleA` Windows API

---

## Testing Plan

### Test 1: Simple String Literal
```csharp
Console.WriteLine("Hello");
```
**Expected:** Should work (currently works)

### Test 2: String Concatenation (Two Strings)
```csharp
Console.WriteLine("Hello" + " World");
```
**Expected:** Should work after fix
**Currently:** Broken due to Issue #1

### Test 3: String Concatenation with Integer
```csharp
Console.WriteLine("Result: " + 42);
```
**Expected:** Should work after fixes #1 and #2
**Currently:** Broken due to both issues

### Test 4: Complex Expression
```csharp
Console.WriteLine("Math: " + (1 + 2));
```
**Expected:** Should work after fixes #1 and #2
**Currently:** Broken - this is the reported error case

### Test 5: PE Executable
```csharp
Console.WriteLine("Hello");
Console.ReadKey();
```
**Expected:** Should display "Hello" and wait for key press
**Currently:** Closes immediately (Issue #3)

### Test 6: User Input
```csharp
string name = Console.ReadLine();
Console.WriteLine("Hello " + name);
```
**Expected:** Should prompt for input and echo greeting
**Currently:** Needs Issue #5 implementation

---

## Implementation Order

1. **Fix EmitBinaryExpression** (15 minutes)
   - Add early return after string concatenation handling
   - This fixes the critical WASM stack corruption

2. **Implement $int_to_string** (1-2 hours)
   - Either implement in WASM or import from JavaScript
   - Enables string concatenation with integers

3. **Fix PE Console Output** (2-3 hours)
   - Research Windows Console API
   - Implement WriteConsoleA calls
   - Implement ReadConsoleInput for blocking

4. **Implement Console.ReadLine** (1 hour)
   - Add WASM import
   - Add JavaScript implementation
   - Add PE implementation

5. **Test All Examples** (1 hour)
   - Test Calculator
   - Test HelloWorld
   - Test all TestProjects

**Total Estimated Time:** 6-8 hours

---

## Code Locations Reference

| Component | File | Lines |
|-----------|------|-------|
| Binary Expression | `Compiler/Core/MapSet.cs` | 869-966 |
| String Detection | `Compiler/Core/MapSet.cs` | 528-562 |
| WASM Module Template | `Compiler/Core/MapSet.cs` | 2044-2127 |
| int_to_string Stub | `Compiler/Core/MapSet.cs` | 2072-2077 |
| Console.ReadKey Detection | `Compiler/Core/MapSet.cs` | 308-325, 592-599 |
| Console Stubs | `StandardLibrary/System/System.Console.cs` | 1-258 |
| PE Container | `Dependencies/BADGER/Containers/PE.cs` | - |
| WASM Binary Converter | `Dependencies/BADGER/Containers/WasmJS.cs` | 1-800+ |
| Calculator Example | `Testing/TestProjects/Calculator.cs` | 1-26 |

---

## Additional Notes

### String Literal Handling
String literals are stored in the WASM data section correctly (lines 2110-2116). The StringRegistry (lines 8-50) manages this properly. The issue is purely in expression code generation.

### Helper Functions
The WASM module includes:
- `$string_concat` - Placeholder that returns left string only (line 2060-2066)
- `$int_to_string` - Not implemented (line 2072-2077)

Both need proper implementations with heap allocation.

### Memory Management
Currently there's no heap allocator in the generated WASM. String concatenation and int_to_string need a simple bump allocator:
```wat
(global $heap_ptr (mut i32) (i32.const 1000))  ;; Start after data section

(func $malloc (param $size i32) (result i32)
  global.get $heap_ptr
  global.get $heap_ptr
  local.get $size
  i32.add
  global.set $heap_ptr
)
```

This should be added to the WASM module template.

---

## Conclusion

The CRAB compiler is close to working but has critical bugs in expression handling. The primary issue is a missing early return statement in `EmitBinaryExpression` that causes string concatenation code to fall through to integer addition code, corrupting the WASM stack.

Once this is fixed, along with implementing `$int_to_string` and PE Console I/O, all the test cases should work correctly.
