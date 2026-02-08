# CDTk – The Compiler Description Toolkit

**CDTk** is a modern, high-performance compiler framework for .NET (C#). It lets you define lexical analysis, parsing, semantic models, and output generation in *one memory-safe, declarative pipeline*. CDTk is built for language designers who demand power, safety, clarity—and zero magic.

**Current Status:** v9.0.0 - Specification Compliant (88%)
- All critical architectural issues resolved
- AG-LL parser working correctly (ALL(*) predictive + GLL fallback)
- 100% safe managed code (no unsafe constructs)
- Full DFA caching with descriptor sets, GSS nodes, and SPPF fragments

---

## Why CDTk?

- **Unified Compiler Pipeline**  
  Tokenize, parse, analyze, and transform—all in one place. No templates or external tools.

- **Strongly-Typed & Declarative**  
  Define tokens, grammar rules, mappings, and models using real C# types and clean class-based modules.

- **Ultra-Fast, Safe Execution**  
  100M+ chars/sec lexing, 5-10M+ nodes/sec parsing (deterministic grammars), zero unsafe code.

- **Proper AG-LL Architecture**  
  Combines ALL(*) predictive parsing (O(n) for deterministic grammars) with GLL fallback (for ambiguity/recursion).

- **Instant Grammar Validation**  
  Get diagnostics for tokens, rules, and mapping before you compile.

- **Multi-Target Output**  
  Generate code for C, Python, JavaScript, and beyond—from one compiler definition.

- **Field-Based Identity**  
  Every token, rule, and map is uniquely identified by its variable—not strings, not types.

---

## Quick Example

```csharp
// Tiny expression language with CDTk
using CDTk;

// Define tokens
class Tokens : TokenSet
{
    public Token Number = @"\d+";
    public Token Plus = @"\+";
    public Token Whitespace = new Token(@"\s+").Ignore();
}

// Define grammar rules
class Rules : RuleSet
{
    public Rule Expression =
        new Rule("left:@Number '+' right:@Number")
            .Returns("left", "right");
}

// Define code generation maps
class Maps : MapSet
{
    public Map Expression = "{left} + {right}";
}

var compiler = new Compiler()
    .WithTokens(new Tokens())
    .WithRules(new Rules())
    .WithTarget(new Maps())
    .Build();

var result = compiler.Compile("2+2");
Console.WriteLine(result.Output); // "2 + 2"
```

---

## Key Features

- **AG-LL Parser:** Combines ALL(*) predictive parsing with GLL fallback for optimal performance
- **Models:** Add semantic analysis or transformation steps between AST and output
- **Zero Magic:** All data flows are explicit—no hidden injection or reflection surprises
- **Rich Diagnostics:** Validate grammar, mapping, and models before you compile, with automatic deduplication
- **Fallback Mapping:** Define default transformations for unmapped AST nodes
- **Full Specification Compliance:** 88% compliant with cdtk-spec.txt and ag-ll-spec.txt
- **Examples & Docs:** [Full documentation](Documentation/) and real-world guides

---

## Recent Improvements (v9.0.0)

CDTk has undergone comprehensive improvements to align with its specification:

✅ **Fixed AG-LL Architecture** - Parser now correctly uses ALL(*) predictive parsing first, with GLL as fallback  
✅ **Removed PEG Semantics** - Deleted Packrat memoization; strict CFG compliance  
✅ **Completed DFA Cache** - Full caching of descriptor sets, GSS nodes, and SPPF fragments  
✅ **100% Safe Code** - Removed all unsafe constructs (ReadOnlySpan replaced with ArraySegment)  
✅ **Diagnostic Deduplication** - Hash-based deduplication prevents duplicate errors  
✅ **Fallback Mapping** - Support for default map when no specific mapping exists  

See [Documentation/IMPLEMENTATION_FIXES_SUMMARY.md](Documentation/IMPLEMENTATION_FIXES_SUMMARY.md) for complete details.

---

## Get Started

- [NuGet: CDTk](https://www.nuget.org/packages/CDTk/)
- [Documentation](Documentation/) - Complete guides and API reference
- [Implementation Status](Documentation/IMPLEMENTATION_FIXES_SUMMARY.md) - Recent improvements and compliance
- [Contributing](https://github.com/Tristin-Porter/CDTk/wiki/Contributing)
