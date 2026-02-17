# CRAB Compiler: WASM String Handling Fixes

## Summary

This change fixes the critical WASM stack error "not enough arguments on the stack for call" by correcting how string literals are emitted in WASM code generation.

## Main Issue Fixed

**Problem**: When compiling `Console.WriteLine("Hello World!")`, the generated WASM only pushed the string pointer onto the stack, but `console_log` expected both pointer and length parameters. This caused a stack underflow error at runtime.

**Root Cause**: The `EmitExpressionDispatcher` function at line 347 was only emitting `i32.const {offset}` for string literals, missing the length.

**Solution**: Updated string literal emission to push both offset and length:
```csharp
// Before
return $";; string \"{text}\" at offset {offset}\ni32.const {offset}";

// After  
return $";; string \"{text}\" at offset {offset}, length {length}\ni32.const {offset}\ni32.const {length}";
```

Also corrected the `console_log` import signature:
```wasm
// Before
(import "env" "console_log" (func $console_log (param i32)))

// After
(import "env" "console_log" (func $console_log (param i32) (param i32)))  ;; ptr, len
```

## Additional Improvements

### Console.ReadKey Support
- Added `console_readkey` import for PE executables to wait for user input
- Implemented detection logic for Console.ReadKey method calls
- Note: Call emission in statement context needs additional work

### String Concatenation Infrastructure
- Added `IsStringExpression()` helper to detect string types
- Enhanced `EmitBinaryExpression()` to handle string concatenation
- Added placeholder helper functions `$string_concat` and `$int_to_string`
- Added `EmitArgumentExpression()` to handle CDTk field shifting

## Testing

✅ Simple Console.WriteLine works correctly
✅ Multiple WriteLine calls work correctly  
✅ String literals emit both pointer and length
✅ Import signatures are correct
✅ No security vulnerabilities (CodeQL: 0 alerts)

## Files Changed

- `Compiler/Core/MapSet.cs` (~370 lines modified)

## Known Limitations

- String concatenation expressions need more CDTk AST handling
- Namespace member generation has separate issues
- Console.ReadKey call emission incomplete (import added)
- Helper functions are placeholders pending proper implementation

## Verification

Test case that now works:
```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello World!");
    }
}
```

Generated WASM:
```wasm
;; Console.WriteLine
;; string "Hello World!" at offset 0, length 12
i32.const 0
i32.const 12
call $console_log
```

See INVESTIGATION_SUMMARY.md and FINAL_SUMMARY.md for complete details.
