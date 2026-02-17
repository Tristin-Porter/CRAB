# CRAB Compiler Fixes Summary

## Quick Reference

### Issue #1: WASM Stack Corruption (CRITICAL) ❌ **NOT ENOUGH ARGS ON STACK**
- **File:** `Compiler/Core/MapSet.cs`  
- **Function:** `EmitBinaryExpression` (lines 869-966)
- **Bug:** Missing `return` statement after string concatenation handling
- **Fix:** Add `return` at line 953 to prevent fall-through to integer arithmetic code
- **Impact:** Causes "not enough arguments on the stack" error in any expression with string concatenation

### Issue #2: Integer-to-String Not Implemented ❌ **MISSING IMPLEMENTATION**
- **File:** `Compiler/Core/MapSet.cs`
- **Function:** `$int_to_string` WASM helper (lines 2072-2077)
- **Bug:** Returns (0, 0) instead of converting integer to string
- **Fix:** Implement actual integer to string conversion or import from JavaScript
- **Impact:** String concatenation with integers produces empty/invalid strings

### Issue #3: PE Executables Close Immediately ❌ **MISSING WIN32 API CALLS**
- **File:** `Dependencies/BADGER/Containers/PE.cs`
- **Bug:** Console I/O functions not implemented, no blocking wait
- **Fix:** Implement Windows Console API calls (WriteConsoleA, ReadConsoleInput)
- **Impact:** PE executables flash open and close without displaying output

### Issue #4: Console.ReadLine Not Implemented ❌ **STUB ONLY**
- **File:** `StandardLibrary/System/System.Console.cs` + MapSet.cs
- **Bug:** Returns empty string, doesn't actually read input
- **Fix:** Add WASM import, implement in JS and PE backends
- **Impact:** Calculator and any interactive programs can't accept user input

---

## Fix Order (Recommended)

### 1. EMERGENCY FIX: EmitBinaryExpression (5 minutes) 🔥

**Location:** `Compiler/Core/MapSet.cs:953`

**Change:**
```csharp
// Line 953 - ADD RETURN STATEMENT:
return $"{leftStrCode}\n{rightStrCode}\ncall $string_concat  ;; concatenate strings";
```

**Before:**
```csharp
if (op == "+" && (IsStringExpression(left) || IsStringExpression(right)))
{
    // ... string concat code ...
}

// NO RETURN - code continues to line 956! ❌
var leftCode = EmitExpression(left);  // ❌ Executes even for strings!
```

**After:**
```csharp
if (op == "+" && (IsStringExpression(left) || IsStringExpression(right)))
{
    // ... string concat code ...
    return $"{leftStrCode}\n{rightStrCode}\ncall $string_concat";  // ✓ RETURN HERE!
}

// Only reached for non-string arithmetic
var leftCode = EmitExpression(left);
```

This ONE LINE FIX will resolve the stack corruption and make simple string concatenation work.

---

### 2. Implement $int_to_string (30-60 minutes)

Two options:

**Option A: Import from JavaScript (Easier)**
```csharp
// In CompilationUnit template (line 2055), add:
sb.AppendLine("  (import \"env\" \"int_to_string\" (func $int_to_string (param i32) (result i32) (result i32)))");

// In JavaScript wrapper (WasmJS.cs), add to imports:
int_to_string: (value) => {
    const str = value.toString();
    const bytes = new TextEncoder().encode(str);
    const ptr = allocateInMemory(bytes);  // Need to implement allocator
    return [ptr, bytes.length];
}
```

**Option B: Implement in WASM (More complex)**
Requires implementing:
- Heap allocator (global $heap_ptr)
- Integer division loop
- Digit extraction and conversion

Recommend Option A for faster implementation.

---

### 3. Fix PE Console I/O (2-3 hours)

**Location:** `Dependencies/BADGER/Containers/PE.cs`

Research needed:
- Windows Console API documentation
- PE import table generation
- Thunking from WASM-like calls to Win32 API

Key APIs to implement:
```c
// For Console.WriteLine
BOOL WriteConsoleA(
    HANDLE  hConsoleOutput,
    const VOID *lpBuffer,
    DWORD   nNumberOfCharsToWrite,
    LPDWORD lpNumberOfCharsWritten,
    LPVOID  lpReserved
);

// For Console.ReadKey
BOOL ReadConsoleInput(
    HANDLE        hConsoleInput,
    PINPUT_RECORD lpBuffer,
    DWORD         nLength,
    LPDWORD       lpNumberOfEventsRead
);
```

---

### 4. Implement Console.ReadLine (1 hour)

**Location:** `Compiler/Core/MapSet.cs` + `StandardLibrary/System/System.Console.cs`

Steps:
1. Add detection in `EmitInvocationExpression` (similar to WriteLine)
2. Add WASM import declaration
3. Implement in JavaScript wrapper
4. Implement in PE backend (use ReadConsoleA)

---

## Testing After Each Fix

### After Fix #1 (EmitBinaryExpression)
```bash
# Test string concatenation
echo 'using System; class P { static void Main() { Console.WriteLine("A" + "B"); } }' > test.cs
dotnet run compile test.cs --verbose
node test.js  # Should output "AB"
```

### After Fix #2 (int_to_string)  
```bash
# Test string + integer
echo 'using System; class P { static void Main() { Console.WriteLine("Result: " + 42); } }' > test.cs
dotnet run compile test.cs --verbose
node test.js  # Should output "Result: 42"
```

### After Fix #3 (PE Console)
```bash
# Test PE executable
dotnet run compile test.cs --to-asm --format pe --verbose
./output.exe  # Should display output and not close immediately
```

### After Fix #4 (ReadLine)
```bash
# Test interactive input
cat > test.cs << 'EOF'
using System;
class Program {
    static void Main() {
        Console.Write("Name: ");
        string name = Console.ReadLine();
        Console.WriteLine("Hello " + name);
    }
}
EOF
dotnet run compile test.cs --verbose
```

---

## Files to Modify

| Priority | File | Function/Line | Estimated Time |
|----------|------|---------------|----------------|
| 1 🔥 | `Compiler/Core/MapSet.cs` | Line 953 - Add return | 5 min |
| 2 | `Compiler/Core/MapSet.cs` | Lines 2072-2077 - Implement int_to_string | 30-60 min |
| 2 | `Dependencies/BADGER/Containers/WasmJS.cs` | Add JS int_to_string import | 15 min |
| 3 | `Dependencies/BADGER/Containers/PE.cs` | Implement Win32 Console APIs | 2-3 hours |
| 4 | `Compiler/Core/MapSet.cs` | Lines 590-600 - Add ReadLine detection | 15 min |
| 4 | `Dependencies/BADGER/Containers/WasmJS.cs` | Add JS console_readline | 30 min |
| 4 | `Dependencies/BADGER/Containers/PE.cs` | Implement ReadConsoleA | 30 min |

**Total estimated time:** 4-6 hours

---

## Verification Checklist

After all fixes are applied:

- [ ] Simple string literal works: `Console.WriteLine("Hello")`
- [ ] String concatenation works: `Console.WriteLine("A" + "B")`  
- [ ] String + integer works: `Console.WriteLine("Result: " + 42)`
- [ ] Complex expression works: `Console.WriteLine("Math: " + (1+2))`
- [ ] Original test case works: `Console.WriteLine("Hello!" + "\n" + "Math: " + (1+2))`
- [ ] Calculator example compiles and runs
- [ ] PE executable displays output and waits for key press
- [ ] Console.ReadLine accepts and returns user input
- [ ] All TestProjects compile without errors

---

## Critical Understanding

The root cause of Issue #1 is **NOT** in the string detection logic. The detection works fine. The bug is that after detecting and emitting string concatenation code, the function **doesn't return** and continues to emit integer arithmetic code.

This is why the WAT output shows:
```wat
i32.const 0    ; string ptr
i32.const 12   ; string len  
i32.add        ; ❌ This shouldn't be here!
```

The `i32.add` comes from lines 956-965 which should never execute for strings.

**The fix is literally adding one word: `return`**

---

## Additional Resources

- [CRAB Specification](/.github/agents/crab-spec.txt)
- [WebAssembly Specification](https://webassembly.github.io/spec/)
- [Windows Console API Documentation](https://docs.microsoft.com/en-us/windows/console/console-functions)
- [WASM Binary Format](https://webassembly.github.io/spec/core/binary/index.html)

---

## Contact Points

If you need help implementing these fixes:

1. **EmitBinaryExpression fix**: Trivial, just add return statement
2. **int_to_string**: Consider importing from JavaScript first (easier)
3. **PE Console I/O**: Most complex, may need Win32 API expertise
4. **ReadLine**: Similar pattern to WriteLine, should be straightforward

Good luck! The compiler is very close to working correctly. 🎯
