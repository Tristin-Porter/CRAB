# Getting Started with CDTk

Welcome to CDTk! You're about to discover the fastest, safest, and most elegant way to build compilers and interpreters in C#. Whether you're creating a domain-specific language, building a new programming language, or just want to parse complex data formats, CDTk has you covered.

## What is CDTk?

CDTk (Compiler Description Toolkit) is a modern compiler framework that provides:

- **🚀 High Performance**: 100M+ chars/sec lexing, 5-10M+ nodes/sec parsing
- **🛡️ Memory Safe**: 100% safe managed C#, no unsafe code ever
- **📝 Declarative**: Define WHAT your language looks like, not HOW to parse it
- **🎯 Complete Pipeline**: Lexing → Parsing → Semantic Analysis → Code Generation
- **✨ AG-LL Parser**: Adaptive Generalized LL(*) - handles ANY context-free grammar
- **🔧 Type-Safe**: Strongly-typed APIs with compile-time validation

## Why Choose CDTk?

### 🌟 It Just Works
```csharp
class Tokens : TokenSet
{
    public Token Number = @"\d+";
    public Token Plus = @"\+";
}

class Rules : RuleSet
{
    public Rule Expr = new Rule("@Number '+' @Number");
}

class Maps : MapSet
{
    public Map Expr = "{left} + {right}";
}

var compiler = new Compiler()
    .WithTokens(new Tokens())
    .WithRules(new Rules())
    .WithTarget(new Maps())
    .Build();

var result = compiler.Compile("2+3");
// That's it! You've built a compiler.
```

### 🎓 Designed for Success

CDTk eliminates the complexity traditionally associated with compiler construction:

- **No parser generators**: Everything is C# code
- **No external tools**: Pure .NET, nothing to install
- **No hidden magic**: Every behavior is explicit and inspectable
- **No unsafe code**: Memory safety guaranteed

### 💪 Production-Ready

CDTk is built on solid theoretical foundations:

- **AG-LL Parsing**: Combines ALL(*) predictive parsing with GLL generalized parsing
- **Full CFG Support**: ANY context-free grammar works - no restrictions
- **Left Recursion Handling**: Automatic elimination, invisible to you
- **Rich Diagnostics**: Comprehensive error reporting with source locations

## Your First 5 Minutes

### 1. Add CDTk to Your Project

Currently, CDTk is included as source. Copy `CDTk.cs` into your project:

```bash
# Clone the repository
git clone https://github.com/Tristin-Porter/CDTk
cd CDTk/Boilerplate

# Copy CDTk.cs to your project
cp CDTk.cs /path/to/your/project/
```

### 2. Create Your First Compiler

Create a new file `MyCompiler.cs`:

```csharp
using System;
using CDTk;

class MyTokens : TokenSet
{
    public Token Number = @"\d+";
    public Token WS = new Token(@"\s+").Ignore();
}

class MyRules : RuleSet
{
    public Rule Root = new Rule("@Number");
}

class MyMaps : MapSet
{
    public Map Root = "Number: {value}";
}

class Program
{
    static void Main()
    {
        var compiler = new Compiler()
            .WithTokens(new MyTokens())
            .WithRules(new MyRules())
            .WithTarget(new MyMaps())
            .Build();

        var result = compiler.Compile("42");
        
        if (!result.Diagnostics.HasErrors)
        {
            Console.WriteLine(result.Output[0]); // "Number: 42"
        }
    }
}
```

### 3. Run It!

```bash
dotnet run
```

**Congratulations!** 🎉 You just built your first compiler with CDTk!

## Understanding the Three Components

Every CDTk compiler has three essential parts:

### 1. **TokenSet** - Define Your Tokens
```csharp
class Tokens : TokenSet
{
    public Token Number = @"\d+";        // Matches numbers
    public Token Plus = @"\+";           // Matches +
    public Token WS = new Token(@"\s+").Ignore();  // Ignore whitespace
}
```

**What it does**: Breaks input text into meaningful chunks (tokens)

### 2. **RuleSet** - Define Your Grammar  
```csharp
class Rules : RuleSet
{
    public Rule Expr = new Rule("left:@Number '+' right:@Number")
        .Returns("left", "right");
}
```

**What it does**: Defines how tokens combine into structures

### 3. **MapSet** - Define Your Output
```csharp
class Maps : MapSet
{
    public Map Expr = "{left} + {right}";
}
```

**What it does**: Transforms parsed structures into target output

## Next Steps

### 📚 Learn the Fundamentals
- **[Quick Start Tutorial](02-QuickStart.md)** - Build increasingly complex compilers
- **[Core Concepts](04-CoreConcepts.md)** - Deep dive into CDTk's architecture
- **[TokenSet Guide](05-TokenSet.md)** - Master lexical analysis

### 🎯 Explore Real Examples
- **[Real-World Examples](15-Examples.md)** - Complete language implementations
- Calculator with operators
- JSON parser
- Expression evaluator
- Simple programming language

### 🚀 Advanced Topics
- **[AG-LL Parser](12-AGLLParser.md)** - Understand the magic
- **[Performance Optimization](14-Performance.md)** - Maximize speed
- **[Best Practices](16-BestPractices.md)** - Professional patterns

## Common Patterns

### Ignore Whitespace
```csharp
public Token WS = new Token(@"\s+").Ignore();
```

### Define Keywords (Order Matters!)
```csharp
class Tokens : TokenSet
{
    public Token If = "if";               // Define keywords FIRST
    public Token While = "while";
    public Token Identifier = @"[a-z]+";  // Generic patterns LAST
}
```

### Optional Elements
```csharp
public Rule FuncDecl = new Rule("@Function name:@Id params:ParamList?");
```

### Repetition
```csharp
public Rule Block = new Rule("Statement+");  // One or more
public Rule Args = new Rule("Arg*");         // Zero or more
```

### Alternation
```csharp
public Rule Literal = new Rule("@Number | @String | @True | @False");
```

## Philosophy

CDTk is built on three core principles:

### 1. **Declarative over Imperative**
Define WHAT your language looks like, not HOW to parse it. CDTk figures out the parsing strategy for you.

### 2. **Type-Safe over Magic**
Explicit types and relationships. No hidden behavior. Everything is inspectable and debuggable.

### 3. **Performance with Safety**
Fast execution without sacrificing memory safety. 100% safe C#, no exceptions.

## Getting Help

- **[FAQ](17-FAQ.md)** - Common questions answered
- **[Troubleshooting](18-Troubleshooting.md)** - Fix common issues
- **[GitHub Issues](https://github.com/Tristin-Porter/CDTk/issues)** - Report bugs or request features

## What Makes CDTk Different?

Unlike other compiler frameworks:

✅ **No code generation step** - Everything is pure C#  
✅ **No grammar files** - Just C# classes  
✅ **Full CFG support** - ANY grammar works  
✅ **Safe by default** - No unsafe code, ever  
✅ **Predictable performance** - No surprises  
✅ **Rich diagnostics** - Helpful error messages  

## Ready to Build?

You now have everything you need to start building with CDTk. The framework handles the complexity - you focus on your language design.

**Next:** [Quick Start Tutorial](02-QuickStart.md) - Build progressively complex compilers

---

*CDTk: Making compiler construction accessible, safe, and fun!* 🚀
