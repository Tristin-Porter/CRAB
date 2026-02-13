using CDTk;

/// <summary>
/// Complete C# 13 token set for CRAB compiler.
/// Tokens are defined in priority order - earlier tokens match first.
/// Field declaration order is CRITICAL for correct tokenization.
/// </summary>
public class Tokens : TokenSet
{
    // ============================================================
    // WHITESPACE & COMMENTS (Ignored Tokens)
    // ============================================================
    // Must be first to avoid being captured by other patterns
    
    public Token Whitespace = new Token(@"[ \t\r\n]+").Ignore();
    public Token SingleLineComment = new Token(@"//[^\r\n]*").Ignore();
    public Token MultiLineComment = new Token(@"/\*[\s\S]*?\*/").Ignore();
    
    // ============================================================
    // PREPROCESSOR DIRECTIVES
    // ============================================================
    // Must come before keywords to capture # at line start
    
    public Token PreprocessorDefine = @"^[ \t]*#[ \t]*define\b";
    public Token PreprocessorUndef = @"^[ \t]*#[ \t]*undef\b";
    public Token PreprocessorIf = @"^[ \t]*#[ \t]*if\b";
    public Token PreprocessorElif = @"^[ \t]*#[ \t]*elif\b";
    public Token PreprocessorElse = @"^[ \t]*#[ \t]*else\b";
    public Token PreprocessorEndif = @"^[ \t]*#[ \t]*endif\b";
    public Token PreprocessorRegion = @"^[ \t]*#[ \t]*region\b";
    public Token PreprocessorEndregion = @"^[ \t]*#[ \t]*endregion\b";
    public Token PreprocessorError = @"^[ \t]*#[ \t]*error\b";
    public Token PreprocessorWarning = @"^[ \t]*#[ \t]*warning\b";
    public Token PreprocessorLine = @"^[ \t]*#[ \t]*line\b";
    public Token PreprocessorPragma = @"^[ \t]*#[ \t]*pragma\b";
    public Token PreprocessorNullable = @"^[ \t]*#[ \t]*nullable\b";
    
    // ============================================================
    // KEYWORDS - Reserved Words (Must come before identifiers)
    // ============================================================
    // Ordered alphabetically for maintainability
    
    public Token KwAbstract = @"\babstract\b";
    public Token KwAs = @"\bas\b";
    public Token KwBase = @"\bbase\b";
    public Token KwBool = @"\bbool\b";
    public Token KwBreak = @"\bbreak\b";
    public Token KwByte = @"\bbyte\b";
    public Token KwCase = @"\bcase\b";
    public Token KwCatch = @"\bcatch\b";
    public Token KwChar = @"\bchar\b";
    public Token KwChecked = @"\bchecked\b";
    public Token KwClass = @"\bclass\b";
    public Token KwConst = @"\bconst\b";
    public Token KwContinue = @"\bcontinue\b";
    public Token KwDecimal = @"\bdecimal\b";
    public Token KwDefault = @"\bdefault\b";
    public Token KwDelegate = @"\bdelegate\b";
    public Token KwDo = @"\bdo\b";
    public Token KwDouble = @"\bdouble\b";
    public Token KwElse = @"\belse\b";
    public Token KwEnum = @"\benum\b";
    public Token KwEvent = @"\bevent\b";
    public Token KwExplicit = @"\bexplicit\b";
    public Token KwExtern = @"\bextern\b";
    public Token KwFalse = @"\bfalse\b";
    public Token KwFinally = @"\bfinally\b";
    public Token KwFixed = @"\bfixed\b";
    public Token KwFloat = @"\bfloat\b";
    public Token KwFor = @"\bfor\b";
    public Token KwForEach = @"\bforeach\b";
    public Token KwGoto = @"\bgoto\b";
    public Token KwIf = @"\bif\b";
    public Token KwImplicit = @"\bimplicit\b";
    public Token KwIn = @"\bin\b";
    public Token KwInt = @"\bint\b";
    public Token KwInterface = @"\binterface\b";
    public Token KwInternal = @"\binternal\b";
    public Token KwIs = @"\bis\b";
    public Token KwLock = @"\block\b";
    public Token KwLong = @"\blong\b";
    public Token KwNamespace = @"\bnamespace\b";
    public Token KwNew = @"\bnew\b";
    public Token KwNull = @"\bnull\b";
    public Token KwObject = @"\bobject\b";
    public Token KwOperator = @"\boperator\b";
    public Token KwOut = @"\bout\b";
    public Token KwOverride = @"\boverride\b";
    public Token KwParams = @"\bparams\b";
    public Token KwPrivate = @"\bprivate\b";
    public Token KwProtected = @"\bprotected\b";
    public Token KwPublic = @"\bpublic\b";
    public Token KwReadonly = @"\breadonly\b";
    public Token KwRef = @"\bref\b";
    public Token KwReturn = @"\breturn\b";
    public Token KwSbyte = @"\bsbyte\b";
    public Token KwSealed = @"\bsealed\b";
    public Token KwShort = @"\bshort\b";
    public Token KwSizeof = @"\bsizeof\b";
    public Token KwStackalloc = @"\bstackalloc\b";
    public Token KwStatic = @"\bstatic\b";
    public Token KwString = @"\bstring\b";
    public Token KwStruct = @"\bstruct\b";
    public Token KwSwitch = @"\bswitch\b";
    public Token KwThis = @"\bthis\b";
    public Token KwThrow = @"\bthrow\b";
    public Token KwTrue = @"\btrue\b";
    public Token KwTry = @"\btry\b";
    public Token KwTypeof = @"\btypeof\b";
    public Token KwUint = @"\buint\b";
    public Token KwUlong = @"\bulong\b";
    public Token KwUnchecked = @"\bunchecked\b";
    public Token KwUnsafe = @"\bunsafe\b";
    public Token KwUshort = @"\bushort\b";
    public Token KwUsing = @"\busing\b";
    public Token KwVirtual = @"\bvirtual\b";
    public Token KwVoid = @"\bvoid\b";
    public Token KwVolatile = @"\bvolatile\b";
    public Token KwWhile = @"\bwhile\b";
    
    // ============================================================
    // CONTEXTUAL KEYWORDS
    // ============================================================
    // These are keywords only in specific contexts
    // Must come before identifiers but after reserved keywords
    
    public Token KwAdd = @"\badd\b";
    public Token KwAlias = @"\balias\b";
    public Token KwAllowsConstraint = @"\ballows\b";  // C# 13
    public Token KwAnd = @"\band\b";
    public Token KwArgs = @"\bargs\b";  // C# 9+
    public Token KwAscending = @"\bascending\b";
    public Token KwAssembly = @"\bassembly\b";  // Attribute target
    public Token KwAsync = @"\basync\b";
    public Token KwAwait = @"\bawait\b";
    public Token KwBy = @"\bby\b";
    public Token KwDescending = @"\bdescending\b";
    public Token KwDynamic = @"\bdynamic\b";
    public Token KwEquals = @"\bequals\b";
    public Token KwField = @"\bfield\b";  // Attribute target
    public Token KwFile = @"\bfile\b";  // C# 11
    public Token KwFrom = @"\bfrom\b";
    public Token KwGet = @"\bget\b";
    public Token KwGlobal = @"\bglobal\b";
    public Token KwGroup = @"\bgroup\b";
    public Token KwInit = @"\binit\b";  // C# 9
    public Token KwInto = @"\binto\b";
    public Token KwJoin = @"\bjoin\b";
    public Token KwLet = @"\blet\b";
    public Token KwManaged = @"\bmanaged\b";
    public Token KwManual = @"\bmanual\b";  // CRAB-specific for manual memory blocks
    public Token KwMethod = @"\bmethod\b";  // Attribute target
    public Token KwModule = @"\bmodule\b";  // Attribute target
    public Token KwNameof = @"\bnameof\b";
    public Token KwNint = @"\bnint\b";  // C# 9
    public Token KwNot = @"\bnot\b";
    public Token KwNotnull = @"\bnotnull\b";
    public Token KwNuint = @"\bnuint\b";  // C# 9
    public Token KwOn = @"\bon\b";
    public Token KwOr = @"\bor\b";
    public Token KwOrderby = @"\borderby\b";
    public Token KwParam = @"\bparam\b";  // Attribute target
    public Token KwPartial = @"\bpartial\b";
    public Token KwProperty = @"\bproperty\b";  // Attribute target
    public Token KwRecord = @"\brecord\b";  // C# 9
    public Token KwRemove = @"\bremove\b";
    public Token KwRequired = @"\brequired\b";  // C# 11
    public Token KwScoped = @"\bscoped\b";  // C# 11
    public Token KwSelect = @"\bselect\b";
    public Token KwSet = @"\bset\b";
    public Token KwType = @"\btype\b";  // Attribute target
    public Token KwUnmanaged = @"\bunmanaged\b";
    public Token KwValue = @"\bvalue\b";
    public Token KwVar = @"\bvar\b";
    public Token KwWhen = @"\bwhen\b";
    public Token KwWhere = @"\bwhere\b";
    public Token KwWith = @"\bwith\b";  // C# 9
    public Token KwYield = @"\byield\b";
    
    // Special identifiers
    public Token KwUnderscore = @"_\b";  // Discard pattern
    
    // ============================================================
    // LITERALS - Must come before identifiers
    // ============================================================
    
    // String Literals (Must be before character literals)
    // Note: Raw string literals require full parser validation for indentation rules
    public Token RawStringLiteral = new Token("\"\"\"([^\"]|\"(?!\"\"))*\"\"\"");  // C# 11 raw strings (parser validates indentation)
    public Token InterpolatedVerbatimStringStart = @"\$@""";
    public Token VerbatimInterpolatedStringStart = @"@\$""";
    public Token InterpolatedStringStart = @"\$""";
    public Token VerbatimStringLiteral = @"@""(?:""""|[^""])*""";
    public Token StringLiteral = @"""(?:\\.|[^""\\])*""";
    public Token Utf8StringLiteral = @"""(?:\\.|[^""\\])*""u8";  // C# 11
    
    // Character Literal
    public Token CharacterLiteral = @"'(?:\\.|[^'\\])'";
    
    // Numeric Literals (Order matters for proper matching)
    // Binary literals (C# 7.0)
    public Token BinaryIntegerLiteral = @"\b0[bB][01][01_]*[uUlL]*\b";
    // Hexadecimal literals
    public Token HexIntegerLiteral = @"\b0[xX][0-9a-fA-F][0-9a-fA-F_]*[uUlL]*\b";
    // Floating-point with exponent
    public Token FloatLiteral = @"\b\d[\d_]*\.[\d_]+([eE][+-]?[\d_]+)?[fFdDmM]?\b";
    public Token FloatLiteralNoDecimal = @"\b\d[\d_]*[eE][+-]?[\d_]+[fFdDmM]?\b";
    public Token FloatLiteralSuffix = @"\b\d[\d_]*[fFdDmM]\b";
    // Decimal integers
    public Token DecimalIntegerLiteral = @"\b\d[\d_]*[uUlL]*\b";
    
    // Null Literal - handled as keyword
    
    // ============================================================
    // OPERATORS - Multi-character operators must come first
    // ============================================================
    
    // Compound Assignment Operators (3 characters)
    public Token NullCoalesceAssign = @"\?\?=";  // C# 8
    public Token UnsignedRightShiftAssign = @">>>=";  // C# 11
    public Token LeftShiftAssign = @"<<=";
    public Token RightShiftAssign = @">>=";
    
    // Logical and Comparison Operators (3 characters)
    public Token UnsignedRightShift = @">>>";  // C# 11
    
    // Lambda and Range Operators (2 characters)
    public Token LambdaArrow = @"=>";
    public Token RangeOperator = @"\.\.";  // C# 8
    public Token NullCoalesce = @"\?\?";
    public Token NullConditional = @"\?\.";  // C# 6
    
    // Equality and Relational (2 characters)
    public Token Equality = @"==";
    public Token Inequality = @"!=";
    public Token LessThanOrEqual = @"<=";
    public Token GreaterThanOrEqual = @">=";
    
    // Logical Operators (2 characters)
    public Token LogicalAnd = @"&&";
    public Token LogicalOr = @"\|\|";
    
    // Increment/Decrement (2 characters)
    public Token Increment = @"\+\+";
    public Token Decrement = @"--";
    
    // Compound Assignment (2 characters)
    public Token PlusAssign = @"\+=";
    public Token MinusAssign = @"-=";
    public Token MultiplyAssign = @"\*=";
    public Token DivideAssign = @"/=";
    public Token ModuloAssign = @"%=";
    public Token BitwiseAndAssign = @"&=";
    public Token BitwiseOrAssign = @"\|=";
    public Token BitwiseXorAssign = @"\^=";
    
    // Arithmetic Operators (1 character)
    public Token Plus = @"\+";
    public Token Minus = @"-";
    public Token Multiply = @"\*";
    public Token Divide = @"/";
    public Token Modulo = @"%";
    
    // Bitwise Operators (1 character)
    public Token BitwiseAnd = @"&";
    public Token BitwiseOr = @"\|";
    public Token BitwiseXor = @"\^";
    public Token BitwiseNot = @"~";
    
    // Shift operators (must come after >= and <=)
    public Token LeftShift = @"<<";
    public Token RightShift = @">>";
    
    // Relational Operators (1 character)
    public Token LessThan = @"<";
    public Token GreaterThan = @">";
    
    // Logical Operators (1 character)
    public Token LogicalNot = @"!";
    
    // Assignment (1 character)
    public Token Assign = @"=";
    
    // Member Access and Indexing
    public Token DoubleColon = @"::";  // Must come before Colon
    public Token Dot = @"\.";
    public Token Comma = @",";
    public Token Semicolon = @";";
    public Token Colon = @":";
    public Token Question = @"\?";
    
    // ============================================================
    // DELIMITERS
    // ============================================================
    
    public Token OpenParen = @"\(";
    public Token CloseParen = @"\)";
    public Token OpenBrace = @"\{";
    public Token CloseBrace = @"\}";
    public Token OpenBracket = @"\[";
    public Token CloseBracket = @"\]";
    
    // Attributes
    public Token OpenBracketColon = @"\[";  // Same as OpenBracket but may need semantic distinction
    
    // Generics (handled by LessThan/GreaterThan contextually)
    
    // Pointer (used in unsafe code)
    public Token Asterisk = @"\*";  // Duplicate of Multiply, contextual
    public Token Ampersand = @"&";  // Duplicate of BitwiseAnd, contextual
    
    // ============================================================
    // IDENTIFIERS - Must come LAST to avoid matching keywords
    // ============================================================
    
    // Verbatim identifiers (start with @)
    public Token VerbatimIdentifier = @"@[a-zA-Z_][a-zA-Z0-9_]*";
    
    // Regular identifiers
    public Token Identifier = @"[a-zA-Z_][a-zA-Z0-9_]*";
    
    // ============================================================
    // SPECIAL TOKENS
    // ============================================================
    
    // Interpolated string components (handled specially)
    public Token InterpolatedStringText = @"[^{}\""]+";  // Text within interpolated strings
    public Token InterpolatedStringEnd = @"""";
    
    // Attributes for string interpolation holes
    public Token OpenBraceInterpolation = @"\{";
    public Token CloseBraceInterpolation = @"\}";
}