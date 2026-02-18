# MapSet.cs Complete Rewrite Analysis

## Executive Summary

**File:** `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
**Lines:** 2,924
**Total Maps:** 450+
**Functional Maps (typed):** 3 (MethodDeclaration, ConstructorDeclaration, IfStatement)
**Template Maps (declarative):** 447+
**Imperative Helper Methods:** 11
**AST Node Access Points:** ~90 (spread across helper methods and 3 functional Maps)

---

## Part 1: Methods to DELETE (Old Imperative Code)

All of these methods manually walk AST nodes, inspect `node.Type`, read `node.Fields`, and perform recursive traversal. They must be completely removed.

### 1.1 Primary Imperative Methods

| Line | Method | Purpose | Why Delete |
|------|--------|---------|------------|
| 30 | `MapCSharpTypeToWasm(string)` | Type mapping | Should be in semantic Model, not Map helper |
| 294 | `ProcessCompilationUnitItem(AstNode)` | AST traversal | Manually walks AST, inspects node.Type |
| 349 | `ProcessNamespaceItem(AstNode)` | AST traversal | Manually walks AST, inspects node.Type |
| 371 | `ProcessTypeDeclaration(AstNode)` | AST traversal | Manually walks AST, extracts fields manually |
| 448 | `ProcessClassMemberDeclaration(AstNode, StringBuilder)` | AST traversal | Manually walks AST, builds output imperatively |
| 469 | `EmitMethodDeclarationInline(AstNode)` | Code generation | Manually accesses node.Fields, builds WASM imperatively |
| 711 | `ExtractTypeFromNode(AstNode)` | Type extraction | Recursively walks AST to find type info |
| 1431 | `EmitParameterList(object?)` | Parameter formatting | Manually walks parameter AST nodes |
| 1499 | `EmitSingleParameter(object?)` | Parameter formatting | Manually extracts type and name from AST |
| 1555 | `MapTypeNodeToWasm(AstNode)` | Type mapping | Wrapper for ExtractTypeFromNode |
| 2410 | `ProcessNamespaceDeclarationInline(AstNode)` | AST traversal | Massive imperative method with workarounds |

### 1.2 Total Lines to Delete

Estimated **~800-1000 lines** of imperative code to remove.

---

## Part 2: Current Map Definitions

### 2.1 Template Maps (Declarative) - KEEP THESE

These are pure declarative Maps that use placeholders like `{name}`, `{body}`, `{expr}`:

**Module Structure:**
- `CompilationUnit` (line 257) - Module wrapper with imports
- `NamespaceMemberDeclarations` (line 335)
- `NamespaceMemberDeclaration` (line 338)
- `NamespaceDeclaration` (line 344)
- `NamespaceBody` (line 545)
- `NamespaceBodyItem` (line 548)
- `TypeDeclaration` (line 555)

**Type Declarations:**
- `ClassDeclaration` (line 575)
- `ClassBody` (line 584)
- `ClassMemberDeclarations` (line 587)
- `ClassMemberDeclaration` (line 590)
- `StructDeclaration` (line 593)
- `StructBody` (line 599)
- `FieldDeclaration` (line 767)

**Statements:** (60+ Maps)
- `Statement`, `EmbeddedStatement`, `Block`, `Statements`
- `EmptyStatement`, `LabeledStatement`, `DeclarationStatement`
- `ExpressionStatement`, `SelectionStatement`
- `WhileStatement`, `DoStatement`, `ForStatement`, `ForEachStatement`
- `BreakStatement`, `ContinueStatement`, `GotoStatement`, `ReturnStatement`
- `ThrowStatement`, `TryStatement`

**Expressions:** (150+ Maps)
- `Expression`, `AssignmentExpression`, `NonAssignmentExpression`
- `ConditionalExpression`, `NullCoalescingExpression`
- Binary operators: `LogicalOrExpression`, `LogicalAndExpression`, etc.
- Unary operators: `UnaryExpression`, `UnaryOperatorExpression`
- Primary expressions: `ParenthesizedExpression`, `MemberAccessExpression`
- `InvocationExpression`, `ElementAccessExpression`
- Object creation: `ObjectCreationExpression`, `ArrayCreationExpression`

**Types:** (50+ Maps)
- Primitive keywords: `KwInt`, `KwLong`, `KwFloat`, `KwDouble`, etc.
- Reference types: `KwObject`, `KwString`, `KwDecimal`, `KwDynamic`
- Type modifiers: `KwVar`, `KwClass`, `KwStruct`, `KwInterface`, `KwEnum`
- Access modifiers: `KwPublic`, `KwPrivate`, `KwProtected`, `KwInternal`

**Literals:** (20+ Maps)
- `IntegerLiteral`, `HexLiteral`, `BinaryLiteral`
- `FloatLiteral`, `CharacterLiteral`, `StringLiteral`
- `BooleanLiteral`, `NullLiteral`

**Tokens:**
- Delimiters: `OpenBrace`, `CloseBrace`, `Semicolon`, `Comma`, etc.

### 2.2 Functional Maps (Typed) - REWRITE THESE

These 3 Maps currently access AST nodes directly and must be rewritten:

| Map | Line | Current Issues | Rewrite Plan |
|-----|------|----------------|--------------|
| `MethodDeclaration` | 619 | Accesses `self.Node`, `node.Fields`, calls `ExtractTypeFromNode`, `EmitParameterList` | Rewrite to use child formatters + semantic metadata |
| `ConstructorDeclaration` | 774 | Accesses `self.Node`, `node.Fields`, calls `EmitParameterList` | Rewrite to use child formatters + semantic metadata |
| `IfStatement` | 880 | Uses child formatters correctly! Shows semantic context usage (`this.OptHints`, `this.Dialect`, `this.Minify`) | **GOOD EXAMPLE** - keep this pattern! |

**Note:** `IfStatement` is already correctly implemented - it only calls child formatters and reads semantic metadata!

---

## Part 3: Semantic Metadata Fields (Already Defined)

These user-defined fields are already present in the WASM class:

### 3.1 Core Semantic Fields

```csharp
public string Dialect { get; set; } = "WASM";
public bool Minify { get; set; } = false;
public OptimizationHints OptHints { get; set; } = new OptimizationHints();
public StringLiteralInfo StringInfo { get; set; } = new StringLiteralInfo();
public LocalVariableInfo LocalVarInfo { get; set; } = new LocalVariableInfo();
```

### 3.2 OptimizationHints Class (lines 2793-2814)

```csharp
public class OptimizationHints
{
    public Dictionary<string, bool> CanInline { get; set; }
    public Dictionary<string, bool> RequiresBlock { get; set; }
    public Dictionary<string, string> FormattingStyle { get; set; }
}
```

### 3.3 StringLiteralInfo Class (lines 2820-2878)

Provides:
- `RegisterString(string)` - Add string to data section
- `GetOffset(int)` - Get memory offset for string ID
- `GetString(int)` - Retrieve string by ID
- `CalculateHeapStart()` - Calculate heap pointer start
- `GenerateDataSection()` - Generate WASM data section

### 3.4 LocalVariableInfo Class (lines 2884-2923)

Provides:
- `SetCurrentFunction(string)` - Set context
- `RegisterVariable(string, string)` - Add variable
- `GetVariableType(string, string)` - Get variable WASM type
- `GetFunctionVariables(string)` - Get all function locals

### 3.5 Model Integration (lines 160-240)

```csharp
public Automatic AutomaticModel => new Automatic(__AllRules!, __Ast!);
public Manual ManualModel => new Manual(__AllRules!, __Ast!);
public Optimization OptimizationModel => new Optimization(__AllRules!, __Ast!);

private AutomaticAnnotations? GetAutomaticAnnotations()
private ManualAnnotations? GetManualAnnotations()
private OptimizationAnnotations? GetOptimizationAnnotations()
```

---

## Part 4: Rewrite Plan

### 4.1 Phase 1: Delete Imperative Code

**DELETE these 11 methods entirely:**

1. `MapCSharpTypeToWasm` (line 30)
2. `ProcessCompilationUnitItem` (line 294)
3. `ProcessNamespaceItem` (line 349)
4. `ProcessTypeDeclaration` (line 371)
5. `ProcessClassMemberDeclaration` (line 448)
6. `EmitMethodDeclarationInline` (line 469)
7. `ExtractTypeFromNode` (line 711)
8. `EmitParameterList` (line 1431)
9. `EmitSingleParameter` (line 1499)
10. `MapTypeNodeToWasm` (line 1555)
11. `ProcessNamespaceDeclarationInline` (line 2410)

**Impact:** Removes ~800-1000 lines of imperative code.

### 4.2 Phase 2: Add Missing Semantic Fields

Type information must come from semantic Models, not AST traversal. Add to WASM class:

```csharp
// NEW semantic fields needed for functional Maps
public TypeInfo TypeMetadata { get; set; } = new TypeInfo();
public MethodInfo MethodMetadata { get; set; } = new MethodInfo();
public ParameterInfo ParamMetadata { get; set; } = new ParameterInfo();
```

Define new info classes:

```csharp
public class TypeInfo
{
    public Dictionary<string, string> NodeIdToWasmType { get; set; } = new();
    public string GetWasmType(string nodeId) => NodeIdToWasmType.GetValueOrDefault(nodeId, "i32");
}

public class MethodInfo
{
    public Dictionary<string, string> MethodNames { get; set; } = new();
    public Dictionary<string, string> MethodReturnTypes { get; set; } = new();
    public Dictionary<string, List<string>> MethodParameters { get; set; } = new();
}

public class ParameterInfo
{
    public Dictionary<string, string> ParameterNames { get; set; } = new();
    public Dictionary<string, string> ParameterTypes { get; set; } = new();
}
```

### 4.3 Phase 3: Rewrite Functional Maps

#### 4.3.1 MethodDeclaration (line 619)

**BEFORE (AST-accessing):**
```csharp
public Map MethodDeclaration => new Map(
    (MapReference self) =>
    {
        var node = self.Node;  // ❌ Accesses AST directly
        var returnTypeField = node.Fields["returnType"];  // ❌ Reads AST fields
        string resultType = ExtractTypeFromNode(typeNode);  // ❌ AST traversal
        parameters = EmitParameterList(parametersField);  // ❌ AST traversal
        // ...
    }
);
```

**AFTER (functional):**
```csharp
public Map MethodDeclaration => new Map(
    (Func<string> mods, Func<string> returnType, Func<string> name, 
     Func<string> parameters, Func<string> body, MapReference self) =>
    {
        // ✅ Read semantic metadata only
        var methodName = this.MethodMetadata.MethodNames[self.Id];
        var returnWasmType = this.MethodMetadata.MethodReturnTypes[self.Id];
        
        // ✅ Call child formatters only
        var paramsWat = parameters();
        var bodyWat = body();
        
        // ✅ Format based on semantic context
        if (this.Minify)
            return $"(func ${methodName}{paramsWat}(result {returnWasmType}){bodyWat})";
        
        return $@"(func ${methodName}
  {paramsWat}
  (result {returnWasmType})
  {bodyWat}
)";
    }
);
```

#### 4.3.2 ConstructorDeclaration (line 774)

**BEFORE (AST-accessing):**
```csharp
public Map ConstructorDeclaration => new Map(
    (MapReference self) =>
    {
        var node = self.Node;  // ❌ Accesses AST
        var nameField = node.Fields["name"];  // ❌ Reads fields
        parameters = EmitParameterList(parametersField);  // ❌ AST traversal
        // ...
    }
);
```

**AFTER (functional):**
```csharp
public Map ConstructorDeclaration => new Map(
    (Func<string> name, Func<string> parameters, Func<string> body, MapReference self) =>
    {
        // ✅ Read semantic metadata
        var className = this.TypeMetadata.GetClassName(self.Id);
        
        // ✅ Call child formatters
        var paramsWat = parameters();
        var bodyWat = body();
        
        return $@"(func ${className}_ctor
  (param $this (ref ${className}))
  {paramsWat}
  {bodyWat}
)";
    }
);
```

### 4.4 Phase 4: Fix Template Maps That Reference Deleted Methods

These template Maps may need adjustment:

1. **CompilationUnit** (line 257) - Currently has static boilerplate. Should become typed Map to:
   - Call `this.StringInfo.GenerateDataSection()`
   - Call `this.StringInfo.CalculateHeapStart()`
   - Properly format module with data section

2. **FormalParameterList** (line 1426) - Currently uses `{params}` placeholder, but needs to format multiple parameters. Should become typed Map that:
   - Calls `parameters()` child formatter multiple times
   - Joins with newlines
   - No AST access

3. **FixedParameter** (line 1564) - Currently uses `"(param ${name} {type})"` which is correct, but type extraction happens in deleted methods. Needs:
   - Semantic metadata for parameter types
   - `this.ParamMetadata.GetType(self.Id)`

### 4.5 Phase 5: Create Semantic Analysis Models

Need to create Models that populate the new semantic fields:

```csharp
public class TypeAnalysisModel : Model
{
    public override object Build(object input)
    {
        var typeInfo = new TypeInfo();
        // Walk AST and populate typeInfo.NodeIdToWasmType
        // Map C# types to WASM types
        // Store in dictionary by node ID
        return typeInfo;
    }
}

public class MethodAnalysisModel : Model
{
    public override object Build(object input)
    {
        var methodInfo = new MethodInfo();
        // Walk AST and extract:
        // - Method names
        // - Return types (mapped to WASM)
        // - Parameter lists
        return methodInfo;
    }
}
```

Then in compiler pipeline:

```csharp
var mapSet = new WASM();
mapSet.TypeMetadata = typeAnalysisModel.Build(ast) as TypeInfo;
mapSet.MethodMetadata = methodAnalysisModel.Build(ast) as MethodInfo;
mapSet.ParamMetadata = paramAnalysisModel.Build(ast) as ParameterInfo;
```

---

## Part 5: Architecture Compliance

### 5.1 Current Violations

| Issue | Location | Severity |
|-------|----------|----------|
| Maps access `self.Node` | Lines 622, 777 | **CRITICAL** |
| Maps access `node.Fields` | Lines 634-636, 780-782 | **CRITICAL** |
| Maps call imperative helpers | Lines 642, 662, 796 | **CRITICAL** |
| Maps perform AST traversal | Throughout helper methods | **CRITICAL** |
| Type info extracted from AST | Lines 711-764 | **HIGH** |
| Parameter formatting uses AST | Lines 1431-1549 | **HIGH** |

### 5.2 After Rewrite

✅ **All Maps will:**
- Call child formatters via `Func<string>` parameters
- Read semantic metadata via `this.*` fields
- Never access AST nodes directly
- Never inspect `node.Type` or `node.Fields`
- Be pure, declarative, and functional

✅ **All semantic info will:**
- Come from Models that run BEFORE Maps
- Be stored in user-defined fields on MapSet
- Be accessed type-safely via `this.*`

✅ **Architecture will:**
- Separate concerns: Models analyze, Maps format
- Enforce that Maps cannot access AST
- Provide unlimited extensibility via user-defined fields
- Enable dialect switching without changing AST

---

## Part 6: Recommended Execution Plan

### Step 1: Create Semantic Models (1-2 days)
- `TypeAnalysisModel` - Map C# types to WASM types by node ID
- `MethodAnalysisModel` - Extract method signatures
- `ParameterAnalysisModel` - Extract parameter info

### Step 2: Add Semantic Fields (30 minutes)
- Add `TypeInfo`, `MethodInfo`, `ParameterInfo` to WASM class
- Add properties to access these fields

### Step 3: Rewrite 3 Functional Maps (2-3 hours)
- `MethodDeclaration` - Remove AST access, use semantic metadata
- `ConstructorDeclaration` - Remove AST access, use semantic metadata
- Keep `IfStatement` as-is (already correct!)

### Step 4: Delete Imperative Methods (30 minutes)
- Remove all 11 helper methods
- Clean up imports if needed

### Step 5: Fix CompilationUnit (1 hour)
- Convert to typed Map
- Call `this.StringInfo.GenerateDataSection()`
- Call `this.StringInfo.CalculateHeapStart()`

### Step 6: Integration & Testing (1-2 days)
- Update compiler pipeline to populate semantic fields
- Run Models before Maps
- Test WASM output
- Verify no AST access in Maps

**Total Estimated Time:** 5-7 days

---

## Part 7: Success Criteria

### Verification Checklist

- [ ] Zero occurrences of `self.Node` in any Map
- [ ] Zero occurrences of `node.Fields` in any Map
- [ ] Zero occurrences of `node.Type` in any Map
- [ ] Zero private helper methods that walk AST
- [ ] All type info comes from `this.TypeMetadata`
- [ ] All method info comes from `this.MethodMetadata`
- [ ] All parameter info comes from `this.ParamMetadata`
- [ ] Models run before Maps in pipeline
- [ ] Semantic fields populated by Models
- [ ] Maps only call child formatters and read `this.*`
- [ ] WASM output identical to before rewrite
- [ ] Architecture document compliance

### Code Review Focus

1. **No AST access** - Search for `AstNode`, `node.`, `self.Node`
2. **No manual traversal** - Search for recursive calls, loops over fields
3. **No type extraction** - Type info from semantic metadata only
4. **Pure functional** - Maps use only parameters and `this.*`
5. **Separation of concerns** - Models analyze, Maps format

---

## Part 8: Example: Before & After

### BEFORE (Current Code)

```csharp
// IMPERATIVE - directly accesses AST
private static string ExtractTypeFromNode(AstNode typeNode)
{
    if (typeNode == null) return "";
    
    if (typeNode.Fields.ContainsKey("lexeme"))
    {
        var typeName = typeNode.Fields["lexeme"]?.ToString() ?? "";
        return MapCSharpTypeToWasm(typeName);
    }
    
    if (typeNode.Fields.ContainsKey("type"))
    {
        var innerType = typeNode.Fields["type"];
        if (innerType is AstNode innerNode)
        {
            return ExtractTypeFromNode(innerNode);  // Recursive AST walk
        }
    }
    // ... 50+ more lines of AST traversal
}

public Map MethodDeclaration => new Map(
    (MapReference self) =>
    {
        var node = self.Node;  // ❌ AST access
        var typeNode = node.Fields["returnType"] as AstNode;  // ❌ Field access
        string resultType = ExtractTypeFromNode(typeNode);  // ❌ AST traversal
        // ... more imperative code
    }
);
```

### AFTER (Functional)

```csharp
// SEMANTIC MODEL - runs before Maps
public class TypeAnalysisModel : Model
{
    public override object Build(object input)
    {
        var typeInfo = new TypeInfo();
        var ast = input as AstNode;
        
        // Walk AST once, populate type info by node ID
        WalkAndPopulateTypes(ast, typeInfo);
        
        return typeInfo;
    }
}

// FUNCTIONAL MAP - reads semantic metadata only
public Map MethodDeclaration => new Map(
    (Func<string> returnType, Func<string> name, Func<string> parameters, 
     Func<string> body, MapReference self) =>
    {
        // ✅ Read semantic metadata
        var returnWasmType = this.MethodMetadata.ReturnTypes[self.Id];
        var methodName = this.MethodMetadata.Names[self.Id];
        
        // ✅ Call child formatters
        var paramsWat = parameters();
        var bodyWat = body();
        
        // ✅ Format with semantic context
        if (this.Minify)
            return $"(func ${methodName}{paramsWat}(result {returnWasmType}){bodyWat})";
        
        return $@"(func ${methodName}
  {paramsWat}
  (result {returnWasmType})
  {bodyWat}
)";
    }
);
```

---

## Summary

**Current State:**
- 450+ Maps (3 functional, 447+ template)
- 11 imperative helper methods
- ~90 AST access points
- Violations: Maps access AST directly

**Target State:**
- 450+ Maps (all functional or pure template)
- 0 imperative helper methods in MapSet
- 0 AST access in Maps
- Models populate semantic fields
- Maps read `this.*` and call child formatters

**Key Changes:**
1. Delete 11 imperative methods (~800-1000 lines)
2. Add 3 semantic info classes
3. Rewrite 3 functional Maps
4. Create 3 semantic Models
5. Keep 447+ template Maps as-is

**Result:**
- Pure functional Map API
- Complete separation: Models analyze, Maps format
- Architecture compliant
- Unlimited extensibility
- Dialect switching without AST changes
