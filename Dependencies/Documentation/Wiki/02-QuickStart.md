# Quick Start Tutorial

Learn CDTk by building progressively complex compilers. By the end of this tutorial, you'll have hands-on experience with tokens, grammar rules, AST construction, and code generation.

## Tutorial 1: Number Parser (2 minutes)

The simplest possible compiler - just recognize numbers.

```csharp
using System;
using CDTk;

class T1Tokens : TokenSet
{
    public Token Number = @"\d+";
    public Token WS = new Token(@"\s+").Ignore();
}

class T1Rules : RuleSet
{
    public Rule Root = new Rule("@Number");
}

class T1Maps : MapSet
{
    public Map Root = "Found number: {value}";
}

class Tutorial1
{
    static void Run()
    {
        var compiler = new Compiler()
            .WithTokens(new T1Tokens())
            .WithRules(new T1Rules())
            .WithTarget(new T1Maps())
            .Build();

        var result = compiler.Compile("42");
        Console.WriteLine(result.Output[0]); // "Found number: 42"
    }
}
```

**What you learned:**
- How to define tokens with regex
- How to ignore whitespace
- How to create simple rules
- How to generate output

## Tutorial 2: Calculator (5 minutes)

Add two numbers together.

```csharp
class T2Tokens : TokenSet
{
    public Token Number = @"\d+";
    public Token Plus = @"\+";
    public Token WS = new Token(@"\s+").Ignore();
}

class T2Rules : RuleSet
{
    public Rule Expr = new Rule("left:@Number '+' right:@Number")
        .Returns("left", "right");
}

class T2Maps : MapSet
{
    public Map Expr = "{left} + {right}";
}

// Usage
var result = compiler.Compile("2 + 3");
// Output: "2 + 3"
```

**What you learned:**
- Labeling pattern parts with `left:` and `right:`
- Using `.Returns()` to specify which parts to keep
- Placeholder substitution in output templates

## Tutorial 3: Multi-Operation Calculator (10 minutes)

Support `+`, `-`, `*`, `/`.

```csharp
class T3Tokens : TokenSet
{
    public Token Number = @"\d+";
    public Token Plus = @"\+";
    public Token Minus = @"-";
    public Token Multiply = @"\*";
    public Token Divide = @"/";
    public Token WS = new Token(@"\s+").Ignore();
}

class T3Rules : RuleSet
{
    // Respect operator precedence
    public Rule Expr = new Rule("Term (('+' | '-') Term)*");
    public Rule Term = new Rule("Factor (('*' | '/') Factor)*");
    public Rule Factor = new Rule("@Number | '(' Expr ')'");
}

class T3Maps : MapSet
{
    public Map Expr = "expr({terms})";
    public Map Term = "term({factors})";
    public Map Factor = "{value}";
}

// Usage
var result = compiler.Compile("(2 + 3) * 4");
```

**What you learned:**
- Grammar rules reference other rules
- Recursive grammar structures
- Operator precedence via grammar layering
- Alternation with `|`
- Grouping with parentheses

## Tutorial 4: Expression Chains (15 minutes)

Handle unlimited chains of operations.

```csharp
class T4Tokens : TokenSet
{
    public Token Number = @"\d+";
    public Token Plus = @"\+";
    public Token Minus = @"-";
    public Token WS = new Token(@"\s+").Ignore();
}

class T4Rules : RuleSet
{
    // Match one number, then zero or more (operator number) pairs
    public Rule Expr = new Rule("first:@Number rest:(Op @Number)*")
        .Returns("first", "rest");
    
    public Rule Op = new Rule("@Plus | @Minus");
}

class T4Maps : MapSet
{
    public Map Expr = "calc({first}, {rest})";
    public Map Op = "{operator}";
}

// Usage
var result = compiler.Compile("1 + 2 - 3 + 4 - 5");
```

**What you learned:**
- Repetition operators: `*` (zero or more), `+` (one or more), `?` (optional)
- Capturing repeated elements
- Pattern grouping with `(...)*`

## Tutorial 5: JSON Parser (30 minutes)

Build a real-world parser for JSON.

```csharp
class JSONTokens : TokenSet
{
    public Token String = @"""([^""\\]|\\.)*""";
    public Token Number = @"-?\d+(\.\d+)?([eE][+-]?\d+)?";
    public Token True = "true";
    public Token False = "false";
    public Token Null = "null";
    public Token LBrace = @"\{";
    public Token RBrace = @"\}";
    public Token LBracket = @"\[";
    public Token RBracket = @"\]";
    public Token Comma = ",";
    public Token Colon = ":";
    public Token WS = new Token(@"\s+").Ignore();
}

class JSONRules : RuleSet
{
    public Rule Value = new Rule("@String | @Number | Object | Array | @True | @False | @Null");
    
    public Rule Object = new Rule("'{' pairs:Pairs? '}'")
        .Returns("pairs");
    
    public Rule Pairs = new Rule("first:Pair rest:(',' Pair)*")
        .Returns("first", "rest");
    
    public Rule Pair = new Rule("key:@String ':' value:Value")
        .Returns("key", "value");
    
    public Rule Array = new Rule("'[' items:Values? ']'")
        .Returns("items");
    
    public Rule Values = new Rule("first:Value rest:(',' Value)*")
        .Returns("first", "rest");
}

class JSONMaps : MapSet
{
    public Map Value = "{value}";
    public Map Object = "{{ {pairs} }}";
    public Map Pairs = "{first}, {rest}";
    public Map Pair = "{key}: {value}";
    public Map Array = "[ {items} ]";
    public Map Values = "{first}, {rest}";
}

// Usage
var json = @"{""name"": ""CDTk"", ""version"": 1.0}";
var result = compiler.Compile(json);
```

**What you learned:**
- Complex token patterns (strings, numbers with exponents)
- Nested grammar structures
- Optional elements with `?`
- Real-world grammar design patterns

## Tutorial 6: Variable Declarations (45 minutes)

Build a mini programming language with variables.

```csharp
class VarTokens : TokenSet
{
    // Keywords MUST come before identifiers!
    public Token Let = "let";
    public Token Const = "const";
    public Token Identifier = @"[a-zA-Z_][a-zA-Z0-9_]*";
    public Token Number = @"\d+";
    public Token String = @"""[^""]*""";
    public Token Equals = "=";
    public Token Semicolon = ";";
    public Token WS = new Token(@"\s+").Ignore();
    public Token Comment = new Token(@"//[^\n]*").Ignore();
}

class VarRules : RuleSet
{
    public Rule Program = new Rule("Statement+");
    
    public Rule Statement = new Rule("Declaration ';'")
        .Returns("Declaration");
    
    public Rule Declaration = new Rule("kind:(@Let | @Const) name:@Identifier '=' value:Value")
        .Returns("kind", "name", "value");
    
    public Rule Value = new Rule("@Number | @String | @Identifier");
}

class VarMaps : MapSet
{
    public Map Program = "{statements}";
    public Map Statement = "{declaration};";
    public Map Declaration = "{kind} {name} = {value}";
    public Map Value = "{value}";
}

// Usage
var code = @"
    let x = 42;
    const name = ""CDTk"";
    let y = x;
";
var result = compiler.Compile(code);
```

**What you learned:**
- Token priority (keywords before identifiers)
- Ignoring comments
- One-or-more repetition (`+`)
- Building a statement-based language

## Common Patterns Reference

### Token Patterns

```csharp
// Numbers
public Token Integer = @"\d+";
public Token Float = @"\d+\.\d+";
public Token Scientific = @"\d+(\.\d+)?([eE][+-]?\d+)?";

// Strings
public Token String = @"""[^""]*""";                    // Simple
public Token StringEscape = @"""([^""\\]|\\.)*""";      // With escapes

// Identifiers
public Token Identifier = @"[a-zA-Z_][a-zA-Z0-9_]*";

// Comments
public Token LineComment = new Token(@"//[^\n]*").Ignore();
public Token BlockComment = new Token(@"/\*.*?\*/").Ignore();

// Whitespace (always ignore)
public Token WS = new Token(@"\s+").Ignore();
```

### Grammar Patterns

```csharp
// Optional
public Rule FuncDecl = new Rule("@Function name:@Id params:ParamList?");

// One or more
public Rule Block = new Rule("Statement+");

// Zero or more
public Rule Args = new Rule("Arg*");

// Alternation
public Rule Literal = new Rule("@Number | @String | @True | @False");

// Grouping
public Rule Expr = new Rule("Term (('+' | '-') Term)*");

// Recursion
public Rule Expr = new Rule("@Number | '(' Expr ')'");
```

## Tips for Success

### 1. **Start Simple, Then Expand**
Build your language incrementally. Get numbers working, then add operators, then functions, etc.

### 2. **Test Each Grammar Rule**
Test each rule in isolation before combining them.

### 3. **Keywords Come First**
Always define keyword tokens before generic identifiers:
```csharp
public Token If = "if";           // FIRST
public Token Identifier = @"\w+"; // LAST
```

### 4. **Use Descriptive Names**
```csharp
// Good
public Token LeftParen = @"\(";
public Rule FunctionCall = new Rule("name:@Id '(' args:ArgList ')'");

// Not as clear
public Token LP = @"\(";
public Rule FC = new Rule("n:@I '(' a:AL ')'");
```

### 5. **Check Diagnostics**
Always check `result.Diagnostics.HasErrors`:
```csharp
if (result.Diagnostics.HasErrors)
{
    foreach (var d in result.Diagnostics.Items)
        Console.WriteLine($"{d.Level}: {d.Message} at {d.Span}");
}
```

## Next Steps

- **[Core Concepts](04-CoreConcepts.md)** - Understand the architecture
- **[TokenSet Guide](05-TokenSet.md)** - Master lexical analysis
- **[RuleSet Guide](06-RuleSet.md)** - Master grammar definition
- **[Real-World Examples](15-Examples.md)** - See complete implementations
- **[Best Practices](16-BestPractices.md)** - Professional patterns

---

**You're now equipped to build real compilers with CDTk!** 🎉
