# ReturnStatement Expression Lowering - Complete Fix Summary

## Problem Statement
```csharp
class Test {
    int M() {
        return 5;
    }
}
```

Generates:
```wasm
(return ;; TODO: Add map for this construct
nop
)
```

Expected:
```wasm
(return (i32.const 5))
```

## Changes Made

### 1. Created Explicit Literal Type Rules
**File:** `Compiler/Core/RuleSet.cs`

Before (lines 773):
```csharp
public Rule Literal = "lit:@KwTrue | lit:@KwFalse | ... | lit:@DecimalIntegerLiteral | ...";
```

After (lines 773-790):
```csharp
public Rule Literal = "TrueLiteral | FalseLiteral | NullLiteral | DecimalIntegerLiteral | ...";

public Rule TrueLiteral = new Rule("@KwTrue");
public Rule FalseLiteral = new Rule("@KwFalse");
public Rule NullLiteral = new Rule("@KwNull");
public Rule DecimalIntegerLiteral = new Rule("@DecimalIntegerLiteral");
// ... etc for all 16 literal types
```

**Rationale:** When a Rule directly matches a token (`@DecimalIntegerLiteral`), CDTk creates an AST node of that Rule's type with a `value` field containing the token text. The Literal rule now dispatches to specific typed rules, allowing each literal type to have its own Map.

### 2. Updated ReturnStatement Map
**File:** `Compiler/Core/MapSet.cs` (line 331)

Before:
```csharp
public Map ReturnStatement = "(return)";  // Hardcoded, ignored {expr}
```

After:
```csharp
public Map ReturnStatement = "(return {expr})";
```

**Rationale:** The ReturnStatement rule has `expr:Expression?` field that needs to be included in the output.

### 3. Fixed NullCoalescingExpression Dispatcher
**File:** `Compiler/Core/RuleSet.cs` (line 653)

Before:
```csharp
public Rule NullCoalescingExpression = "... | LogicalOrExpression";  // Missing label
```

After:
```csharp
public Rule NullCoalescingExpression = "... | expr:LogicalOrExpression";
```

**Rationale:** Consistency with other dispatcher rules. The `expr:` label captures the child node in the `expr` field, which the Map expects.

## Current Status: NOT WORKING

Despite these fixes, the expression still generates the Fallback map. All necessary Maps are defined:
- `DecimalIntegerLiteral = "(i32.const {value})"`
- `Expression = "{expr}"`
- `NonAssignmentExpression = "{expr}"`
- etc. for the entire dispatcher chain

## Investigation Summary

The transformation chain is:
```
ReturnStatement.expr → Expression → NonAssignmentExpression → ConditionalExpression →
NullCoalescingExpression → LogicalOrExpression → LogicalAndExpression → BitwiseOrExpression →
BitwiseXorExpression → BitwiseAndExpression → EqualityExpression → RelationalExpression →
ShiftExpression → AdditiveExpression → MultiplicativeExpression → SwitchExpression →
RangeExpression → UnaryExpression → UnaryExpressionBase → PrimaryExpressionCore →
Literal → DecimalIntegerLiteral
```

Testing shows:
1. The ReturnStatement Map IS being used
2. The `{expr}` placeholder IS being substituted
3. But the substitution produces the Fallback map output
4. The Expression Map shows an empty `{expr}` field when used

## Hypothesis: CDTk Dispatcher Behavior

The issue may be in how CDTk handles dispatcher rules with string syntax:

```csharp
public Rule Expression = "expr:NonAssignmentExpression | expr:AssignmentExpression";
```

Possible issues:
1. CDTk might not be creating the `expr` field properly
2. The AST node type might not match the Map name
3. String syntax might require `.Returns("expr")` to work correctly
4. Pure dispatchers might need a different syntax

## Next Steps

1. **Inspect AST structure:** Create a debug tool to print the actual AST node types and fields
2. **Test minimal case:** Create a simpler test with just `DecimalIntegerLiteral` directly
3. **Review CDTk source:** Check how dispatcher rules work internally
4. **Ask CDTk maintainer:** This might be a known limitation or require specific syntax

## Workaround Options

If the dispatcher chain can't be fixed:
1. **Flatten the grammar:** Remove all intermediate dispatchers, use a single Expression rule with all alternatives
2. **Manual AST manipulation:** Process the AST before transformation to restructure nodes
3. **Custom Map logic:** Use CDTk Models to intercept and fix the AST structure
4. **Direct token matching:** Have ReturnStatement directly match literal tokens instead of Expression

## Files Modified
- `Compiler/Core/RuleSet.cs`: Added literal type rules, fixed NullCoalescingExpression
- `Compiler/Core/MapSet.cs`: Updated ReturnStatement Map
- Test files: `simple_int.crab`, `debug_return.crab`, etc.

## Verification
```bash
dotnet build  # Compiles successfully
dotnet run compile simple_int.crab
cat output.wasm  # Still shows Fallback map
```
