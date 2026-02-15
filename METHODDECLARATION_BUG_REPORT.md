# MethodDeclaration Map Field Shifting Bug

## Problem Summary

The MethodDeclaration Map has a critical field shifting bug caused by how CDTk handles optional fields in grammar rules.

## Root Cause

CDTk's parser assigns field values to the `.Returns()` field names **sequentially based on what was actually parsed**, rather than **by the labeled field names in the grammar pattern**.

### The Grammar Rule

```csharp
public Rule MethodDeclaration = new Rule(
    "attrs:AttributeSections? mods:Modifiers? returnType:Type name:@Identifier " +
    "typeParams:TypeParameterList? @OpenParen parameters:FormalParameterList? @CloseParen " +
    "constraints:TypeParameterConstraintsClauses? body:MethodBody"
)
.Returns("attrs", "mods", "returnType", "name", "typeParams", "parameters", "constraints", "body");
```

### Expected Behavior

Field names should be assigned based on the **labels in the pattern** (attrs:, mods:, etc.):
- `attrs` → AttributeSections (or null if absent)
- `mods` → Modifiers (or null if absent)  
- `returnType` → Type
- `name` → Identifier
- `parameters` → FormalParameterList (or null if absent)
- `body` → MethodBody

### Actual Behavior

Field names are assigned **sequentially** from the Returns() list to whatever was actually parsed.

**Method WITHOUT parameters** (`int NoParams() { return 5; }`):
- Pattern matches: Type, Identifier, MethodBody (3 elements)
- Fields assigned: `attrs`=Type, `mods`=Identifier, `returnType`=MethodBody
- `name`, `parameters`, `body` are NOT present in AST node!

**Method WITH parameters** (`int Add(int a, int b) { return a+b; }`):
- Pattern matches: Type, Identifier, FormalParameterList, MethodBody (4 elements)
- Fields assigned: `attrs`=Type, `mods`=Identifier, `returnType`=FormalParameterList, `name`=MethodBody
- `parameters`, `body` are NOT present in AST node!

## Impact

This makes it impossible to create a single MethodDeclaration Map that works correctly for both cases:

```csharp
// Current attempt:
public Map MethodDeclaration = @"(func ${mods}
{returnType}{name}
)";
```

- **NO params case**: `${mods}` = method name ✓, `{returnType}` = body, `{name}` = missing → outputs body twice
- **WITH params case**: `${mods}` = method name ✓, `{returnType}` = params ✓, `{name}` = body ✓ → works!

## Attempted Solutions

### 1. Use All Fields (Failed)
Can't use `{body}` because it doesn't exist - it was shifted to `{returnType}` or `{name}`.

### 2. Create Conditional Maps (Impossible)
CDTk Maps don't support conditionals or logic - they're pure string templates.

### 3. Pass Parent Context to Child Maps (Impossible)
Maps only see their own node, not parent context. Can't pass return type from MethodDeclaration to MethodBody.

### 4. Post-Processing (Attempted, Incomplete)
Added `FixMethodDeclarations()` in Compile.cs, but full implementation is complex and error-prone.

### 5. Fix CDTk Parser (Correct, But Complex)
The proper fix is to modify CDTk's field assignment logic to respect labeled field names from the pattern rather than using sequential Returns() assignment. This requires deep changes to CDTk internals.

## Current Workaround

1. **CDTk Enhancement**: Added logic to replace undefined placeholders with empty strings instead of leaving them as literals.

2. **Map Best Effort**: Current MethodDeclaration Map outputs:
   ```csharp
   public Map MethodDeclaration = @"(func ${mods}
     (result {attrs})
   {returnType}
   {name}
   )";
   ```
   
   This produces:
   - **Methods WITHOUT params**: `(func $name (result type) body)` - body appears after result ✓, but not nested
   - **Methods WITH params**: `(func $name (result type) params body)` - params AFTER result ✗ (should be before)

3. **Documentation**: This document and inline comments explain the bug for future maintainers.

## Status

**PARTIALLY WORKING**: Methods without parameters generate mostly valid structure. Methods WITH parameters have params in wrong position (after result instead of before).

Both cases lack proper nesting/indentation but are structurally closer to correct.

## Proper Fix

To properly fix this bug, one of the following is required:

### Option A: Fix CDTk Parser (Recommended)
Modify CDTk's AST node field assignment to use the **labeled field names from the grammar pattern** (attrs:, mods:, etc.) rather than sequential Returns() list assignment.

Location: `CDTk/Boilerplate/CDTk.cs` - parser field assignment logic

### Option B: Add Parent Context to Maps
Extend CDTk's Map system to allow child nodes to access parent node fields during transformation. This would allow MethodBody to access the return type from its parent MethodDeclaration node.

### Option C: Grammar Restructuring  
Redesign the MethodDeclaration grammar to avoid optional named fields at the beginning, or use explicit alternatives instead of optional fields.

## Test Cases

```csharp
// Test file: debug_method_fields.crab
class Test {
    int NoParams() { return 5; }              // Currently BROKEN
    int WithParams(int a) { return a; }        // Works correctly
    int MultiParams(int a, int b) { return a+b; }  // Works correctly
}
```

## Files Affected

- `Compiler/Core/MapSet.cs` - MethodDeclaration Map
- `Compiler/Core/RuleSet.cs` - MethodDeclaration Rule
- `Dependencies/CDTk/Boilerplate/CDTk.cs` - Added placeholder replacement, debug output removed
- `CLI/Commands/Compile.cs` - Added FixMethodDeclarations() placeholder
- This document

## References

- CRAB Spec: C# language support requires full method declaration syntax
- WASM Spec: Function syntax is `(func $name (param...) (result...) body...)`
- CDTk Documentation: Rule patterns and Returns() specification
