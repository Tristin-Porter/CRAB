# ReturnStatement Expression Lowering - Investigation Summary

## Problem
`return 5;` generates the CDTk Fallback map instead of proper WASM:

**Current Output:**
```wasm
(return ;; TODO: Add map for this construct
nop
)
```

**Expected Output:**
```wasm
(return (i32.const 5))
```

## Root Cause Analysis

The expression `5` goes through this transformation chain:
1. ReturnStatement: `expr:Expression?` field
2. Expression → NonAssignmentExpression → ConditionalExpression → ... → PrimaryExpressionCore → Literal
3. Literal → DecimalIntegerLiteral
4. DecimalIntegerLiteral should map to `(i32.const {value})`

## Investigation Findings

### Issue 1: Literal Rule Structure (FIXED)
The Literal rule was using `lit:@DecimalIntegerLiteral` to capture tokens directly, but there were no intermediate rules to create AST nodes.

**Fix Applied:**
Created explicit rules for each literal type:
```csharp
public Rule Literal = "TrueLiteral | FalseLiteral | ... | DecimalIntegerLiteral | ...";
public Rule DecimalIntegerLiteral = new Rule("@DecimalIntegerLiteral");
```

This ensures each literal type creates its own AST node with a `value` field.

### Issue 2: Expression Dispatcher Chain
All expression dispatcher rules use the pattern:
```csharp
public Rule Expression = "expr:NonAssignmentExpression | expr:AssignmentExpression";
```

With string syntax, this creates Expression AST nodes with an `expr` field containing the child node.
The Maps use `{expr}` to pass through: `public Map Expression = "{expr}";`

This SHOULD work, but testing shows the `{expr}` field is empty or contains the wrong node type.

### Issue 3: Dispatcher Rules Without `.Returns()`
Some dispatcher rules use string syntax without `.Returns()`:
```csharp
public Rule NonAssignmentExpression = "expr:ConditionalExpression";
```

It's unclear if CDTk requires `.Returns("expr")` for these to work properly.

## Questions for Resolution

1. When a Rule uses string syntax like `"expr:ChildRule"`, does CDTk automatically populate the `expr` field?
2. Do dispatcher rules need `.Returns()` to work correctly?
3. When a Rule directly matches a token like `new Rule("@DecimalIntegerLiteral")`, does CDTk create a `value` field?
4. Should the Literal rule be a pure dispatcher (no AST node) or create its own node?

## Attempted Fixes

1. ✓ Created explicit literal type rules
2. ✗ Added/removed `.Returns()` from dispatcher rules  - no effect
3. ✗ Removed `expr:` labels from passthrough alternatives - made it worse
4. ✗ Changed field names in Maps - no effect

## Current State

The code compiles and runs, but expressions always trigger the Fallback map.
All Maps are defined correctly for each AST node type.
The issue is in how the AST is structured during parsing, not in the Maps themselves.

## Recommended Next Steps

1. Add detailed AST inspection to understand what node types are actually being created
2. Review CDTk documentation on dispatcher rules and string syntax
3. Consider simplifying the expression chain to eliminate unnecessary dispatcher levels
4. Test with a minimal example (just `DecimalIntegerLiteral` without dispatchers)
