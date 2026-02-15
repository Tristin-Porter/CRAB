# Expression Lowering Fix - Summary

## Task
Fix expression lowering so that `return 5;` generates `(return (i32.const 5))` instead of the Fallback map.

## Changes Made

### 1. Fixed Literal Token Capture ✅

**Files Modified:**
- `Compiler/Core/RuleSet.cs` (lines ~790-792)
- `Compiler/Core/MapSet.cs` (lines ~584-603)

**Changes:**
```csharp
// RuleSet.cs - Simplified literal rules to match single token
public Rule DecimalIntegerLiteral = new Rule("@DecimalIntegerLiteral");
public Rule HexIntegerLiteral = new Rule("@HexIntegerLiteral");
public Rule BinaryIntegerLiteral = new Rule("@BinaryIntegerLiteral");

// MapSet.cs - Use {lexeme} to get token text
public Map DecimalIntegerLiteral = "(i32.const {lexeme})";
public Map HexIntegerLiteral = "(i32.const {lexeme})";
public Map BinaryIntegerLiteral = "(i32.const {lexeme})";
public Map FloatLiteral = "(f32.const {lexeme})";
public Map DoubleLiteral = "(f64.const {lexeme})";
public Map StringLiteral = @";; string ""{lexeme}""
(i32.const {offset})";
public Map CharacterLiteral = "(i32.const {lexeme})";
```

**Impact:** Literal expressions now correctly capture and output their values when used directly.

## Investigation Results

### Problem Identified
The Expression dispatcher chain (Expression → NonAssignmentExpression → ConditionalExpression → ... → Literal → DecimalIntegerLiteral) has a fundamental issue with CDTk's map expansion.

When `ReturnStatement` uses `expr:Expression?`, the field should recursively apply maps through the dispatcher chain. However, CDTk's template expansion triggers the Fallback map instead of properly traversing the chain.

### Attempted Solutions (All Unsuccessful)

1. **Direct Alternations in ReturnStatement**
   - Created alternations for each literal type
   - Failed due to `.Returns()` incompatibility with mixed fields

2. **Split Rules (WithExpression/Void)**
   - Created separate rules for value vs void returns
   - Failed because child maps weren't applied through dispatcher

3. **Helper Rule with Labeled Alternatives**
   - Created ReturnExpr as intermediary  
   - Failed because `{expr}` field expansion triggered Fallback

4. **Pure Dispatcher Helper**
   - Tried unlabeled alternatives
   - Failed completely, literals stopped working

### Root Cause
CDTk's map recursion doesn't reliably expand child nodes when:
- Parent field references a dispatcher rule
- Dispatcher uses labeled alternatives
- Multiple levels of indirection exist

This appears to be a limitation or bug in CDTk's template expansion system.

## Current State

### What Works ✅
- ✅ All existing tests compile successfully
- ✅ Literal maps correctly generate WASM (`(i32.const 5)`, `(i32.const 1)` for true, etc.)
- ✅ Class and method declarations work
- ✅ Void returns work (`return;` → `(return)`)
- ✅ Token lexeme capture via `{lexeme}` works perfectly

### What Doesn't Work ❌
- ❌ `return 5;` generates Fallback instead of `(return (i32.const 5))`
- ❌ Expression dispatcher chain doesn't propagate for map expansion
- ❌ Cannot bypass dispatcher chain with current CDTk syntax

### Current Output for `return 5;`
```wasm
(return ;; TODO: Add map for this construct
nop

)
```

**Note:** The expression value is completely missing because the Expression dispatcher chain doesn't populate the `expr` field.

## Recommended Solutions

### Immediate Action: Model Transformation (Option 2)
Create a CDTk Model that transforms the AST after parsing but before lowering:

```csharp
public class ExpressionFlattener : Model
{
    public override void Transform(Node node)
    {
        if (node.Type == "ReturnStatement" && node.HasChild("expr"))
        {
            // Flatten Expression → ... → Literal chain
            var expr = node.GetChild("expr");
            while (expr.Type is "Expression" or "NonAssignmentExpression" or "ConditionalExpression" /* etc */)
            {
                if (expr.HasChild("expr"))
                    expr = expr.GetChild("expr");
                else
                    break;
            }
            node.ReplaceChild("expr", expr);
        }
    }
}
```

Register in `Program.cs`:
```csharp
var CRAB = new Compiler()
    .WithTokens(new Tokens())
    .WithRules(new Rules())
    .WithModel(new ExpressionFlattener())  // Add this
    .WithTarget(new WASM())
    .Build();
```

### Alternative: Fix CDTk Framework (Option 1 - Ideal but Complex)
The dispatcher chain issue is fundamental to CDTk. Fixing it would require:
1. Understanding CDTk's AST node creation for labeled alternatives
2. Fixing template `{field}` expansion to properly recurse through child maps
3. Testing against all existing CDTk functionality

This is the "correct" solution but requires deep CDTk knowledge and extensive testing.

### Nuclear Option: Flatten Expression Grammar (Option 3 - Not Recommended)
Completely eliminate the dispatcher chain by inlining all expression types into a single massive rule. This would break maintainability and C# language semantics.

## Testing

All test files compile successfully:
```
✓ test_literal.crab
✓ test_method_body.crab
✓ test_multiple_stmts.crab
✓ test_return_expr.crab
✓ test_return_literal.crab
✓ test_return_only.crab
✓ test_statements.crab
✓ test_void_return.crab
✓ final_test.crab
✓ comprehensive_test.crab
```

## Next Steps

1. **Implement Model Transformation** - Most practical solution
   - Create `ExpressionFlattener` model
   - Add to compiler pipeline
   - Test with return statements

2. **Extend to Other Expressions** - Once return works
   - Binary expressions (`a + b`)
   - Unary expressions (`-x`, `!flag`)
   - Method calls
   - Property access

3. **Consider CDTk Fix** - Long term
   - Report issue to CDTk maintainers
   - Propose fix for dispatcher map recursion
   - Contribute patch if feasible

## Files Changed

- `/home/runner/work/CRAB/CRAB/Compiler/Core/RuleSet.cs`
  - Lines ~790-792: Fixed literal rules to use single token

- `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
  - Lines ~584-603: Changed literal maps to use `{lexeme}`

## Documentation Created

- `/home/runner/work/CRAB/CRAB/EXPRESSION_LOWERING_INVESTIGATION.md` - Detailed investigation notes
- `/home/runner/work/CRAB/CRAB/EXPRESSION_LOWERING_SUMMARY.md` - This file

## Conclusion

The literal token capture is now working correctly. The Expression dispatcher chain issue is a deeper CDTk framework problem that requires Model-based AST transformation to resolve. The fix is implemented and ready for testing, but the Model transformation approach is the recommended path forward.

**Status:** ⚠️ PARTIAL SUCCESS - Literals work, dispatcher chain needs Model transformation
