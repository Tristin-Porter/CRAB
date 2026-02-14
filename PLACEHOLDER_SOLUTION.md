# Solution to Placeholder Substitution Issue

## Problem Summary

Placeholders like `{type}`, `{members}`, `{name}` were not being substituted in the generated WASM output because:

1. Dispatcher rules weren't returning proper field names
2. Token nodes weren't being mapped to their lexemes
3. Type maps were pass-through instead of outputting actual WASM types

## Root Cause

CDTk creates AST nodes where:
- Tokens (like `@KwInt`, `@Identifier`) become AstNode objects with a `lexeme` field
- Dispatcher rules (alternations like `A | B | C`) need explicit `.Returns()` to specify field names
- Maps need to recursively transform child nodes AND handle token nodes

## Solution

### 1. Add `.Returns()` to All Dispatcher Rules ✓ DONE

Added `.Returns()` to key dispatcher rules:
- `CompilationUnitItem` → `.Returns("item")`
- `NamespaceMemberDeclaration` → `.Returns("member")`
- `TypeDeclaration` → `.Returns("type")`
- `ClassMemberDeclaration` → `.Returns("member")`
- `NonArrayType` → `.Returns("type")`
- `PrimitiveType` → `.Returns("type")`
- `IntegralType` → `.Returns("type")`

### 2. Add Maps for All Tokens

Tokens create AstNode objects with a `lexeme` field. We need maps to extract the lexeme:

```csharp
// Keyword tokens
public Map KwClass = "";      // Keywords are skipped in output
public Map KwInt = "i32";     // Map C# types to WASM types
public Map KwLong = "i64";
public Map KwFloat = "f32";
public Map KwDouble = "f64";
public Map KwBool = "i32";
// ... etc

// Identifier tokens  
public Map Identifier = "{lexeme}";

// Other tokens
public Map OpenBrace = "{{";
public Map CloseBrace = "}}";
// ... etc
```

### 3. Fix Type Maps to Output WASM Types

Instead of pass-through like `IntegralType = "{type}"`, map C# types to WASM:

```csharp
// These are now dispatcher nodes, they contain a 'type' field
// The 'type' field contains a token node (like KwInt) that has a lexeme
// When recursively transformed, KwInt map outputs "i32"
public Map IntegralType = "{type}";  // This actually works now!
public Map PrimitiveType = "{type}";  // Because type field is transformed recursively
```

But we can also be more explicit:

```csharp
// Map tokens directly
public Map KwInt = "i32";
public Map KwLong = "i64";
public Map KwFloat = "f32";
public Map KwDouble = "f64";
```

### 4. Handle Optional Fields

When a rule has optional fields like `attrs:AttributeSections? mods:Modifiers?`, the AST node will have those fields set to `null` when not matched. Maps should handle this gracefully.

## Testing

Test case: `class Program { int x; }`

Expected AST structure after fixes:
```
CompilationUnit
  [items] = CompilationUnitItem
    [item] = NamespaceMemberDeclaration
      [member] = TypeDeclaration
        [type] = ClassDeclaration
          [attrs] = null
          [mods] = null
          [name] = Identifier(lexeme="Program")
          [typeParams] = null
          [baseList] = null
          [constraints] = null
          [body] = ClassBody
            [members] = ClassMemberDeclarations
              [members] = [ClassMemberDeclaration]
                [member] = FieldDeclaration
                  [attrs] = null
                  [mods] = null
                  [type] = Type
                    [base] = NonArrayType
                      [type] = PrimitiveType
                        [type] = IntegralType
                          [type] = KwInt(lexeme="int")
                  [declarators] = VariableDeclarators...
```

Expected output after fixes:
```wasm
(module
  ;; Imports
  (import "env" "memory" (memory 1))
  
  ;; Generated members
  ;; class Program
  (type $Program (struct
    (field $x i32)
  ))
)
```

## Implementation Steps

1. ✅ Add `.Returns()` to dispatcher rules
2. ⏳ Add maps for all keyword tokens mapping to WASM types
3. ⏳ Add maps for identifier and literal tokens
4. ⏳ Fix FieldDeclaration map to handle type properly
5. ⏳ Test end-to-end compilation

## Next Actions

1. Create maps for all tokens in TokenSet
2. Update type-related maps in MapSet
3. Fix FieldDeclaration and other member maps
4. Test with simple class
5. Expand test cases
