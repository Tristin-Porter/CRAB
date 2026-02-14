# CDTk Statement Parsing Bug - Root Cause Analysis and Fix

## Summary

**Problem**: Statement parsing failed even though expression parsing was "fixed" - expression SPPF nodes were created with correct extents, but no statement nodes were created, resulting in parse failure.

**Root Cause**: CDTk's GLL parser didn't update the input position after nonterminal completion, causing continuation descriptors to parse from the wrong position.

**Fix**: Update the `Pop()` function to set continuation position to the completed nonterminal's `RightExtent`.

## Investigation

### Symptoms

When parsing `class A { int Get() { return 5; } }`:

✅ **Working**:
- Expression nodes created: `Expression[9..10]`, `Literal[9..10]`, etc.
- All expression hierarchy nodes present with correct extents
- Expressions without semicolons worked (e.g., field without initializer)

❌ **Failing**:
- NO statement nodes: `ReturnStatement`, `Block`, `Statement`, `Statements`
- NO method body parsing
- Error: "GLL: No SPPF node found for 'CompilationUnit'"

### Test Results

| Test Case | Result | Description |
|-----------|--------|-------------|
| `return;` | ✅ Pass | Return without expression |
| `return 5;` | ❌ Fail | Return with expression |
| `int x;` | ✅ Pass | Declaration without initializer |
| `int x = 5;` | ❌ Fail | Declaration with initializer |
| `break;` | ✅ Pass | Simple keyword + semicolon |
| `5;` | ❌ Fail | Expression statement |

**Pattern**: Any construct with **expression followed by semicolon** failed.

### SPPF Analysis

For `return 5;` at token positions [8, 9, 10]:

**Nodes Created**:
```
Expression[9..10]               ✓ Expression parsed correctly
Literal[9..10]                  ✓ Literal parsed correctly
_seq[8..10]                     ✓ Sequence "return 5" created
FormalParameterList[6..6]       ✓ Parameter list parsed
Type[3..4]                      ✓ Return type parsed
```

**Nodes Missing**:
```
ReturnStatement                 ✗ Never created
Block                           ✗ Never created
Statement                       ✗ Never created
MethodDeclaration               ✗ Never created
```

The sequence `_seq[8..10]` proved the parser matched `@KwReturn` and `Expression` successfully, but failed to match the subsequent `@Semicolon`.

### Root Cause

Tracing through the GLL parser execution:

1. **ReturnStatement** rule: `@KwReturn expr:Expression? @Semicolon`
2. Parser at position 8 matches `@KwReturn` → advances to position 9
3. Parser calls **Expression** nonterminal at position 9
4. Expression matches literal "5", creates `Expression[9..10]` (covers token 9)
5. Expression completes, calls `Pop(Expression[9..10])`
6. **BUG**: `Pop()` creates continuation descriptor with `_currentPosition = 9` (unchanged!)
7. Continuation tries to match `@Semicolon` at position 9
8. Position 9 contains literal "5", not semicolon → **match fails**
9. Entire parse fails

### Why Terminals Worked but Nonterminals Didn't

**Terminals** (in `ProcessTerminal()`):
```csharp
if (matches)
{
    _currentPosition++;  // ← Position updated!
    AdvanceToNextSlot(ruleName, slot);
}
```

**Nonterminals** (in `Pop()`):
```csharp
AddDescriptor(new Descriptor(
    GetLabelFromString(edge.Target.Label),
    edge.Target,
    _currentPosition,  // ← Position NOT updated!
    combinedSPPF));
```

Terminals updated `_currentPosition` after matching. Nonterminals did NOT.

### Previous Fix Context

Commit 1f1fdc4 ("Fix CDTk expression parsing bug") addressed SPPF node extent calculation:

```csharp
// Fixed SPPF node extent
var endExtent = _currentSPPFNode != null 
    ? _currentSPPFNode.RightExtent  // Use child's end
    : _currentPosition;
```

This ensured `Expression[9..10]` was created with correct extent instead of empty `Expression[9..9]`. However, it didn't fix parser position tracking, so while SPPF nodes had correct extents, the parser still parsed from wrong positions.

## The Fix

### Code Change

**File**: `Dependencies/CDTk/Boilerplate/CDTk.cs`  
**Function**: `Pop(SPPFNode? sppfNode)`  
**Line**: ~11964

```csharp
private void Pop(SPPFNode? sppfNode)
{
    if (_currentGSSNode == null) return;

    // Determine the position where parsing should continue after this nonterminal
    int continuationPosition = _currentPosition;
    
    if (sppfNode != null && sppfNode is SPPFSymbolNode symbolNode)
    {
        var startPosition = sppfNode.LeftExtent;
        var endPosition = sppfNode.RightExtent;
        var key = (symbolNode.Symbol, startPosition, endPosition);
        if (!_sppfNodes.ContainsKey(key))
        {
            _sppfNodes[key] = sppfNode;
        }
        
        // CRITICAL FIX for statement parsing bug:
        // Update continuation position to where the nonterminal ended
        continuationPosition = endPosition;  // ← THE FIX
    }

    foreach (var edge in _currentGSSNode.Edges)
    {
        var combinedSPPF = CombineSPPFNodes(edge.SPPFNode, sppfNode);
        
        AddDescriptor(new Descriptor(
            GetLabelFromString(edge.Target.Label),
            edge.Target,
            continuationPosition,  // ← Use updated position
            combinedSPPF));
    }
}
```

### How It Works

After fix, the execution trace becomes:

1. Parser at position 8 matches `@KwReturn` → advances to position 9
2. Calls Expression nonterminal at position 9
3. Expression matches literal "5", creates `Expression[9..10]`
4. Expression completes, calls `Pop(Expression[9..10])`
5. **FIX**: `continuationPosition = sppfNode.RightExtent = 10`
6. Continuation descriptor created at position 10
7. Continuation matches `@Semicolon` at position 10 → **succeeds!**
8. `ReturnStatement[8..11]` SPPF node created
9. Parse succeeds!

## Verification

After applying the fix, all test cases pass:

```bash
✅ return 5;                 # Return with expression
✅ int x = 5;                # Local variable with initializer
✅ int field = 42;           # Field with initializer
✅ 5;                        # Expression statement
✅ int Get() { return 5; }   # Method with return expression
✅ Multiple statements       # Complex scenarios
```

## Impact

This fix completes the CDTk GLL parser implementation for C# parsing:

**Before** (with extent fix only):
- ✅ Expression SPPF nodes created with correct extents
- ❌ Statement parsing completely broken
- ❌ Can't parse any real C# code

**After** (with both fixes):
- ✅ Expression SPPF nodes created with correct extents
- ✅ Statement parsing works
- ✅ Full C# method parsing functional
- ✅ Can parse real C# programs

## Key Learnings

1. **SPPF node extents ≠ parser position**:  
   Fixing SPPF extents is necessary but not sufficient. Parser position must track input consumption.

2. **Terminals vs. Nonterminals**:  
   In GLL parsers, terminals and nonterminals must handle position updates differently due to the descriptor-based execution model.

3. **Continuation descriptors**:  
   When creating continuation descriptors after nonterminal completion, the position must reflect where the nonterminal's parse ended, not where it started.

4. **Debug strategy**:  
   The SPPF node dump in error messages was critical for identifying that expression nodes existed but statement nodes didn't, revealing the position tracking bug.

## Files Modified

- `Dependencies/CDTk/Boilerplate/CDTk.cs` - Fixed `Pop()` function

## References

- Previous fix: Commit 1f1fdc4 "Fix CDTk expression parsing bug"
- Investigation: `STATEMENT_PARSING_INVESTIGATION.md`
- GLL algorithm: Scott & Johnstone (2010)
- CDTk spec: Architecture design documents
