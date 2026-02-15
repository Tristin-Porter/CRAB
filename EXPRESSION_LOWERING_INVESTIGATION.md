# Expression Lowering Investigation - Return Statement Fix Attempt

## Goal
Fix expression lowering so that `return 5;` generates `(return (i32.const 5))` instead of the Fallback map.

## Problem Analysis

### Root Cause
The Expression dispatcher chain (Expression → NonAssignmentExpression → ConditionalExpression → ... → Literal → DecimalIntegerLiteral) doesn't properly populate the `expr` field when used in `ReturnStatement`.

**Current Code:**
```csharp
public Rule ReturnStatement = new Rule("@KwReturn expr:Expression? @Semicolon")
    .Returns("expr");
public Map ReturnStatement = "(return {expr})";
```

**Current Output for `return 5;`:**
```wasm
(return ;; TODO: Add map for this construct
nop

)
```

The `expr` field is not being populated through the dispatcher chain, causing CDTk to use the Fallback map instead of the ReturnStatement map.

## Fixes Implemented

### Fix 1: Literal Rules - ✅ WORKING
**Problem:** Literal rules didn't capture token values properly.

**Solution:**
```csharp
// Changed from: public Rule DecimalIntegerLiteral = new Rule("value:@DecimalIntegerLiteral").Returns("value");
public Rule DecimalIntegerLiteral = new Rule("@DecimalIntegerLiteral");

// Map uses {lexeme} to get token text
public Map DecimalIntegerLiteral = "(i32.const {lexeme})";
```

**Result:** When DecimalIntegerLiteral is used directly (not through Expression chain), it correctly generates `(i32.const 5)`.

**Also fixed:** HexIntegerLiteral, BinaryIntegerLiteral with same pattern.

### Fix 2: Bypass Expression Dispatcher - ❌ NOT WORKING
**Attempted Solutions:**

#### Attempt A: Direct Alternations in ReturnStatement
```csharp
public Rule ReturnStatement = new Rule(
    "@KwReturn expr:DecimalIntegerLiteral @Semicolon " +
    "| @KwReturn expr:HexIntegerLiteral @Semicolon " +
    "| ... more alternations ..."
).Returns("expr");
```

**Problem:** Without `.Returns("expr")`, fields aren't populated. With `.Returns("expr")`, the void return case (`@KwReturn @Semicolon`) fails because it doesn't have an `expr` field.

#### Attempt B: Split Rules (WithExpression / Void)
```csharp
public Rule ReturnStatement = "stmt:ReturnStatementWithExpression | stmt:ReturnStatementVoid";
public Rule ReturnStatementWithExpression = new Rule("@KwReturn expr:DecimalIntegerLiteral @Semicolon | ...").Returns("expr");
public Rule ReturnStatementVoid = new Rule("@KwReturn @Semicolon");
```

**Problem:** The child maps (ReturnStatementWithExpression, ReturnStatementVoid) were not being applied when expanded through `{stmt}`. CDTk couldn't find or apply the child maps.

#### Attempt C: Helper Rule with Labeled Alternatives
```csharp
public Rule ReturnStatement = new Rule("@KwReturn expr:ReturnExpr? @Semicolon").Returns("expr");
public Rule ReturnExpr = new Rule("expr:DecimalIntegerLiteral | expr:HexIntegerLiteral | ...").Returns("expr");
public Map ReturnExpr = "{expr}";
```

**Problem:** When ReturnStatement map tries to expand `{expr}` (which contains a ReturnExpr node), CDTk triggers the Fallback instead of applying the ReturnExpr map. Recursive map application doesn't work as expected.

#### Attempt D: Helper Rule as Pure Dispatcher
```csharp
public Rule ReturnExpr = "DecimalIntegerLiteral | HexIntegerLiteral | ...";  // No labels
```

**Problem:** Without labels, ReturnExpr becomes a pure dispatcher and doesn't create an AST node. But this caused even worse results - literals stopped working entirely.

## Key Discoveries

### 1. CDTk Map Recursion Issue
When a Rule field is expanded in a Map using `{fieldName}`, CDTk should recursively apply the child node's map. However, this doesn't work reliably when:
- The child node is created by a dispatcher rule with labeled alternatives
- The parent uses the child through a field reference

**Example that FAILS:**
```csharp
public Rule Parent = new Rule("child:HelperRule").Returns("child");
public Rule HelperRule = new Rule("value:SomeType").Returns("value");
public Map Parent = "{child}";  // Triggers Fallback!
public Map HelperRule = "{value}";
```

### 2. `.Returns()` is Required for Fields
String syntax with labels like `"expr:Type"` creates a labeled parse node, but without `.Returns("expr")`, the field won't exist in the AST node that can be referenced in Maps.

### 3. Alternations with Mixed Fields Don't Work
When a Rule has alternations where some have a field and some don't:
```csharp
new Rule("@KwReturn expr:Expression @Semicolon | @KwReturn @Semicolon").Returns("expr")
```
This fails because the second alternative doesn't have an `expr` field to return.

### 4. Expression Dispatcher Chain Uses Consistent Field Names
Looking at the existing Expression rules:
```csharp
public Rule Expression = "expr:NonAssignmentExpression | expr:AssignmentExpression";
public Rule NonAssignmentExpression = "expr:ConditionalExpression";
public Rule ConditionalExpression = "... | expr:NullCoalescingExpression";
// ... all use "expr" field name
```

They all use the same field name `expr` throughout the chain.

## Current State

### What Works ✅
1. **Literal token capture:** `{lexeme}` properly gets token text
2. **Literal maps:** DecimalIntegerLiteral, HexIntegerLiteral, BinaryIntegerLiteral, TrueLiteral, FalseLiteral correctly generate WASM when used directly  
3. **Compilation:** No parse errors, code compiles successfully
4. **Other language features:** Classes, methods, parameters all still work

### What Doesn't Work ❌
1. **Return with expression:** `return 5;` uses Fallback instead of generating `(return (i32.const 5))`
2. **Expression dispatcher chain:** The Expression → NonAssignmentExpression → ... chain doesn't populate fields for lowering
3. **Recursive map application:** CDTk doesn't reliably apply child node maps when expanding parent fields

## Recommended Next Steps

### Option 1: Fix CDTk Dispatcher Implementation (Ideal)
This is a CDTk framework issue. The dispatcher chain with labeled alternatives should properly propagate fields through the AST for Map expansion. This would require:
1. Investigating CDTk's AST construction for dispatcher rules
2. Understanding why `{expr}` field expansion triggers Fallback instead of applying child maps
3. Potentially fixing CDTk's template expansion to properly recurse

### Option 2: Use CDTk Model Transformations
Create a Model class that transforms the AST after parsing but before lowering:
```csharp
public class ExpressionFlattener : Model
{
    public override void Transform(Node node)
    {
        // Find ReturnStatement nodes
        // Flatten the Expression → NonAssignmentExpression → ... chain
        // Replace multi-level expr fields with direct literal references
    }
}
```

This bypasses the issue by restructuring the AST before Maps are applied.

### Option 3: Simplify Expression Grammar (Nuclear Option)
Flatten the entire expression grammar to eliminate dispatcher levels:
```csharp
public Rule Expression = new Rule(
    "left:UnaryExpression op:BinaryOperator right:Expression " +  // Binary
    "| left:Expression @Question trueExpr:Expression @Colon falseExpr:Expression " +  // Ternary
    "| expr:PrimaryExpression"  // Terminals
).Returns("left", "op", "right", "trueExpr", "falseExpr", "expr");
```

This would require massive grammar restructuring and is not recommended.

### Option 4: Targeted Workarounds
For critical cases like ReturnStatement, manually handle common patterns:
1. Keep Expression chain as-is for complex expressions
2. Add special handling in Models or lowering for simple literal returns
3. Live with Fallback output for now (it's technically correct, just verbose)

## Test Cases

### Test 1: Simple Return
**Input:** `return 5;`
**Expected:** `(return (i32.const 5))`
**Actual:** Fallback with empty expression

### Test 2: Void Return  
**Input:** `return;`
**Expected:** `(return)`
**Actual:** Works (when using optional Expression)

### Test 3: Return Expression
**Input:** `return a + b;`
**Expected:** `(return (i32.add (local.get $a) (local.get $b)))`
**Actual:** Not tested yet (binary expressions not implemented)

## Files Modified

- `/home/runner/work/CRAB/CRAB/Compiler/Core/RuleSet.cs`
  - Fixed DecimalIntegerLiteral, HexIntegerLiteral, BinaryIntegerLiteral rules to use single token without labels
  - Attempted various ReturnStatement rule structures (currently reverted to original)

- `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
  - Changed literal maps to use `{lexeme}` instead of `{value}`
  - Attempted various ReturnStatement map structures (currently at original)

## Conclusion

The literal capture fix (using `{lexeme}`) is solid and should be kept. The Expression dispatcher chain issue is a deeper CDTk framework problem that requires either:
1. CDTk framework fixes
2. AST transformation via Models
3. Grammar simplification

Given time constraints, **Option 2 (Model transformation)** is the most practical path forward. It allows keeping the existing grammar structure while working around the dispatcher chain issue through explicit AST manipulation.
