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
    
    /// <summary>Statement dispatcher</summary>
    public Map Statement = "{stmt}";
    
    /// <summary>Embedded statement</summary>
    public Map EmbeddedStatement = "{stmt}";
    
    /// <summary>Block statement</summary>
    public Map Block = @"(block
{stmts}
)";
    
    /// <summary>Statements list</summary>
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
    
    /// <summary>If statement</summary>
    public Map IfStatement = @"(if {condition}
  (then
{thenStmt}
  )
  (else
{elseClause}
  )
)";
    
    /// <summary>Switch statement</summary>
    public Map SwitchStatement = @"(block $switch
  ;; switch on {expr}
{sections}
)";
    
    /// <summary>Iteration statement dispatcher</summary>
    public Map IterationStatement = "{stmt}";
    
    /// <summary>While loop</summary>
    public Map WhileStatement = @"(loop $while
  (br_if $while {condition})
{body}
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
    
    /// <summary>Return statement</summary>
    public Map ReturnStatement = "(return {expr})";
    
    /// <summary>Throw statement</summary>
    public Map ThrowStatement = @";; throw {expr}
(unreachable)";
    
    /// <summary>Try-catch-finally statement</summary>
    public Map TryStatement = @"(block $try
{body}
{handlers}
{finallyClause}
)";
    
    /// <summary>Variable declaration</summary>
    public Map LocalVariableDeclaration = "(local ${name} {type})";
    
    /// <summary>Constant declaration</summary>
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
    
    /// <summary>Additive expression</summary>
    public Map AdditiveExpression = "({op} {left} {right})";
    
    /// <summary>Multiplicative expression</summary>
    public Map MultiplicativeExpression = "({op} {left} {right})";
    
    /// <summary>Switch expression (C# 8+)</summary>
    public Map SwitchExpression = @"(block $switch_expr
  ;; switch expression on {input}
{arms}
)";
    
    /// <summary>Range expression (C# 8+)</summary>
    public Map RangeExpression = @";; range {start}..{end}
(struct.new $Range {start} {end})";
    
    /// <summary>Unary expression dispatcher</summary>
    public Map UnaryExpression = "{expr}";
    
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
    
    /// <summary>Sizeof expression</summary>
    public Map SizeofExpression = @";; sizeof({type})
(i32.const 4)";
    
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
    
    /// <summary>Member access expression</summary>
    public Map MemberAccessExpression = "(struct.get ${type} ${member} {target})";
    
    /// <summary>Method invocation</summary>
    public Map InvocationExpression = "(call ${target} {args})";
    
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
    
    /// <summary>Object creation expression</summary>
    public Map ObjectCreationExpression = @"(struct.new ${type}
{args}
)";
    
    /// <summary>Delegate creation expression</summary>
    public Map DelegateCreationExpression = @";; new delegate {type}
(ref.func ${expr})";
    
    /// <summary>Anonymous object creation</summary>
    public Map AnonymousObjectCreationExpression = @"(struct.new $AnonymousType
{initializer}
)";
    
    /// <summary>Array creation expression</summary>
    public Map ArrayCreationExpression = @"(array.new ${type}
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
    
    /// <summary>Literal dispatcher</summary>
    public Map Literal = "{value}";
    
    /// <summary>Integer literal (decimal)</summary>
    public Map DecimalIntegerLiteral = "(i32.const {value})";
    
    /// <summary>Hexadecimal integer literal</summary>
    public Map HexIntegerLiteral = "(i32.const {value})";
    
    /// <summary>Binary integer literal</summary>
    public Map BinaryIntegerLiteral = "(i32.const {value})";
    
    /// <summary>Floating-point literal</summary>
    public Map FloatLiteral = "(f32.const {value})";
    
    /// <summary>Double literal</summary>
    public Map DoubleLiteral = "(f64.const {value})";
    
    /// <summary>String literal - requires data section</summary>
    public Map StringLiteral = @";; string ""{value}""
(i32.const {offset})";
    
    /// <summary>Character literal</summary>
    public Map CharacterLiteral = "(i32.const {value})";
    
    /// <summary>Boolean true</summary>
    public Map TrueLiteral = "(i32.const 1)";
    
    /// <summary>Boolean false</summary>
    public Map FalseLiteral = "(i32.const 0)";
    
    /// <summary>Null literal</summary>
    public Map NullLiteral = "(ref.null)";
    
    // ============================================================
    // TYPES
    // ============================================================
    
    /// <summary>Type dispatcher</summary>
    public Map Type = "{type}";
    
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
    
    /// <summary>Primitive type - decimal (maps to i64 for now)</summary>
    public Map DecimalType = "i64";
    
    /// <summary>Primitive type - nint (native int, C# 9+)</summary>
    public Map NIntType = "i32";
    
    /// <summary>Primitive type - nuint (native uint, C# 9+)</summary>
    public Map NUIntType = "i32";
    
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
    public Map SimpleName = "{name}";
    
    /// <summary>Identifier name</summary>
    public Map IdentifierName = "(local.get ${name})";
    
    /// <summary>Qualified name (namespace.type)</summary>
    public Map QualifiedName = "{name}";
    
    // ============================================================
    // MODIFIERS AND ATTRIBUTES
    // ============================================================
    
    /// <summary>Modifiers list</summary>
    public Map Modifiers = ";; modifiers: {mods}";
    
    /// <summary>Single modifier</summary>
    public Map Modifier = "";
    
    /// <summary>Attribute sections (ignored in basic WASM)</summary>
    public Map AttributeSections = "";
    
    // ============================================================
    // PARAMETERS AND ARGUMENTS
    // ============================================================
    
    /// <summary>Formal parameter list</summary>
    public Map FormalParameterList = "{parameters}";
    
    /// <summary>Fixed parameter</summary>
    public Map FixedParameter = "(param ${name} {type})";
    
    /// <summary>Argument list</summary>
    public Map ArgumentList = "{args}";
    
    /// <summary>Positional argument</summary>
    public Map PositionalArgument = "{expr}";
    
    /// <summary>Named argument</summary>
    public Map NamedArgument = "{expr}";
    
    /// <summary>Expression list</summary>
    public Map ExpressionList = "{first} {rest}";
    
    // ============================================================
    // INTERFACE AND ENUM DECLARATIONS
    // ============================================================
    
    /// <summary>Interface declaration</summary>
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
    
    /// <summary>Delegate declaration</summary>
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
    
    /// <summary>Bitwise NOT</summary>
    public Map BitwiseNotOperator = "i32.const -1\ni32.xor";
    
    /// <summary>Left shift operator</summary>
    public Map LeftShiftOperator = "i32.shl";
    
    /// <summary>Right shift operator</summary>
    public Map RightShiftOperator = "i32.shr_s";
    
    /// <summary>Unsigned right shift operator (C# 11+)</summary>
    public Map UnsignedRightShiftOperator = "i32.shr_u";
    
    /// <summary>Relational operator dispatcher</summary>
    public Map RelationalOperator = "{op}";
    
    /// <summary>Shift operator dispatcher</summary>
    public Map ShiftOperator = "{op}";
    
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
    
    // ============================================================
    // FALLBACK
    // ============================================================
    
    /// <summary>
    /// Fallback map for unmapped AST nodes.
    /// Generates a comment indicating the node type needs implementation.
    /// </summary>
    public Map Fallback = ";; TODO: Implement {type} mapping to WASM";
}