# Statement Parsing Bug - Root Cause and Fix

## Problem Statement

Despite expression parsing being "fixed" with correct SPPF node extents (`Expression[9..10]` instead of `Expression[9..9]`), statement parsing still failed completely:

- ✅ Expression nodes created with correct extents
- ❌ NO statement nodes created (no `ReturnStatement`, `Block`, `Statement`, etc.)
- ❌ Error: "GLL: No SPPF node found for 'CompilationUnit'"

## Root Cause

**The CDTk GLL parser didn't update the input position after nonterminal completion.**

### Detailed Explanation

When parsing `return 5;` (tokens at positions [8, 9, 10]):

1. `ReturnStatement` rule: `@KwReturn expr:Expression? @Semicolon`
2. Parser at position 8 matches `@KwReturn` → advances to position 9 ✓
3. Parser calls `Expression` nonterminal at position 9
4. `Expression` matches literal "5", creates `Expression[9..10]` ✓
5. `Expression` completes, calls `Pop(Expression[9..10])`
6. **BUG**: `Pop()` creates continuation descriptor with `_currentPosition = 9` (unchanged!)
7. Continuation tries to match `@Semicolon` at position 9
8. Position 9 contains the literal "5", not the semicolon → **FAIL**

### Why Terminals Worked but Nonterminals Didn't

**Terminals** updated `_currentPosition` after matching:
```csharp
_currentPosition++;  // Advances after consuming token
```

**Nonterminals** did NOT update position before creating continuations:
```csharp
AddDescriptor(new Descriptor(..., _currentPosition, ...));  // Still at start position!
```

### Why the Previous Fix Wasn't Enough

Commit 1f1fdc4 fixed SPPF node **extents**:
```csharp
var endExtent = _currentSPPFNode != null 
    ? _currentSPPFNode.RightExtent  // Correct extent
    : _currentPosition;
```

This made `Expression[9..10]` have the right extent, but didn't fix **parser position tracking**. The parser still thought it was at position 9 after the expression, not position 10.

## The Fix

**File**: `Dependencies/CDTk/Boilerplate/CDTk.cs`  
**Function**: `Pop(SPPFNode? sppfNode)`  
**Change**: Update continuation position to nonterminal's `RightExtent`

### Code

```csharp
private void Pop(SPPFNode? sppfNode)
{
    if (_currentGSSNode == null) return;

    int continuationPosition = _currentPosition;  // Start with current
    
    if (sppfNode != null && sppfNode is SPPFSymbolNode symbolNode)
    {
        // ... store SPPF node ...
        
        // THE FIX: Update position to where nonterminal ended
        continuationPosition = endPosition;
    }

    foreach (var edge in _currentGSSNode.Edges)
    {
        AddDescriptor(new Descriptor(
            GetLabelFromString(edge.Target.Label),
            edge.Target,
            continuationPosition,  // ← Use the updated position!
            combinedSPPF));
    }
}
```

### How It Works

After the fix:

1. Parser at position 8 matches `@KwReturn` → advances to 9
2. Calls `Expression` nonterminal at position 9  
3. `Expression` matches literal, creates `Expression[9..10]`
4. `Expression` completes, calls `Pop(Expression[9..10])`
5. **FIX**: `continuationPosition = 10` (from `RightExtent`)
6. Continuation descriptor created at position 10
7. Matches `@Semicolon` at position 10 → **SUCCESS!**
8. `ReturnStatement[8..11]` created, parse succeeds

## Verification

All test cases now pass:

```bash
✅ return 5;              # Return statement with expression
✅ int x = 5;             # Variable declaration with initializer
✅ int field = 42;        # Field with initializer
✅ 5;                     # Expression statement
✅ Multiple statements    # Complex blocks
```

Before the fix, ALL of these failed.

## Summary

**Question 1: Why weren't statement nodes being created when expression parsing is fixed?**

Expression parsing created SPPF nodes with correct **extents**, but the parser's **position tracking** was broken. After parsing an expression, the parser didn't advance its position, so subsequent terminals (like semicolons) tried to match at the wrong position and failed.

**Question 2: Are terminals matching?**

Yes, terminals match fine when they're at the correct position. The bug prevented the parser from reaching the correct position after nonterminals.

**Question 3: Is there another bug in CDTk or the grammar?**

Yes - a bug in CDTk's `Pop()` function. The grammar was correct. The previous "fix" only addressed SPPF extent calculation, not position tracking.

**Question 4: What specific fix is needed?**

Update `Pop()` in CDTk to set continuation descriptors' input position to the completed nonterminal's `RightExtent` instead of using the unchanged `_currentPosition`.

## Files Modified

- `Dependencies/CDTk/Boilerplate/CDTk.cs` - Fixed `Pop()` function (line ~11964)

## Documentation

- `STATEMENT_PARSING_FIX.md` - Comprehensive analysis with test results and execution traces
- `STATEMENT_PARSING_INVESTIGATION.md` - Earlier investigation of expression parsing issues

## Result

**Statement parsing is now fully functional.** The CRAB compiler can parse:
- Return statements with expressions
- Variable declarations with initializers  
- Field declarations with initializers
- Expression statements
- Complex method bodies with multiple statements

This completes the CDTk GLL parser fixes for C# parsing.
