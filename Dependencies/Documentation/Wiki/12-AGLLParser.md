# AG-LL Parser Explained

CDTk's AG-LL (Adaptive Generalized LL) parser is what makes it possible to support ANY context-free grammar while maintaining excellent performance.

## What is AG-LL?

AG-LL combines two parsing strategies:

### 1. **ALL(*) - Adaptive LL(*)** (Fast Path)
- Predictive parsing like traditional LL parsers
- Dynamic lookahead (adapts to grammar complexity)
- Deterministic execution
- Near-linear performance
- Handles most common grammar patterns

### 2. **GLL - Generalized LL** (Fallback Path)
- Handles ANY context-free grammar
- Supports left recursion
- Supports ambiguity
- Uses Graph-Structured Stack (GSS)
- Builds Shared Packed Parse Forest (SPPF) for ambiguous regions

## How It Works

```
Input → Try ALL(*) → Success? → Done
              ↓ No
         Try GLL → Success → Cache result → Done
```

**Key Insight**: Most grammars have deterministic regions. AG-LL uses fast ALL(*) parsing there, only escalating to GLL when needed.

## Performance Characteristics

### Deterministic Grammars
- **Speed**: 5-10M AST nodes/sec
- **Strategy**: Pure ALL(*) path
- **Examples**: Most expression grammars, JSON, XML

### Complex Grammars
- **Speed**: Still practical (varies by complexity)
- **Strategy**: ALL(*) + GLL hybrid
- **Examples**: Left-recursive grammars, ambiguous grammars

### Worst Case
- **Complexity**: O(n³)
- **Reality**: Rare in practice due to DFA caching

## Grammar Support

CDTk supports ALL of these:

✅ **Left Recursion**
```csharp
public Rule Expr = new Rule("Expr '+' @Number | @Number");
```

✅ **Right Recursion**
```csharp
public Rule List = new Rule("@Item List | @Item");
```

✅ **Mutual Recursion**
```csharp
public Rule A = new Rule("B '+' @Number");
public Rule B = new Rule("A '*' @Number | @Number");
```

✅ **Ambiguous Grammars**
CDTk detects ambiguity and can report it via SPPF.

✅ **Deeply Nested Structures**
```csharp
public Rule Expr = new Rule("@Number | '(' Expr ')'");
```

## Optimizations

### 1. **DFA Caching**
GLL results are cached in a DFA, making future parses of similar structures deterministic.

### 2. **Lazy SPPF**
Only built when ambiguity exists, only around ambiguous regions.

### 3. **Speculative Guarding**
Prevents unnecessary exploration of parse paths.

### 4. **Threshold-Based Escalation**
ALL(*) only escalates to GLL when prediction conflict exceeds threshold.

## Why This Matters

Traditional parser generators restrict you to:
- **LL(k)**: Limited lookahead, no left recursion
- **LR**: Complex tables, poor error messages
- **PEG**: Ordered choice (not true alternation), hidden precedence

**AG-LL gives you freedom**: Write grammars that match your language design, not parser limitations.

## Comparison

| Feature | AG-LL | LL(k) | LR | PEG |
|---------|-------|-------|-----|-----|
| Left Recursion | ✅ Auto-handled | ❌ | ✅ | ❌ |
| Ambiguity | ✅ Detected | ❌ | ✅ | ❌ (hidden) |
| Performance | ⚡ Adaptive | ⚡ Fast | ⚡ Fast | 🐢 Backtracking |
| Generality | ✅ ANY CFG | ❌ Limited | ✅ Most CFGs | ⚠️ PEG only |
| Error Messages | ✅ Excellent | ✅ Good | ❌ Poor | ⚠️ Tricky |

## Practical Impact

You can write grammars naturally:

**Expression Grammar (left-recursive)**
```csharp
public Rule Expr = new Rule("Expr '+' Term | Term");
public Rule Term = new Rule("Term '*' Factor | Factor");
public Rule Factor = new Rule("@Number | '(' Expr ')'");
```

CDTk automatically:
1. Detects left recursion
2. Eliminates it internally
3. Parses correctly
4. You never see the transformation

## Learning More

- **[Parsing Deep Dive](11-ParsingDeepDive.md)** - Advanced parsing techniques
- **[Performance](14-Performance.md)** - Optimization strategies
- **Internal Docs**: [AG-LL Implementation](../Internal/05-AGLLImplementation.md)

## The Bottom Line

**AG-LL means freedom**: Design your grammar to match your language, not to satisfy parser limitations. CDTk handles the complexity for you.
