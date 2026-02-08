# MapSet: Code Generation

MapSet transforms AST nodes into target output.

## Basic MapSet

```csharp
class MyMaps : MapSet
{
    public Map Expr = "{left} + {right}";
    public Map Number = "{value}";
}
```

## Placeholder Substitution
`{fieldName}` placeholders are replaced with actual AST field values.

## Fallback Mapping

```csharp
public Map Fallback = "// TODO: Handle {type}";
```

Handles unmapped node types.

## Multi-Target Support
Generate code for multiple target languages from one AST.

See: [Core Concepts](04-CoreConcepts.md)
