# MapSet.cs Rewrite - Executive Summary

## Quick Facts

- **File:** `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
- **Total Lines:** 2,924
- **Total Maps:** 450+
- **Imperative Methods to Delete:** 11 (~538 lines)
- **Functional Maps to Rewrite:** 2 (MethodDeclaration, ConstructorDeclaration)
- **Functional Maps Already Correct:** 1 (IfStatement)
- **Template Maps (No Changes):** 447+

---

## The Problem

MapSet.cs currently violates the functional Map API architecture in two ways:

### 1. Imperative Helper Methods (11 methods)

These methods manually walk AST nodes, inspect `node.Type`, read `node.Fields`, and perform recursive traversal:

- `MapCSharpTypeToWasm` - Type mapping
- `ProcessCompilationUnitItem` - AST traversal
- `ProcessNamespaceItem` - AST traversal  
- `ProcessTypeDeclaration` - AST traversal
- `ProcessClassMemberDeclaration` - AST traversal
- `EmitMethodDeclarationInline` - Code generation
- `ExtractTypeFromNode` - Recursive AST walk
- `EmitParameterList` - AST traversal
- `EmitSingleParameter` - AST field extraction
- `MapTypeNodeToWasm` - Type mapping wrapper
- `ProcessNamespaceDeclarationInline` - AST traversal

**Total:** ~538 lines of imperative code

### 2. Two Functional Maps Access AST

`MethodDeclaration` and `ConstructorDeclaration` currently:
- Access `self.Node` directly
- Read `node.Fields` manually
- Call imperative helper methods
- Extract information from AST structure

These violate the principle: **Maps must NOT access AST nodes directly.**

---

## The Solution

### Phase 1: Delete Imperative Methods ✂️

Remove all 11 helper methods:
- Lines 30-41: `MapCSharpTypeToWasm`
- Lines 294-331: `ProcessCompilationUnitItem`
- Lines 349-365: `ProcessNamespaceItem`
- Lines 371-443: `ProcessTypeDeclaration`
- Lines 448-463: `ProcessClassMemberDeclaration`
- Lines 469-541: `EmitMethodDeclarationInline`
- Lines 709-764: `ExtractTypeFromNode`
- Lines 1428-1494: `EmitParameterList`
- Lines 1496-1550: `EmitSingleParameter`
- Lines 1552-1558: `MapTypeNodeToWasm`
- Lines 2407-2530: `ProcessNamespaceDeclarationInline`

### Phase 2: Add Semantic Metadata Classes 📊

Add to WASM class:

```csharp
public MethodInfo MethodMetadata { get; set; } = new MethodInfo();
public TypeInfo TypeMetadata { get; set; } = new TypeInfo();  // Extend existing
```

Define new classes:

```csharp
public class MethodInfo
{
    public Dictionary<string, string> MethodNames { get; set; } = new();
    public Dictionary<string, string> ReturnTypes { get; set; } = new();
    public Dictionary<string, bool> HasParameters { get; set; } = new();
}

public class TypeInfo  // Extend existing
{
    public Dictionary<string, string> NodeIdToWasmType { get; set; } = new();
    public Dictionary<string, string> ConstructorClasses { get; set; } = new();
    public Dictionary<string, bool> ConstructorHasParams { get; set; } = new();
}
```

### Phase 3: Create Semantic Models 🔍

Create Models that analyze AST and populate metadata:

```csharp
public class MethodAnalysisModel : Model
{
    public override object Build(object input)
    {
        var methodInfo = new MethodInfo();
        // Walk AST once, extract all method info
        // Store in dictionaries by node ID
        return methodInfo;
    }
}

public class TypeAnalysisModel : Model
{
    public override object Build(object input)
    {
        var typeInfo = new TypeInfo();
        // Walk AST once, extract all type info
        // Store in dictionaries by node ID
        return typeInfo;
    }
}
```

### Phase 4: Rewrite Functional Maps 🔄

**MethodDeclaration - Before:**
```csharp
public Map MethodDeclaration => new Map(
    (MapReference self) =>
    {
        var node = self.Node;  // ❌ AST access
        var returnType = ExtractTypeFromNode(node.Fields["returnType"]);  // ❌
        var name = node.Fields["name"].Fields["lexeme"].ToString();  // ❌
        var params = EmitParameterList(node.Fields["parameters"]);  // ❌
        // ...
    }
);
```

**MethodDeclaration - After:**
```csharp
public Map MethodDeclaration => new Map(
    (Func<string> returnType, Func<string> name, 
     Func<string> parameters, Func<string> body, MapReference self) =>
    {
        // ✅ Read semantic metadata
        var methodName = this.MethodMetadata.MethodNames[self.Id];
        var returnWasmType = this.MethodMetadata.ReturnTypes[self.Id];
        
        // ✅ Call child formatters
        var paramsWat = parameters();
        var bodyWat = body();
        
        // ✅ Format with semantic context
        return $@"(func ${methodName}
  {paramsWat}
  (result {returnWasmType})
  {bodyWat}
)";
    }
);
```

Same pattern for `ConstructorDeclaration`.

### Phase 5: Integration 🔗

Update compiler pipeline:

```csharp
// 1. Parse source code
var ast = parser.Parse(sourceCode);

// 2. Run Models to populate semantic metadata
var mapSet = new WASM();
mapSet.TypeMetadata = new TypeAnalysisModel().Build(ast) as TypeInfo;
mapSet.MethodMetadata = new MethodAnalysisModel().Build(ast) as MethodInfo;
mapSet.StringInfo = new StringAnalysisModel().Build(ast) as StringLiteralInfo;
mapSet.LocalVarInfo = new LocalVariableModel().Build(ast) as LocalVariableInfo;

// 3. Run Maps to generate WASM
var wasmOutput = mapSet.Transform(ast);
```

---

## Architecture Compliance

### Before Rewrite ❌

**Violations:**
- Maps access `self.Node` (2 Maps)
- Maps access `node.Fields` (2 Maps)
- Maps inspect `node.Type` (2 Maps)
- Maps call imperative helpers (2 Maps)
- Helper methods walk AST (11 methods)
- Type info extracted from AST in Maps
- Parameter formatting uses AST directly

**Total AST Access Points:** ~90

### After Rewrite ✅

**Compliance:**
- ✅ Zero Maps access AST nodes
- ✅ Zero Maps inspect node.Type or node.Fields
- ✅ Zero imperative helper methods in MapSet
- ✅ All semantic info from Models
- ✅ All Maps use child formatters + `this.*`
- ✅ Pure functional Maps
- ✅ Complete separation: Models analyze, Maps format

**Total AST Access Points:** 0 (in Maps)

---

## Benefits

### 1. Architecture Compliance
- Maps become pure, functional, AST-free
- Structure from SPPF, semantics from Models
- CDTk principles fully enforced

### 2. Unlimited Extensibility
- Add any semantic fields to WASM class
- Create any Models for analysis
- No framework limitations

### 3. Dialect Switching
- Same semantic analysis, different Maps
- Generate Python, JavaScript, C, ARM, etc.
- No AST changes needed

### 4. Maintainability
- Clear separation of concerns
- Models: Analyze AST once
- Maps: Format based on metadata
- No imperative spaghetti code

### 5. Testability
- Test Models independently
- Test Maps with mock metadata
- No AST dependencies in formatting

---

## Effort Estimate

| Phase | Task | Time |
|-------|------|------|
| 1 | Delete 11 imperative methods | 30 min |
| 2 | Add semantic metadata classes | 1 hour |
| 3 | Create 2 semantic Models | 1-2 days |
| 4 | Rewrite 2 functional Maps | 2-3 hours |
| 5 | Integration & testing | 1-2 days |
| **Total** | **Complete rewrite** | **3-5 days** |

---

## Success Criteria

### Code Verification
- [ ] Zero occurrences of `self.Node` in Maps
- [ ] Zero occurrences of `node.Fields` in Maps
- [ ] Zero occurrences of `node.Type` in Maps
- [ ] Zero imperative helper methods
- [ ] All type info from `this.TypeMetadata`
- [ ] All method info from `this.MethodMetadata`
- [ ] Models run before Maps
- [ ] WASM output identical

### Architecture Verification
- [ ] Maps only call child formatters
- [ ] Maps only read `this.*` fields
- [ ] No AST access in Maps
- [ ] Semantic fields user-defined
- [ ] Models separate from Maps
- [ ] CDTk principles enforced

---

## Files Generated

This analysis has produced:

1. **MapSet_Rewrite_Analysis.md** - Complete analysis (this file)
   - Overview of current state
   - Detailed rewrite plan
   - All Maps categorized
   - Semantic fields documented
   - Before/after examples

2. **MapSet_Deletion_List.md** - Line-by-line deletion guide
   - All 11 methods to delete
   - Exact line numbers
   - Code snippets
   - Deletion reasons

3. **MapSet_Functional_Maps_Rewrite.md** - Detailed Map rewrite guide
   - IfStatement (correct example)
   - MethodDeclaration (needs rewrite)
   - ConstructorDeclaration (needs rewrite)
   - Before/after code
   - Required semantic metadata
   - Required Models

4. **MapSet_Rewrite_Summary.md** - Executive summary (this file)
   - Quick facts
   - Problem statement
   - Solution overview
   - Effort estimate

---

## Next Steps

### Immediate Actions

1. **Review Documents**
   - Read all 4 generated analysis files
   - Understand current violations
   - Review proposed solution

2. **Create Models First**
   - Implement `MethodAnalysisModel`
   - Implement extended `TypeAnalysisModel`
   - Test Models independently

3. **Delete Imperative Code**
   - Remove all 11 helper methods
   - Clean up any unused imports
   - Verify compilation errors show where Maps need updates

4. **Rewrite Maps**
   - Update `MethodDeclaration`
   - Update `ConstructorDeclaration`
   - Keep `IfStatement` as-is

5. **Integrate & Test**
   - Update compiler pipeline
   - Run Models before Maps
   - Verify WASM output
   - Run full test suite

### Long-term Goals

- Add more semantic Models (optimization, memory analysis, etc.)
- Extend dialect support (Python, JavaScript, C, etc.)
- Add more semantic fields as needed
- Keep Maps pure and functional

---

## Questions?

Refer to:
- **FUNCTIONAL_MAP_API.md** - Architecture overview
- **MapSet_Rewrite_Analysis.md** - Detailed technical analysis
- **MapSet_Deletion_List.md** - What to delete
- **MapSet_Functional_Maps_Rewrite.md** - How to rewrite Maps

All documentation enforces the same principle:

> **Maps must NEVER access AST nodes directly.  
> All semantic info comes from Models.  
> All structure comes from SPPF.  
> Maps only call child formatters and read semantic metadata.**

---

## Conclusion

The rewrite transforms MapSet.cs from an imperative, AST-walking code generator into a pure functional Map API that:

✅ Never accesses AST  
✅ Uses semantic metadata from Models  
✅ Calls child formatters only  
✅ Provides unlimited extensibility  
✅ Enables dialect switching  
✅ Maintains architecture compliance  

**Estimated effort:** 3-5 days  
**Impact:** Complete architecture compliance  
**Benefit:** Unlimited extensibility and maintainability  
