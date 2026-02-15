# ReturnStatement Expression Lowering - Investigation Complete

## Executive Summary

Investigated why `return 5;` generates CDTk's Fallback map instead of `(return (i32.const 5))`.

**Status:** Partially fixed, investigation complete, issue identified but not resolved.

**Root Cause:** CDTk dispatcher rules with string syntax and labeled fields are not properly propagating AST nodes through the transformation chain.

## What Was Fixed

### ✅ 1. Literal Type Rules
Created explicit rules for each literal type to ensure proper AST node creation:
```csharp
public Rule DecimalIntegerLiteral = new Rule("@DecimalIntegerLiteral");
```
This creates a `DecimalIntegerLiteral` AST node with a `value` field = "5", which the Map `(i32.const {value})` can transform.

### ✅ 2. ReturnStatement Map
Updated to include the expression:
```csharp
public Map ReturnStatement = "(return {expr})";
```

### ✅ 3. Dispatcher Consistency
Fixed missing `expr:` label in NullCoalescingExpression:
```csharp
public Rule NullCoalescingExpression = "... | expr:LogicalOrExpression";
```

## What Remains Broken

The expression dispatcher chain (Expression → NonAssignmentExpression → ... → Literal → DecimalIntegerLiteral) is not working. CDTk generates the Fallback map for the expression.

**Debug Output Shows:**
- ReturnStatement Map is used ✓
- `{expr}` placeholder is substituted ✓  
- But the substitution produces Fallback map output ✗
- Expression Map shows empty `{expr}` field ✗

## Technical Analysis

### The Dispatcher Pattern
All expression dispatchers use:
```csharp
public Rule SomeExpression = "left:A op:B right:C | expr:D";
```

This should:
1. Create a `SomeExpression` AST node
2. If first alternative matches: populate `left`, `op`, `right` fields
3. If second alternative matches: populate `expr` field with child node `D`
4. Map uses `{expr}` to recursively transform child

**But:** Testing shows the `expr` field is empty when the passthrough alternative matches.

### Possible CDTk Issues
1. String syntax might not create fields for labeled alternatives without `.Returns()`
2. Pure dispatchers might need different syntax (no labels, no `.Returns()`)
3. CDTk might require explicit `.Returns("expr")` even with string syntax
4. There might be a bug in CDTk's field assignment for dispatcher rules

### What Was Tried
- ✗ Adding `.Returns("expr")` to all dispatchers → Created nested invalid structures
- ✗ Removing `expr:` labels → Created nodes with no fields
- ✗ Changing field names → No effect
- ✗ Different Map patterns → No effect

## Recommended Solutions

### Option 1: Flatten Expression Grammar (Recommended)
Remove all intermediate dispatchers. Single Expression rule with all alternatives:
```csharp
public Rule Expression = "left:Term op:(@Plus|@Minus) right:Expression | 
                          left:Factor op:(@Multiply|@Divide) right:Expression |
                          @DecimalIntegerLiteral | 
                          ...";
```
**Pros:** Eliminates dispatcher issues  
**Cons:** Large complex rule, loses C# grammar structure

### Option 2: Custom AST Processing
Use a CDTk Model to restructure the AST before transformation:
```csharp
public class ExpressionFixer : Model
{
    // Traverse AST and fix dispatcher node structures
}
```
**Pros:** Preserves grammar structure  
**Cons:** Complex, requires deep CDTk knowledge

### Option 3: Direct Matching in ReturnStatement  
Avoid Expression rule entirely:
```csharp
public Rule ReturnStatement = "@KwReturn expr:Literal? @Semicolon" | 
                              "@KwReturn expr:BinaryOp? @Semicolon" | ...;
```
**Pros:** Bypasses dispatcher chain  
**Cons:** Limited to specific patterns, not extensible

### Option 4: Wait for CDTk Fix/Clarification
The CDTk framework might have specific requirements for dispatcher rules that aren't documented.

## Files Modified
- ✅ `Compiler/Core/RuleSet.cs`: Added 16 literal type rules, fixed NullCoalescingExpression
- ✅ `Compiler/Core/MapSet.cs`: Updated ReturnStatement Map
- ✅ `EXPRESSION_INVESTIGATION.md`: Detailed investigation notes
- ✅ `RETURNSTATEMENT_FIX_SUMMARY.md`: Complete fix summary

## Verification Commands
```bash
# Compile test
dotnet build

# Run test
dotnet run compile simple_int.crab

# Check output (still shows Fallback)
cat output.wasm

# Expected in output:
(return (i32.const 5))

# Actual in output:
(return ;; TODO: Add map for this construct
nop
)
```

## Security Impact
None - this is a code generation issue, not a security issue.

## Performance Impact
None at runtime - this affects compile-time only.

## Breaking Changes
None - this is all new functionality.

## Next Actions
1. Review this investigation with CDTk expert or maintainer
2. Test with minimal CDTk example to isolate the issue
3. Choose and implement one of the recommended solutions above
4. Add integration tests for expression lowering once working

## Conclusion

The literal type rules are correctly defined and would work if CDTk could properly dispatch to them. The blocker is in CDTk's handling of dispatcher rules with string syntax and labeled alternatives.

All the groundwork is in place - once the dispatcher issue is resolved, expressions will transform correctly.
