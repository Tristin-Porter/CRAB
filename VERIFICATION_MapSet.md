# MapSet.cs Verification - ONE Example Map

## What Was Done

✅ **Deleted entire MapSet.cs** (2383 lines)
✅ **Created fresh minimal MapSet.cs** (116 lines) 
✅ **One example Map**: `MethodDeclaration`
✅ **Removed Python/Dialect** references
✅ **Build succeeds** with 0 errors

## The Example Map Pattern

```csharp
public Map MethodDeclaration => new Map(
    (Func<string> parameters, Func<string> body) =>
    {
        // Inside the lambda:
        // - 'this' = MapSet instance (self-reference)
        // - Can access: this.Methods, this.Minify, this.OtherMap
        // - Can call child Maps: parameters(), body()
        // - NO AST access: no node.Fields, no node.Type
        
        var meta = new MethodMetadata 
        { 
            Name = "placeholder", 
            ResultType = "i32" 
        };
        
        var sb = new StringBuilder();
        sb.Append($"(func ${meta.Name}");
        
        // Call child Map for parameters
        var paramsWat = parameters();
        if (!string.IsNullOrWhiteSpace(paramsWat))
        {
            if (this.Minify)
                sb.Append(paramsWat);
            else
                sb.Append("\n  " + paramsWat);
        }
        
        // Add result type if not void
        if (!string.IsNullOrWhiteSpace(meta.ResultType))
        {
            if (this.Minify)
                sb.Append($"(result {meta.ResultType})");
            else
                sb.Append($"\n  (result {meta.ResultType})");
        }
        
        // Call child Map for body
        var bodyWat = body();
        if (!string.IsNullOrWhiteSpace(bodyWat))
        {
            if (this.Minify)
                sb.Append(bodyWat);
            else
                sb.Append("\n" + bodyWat);
        }
        
        sb.Append(this.Minify ? ")" : "\n)");
        return sb.ToString();
    }
);
```

## Key Features Demonstrated

### ✅ Child Formatters as Parameters
```csharp
(Func<string> parameters, Func<string> body) =>
```
- Parameter names match AST field names
- Calling `parameters()` formats the 'parameters' child node
- Calling `body()` formats the 'body' child node
- CDTk injects the appropriate Maps for these fields

### ✅ MapSet Self-Reference via 'this'
```csharp
if (this.Minify)  // Access user-defined field
    sb.Append(paramsWat);

// Could also do:
// var ifMap = this.IfStatement;  // Reference another Map
// var meta = this.Methods["key"]; // Access semantic metadata
```

### ✅ NO MapReference self Parameter
```csharp
// OLD (removed):
(Func<string> parameters, Func<string> body, MapReference self) =>

// NEW (correct):
(Func<string> parameters, Func<string> body) =>
```

### ✅ NO AST Access
```csharp
// OLD (removed):
var node = self.Node;
var name = node.Fields["name"];
var type = node.Type;

// NEW (correct):
var meta = this.Methods["key"];  // Get from semantic metadata
var name = meta.Name;             // Populated by Model
```

### ✅ Semantic Metadata
```csharp
// USER-DEFINED field on MapSet
public Dictionary<string, MethodMetadata> Methods { get; set; } = new();

// USER-DEFINED class
public class MethodMetadata
{
    public string Name { get; set; } = "";
    public string ResultType { get; set; } = "";
}
```

## File Structure

```
MapSet.cs (116 lines)
├── WASM class extends MapSet
│   ├── User-defined semantic fields
│   │   ├── Dictionary<string, MethodMetadata> Methods
│   │   └── bool Minify
│   └── Maps
│       └── MethodDeclaration (example)
└── User-defined metadata classes
    └── MethodMetadata
```

## Questions for Verification

1. ✅ Is the parameter signature correct?
   - `(Func<string> parameters, Func<string> body) =>`
   
2. ✅ Is 'this' usage correct?
   - `this.Minify`, `this.Methods`, could do `this.OtherMap`
   
3. ✅ Is NO MapReference self correct?
   - Removed entirely
   
4. ✅ Is semantic metadata structure correct?
   - User-defined Dictionary + class
   
5. ❓ Node key mechanism?
   - Currently using placeholder metadata
   - TODO: How to get correct entry for current node?

## Next Steps After Approval

If this pattern is correct, will:
1. Add remaining 450+ Maps using same pattern
2. Add all semantic metadata classes
3. Implement Models to populate metadata
4. Wire up compiler pipeline

## Build Status

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

File location: `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
Backup of old file: `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs.backup`
