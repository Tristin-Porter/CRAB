using CDTk;

/// <summary>
/// Complete C# 13 grammar rules for CRAB compiler.
/// Defines the complete syntax tree structure for C# programs.
/// Covers all C# language features including latest C# 13 additions.
/// </summary>
public class Rules : RuleSet
{
    // ============================================================
    // COMPILATION UNIT - Top Level
    // ============================================================

    public Rule CompilationUnit = new Rule("items:CompilationUnitItem+")
        .Returns("items");

    public Rule CompilationUnitItem = new Rule("item:ExternAliasDirective | item:UsingDirective | item:GlobalAttributeSection | item:NamespaceMemberDeclaration")
        .Returns("item");

    // ============================================================
    // EXTERN ALIAS DIRECTIVES
    // ============================================================

    public Rule ExternAliasDirective = new Rule("@KwExtern @KwAlias name:@Identifier @Semicolon")
        .Returns("name");

    // ============================================================
    // USING DIRECTIVES
    // ============================================================

    public Rule UsingDirective = new Rule("directive:UsingAliasDirective | directive:UsingNamespaceDirective | directive:UsingStaticDirective")
        .Returns("directive");

    public Rule UsingAliasDirective = new Rule("@KwUsing alias:@Identifier @Assign name:QualifiedName @Semicolon")
        .Returns("alias", "name");

    public Rule UsingStaticDirective = new Rule("@KwUsing @KwStatic name:QualifiedName @Semicolon")
        .Returns("name");

    public Rule UsingNamespaceDirective = new Rule("global:(@KwGlobal)? @KwUsing name:QualifiedName @Semicolon")
        .Returns("global", "name");

    // ============================================================
    // GLOBAL ATTRIBUTES
    // ============================================================

    public Rule GlobalAttributeSection = new Rule("@OpenBracket target:GlobalAttributeTarget @Colon attributes:AttributeList @CloseBracket")
        .Returns("target", "attributes");

    public Rule GlobalAttributeTarget = new Rule("target:@KwAssembly | target:@KwModule")
        .Returns("target");

    // ============================================================
    // NAMESPACE DECLARATIONS
    // ============================================================

    public Rule NamespaceMemberDeclaration = new Rule("member:NamespaceDeclaration | member:FileScopedNamespaceDeclaration | member:TypeDeclaration")
        .Returns("member");

    public Rule NamespaceDeclaration = new Rule("@KwNamespace name:QualifiedName body:NamespaceBody @Semicolon?")
        .Returns("name", "body");

    // C# 10 File-scoped namespace
    public Rule FileScopedNamespaceDeclaration = new Rule("@KwNamespace name:QualifiedName @Semicolon members:NamespaceMemberDeclarations?")
        .Returns("name", "members");

    public Rule NamespaceBody = new Rule("@OpenBrace items:NamespaceBodyItem* @CloseBrace")
        .Returns("items");

    public Rule NamespaceBodyItem = new Rule("item:ExternAliasDirective | item:UsingDirective | item:NamespaceMemberDeclaration")
        .Returns("item");

    public Rule NamespaceMemberDeclarations = new Rule("members:NamespaceMemberDeclaration+")
        .Returns("members");

    // ============================================================
    // TYPE DECLARATIONS
    // ============================================================

    public Rule TypeDeclaration = new Rule("type:ClassDeclaration | type:StructDeclaration | type:InterfaceDeclaration | type:EnumDeclaration | type:DelegateDeclaration | type:RecordDeclaration")
        .Returns("type");

    // CLASS DECLARATION
    public Rule ClassDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwClass name:@Identifier typeParams:TypeParameterList? baseList:BaseList? constraints:TypeParameterConstraintsClauses? body:ClassBody @Semicolon?")
        .Returns("attrs", "mods", "name", "typeParams", "baseList", "constraints", "body");

    public Rule ClassBody = new Rule("@OpenBrace members:ClassMemberDeclarations? @CloseBrace")
        .Returns("members");

    public Rule ClassMemberDeclarations = new Rule("members:ClassMemberDeclaration+")
        .Returns("members");

    public Rule ClassMemberDeclaration = new Rule("member:FieldDeclaration | member:MethodDeclaration | member:PropertyDeclaration | member:EventDeclaration | member:EventDeclarationWithAccessors | member:IndexerDeclaration | member:OperatorDeclaration | member:ConversionOperatorDeclaration | member:ConstructorDeclaration | member:DestructorDeclaration | member:TypeDeclaration")
        .Returns("member");

    // STRUCT DECLARATION
    public Rule StructDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwStruct name:@Identifier typeParams:TypeParameterList? baseList:BaseList? constraints:TypeParameterConstraintsClauses? body:StructBody @Semicolon?")
        .Returns("attrs", "mods", "name", "typeParams", "baseList", "constraints", "body");

    public Rule StructBody = new Rule("@OpenBrace members:StructMemberDeclarations? @CloseBrace")
        .Returns("members");

    public Rule StructMemberDeclarations = new Rule("members:StructMemberDeclaration+")
        .Returns("members");

    public Rule StructMemberDeclaration = new Rule("member:FieldDeclaration | member:MethodDeclaration | member:PropertyDeclaration | member:EventDeclaration | member:EventDeclarationWithAccessors | member:IndexerDeclaration | member:OperatorDeclaration | member:ConversionOperatorDeclaration | member:ConstructorDeclaration | member:TypeDeclaration")
        .Returns("member");

    // INTERFACE DECLARATION
    public Rule InterfaceDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwInterface name:@Identifier typeParams:TypeParameterList? baseList:BaseList? constraints:TypeParameterConstraintsClauses? body:InterfaceBody @Semicolon?")
        .Returns("attrs", "mods", "name", "typeParams", "baseList", "constraints", "body");

    public Rule InterfaceBody = new Rule("@OpenBrace members:InterfaceMemberDeclarations? @CloseBrace")
        .Returns("members");

    public Rule InterfaceMemberDeclarations = new Rule("members:InterfaceMemberDeclaration+")
        .Returns("members");

    public Rule InterfaceMemberDeclaration = new Rule("member:InterfaceMethodDeclaration | member:InterfacePropertyDeclaration | member:InterfaceEventDeclaration | member:InterfaceIndexerDeclaration")
        .Returns("member");

    // ENUM DECLARATION
    public Rule EnumDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwEnum name:@Identifier baseType:EnumBase? body:EnumBody @Semicolon?")
        .Returns("attrs", "mods", "name", "baseType", "body");

    public Rule EnumBase = new Rule("@Colon type:IntegralType")
        .Returns("type");

    public Rule EnumBody = new Rule("@OpenBrace members:EnumMemberDeclarations? @Comma? @CloseBrace")
        .Returns("members");

    public Rule EnumMemberDeclarations = new Rule("first:EnumMemberDeclaration rest:(@Comma EnumMemberDeclaration)*")
        .Returns("first", "rest");

    public Rule EnumMemberDeclaration = new Rule("attrs:AttributeSections? name:@Identifier value:(@Assign Expression)?")
        .Returns("attrs", "name", "value");

    // DELEGATE DECLARATION
    public Rule DelegateDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwDelegate returnType:Type name:@Identifier typeParams:TypeParameterList? @OpenParen parameters:FormalParameterList? @CloseParen constraints:TypeParameterConstraintsClauses? @Semicolon")
        .Returns("attrs", "mods", "returnType", "name", "typeParams", "parameters", "constraints");

    // RECORD DECLARATION (C# 9)
    public Rule RecordDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwRecord kind:(@KwClass | @KwStruct)? name:@Identifier typeParams:TypeParameterList? paramList:RecordParameterList? baseList:BaseList? constraints:TypeParameterConstraintsClauses? body:RecordBody @Semicolon?")
        .Returns("attrs", "mods", "kind", "name", "typeParams", "paramList", "baseList", "constraints", "body");

    public Rule RecordParameterList = new Rule("@OpenParen parameters:FormalParameterList? @CloseParen")
        .Returns("parameters");

    public Rule RecordBody = new Rule("@OpenBrace members:ClassMemberDeclarations? @CloseBrace")
        .Returns("members");

    // ============================================================
    // MODIFIERS
    // ============================================================

    public Rule Modifiers = new Rule("mods:Modifier+")
        .Returns("mods");

    public Rule Modifier = new Rule("mod:@KwNew | mod:@KwPublic | mod:@KwProtected | mod:@KwInternal | mod:@KwPrivate | mod:@KwAbstract | mod:@KwSealed | mod:@KwStatic | mod:@KwReadonly | mod:@KwVirtual | mod:@KwOverride | mod:@KwExtern | mod:@KwAsync | mod:@KwUnsafe | mod:@KwVolatile | mod:@KwPartial | mod:@KwFile | mod:@KwRequired | mod:@KwManual")
        .Returns("mod");

    // ============================================================
    // TYPE PARAMETERS AND CONSTRAINTS
    // ============================================================

    public Rule TypeParameterList = new Rule("@LessThan parameters:TypeParameters @GreaterThan")
        .Returns("parameters");

    public Rule TypeParameters = new Rule("first:TypeParameter rest:(@Comma TypeParameter)*")
        .Returns("first", "rest");

    public Rule TypeParameter = new Rule("attrs:AttributeSections? variance:VarianceAnnotation? name:@Identifier")
        .Returns("attrs", "variance", "name");

    public Rule VarianceAnnotation = new Rule("variance:@KwIn | variance:@KwOut")
        .Returns("variance");

    public Rule TypeParameterConstraintsClauses = "clauses:TypeParameterConstraintsClause+";

    public Rule TypeParameterConstraintsClause = new Rule("@KwWhere name:@Identifier @Colon constraints:TypeParameterConstraints")
        .Returns("name", "constraints");

    public Rule TypeParameterConstraints = new Rule("first:TypeParameterConstraint rest:(@Comma TypeParameterConstraint)*")
        .Returns("first", "rest");

    public Rule TypeParameterConstraint = new Rule("constraint:PrimaryConstraint | constraint:SecondaryConstraint | constraint:ConstructorConstraint | constraint:AllowsConstraint")
        .Returns("constraint");

    public Rule PrimaryConstraint = new Rule("constraint:@KwClass | constraint:@KwStruct | constraint:@KwNotnull | constraint:@KwUnmanaged | constraint:BaseType")
        .Returns("constraint");

    public Rule SecondaryConstraint = "constraint:BaseType";

    public Rule ConstructorConstraint = "constraint:@KwNew @OpenParen @CloseParen";

    // C# 13 allows constraint
    public Rule AllowsConstraint = new Rule("@KwAllowsConstraint @KwRef @KwStruct")
        .Returns();

    // ============================================================
    // BASE LISTS
    // ============================================================

    public Rule BaseList = new Rule("@Colon types:BaseTypes")
        .Returns("types");

    public Rule BaseTypes = new Rule("first:BaseType rest:(@Comma BaseType)*")
        .Returns("first", "rest");

    public Rule BaseType = "type:QualifiedName typeArgs:TypeArgumentList?";

    // ============================================================
    // MEMBER DECLARATIONS
    // ============================================================

    // FIELD DECLARATION
    public Rule FieldDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? type:Type declarators:VariableDeclarators @Semicolon")
        .Returns("attrs", "mods", "type", "declarators");

    public Rule VariableDeclarators = new Rule("first:VariableDeclarator rest:(@Comma VariableDeclarator)*")
        .Returns("first", "rest");

    public Rule VariableDeclarator = new Rule("name:@Identifier initializer:(@Assign VariableInitializer)?")
        .Returns("name", "initializer");

    public Rule VariableInitializer = new Rule("init:Expression | init:ArrayInitializer")
        .Returns("init");

    // METHOD DECLARATION
    public Rule MethodDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? returnType:Type name:@Identifier typeParams:TypeParameterList? @OpenParen parameters:FormalParameterList? @CloseParen constraints:TypeParameterConstraintsClauses? body:MethodBody")
        .Returns("attrs", "mods", "returnType", "name", "typeParams", "parameters", "constraints", "body");

    public Rule MethodBody = new Rule("body:Block | body:ExpressionBody | body:@Semicolon")
        .Returns("body");

    public Rule ExpressionBody = new Rule("@LambdaArrow expr:Expression @Semicolon")
        .Returns("expr");

    // PROPERTY DECLARATION
    public Rule PropertyDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? type:Type name:@Identifier accessors:AccessorDeclarations initializer:(@Assign Expression)? @Semicolon?")
        .Returns("attrs", "mods", "type", "name", "accessors", "initializer");

    public Rule AccessorDeclarations = new Rule("@OpenBrace accessors:AccessorList @CloseBrace")
        .Returns("accessors");

    public Rule AccessorList = "accessors:Accessor+";

    public Rule Accessor = new Rule("attrs:AttributeSections? mods:Modifiers? kind:AccessorKind body:AccessorBody")
        .Returns("attrs", "mods", "kind", "body");

    public Rule AccessorKind = new Rule("kind:@KwGet | kind:@KwSet | kind:@KwInit | kind:@KwAdd | kind:@KwRemove")
        .Returns("kind");

    public Rule AccessorBody = new Rule("body:Block | body:ExpressionBody | body:@Semicolon")
        .Returns("body");

    // EVENT DECLARATION
    public Rule EventDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwEvent type:Type declarators:VariableDeclarators @Semicolon")
        .Returns("attrs", "mods", "type", "declarators");

    public Rule EventDeclarationWithAccessors = new Rule("attrs:AttributeSections? mods:Modifiers? @KwEvent type:Type name:@Identifier accessors:AccessorDeclarations")
        .Returns("attrs", "mods", "type", "name", "accessors");

    // INDEXER DECLARATION
    public Rule IndexerDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? type:Type @KwThis @OpenBracket parameters:FormalParameterList @CloseBracket accessors:AccessorDeclarations")
        .Returns("attrs", "mods", "type", "parameters", "accessors");

    // OPERATOR DECLARATION
    public Rule OperatorDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? returnType:Type @KwOperator op:OverloadableOperator @OpenParen parameters:FormalParameterList @CloseParen body:MethodBody")
        .Returns("attrs", "mods", "returnType", "op", "parameters", "body");

    public Rule OverloadableOperator = new Rule("op:@Plus | op:@Minus | op:@Multiply | op:@Divide | op:@Modulo | op:@BitwiseAnd | op:@BitwiseOr | op:@BitwiseXor | op:@BitwiseNot | op:@LogicalNot | op:@LeftShift | op:@RightShift | op:@UnsignedRightShift | op:@Equality | op:@Inequality | op:@LessThan | op:@GreaterThan | op:@LessThanOrEqual | op:@GreaterThanOrEqual | op:@Increment | op:@Decrement | op:@KwTrue | op:@KwFalse")
        .Returns("op");

    // CONVERSION OPERATOR DECLARATION
    public Rule ConversionOperatorDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? kind:(@KwImplicit | @KwExplicit) @KwOperator type:Type @OpenParen parameter:FormalParameter @CloseParen body:MethodBody")
        .Returns("attrs", "mods", "kind", "type", "parameter", "body");

    // CONSTRUCTOR DECLARATION
    public Rule ConstructorDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? name:@Identifier @OpenParen parameters:FormalParameterList? @CloseParen initializer:ConstructorInitializer? body:MethodBody")
        .Returns("attrs", "mods", "name", "parameters", "initializer", "body");

    public Rule ConstructorInitializer = new Rule("@Colon kind:(@KwBase | @KwThis) @OpenParen args:ArgumentList? @CloseParen")
        .Returns("kind", "args");

    // DESTRUCTOR DECLARATION
    public Rule DestructorDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @BitwiseNot name:@Identifier @OpenParen @CloseParen body:MethodBody")
        .Returns("attrs", "mods", "name", "body");

    // INTERFACE MEMBER DECLARATIONS
    public Rule InterfaceMethodDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? returnType:Type name:@Identifier typeParams:TypeParameterList? @OpenParen parameters:FormalParameterList? @CloseParen constraints:TypeParameterConstraintsClauses? body:InterfaceMethodBody")
        .Returns("attrs", "mods", "returnType", "name", "typeParams", "parameters", "constraints", "body");

    public Rule InterfaceMethodBody = new Rule("body:@Semicolon | body:Block | body:ExpressionBody")
        .Returns("body");

    public Rule InterfacePropertyDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? type:Type name:@Identifier accessors:AccessorDeclarations")
        .Returns("attrs", "mods", "type", "name", "accessors");

    public Rule InterfaceEventDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? @KwEvent type:Type name:@Identifier @Semicolon")
        .Returns("attrs", "mods", "type", "name");

    public Rule InterfaceIndexerDeclaration = new Rule("attrs:AttributeSections? mods:Modifiers? type:Type @KwThis @OpenBracket parameters:FormalParameterList @CloseBracket accessors:AccessorDeclarations")
        .Returns("attrs", "mods", "type", "parameters", "accessors");

    // ============================================================
    // FORMAL PARAMETERS
    // ============================================================

    public Rule FormalParameterList = new Rule("params:FormalParameterListContent?")
        .Returns("params");

    public Rule FormalParameterListContent = new Rule("params:FixedParameters @Comma paramArray:ParameterArray | params:FixedParameters | params:ParameterArray")
        .Returns("params", "paramArray");

    public Rule FixedParameters = new Rule("first:FixedParameter rest:(@Comma FixedParameter)*")
        .Returns("first", "rest");

    public Rule FixedParameter = new Rule("attrs:AttributeSections? modifier:ParameterModifier? type:Type name:@Identifier default:(@Assign Expression)?")
        .Returns("attrs", "modifier", "type", "name", "default");

    public Rule FormalParameter = new Rule("attrs:AttributeSections? modifier:ParameterModifier? type:Type name:@Identifier")
        .Returns("attrs", "modifier", "type", "name");

    public Rule ParameterModifier = new Rule("mod:@KwRef | mod:@KwOut | mod:@KwIn | mod:@KwParams | mod:@KwThis | mod:@KwScoped")
        .Returns("mod");

    public Rule ParameterArray = new Rule("attrs:AttributeSections? @KwParams type:Type name:@Identifier")
        .Returns("attrs", "type", "name");

    // ============================================================
    // ATTRIBUTES
    // ============================================================

    public Rule AttributeSections = "sections:AttributeSection+";

    public Rule AttributeSection = new Rule("@OpenBracket target:AttributeTarget? attributes:AttributeList @CloseBracket")
        .Returns("target", "attributes");

    public Rule AttributeTarget = new Rule("target:AttributeTargetSpecifier @Colon")
        .Returns("target");

    public Rule AttributeTargetSpecifier = new Rule("target:@KwField | target:@KwEvent | target:@KwMethod | target:@KwParam | target:@KwProperty | target:@KwReturn | target:@KwType | target:@KwAssembly | target:@KwModule")
        .Returns("target");

    public Rule AttributeList = new Rule("first:Attribute rest:(@Comma Attribute)*")
        .Returns("first", "rest");

    public Rule Attribute = new Rule("name:QualifiedName args:AttributeArguments?")
        .Returns("name", "args");

    public Rule AttributeArguments = new Rule("@OpenParen args:AttributeArgumentList? @CloseParen")
        .Returns("args");

    public Rule AttributeArgumentList = new Rule("positional:PositionalArgumentList @Comma named:NamedArgumentList | positional:PositionalArgumentList | named:NamedArgumentList")
        .Returns("positional", "named");

    public Rule PositionalArgumentList = new Rule("first:Expression rest:(@Comma Expression)*")
        .Returns("first", "rest");

    public Rule NamedArgumentList = new Rule("first:NamedArgument rest:(@Comma NamedArgument)*")
        .Returns("first", "rest");

    public Rule NamedArgument = new Rule("name:@Identifier @Assign expr:Expression")
        .Returns("name", "expr");

    // ============================================================
    // TYPES - Fixed left recursion
    // ============================================================

    public Rule Type = new Rule("base:NonArrayType suffixes:TypeSuffixes?")
        .Returns("base", "suffixes");

    public Rule NonArrayType = new Rule("type:PrimitiveType | type:NamedType | type:TupleType | type:FunctionPointerType")
        .Returns("type");

    public Rule TypeSuffixes = new Rule("suffixes:TypeSuffix+")
        .Returns("suffixes");

    public Rule TypeSuffix = new Rule("suffix:ArrayRankSpecifier | suffix:PointerSuffix | suffix:NullableSuffix")
        .Returns("suffix");

    public Rule PointerSuffix = "@Asterisk";

    public Rule NullableSuffix = "@Question";

    public Rule PrimitiveType = new Rule("type:@KwVoid | type:@KwDynamic | type:@KwObject | type:@KwString | type:@KwBool | type:@KwChar | type:@KwDecimal | type:IntegralType | type:FloatingPointType")
        .Returns("type");

    public Rule NamedType = new Rule("name:QualifiedName typeArgs:TypeArgumentList?")
        .Returns("name", "typeArgs");

    public Rule ArrayRankSpecifier = new Rule("@OpenBracket dims:ArrayDimensions? @CloseBracket")
        .Returns("dims");

    public Rule ArrayDimensions = "dims:@Comma+";

    // C# 7 Tuples
    public Rule TupleType = new Rule("@OpenParen elements:TupleElements @CloseParen")
        .Returns("elements");

    public Rule TupleElements = new Rule("first:TupleElement @Comma second:TupleElement rest:(@Comma TupleElement)*")
        .Returns("first", "second", "rest");

    public Rule TupleElement = new Rule("type:Type name:@Identifier?")
        .Returns("type", "name");

    // C# 9 Function Pointers
    public Rule FunctionPointerType = new Rule("@KwDelegate @Asterisk signature:FunctionPointerSignature")
        .Returns("signature");

    public Rule FunctionPointerSignature = new Rule("@LessThan returnType:Type parameters:FunctionPointerParameters? @GreaterThan")
        .Returns("returnType", "parameters");

    public Rule FunctionPointerParameters = new Rule("@Comma first:Type rest:(@Comma Type)*")
        .Returns("first", "rest");

    public Rule IntegralType = new Rule("type:@KwSbyte | type:@KwByte | type:@KwShort | type:@KwUshort | type:@KwInt | type:@KwUint | type:@KwLong | type:@KwUlong | type:@KwNint | type:@KwNuint")
        .Returns("type");

    public Rule FloatingPointType = "type:@KwFloat | type:@KwDouble";

    public Rule TypeArgumentList = new Rule("@LessThan args:TypeArguments @GreaterThan")
        .Returns("args");

    public Rule TypeArguments = new Rule("first:Type rest:(@Comma Type)*")
        .Returns("first", "rest");

    // ============================================================
    // QUALIFIED NAMES
    // ============================================================

    public Rule QualifiedName = new Rule("global:GlobalPrefix? segments:NameSegments")
        .Returns("global", "segments");

    public Rule GlobalPrefix = "@KwGlobal @DoubleColon";

    public Rule NameSegments = new Rule("first:NameSegment rest:NameSegmentRest*")
        .Returns("first", "rest");

    public Rule NameSegmentRest = new Rule("@Dot segment:NameSegment")
        .Returns("segment");

    public Rule NameSegment = new Rule("name:IdentifierName typeArgs:TypeArgumentList?")
        .Returns("name", "typeArgs");

    public Rule IdentifierName = "name:@Identifier | name:@VerbatimIdentifier";

    // ============================================================
    // STATEMENTS
    // ============================================================

    public Rule Statement = new Rule("stmt:Block | stmt:LabeledStatement | stmt:DeclarationStatement | stmt:EmbeddedStatement")
        .Returns("stmt");

    public Rule EmbeddedStatement = new Rule("stmt:EmptyStatement | stmt:ExpressionStatement | stmt:SelectionStatement | stmt:IterationStatement | stmt:JumpStatement | stmt:TryStatement | stmt:CheckedStatement | stmt:UncheckedStatement | stmt:LockStatement | stmt:UsingStatement | stmt:YieldStatement | stmt:LocalFunctionStatement")
        .Returns("stmt");

    public Rule Block = new Rule("@OpenBrace stmts:Statements? @CloseBrace")
        .Returns("stmts");

    public Rule Statements = new Rule("stmts:Statement+")
        .Returns("stmts");

    public Rule EmptyStatement = "@Semicolon";

    public Rule LabeledStatement = new Rule("label:@Identifier @Colon stmt:Statement")
        .Returns("label", "stmt");

    public Rule DeclarationStatement = new Rule("decl:LocalDeclaration @Semicolon")
        .Returns("decl");

    public Rule LocalDeclaration = "decl:LocalVariableDeclaration | decl:LocalConstantDeclaration";

    public Rule LocalVariableDeclaration = new Rule("modifier:LocalVariableModifier? type:LocalVariableType declarators:LocalVariableDeclarators")
        .Returns("modifier", "type", "declarators");

    public Rule LocalVariableModifier = "mod:@KwRef | mod:@KwScoped | mod:@KwUsing";

    public Rule LocalVariableType = "type:@KwVar | type:RefType | type:Type";

    public Rule RefType = new Rule("@KwRef type:Type")
        .Returns("type");

    public Rule LocalVariableDeclarators = new Rule("first:LocalVariableDeclarator rest:(@Comma LocalVariableDeclarator)*")
        .Returns("first", "rest");

    public Rule LocalVariableDeclarator = new Rule("name:@Identifier init:(@Assign LocalVariableInitializer)?")
        .Returns("name", "init");

    public Rule LocalVariableInitializer = "init:Expression | init:ArrayInitializer | init:StackallocInitializer";

    public Rule LocalConstantDeclaration = new Rule("@KwConst type:Type declarators:ConstantDeclarators")
        .Returns("type", "declarators");

    public Rule ConstantDeclarators = new Rule("first:ConstantDeclarator rest:(@Comma ConstantDeclarator)*")
        .Returns("first", "rest");

    public Rule ConstantDeclarator = new Rule("name:@Identifier @Assign init:Expression")
        .Returns("name", "init");

    public Rule ExpressionStatement = new Rule("expr:Expression @Semicolon")
        .Returns("expr");

    // SELECTION STATEMENTS
    public Rule SelectionStatement = new Rule("stmt:IfStatement | stmt:SwitchStatement")
        .Returns("stmt");

    public Rule IfStatement = new Rule("@KwIf @OpenParen condition:Expression @CloseParen thenStmt:Statement elseClause:ElseClause?")
        .Returns("condition", "thenStmt", "elseClause");

    public Rule ElseClause = new Rule("@KwElse stmt:Statement")
        .Returns("stmt");

    public Rule SwitchStatement = new Rule("@KwSwitch @OpenParen expr:Expression @CloseParen @OpenBrace sections:SwitchSections? @CloseBrace")
        .Returns("expr", "sections");

    public Rule SwitchSections = "sections:SwitchSection+";

    public Rule SwitchSection = new Rule("labels:SwitchLabels stmts:Statements")
        .Returns("labels", "stmts");

    public Rule SwitchLabels = "labels:SwitchLabel+";

    public Rule SwitchLabel = "label:CaseLabel | label:DefaultLabel";

    public Rule CaseLabel = new Rule("@KwCase pattern:Pattern guard:WhenClause? @Colon")
        .Returns("pattern", "guard");

    public Rule DefaultLabel = "@KwDefault @Colon";

    // ITERATION STATEMENTS
    public Rule IterationStatement = new Rule("stmt:WhileStatement | stmt:DoStatement | stmt:ForStatement | stmt:ForEachStatement")
        .Returns("stmt");

    public Rule WhileStatement = new Rule("@KwWhile @OpenParen condition:Expression @CloseParen body:Statement")
        .Returns("condition", "body");

    public Rule DoStatement = new Rule("@KwDo body:Statement @KwWhile @OpenParen condition:Expression @CloseParen @Semicolon")
        .Returns("body", "condition");

    public Rule ForStatement = new Rule("@KwFor @OpenParen init:ForInitializer? @Semicolon condition:Expression? @Semicolon iterator:ForIterator? @CloseParen body:Statement")
        .Returns("init", "condition", "iterator", "body");

    public Rule ForInitializer = "init:LocalVariableDeclaration | init:ExpressionList";

    public Rule ForIterator = "iterator:ExpressionList";

    public Rule ExpressionList = new Rule("first:Expression rest:(@Comma Expression)*")
        .Returns("first", "rest");

    public Rule ForEachStatement = new Rule("@KwForEach @OpenParen modifier:ForEachModifier? type:LocalVariableType name:@Identifier @KwIn collection:Expression @CloseParen body:Statement")
        .Returns("modifier", "type", "name", "collection", "body");

    public Rule ForEachModifier = "mod:@KwRef | mod:@KwScoped";

    // JUMP STATEMENTS
    public Rule JumpStatement = new Rule("stmt:BreakStatement | stmt:ContinueStatement | stmt:GotoStatement | stmt:ReturnStatement | stmt:ThrowStatement")
        .Returns("stmt");

    public Rule BreakStatement = "@KwBreak @Semicolon";

    public Rule ContinueStatement = "@KwContinue @Semicolon";

    public Rule GotoStatement = new Rule("@KwGoto target:GotoTarget @Semicolon")
        .Returns("target");

    public Rule GotoTarget = "target:@Identifier | target:GotoCaseTarget | target:GotoDefaultTarget";

    public Rule GotoCaseTarget = new Rule("@KwCase expr:Expression")
        .Returns("expr");

    public Rule GotoDefaultTarget = "@KwDefault";

    // Return statement - uses Expression which goes through dispatcher chain
    // KNOWN ISSUE: Expression dispatcher chain doesn't properly populate expr field
    // causing Fallback map to be used instead of proper lowering
    public Rule ReturnStatement = new Rule("@KwReturn expr:Expression? @Semicolon")
        .Returns("expr");

    public Rule ThrowStatement = new Rule("@KwThrow expr:Expression? @Semicolon")
        .Returns("expr");

    // TRY STATEMENT
    public Rule TryStatement = new Rule("@KwTry body:Block handlers:CatchClauses? finallyClause:FinallyClause?")
        .Returns("body", "handlers", "finallyClause");

    public Rule CatchClauses = "clauses:CatchClause+";

    public Rule CatchClause = new Rule("@KwCatch filter:CatchFilter? body:Block")
        .Returns("filter", "body");

    public Rule CatchFilter = new Rule("@OpenParen type:Type name:@Identifier? @CloseParen when:WhenClause?")
        .Returns("type", "name", "when");

    public Rule WhenClause = new Rule("@KwWhen @OpenParen condition:Expression @CloseParen")
        .Returns("condition");

    public Rule FinallyClause = new Rule("@KwFinally body:Block")
        .Returns("body");

    // CHECKED/UNCHECKED STATEMENTS
    public Rule CheckedStatement = new Rule("@KwChecked body:Block")
        .Returns("body");

    public Rule UncheckedStatement = new Rule("@KwUnchecked body:Block")
        .Returns("body");

    // LOCK STATEMENT
    public Rule LockStatement = new Rule("@KwLock @OpenParen expr:Expression @CloseParen body:Statement")
        .Returns("expr", "body");

    // USING STATEMENT
    public Rule UsingStatement = new Rule("@KwUsing @OpenParen resource:ResourceAcquisition @CloseParen body:Statement")
        .Returns("resource", "body");

    public Rule ResourceAcquisition = "resource:LocalVariableDeclaration | resource:Expression";

    // YIELD STATEMENT
    public Rule YieldStatement = new Rule("@KwYield kind:YieldKind @Semicolon")
        .Returns("kind");

    public Rule YieldKind = "kind:YieldReturn | kind:YieldBreak";

    public Rule YieldReturn = new Rule("@KwReturn expr:Expression")
        .Returns("expr");

    public Rule YieldBreak = "@KwBreak";

    // LOCAL FUNCTION STATEMENT (C# 7)
    public Rule LocalFunctionStatement = new Rule("attrs:AttributeSections? mods:LocalFunctionModifiers? returnType:Type name:@Identifier typeParams:TypeParameterList? @OpenParen parameters:FormalParameterList? @CloseParen constraints:TypeParameterConstraintsClauses? body:MethodBody")
        .Returns("attrs", "mods", "returnType", "name", "typeParams", "parameters", "constraints", "body");

    public Rule LocalFunctionModifiers = "mods:LocalFunctionModifier+";

    public Rule LocalFunctionModifier = "mod:@KwAsync | mod:@KwUnsafe | mod:@KwStatic | mod:@KwExtern";

    // ============================================================
    // EXPRESSIONS - Fixed left recursion
    // ============================================================

    // Expression dispatcher - uses string syntax with labeled alternatives
    // "expr:NonAssignmentExpression | expr:AssignmentExpression" creates an Expression AST node
    // with an 'expr' field containing whichever alternative matched.
    // The Map uses {expr} to pass through to the child node.
    // NOTE: String syntax with labels automatically populates fields; .Returns() not needed here.
    public Rule Expression = "expr:NonAssignmentExpression | expr:AssignmentExpression";

    // ASSIGNMENT EXPRESSIONS
    public Rule AssignmentExpression = new Rule("left:UnaryExpressionBase op:AssignmentOperator right:Expression")
        .Returns("left", "op", "right");

    public Rule AssignmentOperator = "op:@NullCoalesceAssign | op:@UnsignedRightShiftAssign | op:@RightShiftAssign | op:@LeftShiftAssign | op:@PlusAssign | op:@MinusAssign | op:@MultiplyAssign | op:@DivideAssign | op:@ModuloAssign | op:@BitwiseAndAssign | op:@BitwiseOrAssign | op:@BitwiseXorAssign | op:@Assign";

    public Rule NonAssignmentExpression = "expr:ConditionalExpression";

    // CONDITIONAL EXPRESSION (Ternary)
    public Rule ConditionalExpression = "condition:NullCoalescingExpression @Question trueExpr:Expression @Colon falseExpr:Expression | expr:NullCoalescingExpression";

    // NULL-COALESCING EXPRESSION
    public Rule NullCoalescingExpression = "left:LogicalOrExpression @NullCoalesce right:NullCoalescingExpression | expr:LogicalOrExpression";

    // LOGICAL OR EXPRESSION
    public Rule LogicalOrExpression = "left:LogicalAndExpression @LogicalOr right:LogicalOrExpression | expr:LogicalAndExpression";

    // LOGICAL AND EXPRESSION
    public Rule LogicalAndExpression = "left:BitwiseOrExpression @LogicalAnd right:LogicalAndExpression | expr:BitwiseOrExpression";

    // BITWISE OR EXPRESSION
    public Rule BitwiseOrExpression = "left:BitwiseXorExpression @BitwiseOr right:BitwiseOrExpression | expr:BitwiseXorExpression";

    // BITWISE XOR EXPRESSION
    public Rule BitwiseXorExpression = "left:BitwiseAndExpression @BitwiseXor right:BitwiseXorExpression | expr:BitwiseAndExpression";

    // BITWISE AND EXPRESSION
    public Rule BitwiseAndExpression = "left:EqualityExpression @BitwiseAnd right:BitwiseAndExpression | expr:EqualityExpression";

    // EQUALITY EXPRESSION
    public Rule EqualityExpression = "left:RelationalExpression op:EqualityOperator right:EqualityExpression | expr:RelationalExpression";

    public Rule EqualityOperator = "op:@Equality | op:@Inequality";

    // RELATIONAL EXPRESSION
    public Rule RelationalExpression = "left:ShiftExpression op:RelationalOperator right:RelationalExpression | expr:ShiftExpression";

    public Rule RelationalOperator = "op:@LessThanOrEqual | op:@GreaterThanOrEqual | op:@LessThan | op:@GreaterThan";

    // SHIFT EXPRESSION
    public Rule ShiftExpression = "left:AdditiveExpression op:ShiftOperator right:ShiftExpression | expr:AdditiveExpression";

    public Rule ShiftOperator = "op:@UnsignedRightShift | op:@LeftShift | op:@RightShift";

    // ADDITIVE EXPRESSION
    public Rule AdditiveExpression = "left:MultiplicativeExpression op:AdditiveOperator right:AdditiveExpression | expr:MultiplicativeExpression";

    public Rule AdditiveOperator = "op:@Plus | op:@Minus";

    // MULTIPLICATIVE EXPRESSION
    public Rule MultiplicativeExpression = "left:SwitchExpression op:MultiplicativeOperator right:MultiplicativeExpression | expr:SwitchExpression";

    public Rule MultiplicativeOperator = "op:@Multiply | op:@Divide | op:@Modulo";

    // SWITCH EXPRESSION (C# 8)
    public Rule SwitchExpression = "input:RangeExpression @KwSwitch @OpenBrace arms:SwitchExpressionArms @CloseBrace | expr:RangeExpression";

    public Rule SwitchExpressionArms = new Rule("first:SwitchExpressionArm rest:SwitchExpressionArmRest* @Comma?")
        .Returns("first", "rest");

    public Rule SwitchExpressionArmRest = new Rule("@Comma arm:SwitchExpressionArm")
        .Returns("arm");

    public Rule SwitchExpressionArm = new Rule("pattern:Pattern guard:WhenClause? @LambdaArrow expr:Expression")
        .Returns("pattern", "guard", "expr");

    // RANGE EXPRESSION (C# 8)
    public Rule RangeExpression = "start:UnaryExpression @RangeOperator end:UnaryExpression | @RangeOperator end:UnaryExpression | start:UnaryExpression @RangeOperator | expr:UnaryExpression";

    // UNARY EXPRESSION - Fixed to use suffix pattern
    public Rule UnaryExpression = new Rule("expr:UnaryExpressionBase suffixes:UnaryExpressionSuffixes?")
        .Returns("expr", "suffixes");

    public Rule UnaryExpressionBase = "expr:UnaryOperatorExpression | expr:CastExpression | expr:AwaitExpression | expr:DefaultExpression | expr:NameofExpression | expr:SizeofExpression | expr:CheckedExpression | expr:UncheckedExpression | expr:PrimaryExpressionCore";

    public Rule UnaryExpressionSuffixes = "suffixes:UnaryExpressionSuffix+";

    public Rule UnaryExpressionSuffix = "suffix:MemberAccessSuffix | suffix:InvocationSuffix | suffix:ElementAccessSuffix | suffix:PostIncrementSuffix | suffix:PostDecrementSuffix | suffix:WithExpressionSuffix | suffix:IsPatternSuffix | suffix:AsTypeSuffix";

    public Rule MemberAccessSuffix = new Rule("accessor:MemberAccessor member:@Identifier typeArgs:TypeArgumentList?")
        .Returns("accessor", "member", "typeArgs");

    public Rule MemberAccessor = "op:@Dot | op:@NullConditional";

    public Rule InvocationSuffix = new Rule("@OpenParen args:ArgumentList? @CloseParen")
        .Returns("args");

    public Rule ElementAccessSuffix = new Rule("@OpenBracket indices:ExpressionList @CloseBracket")
        .Returns("indices");

    public Rule PostIncrementSuffix = "@Increment";

    public Rule PostDecrementSuffix = "@Decrement";

    public Rule WithExpressionSuffix = new Rule("@KwWith @OpenBrace initializers:MemberInitializerList? @CloseBrace")
        .Returns("initializers");

    public Rule IsPatternSuffix = new Rule("@KwIs pattern:Pattern")
        .Returns("pattern");

    public Rule AsTypeSuffix = new Rule("@KwAs type:Type")
        .Returns("type");

    public Rule UnaryOperatorExpression = new Rule("op:UnaryOperator operand:UnaryExpression")
        .Returns("op", "operand");

    public Rule UnaryOperator = "op:@Increment | op:@Decrement | op:@Plus | op:@Minus | op:@LogicalNot | op:@BitwiseNot | op:@Asterisk | op:@Ampersand | op:@KwRef";

    public Rule CastExpression = new Rule("@OpenParen type:Type @CloseParen expr:UnaryExpression")
        .Returns("type", "expr");

    public Rule AwaitExpression = new Rule("@KwAwait expr:UnaryExpression")
        .Returns("expr");

    public Rule DefaultExpression = "@KwDefault @OpenParen type:Type @CloseParen | @KwDefault";

    public Rule NameofExpression = new Rule("@KwNameof @OpenParen expr:Expression @CloseParen")
        .Returns("expr");

    public Rule SizeofExpression = new Rule("@KwSizeof @OpenParen type:Type @CloseParen")
        .Returns("type");

    public Rule CheckedExpression = new Rule("@KwChecked @OpenParen expr:Expression @CloseParen")
        .Returns("expr");

    public Rule UncheckedExpression = new Rule("@KwUnchecked @OpenParen expr:Expression @CloseParen")
        .Returns("expr");

    // PRIMARY EXPRESSION - Core items without suffixes
    public Rule PrimaryExpressionCore = "expr:Literal | expr:SimpleName | expr:ParenthesizedExpression | expr:ThisAccessExpression | expr:BaseAccessExpression | expr:ObjectCreationExpression | expr:DelegateCreationExpression | expr:AnonymousObjectCreationExpression | expr:ArrayCreationExpression | expr:ImplicitArrayCreationExpression | expr:TypeofExpression | expr:LambdaExpression | expr:QueryExpression | expr:StackallocExpression | expr:TupleExpression | expr:CollectionExpression";

    // Literal dispatcher - creates specific literal type nodes
    public Rule Literal = "TrueLiteral | FalseLiteral | NullLiteral | DecimalIntegerLiteral | HexIntegerLiteral | BinaryIntegerLiteral | FloatLiteral | FloatLiteralNoDecimal | FloatLiteralSuffix | CharacterLiteral | StringLiteral | VerbatimStringLiteral | InterpolatedStringStart | RawStringLiteral | Utf8StringLiteral";

    // Literal type rules - Each creates its own AST node type
    public Rule TrueLiteral = new Rule("@KwTrue");
    public Rule FalseLiteral = new Rule("@KwFalse");
    public Rule NullLiteral = new Rule("@KwNull");
    public Rule DecimalIntegerLiteral = new Rule("@DecimalIntegerLiteral");
    public Rule HexIntegerLiteral = new Rule("@HexIntegerLiteral");
    public Rule BinaryIntegerLiteral = new Rule("@BinaryIntegerLiteral");
    public Rule FloatLiteral = new Rule("@FloatLiteral");
    public Rule FloatLiteralNoDecimal = new Rule("@FloatLiteralNoDecimal");
    public Rule FloatLiteralSuffix = new Rule("@FloatLiteralSuffix");
    public Rule CharacterLiteral = new Rule("@CharacterLiteral");
    public Rule StringLiteral = new Rule("@StringLiteral");
    public Rule VerbatimStringLiteral = new Rule("@VerbatimStringLiteral");
    public Rule InterpolatedStringStart = new Rule("@InterpolatedStringStart");
    public Rule RawStringLiteral = new Rule("@RawStringLiteral");
    public Rule Utf8StringLiteral = new Rule("@Utf8StringLiteral");

    public Rule SimpleName = new Rule("name:@Identifier typeArgs:TypeArgumentList?")
        .Returns("name", "typeArgs");

    public Rule ParenthesizedExpression = new Rule("@OpenParen expr:Expression @CloseParen")
        .Returns("expr");

    public Rule ArgumentList = new Rule("first:Argument rest:ArgumentRest*")
        .Returns("first", "rest");

    public Rule ArgumentRest = new Rule("@Comma arg:Argument")
        .Returns("arg");

    public Rule Argument = "modifier:ArgumentModifier? value:Expression | name:@Identifier @Colon value:Expression";

    public Rule ArgumentModifier = "mod:@KwRef | mod:@KwOut | mod:@KwIn";

    public Rule ThisAccessExpression = "@KwThis";

    public Rule BaseAccessExpression = "@KwBase @Dot member:@Identifier | @KwBase @OpenBracket indices:ExpressionList @CloseBracket";

    public Rule ObjectCreationExpression = new Rule("@KwNew type:Type @OpenParen args:ArgumentList? @CloseParen initializer:ObjectInitializer?")
        .Returns("type", "args", "initializer");

    public Rule ObjectInitializer = new Rule("@OpenBrace initializers:MemberInitializerList? @Comma? @CloseBrace")
        .Returns("initializers");

    public Rule MemberInitializerList = new Rule("first:MemberInitializer rest:MemberInitializerRest*")
        .Returns("first", "rest");

    public Rule MemberInitializerRest = new Rule("@Comma init:MemberInitializer")
        .Returns("init");

    public Rule MemberInitializer = "name:@Identifier @Assign value:Expression | name:@Identifier @Assign init:ObjectInitializer";

    public Rule DelegateCreationExpression = new Rule("@KwNew type:Type @OpenParen expr:Expression @CloseParen")
        .Returns("type", "expr");

    public Rule AnonymousObjectCreationExpression = new Rule("@KwNew initializer:ObjectInitializer")
        .Returns("initializer");

    public Rule ArrayCreationExpression = new Rule("@KwNew type:Type @OpenBracket dims:ExpressionList @CloseBracket ranks:ArrayRankSpecifiers? initializer:ArrayInitializer?")
        .Returns("type", "dims", "ranks", "initializer");

    public Rule ArrayRankSpecifiers = "ranks:ArrayRankSpecifier+";

    public Rule ImplicitArrayCreationExpression = new Rule("@KwNew @OpenBracket commas:ArrayDimensions? @CloseBracket initializer:ArrayInitializer")
        .Returns("commas", "initializer");

    public Rule ArrayInitializer = new Rule("@OpenBrace inits:VariableInitializerList? @Comma? @CloseBrace")
        .Returns("inits");

    public Rule VariableInitializerList = new Rule("first:VariableInitializer rest:VariableInitializerRest*")
        .Returns("first", "rest");

    public Rule VariableInitializerRest = new Rule("@Comma init:VariableInitializer")
        .Returns("init");

    public Rule TypeofExpression = new Rule("@KwTypeof @OpenParen type:Type @CloseParen")
        .Returns("type");

    // LAMBDA EXPRESSIONS
    public Rule LambdaExpression = "lambda:AnonymousMethodExpression | lambda:SimpleLambdaExpression | lambda:ParenthesizedLambdaExpression";

    public Rule AnonymousMethodExpression = new Rule("async:@KwAsync? @KwDelegate @OpenParen parameters:FormalParameterList @CloseParen body:Block")
        .Returns("async", "parameters", "body");

    public Rule SimpleLambdaExpression = new Rule("async:@KwAsync? parameter:@Identifier @LambdaArrow body:LambdaBody")
        .Returns("async", "parameter", "body");

    public Rule ParenthesizedLambdaExpression = new Rule("async:@KwAsync? @OpenParen parameters:FormalParameterList @CloseParen @LambdaArrow body:LambdaBody")
        .Returns("async", "parameters", "body");

    public Rule LambdaBody = "body:Expression | body:Block";

    // QUERY EXPRESSIONS (LINQ)
    public Rule QueryExpression = new Rule("from:FromClause body:QueryBody")
        .Returns("from", "body");

    public Rule FromClause = new Rule("@KwFrom type:Type? name:@Identifier @KwIn source:Expression")
        .Returns("type", "name", "source");

    public Rule QueryBody = new Rule("clauses:QueryBodyClauses? select:SelectOrGroupClause continuation:QueryContinuation?")
        .Returns("clauses", "select", "continuation");

    public Rule QueryBodyClauses = "clauses:QueryBodyClause+";

    public Rule QueryBodyClause = "clause:FromClause | clause:LetClause | clause:WhereClause | clause:JoinClause | clause:OrderbyClause";

    public Rule LetClause = new Rule("@KwLet name:@Identifier @Assign expr:Expression")
        .Returns("name", "expr");

    public Rule WhereClause = new Rule("@KwWhere condition:Expression")
        .Returns("condition");

    public Rule JoinClause = new Rule("@KwJoin type:Type? name:@Identifier @KwIn source:Expression @KwOn left:Expression @KwEquals right:Expression into:JoinIntoClause?")
        .Returns("type", "name", "source", "left", "right", "into");

    public Rule JoinIntoClause = new Rule("@KwInto name:@Identifier")
        .Returns("name");

    public Rule OrderbyClause = new Rule("@KwOrderby orderings:Orderings")
        .Returns("orderings");

    public Rule Orderings = new Rule("first:Ordering rest:OrderingRest*")
        .Returns("first", "rest");

    public Rule OrderingRest = new Rule("@Comma ordering:Ordering")
        .Returns("ordering");

    public Rule Ordering = new Rule("expr:Expression direction:OrderingDirection?")
        .Returns("expr", "direction");

    public Rule OrderingDirection = "dir:@KwAscending | dir:@KwDescending";

    public Rule SelectOrGroupClause = "clause:SelectClause | clause:GroupClause";

    public Rule SelectClause = new Rule("@KwSelect expr:Expression")
        .Returns("expr");

    public Rule GroupClause = new Rule("@KwGroup element:Expression @KwBy key:Expression")
        .Returns("element", "key");

    public Rule QueryContinuation = new Rule("@KwInto name:@Identifier body:QueryBody")
        .Returns("name", "body");

    // STACKALLOC EXPRESSION
    public Rule StackallocExpression = new Rule("@KwStackalloc type:Type @OpenBracket size:Expression @CloseBracket init:StackallocInitializer?")
        .Returns("type", "size", "init");

    public Rule StackallocInitializer = new Rule("@OpenBrace inits:VariableInitializerList? @Comma? @CloseBrace")
        .Returns("inits");

    // TUPLE EXPRESSION (C# 7)
    public Rule TupleExpression = new Rule("@OpenParen elements:TupleExpressionElements @CloseParen")
        .Returns("elements");

    public Rule TupleExpressionElements = new Rule("first:TupleExpressionElement @Comma second:TupleExpressionElement rest:TupleExpressionElementRest*")
        .Returns("first", "second", "rest");

    public Rule TupleExpressionElementRest = new Rule("@Comma element:TupleExpressionElement")
        .Returns("element");

    public Rule TupleExpressionElement = "name:@Identifier @Colon expr:Expression | expr:Expression";

    // COLLECTION EXPRESSION (C# 12)
    public Rule CollectionExpression = new Rule("@OpenBracket elements:CollectionElementList? @CloseBracket")
        .Returns("elements");

    public Rule CollectionElementList = new Rule("first:CollectionElement rest:CollectionElementRest* @Comma?")
        .Returns("first", "rest");

    public Rule CollectionElementRest = new Rule("@Comma element:CollectionElement")
        .Returns("element");

    public Rule CollectionElement = "@RangeOperator expr:Expression | expr:Expression";

    // ============================================================
    // PATTERNS (C# 7-13) - Fixed left recursion
    // ============================================================

    public Rule Pattern = new Rule("pattern:LogicalAndPattern rest:PatternOrSuffix*")
        .Returns("pattern", "rest");

    public Rule PatternOrSuffix = new Rule("@KwOr pattern:LogicalAndPattern")
        .Returns("pattern");

    public Rule LogicalAndPattern = new Rule("pattern:NotPattern rest:PatternAndSuffix*")
        .Returns("pattern", "rest");

    public Rule PatternAndSuffix = new Rule("@KwAnd pattern:NotPattern")
        .Returns("pattern");

    public Rule NotPattern = "@KwNot pattern:PrimaryPattern | pattern:PrimaryPattern";

    public Rule PrimaryPattern = "pattern:DeclarationPattern | pattern:ConstantPattern | pattern:VarPattern | pattern:RecursivePattern | pattern:PropertyPattern | pattern:PositionalPattern | pattern:DiscardPattern | pattern:TypePattern | pattern:RelationalPattern | pattern:ListPattern | pattern:ParenthesizedPattern";

    public Rule ParenthesizedPattern = new Rule("@OpenParen pattern:Pattern @CloseParen")
        .Returns("pattern");

    public Rule DeclarationPattern = new Rule("type:Type designation:Designation")
        .Returns("type", "designation");

    public Rule Designation = "designation:@Identifier | designation:DiscardDesignation | designation:ParenthesizedDesignation";

    public Rule DiscardDesignation = "@KwUnderscore";

    public Rule ParenthesizedDesignation = new Rule("@OpenParen designations:DesignationList @CloseParen")
        .Returns("designations");

    public Rule DesignationList = new Rule("first:Designation rest:DesignationRest*")
        .Returns("first", "rest");

    public Rule DesignationRest = new Rule("@Comma designation:Designation")
        .Returns("designation");

    public Rule ConstantPattern = "pattern:Literal";

    public Rule VarPattern = new Rule("@KwVar designation:Designation")
        .Returns("designation");

    public Rule RecursivePattern = new Rule("type:Type? positional:PositionalPattern? property:PropertyPattern? designation:Designation?")
        .Returns("type", "positional", "property", "designation");

    public Rule PropertyPattern = new Rule("@OpenBrace subpatterns:SubpatternList? @CloseBrace")
        .Returns("subpatterns");

    public Rule SubpatternList = new Rule("first:Subpattern rest:SubpatternRest* @Comma?")
        .Returns("first", "rest");

    public Rule SubpatternRest = new Rule("@Comma subpattern:Subpattern")
        .Returns("subpattern");

    public Rule Subpattern = new Rule("name:@Identifier @Colon pattern:Pattern")
        .Returns("name", "pattern");

    public Rule PositionalPattern = new Rule("@OpenParen patterns:SubpatternList? @CloseParen")
        .Returns("patterns");

    public Rule DiscardPattern = "@KwUnderscore";

    public Rule TypePattern = new Rule("type:Type")
        .Returns("type");

    // C# 9 Relational Patterns
    public Rule RelationalPattern = new Rule("op:RelationalOperator expr:Literal")
        .Returns("op", "expr");

    // C# 11 List Patterns
    public Rule ListPattern = new Rule("@OpenBracket patterns:ListPatternElements? @CloseBracket designation:Designation?")
        .Returns("patterns", "designation");

    public Rule ListPatternElements = new Rule("first:Pattern rest:ListPatternElementRest* slice:ListPatternSlice?")
        .Returns("first", "rest", "slice");

    public Rule ListPatternElementRest = new Rule("@Comma pattern:Pattern")
        .Returns("pattern");

    public Rule ListPatternSlice = new Rule("@Comma slice:SlicePattern")
        .Returns("slice");

    public Rule SlicePattern = new Rule("@RangeOperator pattern:Pattern?")
        .Returns("pattern");
}