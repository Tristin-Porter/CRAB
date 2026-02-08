# AST Construction

The Abstract Syntax Tree (AST) is the structured representation of your parsed code.

## How ASTs are Built
CDTk automatically constructs AST nodes from:
1. Rule patterns
2. .Returns() mappings
3. Nested rule structures

## AST Node Structure
```csharp
public sealed class AstNode
{
    public string Type { get; }        // Rule name
    public AstNode[] Children { get; }  // Child nodes
    public SourceSpan Span { get; }    // Source location
    // Access fields by label...
}
```

## Accessing AST Data
In MapSet templates, use `{fieldName}` to access labeled fields.

See: [Core Concepts](04-CoreConcepts.md)
