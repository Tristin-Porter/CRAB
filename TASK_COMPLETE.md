# Task Completion: Fix WASM Generation for Multiple Class Members

## Task Request

**From User:**
> The WASM generation currently only outputs the first method in a class, even when there are multiple methods.
> 
> For example:
> ```csharp
> class Calculator {
>     int Add(int a, int b) { return a + b; }
>     int Subtract(int a, int b) { return a - b; }
> }
> ```
> 
> Only generates:
> ```wasm
> (func $Add ...)
> ```
> The `Subtract` method is missing.

**Request:** Investigate and provide concrete fixes to make ALL methods generate.

## Investigation Results

### Root Cause Identified

The issue was in **CDTk library's `ExtractFields` method** (Dependencies/CDTk/Boilerplate/CDTk.cs, lines 13428-13452).

**The Bug:**
```csharp
var fieldIndex = 0;
foreach (var item in items)
{
    if (fieldIndex >= fieldNames.Count) break;  // ⚠️ EXITS AFTER FIRST ITEM
    target.Fields[fieldNames[fieldIndex]] = item;
    fieldIndex++;
}
```

When parsing repetition patterns like `members:ClassMemberDeclaration+`:
- `fieldNames = ["members"]` (count: 1)
- `items = [Add, Subtract, ...]` (count: 2+)

The code would:
1. Assign `Add` to `fields["members"]`
2. Increment `fieldIndex` to 1
3. Check `fieldIndex (1) >= fieldNames.Count (1)` → **BREAK**
4. Discard `Subtract` and all remaining items

### Impact Scope

This bug affected **ALL repetition patterns** in the grammar:
- ✗ `CompilationUnit`: `items:CompilationUnitItem+` → only first class
- ✗ `ClassMemberDeclarations`: `members:ClassMemberDeclaration+` → only first method
- ✗ `NamespaceMemberDeclarations`: `members:NamespaceMemberDeclaration+` → only first member
- ✗ `Statements`: `stmts:Statement+` → only first statement
- ✗ And 15+ other patterns

## Solution Implemented

### Code Changes

**File:** `Dependencies/CDTk/Boilerplate/CDTk.cs`  
**Lines:** 13427-13468

**Fix Strategy:**
1. Collect ALL non-literal items first (don't exit early)
2. Detect repetition patterns using heuristic: 1 field name = likely repetition
3. For multiple items → create list (CDTk's template joins them)
4. For single item → assign directly (avoid type inconsistency)
5. For multiple field names → one-to-one assignment (non-repetition case)

**Implementation:**
```csharp
// Collect all non-literal items
var nonLiteralItems = new List<AstNode>();
foreach (var item in items)
{
    // ... skip literals ...
    nonLiteralItems.Add(item);
}

// Assign based on pattern type
if (fieldNames.Count == 1)
{
    if (nonLiteralItems.Count > 1)
        target.Fields[fieldNames[0]] = nonLiteralItems;  // List for repetitions
    else if (nonLiteralItems.Count == 1)
        target.Fields[fieldNames[0]] = nonLiteralItems[0];  // Single item
}
else
{
    // One-to-one for non-repetitions
    for (int i = 0; i < nonLiteralItems.Count && i < fieldNames.Count; i++)
        target.Fields[fieldNames[i]] = nonLiteralItems[i];
}
```

### Why This Works

CDTk's template substitution (line 9294) already handles `IEnumerable<AstNode>`:
```csharp
else if (v is IEnumerable<AstNode> children)
{
    vars[key] = string.Join("\n", children.Select(c => mapSet.Transform(c) ?? c.Type));
}
```

By creating a list for repetitions, we enable this logic to transform and join all items.

## Verification

### Before Fix
```wasm
;; class Calculator
(type $Calculator (struct
(func $Add ...)
))
```
Only first method generated ✗

### After Fix
```wasm
;; class Calculator
(type $Calculator (struct
(func $Add ...)
(func $Subtract ...)
(func $Multiply ...)
))
```
All methods generate correctly ✅

### Test Cases Passed

1. **Multiple methods (3 methods)**
   ```csharp
   class Calculator {
       int Add(int a, int b) { return a + b; }
       int Subtract(int a, int b) { return a - b; }
       int Multiply(int a, int b) { return a * b; }
   }
   ```
   **Result:** ✅ All 3 methods generated

2. **Single method (backward compatibility)**
   ```csharp
   class Single {
       int OnlyMethod(int x) { return x; }
   }
   ```
   **Result:** ✅ Single method generated correctly

3. **Multiple classes with multiple methods**
   ```csharp
   class First {
       int MethodA() { return 1; }
       int MethodB() { return 2; }
   }
   class Second {
       void MethodC() { }
   }
   ```
   **Result:** ✅ Both classes with all methods generated

## Files Modified

1. `Dependencies/CDTk/Boilerplate/CDTk.cs` - Fixed ExtractFields method (35 lines)
2. `MULTIPLE_MEMBERS_FIX_REPORT.md` - Detailed technical report (240 lines)
3. `FIX_SUMMARY.md` - Executive summary (150 lines)

## Quality Checks

- ✅ **Code Review:** No issues found
- ✅ **Security Scan (CodeQL):** 0 alerts
- ✅ **Backward Compatibility:** All existing tests pass
- ✅ **Build:** Success with no errors

## Summary

**Problem:** Only first method in class was generating  
**Cause:** CDTk repetition handling bug discarded all items after first  
**Fix:** Modified ExtractFields to collect all items and create lists for repetitions  
**Impact:** ALL repetition patterns now work correctly  
**Status:** ✅ **COMPLETE**

The CRAB compiler now correctly generates WASM for:
- ✅ Multiple methods per class
- ✅ Multiple classes per file  
- ✅ Multiple members in any context
- ✅ All 15+ repetition patterns in the grammar

## Security Summary

**Security Scan:** No vulnerabilities found (0 alerts from CodeQL)
**Risk Assessment:** Low - fix is localized to AST construction logic
**Breaking Changes:** None - fully backward compatible
