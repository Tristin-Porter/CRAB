using CDTk;

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
class WASM : MapSet
{
    // ============================================================
    // MODULE STRUCTURE
    // ============================================================
    
    /// <summary>Top-level compilation unit - generates complete WASM module</summary>
    public Map CompilationUnit = @"(module
  ;; Imports
  (import ""env"" ""memory"" (memory 1))
  
  ;; Generated members
{members}
)";
    
    /// <summary>Namespace member declarations</summary>
    public Map NamespaceMemberDeclarations = "{members}";
    
    /// <summary>Single namespace member</summary>
    public Map NamespaceMemberDeclaration = "{member}";
    
    /// <summary>Namespace declaration (flattened in WASM)</summary>
    public Map NamespaceDeclaration = @";; namespace {name}
{body}";
    
    /// <summary>Namespace body</summary>
    public Map NamespaceBody = "{members}";
    
    // ============================================================
    // TYPE DECLARATIONS
    // ============================================================
    
    /// <summary>Type declaration dispatcher</summary>
    public Map TypeDeclaration = "{type}";
    
    /// <summary>Class declaration - mapped to struct type in WASM</summary>
    public Map ClassDeclaration = @";; class {name}
(type ${name} (struct
{body}
))";
    
    /// <summary>Class body</summary>
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
    
    /// <summary>Method declaration - primary compilation target</summary>
    public Map MethodDeclaration = @"(func ${name}
  (param {parameters})
  (result {returnType})
{body}
)";
    
    /// <summary>Field declaration - mapped to struct field</summary>
    public Map FieldDeclaration = "(field ${name} {type})";
    
    /// <summary>Constructor declaration</summary>
    public Map ConstructorDeclaration = @"(func ${name}_ctor
  (param $this (ref ${name}))
{body}
)";
    
    // ============================================================
    // STATEMENTS
    // ============================================================
    
    /// <summary>Block statement</summary>
    public Map BlockStatement = @"(block
{statements}
)";
    
    /// <summary>Expression statement</summary>
    public Map ExpressionStatement = "{expression}";
    
    /// <summary>Return statement</summary>
    public Map ReturnStatement = "(return {expression})";
    
    /// <summary>If statement</summary>
    public Map IfStatement = @"(if {condition}
  (then
{thenBranch}
  )
  (else
{elseBranch}
  )
)";
    
    /// <summary>While loop</summary>
    public Map WhileStatement = @"(loop $while
  (br_if $while {condition})
{body}
)";
    
    /// <summary>Variable declaration</summary>
    public Map LocalVariableDeclaration = "(local ${name} {type})";
    
    // ============================================================
    // EXPRESSIONS
    // ============================================================
    
    /// <summary>Binary expression - arithmetic/logical operations</summary>
    public Map BinaryExpression = "({operator} {left} {right})";
    
    /// <summary>Unary expression</summary>
    public Map UnaryExpression = "({operator} {operand})";
    
    /// <summary>Assignment expression</summary>
    public Map AssignmentExpression = "(local.set ${target} {value})";
    
    /// <summary>Method invocation</summary>
    public Map InvocationExpression = "(call ${method} {arguments})";
    
    /// <summary>Identifier reference</summary>
    public Map IdentifierName = "(local.get ${name})";
    
    /// <summary>Member access expression</summary>
    public Map MemberAccessExpression = "(struct.get ${type} ${member} {target})";
    
    /// <summary>Object creation expression</summary>
    public Map ObjectCreationExpression = @"(struct.new ${type}
{arguments}
)";
    
    // ============================================================
    // LITERALS
    // ============================================================
    
    /// <summary>Integer literal</summary>
    public Map IntegerLiteral = "(i32.const {value})";
    
    /// <summary>String literal - requires data section</summary>
    public Map StringLiteral = @";; string ""{value}""
(i32.const {offset})";
    
    /// <summary>Boolean true</summary>
    public Map TrueLiteral = "(i32.const 1)";
    
    /// <summary>Boolean false</summary>
    public Map FalseLiteral = "(i32.const 0)";
    
    /// <summary>Null literal</summary>
    public Map NullLiteral = "(ref.null)";
    
    // ============================================================
    // TYPES
    // ============================================================
    
    /// <summary>Primitive type int</summary>
    public Map Int32Type = "i32";
    
    /// <summary>Primitive type long</summary>
    public Map Int64Type = "i64";
    
    /// <summary>Primitive type float</summary>
    public Map Float32Type = "f32";
    
    /// <summary>Primitive type double</summary>
    public Map Float64Type = "f64";
    
    /// <summary>Primitive type bool</summary>
    public Map BooleanType = "i32";
    
    /// <summary>Reference type</summary>
    public Map ReferenceType = "(ref ${name})";
    
    /// <summary>Void type (no result)</summary>
    public Map VoidType = "";
    
    // ============================================================
    // OPERATORS
    // ============================================================
    
    /// <summary>Addition operator</summary>
    public Map AddOperator = "i32.add";
    
    /// <summary>Subtraction operator</summary>
    public Map SubtractOperator = "i32.sub";
    
    /// <summary>Multiplication operator</summary>
    public Map MultiplyOperator = "i32.mul";
    
    /// <summary>Division operator</summary>
    public Map DivideOperator = "i32.div_s";
    
    /// <summary>Equality operator</summary>
    public Map EqualsOperator = "i32.eq";
    
    /// <summary>Inequality operator</summary>
    public Map NotEqualsOperator = "i32.ne";
    
    /// <summary>Less than operator</summary>
    public Map LessThanOperator = "i32.lt_s";
    
    /// <summary>Greater than operator</summary>
    public Map GreaterThanOperator = "i32.gt_s";
    
    /// <summary>Logical AND</summary>
    public Map LogicalAndOperator = "i32.and";
    
    /// <summary>Logical OR</summary>
    public Map LogicalOrOperator = "i32.or";
    
    /// <summary>Logical NOT</summary>
    public Map LogicalNotOperator = "i32.eqz";
    
    // ============================================================
    // FALLBACK
    // ============================================================
    
    /// <summary>
    /// Fallback map for unmapped AST nodes.
    /// Generates a comment indicating the node type needs implementation.
    /// </summary>
    public Map Fallback = ";; TODO: Implement {type} mapping to WASM";
}