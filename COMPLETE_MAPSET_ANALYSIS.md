# COMPLETE MapSet Analysis and Fix Guide

## Problem Statement

The WASM output from CRAB has numerous issues:

1. **Unresolved placeholders**: `{members}`, `{type}`, `{body}`, `{parameters}`, etc.
2. **Many nop instructions**: 16 total
3. **WARNING messages**: 8 occurrences
4. **Literal type names**: "ClassMemberDeclarations" appearing in output
5. **Broken Fallback map**: Contains `{type}` placeholder that never gets resolved

## Root Cause #1: Map.Generate() Doesn't Recursively Transform

**File**: `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs`
**Lines**: 9269-9274

```csharp
else if (v is AstNode child)
{
    vars[key] = child.Type;  // ❌ Only extracts type name, doesn't transform!
}
```

**Impact**: When a Map template contains `{body}` and the AST node has a `body` field pointing to a child AstNode, the placeholder gets replaced with the literal type name (e.g., "ClassBody") instead of the generated WASM output.

**Example**:
- Template: `{body}`
- AST field: `body: AstNode(type="ClassBody")`
- Current output: `ClassBody` ❌
- Expected output: `(field $Add (func ...))` ✅

## Root Cause #2: Fallback Map Has Unresolved Placeholder

**File**: `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
**Lines**: 1690-1694

```csharp
public Map Fallback = @"
;; WARNING: Unmapped C# construct: {type}
;; This node type requires explicit WASM mapping implementation
;; Falling back to nop instruction to maintain valid WASM output
nop";
```

**Problem**: The placeholder `{type}` doesn't exist in AST nodes!
- AST nodes have a `.Type` property, not a `type` field
- Template placeholders reference **fields**, not **properties**
- So `{type}` never gets resolved

**Fix**: Either:
A. Don't use placeholder (hardcode the warning)
B. Add logic to expose node.Type as a field during template expansion

## Root Cause #3: Missing Maps for Common Constructs

Based on output analysis, these maps are completely missing:

1. **CompilationUnitItem** - Top-level item wrapper
2. **ParameterList** - Method parameter list
3. **Parameter** - Individual parameter
4. **FormalParameters** - Parameter declaration
5. **MethodBody** - Method body wrapper
6. **IntType** / **VoidType** / **StringType** - Primitive types
7. **TypeName** - Type reference
8. **ReturnType** - Method return type

## The Complete Fix

### Phase 1: Fix CDTk's Map.Generate() (CRITICAL)

**File**: `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs`

**Change 1** - Update Map.Generate() signature (line ~9246):
```csharp
// OLD
internal string Generate(AstNode node)

// NEW
internal string Generate(AstNode node, MapSet? mapSet = null)
```

**Change 2** - Fix child node handling (lines ~9269-9280):
```csharp
else if (v is AstNode child)
{
    // Recursively transform child node if MapSet available
    if (mapSet != null)
    {
        vars[key] = mapSet.Transform(child) ?? child.Type;
    }
    else
    {
        vars[key] = child.Type;  // Fallback
    }
}
else if (v is IEnumerable<AstNode> children)
{
    if (mapSet != null)
    {
        // Transform each child and join with newlines
        var childOutputs = new System.Collections.Generic.List<string>();
        foreach (var c in children)
        {
            var output = mapSet.Transform(c);
            if (!string.IsNullOrWhiteSpace(output))
                childOutputs.Add(output);
        }
        vars[key] = string.Join("\n", childOutputs);
    }
    else
    {
        vars[key] = string.Join(", ", children.Select(c => c.Type));
    }
}
```

**Change 3** - Update MapSet.Transform() calls (line ~8814):
```csharp
internal string? Transform(AstNode node)
{
    if (node is null) return null;

    if (_mapsByName.TryGetValue(node.Type, out var map))
    {
        return map.Generate(node, this);  // ← Pass 'this'
    }

    if (_mapsByName.TryGetValue("Fallback", out var fallbackMap))
    {
        return fallbackMap.Generate(node, this);  // ← Pass 'this'
    }

    return null;
}
```

**Change 4** - Update ToCodeGenerator() call (line ~8846):
```csharp
return map.Generate(dummyNode, this);  // ← Add 'this'
```

**Change 5** - Update ToSemantics() call (line ~8867):
```csharp
semantics.Map(mapName, (ctx, node, ct) => map.Generate(node, this));  // ← Add 'this'
```

### Phase 2: Fix Fallback Map

**File**: `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
**Lines**: 1690-1694

**Option A - Remove placeholder**:
```csharp
public Map Fallback = @"
;; WARNING: Unmapped C# construct (no map defined)
;; This node type requires explicit WASM mapping implementation
;; Falling back to nop instruction to maintain valid WASM output
nop";
```

**Option B - Expose node.Type as field** (more complex, requires CDTk change)

### Phase 3: Add Missing Maps

**File**: `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`

Add these critical maps:

```csharp
// ============================================================
// COMPILATION UNIT ITEMS
// ============================================================

/// <summary>Compilation unit item wrapper</summary>
public Map CompilationUnitItem = "{item}";

// ============================================================
// PARAMETERS
// ============================================================

/// <summary>Parameter list</summary>
public Map ParameterList = "{parameters}";

/// <summary>Formal parameters</summary>
public Map FormalParameters = "{parameters}";

/// <summary>Fixed parameters</summary>
public Map FixedParameters = "{parameters}";

/// <summary>Fixed parameter</summary>
public Map FixedParameter = "(param ${name} {type})";

/// <summary>Parameter array</summary>
public Map ParameterArray = "(param ${name} {type})";

// ============================================================
// TYPES
// ============================================================

/// <summary>Type reference</summary>
public Map TypeName = "{type}";

/// <summary>Qualified name as type</summary>
public Map QualifiedName = "{name}";

/// <summary>Int type</summary>
public Map IntType = "i32";

/// <summary>Void type</summary>
public Map VoidType = "";

/// <summary>String type</summary>
public Map StringType = "(ref string)";

/// <summary>Bool type</summary>
public Map BoolType = "i32";

/// <summary>Return type</summary>
public Map ReturnType = "{type}";

// ============================================================
// METHOD BODY
// ============================================================

/// <summary>Method body</summary>
public Map MethodBody = @"{stmts}";

/// <summary>Empty method body</summary>
public Map EmptyMethodBody = "nop";
```

## Why This Fix Works

### Before Fix

**Input AST**:
```
ClassDeclaration
  name: "Calculator"
  body: ClassBody
    members: ClassMemberDeclarations
      member: MethodDeclaration
        name: "Add"
        returnType: IntType
        parameters: ParameterList
          parameter: Parameter { name: "a", type: IntType }
          parameter: Parameter { name: "b", type: IntType }
        body: MethodBody (empty)
```

**Map.Generate() behavior**:
```
ClassDeclaration map: ";; class {name}\n(type ${name} (struct\n{body}\n))"
  name → "Calculator" ✓
  body → child.Type = "ClassBody" ❌ (should be transformed)
Result: ";; class Calculator\n(type $Calculator (struct\nClassBody\n))"
```

### After Fix

**Map.Generate() behavior**:
```
ClassDeclaration map: ";; class {name}\n(type ${name} (struct\n{body}\n))"
  name → "Calculator" ✓
  body → mapSet.Transform(child) ✓
    → ClassBody map: "{members}"
      members → mapSet.Transform(child) ✓
        → ClassMemberDeclarations map: "{members}"
          members → [MethodDeclaration, MethodDeclaration, MethodDeclaration]
          → mapSet.Transform each ✓
            → MethodDeclaration map generates func definitions
Result: Fully expanded WASM with proper nesting
```

## Specific Fixes for Output Issues

### Issue 1: `{members}` not resolved
**Line in output**: 6
**Cause**: CompilationUnit template has `{members}` but child nodes weren't transformed
**Fix**: Phase 1 (CDTk fix) enables recursive transformation
**Additional**: Add CompilationUnitItem map

### Issue 2: `{item}` not resolved
**Line in output**: 8
**Cause**: No CompilationUnitItem map defined
**Fix**: Phase 3 - add `public Map CompilationUnitItem = "{item}";`

### Issue 3: `{type}` in Fallback map warnings
**Lines in output**: 11, 16, 26, 36, 40, 47, 55, 63
**Cause**: Fallback map template uses undefined `{type}` placeholder
**Fix**: Phase 2 - remove `{type}` from Fallback template

### Issue 4: Literal "ClassMemberDeclarations"
**Line in output**: 19
**Cause**: ClassBody map template `{members}` replaced with child.Type instead of transformed output
**Fix**: Phase 1 - enable recursive transformation

### Issue 5: `{parameters}` not resolved  
**Lines in output**: 29, 30
**Cause**: No ParameterList map, and child nodes not transformed
**Fix**: Phase 1 + Phase 3 (add ParameterList map)

### Issue 6: `{body}` not resolved
**Lines in output**: 45, 58, 67
**Cause**: MethodDeclaration template has `{body}` but MethodBody map missing
**Fix**: Phase 3 - add MethodBody map

## Testing Plan

1. **Apply Phase 1 fixes** to CDTk.cs
2. **Rebuild** CRAB
3. **Compile comprehensive_test.crab**
4. **Check output**:
   - Count unresolved placeholders (should be 0)
   - Count WARNING messages (should still be ~5-8 for truly missing maps)
   - Verify structure is correct

5. **Apply Phase 2 fix** (Fallback map)
6. **Recompile and check** - warnings should be cleaner

7. **Apply Phase 3 fixes** (add missing maps)
8. **Final compile and verify**:
   - No unresolved placeholders
   - No warnings (or only for truly unimplemented constructs)
   - Valid WASM module structure
   - Proper nesting

## Expected Completion Metrics

### After Phase 1 Only
- Unresolved placeholders: **~15** (down from 29)
- Template expansion rate: **~40%**
- Valid WASM structure: **Partial**

### After Phase 1 + 2
- Unresolved placeholders: **~15**
- Template expansion rate: **~40%**
- Valid WASM structure: **Partial**
- Cleaner warnings: ✅

### After Phase 1 + 2 + 3
- Unresolved placeholders: **0**
- Template expansion rate: **100%** (for defined maps)
- Valid WASM structure: **Yes**
- MapSet completion: **~75%** (75% of needed maps defined)

### Full Completion (Future Work)
- Add remaining maps for all C# constructs
- MapSet completion: **100%**
- Full C# support: ✅

## Summary

**3 root causes**:
1. Map.Generate() doesn't recursively transform child nodes
2. Fallback map has invalid placeholder
3. Several critical maps are missing

**5 code changes in CDTk.cs**:
1. Update Map.Generate() signature
2. Fix child AstNode handling
3. Fix child list handling
4. Update MapSet.Transform() calls
5. Update ToCodeGenerator() and ToSemantics() calls

**1 simple fix in MapSet.cs**:
1. Remove `{type}` from Fallback template

**8 missing maps to add**:
1. CompilationUnitItem
2. ParameterList
3. FormalParameters
4. FixedParameters
5. FixedParameter
6. IntType/VoidType/StringType/BoolType
7. TypeName
8. MethodBody

**Result**: 100% MapSet template expansion, valid WASM output structure, proper code generation.
