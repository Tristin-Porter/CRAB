# CDTk Parser Bug Report: Field Assignment Mismatch

## Summary

The CDTk parser is incorrectly assigning matched grammar elements to named fields in the AST. Fields are assigned in the wrong order, with wrong values, and the parser stops creating fields prematurely.

## Minimal Reproduction

### Grammar Rule
```csharp
public class Rules : RuleSet
{
    public Rule ClassDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwClass name:@Identifier typeParams:TypeParameterList? baseList:BaseList? constraints:TypeParameterConstraintsClauses? body:ClassBody @Semicolon?")
        .Returns("attrs", "mods", "name", "typeParams", "baseList", "constraints", "body");
        
    public Rule ClassBody = new Rule("@OpenBrace members:ClassMemberDeclarations? @CloseBrace")
        .Returns("members");
        
    // ... other required rules
}
```

### Input
```csharp
class Program { int x; }
```

### Expected AST
```
ClassDeclaration
  [attrs] = null (no AttributeSections matched)
  [mods] = null (no Modifiers matched)
  [name] = "Program" (from @Identifier token)
  [typeParams] = null (no TypeParameterList matched)
  [baseList] = null (no BaseList matched)
  [constraints] = null (no TypeParameterConstraintsClauses matched)
  [body] = ClassBody node
```

### Actual AST
```
ClassDeclaration
  [attrs] = KwClass node (WRONG! This is the @KwClass token)
  [mods] = Identifier node (WRONG! This is the @Identifier token "Program")
  [name] = ClassBody node (WRONG! This is the body, not the name)
  (missing: typeParams, baseList, constraints, body)
```

## Problem Analysis

1. **Wrong Field Assignment**: The parser is assigning grammar elements to fields in incorrect order
2. **Wrong Values**: Tokens are being assigned to the wrong field names
3. **Premature Termination**: Only 3 of 7 fields are created
4. **Pattern**: It appears tokens prefixed with `@` are being skipped in the field assignment logic, causing a misalignment

## Field Assignment Pattern

Looking at the rule pattern:
```
attrs:AttributeSections? mods:Modifiers? @KwClass name:@Identifier ... body:ClassBody @Semicolon?
```

Expected field assignment:
```
Field 1 (attrs): AttributeSections? → null (optional, not matched)
Field 2 (mods): Modifiers? → null (optional, not matched)
Field 3 (name): @Identifier → "Program" (token lexeme)
Field 4 (body): ClassBody → ClassBody node
```

Actual field assignment:
```
Field 1 (attrs): @KwClass → KwClass node (should skip @-prefixed tokens!)
Field 2 (mods): @Identifier → Identifier node (should be in 'name' field!)
Field 3 (name): ClassBody → ClassBody node (should be in 'body' field!)
```

## Root Cause Hypothesis

The parser appears to have a bug in how it handles:
1. Optional fields (`?` suffix) that don't match
2. Anonymous tokens (`@` prefix) that shouldn't create named fields
3. Field assignment indexing/mapping

The pattern suggests the parser is:
- Not skipping unmatched optional fields properly
- Not skipping `@`-prefixed tokens when assigning to named fields
- Using a simple index-based assignment instead of name-based assignment

## Impact

This bug makes it **impossible to use CDTk for any non-trivial grammar** because:
1. AST nodes have wrong structure
2. Map templates cannot find expected fields
3. Placeholders like `{name}`, `{body}` are not substituted
4. Generated output contains literal placeholder text like `{name}`, `{members}`

## Workaround Attempts

### Attempt 1: Add `.Returns()` to Dispatcher Rules ✓ PARTIAL SUCCESS
Adding `.Returns("field")` to dispatcher rules like:
```csharp
TypeDeclaration = new Rule("type:ClassDeclaration | ...").Returns("type");
```

This fixed dispatcher rules but didn't fix the field assignment bug in structured rules.

### Attempt 2: Remove Optional Fields ❌ NOT TESTED
Try removing `?` suffix from optional fields to see if that helps.

### Attempt 3: Reorder Fields ❌ NOT TESTED  
Try reordering rule fields to work around the bug.

## Next Steps

1. **File bug with CDTk maintainers** with this detailed report
2. **Implement workaround**: Modify maps to expect wrong field names temporarily
3. **Consider parser replacement**: If CDTk can't be fixed quickly, may need alternative
4. **Add test suite**: Create comprehensive parser tests to catch regressions

## Test Case for CDTk

```csharp
[Test]
public void TestFieldAssignment()
{
    var tokens = new TokenSet();
    tokens.Add("class", "CLASS");
    tokens.Add("[a-zA-Z_][a-zA-Z0-9_]*", "ID");
    tokens.Add("{", "LBRACE");
    tokens.Add("}", "RBRACE");
    
    var rules = new RuleSet();
    rules.Add("ClassDecl", new Rule("@CLASS name:@ID @LBRACE @RBRACE")
        .Returns("name"));
    
    var parser = new Parser(tokens, rules);
    var result = parser.Parse("class Foo { }");
    
    var classDecl = result.Ast;
    Assert.Equal("ClassDecl", classDecl.Type);
    Assert.True(classDecl.Fields.ContainsKey("name"));
    Assert.Equal("Foo", classDecl.Fields["name"]); // Should be string "Foo", not ID node
}
```

## References

- CRAB Repository: `/home/runner/work/CRAB/CRAB`
- Affected File: `Compiler/Core/RuleSet.cs`
- CDTk Parser: `Dependencies/CDTk/Boilerplate/CDTk.cs` (lines 6000-8000, parser implementation)
- Map Generator: `Dependencies/CDTk/Boilerplate/CDTk.cs` (lines 9246-9306)

## Date

2025-01-14

## Severity

**CRITICAL** - Blocks all compilation functionality
