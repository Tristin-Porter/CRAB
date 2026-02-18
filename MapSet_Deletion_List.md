# MapSet.cs - Complete Deletion List

## Methods to Delete (Line-by-Line)

### 1. MapCSharpTypeToWasm (Lines 30-41)
**Type:** Static helper method  
**Purpose:** Maps C# type names to WASM types  
**Reason for deletion:** Type mapping should be in semantic Model, not Map helper  
**References:** Called by ExtractTypeFromNode, MapTypeNodeToWasm, EmitSingleParameter

```csharp
private static string MapCSharpTypeToWasm(string typeName)
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
```

**Lines to delete:** 30-41 (12 lines)

---

### 2. ProcessCompilationUnitItem (Lines 294-331)
**Type:** Static helper method  
**Purpose:** Processes compilation unit items (using directives, namespaces, types)  
**Reason for deletion:** Manually walks AST, inspects node.Type, reads node.Fields  
**References:** Not used (was for old static processing)

```csharp
private static string ProcessCompilationUnitItem(AstNode node)
{
    if (node == null) return "";
    
    if (node.Type == "CompilationUnitItem" && node.Fields.ContainsKey("item"))
    {
        var item = node.Fields["item"];
        if (item is AstNode itemNode)
        {
            return ProcessCompilationUnitItem(itemNode);
        }
    }
    
    // Handle different item types
    if (node.Type.Contains("Using"))
    {
        return ";; using ;";
    }
    
    if (node.Type == "NamespaceMemberDeclaration" && node.Fields.ContainsKey("member"))
    {
        var member = node.Fields["member"];
        if (member is AstNode memberNode)
        {
            // Could be NamespaceDeclaration or TypeDeclaration
            if (memberNode.Type == "NamespaceDeclaration")
            {
                return ProcessNamespaceDeclarationInline(memberNode);
            }
            else
            {
                return ProcessTypeDeclaration(memberNode);
            }
        }
    }
    
    return "";
}
```

**Lines to delete:** 294-331 (38 lines)

---

### 3. ProcessNamespaceItem (Lines 349-365)
**Type:** Static helper method  
**Purpose:** Processes namespace items (using, namespace, type declarations)  
**Reason for deletion:** Manually walks AST, inspects node.Type, reads node.Fields  
**References:** Called by ProcessNamespaceDeclarationInline

```csharp
private static string ProcessNamespaceItem(AstNode item)
{
    // Skip using directives
    if (item.Type.Contains("Using"))
        return ";; using ;";
    
    // For NamespaceMemberDeclaration, unwrap to get the actual member
    if (item.Type == "NamespaceMemberDeclaration" && item.Fields.ContainsKey("member"))
    {
        var member = item.Fields["member"];
        if (member is AstNode memberNode)
            return ProcessTypeDeclaration(memberNode);
    }
    
    // Direct type declarations
    return ProcessTypeDeclaration(item);
}
```

**Lines to delete:** 349-365 (17 lines)

---

### 4. ProcessTypeDeclaration (Lines 371-443)
**Type:** Static helper method  
**Purpose:** Processes type declarations (class, struct, interface, enum)  
**Reason for deletion:** Massive AST traversal, manually builds output  
**References:** Called by ProcessCompilationUnitItem, ProcessNamespaceItem, ProcessNamespaceDeclarationInline

```csharp
private static string ProcessTypeDeclaration(AstNode typeNode)
{
    // TypeDeclaration is a wrapper - unwrap it
    if (typeNode.Type == "TypeDeclaration" && typeNode.Fields.ContainsKey("type"))
    {
        var actualType = typeNode.Fields["type"];
        if (actualType is AstNode actualTypeNode)
            return ProcessTypeDeclaration(actualTypeNode);
    }
    
    if (typeNode.Type == "ClassDeclaration")
    {
        // CDTk fix: Fields are now correctly assigned
        string className = "";
        if (typeNode.Fields.ContainsKey("name") && typeNode.Fields["name"] is AstNode nameNode)
        {
            if (nameNode.Type == "Identifier" && nameNode.Fields.ContainsKey("lexeme"))
                className = nameNode.Fields["lexeme"]?.ToString() ?? "";
        }
        
        var output = new System.Text.StringBuilder();
        output.AppendLine($";; class {className}");
        
        // Get class body from body field
        if (typeNode.Fields.ContainsKey("body") && typeNode.Fields["body"] is AstNode bodyNode)
        {
            
            // ClassBody has members field
            if (bodyNode.Fields.ContainsKey("members") && bodyNode.Fields["members"] is AstNode membersNode)
            {
                
                // ClassMemberDeclarations has members field (linked list or single node)
                if (membersNode.Fields.ContainsKey("members"))
                {
                    var members = membersNode.Fields["members"];
                    
                    if (members is List<AstNode> memberList)
                    {
                        
                        foreach (var memberDecl in memberList)
                        {
                            ProcessClassMemberDeclaration(memberDecl, output);
                        }
                    }
                    else if (members is AstNode memberNode)
                    {
                        
                        // Iterate through linked list of members
                        var current = memberNode;
                        while (current != null)
                        {
                            ProcessClassMemberDeclaration(current, output);
                            
                            // Check for next member in the chain
                            if (current.Fields.ContainsKey("next") && current.Fields["next"] is AstNode next)
                            {
                                current = next;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
        
        return output.ToString();
    }
    
    return "";
}
```

**Lines to delete:** 371-443 (73 lines)

---

### 5. ProcessClassMemberDeclaration (Lines 448-463)
**Type:** Static helper method  
**Purpose:** Processes a single class member (method, field, property, etc.)  
**Reason for deletion:** Manually walks AST, calls EmitMethodDeclarationInline  
**References:** Called by ProcessTypeDeclaration

```csharp
private static void ProcessClassMemberDeclaration(AstNode memberDecl, System.Text.StringBuilder output)
{
    
    // ClassMemberDeclaration has member field
    if (memberDecl.Fields.ContainsKey("member") && memberDecl.Fields["member"] is AstNode actualMember)
    {
        
        // For MethodDeclaration, we need to inline the processing
        if (actualMember.Type == "MethodDeclaration")
        {
            var methodOutput = EmitMethodDeclarationInline(actualMember);
            if (!string.IsNullOrWhiteSpace(methodOutput))
                output.Append(methodOutput);
        }
    }
}
```

**Lines to delete:** 448-463 (16 lines)

---

### 6. EmitMethodDeclarationInline (Lines 469-541)
**Type:** Static helper method  
**Purpose:** Emits method declaration inline (simplified after CDTk fix)  
**Reason for deletion:** Manually accesses node.Fields, builds WASM imperatively  
**References:** Called by ProcessClassMemberDeclaration

```csharp
private static string EmitMethodDeclarationInline(AstNode node)
{
    // CDTk fix: Fields are now correctly assigned
    var attrsField = node.Fields.ContainsKey("attrs") ? node.Fields["attrs"] : null;
    var modsField = node.Fields.ContainsKey("mods") ? node.Fields["mods"] : null;
    var returnTypeField = node.Fields.ContainsKey("returnType") ? node.Fields["returnType"] : null;
    var nameField = node.Fields.ContainsKey("name") ? node.Fields["name"] : null;
    var parametersField = node.Fields.ContainsKey("parameters") ? node.Fields["parameters"] : null;
    var bodyField = node.Fields.ContainsKey("body") ? node.Fields["body"] : null;
    
    // Extract function name
    string funcName = "";
    if (nameField is AstNode nameNode && nameNode.Type == "Identifier" && nameNode.Fields.ContainsKey("lexeme"))
    {
        funcName = nameNode.Fields["lexeme"]?.ToString() ?? "";
    }
    
    // Extract return type
    string resultType = "";
    if (returnTypeField is AstNode typeNode)
    {
        resultType = ExtractTypeFromNode(typeNode);
    }
    
    // Extract parameters
    string parameters = "";
    if (parametersField != null)
    {
        parameters = EmitParameterList(parametersField);
    }
    
    // TODO: Local variable analysis should be done in a Model before Maps run
    // For now, we skip local variable declarations
    
    // Extract body
    string body = "";
    if (bodyField is AstNode bodyNode)
    {
        // TODO: Body transformation removed - need to implement via CDTk Maps
        // The body should be transformed by CDTk's Map templates automatically
        body = ";; Method body (transformation TODO)";
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
    
    // TODO: Emit local variable declarations from this.LocalVarInfo
    // This requires a Model to populate LocalVarInfo before Maps run
    
    // Emit the body code
    if (!string.IsNullOrWhiteSpace(body))
    {
        sb.Append("\n  ");
        sb.Append(body);
    }
    
    sb.Append("\n)\n");
    return sb.ToString();
}
```

**Lines to delete:** 469-541 (73 lines)

---

### 7. ExtractTypeFromNode (Lines 711-764)
**Type:** Static helper method  
**Purpose:** Extracts WASM type from Type AST node by recursively traversing structure  
**Reason for deletion:** Recursive AST traversal, should be in semantic Model  
**References:** Called by EmitMethodDeclarationInline, MapTypeNodeToWasm, MethodDeclaration Map

```csharp
private static string ExtractTypeFromNode(AstNode typeNode)
{
    if (typeNode == null) return "";
    
    // Direct lexeme (simple type like "int")
    if (typeNode.Fields.ContainsKey("lexeme"))
    {
        var typeName = typeNode.Fields["lexeme"]?.ToString() ?? "";
        return MapCSharpTypeToWasm(typeName);
    }
    
    // Type has a 'type' field (common pattern)
    if (typeNode.Fields.ContainsKey("type"))
    {
        var innerType = typeNode.Fields["type"];
        if (innerType is AstNode innerNode)
        {
            return ExtractTypeFromNode(innerNode);
        }
    }
    
    // Type has a 'base' field
    if (typeNode.Fields.ContainsKey("base"))
    {
        var baseType = typeNode.Fields["base"];
        if (baseType is AstNode baseNode)
        {
            return ExtractTypeFromNode(baseNode);
        }
    }
    
    // Look for any Identifier with lexeme
    foreach (var field in typeNode.Fields.Values)
    {
        if (field is AstNode node)
        {
            if (node.Type == "Identifier" && node.Fields.ContainsKey("lexeme"))
            {
                var typeName = node.Fields["lexeme"]?.ToString() ?? "";
                return MapCSharpTypeToWasm(typeName);
            }
            
            // Recursively search nested nodes
            if (node.Type == "SimpleName" || node.Type == "NamedType" || node.Type.Contains("Type"))
            {
                var result = ExtractTypeFromNode(node);
                if (!string.IsNullOrEmpty(result))
                    return result;
            }
        }
    }
    
    return "";
}
```

**Lines to delete:** 709-764 (56 lines, including comment on line 709)

---

### 8. EmitParameterList (Lines 1431-1494)
**Type:** Static helper method  
**Purpose:** Emits parameter list from params field  
**Reason for deletion:** Manually walks parameter AST nodes  
**References:** Called by EmitMethodDeclarationInline, MethodDeclaration Map, ConstructorDeclaration Map

```csharp
private static string EmitParameterList(object? paramsNode)
{
    
    if (paramsNode == null) return "";
    
    if (!(paramsNode is AstNode node))
    {
        return "";
    }
    
    
    // Handle FormalParameterList -> extract params field
    if (node.Type == "FormalParameterList" && node.Fields.ContainsKey("params"))
    {
        return EmitParameterList(node.Fields["params"]);
    }
    
    // Handle FormalParameterListContent
    if (node.Type == "FormalParameterListContent" && node.Fields.ContainsKey("params"))
    {
        return EmitParameterList(node.Fields["params"]);
    }
    
    // Handle FixedParameters
    if (node.Type == "FixedParameters")
    {
        var results = new List<string>();
        
        
        // Check if we have a 'params' field with a list of parameters
        if (node.Fields.ContainsKey("params"))
        {
            var paramsField = node.Fields["params"];
            
            
            if (paramsField is List<AstNode> paramsList)
            {
                // New grammar: all parameters in a list (including Comma tokens)
                foreach (var item in paramsList)
                {
                    // Skip Comma tokens, only process FixedParameter nodes
                    if (item.Type == "FixedParameter")
                    {
                        var paramStr = EmitSingleParameter(item);
                        if (!string.IsNullOrWhiteSpace(paramStr))
                            results.Add(paramStr);
                    }
                }
            }
            else if (paramsField is AstNode singleParam)
            {
                // Single parameter
                var paramStr = EmitSingleParameter(singleParam);
                if (!string.IsNullOrWhiteSpace(paramStr))
                    results.Add(paramStr);
            }
        }
        
        return string.Join("\n  ", results);
    }
    
    // Single FixedParameter
    return EmitSingleParameter(paramsNode);
}
```

**Lines to delete:** 1428-1494 (67 lines, including comments starting at 1428)

---

### 9. EmitSingleParameter (Lines 1499-1550)
**Type:** Static helper method  
**Purpose:** Emits a single parameter  
**Reason for deletion:** Manually extracts type and name from AST  
**References:** Called by EmitParameterList

```csharp
private static string EmitSingleParameter(object? paramNode)
{
    if (paramNode == null) return "";
    if (!(paramNode is AstNode node)) return "";
    
    if (node.Type != "FixedParameter" && node.Type != "FormalParameter") return "";
    
    // Due to CDTk field shifting bug, the actual fields are in the wrong places:
    // - For FixedParameter with "int a", we expect type="Type", name="Identifier"
    // - But CDTk returns attrs="Type", modifier="Identifier"
    // Try all possible field names to work around this
    
    // Get parameter name - try modifier first (field shift bug), then name
    var nameField = node.Fields.ContainsKey("modifier") ? node.Fields["modifier"] : 
                   (node.Fields.ContainsKey("name") ? node.Fields["name"] : null);
    string name = "param";
    if (nameField is TokenInstance token)
    {
        name = token.Lexeme;
    }
    else if (nameField is string str)
    {
        name = str;
    }
    else if (nameField is AstNode nameNode)
    {
        // Identifier node with lexeme field
        if (nameNode.Type == "Identifier" && nameNode.Fields.ContainsKey("lexeme"))
        {
            var lexeme = nameNode.Fields["lexeme"];
            if (lexeme is string lexStr)
            {
                name = lexStr;
            }
        }
    }
    
    // Get type - try attrs first (field shift bug), then type
    var typeField = node.Fields.ContainsKey("attrs") ? node.Fields["attrs"] :
                   (node.Fields.ContainsKey("type") ? node.Fields["type"] : null);
    string wasmType = "i32";
    if (typeField is AstNode typeNode)
    {
        wasmType = MapTypeNodeToWasm(typeNode);
    }
    else if (typeField is string typeStr)
    {
        wasmType = MapCSharpTypeToWasm(typeStr);
    }
    
    return $"(param ${name} {wasmType})";
}
```

**Lines to delete:** 1496-1550 (55 lines, including comments starting at 1496)

---

### 10. MapTypeNodeToWasm (Lines 1555-1558)
**Type:** Static helper method  
**Purpose:** Maps a type AstNode to WASM type (wrapper for ExtractTypeFromNode)  
**Reason for deletion:** Wrapper for deleted method  
**References:** Called by EmitSingleParameter

```csharp
private static string MapTypeNodeToWasm(AstNode typeNode)
{
    return ExtractTypeFromNode(typeNode);
}
```

**Lines to delete:** 1552-1558 (7 lines, including comments starting at 1552)

---

### 11. ProcessNamespaceDeclarationInline (Lines 2410-2530)
**Type:** Static helper method  
**Purpose:** Processes a NamespaceDeclaration inline  
**Reason for deletion:** Massive imperative method with AST workarounds  
**References:** Called by ProcessCompilationUnitItem

```csharp
private static string ProcessNamespaceDeclarationInline(AstNode nsNode)
{
    
    // Check all fields
    foreach (var kvp in nsNode.Fields)
    {
        var value = kvp.Value;
    }
    
    // WORKAROUND for CDTk field shifting bug:
    // Due to the @KwNamespace token at the start, fields may be shifted.
    // Expected: name=QualifiedName, body=NamespaceBody
    // Actual due to bug: name=KwNamespace, body=QualifiedName, and NamespaceBody in next field
    
    // Try to find NamespaceBody - it might be in 'body' or any other field
    AstNode? bodyNode = null;
    
    // First try to find it explicitly by type
    foreach (var kvp in nsNode.Fields)
    {
        if (kvp.Value is AstNode astNode && astNode.Type == "NamespaceBody")
        {
            bodyNode = astNode;
            break;
        }
    }
    
    // If not found by type, check if there's an AstNode with "items" field that could be NamespaceBody
    if (bodyNode == null)
    {
        foreach (var kvp in nsNode.Fields)
        {
            if (kvp.Value is AstNode astNode && astNode.Fields.ContainsKey("items"))
            {
                bodyNode = astNode;
                break;
            }
        }
    }
    
    if (bodyNode == null)
    {
        
        // Last resort: check if any field contains a ClassDeclaration directly
        // This handles the case where the namespace is being skipped entirely
        foreach (var kvp in nsNode.Fields)
        {
            if (kvp.Value is AstNode astNode)
            {
                // Check if this looks like it might contain type declarations
                if (astNode.Type.Contains("Class") || astNode.Type.Contains("Type") || 
                    astNode.Type.Contains("Member") || astNode.Type.Contains("Declaration"))
                {
                    var result = ProcessTypeDeclaration(astNode);
                    if (!string.IsNullOrWhiteSpace(result))
                        return result;
                }
            }
        }
        
        return "";
    }
    
    
    // NamespaceBody has items field
    if (!bodyNode.Fields.ContainsKey("items"))
    {
        return "";
    }
    
    var items = bodyNode.Fields["items"];
    
    var results = new List<string>();
    
    // Process list of NamespaceBodyItem
    if (items is List<AstNode> itemList)
    {
        
        foreach (var bodyItem in itemList)
        {
            // NamespaceBodyItem has item field
            if (bodyItem.Fields.ContainsKey("item") && bodyItem.Fields["item"] is AstNode item)
            {
                // Process based on item type
                string itemOutput = ProcessNamespaceItem(item);
                if (!string.IsNullOrWhiteSpace(itemOutput))
                    results.Add(itemOutput);
            }
        }
    }
    else if (items is AstNode itemNode)
    {
        
        // Single item or linked list
        var current = itemNode;
        while (current != null)
        {
            if (current.Fields.ContainsKey("item") && current.Fields["item"] is AstNode item)
            {
                string itemOutput = ProcessNamespaceItem(item);
                if (!string.IsNullOrWhiteSpace(itemOutput))
                    results.Add(itemOutput);
            }
            
            // Check for next
            if (current.Fields.ContainsKey("next") && current.Fields["next"] is AstNode next)
            {
                current = next;
            }
            else
            {
                break;
            }
        }
    }
    else
    {
    }
    
    return string.Join("\n", results);
}
```

**Lines to delete:** 2407-2530 (124 lines, including comments starting at 2407)

---

## Summary

| Method | Start Line | End Line | Lines | Primary Issue |
|--------|------------|----------|-------|---------------|
| MapCSharpTypeToWasm | 30 | 41 | 12 | Type mapping in helper |
| ProcessCompilationUnitItem | 294 | 331 | 38 | AST traversal |
| ProcessNamespaceItem | 349 | 365 | 17 | AST traversal |
| ProcessTypeDeclaration | 371 | 443 | 73 | Massive AST traversal |
| ProcessClassMemberDeclaration | 448 | 463 | 16 | AST traversal |
| EmitMethodDeclarationInline | 469 | 541 | 73 | Imperative code gen |
| ExtractTypeFromNode | 709 | 764 | 56 | Recursive AST walk |
| EmitParameterList | 1428 | 1494 | 67 | AST traversal |
| EmitSingleParameter | 1496 | 1550 | 55 | AST field extraction |
| MapTypeNodeToWasm | 1552 | 1558 | 7 | Wrapper for deleted method |
| ProcessNamespaceDeclarationInline | 2407 | 2530 | 124 | Massive imperative processing |

**Total lines to delete:** ~538 lines (excluding blank lines and some adjacent comments)

**Actual impact:** These methods contain the core imperative logic that violates the functional Map API architecture. Removing them forces a complete rewrite to use semantic Models and functional Maps.
