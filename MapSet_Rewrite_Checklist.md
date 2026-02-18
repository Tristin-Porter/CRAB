# MapSet.cs Rewrite - Quick Checklist

## Pre-Rewrite Preparation

- [ ] Read `MapSet_Rewrite_Summary.md` (executive overview)
- [ ] Read `MapSet_Rewrite_Analysis.md` (detailed technical analysis)
- [ ] Read `MapSet_Deletion_List.md` (what to delete, line by line)
- [ ] Read `MapSet_Functional_Maps_Rewrite.md` (how to rewrite Maps)
- [ ] Read `FUNCTIONAL_MAP_API.md` (architecture principles)
- [ ] Backup current MapSet.cs (git commit)

---

## Phase 1: Delete Imperative Methods (30 minutes)

### Methods to Delete

- [ ] Line 30-41: `MapCSharpTypeToWasm(string)` (12 lines)
- [ ] Line 294-331: `ProcessCompilationUnitItem(AstNode)` (38 lines)
- [ ] Line 349-365: `ProcessNamespaceItem(AstNode)` (17 lines)
- [ ] Line 371-443: `ProcessTypeDeclaration(AstNode)` (73 lines)
- [ ] Line 448-463: `ProcessClassMemberDeclaration(AstNode, StringBuilder)` (16 lines)
- [ ] Line 469-541: `EmitMethodDeclarationInline(AstNode)` (73 lines)
- [ ] Line 709-764: `ExtractTypeFromNode(AstNode)` (56 lines)
- [ ] Line 1428-1494: `EmitParameterList(object?)` (67 lines)
- [ ] Line 1496-1550: `EmitSingleParameter(object?)` (55 lines)
- [ ] Line 1552-1558: `MapTypeNodeToWasm(AstNode)` (7 lines)
- [ ] Line 2407-2530: `ProcessNamespaceDeclarationInline(AstNode)` (124 lines)

**Total: ~538 lines to delete**

### Verification

- [ ] All 11 methods deleted
- [ ] Compilation fails (expected - Maps reference deleted methods)
- [ ] No other compilation errors (no accidental deletions)

---

## Phase 2: Add Semantic Metadata Classes (1 hour)

### Add to WASM Class (around line 114)

```csharp
/// <summary>
/// Method metadata - USER DEFINED field, populated by MethodAnalysisModel.
/// Maps use this to get method names, return types, and parameter info.
/// </summary>
public MethodInfo MethodMetadata { get; set; } = new MethodInfo();
```

### Extend TypeInfo Class (after line 2923)

```csharp
public class MethodInfo
{
    private Dictionary<string, MethodData> _methods = new();
    
    public void RegisterMethod(string nodeId, string name, string returnWasmType, bool hasParameters)
    {
        _methods[nodeId] = new MethodData
        {
            Name = name,
            ReturnWasmType = returnWasmType,
            HasParameters = hasParameters
        };
    }
    
    public MethodData GetInfo(string nodeId)
    {
        return _methods.TryGetValue(nodeId, out var info) 
            ? info 
            : new MethodData { Name = "unknown", ReturnWasmType = "i32", HasParameters = false };
    }
}

public class MethodData
{
    public string Name { get; set; } = "";
    public string ReturnWasmType { get; set; } = "";
    public bool HasParameters { get; set; } = false;
}
```

### Extend Existing TypeInfo Class

Add to existing `TypeInfo` class (around line 2820):

```csharp
public class TypeInfo
{
    private Dictionary<string, string> _nodeIdToWasmType = new();
    private Dictionary<string, ConstructorData> _constructors = new();
    
    public string GetWasmType(string nodeId) => _nodeIdToWasmType.GetValueOrDefault(nodeId, "i32");
    
    public void RegisterConstructor(string nodeId, string className, bool hasParameters)
    {
        _constructors[nodeId] = new ConstructorData
        {
            ClassName = className,
            HasParameters = hasParameters
        };
    }
    
    public ConstructorData GetConstructorInfo(string nodeId)
    {
        return _constructors.TryGetValue(nodeId, out var info)
            ? info
            : new ConstructorData { ClassName = "unknown", HasParameters = false };
    }
}

public class ConstructorData
{
    public string ClassName { get; set; } = "";
    public bool HasParameters { get; set; } = false;
}
```

### Verification

- [ ] `MethodInfo` class added
- [ ] `MethodData` class added
- [ ] `ConstructorData` class added
- [ ] TypeInfo extended with constructor support
- [ ] WASM class has `MethodMetadata` property
- [ ] Code compiles

---

## Phase 3: Create Semantic Models (1-2 days)

### Create MethodAnalysisModel.cs

Location: `/home/runner/work/CRAB/CRAB/Compiler/Models/MethodAnalysisModel.cs`

```csharp
using CDTk;

namespace CRAB;

public class MethodAnalysisModel : Model
{
    private AllRules _rules;
    private Ast _ast;
    
    public MethodAnalysisModel(AllRules rules, Ast ast)
    {
        _rules = rules;
        _ast = ast;
    }
    
    public override object Build(object input)
    {
        var methodInfo = new MethodInfo();
        var ast = input as AstNode;
        
        if (ast != null)
        {
            WalkAndAnalyzeMethods(ast, methodInfo);
        }
        
        return methodInfo;
    }
    
    private void WalkAndAnalyzeMethods(AstNode node, MethodInfo info)
    {
        if (node == null) return;
        
        // Find MethodDeclaration nodes
        if (node.Type == "MethodDeclaration")
        {
            var nodeId = node.Id;
            var name = ExtractMethodName(node);
            var returnType = ExtractReturnType(node);
            var returnWasmType = MapCSharpTypeToWasm(returnType);
            var hasParams = HasParameters(node);
            
            info.RegisterMethod(nodeId, name, returnWasmType, hasParams);
        }
        
        // Recurse into children
        foreach (var field in node.Fields.Values)
        {
            if (field is AstNode child)
                WalkAndAnalyzeMethods(child, info);
            else if (field is List<AstNode> children)
                foreach (var child in children)
                    WalkAndAnalyzeMethods(child, info);
        }
    }
    
    private string ExtractMethodName(AstNode node)
    {
        // TODO: Implement based on grammar
        return "unknown";
    }
    
    private string ExtractReturnType(AstNode node)
    {
        // TODO: Implement based on grammar
        return "int";
    }
    
    private bool HasParameters(AstNode node)
    {
        // TODO: Implement based on grammar
        return false;
    }
    
    private string MapCSharpTypeToWasm(string typeName)
    {
        return typeName switch
        {
            "int" or "uint" or "byte" or "sbyte" or "short" or "ushort" or "bool" or "char" => "i32",
            "long" or "ulong" => "i64",
            "float" => "f32",
            "double" => "f64",
            "void" => "",
            _ => "i32"
        };
    }
}
```

### Extend TypeAnalysisModel.cs

Location: Create or extend `/home/runner/work/CRAB/CRAB/Compiler/Models/TypeAnalysisModel.cs`

```csharp
using CDTk;

namespace CRAB;

public class TypeAnalysisModel : Model
{
    private AllRules _rules;
    private Ast _ast;
    
    public TypeAnalysisModel(AllRules rules, Ast ast)
    {
        _rules = rules;
        _ast = ast;
    }
    
    public override object Build(object input)
    {
        var typeInfo = new TypeInfo();
        var ast = input as AstNode;
        
        if (ast != null)
        {
            WalkAndAnalyzeTypes(ast, typeInfo);
        }
        
        return typeInfo;
    }
    
    private void WalkAndAnalyzeTypes(AstNode node, TypeInfo info)
    {
        if (node == null) return;
        
        // Handle class declarations
        if (node.Type == "ClassDeclaration")
        {
            var className = ExtractClassName(node);
            WalkForConstructors(node, info, className);
        }
        
        // Map type nodes to WASM types
        if (node.Type.Contains("Type"))
        {
            var nodeId = node.Id;
            var wasmType = MapTypeToWasm(node);
            info._nodeIdToWasmType[nodeId] = wasmType;
        }
        
        // Recurse into children
        foreach (var field in node.Fields.Values)
        {
            if (field is AstNode child)
                WalkAndAnalyzeTypes(child, info);
            else if (field is List<AstNode> children)
                foreach (var child in children)
                    WalkAndAnalyzeTypes(child, info);
        }
    }
    
    private void WalkForConstructors(AstNode node, TypeInfo info, string className)
    {
        if (node.Type == "ConstructorDeclaration")
        {
            var nodeId = node.Id;
            var hasParams = HasParameters(node);
            info.RegisterConstructor(nodeId, className, hasParams);
        }
        
        // Recurse
        foreach (var field in node.Fields.Values)
        {
            if (field is AstNode child)
                WalkForConstructors(child, info, className);
            else if (field is List<AstNode> children)
                foreach (var child in children)
                    WalkForConstructors(child, info, className);
        }
    }
    
    private string ExtractClassName(AstNode node)
    {
        // TODO: Implement based on grammar
        return "unknown";
    }
    
    private string MapTypeToWasm(AstNode node)
    {
        // TODO: Implement based on grammar
        return "i32";
    }
    
    private bool HasParameters(AstNode node)
    {
        // TODO: Implement based on grammar
        return false;
    }
}
```

### Verification

- [ ] MethodAnalysisModel.cs created
- [ ] TypeAnalysisModel.cs created or extended
- [ ] Both Models compile
- [ ] Both Models can be instantiated
- [ ] Test Models on sample AST
- [ ] Verify metadata populated correctly

---

## Phase 4: Rewrite Functional Maps (2-3 hours)

### Update MethodDeclaration (Line 619)

**Replace:**
```csharp
public Map MethodDeclaration => new Map(
    (MapReference self) =>
    {
        var node = self.Node;  // OLD: AST access
        // ... 80+ lines of imperative code ...
    }
);
```

**With:**
```csharp
public Map MethodDeclaration => new Map(
    (Func<string> mods, Func<string> returnType, Func<string> name, 
     Func<string> parameters, Func<string> body, MapReference self) =>
    {
        // Read semantic metadata
        var methodInfo = this.MethodMetadata.GetInfo(self.Id);
        var methodName = methodInfo.Name;
        var returnWasmType = methodInfo.ReturnWasmType;
        var hasParameters = methodInfo.HasParameters;
        
        // Call child formatters
        var paramsWat = hasParameters ? parameters() : "";
        var bodyWat = body();
        
        // Format with semantic context
        if (this.Minify)
        {
            var result = returnWasmType != "" ? $"(result {returnWasmType})" : "";
            return $"(func ${methodName}{paramsWat}{result}{bodyWat})";
        }
        
        // Default formatting
        var sb = new System.Text.StringBuilder();
        sb.Append($"(func ${methodName}");
        
        if (!string.IsNullOrWhiteSpace(paramsWat))
        {
            sb.Append("\n  ");
            sb.Append(paramsWat);
        }
        
        if (!string.IsNullOrWhiteSpace(returnWasmType))
        {
            sb.Append($"\n  (result {returnWasmType})");
        }
        
        if (!string.IsNullOrWhiteSpace(bodyWat))
        {
            sb.Append("\n");
            sb.Append(bodyWat);
        }
        
        sb.Append("\n)");
        return sb.ToString();
    }
);
```

### Update ConstructorDeclaration (Line 774)

**Replace:**
```csharp
public Map ConstructorDeclaration => new Map(
    (MapReference self) =>
    {
        var node = self.Node;  // OLD: AST access
        // ... 50+ lines of imperative code ...
    }
);
```

**With:**
```csharp
public Map ConstructorDeclaration => new Map(
    (Func<string> name, Func<string> parameters, Func<string> body, MapReference self) =>
    {
        // Read semantic metadata
        var ctorInfo = this.TypeMetadata.GetConstructorInfo(self.Id);
        var className = ctorInfo.ClassName;
        var hasParameters = ctorInfo.HasParameters;
        
        // Call child formatters
        var paramsWat = hasParameters ? parameters() : "";
        var bodyWat = body();
        
        // Format with semantic context
        if (this.Minify)
        {
            return $"(func ${className}_ctor(param $this (ref ${className})){paramsWat}{bodyWat})";
        }
        
        // Default formatting
        var sb = new System.Text.StringBuilder();
        sb.Append($"(func ${className}_ctor");
        
        // Add 'this' parameter
        sb.Append($"\n  (param $this (ref ${className}))");
        
        // Add other parameters if any
        if (!string.IsNullOrWhiteSpace(paramsWat))
        {
            sb.Append("\n  ");
            sb.Append(paramsWat);
        }
        
        // Emit the body code
        if (!string.IsNullOrWhiteSpace(bodyWat))
        {
            sb.Append("\n");
            sb.Append(bodyWat);
        }
        
        sb.Append("\n)");
        return sb.ToString();
    }
);
```

### Keep IfStatement Unchanged

- [ ] IfStatement already correct (line 880)
- [ ] Uses child formatters
- [ ] Reads semantic context
- [ ] No AST access

### Verification

- [ ] MethodDeclaration rewritten
- [ ] ConstructorDeclaration rewritten
- [ ] Both use child formatters (`Func<string>` parameters)
- [ ] Both read `this.MethodMetadata` / `this.TypeMetadata`
- [ ] Neither accesses `self.Node`
- [ ] Code compiles

---

## Phase 5: Integration & Testing (1-2 days)

### Update Compiler Pipeline

Find where MapSet is instantiated and add Model execution:

```csharp
// Before Maps run, populate semantic metadata
var mapSet = new WASM();

// Run Models
var typeModel = new TypeAnalysisModel(__AllRules, __Ast);
var methodModel = new MethodAnalysisModel(__AllRules, __Ast);

// Populate metadata
mapSet.TypeMetadata = typeModel.Build(ast) as TypeInfo;
mapSet.MethodMetadata = methodModel.Build(ast) as MethodInfo;

// Now run Maps
var wasmOutput = mapSet.Transform(ast);
```

### Testing Checklist

- [ ] Test simple method (no parameters)
- [ ] Test method with parameters
- [ ] Test method with return type
- [ ] Test constructor (no parameters)
- [ ] Test constructor with parameters
- [ ] Test nested classes
- [ ] Test multiple methods
- [ ] Compare WASM output with original
- [ ] Verify no regressions

### Final Verification

- [ ] All tests pass
- [ ] WASM output identical to before
- [ ] No AST access in any Map
- [ ] No imperative helper methods
- [ ] Models run before Maps
- [ ] Semantic fields populated
- [ ] Architecture compliant

---

## Post-Rewrite Verification

### Code Audit

Run these searches - all should return 0 results:

```bash
# Search for AST access in Maps
grep -n "self\.Node" MapSet.cs

# Search for field access in Maps  
grep -n "node\.Fields" MapSet.cs

# Search for type inspection in Maps
grep -n "node\.Type" MapSet.cs

# Search for deleted methods
grep -n "ProcessCompilationUnitItem\|ProcessNamespaceItem\|ProcessTypeDeclaration\|ExtractTypeFromNode\|EmitParameterList\|EmitSingleParameter" MapSet.cs
```

Expected: **0 matches for all searches**

### Architecture Compliance

- [ ] Zero Maps access `self.Node`
- [ ] Zero Maps access `node.Fields`
- [ ] Zero Maps inspect `node.Type`
- [ ] Zero imperative helper methods
- [ ] All type info from `this.TypeMetadata`
- [ ] All method info from `this.MethodMetadata`
- [ ] Models run before Maps
- [ ] Semantic fields user-defined
- [ ] Maps only call child formatters
- [ ] Maps only read `this.*` fields

### Documentation

- [ ] Update code comments
- [ ] Document new semantic fields
- [ ] Document Model usage
- [ ] Update architecture docs
- [ ] Add examples of extending with new fields

---

## Success Metrics

### Quantitative

| Metric | Before | After | Status |
|--------|--------|-------|--------|
| Imperative methods | 11 | 0 | ✅ |
| AST access in Maps | ~90 | 0 | ✅ |
| Functional Maps | 3 | 3 | ✅ |
| Template Maps | 447+ | 447+ | ✅ |
| Lines of imperative code | ~538 | 0 | ✅ |

### Qualitative

- [ ] ✅ Architecture compliant
- [ ] ✅ Pure functional Maps
- [ ] ✅ Semantic metadata from Models
- [ ] ✅ Clear separation of concerns
- [ ] ✅ Unlimited extensibility
- [ ] ✅ Dialect switching enabled
- [ ] ✅ Maintainable codebase
- [ ] ✅ Testable components

---

## Troubleshooting

### If Models don't populate metadata correctly:

1. Check AST structure matches expectations
2. Verify node types in grammar
3. Add debug logging to Models
4. Test Models with simple AST first

### If Maps fail after rewrite:

1. Verify semantic metadata populated
2. Check child formatter signatures match grammar
3. Ensure `self.Id` is correct node ID
4. Test with simple cases first

### If WASM output differs:

1. Compare method names
2. Compare return types
3. Compare parameter lists
4. Check whitespace/formatting
5. Verify all helper logic moved to Models

---

## Completion Sign-Off

Project Lead: __________________  Date: __________

Technical Review: ______________  Date: __________

Testing Complete: ______________  Date: __________

Documentation Updated: _________  Date: __________

**Rewrite Status:** [ ] Complete [ ] In Progress [ ] Not Started

**Architecture Compliance:** [ ] Verified [ ] Issues Found [ ] Not Checked

**WASM Output:** [ ] Identical [ ] Minor Differences [ ] Major Issues

---

## Notes

_Use this space for any additional notes, issues discovered, or improvements made during the rewrite:_

