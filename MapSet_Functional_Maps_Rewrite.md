# Functional Maps Rewrite Guide

This document details how to rewrite the 3 functional (typed) Maps in MapSet.cs to be purely functional and compliant with the CDTk architecture.

---

## Overview

**Current State:**
- 3 functional Maps exist: `MethodDeclaration`, `ConstructorDeclaration`, `IfStatement`
- `IfStatement` is **already correct** - it only calls child formatters and reads semantic context
- `MethodDeclaration` and `ConstructorDeclaration` violate architecture by accessing AST directly

**Target State:**
- All 3 Maps use only child formatters (`Func<string>` parameters)
- All 3 Maps read semantic metadata via `this.*` fields
- Zero AST node access (`self.Node`, `node.Fields`, `node.Type`)

---

## Map 1: IfStatement (Line 880) - ALREADY CORRECT ✅

### Current Implementation

```csharp
public Map IfStatement => new Map(
    (Func<string> condition, Func<string> thenStmt, Func<string> elseClause, MapReference self) =>
    {
        // Access semantic context for formatting decisions
        if (this.OptHints.CanInline.TryGetValue(self.Id, out var inline) && inline)
            return "if(" + condition() + ")" + thenStmt();

        if (this.OptHints.RequiresBlock.TryGetValue(self.Id, out var block) && block)
            return "if (" + condition() + ") { " + thenStmt() + " }";

        if (this.Dialect == "Python")
            return "if " + condition() + ":\n" + thenStmt();

        if (this.Minify)
            return "if(" + condition() + ")" + thenStmt();

        // Default WASM formatting
        return @"(if " + condition() + @"
  (then
" + thenStmt() + @"
  )" + elseClause() + @"
)";
    }
);
```

### Why This is Correct

✅ **Uses child formatters:**
- `condition()` - Calls child formatter for condition expression
- `thenStmt()` - Calls child formatter for then branch
- `elseClause()` - Calls child formatter for else branch

✅ **Reads semantic metadata:**
- `this.OptHints.CanInline[self.Id]` - Optimization hint from Model
- `this.OptHints.RequiresBlock[self.Id]` - Formatting hint from Model
- `this.Dialect` - User-defined target language
- `this.Minify` - User-defined formatting preference

✅ **Never accesses AST:**
- No `self.Node`
- No `node.Fields`
- No `node.Type`

✅ **Pure functional:**
- Input: Child formatters + semantic context
- Output: Formatted string
- No side effects, no state mutation

**Action:** Keep this Map exactly as-is. It's a perfect example!

---

## Map 2: MethodDeclaration (Line 619) - NEEDS REWRITE ❌

### Current Implementation (WRONG)

```csharp
public Map MethodDeclaration => new Map(
    (MapReference self) =>
    {
        var node = self.Node;  // ❌ AST access
        if (node == null) return "";
        
        // ❌ Extract fields from AST node
        var modsField = node.Fields.ContainsKey("mods") ? node.Fields["mods"] : null;
        var returnTypeField = node.Fields.ContainsKey("returnType") ? node.Fields["returnType"] : null;
        var nameField = node.Fields.ContainsKey("name") ? node.Fields["name"] : null;
        var typeParamsField = node.Fields.ContainsKey("typeParams") ? node.Fields["typeParams"] : null;
        
        // ❌ Extract return type from AST
        string resultType = "";
        if (modsField is AstNode typeNode)
        {
            resultType = ExtractTypeFromNode(typeNode);  // ❌ Calls imperative helper
        }
        
        // ❌ Extract function name from AST
        string funcName = "";
        if (returnTypeField is AstNode nameNode && nameNode.Type == "Identifier" && nameNode.Fields.ContainsKey("lexeme"))
        {
            funcName = nameNode.Fields["lexeme"]?.ToString() ?? "";
        }
        
        // ❌ Extract parameters from AST
        string parameters = "";
        object? bodyField = null;
        
        if (nameField is AstNode nameContent)
        {
            if (nameContent.Type == "FormalParameterList")
            {
                parameters = EmitParameterList(nameContent);  // ❌ Calls imperative helper
                bodyField = typeParamsField;
            }
            else if (nameContent.Type == "MethodBody")
            {
                bodyField = nameContent;
            }
        }
        
        // Format body through Maps - ✅ This part is correct!
        string bodyOutput = "";
        if (bodyField is AstNode bodyNode)
        {
            bodyOutput = self.Transform(bodyNode);  // ✅ Calls Map recursively
        }
        
        // Build the function
        var sb = new System.Text.StringBuilder();
        sb.Append($"(func ${funcName}");
        
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            sb.Append("\n  ");
            sb.Append(parameters);
        }
        
        if (!string.IsNullOrWhiteSpace(resultType))
        {
            sb.Append("\n  (result ");
            sb.Append(resultType);
            sb.Append(")");
        }
        
        if (!string.IsNullOrWhiteSpace(bodyOutput))
        {
            sb.Append("\n");
            sb.Append(bodyOutput);
        }
        
        sb.Append("\n)");
        return sb.ToString();
    }
);
```

### Problems Identified

❌ **Accesses AST directly:**
- Line 622: `var node = self.Node;`
- Lines 626-629: `node.Fields.ContainsKey("mods")`, etc.

❌ **Inspects node types:**
- Line 636: `nameNode.Type == "Identifier"`
- Line 658: `nameContent.Type == "FormalParameterList"`

❌ **Reads node fields manually:**
- Line 636: `nameNode.Fields.ContainsKey("lexeme")`
- Line 637: `nameNode.Fields["lexeme"]?.ToString()`

❌ **Calls imperative helpers:**
- Line 642: `ExtractTypeFromNode(typeNode)`
- Line 662: `EmitParameterList(nameContent)`

❌ **No child formatters:**
- Should have `Func<string>` parameters for modifiers, return type, name, parameters, body
- Currently has only `MapReference self`

### Correct Implementation

```csharp
public Map MethodDeclaration => new Map(
    (Func<string> mods, Func<string> returnType, Func<string> name, 
     Func<string> parameters, Func<string> body, MapReference self) =>
    {
        // ✅ Read semantic metadata ONLY
        var methodInfo = this.MethodMetadata.GetInfo(self.Id);
        var methodName = methodInfo.Name;
        var returnWasmType = methodInfo.ReturnWasmType;
        var hasParameters = methodInfo.HasParameters;
        
        // ✅ Call child formatters ONLY
        var paramsWat = hasParameters ? parameters() : "";
        var bodyWat = body();
        
        // ✅ Format based on semantic context
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

### Required Semantic Metadata

Add to WASM class:

```csharp
public MethodInfo MethodMetadata { get; set; } = new MethodInfo();
```

Define MethodInfo class:

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

### Required Model

Create MethodAnalysisModel:

```csharp
public class MethodAnalysisModel : Model
{
    public override object Build(object input)
    {
        var methodInfo = new MethodInfo();
        var ast = input as AstNode;
        
        // Walk AST and populate method info
        WalkAndAnalyzeMethods(ast, methodInfo);
        
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
    
    // Helper methods for extracting info from AST
    // These run BEFORE Maps, so AST access is allowed here
    private string ExtractMethodName(AstNode node) { /* ... */ }
    private string ExtractReturnType(AstNode node) { /* ... */ }
    private bool HasParameters(AstNode node) { /* ... */ }
    private string MapCSharpTypeToWasm(string type) { /* ... */ }
}
```

---

## Map 3: ConstructorDeclaration (Line 774) - NEEDS REWRITE ❌

### Current Implementation (WRONG)

```csharp
public Map ConstructorDeclaration => new Map(
    (MapReference self) =>
    {
        var node = self.Node;  // ❌ AST access
        if (node == null) return "";
        
        // ❌ Extract fields from AST node
        var nameField = node.Fields.ContainsKey("name") ? node.Fields["name"] : null;
        var parametersField = node.Fields.ContainsKey("parameters") ? node.Fields["parameters"] : null;
        var bodyField = node.Fields.ContainsKey("body") ? node.Fields["body"] : null;
        
        // ❌ Extract constructor/class name from AST
        string className = "";
        if (nameField is AstNode nameNode && nameNode.Type == "Identifier" && nameNode.Fields.ContainsKey("lexeme"))
        {
            className = nameNode.Fields["lexeme"]?.ToString() ?? "";
        }
        
        // ❌ Extract parameters from AST
        string parameters = "";
        if (parametersField != null)
        {
            parameters = EmitParameterList(parametersField);  // ❌ Calls imperative helper
        }
        
        // ✅ Format body through Maps - correct!
        string bodyOutput = "";
        if (bodyField is AstNode bodyNode)
        {
            bodyOutput = self.Transform(bodyNode);  // ✅ Calls Map recursively
        }
        
        // Build the constructor function
        var sb = new System.Text.StringBuilder();
        sb.Append($"(func ${className}_ctor");
        
        // Add 'this' parameter
        sb.Append($"\n  (param $this (ref ${className}))");
        
        // Add other parameters if any
        if (!string.IsNullOrWhiteSpace(parameters))
        {
            sb.Append("\n  ");
            sb.Append(parameters);
        }
        
        // Emit the body code (transformed through Maps)
        if (!string.IsNullOrWhiteSpace(bodyOutput))
        {
            sb.Append("\n");
            sb.Append(bodyOutput);
        }
        
        sb.Append("\n)");
        return sb.ToString();
    }
);
```

### Problems Identified

❌ **Accesses AST directly:**
- Line 777: `var node = self.Node;`
- Lines 781-783: `node.Fields.ContainsKey("name")`, etc.

❌ **Inspects node types:**
- Line 788: `nameNode.Type == "Identifier"`

❌ **Reads node fields manually:**
- Line 788: `nameNode.Fields.ContainsKey("lexeme")`
- Line 789: `nameNode.Fields["lexeme"]?.ToString()`

❌ **Calls imperative helpers:**
- Line 796: `EmitParameterList(parametersField)`

❌ **No child formatters:**
- Should have `Func<string>` parameters for name, parameters, body
- Currently has only `MapReference self`

### Correct Implementation

```csharp
public Map ConstructorDeclaration => new Map(
    (Func<string> name, Func<string> parameters, Func<string> body, MapReference self) =>
    {
        // ✅ Read semantic metadata ONLY
        var ctorInfo = this.TypeMetadata.GetConstructorInfo(self.Id);
        var className = ctorInfo.ClassName;
        var hasParameters = ctorInfo.HasParameters;
        
        // ✅ Call child formatters ONLY
        var paramsWat = hasParameters ? parameters() : "";
        var bodyWat = body();
        
        // ✅ Format based on semantic context
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

### Required Semantic Metadata

Extend TypeInfo class:

```csharp
public class TypeInfo
{
    private Dictionary<string, string> _nodeIdToWasmType = new();
    private Dictionary<string, ConstructorData> _constructors = new();
    
    // Existing methods...
    public string GetWasmType(string nodeId) => _nodeIdToWasmType.GetValueOrDefault(nodeId, "i32");
    
    // New methods for constructors
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

### Required Model

Extend TypeAnalysisModel:

```csharp
public class TypeAnalysisModel : Model
{
    public override object Build(object input)
    {
        var typeInfo = new TypeInfo();
        var ast = input as AstNode;
        
        // Walk AST and populate type info
        WalkAndAnalyzeTypes(ast, typeInfo);
        
        return typeInfo;
    }
    
    private void WalkAndAnalyzeTypes(AstNode node, TypeInfo info)
    {
        if (node == null) return;
        
        // Handle type declarations
        if (node.Type == "ClassDeclaration")
        {
            var className = ExtractClassName(node);
            
            // Look for constructor declarations
            WalkForConstructors(node, info, className);
        }
        
        // Find all type nodes and map them
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
    
    // Helper methods
    private string ExtractClassName(AstNode node) { /* ... */ }
    private string MapTypeToWasm(AstNode node) { /* ... */ }
    private bool HasParameters(AstNode node) { /* ... */ }
}
```

---

## Summary of Changes

### Maps to Keep

| Map | Status | Reason |
|-----|--------|--------|
| IfStatement | ✅ Keep as-is | Already correct - perfect example! |

### Maps to Rewrite

| Map | Current Issues | Required Changes |
|-----|----------------|------------------|
| MethodDeclaration | AST access, imperative helpers | Add child formatters, use MethodMetadata |
| ConstructorDeclaration | AST access, imperative helpers | Add child formatters, use TypeMetadata |

### New Semantic Fields Needed

```csharp
// Add to WASM class
public MethodInfo MethodMetadata { get; set; } = new MethodInfo();

// Extend existing TypeMetadata
public TypeInfo TypeMetadata { get; set; } = new TypeInfo();
```

### New Models Needed

1. **MethodAnalysisModel** - Analyzes methods, populates MethodMetadata
2. **TypeAnalysisModel** (extend existing) - Analyzes types and constructors, populates TypeMetadata

### Integration

```csharp
// In compiler pipeline
var mapSet = new WASM();

// Run Models BEFORE Maps
var typeModel = new TypeAnalysisModel();
var methodModel = new MethodAnalysisModel();

mapSet.TypeMetadata = typeModel.Build(ast) as TypeInfo;
mapSet.MethodMetadata = methodModel.Build(ast) as MethodInfo;

// Now Maps can access semantic metadata
var wasmOutput = mapSet.Transform(ast);
```

---

## Verification

After rewriting, verify:

- [ ] `MethodDeclaration` has child formatter parameters (`Func<string>`)
- [ ] `MethodDeclaration` reads `this.MethodMetadata` only
- [ ] `MethodDeclaration` never accesses `self.Node`
- [ ] `ConstructorDeclaration` has child formatter parameters
- [ ] `ConstructorDeclaration` reads `this.TypeMetadata` only
- [ ] `ConstructorDeclaration` never accesses `self.Node`
- [ ] Both Maps work like `IfStatement` (pure functional)
- [ ] No calls to deleted helper methods
- [ ] WASM output is identical to before

---

## Example: Perfect Functional Map Pattern

Use `IfStatement` as the template for all functional Maps:

```csharp
public Map MyMap => new Map(
    (Func<string> child1, Func<string> child2, MapReference self) =>
    {
        // 1. Read semantic metadata via this.*
        var metadata = this.MyMetadata.GetInfo(self.Id);
        
        // 2. Call child formatters
        var output1 = child1();
        var output2 = child2();
        
        // 3. Format based on semantic context
        if (this.Dialect == "Python")
            return python_format(metadata, output1, output2);
        
        if (this.Minify)
            return compact_format(metadata, output1, output2);
        
        // 4. Return formatted output
        return default_format(metadata, output1, output2);
    }
);
```

**Never:**
- Access `self.Node`
- Read `node.Fields` or `node.Type`
- Call imperative helper methods
- Walk AST directly
- Extract information from AST structure

**Always:**
- Use `Func<string>` child formatters
- Read `this.*` semantic fields
- Make formatting decisions based on semantic context
- Return formatted string
- Be pure and functional
