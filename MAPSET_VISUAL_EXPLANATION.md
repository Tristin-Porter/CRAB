# MapSet Template Expansion: Problem vs Solution

## The Problem: Current Behavior

```
┌─────────────────────────────────────────────────────────────┐
│ Compiler.GenerateNode(ClassDeclaration)                     │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ MapSet.Transform(ClassDeclaration)                          │
│   → Finds Map: ";; class {name}\n(type ${name} (struct\n   │
│                {body}\n))"                                   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ Map.Generate(ClassDeclaration)                              │
│   Extracts fields:                                          │
│     name = "Calculator" ✓                                   │
│     body = AstNode(type="ClassBody")                        │
│            ↓                                                 │
│            vars["body"] = child.Type ❌                      │
│            vars["body"] = "ClassBody"  ← WRONG!             │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ Template substitution:                                      │
│   {name} → "Calculator" ✓                                   │
│   {body} → "ClassBody" ❌ (literal type name!)              │
│                                                              │
│ Result: ";; class Calculator                                │
│         (type $Calculator (struct                           │
│         ClassBody                                           │
│         ))"                                                  │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
                  Write to output ❌
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ THEN recursively process children (TOO LATE!)               │
│   GenerateNode(ClassBody)                                   │
│     → Outputs after parent already written                  │
│     → No way to substitute back into {body} placeholder!    │
└─────────────────────────────────────────────────────────────┘

OUTPUT.WASM:
;; class Calculator
(type $Calculator (struct
ClassBody          ← ❌ Literal type name
))
{members}          ← ❌ Child output appears after, placeholders remain
{member}
...
```

## The Solution: Recursive Transformation

```
┌─────────────────────────────────────────────────────────────┐
│ Compiler.GenerateNode(ClassDeclaration)                     │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ MapSet.Transform(ClassDeclaration)                          │
│   → Finds Map: ";; class {name}\n(type ${name} (struct\n   │
│                {body}\n))"                                   │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ Map.Generate(ClassDeclaration, mapSet) ← Pass MapSet!       │
│   Extracts fields:                                          │
│     name = "Calculator" ✓                                   │
│     body = AstNode(type="ClassBody")                        │
│            ↓                                                 │
│            vars["body"] = mapSet.Transform(child) ✓         │
│                             │                                │
└─────────────────────────────┼────────────────────────────────┘
                              │
                              ▼
            ┌─────────────────────────────────────────┐
            │ MapSet.Transform(ClassBody) [RECURSIVE] │
            │   → Finds Map: "{members}"              │
            └─────────────────────────────────────────┘
                              │
                              ▼
            ┌─────────────────────────────────────────┐
            │ Map.Generate(ClassBody, mapSet)         │
            │   members = List<AstNode>               │
            │            ↓                             │
            │   For each child in members:            │
            │     output += mapSet.Transform(child)   │
            │                   │                      │
            └───────────────────┼──────────────────────┘
                                │
                                ▼
                  ┌─────────────────────────────────────┐
                  │ MapSet.Transform(MethodDeclaration) │
                  │   → Finds Map: "(func ${name} ...)" │
                  └─────────────────────────────────────┘
                                │
                                ▼
                  ┌─────────────────────────────────────┐
                  │ Map.Generate(MethodDeclaration)     │
                  │   → Recursively expands parameters, │
                  │      body, return type, etc.        │
                  │   → Returns complete func definition│
                  └─────────────────────────────────────┘
                                │
                                │
            ┌───────────────────┘
            │
            ▼
┌─────────────────────────────────────────────────────────────┐
│ All recursive calls complete, substitutions bubble up:      │
│                                                              │
│ vars["body"] = ";; Method: Add                              │
│                 (func $Add                                   │
│                   (param $a i32)                             │
│                   (param $b i32)                             │
│                   (result i32)                               │
│                   nop                                        │
│                 )                                            │
│                 ;; Method: PrintResult                       │
│                 (func $PrintResult ...)"                     │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────┐
│ Template substitution:                                      │
│   {name} → "Calculator" ✓                                   │
│   {body} → [full WASM from recursive call] ✓                │
│                                                              │
│ Result: ";; class Calculator                                │
│         (type $Calculator (struct                           │
│           ;; Method: Add                                    │
│           (func $Add                                        │
│             (param $a i32)                                  │
│             (param $b i32)                                  │
│             (result i32)                                    │
│             nop                                             │
│           )                                                 │
│           ;; Method: PrintResult                            │
│           (func $PrintResult ...)                           │
│         ))"                                                 │
└─────────────────────────────────────────────────────────────┘
                           │
                           ▼
                  Write to output ✅
                           │
                           ▼
                    PERFECT WASM! ✅

OUTPUT.WASM:
(module
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

## Key Difference

### Before (BROKEN)
```
Map.Generate(node) {
    if (field is AstNode child) {
        vars[key] = child.Type;  // ❌ Just type name
    }
}
```

**Result**: `{body}` → `"ClassBody"` (literal string)

### After (FIXED)
```
Map.Generate(node, mapSet) {
    if (field is AstNode child) {
        vars[key] = mapSet.Transform(child);  // ✅ Recursive!
    }
}
```

**Result**: `{body}` → `"(func $Add ...) (func $PrintResult ...)"` (actual WASM)

## Code Changes Required

### CDTk.cs - Map.Generate()
```csharp
// Line ~9246: Add mapSet parameter
internal string Generate(AstNode node, MapSet? mapSet = null)

// Lines ~9269-9280: Fix child handling
else if (v is AstNode child)
{
    if (mapSet != null)
        vars[key] = mapSet.Transform(child) ?? child.Type;
    else
        vars[key] = child.Type;
}
else if (v is IEnumerable<AstNode> children)
{
    if (mapSet != null)
    {
        var outputs = new List<string>();
        foreach (var c in children)
        {
            var output = mapSet.Transform(c);
            if (!string.IsNullOrWhiteSpace(output))
                outputs.Add(output);
        }
        vars[key] = string.Join("\n", outputs);
    }
    else
        vars[key] = string.Join(", ", children.Select(c => c.Type));
}
```

### CDTk.cs - MapSet.Transform()
```csharp
// Line ~8814: Pass 'this' to Generate
internal string? Transform(AstNode node)
{
    if (node is null) return null;
    
    if (_mapsByName.TryGetValue(node.Type, out var map))
    {
        return map.Generate(node, this);  // ← Add 'this'
    }
    
    if (_mapsByName.TryGetValue("Fallback", out var fallbackMap))
    {
        return fallbackMap.Generate(node, this);  // ← Add 'this'
    }
    
    return null;
}
```

### CDTk.cs - Other Call Sites
```csharp
// Line 8846
return map.Generate(dummyNode, this);  // ← Add 'this'

// Line 8867
semantics.Map(mapName, (ctx, node, ct) => map.Generate(node, this));  // ← Add 'this'
```

## Impact

| Change | Lines Modified | Impact |
|--------|----------------|--------|
| Map.Generate signature | 1 | Enables recursive transformation |
| Map.Generate child handling | ~12 | Fixes template expansion |
| MapSet.Transform calls | 2 | Passes MapSet reference |
| Other call sites | 2 | Consistency |
| **TOTAL** | **~17 lines** | **100% template expansion** ✅ |

## Bottom Line

This is a **bottom-up tree transformation problem**:
- Parent templates reference children via placeholders
- Children must be transformed **before** parent substitution
- Current code does **top-down** (parent before children)
- Fix enables **bottom-up** (children before parent)

**Result**: Perfect recursive composition of WASM output ✅
