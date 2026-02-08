# RuleSet: Grammar & Parsing Guide

RuleSet defines your language's grammar - how tokens combine into structures.

## Basic RuleSet

```csharp
class MyRules : RuleSet
{
    public Rule Expr = new Rule("left:@Number '+' right:@Number")
        .Returns("left", "right");
}
```

## Pattern Syntax
- `@TokenName` - Match a token
- `RuleName` - Match another rule
- `'literal'` - Exact text match
- `|` - Alternation
- `*` - Zero or more
- `+` - One or more
- `?` - Optional
- `(...)` - Grouping

## The .Returns() Method
Specifies which labeled parts become AST fields. Unlabeled parts are included automatically.

## Grammar Generality
CDTk supports ANY context-free grammar via AG-LL parsing.

See: [AG-LL Parser Explained](12-AGLLParser.md)
