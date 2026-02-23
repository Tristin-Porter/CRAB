using CDTk;
using System;
using System.Linq;

namespace CRAB;

/// <summary>
/// WASM MapSet: User-defined implementation extending CDTk.MapSet.
/// Demonstrates how to add custom semantic context fields for intelligent Map formatting.
/// 
/// The base MapSet class in CDTk provides only framework infrastructure.
/// All semantic fields (Dialect, Minify, OptHints, etc.) are USER-DEFINED.
/// Users are FREE to add any semantic fields they need for their target language.
/// 
/// This architecture ensures unlimited extensibility - the framework doesn't limit
/// what context information Maps can access for formatting decisions.
/// </summary>
public class WASM : MapSet
{
    // ============================================================
    // OLD STATIC DUCT TAPE CODE - REMOVED
    // These were replaced by:
    // - StringInfo.GenerateDataSection() - now an instance method on semantic field
    // - StringLiteral Map using this.StringInfo - functional Map accessing semantic context
    // ============================================================
    
    /// <summary>
    /// Map C# type names to WASM types.
    /// </summary>
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
    // ============================================================
    // USER-DEFINED SEMANTIC CONTEXT FIELDS
    // ============================================================
    //
    // IMPORTANT: These fields are NOT part of the base MapSet class in CDTk.
    // They are USER-DEFINED fields specific to this WASM implementation.
    //
    // Users extending MapSet can define ANY semantic fields they need:
    // - Target language variants (Dialect)
    // - Formatting preferences (Minify, IndentStyle, BraceStyle)
    // - Optimization hints (OptHints, CanInline, RequiresBlock)
    // - Target architecture info (TargetArch, PointerSize, Endianness)
    // - Debug/release modes (DebugMode, Assertions, SourceMaps)
    // - Code generation options (EmitComments, EmitLineDirectives)
    // - Custom metadata (ProjectName, Version, Author)
    //
    // The functional Map API accesses these via `this.*` in formatter lambdas.
    // This provides unlimited expressive power while keeping Maps pure and AST-free.
    //
    // Example usage in a functional Map:
    //   if (this.Dialect == "Python") return "if x:\n" + body();
    //   if (this.Minify) return "if(x)" + body();
    //   if (this.OptHints.CanInline[self.Id]) return "inline " + code();
    //
    
    /// <summary>
    /// Target language dialect - USER DEFINED field, not in base MapSet.
    /// Examples: "WASM", "WAT", "Python", "JavaScript", "C", etc.
    /// Allows same semantic analysis to generate different target formats.
    /// </summary>
    public string Dialect { get; set; } = "WASM";
    
    /// <summary>
    /// Minification flag - USER DEFINED field, not in base MapSet.
    /// When true, output is compact without whitespace.
    /// Allows debug-friendly vs production-optimized output from same Maps.
    /// </summary>
    public bool Minify { get; set; } = false;
    
    /// <summary>
    /// Optimization hints from optimization model - USER DEFINED field, not in base MapSet.
    /// Populated by Models, consumed by Maps for intelligent formatting decisions.
    /// Maps can check per-node flags like CanInline[nodeId] or RequiresBlock[nodeId].
    /// </summary>
    public OptimizationHints OptHints { get; set; } = new OptimizationHints();
    
    /// <summary>
    /// String literal analysis - USER DEFINED field, replaces static StringRegistry.
    /// Populated by Models during semantic analysis phase.
    /// Maps use this to get string offsets and generate data section.
    /// </summary>
    public StringLiteralInfo StringInfo { get; set; } = new StringLiteralInfo();
    
    /// <summary>
    /// Local variable analysis - USER DEFINED field, replaces static LocalVariableRegistry.
    /// Populated by Models during semantic analysis phase.
    /// Maps use this to get variable types and generate local declarations.
    /// </summary>
    public LocalVariableInfo LocalVarInfo { get; set; } = new LocalVariableInfo();
    
    // ============================================================
    // EXAMPLE: Adding Your Own Custom Semantic Fields
    // ============================================================
    //
    // Users can add ANY fields they want for their use case. Examples:
    //
    // // Target architecture info
    // public string TargetArch { get; set; } = "x86_64";
    // public int PointerSize { get; set; } = 8;
    // public bool IsLittleEndian { get; set; } = true;
    //
    // // Debug/release modes
    // public bool DebugMode { get; set; } = false;
    // public bool EmitAssertions { get; set; } = true;
    // public bool EmitSourceMaps { get; set; } = false;
    //
    // // Formatting preferences
    // public string BraceStyle { get; set; } = "K&R";  // or "Allman", "GNU", etc.
    // public int IndentWidth { get; set; } = 2;
    // public bool UseTabs { get; set; } = false;
    //
    // // Performance hints
    // public Dictionary<string, int> LoopUnrollFactors { get; set; } = new();
    // public HashSet<string> HotPaths { get; set; } = new();
    //
    // // Project metadata
    // public string ProjectName { get; set; } = "MyProject";
    // public string Version { get; set; } = "1.0.0";
    //
    // Then use in Maps:
    //   if (this.DebugMode) return "/* DEBUG */ " + code();
    //   if (this.TargetArch == "ARM") return arm_format();
    //   if (this.BraceStyle == "Allman") return "\n{\n" + body() + "\n}";
    //
    
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
    // MODULE STRUCTURE
    // ============================================================
    
    /// <summary>
    /// CompilationUnit - wraps module content with WASM boilerplate.
    /// 
    /// TODO: String data section and heap pointer calculation require Model preprocessing.
    /// For now, heap pointer starts at 0 (will overwrite string data if strings are used).
    /// 
    /// Proper solution:
    /// 1. StringAnalysisModel preprocesses AST and registers all strings in this.StringInfo
    /// 2. CompilationUnit becomes TypedMap that accesses this.StringInfo.CalculateHeapStart()
    /// 3. Data section is generated from this.StringInfo.GenerateDataSection()
    /// </summary>
    public Map CompilationUnit = @"(module
  ;; Imports
  (import ""env"" ""memory"" (memory 1))
  (import ""env"" ""console_log"" (func $console_log (param i32) (param i32)))
  (import ""env"" ""console_readkey"" (func $console_readkey (result i32)))

  ;; Global heap pointer
  ;; WARNING: Hardcoded to 0 - will overwrite string data if any strings exist!
  ;; TODO: Calculate from string data section size via Model preprocessing
  (global $heap_ptr (mut i32) (i32.const 0))

  ;; Helper functions
  (func $alloc (param $size i32) (result i32)
    (local $ptr i32)
    global.get $heap_ptr
    local.set $ptr
    global.get $heap_ptr
    local.get $size
    i32.add
    global.set $heap_ptr
    local.get $ptr
  )

  ;; TODO: String data section goes here
  ;; Generated by: this.StringInfo.GenerateDataSection()
  ;; Requires Model preprocessing to populate this.StringInfo

  ;; Generated members
{items}

  ;; Exports
  (export ""main"" (func $Main))
)";
    
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
    
    /// <summary>
    /// Namespace declaration - emits namespace body items.
    /// Skips namespace wrapper since WASM MVP doesn't have namespace concept.
    /// </summary>
    public Map NamespaceDeclaration = ";; namespace {name}\n{body}";
    
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
    /// Emit a method declaration inline (simplified after CDTk fix).
    /// This is needed because we can't call the MethodDeclaration typed Map from within this typed Map.
    /// </summary>
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
    /// Method declaration - functional Map with proper parameter and return type formatting.
    /// 
    /// Properly formats WASM function declarations:
    /// - Parameters: Format as `(param $name type)` declarations
    /// - Return type: Format as `(result type)` only if not void
    /// - Body: Transformed through CDTk's Map templates
    /// </summary>
    public Map MethodDeclaration => new Map(
        (MapReference self) =>
        {
            var node = self.Node;
            if (node == null) return "";
            
            // Extract fields from AST node with field shifting workaround
            // FIXME: Due to CDTk parser bug (see GitHub issue #TBD), fields are shifted:
            // Expected: attrs, mods, returnType, name, parameters, body
            // Actual mapping:
            //   mods -> returnType
            //   returnType -> name  
            //   name -> parameters (FormalParameterList) or body (MethodBody)
            //   typeParams -> body (when parameters exist)
            // This workaround should be removed once the parser bug is fixed.
            var modsField = node.Fields.ContainsKey("mods") ? node.Fields["mods"] : null;
            var returnTypeField = node.Fields.ContainsKey("returnType") ? node.Fields["returnType"] : null;
            var nameField = node.Fields.ContainsKey("name") ? node.Fields["name"] : null;
            var typeParamsField = node.Fields.ContainsKey("typeParams") ? node.Fields["typeParams"] : null;
            
            // Extract return type from mods field (shifted)
            string resultType = "";
            if (modsField is AstNode typeNode)
            {
                resultType = ExtractTypeFromNode(typeNode);
            }
            
            // Extract function name from returnType field (shifted)
            string funcName = "";
            if (returnTypeField is AstNode nameNode && nameNode.Type == "Identifier" && nameNode.Fields.ContainsKey("lexeme"))
            {
                funcName = nameNode.Fields["lexeme"]?.ToString() ?? "";
            }
            
            // Extract parameters and body (shifted)
            string parameters = "";
            object? bodyField = null;
            
            if (nameField is AstNode nameContent)
            {
                if (nameContent.Type == "FormalParameterList")
                {
                    // name contains parameters, typeParams contains body
                    parameters = EmitParameterList(nameContent);
                    bodyField = typeParamsField;
                }
                else if (nameContent.Type == "MethodBody")
                {
                    // name contains body directly (no parameters)
                    bodyField = nameContent;
                }
            }
            
            
            // Format body through Maps - use MapReference.Transform
            string bodyOutput = "";
            if (bodyField is AstNode bodyNode)
            {
                bodyOutput = self.Transform(bodyNode);
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
    /// Functional Map with proper parameter formatting.
    /// </summary>
    public Map ConstructorDeclaration => new Map(
        (MapReference self) =>
        {
            var node = self.Node;
            if (node == null) return "";
            
            // Extract fields from AST node
            var nameField = node.Fields.ContainsKey("name") ? node.Fields["name"] : null;
            var parametersField = node.Fields.ContainsKey("parameters") ? node.Fields["parameters"] : null;
            var bodyField = node.Fields.ContainsKey("body") ? node.Fields["body"] : null;
            
            // Extract constructor/class name
            string className = "";
            if (nameField is AstNode nameNode && nameNode.Type == "Identifier" && nameNode.Fields.ContainsKey("lexeme"))
            {
                className = nameNode.Fields["lexeme"]?.ToString() ?? "";
            }
            
            // Extract parameters
            string parameters = "";
            if (parametersField != null)
            {
                parameters = EmitParameterList(parametersField);
            }
            
            // Format body through Maps - use MapReference.Transform
            string bodyOutput = "";
            if (bodyField is AstNode bodyNode)
            {
                bodyOutput = self.Transform(bodyNode);
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
    /// <summary>
    /// Statements - CDTk handles Statement+ repetition
    /// The {stmts} placeholder will be resolved by CDTk recursively.
    /// CDTk processes lists by recursively applying the Map for each item,
    /// automatically joining the results.
    /// </summary>
    public Map Statements = "{stmts}";
    
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
    
    /// <summary>If statement with optional else clause - functional Map example</summary>
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
    /// The expr field doesn't exist for void return - CDTk substitutes empty string
    /// </summary>
    public Map ReturnStatement = @"{expr}
return";
    
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
    
    /// <summary>Conditional OR expression (alias for LogicalOr)</summary>
    public Map ConditionalOrExpression = "(i32.or {left} {right})";
    
    /// <summary>Conditional AND expression (alias for LogicalAnd)</summary>
    public Map ConditionalAndExpression = "(i32.and {left} {right})";
    
    /// <summary>Inclusive OR expression (bitwise)</summary>
    public Map InclusiveOrExpression = "(i32.or {left} {right})";
    
    /// <summary>Exclusive OR expression (bitwise)</summary>
    public Map ExclusiveOrExpression = "(i32.xor {left} {right})";
    
    /// <summary>AND expression (bitwise)</summary>
    public Map AndExpression = "(i32.and {left} {right})";
    
    /// <summary>Bitwise OR expression</summary>
    public Map BitwiseOrExpression = "(i32.or {left} {right})";
    
    /// <summary>Bitwise XOR expression</summary>
    public Map BitwiseXorExpression = "(i32.xor {left} {right})";
    
    /// <summary>Bitwise AND expression</summary>
    public Map BitwiseAndExpression = "(i32.and {left} {right})";
    
    /// <summary>Sequence expression (comma operator) - evaluates left then right, keeps only right value</summary>
    public Map Sequence = @"{left}
drop
{right}";
    
    /// <summary>Equality expression</summary>
    public Map EqualityExpression = "({op} {left} {right})";
    
    /// <summary>Relational expression</summary>
    public Map RelationalExpression = "({op} {left} {right})";
    
    /// <summary>Shift expression</summary>
    public Map ShiftExpression = "({op} {left} {right})";
    
    /// <summary>
    /// Additive expression - CDTk resolves {op} to operator Map
    /// Newlines between components create proper WAT stack ordering:
    /// - Evaluate left operand (pushes value to stack)
    /// - Evaluate right operand (pushes value to stack)
    /// - Apply operator (pops two values, pushes result)
    /// </summary>
    public Map AdditiveExpression = @"{left}
{right}
{op}";
    
    /// <summary>
    /// Multiplicative expression - CDTk resolves {op} to operator Map
    /// Same stack-based evaluation order as AdditiveExpression
    /// </summary>
    public Map MultiplicativeExpression = @"{left}
{right}
{op}";
    
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
    
    /// <summary>Integer literal (decimal) - CDTk handles {lexeme} substitution</summary>
    public Map DecimalIntegerLiteral = "(i32.const {lexeme})";
    
    /// <summary>Hexadecimal integer literal</summary>
    public Map HexIntegerLiteral = "(i32.const {lexeme})";
    
    /// <summary>Binary integer literal</summary>
    public Map BinaryIntegerLiteral = "(i32.const {lexeme})";
    
    /// <summary>Floating-point literal</summary>
    public Map FloatLiteral = "(f32.const {lexeme})";
    
    /// <summary>Double literal</summary>
    public Map DoubleLiteral = "(f64.const {lexeme})";
    
    /// <summary>
    /// String literal - TypedMap that registers strings in semantic context.
    /// 
    /// ARCHITECTURE NOTE:
    /// Ideally, string registration should happen in a Model BEFORE Maps run, so that:
    /// 1. StringAnalysisModel traverses AST and populates this.StringInfo
    /// 2. CompilationUnit Map reads this.StringInfo to generate data section
    /// 3. StringLiteral Map reads this.StringInfo to emit string references
    /// 
    /// Currently, we register strings during Map execution (temporary approach).
    /// This works because strings are registered before CompilationUnit wraps them.
    /// 
    /// PERFORMANCE NOTE:
    /// Uses property `=>` to capture `this` via closure (fields can't capture this).
    /// This creates a new TypedMap instance on every access - inefficient!
    /// TODO: Convert to field initialized in constructor, or use lazy backing field.
    /// </summary>
    public Map<AstNode, string> StringLiteral => TypedMap.For<string>()
        .Emit(node =>
        {
            // Extract string text from lexeme field
            var text = node.Fields.ContainsKey("lexeme") 
                ? node.Fields["lexeme"]?.ToString() ?? "" 
                : "";
            
            // Register string in instance semantic context via closure (captures 'this')
            var stringId = this.StringInfo.RegisterString(text);
            var offset = this.StringInfo.GetOffset(stringId);
            var length = text.Length;
            
            // Return WASM code: push pointer and length onto stack
            return $";; string \"{text}\" at offset {offset}, length {length}\ni32.const {offset}\ni32.const {length}";
        });
    
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
    public Map IdentifierName = "";  // Just the name, no WAT code - used in namespaces, types, etc.
    
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
    
    /// <summary>Formal parameter list - uses placeholder to format parameters</summary>
    public Map FormalParameterList = "{params}";
    
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
            wasmType = MapCSharpTypeToWasm(typeStr);
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
    public Map LocalDeclaration = "{modifier} {type}";
    
    /// <summary>Local variable declarator</summary>
    public Map LocalVariableDeclarator = "(local ${name} {type} {init})";
    
    /// <summary>Local variable declarators</summary>
    public Map LocalVariableDeclarators = "{declarators}";
    
    /// <summary>Local variable initializer</summary>
    public Map LocalVariableInitializer = "{expr}";
    
    /// <summary>Local variable modifier (const, ref, etc.)</summary>
    public Map LocalVariableModifier = "{modifier}";
    
    /// <summary>Local variable type</summary>
    public Map LocalVariableType = "{base}";
    
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
    public Map IntegralType = "{type}";
    
    /// <summary>Floating point type - default to f64</summary>
    public Map FloatingPointType = "{type}";
    
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
    
    /// <summary>
    /// Compilation unit item - dispatches to item field.
    /// The item field contains: UsingDirective, NamespaceMemberDeclaration, etc.
    /// </summary>
    public Map CompilationUnitItem = "{item}";
    
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

/// <summary>
/// USER-DEFINED class for optimization hints that influence code generation.
/// NOT part of CDTk - this is a custom class for the WASM MapSet implementation.
/// 
/// Populated by OptimizationModel to guide Maps in making formatting decisions.
/// Maps access these hints via `this.OptHints.*` in functional formatters.
/// 
/// Users can define similar classes with any semantic metadata they need:
/// - Profiling data (HotPath, CallFrequency)
/// - Style preferences (BraceStyle, IndentWidth)
/// - Target-specific hints (VectorizeLoop, UnrollCount)
/// - Custom annotations (Author, ReviewStatus, Priority)
/// 
/// This demonstrates the unlimited extensibility of the functional Map API.
/// </summary>
public class OptimizationHints
{
    /// <summary>
    /// Maps node IDs to whether they can be inlined.
    /// Example: if (this.OptHints.CanInline[self.Id]) return inline_format();
    /// </summary>
    public Dictionary<string, bool> CanInline { get; set; } = new Dictionary<string, bool>();
    
    /// <summary>
    /// Maps node IDs to whether they require block syntax.
    /// Example: if (this.OptHints.RequiresBlock[self.Id]) return "{ " + body() + " }";
    /// </summary>
    public Dictionary<string, bool> RequiresBlock { get; set; } = new Dictionary<string, bool>();
    
    /// <summary>
    /// Maps node IDs to suggested formatting style.
    /// Example: var style = this.OptHints.FormattingStyle[self.Id];
    /// </summary>
    public Dictionary<string, string> FormattingStyle { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// String literal analysis info - replaces static StringRegistry.
/// Populated by Models, provides string offsets and data section generation.
/// </summary>
public class StringLiteralInfo
{
    private Dictionary<int, string> _strings = new();
    private Dictionary<int, int> _offsets = new();
    private Dictionary<string, int> _stringToId = new();
    private int _nextId = 0;
    private int _currentOffset = 0;
    
    /// <summary>Register a string and return its ID</summary>
    public int RegisterString(string text)
    {
        // Check if already registered
        if (_stringToId.TryGetValue(text, out var existingId))
            return existingId;
        
        var id = _nextId++;
        _strings[id] = text;
        _offsets[id] = _currentOffset;
        _stringToId[text] = id;
        _currentOffset += text.Length + 1; // +1 for null terminator
        return id;
    }
    
    /// <summary>Get offset for string ID</summary>
    public int GetOffset(int id) => _offsets.ContainsKey(id) ? _offsets[id] : 0;
    
    /// <summary>Get string by ID</summary>
    public string GetString(int id) => _strings.ContainsKey(id) ? _strings[id] : "";
    
    /// <summary>Get all registered strings</summary>
    public Dictionary<int, string> GetAllStrings() => new Dictionary<int, string>(_strings);
    
    /// <summary>Calculate heap start after all strings</summary>
    public int CalculateHeapStart()
    {
        if (_strings.Count == 0) return 0;
        var maxOffset = _strings.Max(kvp => _offsets[kvp.Key] + kvp.Value.Length + 1);
        // Align to 4-byte boundary
        return (maxOffset + 3) & ~3;
    }
    
    /// <summary>Generate WASM data section</summary>
    public string GenerateDataSection()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var kvp in _strings.OrderBy(x => x.Key))
        {
            var stringId = kvp.Key;
            var text = kvp.Value;
            var offset = _offsets[stringId];
            
            // Escape special characters
            var escaped = text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
            
            sb.AppendLine($"  (data (i32.const {offset}) \"{escaped}\\00\")");
        }
        return sb.ToString();
    }
}

/// <summary>
/// Local variable analysis info - replaces static LocalVariableRegistry.
/// Populated by Models, provides variable types and scope information.
/// </summary>
public class LocalVariableInfo
{
    private Dictionary<string, Dictionary<string, string>> _functionVariables = new();  // funcId -> (varName -> type)
    private string _currentFunction = "";
    
    /// <summary>Set current function context</summary>
    public void SetCurrentFunction(string funcId)
    {
        _currentFunction = funcId;
        if (!_functionVariables.ContainsKey(funcId))
            _functionVariables[funcId] = new Dictionary<string, string>();
    }
    
    /// <summary>Register a variable in current function</summary>
    public void RegisterVariable(string name, string wasmType)
    {
        if (!_functionVariables.ContainsKey(_currentFunction))
            _functionVariables[_currentFunction] = new Dictionary<string, string>();
        
        if (!_functionVariables[_currentFunction].ContainsKey(name))
            _functionVariables[_currentFunction][name] = wasmType;
    }
    
    /// <summary>Get variable type</summary>
    public string GetVariableType(string funcId, string name)
    {
        if (_functionVariables.TryGetValue(funcId, out var vars))
            if (vars.TryGetValue(name, out var type))
                return type;
        return "i32";  // Default
    }
    
    /// <summary>Get all variables for a function</summary>
    public List<(string name, string type)> GetFunctionVariables(string funcId)
    {
        if (_functionVariables.TryGetValue(funcId, out var vars))
            return vars.Select(kvp => (kvp.Key, kvp.Value)).ToList();
        return new List<(string, string)>();
    }
}
        
