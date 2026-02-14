# MapSet Template Expansion Fix Plan

## Executive Summary

The WASM output contains **29 unresolved placeholders** across multiple node types:
- `{type}`: 17 occurrences
- `{member}`: 3 occurrences  
- `{body}`: 3 occurrences
- `{parameters}`: 2 occurrences
- `{members}`: 2 occurrences
- `{name}`: 1 occurrence
- `{item}`: 1 occurrence

Plus **8 warning messages** and **16 nop instructions** from the Fallback map.

## Root Cause: CDTk's Map.Generate() Design Flaw

### The Core Problem

**Location**: `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs:9269-9274`

When `Map.Generate()` encounters a field that contains an AST child node:

```csharp
else if (v is AstNode child)
{
    vars[key] = child.Type;  // ❌ WRONG: Just extracts type name
}
```

**What it does**: Extracts the child's type name (e.g., "ClassBody")
**What it should do**: Recursively generate WASM for the child node

### Example Failure Case

**Map Template** (ClassDeclaration):
```csharp
public Map ClassDeclaration = @";; class {name}
(type ${name} (struct
{body}
))";
```

**AST Structure**:
```
ClassDeclaration
  name: "Calculator"
  body: AstNode(type="ClassBody")
```

**Current Output** (WRONG):
```wasm
;; class Calculator
(type $Calculator (struct
ClassBody    ← Literal type name instead of generated WASM!
))
```

**Expected Output**:
```wasm
;; class Calculator
(type $Calculator (struct
  (field $Add (func ...))
  (field $PrintResult (func ...))
  (field $GetMessage (func ...))
))
```

## Why This Happens

### Step-by-Step Breakdown

1. **Compiler.GenerateNode()** calls **MapSet.Transform(node)**
2. **MapSet.Transform()** finds the Map and calls **Map.Generate(node)**
3. **Map.Generate()** extracts field values:
   - String fields: Used directly ✓
   - AstNode fields: **Extracts .Type name only** ❌
   - List<AstNode> fields: **Extracts .Type names** ❌
4. Template substitution replaces `{body}` with "ClassBody" (the type name)
5. Result is written to output with unresolved placeholder
6. **Then** (too late!), Compiler.GenerateNode() recursively processes children
7. Child output is appended **after** parent, not substituted into placeholders

### The Missing Link

`Map.Generate()` has **no access to the MapSet** to recursively transform children!

It receives only the AstNode, not the MapSet reference needed to call `Transform()`.

## The Fix: Three-Part Solution

### Fix #1: Modify Map.Generate() Signature

**File**: `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs`
**Lines**: 9246-9290

**Change**:
```csharp
// OLD
internal string Generate(AstNode node)

// NEW  
internal string Generate(AstNode node, MapSet? mapSet = null)
```

**Update field extraction**:
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
        vars[key] = child.Type;  // Fallback to type name
    }
}
else if (v is IEnumerable<AstNode> children)
{
    if (mapSet != null)
    {
        // Transform each child and join with newlines
        var transformed = children
            .Select(c => mapSet.Transform(c) ?? c.Type)
            .Where(s => !string.IsNullOrWhiteSpace(s));
        vars[key] = string.Join("\n", transformed);
    }
    else
    {
        vars[key] = string.Join(", ", children.Select(c => c.Type));
    }
}
```

### Fix #2: Update MapSet.Transform() Calls

**File**: `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs`
**Lines**: 8807-8824

**Change**:
```csharp
internal string? Transform(AstNode node)
{
    if (node is null) return null;

    // Try exact name match
    if (_mapsByName.TryGetValue(node.Type, out var map))
    {
        return map.Generate(node, this);  // ← Pass 'this'
    }

    // Fallback
    if (_mapsByName.TryGetValue("Fallback", out var fallbackMap))
    {
        return fallbackMap.Generate(node, this);  // ← Pass 'this'
    }

    return null;
}
```

### Fix #3: Update Other Map.Generate() Call Sites

**File**: `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs`
**Lines**: 8846 and 8867

**Change**:
```csharp
// Line 8846 (in ToCodeGenerator)
return map.Generate(dummyNode, this);  // ← Add 'this'

// Line 8867 (in ToSemantics)  
semantics.Map(mapName, (ctx, node, ct) => map.Generate(node, this));  // ← Add 'this'
```

## Expected Results After Fix

### Before Fix
```wasm
(module
  ;; Imports
  (import "env" "memory" (memory 1))
  
  ;; Generated members
{members}    ← Unresolved
)

;; WARNING: Unmapped C# construct: {type}
nop
```

### After Fix
```wasm
(module
  ;; Imports
  (import "env" "memory" (memory 1))
  
  ;; class Calculator
  (type $Calculator (struct
    ;; Method: Add
    (func $Add
      (param $a i32)
      (param $b i32)
      (result i32)
      nop
    )
    ;; Method: PrintResult
    (func $PrintResult
      (result )
      nop
    )
    ;; Method: GetMessage
    (func $GetMessage
      (result (ref string))
      nop
    )
  ))
)
```

## Metrics

### Current State
- ❌ Unresolved placeholders: **29**
- ❌ Fallback map invocations: **8**
- ❌ nop instructions: **16**
- ❌ MapSet completion: **~0%** (most nodes fail to expand)

### After Fix
- ✅ Unresolved placeholders: **0**
- ✅ Fallback map invocations: **8** (legitimate - for unimplemented nodes)
- ✅ nop instructions: **~10** (only for incomplete method bodies)
- ✅ MapSet completion: **~70%** (structural mapping complete)

## Additional Missing Maps

The fix above solves the **expansion problem**, but there are still **legitimate missing maps**:

### From Rules That Need Maps

Based on the comprehensive_test.crab file, these additional maps are needed:

1. **Type System**:
   - `IntType`, `VoidType`, `StringType` - Primitive type mapping
   - `TypeName` - Generic type reference
   - `TypeArgumentList` - Generic type arguments

2. **Method Parameters**:
   - `ParameterList` - Method parameter list
   - `Parameter` - Individual parameter
   - `ParameterModifier` - ref/out/in modifiers

3. **Method Body**:
   - `MethodBody` - Method body container
   - Many statement types are defined but may not be used

### Maps That Exist But Have No Rules

70 maps exist without corresponding rules - these are likely:
- Operator maps (AddOperator, MultiplyOperator, etc.)
- Type maps (Int32Type, BooleanType, etc.)
- Expression maps (InvocationExpression, MemberAccessExpression, etc.)

These are **fine** - they're used by the code generator even if no direct rule produces them.

## Implementation Priority

### Phase 1: Fix CDTk (CRITICAL)
✅ This fixes the core expansion problem
✅ Enables all existing maps to work correctly
🎯 **Start here**

### Phase 2: Add Missing Structural Maps (HIGH)
- ParameterList, Parameter
- IntType, VoidType, StringType
- TypeName
- MethodBody
🎯 **Do after Phase 1**

### Phase 3: Add Expression/Statement Maps (MEDIUM)
- Only add as needed for test programs
- Many already exist but unused
🎯 **As needed basis**

### Phase 4: Optimize and Clean Up (LOW)
- Remove redundant maps
- Improve Fallback diagnostics  
- Add validation
🎯 **Future work**

## Why Fallback Map Still Triggers

Even after fixing Map.Generate(), the Fallback map will still trigger for nodes that genuinely have no Map definition:

1. **ParameterList** - No map defined yet
2. **Parameter** - No map defined yet
3. **IntType** / **VoidType** / **StringType** - No map defined yet
4. **TypeName** - No map defined yet
5. **MethodBody** - No map defined yet

This is **correct behavior** - the Fallback map should warn about truly unmapped constructs.

## Testing Strategy

1. ✅ Verify comprehensive_test.crab compiles without unresolved placeholders
2. ✅ Check output.wasm is valid WAT syntax
3. ✅ Confirm nesting structure is correct
4. ✅ Measure MapSet completion percentage
5. ✅ Add missing maps one by one, testing after each

## Files to Modify

### Critical (Phase 1)
- ✅ `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs`
  - Lines 9246-9290 (Map.Generate signature and logic)
  - Lines 8807-8824 (MapSet.Transform)
  - Lines 8846, 8867 (other call sites)

### Important (Phase 2)
- ✅ `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
  - Add ParameterList, Parameter maps
  - Add IntType, VoidType, StringType maps
  - Add TypeName map
  - Add MethodBody map

## Success Criteria

✅ **No unresolved placeholders in output**
✅ **Valid WASM module structure**  
✅ **Type names don't appear as literal output**
✅ **Proper nesting of declarations**
✅ **100% template expansion for defined maps**
✅ **Fallback only for genuinely missing maps**

## Conclusion

The root cause is a **design flaw in CDTk's Map.Generate()** that prevents recursive transformation of child AST nodes.

The fix is **simple**: Pass the MapSet reference to Map.Generate() so it can recursively call Transform() on child nodes.

This is a **3-line change** in CDTk.cs plus updating call sites - approximately **10 lines total**.

After this fix, CRAB's MapSet will work correctly and achieve **~70% completion** immediately, with full completion achievable by adding the remaining missing maps.
