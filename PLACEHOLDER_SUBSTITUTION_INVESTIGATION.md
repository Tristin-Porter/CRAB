# Placeholder Substitution Investigation

## Problem Statement

When compiling `class Program { int x; }`, the output contains unresolved placeholders like `{item}`, `{members}`, `{type}` instead of actual WASM code.

## Root Cause Analysis

### Issue 1: Parser Creates Wrong AST Structure

**Expected AST:**
```
CompilationUnit
  [items] = CompilationUnitItem
    [item] = NamespaceMemberDeclaration
      [member] = TypeDeclaration
        [type] = ClassDeclaration
          [attrs] = null
          [mods] = null
          [name] = "Program"
          [body] = ClassBody
            [members] = ClassMemberDeclarations
              [members] = [ClassMemberDeclaration]
                [member] = FieldDeclaration
                  [type] = Type...
```

**Actual AST:**
```
CompilationUnit
  [items] = CompilationUnitItem
    [attrs] = KwClass        <- WRONG! Should be [item] field
    [mods] = Identifier      <- WRONG!
    [name] = ClassBody       <- WRONG!
```

The parser is **collapsing the rule chain** and assigning fields from the deepest matched rule (`ClassDeclaration`) to an intermediate node (`CompilationUnitItem`), bypassing the proper structure.

### Issue 2: Field Name Mismatch

Map templates expect specific field names based on rule definitions:

1. `CompilationUnit` template: `"{items}"` expects field `items` ✓ (WORKS)
2. `CompilationUnitItem` template: `"{item}"` expects field `item` ✗ (has `attrs`, `mods`, `name` instead)

When `Map.Generate()` tries to substitute `{item}`, it looks for a field named `item` in the node, doesn't find it, and leaves the placeholder as-is.

### Issue 3: Token Matches Creating Wrong Nodes

Looking at the AST, tokens like `@KwClass` and `@Identifier` are being wrapped in AstNodes with types `KwClass` and `Identifier`. These have a `lexeme` field containing the actual text.

But the maps expect:
- For `IntegralType = "{type}"`, the `type` field should contain the token text "int", not an AstNode

## CDTk Parser Behavior

The parser appears to have a "pass-through" optimization where:
- When a rule is just an alternation (`A | B | C`), it doesn't create a wrapper node
- Instead, it uses the matched child's fields directly
- But it assigns the PARENT rule's Type name to the node

This causes:
```csharp
CompilationUnitItem = "item:ExternAliasDirective | item:UsingDirective | ... | item:NamespaceMemberDeclaration";
```

To create a node with:
- Type: `CompilationUnitItem` (from the rule name)
- Fields: `attrs`, `mods`, `name` (from ClassDeclaration, deep in the tree)

## The Real Problem

There are two potential issues:

### Hypothesis A: Parser Bug
The parser is incorrectly collapsing rule chains and assigning wrong field names. The parser should create proper nested structures.

### Hypothesis B: Rules Need `.Returns()`
The dispatcher rules might need explicit `.Returns()` clauses to tell the parser what fields to extract:

```csharp
// Current (implicit)
public Rule CompilationUnitItem = "item:ExternAliasDirective | item:UsingDirective | ... | item:NamespaceMemberDeclaration";

// Should be (explicit)
public Rule CompilationUnitItem = new Rule("item:ExternAliasDirective | item:UsingDirective | ... | item:NamespaceMemberDeclaration")
    .Returns("item");
```

### Hypothesis C: Token Extraction Issue
When a rule matches a token like `type:@KwInt`, the parser creates:
- An AstNode with Type=`KwInt` and Fields={lexeme: "int"}

But maps expect the field to contain just the string "int", not the wrapper node.

## Solution Strategy

1. **Fix Rule Returns**: Add `.Returns()` to all dispatcher rules to explicitly specify field names
2. **Fix Token Handling**: Tokens should be stored as strings in parent node fields, not as wrapped AstNodes
3. **Fix Type Maps**: Type maps like `IntegralType` should output WASM types, not pass through with `{type}`

## Next Steps

1. Add `.Returns("item")` to `CompilationUnitItem` rule
2. Add `.Returns("member")` to `NamespaceMemberDeclaration` rule  
3. Add `.Returns("type")` to `TypeDeclaration` rule
4. Add `.Returns("member")` to `ClassMemberDeclaration` rule
5. Add `.Returns("type")` to `IntegralType`, `PrimitiveType`, etc.
6. Fix type maps to output actual WASM types like "i32" instead of "{type}"
7. Test if placeholders are now substituted

## Type Mapping Reference

C# types should map to WASM types:
- `int`, `uint`, `short`, `ushort`, `byte`, `sbyte` → `i32`
- `long`, `ulong` → `i64`
- `float` → `f32`
- `double` → `f64`
- `bool` → `i32`
- `char` → `i32`
- Reference types → `(ref ...)` or `i32` (as pointers)
