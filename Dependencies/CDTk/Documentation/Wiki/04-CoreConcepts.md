# Core Concepts

This guide explains the fundamental architecture of CDTk and how all the pieces fit together.

## The Four-Stage Pipeline

Every CDTk compiler operates through four distinct stages:

```
Source Code → [Lexing] → Tokens → [Parsing] → AST → [Semantic] → Enhanced AST → [Mapping] → Output
```

### 1. **Lexical Analysis** (TokenSet)
Breaks source code into meaningful tokens.

**Input**: `"2 + 3"`  
**Output**: `[Number("2"), Plus("+"), Number("3")]`

### 2. **Syntax Analysis** (RuleSet)  
Builds an Abstract Syntax Tree (AST) from tokens.

**Input**: `[Number("2"), Plus("+"), Number("3")]`  
**Output**: `Expr(left: Number("2"), right: Number("3"))`

### 3. **Semantic Analysis** (Models)
Validates and enhances the AST.

**Input**: `Expr(left: Number("2"), right: Number("3"))`  
**Output**: Validated/transformed AST

### 4. **Code Generation** (MapSet)
Transforms AST into target output.

**Input**: `Expr(left: Number("2"), right: Number("3"))`  
**Output**: `"2 + 3"` (or any target language)

## Token Identity: Field-Based

CDTk uses **field identity** for tokens, not string names:

```csharp
class Tokens : TokenSet
{
    public Token Number = @"\d+";  // Field name "Number" is the identity
    public Token Plus = @"\+";      // Field name "Plus" is the identity
}
```

The **field reference itself** is the identity. This enables:
- Compile-time safety
- Refactoring support (rename field = rename all references)
- No string-based lookups

## Rule Identity: Field-Based

Same principle applies to rules:

```csharp
class Rules : RuleSet
{
    public Rule Expr = new Rule("@Number '+' @Number");  // Field "Expr" is identity
}
```

## Token Ordering & Priority

**Order matters!** Earlier fields have higher priority:

```csharp
class Tokens : TokenSet
{
    public Token If = "if";              // Priority 1 - matches first
    public Token While = "while";        // Priority 2
    public Token Identifier = @"\w+";    // Priority 3 - matches last
}
```

Input `"if"` will match `If`, not `Identifier`.

## Pattern Syntax

### In Rules

- `@TokenName` - Reference a token
- `RuleName` - Reference another rule  
- `'literal'` - Match exact text
- `|` - Alternation (or)
- `*` - Zero or more
- `+` - One or more
- `?` - Optional
- `(...)` - Grouping

### Labeled Patterns

```csharp
public Rule Expr = new Rule("left:@Number '+' right:@Number")
    .Returns("left", "right");
```

Labels allow you to:
1. Name matched parts
2. Extract them via `.Returns()`
3. Reference them in MapSet templates

## The Returns Mechanism

`.Returns()` specifies which labeled parts become AST fields:

```csharp
public Rule Expr = new Rule("left:@Number op:@Plus right:@Number")
    .Returns("left", "right");  // Only keep left and right, drop op
```

AST will have:
- `Expr.left` → Number node
- `Expr.right` → Number node
- No `op` field (we know it's Plus from the node type)

## Diagnostics: Unified Error Reporting

All stages produce diagnostics with:

```csharp
public class Diagnostic
{
    public Stage Stage { get; }             // Which stage?
    public DiagnosticLevel Level { get; }   // Error/Warning/Info?
    public string Message { get; }          // What happened?
    public SourceSpan Span { get; }         // Where in the source?
}
```

Always check for errors:

```csharp
var result = compiler.Compile(source);
if (result.Diagnostics.HasErrors)
{
    // Handle errors
}
```

## Memory Safety Guarantee

CDTk never uses unsafe code. Everything is:
- Safe managed C#
- No pointers
- No stackalloc
- No custom allocators

Performance comes from smart algorithms, not unsafe optimizations.

## Grammar Generality

CDTk supports **ANY context-free grammar** because:

1. **ALL(*) handles deterministic cases** - Fast predictive parsing
2. **GLL handles everything else** - Generalized parsing for complex grammars
3. **AG-LL escalates only when needed** - Best of both worlds
4. **No grammar restrictions** - Left recursion, ambiguity, all supported

See [AG-LL Parser Explained](12-AGLLParser.md) for details.

## Performance Targets

From the CDTk specification:

- **Lexing**: 100-200M chars/sec
- **Parsing**: 5-10M AST nodes/sec (deterministic grammars)
- **Semantic**: 15M+ operations/sec

Achieved through:
- DFA-based lexing
- Adaptive parsing strategies
- Object pooling
- String interning
- Smart caching

## Next Steps

- **[TokenSet Guide](05-TokenSet.md)** - Master lexical analysis
- **[RuleSet Guide](06-RuleSet.md)** - Master grammar definition
- **[AST Construction](07-AST.md)** - Understand AST building
- **[Models](08-Models.md)** - Add semantic analysis
- **[MapSet](09-MapSet.md)** - Generate code
