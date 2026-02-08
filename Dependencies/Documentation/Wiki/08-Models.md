# Models: Semantic Analysis

Models extend the base Model class to perform semantic analysis on the AST.

## Basic Model

```csharp
public class MyModel : Model
{
    public MyModel(__AllRules rules, __Ast ast) : base(rules, ast)
    {
        // Perform semantic analysis
    }
}
```

## Integration with MapSet

```csharp
class Maps : MapSet
{
    public MyModel Model => new MyModel(__AllRules!, __Ast!);
    public Map Expr = "{left} + {right}";
}
```

Models have access to:
- `__AllRules` - All grammar rules
- `__Ast` - The parsed AST
- Semantic context

See: [Core Concepts](04-CoreConcepts.md)
