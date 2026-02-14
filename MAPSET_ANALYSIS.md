# MapSet Template Expansion Analysis

## Problem Summary

The WASM output from CRAB contains:
1. **Unresolved placeholders** like `{members}`, `{type}`, `{body}` 
2. **Many `nop` instructions** from Fallback map
3. **WARNING messages** like "WARNING: Unmapped C# construct: {type}"
4. **Literal type names** appearing in output (e.g., "ClassMemberDeclarations")

## Root Cause Analysis

### Issue #1: CDTk's Map.Generate() Does Not Recursively Transform Child Nodes

**Location**: `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs` lines 9246-9290

The `Map.Generate(AstNode node)` method extracts placeholder values from AST nodes like this:

```csharp
// Extract field values from the node
foreach (var kv in node.Fields)
{
    var key = kv.Key;
    var v = kv.Value;

    if (v is null) continue;

    if (v is string s)
    {
        vars[key] = s;
    }
    else if (v is IEnumerable<string> ss)
    {
        vars[key] = string.Join(", ", ss);
    }
    else if (v is AstNode child)  // ← THE PROBLEM
    {
        vars[key] = child.Type;   // ← Only extracts type name, doesn't transform!
    }
    else if (v is IEnumerable<AstNode> children)  // ← ALSO A PROBLEM
    {
        vars[key] = string.Join(", ", children.Select(c => c.Type));  // ← Just type names!
    }
    else
    {
        vars[key] = v.ToString() ?? "";
    }
}
```

**What happens:**
- When a Map template contains `{body}` and the AST node has a field `body` that points to a child AstNode
- Instead of recursively generating WASM for that child node
- It just extracts the child's type name (e.g., "ClassBody")
- So the template `{body}` gets replaced with the literal string "ClassBody"

**What should happen:**
- `{body}` should be replaced with the **generated WASM output** of the ClassBody node
- This requires the Map.Generate() to have access to the MapSet and recursively call Transform()

### Issue #2: Compiler.GenerateNode() Processes Children Outside Template Substitution

**Location**: `/home/runner/work/CRAB/CRAB/Dependencies/CDTk/Boilerplate/CDTk.cs` lines 10150-10181

```csharp
private void GenerateNode(AstNode node, MapSet mapping, StringBuilder output)
{
    // Try mapping by node (exact node-type maps or pattern fallback)
    var transformed = mapping.Transform(node);

    if (transformed != null)
    {
        output.AppendLine(transformed);  // ← Writes parent with unresolved placeholders
    }
    else
    {
        output.AppendLine($"// {node.Type}");
    }

    // Process child nodes  ← This happens AFTER parent is already written!
    foreach (var field in node.Fields)
    {
        if (field.Value is AstNode childNode)
        {
            GenerateNode(childNode, mapping, output);  // ← Recursive call
        }
        else if (field.Value is IEnumerable<AstNode> childNodes)
        {
            foreach (var child in childNodes)
            {
                GenerateNode(child, mapping, output);
            }
        }
    }
}
```

**The problem:**
1. Parent template is expanded and written to output
2. Placeholders like `{body}` are replaced with type names (due to Issue #1)
3. Then child nodes are recursively processed and their output is appended **after** the parent
4. But there's no mechanism to substitute that child output back into the parent's placeholders!

**Result:**
```wasm
;; class {name}
(type ${name} (struct
ClassBody    ← Placeholder was replaced with type name
))
;; More output from recursive processing of ClassBody appears here
{members}    ← Unresolved placeholder
```

### Issue #3: Fallback Map Implementation

**Location**: `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs` (needs to be checked for Fallback map)

When CDTk can't find a Map for a node type, it falls back to a "Fallback" map if defined.

**Current behavior:**
- Many warnings like "WARNING: Unmapped C# construct: {type}"
- Indicates Fallback map is defined and being used extensively
- Fallback map likely outputs `nop` instructions

Let me verify this...

## Detailed Inspection of MapSet.cs

### Map Definitions Found

Looking at `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`:

**Module Structure:**
- ✓ CompilationUnit
- ✓ NamespaceMemberDeclarations
- ✓ NamespaceMemberDeclaration  
- ✓ NamespaceDeclaration
- ✓ NamespaceBody

**Type Declarations:**
- ✓ TypeDeclaration
- ✓ ClassDeclaration
- ✓ ClassBody
- ✓ ClassMemberDeclarations
- ✓ ClassMemberDeclaration
- ✓ StructDeclaration
- ✓ StructBody
- ✓ StructMemberDeclarations
- ✓ StructMemberDeclaration

**Member Declarations:**
- ✓ MethodDeclaration
- ✓ FieldDeclaration
- ✓ ConstructorDeclaration

**Statements:**
- ✓ Statement
- ✓ EmbeddedStatement
- ✓ Block
- ✓ Statements
- ✓ EmptyStatement
- ✓ LabeledStatement
- ✓ DeclarationStatement
- ✓ ExpressionStatement
- ✓ SelectionStatement
- ✓ IfStatement
- ✓ SwitchStatement
- ✓ IterationStatement
- ✓ WhileStatement
- ✓ DoStatement
- ✓ ForStatement
- ✓ ForEachStatement
- ✓ JumpStatement
- ✓ BreakStatement
- ✓ ContinueStatement
- ✓ GotoStatement
- ✓ ReturnStatement
- ✓ ThrowStatement
- ✓ TryStatement
- ✓ LocalVariableDeclaration
- ✓ LocalConstantDeclaration

**Expressions:**
- ✓ Expression
- ✓ AssignmentExpression
- ✓ NonAssignmentExpression
- ✓ ConditionalExpression
- ✓ NullCoalescingExpression
- ✓ LogicalOrExpression
- ✓ LogicalAndExpression
- ✓ BitwiseOrExpression
- ✓ BitwiseXorExpression
- ✓ BitwiseAndExpression
- ✓ EqualityExpression
- ✓ RelationalExpression
- ✓ ShiftExpression
- ✓ AdditiveExpression
- ✓ MultiplicativeExpression
- ✓ SwitchExpression
- ✓ RangeExpression
- ✓ UnaryExpression

## Missing Maps Analysis

To identify missing maps, we need to:
1. Compare RuleSet.cs rule names with MapSet.cs map names
2. Identify which AST node types are being generated but have no corresponding Map

Let me check what rules exist vs what maps exist...

## Example of Problem in Output

From `output.wasm`:
```wasm
(module
  ;; Imports
  (import "env" "memory" (memory 1))
  
  ;; Generated members
{members}          ← Unresolved! Should be replaced with actual member definitions
)
{item}             ← Unresolved! Probably from CompilationUnitItem

;; WARNING: Unmapped C# construct: {type}
nop                ← Fallback map fired

ClassMemberDeclarations  ← Type name instead of generated WASM!
{members}          ← Unresolved!
{member}           ← Unresolved!
{type}             ← Unresolved!
```

## What Needs To Be Fixed

### Fix #1: Modify CDTk's Map.Generate() to Recursively Transform

**Problem**: Map.Generate() needs access to the MapSet to recursively transform child nodes.

**Solution Options**:

A. **Modify CDTk.cs** to pass MapSet reference to Map.Generate():
   ```csharp
   internal string Generate(AstNode node, MapSet mapSet)
   {
       // When extracting child AstNode fields:
       if (v is AstNode child)
       {
           vars[key] = mapSet.Transform(child) ?? child.Type;
       }
       else if (v is IEnumerable<AstNode> children)
       {
           vars[key] = string.Join("\n", 
               children.Select(c => mapSet.Transform(c) ?? c.Type));
       }
   }
   ```

B. **Modify MapSet.Transform()** to pass itself to Map.Generate():
   ```csharp
   internal string? Transform(AstNode node)
   {
       if (node is null) return null;
       
       if (_mapsByName.TryGetValue(node.Type, out var map))
       {
           return map.Generate(node, this);  // Pass 'this'
       }
       
       if (_mapsByName.TryGetValue("Fallback", out var fallbackMap))
       {
           return fallbackMap.Generate(node, this);
       }
       
       return null;
   }
   ```

### Fix #2: Remove or Fix Compiler.GenerateNode() Child Processing

The recursive child processing in Compiler.GenerateNode() (lines 10166-10180) is redundant if Map.Generate() does recursive transformation.

**Option A**: Remove it entirely (children are handled during template expansion)
**Option B**: Keep it as fallback for nodes with no Map definition

### Fix #3: Identify and Add All Missing Maps

Need to:
1. Parse comprehensive_test.crab to see what C# constructs are used
2. Compare RuleSet.cs rules with MapSet.cs maps
3. Add Map definitions for every missing rule

### Fix #4: Remove or Improve Fallback Map

Current Fallback map is generating warnings and nop instructions.

**Better Fallback**:
```csharp
public Map Fallback = @";; WARNING: Unmapped C# construct: {nodeType}
;; This node type requires explicit WASM mapping implementation
;; Falling back to nop instruction to maintain valid WASM output
nop";
```

But this still isn't right because it uses `{nodeType}` placeholder, which doesn't exist in AST nodes.

**Should be**:
- Either removed entirely (fail fast on unmapped nodes)
- Or improved to show the actual node type from the node itself

## Step-by-Step Action Plan

1. **Investigate comprehensive_test.crab** - See what C# is being compiled
2. **Grep output.wasm for all placeholders** - Find all unresolved template variables
3. **Compare RuleSet vs MapSet** - Find which rules have no corresponding maps
4. **Modify CDTk.cs Map.Generate()** - Add recursive transformation of child nodes
5. **Update MapSet.Transform()** - Pass MapSet reference to Map.Generate()
6. **Add missing Map definitions** - For every rule that appears in AST
7. **Test compilation** - Verify placeholders are now resolved
8. **Measure MapSet completion** - Calculate % of rules with maps

## Expected Outcome

After fixes:
- ✓ No unresolved placeholders (100% template expansion)
- ✓ No Fallback map usage (or minimal, well-documented cases)
- ✓ Valid WASM output with proper structure
- ✓ No type names appearing as literal output
- ✓ Proper nesting of WASM constructs

## Technical Debt in CDTk

The fundamental issue is that CDTk's design assumes:
- Maps can operate on individual nodes in isolation
- Parent-child relationships are handled outside Map.Generate()

But for proper code generation:
- Maps need to recursively compose child outputs
- Parent templates need child outputs substituted in place
- This is a **bottom-up transformation**, not top-down

This is a design limitation in CDTk that affects any compiler using it.
