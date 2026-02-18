# Functional Map API - Architecture Guide

## User-Defined Semantic Context

### Key Architectural Point

**All semantic context fields are USER-DEFINED, not built into CDTk's base MapSet class.**

This is a fundamental design decision that provides unlimited extensibility.

## What's in Base MapSet (CDTk)

The base `MapSet` class in CDTk provides **only framework infrastructure**:

```csharp
public class MapSet
{
    // Framework infrastructure:
    protected __AllTokens? __AllTokens { get; private set; }
    protected __AllRules? __AllRules { get; private set; }
    protected __Ast? __Ast { get; private set; }
    
    // NO semantic fields like OptHints, Dialect, Minify, etc.
    // Users define their own!
}
```

## What's in WASM MapSet (User Implementation)

The `WASM` class extends `MapSet` and adds **user-defined semantic fields**:

```csharp
public class WASM : MapSet
{
    // USER-DEFINED semantic fields:
    public string Dialect { get; set; } = "WASM";
    public bool Minify { get; set; } = false;
    public OptimizationHints OptHints { get; set; } = new OptimizationHints();
    
    // Users can add ANY fields they want!
}
```

## Why This Architecture?

### 1. Unlimited Extensibility

Different target languages need different semantic context:

```csharp
// WASM compiler
public class WASM : MapSet
{
    public OptimizationHints OptHints { get; set; }
    public bool Minify { get; set; }
}

// Python compiler
public class Python : MapSet
{
    public int IndentWidth { get; set; } = 4;
    public bool UseTabs { get; set; } = false;
    public string QuoteStyle { get; set; } = "double";
}

// ARM assembler
public class ARM : MapSet
{
    public string TargetArch { get; set; } = "ARMv8";
    public bool ThumbMode { get; set; } = false;
    public Dictionary<string, string> RegisterAllocation { get; set; }
}
```

### 2. No Framework Limitations

The framework doesn't dictate what context Maps can access. Users decide based on their needs.

### 3. Clean Separation of Concerns

- **CDTk**: Provides transformation engine
- **Models**: Populate semantic fields
- **MapSet**: Declares semantic fields (user-defined)
- **Maps**: Access semantic fields for formatting

## Examples of User-Defined Fields

### Optimization Hints

```csharp
public class OptimizationHints
{
    public Dictionary<string, bool> CanInline { get; set; }
    public Dictionary<string, bool> RequiresBlock { get; set; }
}

// In Map:
if (this.OptHints.CanInline[self.Id])
    return "inline " + code();
```

### Target Architecture

```csharp
public string TargetArch { get; set; } = "x86_64";
public int PointerSize { get; set; } = 8;

// In Map:
if (this.TargetArch == "ARM")
    return arm_format();
```

### Formatting Style

```csharp
public string BraceStyle { get; set; } = "K&R";
public int IndentWidth { get; set; } = 2;

// In Map:
if (this.BraceStyle == "Allman")
    return "\n{\n" + body() + "\n}";
```

### Debug/Release

```csharp
public bool DebugMode { get; set; } = false;
public bool EmitComments { get; set; } = true;

// In Map:
if (this.DebugMode)
    return "/* DEBUG: " + info + " */ " + code();
```

### Dialect Variants

```csharp
public string Dialect { get; set; } = "WASM";

// In Map:
if (this.Dialect == "Python")
    return "if " + cond() + ":\n" + body();
else if (this.Dialect == "JavaScript")
    return "if (" + cond() + ") { " + body() + " }";
```

## How to Add Custom Fields

1. **Declare in your MapSet class:**

```csharp
public class MyCompiler : MapSet
{
    public string MyCustomField { get; set; } = "default";
    public MyCustomClass MyData { get; set; } = new MyCustomClass();
}
```

2. **Populate from Models:**

```csharp
public class MyModel : Model
{
    public override object Build(object input)
    {
        // Analyze AST
        var analysis = AnalyzeCode(input);
        
        // Return metadata
        return new MyCustomClass {
            Data = analysis
        };
    }
}

// In compiler:
var mapSet = new MyCompiler();
mapSet.MyData = myModel.Build(ast) as MyCustomClass;
```

3. **Access in Maps:**

```csharp
public Map MyMap => new Map(
    (Func<string> child, MapReference self) =>
    {
        if (this.MyCustomField == "special")
            return special_format();
        
        var data = this.MyData.GetInfo(self.Id);
        return format_with_data(data, child());
    }
);
```

## Real-World Example

```csharp
public class WebAssembly : MapSet
{
    // Target variant
    public string WasmVersion { get; set; } = "MVP";  // or "1.0", "2.0", etc.
    
    // Performance hints
    public Dictionary<string, int> LoopUnrollFactors { get; set; } = new();
    public HashSet<string> HotPaths { get; set; } = new();
    
    // Memory model
    public bool Use64BitMemory { get; set; } = false;
    public int PageSize { get; set; } = 65536;
    
    // Output options
    public bool EmitNames { get; set; } = true;
    public bool EmitDebugSections { get; set; } = false;
    
    // Maps use these fields:
    public Map Loop => new Map(
        (Func<string> body, MapReference self) =>
        {
            var unroll = this.LoopUnrollFactors.GetValueOrDefault(self.Id, 1);
            var isHot = this.HotPaths.Contains(self.Id);
            
            if (unroll > 1)
                return UnrolledLoop(body(), unroll);
            
            if (isHot && this.WasmVersion == "2.0")
                return "(loop (result i32) ...)";  // SIMD version
            
            return "(loop ...)";  // Standard version
        }
    );
}
```

## Summary

✅ **Semantic fields are USER-DEFINED** - Not in base MapSet
✅ **Users have complete freedom** - Add any fields needed
✅ **No framework limitations** - Architecture is unlimited extensible
✅ **Maps access via `this.*`** - Clean, type-safe access
✅ **Models populate fields** - Clean separation of concerns

This architecture ensures the functional Map API can support ANY target language, ANY optimization strategy, and ANY code generation requirements without framework modifications.
