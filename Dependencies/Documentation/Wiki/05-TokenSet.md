# TokenSet: Lexical Analysis Guide

TokenSet defines the lexical structure of your language - how to break source code into meaningful tokens.

## Basic TokenSet

```csharp
class MyTokens : TokenSet
{
    public Token Number = @"\d+";
    public Token Identifier = @"[a-zA-Z_]\w*";
    public Token Plus = @"\+";
    public Token WS = new Token(@"\s+").Ignore();
}
```

## Key Concepts

### Field-Based Identity
The field name IS the token identity. Renaming the field renames all references.

### Token Priority
Order matters! Earlier fields match first:
```csharp
public Token If = "if";           // Check this first
public Token Identifier = @"\w+";  // Then this
```

### Ignoring Tokens
```csharp
public Token WS = new Token(@"\s+").Ignore();
public Token Comment = new Token(@"//[^\n]*").Ignore();
```

## Performance
- DFA-based matching: 100-200M chars/sec
- Zero allocations in hot paths
- String interning for common tokens
- Branch-predictable execution

## See Also
- [Lexical Analysis Deep Dive](10-LexicalAnalysisDeepDive.md)
- [Core Concepts](04-CoreConcepts.md)
