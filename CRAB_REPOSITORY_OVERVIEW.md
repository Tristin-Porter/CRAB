# CRAB Compiler Repository Structure - Comprehensive Overview

## Executive Summary

This document provides a comprehensive overview of the CRAB (Compiler for Reliably Acceptable Binaries) repository structure, focusing on implementing a new optimization model. CRAB is a zero-runtime C# to WebAssembly compiler with mathematically proven memory safety.

**Key Facts:**
- **Language**: C# 13 (.NET 10)
- **Framework**: CDTk.cs (Compiler Development Toolkit)
- **Target**: WebAssembly MVP
- **Memory Models**: Automatic (CTGC) and Manual (verified)
- **Build Status**: ✅ 0 errors, 3 warnings (CDTk only)
- **Line Count**: ~4,258 lines in compiler core

---

## 1. Current Optimization.cs Structure

**Location**: `/home/runner/work/CRAB/CRAB/Compiler/Models/Optimization.cs`

**Current Implementation** (9 lines):
```csharp
using CDTk;

class Optimization : Model
{
    // This is the model that applies optimizations (that do not compromise memory safety) to the code.
    public override object Build(object input)
    {
        return "";
    }
}
```

**Status**: Minimal stub implementation - ready for enhancement

**Purpose**: Apply optimization transformations to the AST that preserve memory safety guarantees established by the Automatic and Manual models.

---

## 2. MapSet Organization and Model Plugin System

### 2.1 MapSet Architecture

**Location**: `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`
- **File Size**: 916 lines
- **Map Definitions**: 188 public Map fields
- **Purpose**: Direct C# AST → WebAssembly text format (WAT) translation

### 2.2 Model Integration Pattern (CDTk Design)

Models are integrated as **properties** of the MapSet class, not standalone components:

```csharp
class WASM : MapSet
{
    // ============================================================
    // SEMANTIC ANALYSIS MODELS (Lines 18-36)
    // ============================================================
    
    /// <summary>
    /// Automatic memory model (CTGC) for semantic analysis.
    /// Performs lifetime inference, region analysis, and memory safety verification.
    /// Used by Maps to insert deallocation instructions and memory management code.
    /// </summary>
    public Automatic AutomaticModel => new Automatic(__AllRules!, __Ast!);
    
    /// <summary>
    /// Manual memory model for semantic analysis.
    /// Performs verification of manual{} blocks using abstract interpretation,
    /// symbolic execution, and ownership graphs.
    /// Used by Maps to verify and annotate manual memory operations.
    /// </summary>
    public Manual ManualModel => new Manual(__AllRules!, __Ast!);
    
    // Optimization model would be added here:
    // public Optimization OptimizationModel => new Optimization(__AllRules!, __Ast!);
    
    // ============================================================
    // HELPER METHODS FOR MODEL INTEGRATION (Lines 38-77)
    // ============================================================
    
    /// <summary>
    /// Get memory management annotations from the automatic model.
    /// This is called during WASM generation to insert deallocation instructions.
    /// </summary>
    private AutomaticAnnotations? GetAutomaticAnnotations()
    {
        if (__Ast?.Root == null) return null;
        
        try
        {
            return AutomaticModel.Build(__Ast.Root) as AutomaticAnnotations;
        }
        catch
        {
            // If model analysis fails, return null - Maps will generate basic WASM
            return null;
        }
    }
    
    /// <summary>
    /// Get manual memory verification results.
    /// This is called during WASM generation to verify manual blocks.
    /// </summary>
    private ManualAnnotations? GetManualAnnotations()
    {
        if (__Ast?.Root == null) return null;
        
        try
        {
            return ManualModel.Build(__Ast.Root) as ManualAnnotations;
        }
        catch
        {
            // If verification fails, return null - compilation will fail with diagnostics
            return null;
        }
    }
}
```

### 2.3 How Models Plug Into MapSet

**Integration Flow**:

1. **CDTk Initialization**: Parser creates AST and passes it to MapSet
2. **Model Property Access**: `__AllRules` and `__Ast` are set by CDTk framework
3. **Lazy Instantiation**: Models are created on-demand using `=>` property syntax
4. **Model.Build() Invocation**: Maps call model methods during code generation
5. **Annotation Return**: Models return metadata that guides WASM emission

**Key Points**:
- Models receive `__AllRules` (grammar) and `__Ast` (parsed tree) from CDTk
- Models extend the `CDTk.Model` base class
- Models implement `public override object Build(object input)` method
- Models return annotation objects that Maps use during code generation

### 2.4 Existing Models as Examples

#### Automatic Model (`Automatic.cs` - 1,127 lines)

```csharp
public class Automatic : Model
{
    private readonly __AllRules _rules;
    private readonly __Ast _ast;

    public Automatic(__AllRules rules, __Ast ast)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _ast = ast ?? throw new ArgumentNullException(nameof(ast));
    }

    public override object Build(object input)
    {
        var ast = _ast?.Root ?? input as AstNode;
        if (ast == null)
        {
            throw new InvalidOperationException("Automatic model requires AST input");
        }

        var context = new AutomaticContext();
        
        try
        {
            // Phase 1: Infer lifetimes
            var lifetimeGraph = InferLifetimes(ast, context);
            
            // Phase 2: Perform region analysis
            var regions = AnalyzeRegions(lifetimeGraph, context);
            
            // Phase 3: Track all allocations
            var allocations = TrackAllocations(ast, regions, context);
            
            // Phase 4: Compute deallocation points
            var deallocations = ComputeDeallocations(allocations, lifetimeGraph, context);
            
            // Phase 5: Verify memory safety guarantees
            VerifyMemorySafety(allocations, deallocations, lifetimeGraph, context);
            
            // Phase 6: Return annotated AST with memory management metadata
            var annotations = AnnotateAST(ast, allocations, deallocations, context);
            
            return annotations;
        }
        catch (Exception ex)
        {
            context.Diagnostics.Add(new AutomaticDiagnostic(
                AutomaticDiagnosticLevel.Error,
                $"Automatic memory model failed: {ex.Message}",
                ex
            ));
            throw;
        }
    }
    
    // ... 6 phases of CTGC analysis (1000+ lines)
}
```

**Automatic Model Phases**:
1. **Lifetime Inference**: Data flow analysis for birth/death points
2. **Region Analysis**: Group allocations with similar lifetimes
3. **Allocation Tracking**: Identify all heap allocations
4. **Deallocation Computation**: Calculate optimal free points
5. **Safety Verification**: Prove no leaks, use-after-free, double-free
6. **AST Annotation**: Return metadata for code generation

#### Manual Model (`Manual.cs` - 992 lines)

```csharp
public class Manual : Model
{
    private readonly __AllRules _rules;
    private readonly __Ast _ast;

    public Manual(__AllRules rules, __Ast ast)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _ast = ast ?? throw new ArgumentNullException(nameof(ast));
    }

    public override object Build(object input)
    {
        var ast = _ast?.Root ?? input as AstNode;
        if (ast == null)
        {
            throw new InvalidOperationException("Manual model requires AST input");
        }

        var context = new ManualContext();
        
        try
        {
            // Phase 1: Ownership graph construction
            var ownershipGraph = BuildOwnershipGraph(ast, context);
            
            // Phase 2: Abstract interpretation
            var abstractState = AbstractInterpret(ast, ownershipGraph, context);
            
            // Phase 3: Symbolic execution
            var symbolicResults = SymbolicExecute(ast, abstractState, context);
            
            // ... 10 verification phases
            
            return annotations;
        }
        catch (Exception ex)
        {
            context.Diagnostics.Add(new ManualDiagnostic(...));
            throw;
        }
    }
}
```

**Manual Model Phases**:
1. **Ownership Graph Construction**: Build pointer ownership relationships
2. **Abstract Interpretation**: Analyze all execution paths
3. **Symbolic Execution**: Prove safety properties mathematically
4. **Alias Tracking**: Track all pointer aliases
5. **Escape Analysis**: Identify pointers that escape scope
6. **Isolation Verification**: Ensure no cross-model aliasing
7. **Leak Detection**: Prove all allocations are freed
8. **Use-After-Free Detection**: Prove no dangling pointers
9. **Double-Free Detection**: Prove single deallocation
10. **Undefined Behavior Detection**: Prove all operations are safe

---

## 3. Test Infrastructure

### 3.1 Test Organization

```
Testing/
├── Automatic/          # CTGC tests (9 categories)
│   ├── Allocation/     # 1 test file
│   ├── Async/
│   ├── Deallocation/
│   ├── Escape/
│   ├── LINQ/
│   ├── Lifetime/
│   ├── Regions/
│   └── Safety/
├── Manual/             # Manual memory tests
│   ├── Ownership/
│   ├── Symbolic/
│   ├── Verification/
│   └── Isolation/
├── Language/           # C# language tests
│   ├── Basics/
│   ├── Generics/
│   ├── Async/
│   ├── LINQ/
│   ├── Pattern/
│   └── Records/
├── Integration/        # End-to-end tests
│   ├── SimplePrograms/
│   ├── RealWorld/
│   └── Performance/
├── WASM/               # WASM output tests
│   ├── Correctness/
│   ├── Validation/
│   ├── Execution/
│   └── Size/
├── TestRunner.cs       # Test infrastructure (292 lines)
└── README.md
```

### 3.2 Test Runner Infrastructure

**Location**: `/home/runner/work/CRAB/CRAB/Testing/TestRunner.cs`

**Key Components**:

```csharp
namespace CRAB.Testing
{
    /// <summary>
    /// Test result status
    /// </summary>
    public enum TestStatus
    {
        Passed,
        Failed,
        Skipped,
        Error
    }

    /// <summary>
    /// Base class for all CRAB tests
    /// </summary>
    public abstract class CRABTest
    {
        public abstract string TestName { get; }
        public abstract string Category { get; }
        
        public virtual bool Skip => false;
        public virtual string? SkipReason => null;
        
        public abstract TestResult Run();
        
        protected TestResult Success(string? message = null, TimeSpan? duration = null) { ... }
        protected TestResult Failure(string message, TimeSpan? duration = null) { ... }
        protected TestResult Skipped(string reason) { ... }
    }

    /// <summary>
    /// Main test runner for CRAB
    /// </summary>
    public class TestRunner
    {
        public void RegisterTest(CRABTest test) { ... }
        public TestResults RunAll() { ... }
        public TestResults RunCategory(string category) { ... }
    }

    /// <summary>
    /// Assertion utilities for tests
    /// </summary>
    public static class Assert
    {
        public static void IsTrue(bool condition, string? message = null) { ... }
        public static void AreEqual<T>(T expected, T actual, string? message = null) { ... }
        public static void Throws<TException>(Action action, string? message = null) { ... }
        // ... more assertion methods
    }
}
```

### 3.3 Example Test File

**Location**: `/home/runner/work/CRAB/CRAB/Testing/Automatic/Allocation/SimpleAllocation.cs`

```csharp
// Test automatic memory model (CTGC) allocation and deallocation
// CRAB should infer lifetimes and insert deallocations automatically

namespace AutomaticTest
{
    class MemoryTest
    {
        static void TestAllocation()
        {
            // Single allocation - should be freed at end of scope
            var obj1 = new MyClass();
            obj1.DoWork();
            
            // Multiple allocations
            var obj2 = new MyClass();
            var obj3 = new MyClass();
            
            // Nested scopes
            {
                var obj4 = new MyClass();
                obj4.DoWork();
                // obj4 freed here
            }
            
            // obj1, obj2, obj3 freed here
        }
        
        static void TestConditional()
        {
            // Conditional allocations
            bool condition = true;
            
            if (condition)
            {
                var obj = new MyClass();
                obj.DoWork();
                // obj freed here
            }
        }
        
        static void TestLoop()
        {
            // Loop allocations
            for (int i = 0; i < 10; i++)
            {
                var obj = new MyClass();
                obj.DoWork();
                // obj freed at end of each iteration
            }
        }
    }
    
    class MyClass
    {
        private int value;
        
        public void DoWork()
        {
            value = 42;
        }
    }
}
```

**Test Philosophy**: Compilation success = test pass. The compiler must verify correctness during compilation.

---

## 4. Build System

### 4.1 Project Configuration

**Location**: `/home/runner/work/CRAB/CRAB/CRAB.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Exclude test files from compilation -->
    <Compile Remove="Testing/**/*.cs" />
    <None Include="Testing/**/*.cs" />
  </ItemGroup>

</Project>
```

**Key Points**:
- C#-only project (no external dependencies except .NET 10)
- Test files excluded from build (data files, not compiled code)
- Nullable reference types enabled
- Implicit usings enabled

### 4.2 Build Commands

```bash
# Restore dependencies
dotnet restore

# Build the compiler
dotnet build

# Run the CRAB CLI
dotnet run

# Build output
Build succeeded.
    3 Warning(s)   # All from CDTk.cs (pre-existing)
    0 Error(s)
```

### 4.3 Test Script

**Location**: `/home/runner/work/CRAB/CRAB/run_tests.sh`

```bash
#!/bin/bash
# CRAB Test Runner Script

# Run all tests
./run_tests.sh

# Run specific category
./run_tests.sh automatic
./run_tests.sh manual
./run_tests.sh language
./run_tests.sh integration
./run_tests.sh wasm

# Run individual test
dotnet run -- check Testing/Automatic/Lifetime/LifetimeInferenceTests.cs
```

**Test Categories**:
- `automatic` - Automatic memory (CTGC) tests
- `manual` - Manual memory verification tests
- `language` - C# language feature tests
- `integration` - End-to-end program tests
- `wasm` - WebAssembly output tests

---

## 5. Documentation Structure

### 5.1 Documentation Files

```
Documentation/
├── README.md                    # Documentation index
├── Wiki/                        # User documentation
│   ├── CLIReference.md
│   ├── FAQ.md
│   ├── GettingStarted.md
│   ├── Installation.md
│   ├── LanguageSupport.md
│   └── MemoryModels.md
└── Internal/                    # Internal/developer docs
    ├── ArchitectureOverview.md
    ├── AUTOMATIC_MODEL_IMPLEMENTATION_SUMMARY.md
    ├── AUTOMATIC_MODEL_README.md
    ├── IMPLEMENTATION_SUMMARY.md
    ├── MANUAL_MODEL_IMPLEMENTATION_SUMMARY.md
    ├── MANUAL_MODEL_README.md
    ├── MAPSET_IMPLEMENTATION_SUMMARY.md
    ├── MODEL_MAPSET_INTEGRATION.md      # *** KEY FOR OPTIMIZATION MODEL ***
    ├── PROJECT_STATUS.md
    └── TestingGuide.md
```

### 5.2 Key Documentation for Optimization Model

**Must Read**:

1. **MODEL_MAPSET_INTEGRATION.md** (337 lines)
   - How models integrate with MapSet
   - CDTk design patterns
   - Model property syntax
   - Example flows for Automatic and Manual models
   - Complete integration examples

2. **AUTOMATIC_MODEL_README.md**
   - CTGC architecture and phases
   - Lifetime inference patterns
   - Region analysis strategies
   - Safety verification approach
   - Good reference for optimization structure

3. **MAPSET_IMPLEMENTATION_SUMMARY.md**
   - 188 map definitions
   - WASM code generation strategy
   - Template substitution system
   - Helper methods for model integration

---

## 6. CRAB Spec File - Key Requirements

**Location**: `/home/runner/work/CRAB/CRAB/.github/agents/crab-spec.txt`

### 6.1 Core Principles

```
CRAB is a sovereign, zero‑runtime C# to WebAssembly compiler designed to compile 
the entire C# language into pure WASM MVP while guaranteeing mathematically provable 
memory safety.

Key Invariants:
1. 100% memory safe (no use-after-free, double-free, leaks, undefined behavior)
2. Zero runtime overhead (all costs at compile time)
3. 100% C# compatible (C# 1.0 through C# 13)
4. 100% WASM MVP compliant
5. Deterministic output
6. Verifiable
```

### 6.2 Optimization Requirements (Inferred from Spec)

**Memory Safety is Non-Negotiable**:
> "CRAB must guarantee memory safety, including no use‑after‑free, no double‑free, 
> no dangling pointers, no leaks, no buffer overflows, no invalid aliasing, and 
> no undefined behavior."

**Implications for Optimization Model**:

1. **Safety Preservation**: Optimizations MUST NOT compromise memory safety guarantees
   - Cannot reorder operations that affect memory safety
   - Cannot eliminate safety checks
   - Cannot introduce new allocation/deallocation patterns

2. **CTGC Compatibility**: Optimizations must work with CTGC-inserted deallocations
   - Respect lifetime annotations
   - Preserve deallocation point correctness
   - Cannot extend object lifetimes unsafely

3. **Manual Model Isolation**: Optimizations cannot cross manual{} boundaries
   - No automatic code in manual blocks
   - No manual pointers in automatic code
   - Strict model isolation enforcement

4. **Determinism**: Optimizations must maintain deterministic output
   - Same input always produces same output
   - No non-deterministic transformations

5. **Performance Goals**:
   > "CRAB must outperform .NET AOT in runtime speed and must match or exceed 
   > Rust and C++ to WASM performance."
   
   - Dead code elimination
   - Constant folding
   - Inlining (respect safety)
   - Loop optimizations
   - Memory layout optimization

### 6.3 Forbidden Optimizations

Based on the spec, these optimizations are **NOT** allowed:

❌ Runtime garbage collection or reference counting  
❌ Speculative execution that could violate memory safety  
❌ Unsafe pointer arithmetic optimizations  
❌ Cross-model optimizations (automatic ↔ manual)  
❌ Non-deterministic transformations  
❌ Optimizations requiring runtime metadata  

### 6.4 Allowed/Encouraged Optimizations

Based on the spec and architecture:

✅ **Dead Code Elimination**: Remove unreachable code  
✅ **Constant Folding**: Evaluate constants at compile-time  
✅ **Constant Propagation**: Propagate known constant values  
✅ **Inlining**: Inline small functions (respect lifetimes)  
✅ **Common Subexpression Elimination**: Eliminate redundant calculations  
✅ **Loop Invariant Code Motion**: Move invariant code outside loops  
✅ **Strength Reduction**: Replace expensive operations with cheaper ones  
✅ **Tail Call Optimization**: Convert tail recursion to iteration  
✅ **Memory Layout Optimization**: Optimize struct layouts for cache  
✅ **Region Merging**: Merge compatible memory regions (CTGC integration)  
✅ **Escape Analysis Optimization**: Stack-allocate non-escaping objects  
✅ **Algebraic Simplification**: Simplify mathematical expressions  

---

## 7. Implementing a New Optimization Model

### 7.1 Architecture Pattern

Based on Automatic and Manual models:

```csharp
using CDTk;
using System;
using System.Collections.Generic;

/// <summary>
/// Optimization Model - Applies safe optimizations to the AST
/// 
/// Responsibilities:
/// 1. Analyze AST for optimization opportunities
/// 2. Apply transformations that preserve memory safety
/// 3. Integrate with CTGC and Manual model annotations
/// 4. Return optimized AST with metadata
/// 
/// Safety Constraints:
/// - Must preserve all safety guarantees from Automatic/Manual models
/// - Cannot reorder memory operations
/// - Cannot eliminate safety checks
/// - Must maintain deterministic output
/// </summary>
public class Optimization : Model
{
    private readonly __AllRules _rules;
    private readonly __Ast _ast;

    /// <summary>
    /// Constructor for CDTk integration - called from MapSet property
    /// </summary>
    public Optimization(__AllRules rules, __Ast ast)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _ast = ast ?? throw new ArgumentNullException(nameof(ast));
    }

    /// <summary>
    /// Access to grammar rules - can be used for rule-specific analysis
    /// </summary>
    protected __AllRules Rules => _rules;

    /// <summary>
    /// Build optimization analysis and transformation for the input AST
    /// </summary>
    public override object Build(object input)
    {
        // Use the AST from constructor when called from MapSet
        var ast = _ast?.Root ?? input as AstNode;
        if (ast == null)
        {
            throw new InvalidOperationException("Optimization model requires AST input");
        }

        var context = new OptimizationContext();
        
        try
        {
            // Phase 1: Analyze for optimization opportunities
            var opportunities = AnalyzeOptimizations(ast, context);
            
            // Phase 2: Validate safety of transformations
            var safeTransforms = ValidateSafety(opportunities, context);
            
            // Phase 3: Apply optimizations
            var optimizedAst = ApplyOptimizations(ast, safeTransforms, context);
            
            // Phase 4: Return annotated AST with optimization metadata
            var annotations = new OptimizationAnnotations
            {
                OriginalAST = ast,
                OptimizedAST = optimizedAst,
                AppliedOptimizations = safeTransforms,
                IsOptimized = true,
                Diagnostics = context.Diagnostics
            };
            
            return annotations;
        }
        catch (Exception ex)
        {
            context.Diagnostics.Add(new OptimizationDiagnostic(
                OptimizationDiagnosticLevel.Error,
                $"Optimization model failed: {ex.Message}",
                ex
            ));
            throw;
        }
    }
    
    /// <summary>
    /// Phase 1: Analyze AST for optimization opportunities
    /// </summary>
    private List<OptimizationOpportunity> AnalyzeOptimizations(AstNode ast, OptimizationContext context)
    {
        var opportunities = new List<OptimizationOpportunity>();
        
        // Dead code detection
        opportunities.AddRange(FindDeadCode(ast, context));
        
        // Constant folding opportunities
        opportunities.AddRange(FindConstantExpressions(ast, context));
        
        // Inlining candidates
        opportunities.AddRange(FindInlineCandidates(ast, context));
        
        // Common subexpression elimination
        opportunities.AddRange(FindCommonSubexpressions(ast, context));
        
        // Loop optimizations
        opportunities.AddRange(FindLoopOptimizations(ast, context));
        
        return opportunities;
    }
    
    /// <summary>
    /// Phase 2: Validate that transformations preserve memory safety
    /// </summary>
    private List<SafeTransformation> ValidateSafety(
        List<OptimizationOpportunity> opportunities, 
        OptimizationContext context)
    {
        var safeTransforms = new List<SafeTransformation>();
        
        foreach (var opp in opportunities)
        {
            // Check if transformation preserves safety
            if (IsMemorySafe(opp, context) && 
                IsDeterministic(opp, context) &&
                PreservesIsolation(opp, context))
            {
                safeTransforms.Add(new SafeTransformation(opp));
            }
            else
            {
                context.Diagnostics.Add(new OptimizationDiagnostic(
                    OptimizationDiagnosticLevel.Warning,
                    $"Skipped unsafe optimization: {opp.Description}",
                    null
                ));
            }
        }
        
        return safeTransforms;
    }
    
    /// <summary>
    /// Phase 3: Apply validated optimizations to AST
    /// </summary>
    private AstNode ApplyOptimizations(
        AstNode ast, 
        List<SafeTransformation> transforms, 
        OptimizationContext context)
    {
        var optimizedAst = ast; // Start with original
        
        foreach (var transform in transforms)
        {
            optimizedAst = transform.Apply(optimizedAst, context);
        }
        
        return optimizedAst;
    }
    
    // ... helper methods for specific optimizations
    
    private List<OptimizationOpportunity> FindDeadCode(AstNode ast, OptimizationContext context)
    {
        // TODO: Implement dead code detection
        return new List<OptimizationOpportunity>();
    }
    
    private List<OptimizationOpportunity> FindConstantExpressions(AstNode ast, OptimizationContext context)
    {
        // TODO: Implement constant folding detection
        return new List<OptimizationOpportunity>();
    }
    
    private bool IsMemorySafe(OptimizationOpportunity opp, OptimizationContext context)
    {
        // TODO: Verify transformation preserves memory safety
        return true;
    }
    
    private bool IsDeterministic(OptimizationOpportunity opp, OptimizationContext context)
    {
        // TODO: Verify transformation is deterministic
        return true;
    }
    
    private bool PreservesIsolation(OptimizationOpportunity opp, OptimizationContext context)
    {
        // TODO: Verify transformation doesn't cross automatic/manual boundaries
        return true;
    }
}

// ============================================================
// Supporting Data Structures
// ============================================================

class OptimizationContext
{
    public List<OptimizationDiagnostic> Diagnostics { get; } = new List<OptimizationDiagnostic>();
    public bool AggressiveOptimizations { get; set; } = false;
    public int OptimizationLevel { get; set; } = 2; // 0 = none, 1 = basic, 2 = standard, 3 = aggressive
}

class OptimizationDiagnostic
{
    public OptimizationDiagnosticLevel Level { get; }
    public string Message { get; }
    public Exception? Exception { get; }
    
    public OptimizationDiagnostic(OptimizationDiagnosticLevel level, string message, Exception? exception = null)
    {
        Level = level;
        Message = message;
        Exception = exception;
    }
}

enum OptimizationDiagnosticLevel
{
    Info,
    Warning,
    Error
}

class OptimizationOpportunity
{
    public string Id { get; set; } = "";
    public string Description { get; set; } = "";
    public AstNode? Node { get; set; }
    public OptimizationType Type { get; set; }
    public int ExpectedSpeedup { get; set; } // Estimated % improvement
}

enum OptimizationType
{
    DeadCodeElimination,
    ConstantFolding,
    ConstantPropagation,
    Inlining,
    CommonSubexpressionElimination,
    LoopInvariantCodeMotion,
    StrengthReduction,
    TailCallOptimization
}

class SafeTransformation
{
    private OptimizationOpportunity _opportunity;
    
    public SafeTransformation(OptimizationOpportunity opportunity)
    {
        _opportunity = opportunity;
    }
    
    public AstNode Apply(AstNode ast, OptimizationContext context)
    {
        // Apply the transformation to the AST
        // TODO: Implement transformation logic
        return ast;
    }
}

class OptimizationAnnotations
{
    public AstNode? OriginalAST { get; set; }
    public AstNode? OptimizedAST { get; set; }
    public List<SafeTransformation> AppliedOptimizations { get; set; } = new List<SafeTransformation>();
    public bool IsOptimized { get; set; }
    public List<OptimizationDiagnostic> Diagnostics { get; set; } = new List<OptimizationDiagnostic>();
}
```

### 7.2 Integration into MapSet

**Location**: `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs`

Add optimization model property (after line 35):

```csharp
/// <summary>
/// Optimization model for code transformations.
/// Applies safe optimizations that preserve memory safety guarantees.
/// Used by Maps to generate optimized WASM code.
/// </summary>
public Optimization OptimizationModel => new Optimization(__AllRules!, __Ast!);
```

Add helper method for optimization annotations:

```csharp
/// <summary>
/// Get optimization annotations.
/// This is called during WASM generation to apply optimizations.
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
        // If optimization fails, return null - Maps will generate unoptimized WASM
        return null;
    }
}
```

### 7.3 Testing Strategy

Create optimization test files in `/home/runner/work/CRAB/CRAB/Testing/Optimization/`:

```
Testing/Optimization/
├── DeadCode/
│   ├── UnreachableCode.cs
│   ├── UnusedVariables.cs
│   └── ConstantConditions.cs
├── ConstantFolding/
│   ├── ArithmeticConstants.cs
│   ├── StringConstants.cs
│   └── BooleanConstants.cs
├── Inlining/
│   ├── SmallFunctions.cs
│   ├── SingleUse.cs
│   └── SafetyPreservation.cs
├── CommonSubexpression/
│   ├── Arithmetic.cs
│   ├── MethodCalls.cs
│   └── FieldAccess.cs
└── LoopOptimization/
    ├── InvariantCodeMotion.cs
    ├── StrengthReduction.cs
    └── Unrolling.cs
```

Example test:

```csharp
// Testing/Optimization/ConstantFolding/ArithmeticConstants.cs

namespace OptimizationTest
{
    /// <summary>
    /// Test: Constant folding for arithmetic expressions
    /// Expected: Compile-time evaluation of constants
    /// </summary>
    class ConstantFoldingTest
    {
        static int TestArithmetic()
        {
            // Should be folded to: return 42;
            return 10 + 20 + 12;
        }
        
        static int TestComplex()
        {
            // Should be folded to: return 100;
            return (5 * 10) + (3 * 20) - 10;
        }
        
        static bool TestBoolean()
        {
            // Should be folded to: return true;
            return (5 > 3) && (10 < 20);
        }
    }
}
```

---

## 8. Repository File Structure Summary

```
CRAB/
├── .github/
│   └── agents/
│       └── crab-spec.txt              # *** CRAB SPECIFICATION ***
│
├── CLI/                                # Command-line interface
│   ├── Commands/
│   └── Help.cs
│
├── Compiler/
│   ├── Core/
│   │   ├── TokenSet.cs                # Lexical tokens (332 lines)
│   │   ├── RuleSet.cs                 # Grammar rules (882 lines)
│   │   └── MapSet.cs                  # Code generation maps (916 lines)
│   │                                   # *** MODELS PLUG IN HERE ***
│   └── Models/
│       ├── Automatic.cs               # CTGC model (1,127 lines)
│       ├── Manual.cs                  # Manual verification (992 lines)
│       └── Optimization.cs            # Optimization model (9 lines - STUB)
│                                       # *** TARGET FOR IMPLEMENTATION ***
│
├── Dependencies/
│   ├── Boilerplate/
│   │   └── CDTk.cs                    # CDTk framework (~12,000 lines)
│   ├── Documentation/                 # CDTk docs
│   └── README.md                      # CDTk overview
│
├── Documentation/
│   ├── Wiki/                          # User documentation
│   └── Internal/                      # Developer documentation
│       ├── MODEL_MAPSET_INTEGRATION.md  # *** KEY INTEGRATION GUIDE ***
│       ├── AUTOMATIC_MODEL_README.md
│       ├── MAPSET_IMPLEMENTATION_SUMMARY.md
│       └── ... (14 more files)
│
├── Testing/
│   ├── Automatic/                     # CTGC tests
│   │   ├── Allocation/
│   │   │   └── SimpleAllocation.cs    # Example test
│   │   ├── Async/
│   │   ├── Deallocation/
│   │   ├── Escape/
│   │   ├── LINQ/
│   │   ├── Lifetime/
│   │   ├── Regions/
│   │   └── Safety/
│   ├── Manual/                        # Manual memory tests
│   ├── Language/                      # C# language tests
│   ├── Integration/                   # End-to-end tests
│   ├── WASM/                          # WASM output tests
│   ├── TestRunner.cs                  # Test infrastructure
│   └── README.md
│
├── CRAB.csproj                        # Project file
├── CRAB.sln                           # Solution file
├── Program.cs                         # Entry point (101 lines)
├── run_tests.sh                       # Test runner script
├── README.md                          # Project README
├── LICENSE.md
└── SECURITY.md
```

**Total Repository Size**: ~20,000 lines of C# code
- Core Compiler: ~4,258 lines
- Models: ~2,128 lines (Automatic + Manual)
- CDTk Framework: ~12,000 lines
- Tests: ~500 lines (to be expanded)
- CLI: ~300 lines

---

## 9. Key Insights for Optimization Model Implementation

### 9.1 Safety-First Design

1. **Never compromise memory safety** - It's better to skip an optimization than introduce undefined behavior
2. **Verify before transform** - Check safety of each transformation
3. **Respect model isolation** - Don't optimize across automatic/manual boundaries

### 9.2 Integration Points

1. **Model Property in MapSet** - `public Optimization OptimizationModel => new Optimization(__AllRules!, __Ast!);`
2. **Helper Method** - `private OptimizationAnnotations? GetOptimizationAnnotations()`
3. **Map Usage** - Maps call helper to get optimization metadata
4. **AST Annotation** - Return optimized AST with metadata for code generation

### 9.3 CDTk Patterns

1. **Constructor Pattern**: `public Optimization(__AllRules rules, __Ast ast)`
2. **Build Method**: `public override object Build(object input)`
3. **Property Syntax**: `=> new Optimization(__AllRules!, __Ast!)`
4. **Exception Handling**: Try/catch with diagnostics

### 9.4 Testing Approach

1. **Category-Based** - Organize tests by optimization type
2. **Compilation Success** - Test passes if it compiles safely
3. **Output Validation** - Verify optimized WASM is correct
4. **Safety Verification** - Ensure no safety violations

### 9.5 Documentation Requirements

1. **Model README** - Document optimization phases and strategies
2. **Implementation Summary** - Track progress and decisions
3. **Integration Guide** - How optimization integrates with CTGC/Manual
4. **Test Guide** - How to write optimization tests

---

## 10. Quick Reference Commands

```bash
# Build the project
dotnet restore
dotnet build

# Run CRAB CLI
dotnet run

# Run all tests
./run_tests.sh

# Run optimization tests (after implementation)
./run_tests.sh optimization

# Compile a specific test
dotnet run -- check Testing/Optimization/ConstantFolding/ArithmeticConstants.cs

# View WASM output
dotnet run -- compile --emit-text myfile.cs
cat myfile.wat
```

---

## 11. Next Steps for Optimization Model

1. **Study Existing Models**: Read Automatic.cs and Manual.cs thoroughly
2. **Read Integration Guide**: Study MODEL_MAPSET_INTEGRATION.md
3. **Implement Phases**: Start with simple optimizations (constant folding)
4. **Add to MapSet**: Integrate model property and helper method
5. **Write Tests**: Create test files for each optimization type
6. **Document**: Create Optimization README and integration guide
7. **Verify**: Ensure all optimizations preserve safety

---

## Conclusion

CRAB is a well-architected compiler with clear separation of concerns:

- **Frontend** (CDTk): Parsing and AST construction
- **Models**: Semantic analysis and verification (Automatic, Manual, **Optimization**)
- **Backend** (MapSet): Direct AST → WASM translation

The optimization model fits naturally into this pipeline as a MapSet property that analyzes and transforms the AST while preserving the memory safety guarantees established by the Automatic and Manual models.

**The current Optimization.cs is a minimal stub (9 lines) ready for implementation following the patterns established by Automatic.cs (1,127 lines) and Manual.cs (992 lines).**

All infrastructure is in place: build system, test framework, documentation structure, and integration points. The path forward is clear: implement optimization phases, integrate with MapSet, write comprehensive tests, and document the approach.
