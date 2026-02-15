# Expression Lowering - Current State & Next Steps

## Quick Summary

**Goal:** Fix `return 5;` to generate `(return (i32.const 5))` instead of Fallback

**Status:** ⚠️ PARTIAL - Literal maps work, but Expression dispatcher chain blocks proper lowering

**What Changed:**
- ✅ Fixed literal token capture (DecimalIntegerLiteral, HexIntegerLiteral, etc.)
- ✅ All literals now use `{lexeme}` to get token text
- ✅ All tests compile successfully
- ❌ Return statements still use Fallback (Expression dispatcher chain issue)

## Problem

The Expression dispatcher chain doesn't propagate fields for CDTk map expansion:
```
Expression → NonAssignmentExpression → ConditionalExpression → ... → Literal → DecimalIntegerLiteral
```

When `ReturnStatement` uses `expr:Expression?`, CDTk's template system can't recursively apply maps through this chain.

## What Works Now

### Literal Maps ✅
```csharp
// RuleSet.cs
public Rule DecimalIntegerLiteral = new Rule("@DecimalIntegerLiteral");

// MapSet.cs  
public Map DecimalIntegerLiteral = "(i32.const {lexeme})";
```

**Result:** When DecimalIntegerLiteral is used *directly*, it generates `(i32.const 5)` correctly.

### Test Results ✅
All 10 test files compile without errors:
- `test_literal.crab`
- `test_method_body.crab`
- `test_multiple_stmts.crab`
- `test_return_expr.crab`
- `test_return_literal.crab`
- `test_return_only.crab`
- `test_statements.crab`
- `test_void_return.crab`
- `final_test.crab`
- `comprehensive_test.crab`

## What Doesn't Work

### Return Statements ❌
```csharp
int GetFive() {
    return 5;  
}
```

**Current Output:**
```wasm
(return ;; TODO: Add map for this construct
nop

)
```

**Expected:**
```wasm
(return (i32.const 5))
```

The literal value completely disappears because the Expression dispatcher chain doesn't populate the `expr` field.

## Why Bypass Attempts Failed

### Attempt 1: Direct Alternations
```csharp
public Rule ReturnStatement = new Rule(
    "@KwReturn expr:DecimalIntegerLiteral @Semicolon | ... | @KwReturn @Semicolon"
).Returns("expr");
```
**Problem:** void return (`@KwReturn @Semicolon`) has no `expr` field, conflicts with `.Returns("expr")`

### Attempt 2: Split Rules  
```csharp
public Rule ReturnStatement = "stmt:ReturnStatementWithExpression | stmt:ReturnStatementVoid";
```
**Problem:** Child maps not applied when expanding `{stmt}` field

### Attempt 3: Helper Rule
```csharp
public Rule ReturnExpr = new Rule("expr:DecimalIntegerLiteral | ...").Returns("expr");
```
**Problem:** `{expr}` expansion triggers Fallback instead of applying ReturnExpr map

### Root Cause
CDTk's map recursion doesn't work when:
- Parent field references a dispatcher rule
- Dispatcher uses labeled alternatives  
- Multiple indirection levels exist

This appears to be a CDTk framework limitation.

## Recommended Solution: Model Transformation

Create an AST transformer that flattens the Expression chain *before* lowering:

```csharp
using CDTk;

public class ExpressionFlattener : Model
{
    public override Node Transform(Node node)
    {
        // For ReturnStatement nodes with expr field
        if (node.Type == "ReturnStatement" && node.HasField("expr"))
        {
            var expr = node.GetField("expr");
            
            // Follow the dispatcher chain: Expression → NonAssignmentExpression → ...
            while (expr != null && IsDispatcher(expr.Type))
            {
                if (expr.HasField("expr"))
                    expr = expr.GetField("expr");
                else
                    break;
            }
            
            // Replace multi-level chain with direct literal reference
            node.SetField("expr", expr);
        }
        
        return node;
    }
    
    private bool IsDispatcher(string type)
    {
        return type is "Expression" or "NonAssignmentExpression" or 
               "ConditionalExpression" or "NullCoalescingExpression" or
               "LogicalOrExpression" or "LogicalAndExpression" or
               "BitwiseOrExpression" or "BitwiseXorExpression" or
               "BitwiseAndExpression" or "EqualityExpression" or
               "RelationalExpression" or "ShiftExpression" or
               "AdditiveExpression" or "MultiplicativeExpression" or
               "SwitchExpression" or "RangeExpression" or
               "UnaryExpression" or "PrimaryExpressionCore";
    }
}
```

**Register in Program.cs:**
```csharp
var CRAB = new Compiler()
    .WithTokens(new Tokens())
    .WithRules(new Rules())
    .WithModel(new ExpressionFlattener())  // ← Add this
    .WithTarget(new WASM())
    .Build();
```

**How It Works:**
1. After parsing, before lowering, traverse AST
2. Find ReturnStatement nodes with expr fields
3. Follow the Expression → NonAssignmentExpression → ... chain
4. Replace the chain with the final literal node
5. Now `{expr}` in the map directly references the literal
6. Literal map is applied correctly → `(i32.const 5)`

## Implementation Steps

1. **Create Model File**
   ```bash
   touch Compiler/Models/ExpressionFlattener.cs
   ```

2. **Implement Transformation**
   - Copy code above
   - Test with CDTk's Node API (may need adjustment)

3. **Register Model**
   - Edit `Program.cs`
   - Add `.WithModel(new ExpressionFlattener())`

4. **Test**
   ```bash
   dotnet build
   ./bin/Debug/net10.0/CRAB compile test_final_demo.crab
   cat output.wasm  # Should see (return (i32.const 5))
   ```

5. **Extend**
   - Binary expressions (`a + b`)
   - Unary expressions (`-x`)
   - Method calls
   - All other expression contexts

## Alternative: Fix CDTk (Long Term)

The "correct" solution is fixing CDTk's template expansion to properly recurse through dispatcher chains. This requires:
1. Deep CDTk knowledge
2. Understanding AST construction for labeled alternatives
3. Fixing `{field}` expansion in template system
4. Extensive testing

Consider contributing this fix back to CDTk project.

## Files Modified

### Compiler/Core/RuleSet.cs
```diff
+// Return statement - uses Expression which goes through dispatcher chain
+// KNOWN ISSUE: Expression dispatcher chain doesn't properly populate expr field
+// causing Fallback map to be used instead of proper lowering
 public Rule ReturnStatement = new Rule("@KwReturn expr:Expression? @Semicolon")
     .Returns("expr");

 // Simplified - no value: label needed
 public Rule DecimalIntegerLiteral = new Rule("@DecimalIntegerLiteral");
 public Rule HexIntegerLiteral = new Rule("@HexIntegerLiteral");
 public Rule BinaryIntegerLiteral = new Rule("@BinaryIntegerLiteral");
```

### Compiler/Core/MapSet.cs
```diff
 // Use {lexeme} to get token text
-public Map DecimalIntegerLiteral = "(i32.const {value})";
+public Map DecimalIntegerLiteral = "(i32.const {lexeme})";

-public Map HexIntegerLiteral = "(i32.const {value})";
+public Map HexIntegerLiteral = "(i32.const {lexeme})";

-public Map BinaryIntegerLiteral = "(i32.const {value})";
+public Map BinaryIntegerLiteral = "(i32.const {lexeme})";

-public Map FloatLiteral = "(f32.const {value})";
+public Map FloatLiteral = "(f32.const {lexeme})";

-public Map CharacterLiteral = "(i32.const {value})";
+public Map CharacterLiteral = "(i32.const {lexeme})";

-public Map StringLiteral = @";; string ""{value}""...";
+public Map StringLiteral = @";; string ""{lexeme}""...";
```

## Documentation

- `EXPRESSION_LOWERING_INVESTIGATION.md` - Detailed investigation with all attempts
- `EXPRESSION_LOWERING_SUMMARY.md` - High-level summary of changes
- `EXPRESSION_LOWERING_README.md` - This file

## Conclusion

The literal capture fix is solid and ready. The Expression dispatcher issue requires Model-based AST transformation. Implementation is straightforward once CDTk's Node API is understood. This is the recommended path forward.

**Next Action:** Implement ExpressionFlattener Model and test with return statements.
