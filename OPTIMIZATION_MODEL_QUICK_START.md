# Optimization Model Quick Start Guide

## TL;DR - What You Need to Know

**Current State**: Optimization.cs is a 9-line stub ready for implementation  
**Pattern**: Follow Automatic.cs (1,127 lines) and Manual.cs (992 lines) as examples  
**Integration**: Models are properties of MapSet, not standalone components  
**Requirement**: MUST preserve 100% memory safety - no compromises  

## Quick Integration Checklist

### 1. Update Optimization.cs

```csharp
using CDTk;
using System;
using System.Collections.Generic;

public class Optimization : Model
{
    private readonly __AllRules _rules;
    private readonly __Ast _ast;

    public Optimization(__AllRules rules, __Ast ast)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _ast = ast ?? throw new ArgumentNullException(nameof(ast));
    }

    protected __AllRules Rules => _rules;

    public override object Build(object input)
    {
        var ast = _ast?.Root ?? input as AstNode;
        if (ast == null)
        {
            throw new InvalidOperationException("Optimization model requires AST input");
        }

        var context = new OptimizationContext();
        
        try
        {
            // Phase 1: Analyze opportunities
            var opportunities = AnalyzeOptimizations(ast, context);
            
            // Phase 2: Validate safety
            var safeTransforms = ValidateSafety(opportunities, context);
            
            // Phase 3: Apply optimizations
            var optimizedAst = ApplyOptimizations(ast, safeTransforms, context);
            
            // Phase 4: Return annotations
            return new OptimizationAnnotations
            {
                OriginalAST = ast,
                OptimizedAST = optimizedAst,
                AppliedOptimizations = safeTransforms,
                IsOptimized = true,
                Diagnostics = context.Diagnostics
            };
        }
        catch (Exception ex)
        {
            context.Diagnostics.Add(new OptimizationDiagnostic(
                OptimizationDiagnosticLevel.Error,
                $"Optimization failed: {ex.Message}",
                ex
            ));
            throw;
        }
    }
    
    // Implement phases here...
}
```

### 2. Add to MapSet.cs

**Location**: Line 36 in `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`

```csharp
/// <summary>
/// Optimization model for code transformations.
/// Applies safe optimizations that preserve memory safety guarantees.
/// </summary>
public Optimization OptimizationModel => new Optimization(__AllRules!, __Ast!);
```

**Add helper method** (after line 77):

```csharp
/// <summary>
/// Get optimization annotations.
/// </summary>
private OptimizationAnnotations? GetOptimizationAnnotations()
{
    if (__Ast?.Root == null) return null;
    
    try
    {
        return OptimizationModel.Build(__Ast.Root) as OptimizationAnnotations;
    }
    catch
    {
        return null; // Maps generate unoptimized WASM
    }
}
```

### 3. Create Test Structure

```bash
mkdir -p Testing/Optimization/{DeadCode,ConstantFolding,Inlining,CommonSubexpression,LoopOptimization}
```

### 4. Write First Test

**File**: `Testing/Optimization/ConstantFolding/Simple.cs`

```csharp
namespace OptimizationTest
{
    /// <summary>
    /// Test: Basic constant folding
    /// Expected: Compile-time evaluation
    /// </summary>
    class ConstantFoldingTest
    {
        static int TestBasic()
        {
            // Should fold to: return 42;
            return 10 + 20 + 12;
        }
    }
}
```

### 5. Test Your Changes

```bash
# Build
dotnet build

# Test compilation
dotnet run -- check Testing/Optimization/ConstantFolding/Simple.cs

# View output
dotnet run -- compile --emit-text Testing/Optimization/ConstantFolding/Simple.cs
```

## Key Files to Read

1. **CRAB_REPOSITORY_OVERVIEW.md** (this directory) - Complete guide
2. **Documentation/Internal/MODEL_MAPSET_INTEGRATION.md** - Integration patterns
3. **Compiler/Models/Automatic.cs** - Example model structure
4. **Compiler/Models/Manual.cs** - Example verification approach

## Safety Rules (MUST FOLLOW)

✅ **Allowed**:
- Constant folding/propagation
- Dead code elimination
- Inlining (respecting lifetimes)
- Common subexpression elimination
- Loop optimizations (invariant code motion, strength reduction)
- Tail call optimization

❌ **Forbidden**:
- Runtime garbage collection
- Cross-model optimizations (automatic ↔ manual)
- Non-deterministic transformations
- Breaking CTGC deallocation points
- Eliminating safety checks

## Common Patterns

### Phase Structure

All models follow this pattern:

```csharp
public override object Build(object input)
{
    var ast = GetAstNode(input);
    var context = CreateContext();
    
    try
    {
        // Phase 1: Analysis
        var analysis = Analyze(ast, context);
        
        // Phase 2: Validation
        var validated = Validate(analysis, context);
        
        // Phase 3: Transformation
        var transformed = Transform(validated, context);
        
        // Phase 4: Return annotations
        return CreateAnnotations(transformed, context);
    }
    catch (Exception ex)
    {
        HandleError(ex, context);
        throw;
    }
}
```

### Data Structures

```csharp
class OptimizationContext
{
    public List<OptimizationDiagnostic> Diagnostics { get; }
    public int OptimizationLevel { get; set; } = 2;
}

class OptimizationAnnotations
{
    public AstNode? OriginalAST { get; set; }
    public AstNode? OptimizedAST { get; set; }
    public List<Transformation> AppliedOptimizations { get; set; }
    public bool IsOptimized { get; set; }
    public List<OptimizationDiagnostic> Diagnostics { get; set; }
}
```

### Safety Validation

```csharp
private bool IsMemorySafe(Transformation t, Context ctx)
{
    // Check if transformation preserves safety
    return !AffectsLifetimes(t) &&
           !CrossesModelBoundary(t) &&
           !ViolatesCTGC(t);
}
```

## Build & Test Commands

```bash
# Build
dotnet restore
dotnet build

# Run CRAB
dotnet run

# Test single file
dotnet run -- check path/to/test.cs

# View WASM output
dotnet run -- compile --emit-text path/to/test.cs
cat test.wat

# Run test suite
./run_tests.sh
./run_tests.sh optimization  # After creating optimization tests
```

## Development Workflow

1. **Read** CRAB_REPOSITORY_OVERVIEW.md and MODEL_MAPSET_INTEGRATION.md
2. **Study** Automatic.cs and Manual.cs structure
3. **Design** optimization phases on paper
4. **Implement** one optimization at a time (start simple)
5. **Test** with dedicated test files
6. **Verify** safety preservation
7. **Document** approach and decisions
8. **Iterate** - add more optimizations

## File Locations

```
Key Files:
  Compiler/Models/Optimization.cs           # Your implementation
  Compiler/Core/MapSet.cs                   # Integration point
  Testing/Optimization/                     # Your tests
  Documentation/Internal/                   # Add your docs here

References:
  .github/agents/crab-spec.txt             # CRAB specification
  Compiler/Models/Automatic.cs             # Example model
  Compiler/Models/Manual.cs                # Example model
  Documentation/Internal/MODEL_MAPSET_INTEGRATION.md
  CRAB_REPOSITORY_OVERVIEW.md              # Complete guide
```

## Example: Simple Constant Folding

```csharp
private List<OptimizationOpportunity> FindConstantExpressions(AstNode ast, Context ctx)
{
    var opportunities = new List<OptimizationOpportunity>();
    
    // Find binary operations with constant operands
    if (ast is BinaryExpression binary)
    {
        if (IsConstant(binary.Left) && IsConstant(binary.Right))
        {
            opportunities.Add(new OptimizationOpportunity
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Constant folding",
                Node = binary,
                Type = OptimizationType.ConstantFolding
            });
        }
    }
    
    // Recurse through children
    foreach (var child in ast.Children)
    {
        opportunities.AddRange(FindConstantExpressions(child, ctx));
    }
    
    return opportunities;
}

private AstNode ApplyConstantFolding(BinaryExpression binary)
{
    var left = EvaluateConstant(binary.Left);
    var right = EvaluateConstant(binary.Right);
    var result = EvaluateOperation(binary.Operator, left, right);
    
    return new ConstantExpression(result);
}
```

## Getting Help

- **Full Guide**: See CRAB_REPOSITORY_OVERVIEW.md (1,211 lines)
- **Integration**: See Documentation/Internal/MODEL_MAPSET_INTEGRATION.md
- **Spec**: See .github/agents/crab-spec.txt
- **Examples**: See Compiler/Models/Automatic.cs and Manual.cs

## Next Steps

1. Read CRAB_REPOSITORY_OVERVIEW.md thoroughly
2. Study MODEL_MAPSET_INTEGRATION.md
3. Review Automatic.cs implementation
4. Design your optimization phases
5. Start implementing (constant folding is easiest)
6. Test early and often
7. Document as you go

---

**Remember**: Safety first! An unoptimized but safe program is better than an optimized but unsafe one. CRAB's core guarantee is 100% memory safety - never compromise it.
