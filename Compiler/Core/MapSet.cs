using CDTk;
using System;
using System.Linq;

namespace CRAB;

/// <summary>
/// Manages string literals for WASM data section.
/// </summary>
public static class StringRegistry
{
    private static Dictionary<int, string> strings = new();
    private static Dictionary<int, int> offsets = new();
    private static int nextId = 0;
    private static int currentOffset = 0;
    
    public static int RegisterString(string text)
    {
        var id = nextId++;
        strings[id] = text;
        offsets[id] = currentOffset;
        currentOffset += text.Length + 1; // +1 for null terminator
        return id;
    }
    
    public static int GetStringOffset(int id)
    {
        return offsets.ContainsKey(id) ? offsets[id] : 0;
    }
    
    public static string GetString(int id)
    {
        return strings.ContainsKey(id) ? strings[id] : "";
    }
    
    public static Dictionary<int, string> GetAllStrings()
    {
        return new Dictionary<int, string>(strings);
    }
    
    public static void Clear()
    {
        strings.Clear();
        offsets.Clear();
        nextId = 0;
        currentOffset = 0;
    }
}

/// <summary>
/// Manages local variables for current function scope.
/// Tracks variable names, types, and generates WASM local declarations.
/// </summary>
public static class LocalVariableRegistry
{
    private static Dictionary<string, string> variables = new();  // name -> type
    private static HashSet<string> currentFunctionVars = new();
    
    public static void RegisterVariable(string name, string wasmType)
    {
        if (!variables.ContainsKey(name))
        {
            variables[name] = wasmType;
            currentFunctionVars.Add(name);
        }
    }
    
    public static string GetVariableType(string name)
    {
        return variables.ContainsKey(name) ? variables[name] : "i32";
    }
    
    public static bool HasVariable(string name)
    {
        return variables.ContainsKey(name);
    }
    
    public static List<(string name, string type)> GetCurrentFunctionVariables()
    {
        return currentFunctionVars.Select(name => (name, variables[name])).ToList();
    }
    
    public static void ClearCurrentFunction()
    {
        currentFunctionVars.Clear();
    }
    
    public static void Clear()
    {
        variables.Clear();
        currentFunctionVars.Clear();
    }
}

/// <summary>
/// Static helper class for WASM code emission.
/// Used by typed Maps to generate WASM instructions from AST nodes.
/// All methods are static so they can be called from field initializers.
/// </summary>
public static class WasmEmit
{
    /// <summary>
    /// Emit WASM code for an expression AST node.
    /// Returns WAT text format code.
    /// </summary>
    public static string EmitExpression(object? exprNode)
    {
        if (exprNode == null) return "";
        
        // Handle List<AstNode> - take the first element
        if (exprNode is List<object> list && list.Count > 0)
        {
            exprNode = list[0];
        }
        
        // Handle List<AstNode> (CDTk may return this type)
        if (exprNode is List<AstNode> astList && astList.Count > 0)
        {
            exprNode = astList[0];
        }
        
        if (!(exprNode is AstNode node))
        {
            // Might be a literal value
            return exprNode?.ToString() ?? "";
        }
        
        // Dispatch based on node type
        return node.Type switch
        {
            // Literals
            "DecimalIntegerLiteral" or "HexIntegerLiteral" or "BinaryIntegerLiteral" => EmitIntegerLiteral(node),
            "FloatLiteral" => EmitFloatLiteral(node),
            "DoubleLiteral" => EmitDoubleLiteral(node),
            "StringLiteral" => EmitStringLiteral(node),
            "TrueLiteral" => "i32.const 1",
            "FalseLiteral" => "i32.const 0",
            
            // Method calls and member access
            "InvocationExpression" => EmitInvocationExpression(node),
            "MemberAccessExpression" => EmitMemberAccessExpression(node),
            "NameSegment" => EmitNameSegment(node),
            "NameSegmentRest" => EmitExpressionDispatcher(node),  // Pass through to child
            "SimpleName" => EmitExpressionDispatcher(node),  // Pass through to child
            "QualifiedName" => EmitExpressionDispatcher(node),  // Pass through to child
            "Argument" => 
                (node.Fields.ContainsKey("expr") && node.Fields["expr"] != null) ?
                    EmitExpression(node.Fields["expr"]) 
                    : "",  // Unwrap argument to get expression
            
            // Binary operations
            // ONLY call EmitBinaryExpression if the node has left/op/right fields
            // If it only has 'expr' field, it's not a binary operation - pass through
            "AdditiveExpression" => node.Fields.ContainsKey("left") && node.Fields.ContainsKey("op") && node.Fields.ContainsKey("right") 
                ? EmitBinaryExpression(node, "+")
                : EmitExpressionDispatcher(node),
            "MultiplicativeExpression" => node.Fields.ContainsKey("left") && node.Fields.ContainsKey("op") && node.Fields.ContainsKey("right")
                ? EmitBinaryExpression(node, "*")
                : EmitExpressionDispatcher(node),
            "RelationalExpression" => node.Fields.ContainsKey("left") && node.Fields.ContainsKey("op") && node.Fields.ContainsKey("right")
                ? EmitBinaryExpression(node, "<")
                : EmitExpressionDispatcher(node),
            "EqualityExpression" => node.Fields.ContainsKey("left") && node.Fields.ContainsKey("op") && node.Fields.ContainsKey("right")
                ? EmitBinaryExpression(node, "==")
                : EmitExpressionDispatcher(node),
            
            // Identifier (variable access)
            "Identifier" => EmitIdentifier(node),
            
            // Parenthesized expression - unwrap
            "ParenthesizedExpression" => node.Fields.ContainsKey("expr") ? EmitExpression(node.Fields["expr"]) : "",
            
            // Expression dispatchers - pass through to child
            // CDTk may return different field names due to field shifting
            "Expression" or "NonAssignmentExpression" or "UnaryExpression" or "UnaryExpressionBase" or
            "UnaryExpressionSuffix" or
            "PrimaryExpression" or "PrimaryNoArrayCreationExpression" or "Sequence" or
            "SwitchExpression" or "RangeExpression" or "NullCoalescingExpression" or
            "ConditionalOrExpression" or "ConditionalAndExpression" or "InclusiveOrExpression" or
            "ExclusiveOrExpression" or "AndExpression" or "ShiftExpression" => EmitExpressionDispatcher(node),
            
            // Skip operator nodes - they're handled by their parent
            "AdditiveOperator" or "MultiplicativeOperator" or "RelationalOperator" or
            "EqualityOperator" or "ShiftOperator" or "UnaryOperator" or
            "AssignmentOperator" or "ConditionalOperator" => "",
            
            // Unknown - comment
            _ => $";; TODO: Emit expression {node.Type}\ni32.const 0"
        };
    }
    
    /// <summary>
    /// Handle expression dispatcher nodes that may have different field names.
    /// </summary>
    private static string EmitExpressionDispatcher(AstNode node)
    {
        // Skip operator nodes - they're handled by their parent binary expression
        if (node.Type.EndsWith("Operator"))
        {
            return "";
        }
        
        // DEBUG: Print what we're dispatching
        
        // For debugging complex expressions
        if (node.Type == "Sequence" && node.Fields.Count > 0)
        {
            foreach (var kvp in node.Fields)
            {
                var value = kvp.Value;
                if (value is AstNode an)
                    System.Console.WriteLine($"  {kvp.Key}: AstNode({an.Type}, fields={string.Join(", ", an.Fields.Keys)})");
                else
                    System.Console.WriteLine($"  {kvp.Key}: {value?.GetType().Name}");
            }
            
            // Sequence might be a binary operation - check for common patterns
            if (node.Fields.ContainsKey("left") && node.Fields.ContainsKey("right"))
            {
                // Check if right is an operator - if so, this is CDTk parsing issue
                var right = node.Fields["right"];
                if (right is AstNode rightNode && rightNode.Type.EndsWith("Operator"))
                {
                }
                
                // This is a binary operation embedded in a sequence
                return EmitBinaryExpression(node, "+");
            }
            
            // Check for expr field
            if (node.Fields.ContainsKey("expr"))
                return EmitExpression(node.Fields["expr"]);
        }
        
        // WORKAROUND for CDTk field shifting bug in Expression node
        // If Expression has 'left' and 'right' fields instead of 'expr',
        // this is CDTk creating a weird structure for binary operations
        if (node.Type == "Expression" && node.Fields.ContainsKey("left") && node.Fields.ContainsKey("right") && !node.Fields.ContainsKey("expr"))
        {
            
            var left = node.Fields["left"];
            var right = node.Fields["right"];
            
            // Check if left is a Sequence with (operand, operator)
            if (left is AstNode leftSeq && leftSeq.Type == "Sequence" && 
                leftSeq.Fields.ContainsKey("left") && leftSeq.Fields.ContainsKey("right"))
            {
                var seqLeft = leftSeq.Fields["left"];   // First operand
                var seqRight = leftSeq.Fields["right"]; // Operator
                
                var seqLeftNode = seqLeft as AstNode;
                var rightExprNode = right as AstNode;
                
                // Extract the operator
                string op = "+";  // default
                if (seqRight is AstNode opNode && opNode.Fields.ContainsKey("lexeme"))
                {
                    op = opNode.Fields["lexeme"]?.ToString() ?? "+";
                }
                
                // Emit: left_operand right_operand operator_instruction
                var leftCode = EmitExpression(seqLeft);
                
                var rightCode = EmitExpression(right);
                
                var opCode = MapOperator(op);
                
                return $"{leftCode}\n{rightCode}\n{opCode}";
            }
            
            // Fallback: just use the right field
            if (right is AstNode rightNode && (rightNode.Type.EndsWith("Expression") || rightNode.Type == "Sequence"))
            {
                return EmitExpression(right);
            }
        }
        
        // Try common field names in order
        
        // Try 'base' and 'suffix' (for member access like Console.WriteLine)
        if (node.Fields.ContainsKey("base"))
        {
            // This might be a member access or invocation
            var baseExpr = node.Fields["base"];
            var suffix = node.Fields.ContainsKey("suffix") ? node.Fields["suffix"] : null;
            
            
            // If suffix is UnaryExpressionSuffix with args, this is a method call
            if (suffix is AstNode suffixNode && suffixNode.Type == "UnaryExpressionSuffix")
            {
                // This is a method invocation - base is the method name, suffix has args
                // But we need to look deeper into base to get the actual method name
                var baseName = GetFullMemberName(baseExpr);
                
                // Check if this is Console.WriteLine (base will be "Console", method is WriteLine)
                if (baseName == "Console" || baseName.EndsWith(".Console") || baseName.EndsWith("WriteLine"))
                {
                    // Emit arguments
                    var args = suffixNode.Fields.ContainsKey("args") ? suffixNode.Fields["args"] : null;
                    var argCode = "";
                    if (args != null)
                    {
                        argCode = EmitArgumentList(args);
                    }
                    
                    // Call imported console_log function
                    return $";; Console.WriteLine\n{argCode}\ncall $console_log";
                }
            }
            
            // If suffix exists and is not handled above, process it
            if (suffix != null)
            {
                return EmitExpression(suffix);
            }
            
            return EmitExpression(baseExpr);
        }
        
        if (node.Fields.ContainsKey("expr"))
            return EmitExpression(node.Fields["expr"]);
        if (node.Fields.ContainsKey("literal"))
            return EmitExpression(node.Fields["literal"]);
        if (node.Fields.ContainsKey("lexeme"))
        {
            var lexeme = node.Fields["lexeme"];
            
            // If lexeme is a string representing a number, emit it as a literal
            if (lexeme is string str)
            {
                // Check if it's a quoted string (string literal)
                if (str.StartsWith("\"") && str.EndsWith("\""))
                {
                    // This is a string literal - remove quotes and emit
                    var text = str.Substring(1, str.Length - 2);
                    var stringId = StringRegistry.RegisterString(text);
                    var offset = StringRegistry.GetStringOffset(stringId);
                    return $";; string \"{text}\" at offset {offset}\ni32.const {offset}";
                }
                
                // Try to parse as integer
                if (int.TryParse(str, out var intValue))
                {
                    return $"i32.const {intValue}";
                }
                // Try to parse as float
                if (float.TryParse(str, out var floatValue))
                {
                    return $"f32.const {floatValue}";
                }
                // Try to parse as double
                if (double.TryParse(str, out var doubleValue))
                {
                    return $"f64.const {doubleValue}";
                }
                // Boolean literals
                if (str == "true")
                    return "i32.const 1";
                if (str == "false")
                    return "i32.const 0";
                    
                // String literal - for now just emit as comment
                return $";; TODO: string literal \"{str}\"";
            }
            
            return EmitExpression(lexeme);
        }
        
        // Try all fields to find an AstNode
        foreach (var field in node.Fields.Values)
        {
            if (field is AstNode astNode)
            {
                return EmitExpression(astNode);
            }
        }
        
        return "";
    }
    
    /// <summary>
    /// Emit integer literal.
    /// </summary>
    public static string EmitIntegerLiteral(AstNode node)
    {
        var lexeme = GetField(node, "lexeme") ?? "0";
        lexeme = lexeme.Replace("_", "");
        
        if (lexeme.StartsWith("0x") || lexeme.StartsWith("0X"))
        {
            var value = Convert.ToInt32(lexeme, 16);
            return $"i32.const {value}";
        }
        
        if (lexeme.StartsWith("0b") || lexeme.StartsWith("0B"))
        {
            var value = Convert.ToInt32(lexeme.Substring(2), 2);
            return $"i32.const {value}";
        }
        
        if (int.TryParse(lexeme, out var intValue))
        {
            return $"i32.const {intValue}";
        }
        
        return "i32.const 0";
    }
    
    /// <summary>
    /// Emit float literal.
    /// </summary>
    private static string EmitFloatLiteral(AstNode node)
    {
        var lexeme = GetField(node, "lexeme") ?? "0.0";
        lexeme = lexeme.Replace("_", "").TrimEnd('f', 'F');
        
        if (float.TryParse(lexeme, out var value))
        {
            return $"f32.const {value}";
        }
        
        return "f32.const 0.0";
    }
    
    /// <summary>
    /// Emit double literal.
    /// </summary>
    private static string EmitDoubleLiteral(AstNode node)
    {
        var lexeme = GetField(node, "lexeme") ?? "0.0";
        lexeme = lexeme.Replace("_", "").TrimEnd('d', 'D');
        
        if (double.TryParse(lexeme, out var value))
        {
            return $"f64.const {value}";
        }
        
        return "f64.const 0.0";
    }
    
    /// <summary>
    /// Emit identifier (variable access).
    /// </summary>
    public static string EmitIdentifier(AstNode node)
    {
        var name = GetField(node, "lexeme") ?? "unknown";
        return $"local.get ${name}";
    }
    
    /// <summary>
    /// Emit string literal.
    /// Stores string in data section and returns pointer.
    /// </summary>
    private static string EmitStringLiteral(AstNode node)
    {
        var text = GetField(node, "lexeme") ?? "";
        
        // Store string in a global registry for later data section emission
        // For now, we'll use a simple approach - just emit a comment and placeholder
        // In a full implementation, we'd track strings and add them to data section
        var stringId = StringRegistry.RegisterString(text);
        var offset = StringRegistry.GetStringOffset(stringId);
        var length = text.Length;
        
        // Return pointer to string in memory (offset) and length
        // For Console.WriteLine, we'll pass both offset and length
        return $";; string \"{text}\" at offset {offset}, length {length}\ni32.const {offset}";
    }
    
    /// <summary>
    /// Emit method invocation (function call).
    /// </summary>
    private static string EmitInvocationExpression(AstNode node)
    {
        // InvocationExpression has 'target' (the method being called) and 'args' (arguments)
        var target = node.Fields.ContainsKey("target") ? node.Fields["target"] : null;
        var args = node.Fields.ContainsKey("args") ? node.Fields["args"] : null;
        
        // Check if this is Console.WriteLine
        if (target is AstNode targetNode)
        {
            var targetStr = GetMethodName(targetNode);
            
            // Special case for Console.WriteLine
            if (targetStr == "Console.WriteLine" || targetStr.EndsWith(".WriteLine"))
            {
                // Emit arguments (string literal)
                var argCode = "";
                if (args != null)
                {
                    argCode = EmitArgumentList(args);
                }
                
                // Call imported console_log function
                return $";; Console.WriteLine\n{argCode}\ncall $console_log";
            }
        }
        
        // Generic method call
        var targetExpr = target != null ? EmitExpression(target) : "";
        var argsExpr = args != null ? EmitArgumentList(args) : "";
        
        return $";; method call\n{argsExpr}\n{targetExpr}";
    }
    
    /// <summary>
    /// Emit member access expression (obj.Member).
    /// </summary>
    private static string EmitMemberAccessExpression(AstNode node)
    {
        // MemberAccessExpression has 'target' (left side) and 'member' (right side)
        var target = node.Fields.ContainsKey("target") ? node.Fields["target"] : null;
        var member = node.Fields.ContainsKey("member") ? node.Fields["member"] : null;
        
        // For now, just concatenate with a dot for debugging
        var targetStr = target is AstNode tn ? GetNodeName(tn) : "";
        var memberStr = member is AstNode mn ? GetNodeName(mn) : "";
        
        // Return as comment for now - this needs proper implementation for field access
        return $";; {targetStr}.{memberStr}";
    }
    
    /// <summary>
    /// Emit name segment (part of qualified name like Console.WriteLine).
    /// </summary>
    private static string EmitNameSegment(AstNode node)
    {
        // NameSegment has 'name' field which is an IdentifierName
        var name = node.Fields.ContainsKey("name") ? node.Fields["name"] : null;
        
        if (name is AstNode nameNode)
        {
            var nameStr = GetField(nameNode, "lexeme") ?? "";
            return $";; name segment: {nameStr}";
        }
        
        return ";; name segment";
    }
    
    /// <summary>
    /// Emit argument list for method call.
    /// </summary>
    private static string EmitArgumentList(object? argsNode)
    {
        if (argsNode == null) return "";
        
        
        if (argsNode is AstNode node)
        {
            // Print fields for debugging
            
            // ArgumentList has 'first' field (CDTk structure)
            if (node.Fields.ContainsKey("first"))
            {
                var first = node.Fields["first"];
                
                // First might be an Argument wrapper
                if (first is AstNode firstNode)
                {
                    
                    // Argument has 'base' field (not 'expr' as expected - CDTk quirk)
                    if (firstNode.Type == "Argument")
                    {
                        var expr = firstNode.Fields.ContainsKey("base") ? firstNode.Fields["base"] : 
                                   firstNode.Fields.ContainsKey("expr") ? firstNode.Fields["expr"] : null;
                        
                        if (expr != null)
                        {
                            return EmitExpression(expr);
                        }
                    }
                    
                    return EmitExpression(firstNode);
                }
            }
            
            // ArgumentList has 'args' field with list of arguments
            if (node.Fields.ContainsKey("args"))
            {
                var args = node.Fields["args"];
                
                
                if (args is List<AstNode> argList)
                {
                    var results = new List<string>();
                    foreach (var arg in argList)
                    {
                        // Each arg might be an Argument wrapper
                        if (arg.Type == "Argument" && arg.Fields.ContainsKey("expr"))
                        {
                            var expr = arg.Fields["expr"];
                            results.Add(EmitExpression(expr));
                        }
                        else
                        {
                            results.Add(EmitExpression(arg));
                        }
                    }
                    return string.Join("\n", results);
                }
                else if (args is AstNode singleArg)
                {
                    return EmitExpression(singleArg);
                }
            }
            
            // Might be a single expression
            return EmitExpression(node);
        }
        
        return "";
    }
    
    /// <summary>
    /// Get method name from AST node (for Console.WriteLine detection).
    /// </summary>
    private static string GetMethodName(AstNode node)
    {
        if (node.Type == "MemberAccessExpression")
        {
            var target = node.Fields.ContainsKey("target") ? node.Fields["target"] : null;
            var member = node.Fields.ContainsKey("member") ? node.Fields["member"] : null;
            
            var targetName = target is AstNode tn ? GetNodeName(tn) : "";
            var memberName = member is AstNode mn ? GetNodeName(mn) : "";
            
            return $"{targetName}.{memberName}";
        }
        
        return GetNodeName(node);
    }
    
    /// <summary>
    /// Get name from AST node.
    /// </summary>
    private static string GetNodeName(AstNode node)
    {
        if (node.Fields.ContainsKey("lexeme"))
        {
            return node.Fields["lexeme"]?.ToString() ?? "";
        }
        
        if (node.Type == "IdentifierName" || node.Type == "Identifier")
        {
            return GetField(node, "lexeme") ?? "";
        }
        
        if (node.Type == "NameSegment" && node.Fields.ContainsKey("name"))
        {
            var name = node.Fields["name"];
            if (name is AstNode nameNode)
            {
                return GetNodeName(nameNode);
            }
        }
        
        if (node.Type == "SimpleName" && node.Fields.ContainsKey("name"))
        {
            var name = node.Fields["name"];
            if (name is AstNode nameNode)
            {
                return GetNodeName(nameNode);
            }
        }
        
        return node.Type;
    }
    
    /// <summary>
    /// Get full member name from AST node (e.g., "Console.WriteLine").
    /// </summary>
    private static string GetFullMemberName(object? node)
    {
        if (node == null) return "";
        
        if (!(node is AstNode astNode)) return "";
        
        // Handle UnaryExpression with base/suffix
        if (astNode.Type == "UnaryExpression" && astNode.Fields.ContainsKey("base"))
        {
            var baseObj = astNode.Fields["base"];
            return GetFullMemberName(baseObj);
        }
        
        // Handle MemberAccessExpression or similar
        if (astNode.Fields.ContainsKey("target") && astNode.Fields.ContainsKey("member"))
        {
            var target = GetFullMemberName(astNode.Fields["target"]);
            var member = GetFullMemberName(astNode.Fields["member"]);
            return $"{target}.{member}";
        }
        
        // Handle QualifiedName with segments
        if (astNode.Type == "QualifiedName" && astNode.Fields.ContainsKey("segments"))
        {
            var segments = astNode.Fields["segments"];
            if (segments is AstNode segNode)
            {
                return GetNameFromSegments(segNode);
            }
        }
        
        // Handle NameSegments
        if (astNode.Type == "NameSegments")
        {
            return GetNameFromSegments(astNode);
        }
        
        // Handle NameSegment
        if (astNode.Type == "NameSegment" && astNode.Fields.ContainsKey("name"))
        {
            var name = astNode.Fields["name"];
            return GetNodeName(name as AstNode ?? astNode);
        }
        
        // Try to get lexeme
        return GetNodeName(astNode);
    }
    
    /// <summary>
    /// Get name from NameSegments structure.
    /// </summary>
    private static string GetNameFromSegments(AstNode node)
    {
        var parts = new List<string>();
        
        // Get first segment
        if (node.Fields.ContainsKey("first"))
        {
            var first = node.Fields["first"];
            if (first is AstNode firstNode)
            {
                var firstName = GetFullMemberName(firstNode);
                if (!string.IsNullOrEmpty(firstName))
                    parts.Add(firstName);
            }
        }
        
        // Get rest of segments
        if (node.Fields.ContainsKey("rest"))
        {
            var rest = node.Fields["rest"];
            if (rest is AstNode restNode)
            {
                // rest might be NameSegmentRest or another NameSegments
                if (restNode.Type == "NameSegmentRest" && restNode.Fields.ContainsKey("segment"))
                {
                    var seg = restNode.Fields["segment"];
                    if (seg is AstNode segNode)
                    {
                        var segName = GetFullMemberName(segNode);
                        if (!string.IsNullOrEmpty(segName))
                            parts.Add(segName);
                    }
                }
                else
                {
                    var restName = GetFullMemberName(restNode);
                    if (!string.IsNullOrEmpty(restName))
                        parts.Add(restName);
                }
            }
        }
        
        return string.Join(".", parts);
    }
    
    /// <summary>
    /// Emit binary expression (a + b, a * b, etc).
    /// </summary>
    public static string EmitBinaryExpression(AstNode node, string defaultOp)
    {
        // DEBUG: Print node structure
        foreach (var kvp in node.Fields)
        {
            var value = kvp.Value;
            if (value is AstNode an)
            {
                System.Console.WriteLine($"  {kvp.Key}: AstNode({an.Type})");
                if (an.Fields.Count > 0)
                {
                    System.Console.WriteLine($"    fields={string.Join(", ", an.Fields.Keys)}");
                }
            }
            else if (kvp.Value is List<AstNode> lan)
                System.Console.WriteLine($"  {kvp.Key}: List<AstNode>({lan.Count})");
            else
                System.Console.WriteLine($"  {kvp.Key}: {value?.GetType().Name}");
        }
        
        // Get left and right operands
        var left = node.Fields.ContainsKey("left") ? node.Fields["left"] : null;
        var right = node.Fields.ContainsKey("right") ? node.Fields["right"] : null;
        
        // Extract operator - it might be nested in an operator node
        string op = defaultOp;
        if (node.Fields.ContainsKey("op"))
        {
            var opField = node.Fields["op"];
            if (opField is AstNode opNode)
            {
                // Operator is an AstNode like AdditiveOperator
                // It has an inner op field with the actual token
                if (opNode.Fields.ContainsKey("op"))
                {
                    var innerOp = opNode.Fields["op"];
                    if (innerOp is TokenInstance token)
                    {
                        op = token.Lexeme;
                    }
                    else if (innerOp is AstNode innerOpNode && innerOpNode.Fields.ContainsKey("lexeme"))
                    {
                        op = innerOpNode.Fields["lexeme"]?.ToString() ?? defaultOp;
                    }
                }
                else if (opNode.Fields.ContainsKey("lexeme"))
                {
                    op = opNode.Fields["lexeme"]?.ToString() ?? defaultOp;
                }
            }
            else if (opField is TokenInstance token)
            {
                op = token.Lexeme;
            }
            else if (opField is string str)
            {
                op = str;
            }
        }
        
        // Emit left operand
        var leftCode = EmitExpression(left);
        
        // Emit right operand
        var rightCode = EmitExpression(right);
        
        // Map operator to WASM instruction
        var opCode = MapOperator(op);
        
        return $"{leftCode}\n{rightCode}\n{opCode}";
    }
    
    /// <summary>
    /// Map C# operator to WASM opcode.
    /// </summary>
    public static string MapOperator(string op)
    {
        return op switch
        {
            "+" => "i32.add",
            "-" => "i32.sub",
            "*" => "i32.mul",
            "/" => "i32.div_s",
            "%" => "i32.rem_s",
            "&" => "i32.and",
            "|" => "i32.or",
            "^" => "i32.xor",
            "==" => "i32.eq",
            "!=" => "i32.ne",
            "<" => "i32.lt_s",
            ">" => "i32.gt_s",
            "<=" => "i32.le_s",
            ">=" => "i32.ge_s",
            "<<" => "i32.shl",
            ">>" => "i32.shr_s",
            "&&" => "i32.and",
            "||" => "i32.or",
            _ => "nop"
        };
    }
    
    /// <summary>
    /// Emit statement code.
    /// </summary>
    public static string EmitStatement(object? stmtNode)
    {
        if (stmtNode == null)
        {
            return "";
        }
        
        // Handle List<object> - process first element
        if (stmtNode is List<object> list)
        {
            if (list.Count > 0)
            {
                return EmitStatement(list[0]);
            }
            return "";
        }
        
        if (!(stmtNode is AstNode node))
        {
            return "";  // Unknown type
        }
        
        return node.Type switch
        {
            "ReturnStatement" => EmitReturnStatement(node),
            "ExpressionStatement" => node.Fields.ContainsKey("expr") ? EmitExpression(node.Fields["expr"]) : "",
            "Block" => EmitBlock(node),
            "EmptyStatement" => "nop",
            
            // Control flow statements
            "IfStatement" => EmitIfStatement(node),
            "WhileStatement" => EmitWhileStatement(node),
            "DoStatement" => EmitDoStatement(node),
            "ForStatement" => EmitForStatement(node),
            "ForEachStatement" => EmitForEachStatement(node),
            "SwitchStatement" => EmitSwitchStatement(node),
            "BreakStatement" => "br 0 ;; break",
            "ContinueStatement" => "br 1 ;; continue",
            "DeclarationStatement" => EmitDeclarationStatement(node),
            
            // Statement dispatchers - pass through to child
            "Statement" or "EmbeddedStatement" or "JumpStatement" or "SelectionStatement" or "IterationStatement" =>
                node.Fields.ContainsKey("stmt") ? EmitStatement(node.Fields["stmt"]) : "",
            
            _ => $";; TODO: Emit {node.Type}\nnop"
        };
    }
    
    /// <summary>
    /// Emit return statement.
    /// </summary>
    private static string EmitReturnStatement(AstNode node)
    {
        if (node.Fields.ContainsKey("expr") && node.Fields["expr"] != null)
        {
            var expr = node.Fields["expr"];
            
            // Handle list of expressions (CDTk may return List<AstNode>)
            if (expr is List<AstNode> astList)
            {
                if (astList.Count > 0)
                {
                    // Try to find the actual expression (not the return keyword or semicolon)
                    foreach (var item in astList)
                    {
                        if (item.Type != "KwReturn" && item.Type != "Semicolon")
                        {
                            expr = item;
                            break;
                        }
                    }
                }
            }
            else if (expr is List<object> objList && objList.Count > 0)
            {
                expr = objList[0];
            }
            
            var exprCode = EmitExpression(expr);
            if (!string.IsNullOrWhiteSpace(exprCode))
            {
                return exprCode + "\nreturn";
            }
        }
        return "return";
    }
    
    /// <summary>
    /// Emit block statement.
    /// </summary>
    private static string EmitBlock(AstNode node)
    {
        if (node.Fields.ContainsKey("stmts"))
        {
            return EmitStatementList(node.Fields["stmts"]);
        }
        return "";
    }
    
    /// <summary>
    /// Emit a list of statements.
    /// CDTk's Statement+ creates a linked list structure where each Statement node
    /// has a 'stmt' field. This function recursively traverses the list.
    /// </summary>
    public static string EmitStatementList(object? stmtsNode)
    {
        if (stmtsNode == null)
        {
            return "";
        }
        
        // Handle List<object> directly (if CDTk returns this)
        if (stmtsNode is List<object> objList)
        {
            return string.Join("\n", objList.Select(EmitStatement));
        }
        
        // Handle List<AstNode>
        if (stmtsNode is List<AstNode> astList)
        {
            return string.Join("\n", astList.Select(EmitStatement));
        }
        
        // Handle single AstNode
        if (!(stmtsNode is AstNode node))
        {
            return "";
        }
        
        // CDTk's Statement+ creates a recursive structure:
        // - Statements node has 'stmts' field containing first Statement  
        // - Each Statement node may have 'stmt' field containing next Statement
        // We need to collect all statements in the linked list
        var statements = new List<string>();
        
        if (node.Type == "Statements" && node.Fields.ContainsKey("stmts"))
        {
            var stmtsField = node.Fields["stmts"];
            
            // Handle if stmts field is a List<AstNode>
            if (stmtsField is List<AstNode> stmtAstList)
            {
                return string.Join("\n", stmtAstList.Select(EmitStatement));
            }
            
            // Handle if stmts field is a List<object>
            if (stmtsField is List<object> stmtObjList)
            {
                return string.Join("\n", stmtObjList.Select(EmitStatement));
            }
            
            // Handle single AstNode in stmts field
            var current = stmtsField;
            while (current != null)
            {
                if (current is AstNode currentNode)
                {
                    // Emit this statement
                    var emitted = EmitStatement(currentNode);
                    if (!string.IsNullOrWhiteSpace(emitted))
                    {
                        statements.Add(emitted);
                    }
                    
                    // Move to next statement if it exists
                    if (currentNode.Fields.ContainsKey("next") && currentNode.Fields["next"] is AstNode)
                    {
                        current = currentNode.Fields["next"];
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            
            return string.Join("\n", statements);
        }
        
        // Single statement node
        return EmitStatement(stmtsNode);
    }
    
    /// <summary>
    /// Emit if statement.
    /// </summary>
    private static string EmitIfStatement(AstNode node)
    {
        // WORKAROUND for CDTk field shifting bug in IfStatement:
        // Expected fields: condition=Expression, thenStmt=Statement, elseClause=ElseClause
        // Actual due to @KwIf at start: condition=KwIf, thenStmt=Expression, elseClause=Statement
        // So we need to shift: thenStmt IS the condition, elseClause IS the then statement
        
        var conditionField = node.Fields.ContainsKey("condition") ? node.Fields["condition"] : null;
        var thenStmtField = node.Fields.ContainsKey("thenStmt") ? node.Fields["thenStmt"] : null;
        var elseClauseField = node.Fields.ContainsKey("elseClause") ? node.Fields["elseClause"] : null;
        
        // Apply field shifting workaround
        object? condition = thenStmtField;  // thenStmt field actually contains the condition
        object? thenStmt = elseClauseField;  // elseClause field actually contains the then statement
        object? elseClause = null;  // Actual else clause is lost or in another field
        
        // Try to find else clause if it exists
        // It might be in a "next" field or other unnamed field
        foreach (var kvp in node.Fields)
        {
            if (kvp.Key != "condition" && kvp.Key != "thenStmt" && kvp.Key != "elseClause")
            {
                if (kvp.Value is AstNode an && an.Type == "ElseClause")
                {
                    elseClause = an;
                    break;
                }
            }
        }
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(";; if statement");
        
        // Emit condition
        if (condition != null)
        {
            var condCode = EmitExpression(condition);
            if (!string.IsNullOrWhiteSpace(condCode))
            {
                sb.AppendLine(condCode);
            }
            else
            {
                sb.AppendLine("i32.const 1  ;; default true");
            }
        }
        else
        {
            sb.AppendLine("i32.const 1  ;; default true");
        }
        
        sb.AppendLine("if");
        
        // Emit then branch
        if (thenStmt is AstNode thenNode)
        {
            var thenCode = EmitStatement(thenNode);
            if (!string.IsNullOrWhiteSpace(thenCode))
            {
                sb.AppendLine(thenCode);
            }
        }
        
        // Emit else branch if present
        if (elseClause is AstNode elseNode)
        {
            sb.AppendLine("else");
            // If elseNode has stmt field, use that
            if (elseNode.Fields.ContainsKey("stmt"))
            {
                sb.AppendLine(EmitStatement(elseNode.Fields["stmt"]));
            }
            else
            {
                sb.AppendLine(EmitStatement(elseNode));
            }
        }
        
        sb.AppendLine("end");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit while statement.
    /// </summary>
    private static string EmitWhileStatement(AstNode node)
    {
        var condition = node.Fields.ContainsKey("condition") ? node.Fields["condition"] : null;
        var body = node.Fields.ContainsKey("body") ? node.Fields["body"] : null;
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(";; while loop");
        sb.AppendLine("block");  // Exit label
        sb.AppendLine("loop");   // Loop label
        
        // Emit condition
        if (condition != null)
        {
            sb.AppendLine(EmitExpression(condition));
        }
        
        // If condition is false, break out
        sb.AppendLine("i32.eqz");
        sb.AppendLine("br_if 1");  // Break to outer block
        
        // Emit loop body
        if (body is AstNode bodyNode)
        {
            sb.AppendLine(EmitStatement(bodyNode));
        }
        
        // Jump back to loop start
        sb.AppendLine("br 0");
        sb.AppendLine("end");    // End loop
        sb.AppendLine("end");    // End block
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit do-while statement.
    /// </summary>
    private static string EmitDoStatement(AstNode node)
    {
        var condition = node.Fields.ContainsKey("condition") ? node.Fields["condition"] : null;
        var body = node.Fields.ContainsKey("body") ? node.Fields["body"] : null;
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(";; do-while loop");
        sb.AppendLine("block");  // Exit label
        sb.AppendLine("loop");   // Loop label
        
        // Emit loop body first (do-while executes at least once)
        if (body is AstNode bodyNode)
        {
            sb.AppendLine(EmitStatement(bodyNode));
        }
        
        // Emit condition
        if (condition != null)
        {
            sb.AppendLine(EmitExpression(condition));
        }
        
        // If condition is true, continue loop
        sb.AppendLine("br_if 0");  // Branch back if true
        sb.AppendLine("end");      // End loop
        sb.AppendLine("end");      // End block
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit for statement.
    /// </summary>
    private static string EmitForStatement(AstNode node)
    {
        var init = node.Fields.ContainsKey("init") ? node.Fields["init"] : null;
        var condition = node.Fields.ContainsKey("condition") ? node.Fields["condition"] : null;
        var iterator = node.Fields.ContainsKey("iterator") ? node.Fields["iterator"] : null;
        var body = node.Fields.ContainsKey("body") ? node.Fields["body"] : null;
        
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(";; for loop");
        
        // Emit initializer
        if (init is AstNode initNode)
        {
            sb.AppendLine(EmitStatement(initNode));
        }
        
        sb.AppendLine("block");  // Exit label
        sb.AppendLine("loop");   // Loop label
        
        // Emit condition (if present)
        if (condition is AstNode condNode)
        {
            sb.AppendLine(EmitExpression(condNode));
            sb.AppendLine("i32.eqz");
            sb.AppendLine("br_if 1");  // Break to outer block if false
        }
        
        // Emit loop body
        if (body is AstNode bodyNode)
        {
            sb.AppendLine(EmitStatement(bodyNode));
        }
        
        // Emit iterator
        if (iterator is AstNode iterNode)
        {
            sb.AppendLine(EmitStatement(iterNode));
        }
        
        // Jump back to loop start
        sb.AppendLine("br 0");
        sb.AppendLine("end");    // End loop
        sb.AppendLine("end");    // End block
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit foreach statement.
    /// </summary>
    private static string EmitForEachStatement(AstNode node)
    {
        // For now, emit a simplified version
        // Full implementation would need iterator protocol
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(";; foreach loop");
        sb.AppendLine(";; TODO: Implement full foreach with iterator protocol");
        sb.AppendLine("nop");
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit switch statement.
    /// </summary>
    private static string EmitSwitchStatement(AstNode node)
    {
        // For now, emit a simplified version
        // Full implementation would need br_table
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(";; switch statement");
        sb.AppendLine(";; TODO: Implement full switch with br_table");
        sb.AppendLine("nop");
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit declaration statement (variable declaration).
    /// </summary>
    /// <summary>
    /// Emit declaration statement (variable declaration).
    /// Handles: int x; int x = 5; string s = "hello";
    /// </summary>
    private static string EmitDeclarationStatement(AstNode node)
    {
        var sb = new System.Text.StringBuilder();
        
        // DeclarationStatement has a 'decl' field containing LocalDeclaration
        if (!node.Fields.ContainsKey("decl"))
        {
            sb.AppendLine(";; variable declaration (no decl field)");
            sb.AppendLine("nop");
            return sb.ToString();
        }
        
        var decl = node.Fields["decl"];
        
        // Handle if decl is a List
        if (decl is List<AstNode> declList)
        {
            if (declList.Count > 0)
            {
                return EmitLocalVariableDeclaration(declList[0]);
            }
        }
        else if (decl is List<object> objList)
        {
            if (objList.Count > 0 && objList[0] is AstNode declNode)
            {
                return EmitLocalVariableDeclaration(declNode);
            }
        }
        else if (decl is AstNode declNode)
        {
            // LocalDeclaration has a 'decl' field containing LocalVariableDeclaration
            if (declNode.Fields.ContainsKey("decl"))
            {
                var innerDecl = declNode.Fields["decl"];
                if (innerDecl is AstNode varDecl)
                {
                    return EmitLocalVariableDeclaration(varDecl);
                }
            }
            
            // Try processing the declNode directly
            return EmitLocalVariableDeclaration(declNode);
        }
        
        sb.AppendLine($";; variable declaration (decl type: {decl?.GetType().Name})");
        sb.AppendLine("nop");
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit local variable declaration with optional initialization.
    /// LocalVariableDeclaration has: type, declarators
    /// </summary>
    private static string EmitLocalVariableDeclaration(AstNode node)
    {
        var sb = new System.Text.StringBuilder();
        
        // If this is a LocalDeclaration, unwrap to LocalVariableDeclaration
        if (node.Type == "LocalDeclaration")
        {
            // Due to CDTk field shifting, the LocalVariableDeclaration fields might be directly in LocalDeclaration
            // Or there might be a 'decl' field
            if (node.Fields.ContainsKey("decl"))
            {
                var innerDecl = node.Fields["decl"];
                if (innerDecl is AstNode declNode)
                {
                    return EmitLocalVariableDeclaration(declNode);
                }
                else if (innerDecl is List<AstNode> declList && declList.Count > 0)
                {
                    return EmitLocalVariableDeclaration(declList[0]);
                }
            }
            // Fall through to process the LocalDeclaration as if it were LocalVariableDeclaration
        }
        
        // WORKAROUND for CDTk field shifting in LocalDeclaration:
        // Due to optional modifier, when no modifier is present, fields shift:
        // - modifier field contains LocalVariableType (the actual type)
        // - type field contains LocalVariableDeclarators (the actual declarators)
        
        string wasmType = "i32";  // default
        object? typeField = null;
        object? declaratorsField = null;
        
        // Check for field shifting pattern
        if (node.Type == "LocalDeclaration" && node.Fields.ContainsKey("modifier"))
        {
            var modifierField = node.Fields["modifier"];
            if (modifierField is AstNode modNode && modNode.Type == "LocalVariableType")
            {
                // Field shifting detected! modifier is actually the type
                typeField = modifierField;
                if (node.Fields.ContainsKey("type"))
                {
                    var typeActual = node.Fields["type"];
                    if (typeActual is AstNode typeNode && typeNode.Type == "LocalVariableDeclarators")
                    {
                        // type field is actually the declarators
                        declaratorsField = typeActual;
                    }
                }
            }
        }
        
        // If not field-shifted, extract normally
        if (typeField == null && node.Fields.ContainsKey("type"))
        {
            typeField = node.Fields["type"];
        }
        if (declaratorsField == null && node.Fields.ContainsKey("declarators"))
        {
            declaratorsField = node.Fields["declarators"];
        }
        
        // Extract WASM type from type field
        if (typeField is AstNode tn)
        {
            wasmType = ExtractWasmTypeFromNode(tn);
        }
        else if (typeField is List<AstNode> typeList && typeList.Count > 0)
        {
            wasmType = ExtractWasmTypeFromNode(typeList[0]);
        }
        
        // Extract declarators
        if (declaratorsField is AstNode declaratorNode)
        {
            return EmitVariableDeclarators(declaratorNode, wasmType);
        }
        else if (declaratorsField is List<AstNode> declList && declList.Count > 0)
        {
            return EmitVariableDeclarators(declList[0], wasmType);
        }
        
        sb.AppendLine(";; variable declaration (no declarators found)");
        sb.AppendLine("nop");
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit variable declarators (one or more variables with optional initializers).
    /// Example: int x, y = 5, z;
    /// </summary>
    private static string EmitVariableDeclarators(AstNode node, string wasmType)
    {
        var sb = new System.Text.StringBuilder();
        
        // LocalVariableDeclarators has a 'declarators' field or might have 'first'
        if (node.Fields.ContainsKey("declarators"))
        {
            var declarators = node.Fields["declarators"];
            
            // Handle list of declarators
            if (declarators is List<AstNode> declList)
            {
                foreach (var decl in declList)
                {
                    var declCode = EmitSingleVariableDeclarator(decl, wasmType);
                    if (!string.IsNullOrWhiteSpace(declCode))
                    {
                        sb.AppendLine(declCode);
                    }
                }
            }
            else if (declarators is AstNode declNode)
            {
                // Single declarator or linked list
                var current = declNode;
                while (current != null)
                {
                    var declCode = EmitSingleVariableDeclarator(current, wasmType);
                    if (!string.IsNullOrWhiteSpace(declCode))
                    {
                        sb.AppendLine(declCode);
                    }
                    
                    // Check for next declarator
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
        // Try 'first' field (common in CDTk)
        else if (node.Fields.ContainsKey("first"))
        {
            var firstDecl = node.Fields["first"];
            if (firstDecl is AstNode declNode)
            {
                var declCode = EmitSingleVariableDeclarator(declNode, wasmType);
                if (!string.IsNullOrWhiteSpace(declCode))
                {
                    sb.AppendLine(declCode);
                }
            }
        }
        
        if (sb.Length == 0)
        {
            sb.AppendLine(";; variable declaration (no declarators found)");
            sb.AppendLine("nop");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Emit a single variable declarator with optional initializer.
    /// Example: x, y = 5, z = a + b
    /// </summary>
    private static string EmitSingleVariableDeclarator(AstNode node, string wasmType)
    {
        var sb = new System.Text.StringBuilder();
        
        // Extract variable name
        string varName = "";
        if (node.Fields.ContainsKey("name"))
        {
            var nameNode = node.Fields["name"];
            if (nameNode is AstNode nn && nn.Fields.ContainsKey("lexeme"))
            {
                varName = nn.Fields["lexeme"]?.ToString() ?? "";
            }
        }
        
        if (string.IsNullOrEmpty(varName))
        {
            // Try to find identifier directly
            if (node.Type == "LocalVariableDeclarator" || node.Type == "VariableDeclarator")
            {
                foreach (var kvp in node.Fields)
                {
                    if (kvp.Value is AstNode an && an.Type == "Identifier")
                    {
                        if (an.Fields.ContainsKey("lexeme"))
                        {
                            varName = an.Fields["lexeme"]?.ToString() ?? "";
                            break;
                        }
                    }
                }
            }
        }
        
        if (string.IsNullOrEmpty(varName))
        {
            return "";  // Can't declare without name
        }
        
        // Register the variable
        LocalVariableRegistry.RegisterVariable(varName, wasmType);
        
        // Check for initializer
        if (node.Fields.ContainsKey("init") || node.Fields.ContainsKey("initializer"))
        {
            var initField = node.Fields.ContainsKey("init") ? node.Fields["init"] : node.Fields["initializer"];
            
            // For now, we'll emit a default value since CDTk isn't capturing the actual initializer
            // TODO: Fix CDTk to properly capture LocalVariableInitializer
            sb.AppendLine($";; {varName} = ... (initializer not captured by parser)");
            sb.AppendLine("i32.const 0  ;; default value");
            sb.AppendLine($"local.set ${varName}");
        }
        
        if (sb.Length == 0)
        {
            // Declaration without initializer - just register it
            sb.AppendLine($";; declare {varName}");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Extract WASM type from a type node.
    /// Maps C# types to WASM types.
    /// </summary>
    private static string ExtractWasmTypeFromNode(AstNode typeNode)
    {
        // Try to get the type name from the node
        string typeName = "";
        
        // Direct lexeme (simple type like "int")
        if (typeNode.Fields.ContainsKey("lexeme"))
        {
            typeName = typeNode.Fields["lexeme"]?.ToString() ?? "";
        }
        // Type has a 'type' field (common pattern)
        else if (typeNode.Fields.ContainsKey("type"))
        {
            var innerType = typeNode.Fields["type"];
            if (innerType is AstNode innerNode && innerNode.Fields.ContainsKey("lexeme"))
            {
                typeName = innerNode.Fields["lexeme"]?.ToString() ?? "";
            }
        }
        
        // Map C# types to WASM types
        return typeName switch
        {
            "int" or "Int32" or "uint" or "UInt32" => "i32",
            "long" or "Int64" or "ulong" or "UInt64" => "i64",
            "float" or "Single" => "f32",
            "double" or "Double" => "f64",
            "bool" or "Boolean" => "i32",
            "byte" or "Byte" or "sbyte" or "SByte" => "i32",
            "short" or "Int16" or "ushort" or "UInt16" => "i32",
            "char" or "Char" => "i32",
            _ => "i32"  // Default to i32 for objects, strings, etc.
        };
    }
    
    /// <summary>
    /// Emit parameter list for function.
    /// </summary>
    public static string EmitParameters(object? paramsNode)
    {
        if (paramsNode == null || !(paramsNode is AstNode node))
        {
            return "";
        }
        
        if (node.Type == "FormalParameterList")
        {
            var results = new List<string>();
            
            // Try to find fixed parameters
            if (node.Fields.ContainsKey("fixedParams"))
            {
                var fixedParams = node.Fields["fixedParams"];
                if (fixedParams is AstNode fixedNode && fixedNode.Type == "FixedParameters")
                {
                    if (fixedNode.Fields.ContainsKey("parameters"))
                    {
                        var parameters = fixedNode.Fields["parameters"];
                        if (parameters is List<object> paramList)
                        {
                            foreach (var param in paramList)
                            {
                                results.Add(EmitParameter(param));
                            }
                        }
                        else
                        {
                            results.Add(EmitParameter(parameters));
                        }
                    }
                }
            }
            
            return string.Join("\n  ", results);
        }
        
        return "";
    }
    
    /// <summary>
    /// Emit single parameter.
    /// </summary>
    private static string EmitParameter(object? paramNode)
    {
        if (paramNode == null || !(paramNode is AstNode node))
        {
            return "";
        }
        
        if (node.Type == "FixedParameter")
        {
            var name = GetField(node, "name") ?? "param";
            var typeStr = GetField(node, "type") ?? "i32";
            var wasmType = MapCSharpTypeToWasm(typeStr);
            
            return $"(param ${name} {wasmType})";
        }
        
        return "";
    }
    
    /// <summary>
    /// Map C# type name to WASM type.
    /// </summary>
    public static string MapCSharpTypeToWasm(string csharpType)
    {
        return csharpType switch
        {
            "int" or "uint" or "byte" or "sbyte" or "short" or "ushort" or "bool" or "char" => "i32",
            "long" or "ulong" => "i64",
            "float" => "f32",
            "double" => "f64",
            "void" => "",
            _ => "i32" // Default
        };
    }
    
    /// <summary>
    /// Helper to safely get a field from an AST node.
    /// </summary>
    private static string? GetField(AstNode? node, string fieldName)
    {
        if (node == null) return null;
        
        if (node.Fields.ContainsKey(fieldName))
        {
            var value = node.Fields[fieldName];
            
            // If it's an AST node, try to get its lexeme
            if (value is AstNode astNode && astNode.Fields.ContainsKey("lexeme"))
            {
                return astNode.Fields["lexeme"]?.ToString();
            }
            
            return value?.ToString();
        }
        
        return null;
    }
    
    /// <summary>
    /// Generate WASM data section for string literals.
    /// Emits all registered strings with proper null termination.
    /// </summary>
    public static string GenerateDataSection()
    {
        var sb = new System.Text.StringBuilder();
        var allStrings = StringRegistry.GetAllStrings();
        
        foreach (var kvp in allStrings.OrderBy(x => x.Key))
        {
            var stringId = kvp.Key;
            var text = kvp.Value;
            var offset = StringRegistry.GetStringOffset(stringId);
            
            // Escape special characters in string
            var escapedText = EscapeString(text);
            
            // Emit data directive: (data (i32.const offset) "text\00")
            sb.AppendLine($"  (data (i32.const {offset}) \"{escapedText}\\00\")");
        }
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Escape special characters in strings for WASM text format.
    /// </summary>
    private static string EscapeString(string text)
    {
        return text
            .Replace("\\", "\\\\")  // Backslash
            .Replace("\"", "\\\"")  // Quote
            .Replace("\n", "\\n")   // Newline
            .Replace("\r", "\\r")   // Carriage return
            .Replace("\t", "\\t");  // Tab
    }
    
    /// <summary>
    /// Emit a compilation unit item (using directive, class, etc.).
    /// </summary>
    public static string EmitCompilationUnitItem(AstNode node)
    {
        if (node.Type == "CompilationUnitItem" && node.Fields.ContainsKey("item"))
        {
            var item = node.Fields["item"];
            if (item is AstNode itemNode)
            {
                return EmitCompilationUnitItem(itemNode);
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
                return EmitCompilationUnitItem(memberNode);
            }
        }
        
        // For type declarations, we need to use the WASM class's processing
        // This will be handled by the existing TypeDeclaration maps
        return "";
    }
}

/// <summary>
/// WASM MapSet: Translates C# AST to WebAssembly text format (WAT).
/// 
/// Maps are organized by category:
/// - Module Structure (CompilationUnit, namespaces, types)
/// - Member Declarations (methods, fields, properties)
/// - Statements (control flow, expressions)
/// - Expressions (operators, literals, calls)
/// - Types (primitives, references, generics)
/// 
/// Each public Map field corresponds to an AST node type from the RuleSet.
/// Placeholders like {name} are replaced with actual AST field values.
/// </summary>
public class WASM : MapSet
{
    // ============================================================
    // SEMANTIC ANALYSIS MODELS
    // ============================================================
    
    /// <summary>
    /// Automatic memory model (CTGC) for semantic analysis.
    /// Performs lifetime inference, region analysis, and memory safety verification.
    /// Used by Maps to insert deallocation instructions and memory management code.
    /// </summary>
    public Automatic AutomaticModel => new Automatic(__AllRules!, __Ast!);
    
    /// <summary>
    /// Manual memory model for semantic analysis.
    /// Performs verification of manual{} blocks using abstract interpretation,
    /// symbolic execution, and ownership graphs.
    /// Used by Maps to verify and annotate manual memory operations.
    /// </summary>
    public Manual ManualModel => new Manual(__AllRules!, __Ast!);
    
    /// <summary>
    /// Optimization model for code transformations.
    /// Applies safe optimizations that preserve 100% memory safety guarantees.
    /// Performs dead code elimination, constant folding, CSE, inlining,
    /// loop optimizations, tail call optimization, and peephole optimizations.
    /// All transformations preserve CTGC deallocation points and ownership semantics.
    /// Used by Maps to generate optimized WASM while maintaining safety.
    /// </summary>
    public Optimization OptimizationModel => new Optimization(__AllRules!, __Ast!);
    
    // ============================================================
    // HELPER METHODS FOR MODEL INTEGRATION
    // ============================================================
    
    /// <summary>
    /// Get memory management annotations from the automatic model.
    /// This is called during WASM generation to insert deallocation instructions.
    /// </summary>
    private AutomaticAnnotations? GetAutomaticAnnotations()
    {
        if (__Ast?.Root == null) return null;
        
        try
        {
            return AutomaticModel.Build(__Ast.Root) as AutomaticAnnotations;
        }
        catch
        {
            // If model analysis fails, return null - Maps will generate basic WASM
            return null;
        }
    }
    
    /// <summary>
    /// Get manual memory verification results.
    /// This is called during WASM generation to verify manual blocks.
    /// </summary>
    private ManualAnnotations? GetManualAnnotations()
    {
        if (__Ast?.Root == null) return null;
        
        try
        {
            return ManualModel.Build(__Ast.Root) as ManualAnnotations;
        }
        catch
        {
            // If verification fails, return null - compilation will fail with diagnostics
            return null;
        }
    }
    
    /// <summary>
    /// Get optimization annotations for code transformations.
    /// This is called during WASM generation to apply safe optimizations.
    /// If optimization fails, Maps generate unoptimized but safe WASM.
    /// </summary>
    private OptimizationAnnotations? GetOptimizationAnnotations()
    {
        if (__Ast?.Root == null) return null;
        
        try
        {
            return OptimizationModel.Build(__Ast.Root) as OptimizationAnnotations;
        }
        catch
        {
            // If optimization fails, return null - Maps will generate unoptimized WASM
            return null;
        }
    }
    
    // ============================================================
    // TYPED MAP API DEMONSTRATION (New Architecture)
    // ============================================================
    // ============================================================
    // MODULE STRUCTURE
    // ============================================================
    
    /// <summary>Top-level compilation unit - generates complete WASM module</summary>
    /// <summary>
    /// CompilationUnit template - generates the complete WASM module.
    /// Uses TypedMap to allow custom emission that includes data section.
    /// </summary>
    public Map<AstNode, string> CompilationUnit = TypedMap.For<string>()
        .Emit(node => {
            // Clear string registry for new compilation
            StringRegistry.Clear();
            LocalVariableRegistry.Clear();
            
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("(module");
            sb.AppendLine("  ;; Imports");
            sb.AppendLine("  (import \"env\" \"memory\" (memory 1))");
            sb.AppendLine("  (import \"env\" \"console_log\" (func $console_log (param i32)))");
            sb.AppendLine();
            
            // First, process items to register strings
            var itemsCode = new System.Text.StringBuilder();
            if (node.Fields.ContainsKey("items"))
            {
                var items = node.Fields["items"];
                if (items is List<AstNode> itemList)
                {
                    foreach (var item in itemList)
                    {
                        // Use the CompilationUnitItem map to process each item
                        var itemCode = "";
                        try
                        {
                            // Try to use the map if it exists in context
                            itemCode = ProcessCompilationUnitItem(item);
                        }
                        catch
                        {
                            itemCode = "";
                        }
                        
                        if (!string.IsNullOrWhiteSpace(itemCode))
                        {
                            itemsCode.AppendLine(itemCode);
                        }
                    }
                }
            }
            
            // Now generate data section with all registered strings
            var dataSection = WasmEmit.GenerateDataSection();
            if (!string.IsNullOrWhiteSpace(dataSection))
            {
                sb.AppendLine("  ;; Data section (string literals)");
                sb.Append(dataSection);
                sb.AppendLine();
            }
            
            sb.AppendLine("  ;; Generated members");
            sb.Append(itemsCode);
            
            sb.AppendLine();
            sb.AppendLine("  ;; Exports");
            sb.AppendLine("  (export \"main\" (func $Main))");
            sb.AppendLine(")");
            
            return sb.ToString();
        });
    
    /// <summary>
    /// Process a compilation unit item using the existing infrastructure.
    /// </summary>
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

    
    /// <summary>Namespace member declarations</summary>
    public Map NamespaceMemberDeclarations = "{members}";
    
    /// <summary>Single namespace member</summary>
    public Map NamespaceMemberDeclaration = "{member}";
    
    /// <summary>Namespace declaration - this is now processed inline from CompilationUnitItem, so this shouldn't be called</summary>
    public Map<AstNode, string> NamespaceDeclaration = TypedMap.For<string>()
        .Emit(node => {
            // This should not be called anymore since we process it inline
            return ProcessNamespaceDeclarationInline(node);
        });
    
    /// <summary>
    /// Process a namespace item (using directive, namespace, or type).
    /// </summary>
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
    
    /// <summary>
    /// Process a type declaration (class, struct, etc).
    /// For now, we can only handle classes inline. Methods require the MethodDeclaration typed Map.
    /// </summary>
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
            // Extract class name (in mods field due to field shifting)
            string className = "";
            if (typeNode.Fields.ContainsKey("mods") && typeNode.Fields["mods"] is AstNode modsNode)
            {
                if (modsNode.Type == "Identifier" && modsNode.Fields.ContainsKey("lexeme"))
                    className = modsNode.Fields["lexeme"]?.ToString() ?? "";
            }
            
            var output = new System.Text.StringBuilder();
            output.AppendLine($";; class {className}");
            
            // Get class body (in name field due to field shifting)
            if (typeNode.Fields.ContainsKey("name") && typeNode.Fields["name"] is AstNode bodyNode)
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
    
    /// <summary>
    /// Process a single class member declaration.
    /// </summary>
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
    
    /// <summary>
    /// Emit a method declaration inline (duplicate of MethodDeclaration typed Map logic).
    /// This is needed because we can't call the MethodDeclaration typed Map from within this typed Map.
    /// </summary>
    private static string EmitMethodDeclarationInline(AstNode node)
    {
        // Extract fields
        var modsField = node.Fields.ContainsKey("mods") ? node.Fields["mods"] : null;
        var attrsField = node.Fields.ContainsKey("attrs") ? node.Fields["attrs"] : null;
        var returnTypeField = node.Fields.ContainsKey("returnType") ? node.Fields["returnType"] : null;
        var nameField = node.Fields.ContainsKey("name") ? node.Fields["name"] : null;
        var typeParamsField = node.Fields.ContainsKey("typeParams") ? node.Fields["typeParams"] : null;
        
        
        string funcName = "";
        string resultType = "";
        string parameters = "";
        string body = "";
        
        // NEW CASE: Detect field shifting when name contains FormalParameterList
        if (nameField is AstNode nameNode && nameNode.Type == "FormalParameterList" && 
            typeParamsField is AstNode typeParamsNode && typeParamsNode.Type == "MethodBody")
        {
            // Field-shifted case: mods=Type, returnType=Identifier(name), name=FormalParameterList, typeParams=MethodBody
            
            // Get function name from returnType field
            if (returnTypeField is AstNode idNode && idNode.Type == "Identifier" && idNode.Fields.ContainsKey("lexeme"))
                funcName = idNode.Fields["lexeme"]?.ToString() ?? "";
            
            // Get return type from mods field
            if (modsField is AstNode modsType)
                resultType = ExtractTypeFromNode(modsType);
            
            // Get parameters from name field (which contains FormalParameterList)
            parameters = EmitParameterList(nameField);
            
            // Clear local variables for this function BEFORE emitting body
            LocalVariableRegistry.ClearCurrentFunction();
            
            // Get body from typeParams field (which contains MethodBody)
            if (typeParamsNode.Fields.ContainsKey("body"))
                body = WasmEmit.EmitStatement(typeParamsNode.Fields["body"]);
            else
                body = WasmEmit.EmitStatement(typeParamsNode);
        }
        // Detect case by checking if mods is an Identifier
        else if (modsField is AstNode modsNode && modsNode.Type == "Identifier")
        {
            // WITH params case
            funcName = modsNode.Fields.ContainsKey("lexeme") ? modsNode.Fields["lexeme"]?.ToString() ?? "" : "";
            
            if (attrsField is AstNode attrsType)
                resultType = ExtractTypeFromNode(attrsType);
            
            if (returnTypeField != null)
            {
                parameters = EmitParameterList(returnTypeField);
            }
            
            // Clear local variables for this function BEFORE emitting body
            LocalVariableRegistry.ClearCurrentFunction();
            
            if (nameField is AstNode bodyNode)
            {
                if (bodyNode.Fields.ContainsKey("body"))
                    body = WasmEmit.EmitStatement(bodyNode.Fields["body"]);
                else
                    body = WasmEmit.EmitStatement(bodyNode);
            }
        }
        else
        {
            // NO params case
            if (returnTypeField is AstNode idNode && idNode.Type == "Identifier" && idNode.Fields.ContainsKey("lexeme"))
                funcName = idNode.Fields["lexeme"]?.ToString() ?? "";
            
            if (modsField is AstNode modsType)
                resultType = ExtractTypeFromNode(modsType);
            
            // Clear local variables for this function BEFORE emitting body
            LocalVariableRegistry.ClearCurrentFunction();
            
            if (nameField is AstNode bodyNode)
            {
                // DEBUG: Print body structure
                if (bodyNode.Fields.ContainsKey("body"))
                {
                    var innerBody = bodyNode.Fields["body"];
                    if (innerBody is AstNode ibn)
                    {
                        if (ibn.Fields.ContainsKey("stmts"))
                        {
                            var stmts = ibn.Fields["stmts"];
                            if (stmts is AstNode sn)
                            {
                            }
                        }
                    }
                    body = WasmEmit.EmitStatement(bodyNode.Fields["body"]);
                }
                else
                    body = WasmEmit.EmitStatement(bodyNode);
            }
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
        
        // Emit function body (this will register local variables)
        string bodyCode = "";
        if (!string.IsNullOrWhiteSpace(body))
        {
            bodyCode = body;
        }
        
        // Now emit local variable declarations based on what was registered
        var locals = LocalVariableRegistry.GetCurrentFunctionVariables();
        foreach (var (name, type) in locals)
        {
        }
        if (locals.Count > 0)
        {
            sb.AppendLine();
            foreach (var (name, type) in locals)
            {
                sb.AppendLine($"  (local ${name} {type})");
            }
        }
        
        // Now emit the body code
        if (!string.IsNullOrWhiteSpace(bodyCode))
        {
            sb.Append("\n  ");
            sb.Append(bodyCode);
        }
        
        sb.Append("\n)\n");
        return sb.ToString();
    }

    
    /// <summary>Namespace body - emits items (list gets unwrapped by dispatcher patterns)</summary>
    public Map NamespaceBody = "{items}";
    
    /// <summary>Namespace body item</summary>
    public Map NamespaceBodyItem = "{item}";
    
    // ============================================================
    // TYPE DECLARATIONS
    // ============================================================
    
    /// <summary>Type declaration dispatcher</summary>
    public Map TypeDeclaration = "{type}";
    
    /// <summary>
    /// Class declaration - generates WASM struct type.
    /// 
    /// Full OOP implementation requires:
    /// - Virtual method table (vtable) generation for virtual/override methods
    /// - Base class field inclusion (inheritance)
    /// - Type identification field for runtime type checking
    /// - Interface implementation tables
    /// - Constructor chaining to base class
    /// 
    /// WORKAROUND for CDTk parser bug with optional fields.
    /// Bug causes field shifting: when 'attrs' is absent, subsequent fields shift:
    /// - 'mods' field receives the class name (e.g., "Calculator")
    /// - 'name' field receives the ClassBody AST node
    /// - 'body' field is absent
    /// Therefore: use {mods} to get class name, {name} to get class body.
    /// This will be fixed when CDTk parser is updated.
    /// </summary>
    public Map ClassDeclaration = @";; class {mods}
{name}";
    
    /// <summary>
    /// Class body - generates class members.
    /// Due to CDTk parser bug with field shifting, this Map is referenced via {name} in ClassDeclaration.
    /// Returns the members field from the ClassBody AST node.
    /// For WASM MVP, we skip the class wrapper and just emit methods at module level.
    /// </summary>
    public Map ClassBody = "{members}";
    
    /// <summary>Class member declarations</summary>
    public Map ClassMemberDeclarations = "{members}";
    
    /// <summary>Single class member</summary>
    public Map ClassMemberDeclaration = "{member}";
    
    /// <summary>Struct declaration</summary>
    public Map StructDeclaration = @";; struct {name}
(type ${name} (struct
{body}
))";
    
    /// <summary>Struct body</summary>
    public Map StructBody = "{members}";
    
    /// <summary>Struct member declarations</summary>
    public Map StructMemberDeclarations = "{members}";
    
    /// <summary>Single struct member</summary>
    public Map StructMemberDeclaration = "{member}";
    
    // ============================================================
    // MEMBER DECLARATIONS
    // ============================================================
    
    /// <summary>
    /// Method declaration - primary compilation target.
    /// Uses typed Map to work around CDTk field-shifting bug.
    /// </summary>
    public Map<AstNode, string> MethodDeclaration = TypedMap.For<string>()
        .Emit(node => {
            if (node == null) return "";
            
            // Extract fields
            var modsField = node.Fields.ContainsKey("mods") ? node.Fields["mods"] : null;
            var attrsField = node.Fields.ContainsKey("attrs") ? node.Fields["attrs"] : null;
            var returnTypeField = node.Fields.ContainsKey("returnType") ? node.Fields["returnType"] : null;
            var nameField = node.Fields.ContainsKey("name") ? node.Fields["name"] : null;
            var bodyField = node.Fields.ContainsKey("body") ? node.Fields["body"] : null;
            
            string funcName = "";
            string resultType = "";
            string parameters = "";
            string body = "";
            
            // Detect case by checking if mods is an Identifier
            if (modsField is AstNode modsNode && modsNode.Type == "Identifier")
            {
                // WITH params: mods=Identifier, attrs=Type, returnType=FormalParameterList, name=MethodBody
                funcName = modsNode.Fields.ContainsKey("lexeme") ? modsNode.Fields["lexeme"]?.ToString() ?? "" : "";
                
                // Get result type from attrs (Type node)
                if (attrsField is AstNode attrsType)
                {
                    resultType = ExtractTypeFromNode(attrsType);
                }
                
                // Get parameters from returnType (FormalParameterList)
                if (returnTypeField != null)
                {
                    parameters = EmitParameterList(returnTypeField);
                }
                
                // Get body from name (MethodBody -> Block)
                if (nameField is AstNode bodyNode)
                {
                    // MethodBody has a 'body' field containing the actual Block
                    if (bodyNode.Fields.ContainsKey("body"))
                    {
                        body = WasmEmit.EmitStatement(bodyNode.Fields["body"]);
                    }
                    else
                    {
                        body = WasmEmit.EmitStatement(bodyNode);
                    }
                }
            }
            else
            {
                // NO params: attrs=Modifiers, mods=Type, returnType=Identifier, name=empty or body
                
                // Get function name from returnType (Identifier)
                if (returnTypeField is AstNode idNode && idNode.Type == "Identifier" && idNode.Fields.ContainsKey("lexeme"))
                {
                    funcName = idNode.Fields["lexeme"]?.ToString() ?? "";
                }
                
                // Get result type from mods (Type node)
                if (modsField is AstNode modsType)
                {
                    resultType = ExtractTypeFromNode(modsType);
                }
                
                // Try to get body from name field (which contains MethodBody for NO params)
                if (nameField is AstNode bodyNode)
                {
                    // MethodBody has a 'body' field containing the actual Block
                    if (bodyNode.Fields.ContainsKey("body"))
                    {
                        body = WasmEmit.EmitStatement(bodyNode.Fields["body"]);
                    }
                    else
                    {
                        body = WasmEmit.EmitStatement(bodyNode);
                    }
                }
                else if (bodyField != null)
                {
                    body = WasmEmit.EmitStatement(bodyField);
                }
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
            
            if (!string.IsNullOrWhiteSpace(body))
            {
                sb.Append("\n  ");
                sb.Append(body);
            }
            
            sb.Append("\n)");
            return sb.ToString();
        });
    
    private static string MapCSharpTypeToWasm(string typeName)
    {
        return typeName switch
        {
            "int" => "i32",
            "uint" => "i32",
            "byte" => "i32",
            "sbyte" => "i32",
            "short" => "i32",
            "ushort" => "i32",
            "bool" => "i32",
            "char" => "i32",
            "long" => "i64",
            "ulong" => "i64",
            "float" => "f32",
            "double" => "f64",
            "void" => "",
            _ => "i32" // Default
        };
    }
    
    /// <summary>
    /// Extract WASM type from Type AST node by recursively traversing structure.
    /// </summary>
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
    
    /// <summary>Field declaration - TODO: properly handle multiple declarators</summary>
    public Map FieldDeclaration = ";; field {type}";

    
    /// <summary>
    /// Constructor declaration - CTGC analyzes object initialization.
    /// </summary>
    public Map ConstructorDeclaration = @"(func ${name}_ctor
  (param $this (ref ${name}))
{body}
)";
    
    // ============================================================
    // STATEMENTS
    // ============================================================
    
    /// <summary>Statement dispatcher</summary>
    public Map Statement = "{stmt}";
    
    /// <summary>Embedded statement</summary>
    public Map EmbeddedStatement = "{stmt}";
    
    /// <summary>
    /// Block statement - scope boundary for automatic memory management.
    /// AutomaticModel.Build() computes deallocation points for allocations in this block.
    /// Deallocations are inserted at the end of the block or at last use points.
    /// Format: (block {stmts} ;; deallocations inserted here by model)
    /// </summary>
    public Map Block = @"(block
{stmts}
  ;; Deallocation instructions inserted here based on AutomaticModel analysis
)";
    
    /// <summary>Statements list - recursively emit all statements in the list</summary>
    public Map<AstNode, string> Statements = TypedMap.For<string>()
        .Emit(node => {
            if (node == null) return "";
            
            // The Statements rule creates stmts:Statement+
            // CDTk's + repetition creates a nested structure or list
            if (node.Fields.ContainsKey("stmts"))
            {
                var result = WasmEmit.EmitStatementList(node.Fields["stmts"]);
                return result ?? "";
            }
            
            // Fallback: try to emit as single statement
            return WasmEmit.EmitStatement(node) ?? "";
        });
    
    /// <summary>Empty statement (no-op)</summary>
    public Map EmptyStatement = "(nop)";
    
    /// <summary>Labeled statement</summary>
    public Map LabeledStatement = @"(block ${label}
{stmt}
)";
    
    /// <summary>Declaration statement</summary>
    public Map DeclarationStatement = "{decl}";
    
    /// <summary>Expression statement</summary>
    public Map ExpressionStatement = "{expr}";
    
    /// <summary>Selection statement dispatcher</summary>
    public Map SelectionStatement = "{stmt}";
    
    /// <summary>If statement (else clause should be omitted if empty, handled by template processing)</summary>
    public Map IfStatement = @"(if {condition}
  (then
{thenStmt}
  ){elseClause}
)";
    
    /// <summary>Switch statement</summary>
    public Map SwitchStatement = @"(block $switch
  ;; switch on {expr}
{sections}
)";
    
    /// <summary>Iteration statement dispatcher</summary>
    public Map IterationStatement = "{stmt}";
    
    /// <summary>While loop</summary>
    public Map WhileStatement = @"(block $while_exit
  (loop $while_loop
    (br_if $while_exit (i32.eqz {condition}))
{body}
    (br $while_loop)
  )
)";
    
    /// <summary>Do-while loop</summary>
    public Map DoStatement = @"(block $do_outer
  (loop $do_loop
{body}
    (br_if $do_loop {condition})
  )
)";
    
    /// <summary>For loop</summary>
    public Map ForStatement = @"(block $for_outer
{init}
  (loop $for_loop
    (br_if $for_outer (i32.eqz {condition}))
{body}
{iterator}
    (br $for_loop)
  )
)";
    
    /// <summary>Foreach loop</summary>
    public Map ForEachStatement = @"(block $foreach
  ;; foreach {name} in {collection}
  (loop $foreach_loop
    ;; iterator logic
{body}
    (br $foreach_loop)
  )
)";
    
    /// <summary>Jump statement dispatcher</summary>
    public Map JumpStatement = "{stmt}";
    
    /// <summary>Break statement</summary>
    public Map BreakStatement = "(br $break)";
    
    /// <summary>Continue statement</summary>
    public Map ContinueStatement = "(br $continue)";
    
    /// <summary>Goto statement</summary>
    public Map GotoStatement = "(br ${target})";
    
    /// <summary>
    /// Return statement - supports optional expression
    /// Outputs (return) for void returns, (return expr) for value returns
    /// The expr field exists for all alternatives except void return
    /// TYPED MAP: Uses WasmEmit helper to recursively emit expression
    /// </summary>
    public Map<AstNode, string> ReturnStatement = TypedMap.For<string>()
        .Emit(node =>
        {
            // Check if there's an expression to return
            if (node.Fields.ContainsKey("expr") && node.Fields["expr"] != null)
            {
                var exprOutput = WasmEmit.EmitExpression(node.Fields["expr"]);
                if (!string.IsNullOrWhiteSpace(exprOutput))
                {
                    return exprOutput + "\nreturn";
                }
            }
            return "return";
        });
    
    /// <summary>Throw statement</summary>
    public Map ThrowStatement = @";; throw {expr}
(unreachable)";
    
    /// <summary>Try-catch-finally statement</summary>
    public Map TryStatement = @"(block $try
{body}
{handlers}
{finallyClause}
)";
    
    /// <summary>
    /// Variable declaration - potential allocation site.
    /// If initialized with 'new', AutomaticModel tracks this allocation and computes deallocation point.
    /// </summary>
    public Map LocalVariableDeclaration = "{modifier} (local {type} {declarators})";
    
    /// <summary>
    /// Constant declaration - if initialized with allocation, tracked by AutomaticModel.
    /// </summary>
    public Map LocalConstantDeclaration = "(local ${name} {type} {init})";
    
    // ============================================================
    // EXPRESSIONS
    // ============================================================
    
    /// <summary>Expression dispatcher</summary>
    public Map Expression = "{expr}";
    
    /// <summary>Assignment expression</summary>
    public Map AssignmentExpression = "({op} {left} {right})";
    
    /// <summary>Non-assignment expression</summary>
    public Map NonAssignmentExpression = "{expr}";
    
    /// <summary>Conditional (ternary) expression</summary>
    public Map ConditionalExpression = "(select {trueExpr} {falseExpr} {condition})";
    
    /// <summary>Null-coalescing expression</summary>
    public Map NullCoalescingExpression = @"(if (ref.is_null {left})
  (then {right})
  (else {left})
)";
    
    /// <summary>Logical OR expression</summary>
    public Map LogicalOrExpression = "(i32.or {left} {right})";
    
    /// <summary>Logical AND expression</summary>
    public Map LogicalAndExpression = "(i32.and {left} {right})";
    
    /// <summary>Bitwise OR expression</summary>
    public Map BitwiseOrExpression = "(i32.or {left} {right})";
    
    /// <summary>Bitwise XOR expression</summary>
    public Map BitwiseXorExpression = "(i32.xor {left} {right})";
    
    /// <summary>Bitwise AND expression</summary>
    public Map BitwiseAndExpression = "(i32.and {left} {right})";
    
    /// <summary>Equality expression</summary>
    public Map EqualityExpression = "({op} {left} {right})";
    
    /// <summary>Relational expression</summary>
    public Map RelationalExpression = "({op} {left} {right})";
    
    /// <summary>Shift expression</summary>
    public Map ShiftExpression = "({op} {left} {right})";
    
    /// <summary>Additive expression - TYPED MAP</summary>
    public Map<AstNode, string> AdditiveExpression = TypedMap.For<string>()
        .Emit(node => WasmEmit.EmitBinaryExpression(node, "+"));
    
    /// <summary>Multiplicative expression - TYPED MAP</summary>
    public Map<AstNode, string> MultiplicativeExpression = TypedMap.For<string>()
        .Emit(node => WasmEmit.EmitBinaryExpression(node, "*"));
    
    /// <summary>Switch expression (C# 8+)</summary>
    public Map SwitchExpression = @"(block $switch_expr
  ;; switch expression on {input}
{arms}
)";
    
    /// <summary>Range expression (C# 8+)</summary>
    public Map RangeExpression = @";; range {start}..{end}
(struct.new $Range {start} {end})";
    
    /// <summary>Unary expression dispatcher</summary>
    public Map UnaryExpression = "{base}{suffix}{expr}";
    
    /// <summary>Unary operator expression</summary>
    public Map UnaryOperatorExpression = "({op} {operand})";
    
    /// <summary>Cast expression</summary>
    public Map CastExpression = @";; cast to {type}
{expr}";
    
    /// <summary>Await expression</summary>
    public Map AwaitExpression = @";; await {expr}
(call $await_impl {expr})";
    
    /// <summary>Default expression</summary>
    public Map DefaultExpression = @";; default({type})
(i32.const 0)";
    
    /// <summary>Nameof expression</summary>
    public Map NameofExpression = @";; nameof({expr})
(i32.const 0) ;; string offset";
    
    /// <summary>Sizeof expression - should return actual type size; requires type information from semantic analysis</summary>
    public Map SizeofExpression = @";; sizeof({type})
(i32.const {size})";
    
    /// <summary>Checked expression</summary>
    public Map CheckedExpression = @";; checked {expr}
{expr}";
    
    /// <summary>Unchecked expression</summary>
    public Map UncheckedExpression = "{expr}";
    
    /// <summary>Primary expression dispatcher</summary>
    public Map PrimaryExpression = "{expr}";
    
    /// <summary>Primary no-array expression dispatcher</summary>
    public Map PrimaryNoArrayCreationExpression = "{expr}";
    
    /// <summary>Parenthesized expression</summary>
    public Map ParenthesizedExpression = "{expr}";
    
    /// <summary>Member access expression - handles struct field access; properties, methods, and static access require additional logic</summary>
    public Map MemberAccessExpression = "(struct.get ${type}.${member} {target})";
    
    /// <summary>Method invocation - simplified direct call; virtual dispatch/interfaces require call_indirect, instance methods need 'this' parameter</summary>
    public Map InvocationExpression = @";; call {target}
(call ${target} {args})";
    
    /// <summary>Element/indexer access</summary>
    public Map ElementAccessExpression = @";; {target}[{indices}]
(array.get {target} {indices})";
    
    /// <summary>This keyword</summary>
    public Map ThisAccessExpression = "(local.get $this)";
    
    /// <summary>Base keyword access</summary>
    public Map BaseAccessExpression = "(local.get $this)";
    
    /// <summary>Post-increment expression</summary>
    public Map PostIncrementExpression = @"(block (result i32)
  (local.get {operand})
  (local.set {operand} (i32.add (local.get {operand}) (i32.const 1)))
)";
    
    /// <summary>Post-decrement expression</summary>
    public Map PostDecrementExpression = @"(block (result i32)
  (local.get {operand})
  (local.set {operand} (i32.sub (local.get {operand}) (i32.const 1)))
)";
    
    /// <summary>
    /// Object creation expression - allocates memory for new object.
    /// AutomaticModel.Build() provides deallocation point information for this allocation.
    /// The generated WASM includes the allocation; deallocation is inserted at the computed point.
    /// </summary>
    public Map ObjectCreationExpression = @";; new {type}() - allocation site tracked by CTGC
(struct.new ${type}
{args}
)";
    
    /// <summary>Delegate creation expression</summary>
    public Map DelegateCreationExpression = @";; new delegate {type}
(ref.func ${expr})";
    
    /// <summary>
    /// Anonymous object creation - allocation tracked by AutomaticModel.
    /// </summary>
    public Map AnonymousObjectCreationExpression = @";; new { ... } - anonymous object allocation
(struct.new $AnonymousType
{initializer}
)";
    
    /// <summary>
    /// Array creation expression - allocation tracked by AutomaticModel.
    /// </summary>
    public Map ArrayCreationExpression = @";; new T[...] - array allocation
(array.new ${type}
{dims}
)";
    
    /// <summary>Implicit array creation</summary>
    public Map ImplicitArrayCreationExpression = @"(array.new $implicit
{initializer}
)";
    
    /// <summary>Typeof expression</summary>
    public Map TypeofExpression = @";; typeof({type})
(i32.const 0) ;; type token";
    
    /// <summary>Is expression</summary>
    public Map IsExpression = @";; {expr} is {pattern}
(i32.const 1) ;; pattern match";
    
    /// <summary>As expression (safe cast)</summary>
    public Map AsExpression = @";; {expr} as {type}
(block (result (ref null {type}))
  {expr}
)";
    
    /// <summary>Lambda expression dispatcher</summary>
    public Map LambdaExpression = "{lambda}";
    
    /// <summary>Simple lambda expression</summary>
    public Map SimpleLambdaExpression = @"(func ${parameter}
{body}
)";
    
    /// <summary>Parenthesized lambda expression</summary>
    public Map ParenthesizedLambdaExpression = @"(func ({parameters})
{body}
)";
    
    /// <summary>Anonymous method expression</summary>
    public Map AnonymousMethodExpression = @"(func ({parameters})
{body}
)";
    
    /// <summary>Stackalloc expression</summary>
    public Map StackallocExpression = @";; stackalloc {type}[{size}]
(i32.const 0) ;; stack pointer";
    
    /// <summary>With expression (record with)</summary>
    public Map WithExpression = @";; {expr} with {initializer}
(struct.new ${type} {expr} {initializer})";
    
    /// <summary>Tuple expression</summary>
    public Map TupleExpression = @"(struct.new $Tuple
{elements}
)";
    
    /// <summary>Collection expression (C# 12+)</summary>
    public Map CollectionExpression = @"(array.new_default $collection
{elements}
)";
    
    // ============================================================
    // LITERALS
    // ============================================================
    
    /// <summary>
    /// Literal dispatcher - passes through to specific literal type.
    /// No Map needed because Literal rule is a pure dispatcher that doesn't create its own AST node.
    /// Each literal type (TrueLiteral, DecimalIntegerLiteral, etc.) has its own Map below.
    /// </summary>
    
    /// <summary>Integer literal (decimal) - Typed implementation</summary>
    public Map<AstNode, string> DecimalIntegerLiteral = TypedMap.For<string>()
        .Emit(node => WasmEmit.EmitIntegerLiteral(node));
    
    /// <summary>Hexadecimal integer literal</summary>
    public Map HexIntegerLiteral = "(i32.const {lexeme})";
    
    /// <summary>Binary integer literal</summary>
    public Map BinaryIntegerLiteral = "(i32.const {lexeme})";
    
    /// <summary>Floating-point literal</summary>
    public Map FloatLiteral = "(f32.const {lexeme})";
    
    /// <summary>Double literal</summary>
    public Map DoubleLiteral = "(f64.const {lexeme})";
    
    /// <summary>String literal - requires data section</summary>
    public Map StringLiteral = @";; string ""{lexeme}""
(i32.const {offset})";
    
    /// <summary>Character literal</summary>
    public Map CharacterLiteral = "(i32.const {lexeme})";
    
    /// <summary>Boolean true</summary>
    public Map TrueLiteral = "(i32.const 1)";
    
    /// <summary>Boolean false</summary>
    public Map FalseLiteral = "(i32.const 0)";
    
    /// <summary>Null literal</summary>
    public Map NullLiteral = "(ref.null any)";
    
    // ============================================================
    // TYPES
    // ============================================================
    
    /// <summary>Type dispatcher</summary>
    public Map Type = "{base}{suffixes}";
    
    /// <summary>Primitive type - signed byte</summary>
    public Map SByteType = "i32";
    
    /// <summary>Primitive type - byte</summary>
    public Map ByteType = "i32";
    
    /// <summary>Primitive type - short</summary>
    public Map Int16Type = "i32";
    
    /// <summary>Primitive type - ushort</summary>
    public Map UInt16Type = "i32";
    
    /// <summary>Primitive type - int</summary>
    public Map Int32Type = "i32";
    
    /// <summary>Primitive type - uint</summary>
    public Map UInt32Type = "i32";
    
    /// <summary>Primitive type - long</summary>
    public Map Int64Type = "i64";
    
    /// <summary>Primitive type - ulong</summary>
    public Map UInt64Type = "i64";
    
    /// <summary>Primitive type - float</summary>
    public Map Float32Type = "f32";
    
    /// <summary>Primitive type - double</summary>
    public Map Float64Type = "f64";
    
    /// <summary>Primitive type - bool</summary>
    public Map BooleanType = "i32";
    
    /// <summary>Primitive type - char</summary>
    public Map CharType = "i32";
    
    /// <summary>Primitive type - decimal (represented as struct with i64 components for 128-bit precision)</summary>
    public Map DecimalType = "(ref $Decimal)";
    
    /// <summary>Primitive type - nint (native int, pointer-sized): i32 for 32-bit targets, i64 for 64-bit targets</summary>
    public Map NIntType = "i64 ;; Native pointer-sized integer (64-bit default, configurable for 32-bit targets)";
    
    /// <summary>Primitive type - nuint (native uint, pointer-sized): i32 for 32-bit targets, i64 for 64-bit targets</summary>
    public Map NUIntType = "i64 ;; Native pointer-sized unsigned integer (64-bit default, configurable for 32-bit targets)";
    
    /// <summary>String type</summary>
    public Map StringType = "(ref $String)";
    
    /// <summary>Object type</summary>
    public Map ObjectType = "(ref $Object)";
    
    /// <summary>Reference type</summary>
    public Map ReferenceType = "(ref ${name})";
    
    /// <summary>Nullable value type</summary>
    public Map NullableType = "(ref null {type})";
    
    /// <summary>Array type</summary>
    public Map ArrayType = "(ref $Array_{element})";
    
    /// <summary>Pointer type (unsafe)</summary>
    public Map PointerType = "i32";
    
    /// <summary>Void type (no result)</summary>
    public Map VoidType = "";
    
    /// <summary>Dynamic type (treated as object)</summary>
    public Map DynamicType = "(ref $Object)";
    
    /// <summary>Tuple type</summary>
    public Map TupleType = "(ref $Tuple_{elements})";
    
    // ============================================================
    // NAMES AND IDENTIFIERS
    // ============================================================
    
    /// <summary>Simple name (identifier)</summary>
    public Map SimpleName = "{name}{typeArgs}";
    
    /// <summary>Identifier name</summary>
    public Map IdentifierName = "";  // Just the name, no WASM code - used in namespaces, types, etc.
    
    /// <summary>Qualified name (namespace.type)</summary>
    public Map QualifiedName = "{global}{segments}";
    
    // ============================================================
    // MODIFIERS AND ATTRIBUTES
    // ============================================================
    
    /// <summary>Modifiers list</summary>
    public Map Modifiers = ";; modifiers: {mods}";
    
    /// <summary>Single modifier</summary>
    public Map Modifier = "{mod}";
    
    /// <summary>Attribute sections (ignored in basic WASM)</summary>
    public Map AttributeSections = "";
    
    // ============================================================
    // PARAMETERS AND ARGUMENTS
    // ============================================================
    
    /// <summary>Formal parameter list - emit WASM parameter declarations</summary>
    public Map<AstNode, string> FormalParameterList = TypedMap.For<string>()
        .Emit(node => {
            if (node == null || !node.Fields.ContainsKey("params")) return "";
            
            var paramsField = node.Fields["params"];
            if (paramsField == null) return "";
            
            // Process the params field to extract parameters
            return EmitParameterList(paramsField);
        });
    
    /// <summary>
    /// Emit parameter list from params field.
    /// </summary>
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
    
    /// <summary>
    /// Emit a single parameter.
    /// </summary>
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
            wasmType = WasmEmit.MapCSharpTypeToWasm(typeStr);
        }
        
        return $"(param ${name} {wasmType})";
    }
    
    /// <summary>
    /// Map a type AstNode to WASM type.
    /// </summary>
    private static string MapTypeNodeToWasm(AstNode typeNode)
    {
        return ExtractTypeFromNode(typeNode);
    }
    
    /// <summary>Formal parameter list</summary>
    public Map OLD_FormalParameterList = "{params}";
    
    /// <summary>Fixed parameter</summary>
    public Map FixedParameter = "(param ${name} {type})";
    
    /// <summary>Argument list</summary>
    public Map ArgumentList = "{first}{rest}";
    
    /// <summary>Positional argument</summary>
    public Map PositionalArgument = "{expr}";
    
    /// <summary>Named argument</summary>
    public Map NamedArgument = "{expr}";
    
    /// <summary>Expression list</summary>
    public Map ExpressionList = "{first} {rest}";
    
    // ============================================================
    // INTERFACE AND ENUM DECLARATIONS
    // ============================================================
    
    /// <summary>
    /// Interface declaration - lowered to WASM struct type.
    /// Full OOP requires:
    /// - Interface method dispatch tables (similar to vtables)
    /// - Type casting and interface implementation checking
    /// - Default interface implementation support (C# 8+)
    /// </summary>
    public Map InterfaceDeclaration = @";; interface {name}
(type ${name} (struct
{body}
))";
    
    /// <summary>Interface body</summary>
    public Map InterfaceBody = "{members}";
    
    /// <summary>Enum declaration</summary>
    public Map EnumDeclaration = @";; enum {name}
(type ${name} (i32))";
    
    /// <summary>Enum body</summary>
    public Map EnumBody = @";; enum members
{members}";
    
    /// <summary>Enum member</summary>
    public Map EnumMemberDeclaration = "(global ${name} i32 (i32.const {value}))";
    
    /// <summary>
    /// Delegate declaration - lowered to WASM function type.
    /// Delegates require:
    /// - Function pointer storage
    /// - Instance object storage (for instance methods)
    /// - Invoke method generation
    /// - Multi-cast support for combining delegates
    /// </summary>
    public Map DelegateDeclaration = @";; delegate {returnType} {name}({parameters})
(type ${name} (func (param {parameters}) (result {returnType})))";
    
    /// <summary>Record declaration (C# 9+)</summary>
    public Map RecordDeclaration = @";; record {name}
(type ${name} (struct
{body}
))";
    
    // ============================================================
    // PROPERTIES AND EVENTS
    // ============================================================
    
    /// <summary>Property declaration</summary>
    public Map PropertyDeclaration = @";; property {type} {name}
(field ${name}_backing {type})
{accessors}";
    
    /// <summary>Event declaration</summary>
    public Map EventDeclaration = @";; event {type} {name}
(field ${name}_event {type})";
    
    /// <summary>Indexer declaration</summary>
    public Map IndexerDeclaration = @";; indexer {type}[{parameters}]
(func ${name}_get (param {parameters}) (result {type}))
(func ${name}_set (param {parameters}) (param value {type}))";
    
    // ============================================================
    // OPERATORS
    // ============================================================
    
    /// <summary>Assignment operator</summary>
    public Map AssignmentOperator = "local.set";
    
    /// <summary>Addition operator</summary>
    public Map AddOperator = "i32.add";
    
    /// <summary>Subtraction operator</summary>
    public Map SubtractOperator = "i32.sub";
    
    /// <summary>Multiplication operator</summary>
    public Map MultiplyOperator = "i32.mul";
    
    /// <summary>Division operator</summary>
    public Map DivideOperator = "i32.div_s";
    
    /// <summary>Modulo operator</summary>
    public Map ModuloOperator = "i32.rem_s";
    
    /// <summary>Equality operator</summary>
    public Map EqualsOperator = "i32.eq";
    
    /// <summary>Inequality operator</summary>
    public Map NotEqualsOperator = "i32.ne";
    
    /// <summary>Less than operator</summary>
    public Map LessThanOperator = "i32.lt_s";
    
    /// <summary>Greater than operator</summary>
    public Map GreaterThanOperator = "i32.gt_s";
    
    /// <summary>Less than or equal operator</summary>
    public Map LessThanOrEqualOperator = "i32.le_s";
    
    /// <summary>Greater than or equal operator</summary>
    public Map GreaterThanOrEqualOperator = "i32.ge_s";
    
    /// <summary>Logical AND</summary>
    public Map LogicalAndOperator = "i32.and";
    
    /// <summary>Logical OR</summary>
    public Map LogicalOrOperator = "i32.or";
    
    /// <summary>Logical NOT</summary>
    public Map LogicalNotOperator = "i32.eqz";
    
    /// <summary>Bitwise AND</summary>
    public Map BitwiseAndOperator = "i32.and";
    
    /// <summary>Bitwise OR</summary>
    public Map BitwiseOrOperator = "i32.or";
    
    /// <summary>Bitwise XOR</summary>
    public Map BitwiseXorOperator = "i32.xor";
    
    /// <summary>Bitwise NOT - XOR with -1 to flip all bits</summary>
    public Map BitwiseNotOperator = @"(i32.xor
  {operand}
  (i32.const -1))";
    
    /// <summary>Left shift operator</summary>
    public Map LeftShiftOperator = "i32.shl";
    
    /// <summary>Right shift operator</summary>
    public Map RightShiftOperator = "i32.shr_s";
    
    /// <summary>Unsigned right shift operator (C# 11+)</summary>
    public Map UnsignedRightShiftOperator = "i32.shr_u";
    
    /// <summary>Relational operator dispatcher</summary>
    public Map RelationalOperator = "";  // Handled by EmitBinaryExpression
    
    /// <summary>Shift operator dispatcher</summary>
    public Map ShiftOperator = "";  // Handled by EmitBinaryExpression
    
    /// <summary>Unary operator dispatcher</summary>
    public Map UnaryOperator = "{op}";
    
    // ============================================================
    // PATTERNS (for pattern matching)
    // ============================================================
    
    /// <summary>Pattern dispatcher</summary>
    public Map Pattern = ";; pattern {type}";
    
    /// <summary>Declaration pattern</summary>
    public Map DeclarationPattern = ";; declaration pattern";
    
    /// <summary>Constant pattern</summary>
    public Map ConstantPattern = ";; constant pattern";
    
    /// <summary>Type pattern</summary>
    public Map TypePattern = ";; type pattern";
    
    // ============================================================
    // LINQ QUERY EXPRESSIONS
    // ============================================================
    
    /// <summary>Query expression</summary>
    public Map QueryExpression = @";; LINQ query
{from}
{body}";
    
    /// <summary>From clause</summary>
    public Map FromClause = @";; from {name} in {source}";
    
    /// <summary>Query body</summary>
    public Map QueryBody = "{clauses}";
    
    // ============================================================
    // USING AND LOCK STATEMENTS
    // ============================================================
    
    /// <summary>Using statement</summary>
    public Map UsingStatement = @"(block $using
{resource}
{body}
  ;; dispose resource
)";
    
    /// <summary>Lock statement</summary>
    public Map LockStatement = @"(block $lock
  ;; acquire lock on {expr}
{body}
  ;; release lock
)";
    
    /// <summary>
    /// Unsafe block - verified by ManualModel.
    /// ManualModel.Build() verifies that all pointer operations in this block are safe.
    /// No unsafe code is allowed without verification proof.
    /// </summary>
    public Map UnsafeStatement = @"(block $unsafe
  ;; WARNING: Use 'manual' keyword instead of 'unsafe'
  ;; ManualModel verifies all pointer operations
{body}
)";
    
    /// <summary>
    /// Manual block - verified by ManualModel.
    /// ManualModel.Build() performs:
    /// 1. Ownership graph construction for all pointers
    /// 2. Abstract interpretation of all paths
    /// 3. Symbolic execution for verification
    /// 4. Proof that no undefined behavior can occur
    /// Compilation fails if verification fails.
    /// </summary>
    public Map ManualStatement = @"(block $manual
  ;; Verified manual memory management block
  ;; ManualModel proved: no leaks, no use-after-free, no undefined behavior
{body}
)";
    
    /// <summary>
    /// Fixed statement - pins managed memory for pointer access.
    /// Verified by ManualModel to ensure pointer doesn't escape the block.
    /// </summary>
    public Map FixedStatement = @"(block $fixed
  ;; fixed ({declaration}) - pointer pinned in scope
  ;; ManualModel verifies pointer doesn't escape
{declaration}
{body}
  ;; pointer unpinned here
)";
    
    // ============================================================
    // ACCESSORS AND PROPERTIES (Extended)
    // ============================================================
    
    /// <summary>Property or indexer accessor (get/set/init)</summary>
    public Map Accessor = @"(func ${name}_{kind}
  (param {params})
  (result {result})
{body}
)";
    
    /// <summary>Accessor body</summary>
    public Map AccessorBody = "{body}";
    
    /// <summary>List of accessors</summary>
    public Map AccessorDeclarations = "{accessors}";
    
    /// <summary>Accessor kind (get, set, init, add, remove)</summary>
    public Map AccessorKind = "{kind}";
    
    /// <summary>Accessor list</summary>
    public Map AccessorList = "{accessors}";
    
    /// <summary>Member accessor in member access expression</summary>
    public Map MemberAccessor = "{member}";
    
    /// <summary>Event declaration with accessors</summary>
    public Map EventDeclarationWithAccessors = @";; event {type} {name}
(field ${name}_event {type})
{accessors}";
    
    /// <summary>Property pattern for pattern matching</summary>
    public Map PropertyPattern = ";; property pattern {name} = {pattern}";
    
    // ============================================================
    // ATTRIBUTES
    // ============================================================
    
    /// <summary>Single attribute</summary>
    public Map Attribute = ";; [{name}({args})]";
    
    /// <summary>Attribute argument list</summary>
    public Map AttributeArgumentList = "{args}";
    
    /// <summary>Attribute arguments</summary>
    public Map AttributeArguments = "{args}";
    
    /// <summary>Attribute list</summary>
    public Map AttributeList = "{attributes}";
    
    /// <summary>Attribute section</summary>
    public Map AttributeSection = ";; {target} {attributes}";
    
    /// <summary>Attribute target (assembly, module, etc.)</summary>
    public Map AttributeTarget = "{target}";
    
    /// <summary>Attribute target specifier</summary>
    public Map AttributeTargetSpecifier = "{target}:";
    
    /// <summary>Global attribute section</summary>
    public Map GlobalAttributeSection = ";; global {attributes}";
    
    /// <summary>Global attribute target</summary>
    public Map GlobalAttributeTarget = "{target}";
    
    // ============================================================
    // ARRAYS (Extended)
    // ============================================================
    
    /// <summary>Array dimensions specification</summary>
    public Map ArrayDimensions = "[{dims}]";
    
    /// <summary>Array initializer expression</summary>
    public Map ArrayInitializer = "{elements}";
    
    /// <summary>Array rank specifier (e.g., [])</summary>
    public Map ArrayRankSpecifier = "[{commas}]";
    
    /// <summary>Multiple array rank specifiers</summary>
    public Map ArrayRankSpecifiers = "{ranks}";
    
    /// <summary>Non-array type</summary>
    public Map NonArrayType = "{type}";
    
    /// <summary>Parameter array (params)</summary>
    public Map ParameterArray = "(param ${name} (ref $Array_{type}))";
    
    // ============================================================
    // EXCEPTION HANDLING (Extended)
    // ============================================================
    
    /// <summary>Single catch clause</summary>
    public Map CatchClause = @"(block $catch_{type}
  ;; catch {type} {name}
{handler}
)";
    
    /// <summary>List of catch clauses</summary>
    public Map CatchClauses = "{clauses}";
    
    /// <summary>Catch filter (when clause)</summary>
    public Map CatchFilter = @"(if {condition}
  (then
    ;; execute catch handler
  )
)";
    
    /// <summary>Finally clause</summary>
    public Map FinallyClause = @"(block $finally
  ;; finally
{body}
)";
    
    // ============================================================
    // GENERICS AND CONSTRAINTS
    // ============================================================
    
    /// <summary>Allows constraint (C# 13+)</summary>
    public Map AllowsConstraint = ";; allows {constraint}";
    
    /// <summary>Constructor constraint (new())</summary>
    public Map ConstructorConstraint = ";; where T : new()";
    
    /// <summary>Primary constraint (class, struct, unmanaged, notnull)</summary>
    public Map PrimaryConstraint = ";; where T : {constraint}";
    
    /// <summary>Secondary constraint (interface, base class)</summary>
    public Map SecondaryConstraint = ";; where T : {type}";
    
    /// <summary>Type parameter constraint</summary>
    public Map TypeParameterConstraint = ";; {constraint}";
    
    /// <summary>Type parameter constraints</summary>
    public Map TypeParameterConstraints = "{constraints}";
    
    /// <summary>Type parameter constraints clause</summary>
    public Map TypeParameterConstraintsClause = ";; where {name} : {constraints}";
    
    /// <summary>Type parameter constraints clauses</summary>
    public Map TypeParameterConstraintsClauses = "{clauses}";
    
    /// <summary>
    /// Type parameter - represents a generic type parameter like T.
    /// Full generic implementation requires:
    /// - Monomorphization (creating concrete versions for each instantiation)
    /// - Or type erasure with runtime type information
    /// - Constraint checking at instantiation sites
    /// - Generic method specialization
    /// </summary>
    public Map TypeParameter = "{attrs}{variance}{name}";
    
    /// <summary>
    /// Type parameter list - <T1, T2, ...>
    /// For WASM: generics are typically handled via monomorphization
    /// </summary>
    public Map TypeParameterList = "{parameters}";
    
    /// <summary>Type parameters</summary>
    public Map TypeParameters = "{parameters}";
    
    /// <summary>Variance annotation (in, out)</summary>
    public Map VarianceAnnotation = "{variance}";
    
    // ============================================================
    // PATTERNS (Extended)
    // ============================================================
    
    /// <summary>Variable designation</summary>
    public Map Designation = "{name}";
    
    /// <summary>Designation list</summary>
    public Map DesignationList = "{designations}";
    
    /// <summary>Designation rest (additional designations)</summary>
    public Map DesignationRest = "{rest}";
    
    /// <summary>Discard designation (_)</summary>
    public Map DiscardDesignation = ";; discard _";
    
    /// <summary>Discard pattern (_)</summary>
    public Map DiscardPattern = ";; discard pattern _";
    
    /// <summary>Is pattern suffix in expression</summary>
    public Map IsPatternSuffix = " is {pattern}";
    
    /// <summary>List pattern [...]</summary>
    public Map ListPattern = ";; list pattern [{elements}]";
    
    /// <summary>List pattern element rest</summary>
    public Map ListPatternElementRest = "{rest}";
    
    /// <summary>List pattern elements</summary>
    public Map ListPatternElements = "{elements}";
    
    /// <summary>List pattern slice (..)</summary>
    public Map ListPatternSlice = "..{pattern}";
    
    /// <summary>Logical AND pattern (pattern1 and pattern2)</summary>
    public Map LogicalAndPattern = ";; {left} and {right}";
    
    /// <summary>NOT pattern (not pattern)</summary>
    public Map NotPattern = ";; not {pattern}";
    
    /// <summary>Parenthesized designation</summary>
    public Map ParenthesizedDesignation = "({designations})";
    
    /// <summary>Parenthesized pattern</summary>
    public Map ParenthesizedPattern = "({pattern})";
    
    /// <summary>Pattern AND suffix</summary>
    public Map PatternAndSuffix = " and {pattern}";
    
    /// <summary>Pattern OR suffix</summary>
    public Map PatternOrSuffix = " or {pattern}";
    
    /// <summary>Positional pattern Type(...)</summary>
    public Map PositionalPattern = ";; {type}({subpatterns})";
    
    /// <summary>Primary pattern</summary>
    public Map PrimaryPattern = "{pattern}";
    
    /// <summary>Recursive pattern Type { ... }</summary>
    public Map RecursivePattern = ";; {type} {{ {properties} }}";
    
    /// <summary>Relational pattern (< > <= >=)</summary>
    public Map RelationalPattern = ";; {op} {value}";
    
    /// <summary>Slice pattern ..</summary>
    public Map SlicePattern = "..";
    
    /// <summary>Subpattern in positional/property pattern</summary>
    public Map Subpattern = "{pattern}";
    
    /// <summary>Subpattern list</summary>
    public Map SubpatternList = "{subpatterns}";
    
    /// <summary>Subpattern rest</summary>
    public Map SubpatternRest = "{rest}";
    
    /// <summary>Var pattern (var x)</summary>
    public Map VarPattern = ";; var {designation}";
    
    /// <summary>When clause in pattern matching</summary>
    public Map WhenClause = " when {condition}";
    
    // ============================================================
    // OPERATORS (Extended)
    // ============================================================
    
    /// <summary>Additive operator (+ or -)</summary>
    public Map AdditiveOperator = "";  // Handled by EmitBinaryExpression
    
    /// <summary>Conversion operator declaration</summary>
    public Map ConversionOperatorDeclaration = @"(func $op_{kind}_{type}
  (param $value {sourceType})
  (result {targetType})
{body}
)";
    
    /// <summary>Equality operator (== or !=)</summary>
    public Map EqualityOperator = "";  // Handled by EmitBinaryExpression
    
    /// <summary>Multiplicative operator (*, /, %)</summary>
    public Map MultiplicativeOperator = "";  // Handled by EmitBinaryExpression
    
    /// <summary>Operator declaration</summary>
    public Map OperatorDeclaration = @"(func $op_{operator}
  (param {parameters})
  (result {returnType})
{body}
)";
    
    /// <summary>Overloadable operator</summary>
    public Map OverloadableOperator = "{operator}";
    
    // ============================================================
    // ARGUMENTS AND PARAMETERS (Extended)
    // ============================================================
    
    /// <summary>Single argument</summary>
    public Map Argument = "{expr}";
    
    /// <summary>Argument modifier (ref, out, in)</summary>
    public Map ArgumentModifier = "{modifier}";
    
    /// <summary>Argument rest (additional arguments)</summary>
    public Map ArgumentRest = "{arg}";
    
    /// <summary>Named argument list</summary>
    public Map NamedArgumentList = "{args}";
    
    /// <summary>Positional argument list</summary>
    public Map PositionalArgumentList = "{args}";
    
    /// <summary>Parameter modifier (ref, out, in, this, params)</summary>
    public Map ParameterModifier = "{modifier}";
    
    /// <summary>Formal parameter</summary>
    public Map FormalParameter = "(param ${name} {type})";
    
    /// <summary>Formal parameter list content</summary>
    public Map FormalParameterListContent = "{params}";
    
    /// <summary>Fixed parameters list</summary>
    public Map FixedParameters = "{first}{rest}";
    
    // ============================================================
    // STATEMENTS (Extended)
    // ============================================================
    
    /// <summary>Checked statement block</summary>
    public Map CheckedStatement = @"(block $checked
  ;; checked
{body}
)";
    
    /// <summary>Unchecked statement block</summary>
    public Map UncheckedStatement = @"(block $unchecked
  ;; unchecked
{body}
)";
    
    /// <summary>Case label in switch</summary>
    public Map CaseLabel = "(br_table $case_{value})";
    
    /// <summary>Default label in switch</summary>
    public Map DefaultLabel = "(br $default)";
    
    /// <summary>Switch label</summary>
    public Map SwitchLabel = "{label}";
    
    /// <summary>Switch labels</summary>
    public Map SwitchLabels = "{labels}";
    
    /// <summary>Switch section</summary>
    public Map SwitchSection = @"{labels}
{statements}";
    
    /// <summary>Switch sections</summary>
    public Map SwitchSections = "{sections}";
    
    /// <summary>Switch expression arm</summary>
    public Map SwitchExpressionArm = "{pattern} => {expression}";
    
    /// <summary>Switch expression arm rest</summary>
    public Map SwitchExpressionArmRest = "{rest}";
    
    /// <summary>Switch expression arms</summary>
    public Map SwitchExpressionArms = "{arms}";
    
    /// <summary>Goto case target</summary>
    public Map GotoCaseTarget = "(br $case_{value})";
    
    /// <summary>Goto default target</summary>
    public Map GotoDefaultTarget = "(br $default)";
    
    /// <summary>Goto target</summary>
    public Map GotoTarget = "(br ${label})";
    
    /// <summary>Else clause in if statement</summary>
    public Map ElseClause = @"
  (else
{stmt}
  )";
    
    /// <summary>Yield statement</summary>
    public Map YieldStatement = @";; yield {kind} {expr}
{expr}";
    
    /// <summary>Yield return</summary>
    public Map YieldReturn = "(return {expr})";
    
    /// <summary>Yield break</summary>
    public Map YieldBreak = "(br $yield_break)";
    
    /// <summary>Yield kind (return or break)</summary>
    public Map YieldKind = "{kind}";
    
    /// <summary>Local function statement</summary>
    public Map LocalFunctionStatement = @"(func ${name}
  (param {parameters})
  (result {returnType})
{body}
)";
    
    /// <summary>Local function modifier</summary>
    public Map LocalFunctionModifier = "{modifier}";
    
    /// <summary>Local function modifiers</summary>
    public Map LocalFunctionModifiers = "{modifiers}";
    
    /// <summary>Constructor initializer (this or base)</summary>
    public Map ConstructorInitializer = @";; {kind}({args})
(call ${kind}_ctor {args})";
    
    /// <summary>Destructor declaration</summary>
    public Map DestructorDeclaration = @"(func $finalize
{body}
)";
    
    // ============================================================
    // VARIABLE DECLARATIONS (Extended)
    // ============================================================
    
    /// <summary>Local declaration statement</summary>
    public Map LocalDeclaration = "{type} {declarators}";
    
    /// <summary>Local variable declarator</summary>
    public Map LocalVariableDeclarator = "(local ${name} {type} {init})";
    
    /// <summary>Local variable declarators</summary>
    public Map LocalVariableDeclarators = "{declarators}";
    
    /// <summary>Local variable initializer</summary>
    public Map LocalVariableInitializer = "{expr}";
    
    /// <summary>Local variable modifier (const, ref, etc.)</summary>
    public Map LocalVariableModifier = "{modifier}";
    
    /// <summary>Local variable type</summary>
    public Map LocalVariableType = "{type}";
    
    /// <summary>Constant declarator</summary>
    public Map ConstantDeclarator = "(global ${name} {type} {value})";
    
    /// <summary>Constant declarators</summary>
    public Map ConstantDeclarators = "{declarators}";
    
    /// <summary>Variable declarator</summary>
    public Map VariableDeclarator = "{name} = {init}";
    
    /// <summary>Variable declarators</summary>
    public Map VariableDeclarators = "{declarators}";
    
    /// <summary>Variable initializer</summary>
    public Map VariableInitializer = "{expr}";
    
    /// <summary>Variable initializer list</summary>
    public Map VariableInitializerList = "{initializers}";
    
    /// <summary>Variable initializer rest</summary>
    public Map VariableInitializerRest = "{rest}";
    
    // ============================================================
    // EXPRESSIONS (Extended)
    // ============================================================
    
    /// <summary>Expression body (=> expr)</summary>
    public Map ExpressionBody = @"(return
{expr}
)";
    
    /// <summary>Method body</summary>
    public Map MethodBody = "{body}";
    
    /// <summary>Primary expression core</summary>
    public Map PrimaryExpressionCore = "{expr}";
    
    /// <summary>Unary expression base</summary>
    public Map UnaryExpressionBase = "{expr}";
    
    /// <summary>Unary expression suffix</summary>
    public Map UnaryExpressionSuffix = "{suffix}";
    
    /// <summary>Unary expression suffixes</summary>
    public Map UnaryExpressionSuffixes = "{suffixes}";
    
    /// <summary>Member access suffix (.member)</summary>
    public Map MemberAccessSuffix = "{accessor}.{member}{typeArgs}";
    
    /// <summary>Element access suffix ([index])</summary>
    public Map ElementAccessSuffix = "[{indices}]";
    
    /// <summary>Invocation suffix (method call)</summary>
    public Map InvocationSuffix = "({args})";
    
    /// <summary>Post-increment suffix (++)</summary>
    public Map PostIncrementSuffix = "++";
    
    /// <summary>Post-decrement suffix (--)</summary>
    public Map PostDecrementSuffix = "--";
    
    /// <summary>As type suffix (as Type)</summary>
    public Map AsTypeSuffix = " as {type}";
    
    /// <summary>With expression suffix (with {...})</summary>
    public Map WithExpressionSuffix = " with {initializer}";
    
    /// <summary>Member initializer</summary>
    public Map MemberInitializer = "{member} = {expr}";
    
    /// <summary>Member initializer list</summary>
    public Map MemberInitializerList = "{initializers}";
    
    /// <summary>Member initializer rest</summary>
    public Map MemberInitializerRest = "{rest}";
    
    /// <summary>Object initializer {...}</summary>
    public Map ObjectInitializer = "{initializers}";
    
    /// <summary>Collection element in collection expression</summary>
    public Map CollectionElement = "{element}";
    
    /// <summary>Collection element list</summary>
    public Map CollectionElementList = "{elements}";
    
    /// <summary>Collection element rest</summary>
    public Map CollectionElementRest = "{rest}";
    
    /// <summary>Stackalloc initializer</summary>
    public Map StackallocInitializer = "{elements}";
    
    // ============================================================
    // TYPES (Extended)
    // ============================================================
    
    /// <summary>Primitive type dispatcher</summary>
    public Map PrimitiveType = "{type}";
    
    /// <summary>Integral type - most map to i32 in WASM (except long/ulong)</summary>
    public Map IntegralType = "i32";
    
    /// <summary>Floating point type - default to f64</summary>
    public Map FloatingPointType = "f64";
    
    /// <summary>Named type (user-defined type)</summary>
    public Map NamedType = "(ref ${name}{typeArgs})";
    
    /// <summary>Ref type (ref T)</summary>
    public Map RefType = "(ref {type})";
    
    /// <summary>Nullable suffix (?)</summary>
    public Map NullableSuffix = "?";
    
    /// <summary>Pointer suffix (*)</summary>
    public Map PointerSuffix = "*";
    
    /// <summary>Type suffix</summary>
    public Map TypeSuffix = "{suffix}";
    
    /// <summary>Type suffixes</summary>
    public Map TypeSuffixes = "{suffixes}";
    
    /// <summary>Type argument list</summary>
    public Map TypeArgumentList = "<{arguments}>";
    
    /// <summary>Type arguments</summary>
    public Map TypeArguments = "{arguments}";
    
    /// <summary>Function pointer type (C# 9+)</summary>
    public Map FunctionPointerType = "i32 ;; function pointer";
    
    /// <summary>Function pointer signature</summary>
    public Map FunctionPointerSignature = "{returnType}({parameters})";
    
    /// <summary>Function pointer parameters</summary>
    public Map FunctionPointerParameters = "{parameters}";
    
    // ============================================================
    // TUPLES (Extended)
    // ============================================================
    
    /// <summary>Tuple element</summary>
    public Map TupleElement = "{type} {name}";
    
    /// <summary>Tuple elements</summary>
    public Map TupleElements = "{elements}";
    
    /// <summary>Tuple expression element</summary>
    public Map TupleExpressionElement = "{expr}";
    
    /// <summary>Tuple expression element rest</summary>
    public Map TupleExpressionElementRest = "{rest}";
    
    /// <summary>Tuple expression elements</summary>
    public Map TupleExpressionElements = "{elements}";
    
    // ============================================================
    // NAMES AND NAMESPACES (Extended)
    // ============================================================
    
    /// <summary>Name segment</summary>
    public Map NameSegment = "{name}{typeArgs}";
    
    /// <summary>Name segment rest</summary>
    public Map NameSegmentRest = "{segment}";
    
    /// <summary>Name segments</summary>
    public Map NameSegments = "{first}{rest}";
    
    /// <summary>Compilation unit item (using, namespace, type) - manually process since typed Maps can't return placeholders</summary>
    public Map<AstNode, string> CompilationUnitItem = TypedMap.For<string>()
        .Emit(node => {
            if (node == null) return "";
            
            
            if (!node.Fields.ContainsKey("item")) return "";
            var item = node.Fields["item"];
            if (!(item is AstNode itemNode)) return "";
            
            
            // Process based on item type
            if (itemNode.Type.Contains("Using"))
                return ";; using ;";
            
            if (itemNode.Type == "NamespaceMemberDeclaration")
            {
                // Unwrap to get the actual member
                if (itemNode.Fields.ContainsKey("member") && itemNode.Fields["member"] is AstNode member)
                {
                    
                    // Could be NamespaceDeclaration or TypeDeclaration
                    if (member.Type == "NamespaceDeclaration")
                    {
                        return ProcessNamespaceDeclarationInline(member);
                    }
                    else
                    {
                        return ProcessTypeDeclaration(member);
                    }
                }
                else
                {
                }
            }
            
            return "";
        });
    
    /// <summary>
    /// Process a NamespaceDeclaration inline.
    /// </summary>
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

    
    /// <summary>File-scoped namespace declaration (C# 10+)</summary>
    public Map FileScopedNamespaceDeclaration = @";; namespace {name};
{members}";

    
    /// <summary>Global prefix (::)</summary>
    public Map GlobalPrefix = "::";
    
    /// <summary>Extern alias directive</summary>
    public Map ExternAliasDirective = ";; extern alias {name};";
    
    /// <summary>Using directive</summary>
    public Map UsingDirective = ";; {directive}";
    
    /// <summary>Using namespace directive</summary>
    public Map UsingNamespaceDirective = "using ;";
    
    /// <summary>Using alias directive</summary>
    public Map UsingAliasDirective = "using ;";
    
    /// <summary>Using static directive</summary>
    public Map UsingStaticDirective = ";; using static {type};";
    
    // ============================================================
    // INTERFACE MEMBERS (Extended)
    // ============================================================
    
    /// <summary>Interface member declaration</summary>
    public Map InterfaceMemberDeclaration = "{member}";
    
    /// <summary>Interface member declarations</summary>
    public Map InterfaceMemberDeclarations = "{members}";
    
    /// <summary>Interface method declaration</summary>
    public Map InterfaceMethodDeclaration = @"(func ${name}
  (param {parameters})
  (result {returnType})
)";
    
    /// <summary>Interface method body (C# 8+)</summary>
    public Map InterfaceMethodBody = "{body}";
    
    /// <summary>Interface property declaration</summary>
    public Map InterfacePropertyDeclaration = @";; property {type} {name}
{accessors}";
    
    /// <summary>Interface event declaration</summary>
    public Map InterfaceEventDeclaration = ";; event {type} {name};";
    
    /// <summary>Interface indexer declaration</summary>
    public Map InterfaceIndexerDeclaration = @";; {type} this[{parameters}]
{accessors}";
    
    // ============================================================
    // RECORDS (Extended)
    // ============================================================
    
    /// <summary>Record body</summary>
    public Map RecordBody = "{members}";
    
    /// <summary>Record parameter list</summary>
    public Map RecordParameterList = "{parameters}";
    
    // ============================================================
    // CLASS/TYPE MEMBERS (Extended)
    // ============================================================
    
    /// <summary>Base list (inheritance)</summary>
    public Map BaseList = ": {types}";
    
    /// <summary>Base type</summary>
    public Map BaseType = "{type}";
    
    /// <summary>Base types</summary>
    public Map BaseTypes = "{types}";
    
    /// <summary>Enum base type</summary>
    public Map EnumBase = ": {type}";
    
    /// <summary>Enum member declarations</summary>
    public Map EnumMemberDeclarations = "{members}";
    
    // ============================================================
    // LINQ QUERY EXPRESSIONS (Extended)
    // ============================================================
    
    /// <summary>Query body clause</summary>
    public Map QueryBodyClause = "{clause}";
    
    /// <summary>Query body clauses</summary>
    public Map QueryBodyClauses = "{clauses}";
    
    /// <summary>Query continuation (into)</summary>
    public Map QueryContinuation = @";; into {name}
{body}";
    
    /// <summary>Select clause</summary>
    public Map SelectClause = @";; select {expr}";
    
    /// <summary>Group clause</summary>
    public Map GroupClause = @";; group {element} by {key}";
    
    /// <summary>Select or group clause</summary>
    public Map SelectOrGroupClause = "{clause}";
    
    /// <summary>Where clause</summary>
    public Map WhereClause = @";; where {condition}";
    
    /// <summary>Let clause</summary>
    public Map LetClause = @";; let {name} = {expr}";
    
    /// <summary>Join clause</summary>
    public Map JoinClause = @";; join {name} in {source} on {left} equals {right}";
    
    /// <summary>Join into clause</summary>
    public Map JoinIntoClause = @" into {name}";
    
    /// <summary>Orderby clause</summary>
    public Map OrderbyClause = @";; orderby {orderings}";
    
    /// <summary>Ordering</summary>
    public Map Ordering = "{expr} {direction}";
    
    /// <summary>Ordering direction (ascending, descending)</summary>
    public Map OrderingDirection = "{direction}";
    
    /// <summary>Ordering rest</summary>
    public Map OrderingRest = "{rest}";
    
    /// <summary>Orderings</summary>
    public Map Orderings = "{orderings}";
    
    // ============================================================
    // FOREACH AND FOR LOOPS (Extended)
    // ============================================================
    
    /// <summary>Foreach modifier (await)</summary>
    public Map ForEachModifier = "{modifier}";
    
    /// <summary>For initializer</summary>
    public Map ForInitializer = "{init}";
    
    /// <summary>For iterator</summary>
    public Map ForIterator = "{iterator}";
    
    // ============================================================
    // LAMBDA EXPRESSIONS (Extended)
    // ============================================================
    
    /// <summary>Lambda body (expression or block)</summary>
    public Map LambdaBody = "{body}";
    
    // ============================================================
    // RESOURCE MANAGEMENT
    // ============================================================
    
    /// <summary>Resource acquisition in using statement</summary>
    public Map ResourceAcquisition = "{resource}";
    
    // ============================================================
    // TOKEN MAPS
    // ============================================================
    // Token nodes are created by the parser for terminals in the grammar.
    // They have a 'lexeme' field containing the matched text.
    // These maps extract the lexeme or map C# types to WASM types.
    
    /// <summary>Identifier token - extract lexeme</summary>
    public Map Identifier = "{lexeme}";
    
    /// <summary>Verbatim identifier (@name) - extract lexeme</summary>
    public Map VerbatimIdentifier = "{lexeme}";
    
    // Type keyword tokens - map C# types to WASM types
    public Map KwInt = "i32";
    public Map KwUint = "i32";
    public Map KwShort = "i32";
    public Map KwUshort = "i32";
    public Map KwByte = "i32";
    public Map KwSbyte = "i32";
    public Map KwLong = "i64";
    public Map KwUlong = "i64";
    public Map KwFloat = "f32";
    public Map KwDouble = "f64";
    public Map KwBool = "i32";
    public Map KwChar = "i32";
    public Map KwNint = "i32";  // Native int
    public Map KwNuint = "i32"; // Native uint
    
    // Other type keywords
    public Map KwVoid = "";  // void has no WASM type
    public Map KwObject = "(ref any)";
    public Map KwString = "(ref string)";
    public Map KwDecimal = "i64 i64";  // Decimal is 128-bit, represented as two i64s
    public Map KwDynamic = "(ref any)";
    
    // C# 3: var keyword for type inference
    // Note: True type inference requires semantic analysis - for now we default to i32
    // A proper implementation would analyze the initializer expression
    public Map KwVar = "i32";  // Default to i32, should be inferred from initializer
    
    // Keyword tokens that are structural (mapped to empty string as they're handled by containing maps)
    public Map KwClass = "";
    public Map KwStruct = "";
    public Map KwInterface = "";
    public Map KwEnum = "";
    public Map KwNamespace = "";
    public Map KwPublic = "";
    public Map KwPrivate = "";
    public Map KwProtected = "";
    public Map KwInternal = "";
    public Map KwStatic = "";
    public Map KwReadonly = "";
    public Map KwConst = "";
    // OOP modifiers - these require semantic analysis for proper implementation
    public Map KwVirtual = "";   // Virtual methods require vtable dispatch
    public Map KwAbstract = "";  // Abstract members prevent instantiation
    public Map KwSealed = "";    // Sealed prevents inheritance
    public Map KwOverride = "";  // Override requires vtable slot matching
    public Map KwNew = "";
    public Map KwAsync = "";
    public Map KwPartial = "";
    
    // Punctuation tokens
    public Map OpenBrace = "{{";
    public Map CloseBrace = "}}";
    public Map OpenParen = "(";
    public Map CloseParen = ")";
    public Map OpenBracket = "[";
    public Map CloseBracket = "]";
    public Map Semicolon = "";  // Semicolons are structural, handled by containing maps
    public Map Comma = ", ";
    public Map Dot = ".";
    public Map Colon = ":";
    
    // ============================================================
    // FALLBACK
    // ============================================================
    
    /// <summary>
    /// Fallback map for unmapped AST nodes.
    /// Generates diagnostic comment for unsupported constructs.
    /// Note: This should rarely be used - most C# constructs should have explicit maps.
    /// </summary>
    public Map Fallback = @";; TODO: Add map for this construct
nop";
}
        
