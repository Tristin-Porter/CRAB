# Model-MapSet Integration in CRAB

## Overview

Per CDTk design pattern, semantic analysis Models are integrated directly into the MapSet class as properties. This document explains how CRAB's Automatic and Manual memory models are used by the MapSet to generate WASM with proper memory management.

## CDTk Design Pattern

According to CDTk documentation, Models should be:
1. Properties of the MapSet class (not standalone)
2. Instantiated using `=>` syntax with `__AllRules` and `__Ast` parameters
3. Called during code generation to provide semantic information

```csharp
class Maps : MapSet
{
    // Model property
    public MyModel Model => new MyModel(__AllRules!, __Ast!);
    
    // Maps can use model results
    public Map SomeExpression = "..."; // Can call Model.Build(...) if needed
}
```

## CRAB Implementation

### MapSet Properties

In `Compiler/Core/MapSet.cs`:

```csharp
class WASM : MapSet
{
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
}
```

### Helper Methods

The MapSet includes helper methods that demonstrate how to call the models:

```csharp
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
        return null; // Maps will generate basic WASM
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
        return null; // Compilation will fail with diagnostics
    }
}
```

## How Models are Used

### Automatic Model (CTGC)

The AutomaticModel performs compile-time garbage collection:

1. **Allocation Tracking**: When Maps encounter `new` expressions, they generate WASM allocation instructions
2. **Lifetime Analysis**: AutomaticModel.Build() analyzes the entire AST to compute lifetimes
3. **Deallocation Insertion**: Model returns deallocation points, Maps insert free instructions

Example flow:

```csharp
// C# code
void Example() {
    var obj = new MyClass();
    obj.DoWork();
    // Deallocation here
}

// Map generates WASM
MethodDeclaration: AutomaticModel analyzes method
  → Tracks allocation at "new MyClass()"
  → Computes last use at "obj.DoWork()"
  → Returns deallocation point
  → Map inserts memory free instruction

// Result WASM
(func $Example
  (local $obj (ref $MyClass))
  (struct.new $MyClass)          ;; allocation
  (local.set $obj)
  (call $MyClass.DoWork $obj)
  ;; AutomaticModel determined deallocation point:
  (call $free (local.get $obj))  ;; inserted by Map
)
```

### Maps That Use AutomaticModel

1. **MethodDeclaration**:
   ```csharp
   /// AutomaticModel.Build() analyzes the entire method body to:
   /// 1. Track all allocations in the method
   /// 2. Infer lifetimes of all values
   /// 3. Compute optimal deallocation points
   /// 4. Insert deallocation instructions in the generated WASM
   public Map MethodDeclaration = @"(func ${name}
     (param {parameters})
     (result {returnType})
     ;; Method body with CTGC-inserted deallocations
   {body}
   )";
   ```

2. **Block**:
   ```csharp
   /// AutomaticModel.Build() computes deallocation points for allocations in this block.
   /// Deallocations are inserted at the end of the block or at last use points.
   public Map Block = @"(block
   {stmts}
     ;; Deallocation instructions inserted here based on AutomaticModel analysis
   )";
   ```

3. **ObjectCreationExpression**:
   ```csharp
   /// AutomaticModel.Build() provides deallocation point information for this allocation.
   public Map ObjectCreationExpression = @";; new {type}() - allocation site tracked by CTGC
   (struct.new ${type}
   {args}
   )";
   ```

### Manual Model Verification

The ManualModel verifies manual{} blocks:

1. **Block Detection**: Maps identify manual{} or unsafe{} blocks
2. **Verification**: ManualModel.Build() performs verification
3. **Proof Generation**: Model proves safety or fails compilation

Example flow:

```csharp
// C# code
manual {
    IntPtr buffer = Marshal.AllocHGlobal(1024);
    ProcessData(buffer);
    Marshal.FreeHGlobal(buffer);
}

// Map generates WASM
ManualStatement: ManualModel verifies block
  → Builds ownership graph
  → Performs abstract interpretation
  → Proves: allocated once, freed once, no escapes
  → Returns verification result
  → Map generates verified WASM

// Result WASM
(block $manual
  ;; Verified manual memory management block
  ;; ManualModel proved: no leaks, no use-after-free, no undefined behavior
  (local $buffer i32)
  (call $AllocHGlobal (i32.const 1024))
  (local.set $buffer)
  (call $ProcessData (local.get $buffer))
  (call $FreeHGlobal (local.get $buffer))
)
```

### Maps That Use ManualModel

1. **ManualStatement**:
   ```csharp
   /// ManualModel.Build() performs:
   /// 1. Ownership graph construction for all pointers
   /// 2. Abstract interpretation of all paths
   /// 3. Symbolic execution for verification
   /// 4. Proof that no undefined behavior can occur
   /// Compilation fails if verification fails.
   public Map ManualStatement = @"(block $manual
     ;; Verified manual memory management block
     ;; ManualModel proved: no leaks, no use-after-free, no undefined behavior
   {body}
   )";
   ```

2. **UnsafeStatement**:
   ```csharp
   /// ManualModel.Build() verifies that all pointer operations in this block are safe.
   /// No unsafe code is allowed without verification proof.
   public Map UnsafeStatement = @"(block $unsafe
     ;; WARNING: Use 'manual' keyword instead of 'unsafe'
     ;; ManualModel verifies all pointer operations
   {body}
   )";
   ```

3. **FixedStatement**:
   ```csharp
   /// Verified by ManualModel to ensure pointer doesn't escape the block.
   public Map FixedStatement = @"(block $fixed
     ;; fixed ({declaration}) - pointer pinned in scope
     ;; ManualModel verifies pointer doesn't escape
   {declaration}
   {body}
     ;; pointer unpinned here
   )";
   ```

## Integration Flow

```
1. CDTk parses C# source → AST
   ↓
2. MapSet is instantiated
   - __AllRules and __Ast are set by CDTk
   - Model properties become available
   ↓
3. Code generation begins
   ↓
4. For each AST node being mapped:
   
   a) Automatic Memory:
      - Map encounters allocation (new, array, etc.)
      - Map calls GetAutomaticAnnotations()
      - AutomaticModel.Build(__Ast.Root) analyzes entire AST
      - Returns: allocations list + deallocation points
      - Map inserts allocation WASM + deallocation WASM
   
   b) Manual Memory:
      - Map encounters manual{} or unsafe{} block
      - Map calls GetManualAnnotations()
      - ManualModel.Build(__Ast.Root) verifies block
      - Returns: verification result (pass/fail)
      - Map generates WASM if verified, errors if not
   ↓
5. WASM output generated with memory management
```

## Benefits of This Integration

1. **Single Source of Truth**: AST is analyzed once, results cached
2. **Clean Separation**: Models handle analysis, Maps handle generation
3. **Verification First**: Manual blocks must verify before code gen
4. **Optimal Placement**: CTGC computes best deallocation points
5. **Type Safety**: CDTk ensures correct data flow
6. **Declarative**: Maps declare what to generate, models provide how

## Example: Complete Flow

```csharp
// Input C# code
class Example
{
    void Process()
    {
        var list = new List<int>();
        list.Add(42);
        Console.WriteLine(list[0]);
        // list deallocated here by CTGC
    }
}

// MapSet processing:

1. CompilationUnit map generates module structure
   
2. ClassDeclaration map generates struct type
   
3. MethodDeclaration map generates function:
   - Calls AutomaticModel.Build(__Ast.Root)
   - AutomaticModel analyzes method:
     * Allocation: "new List<int>()" at line 4
     * Last use: list[0] access at line 6
     * Deallocation point: end of method
   - Returns AutomaticAnnotations with deallocation info
   
4. ObjectCreationExpression map generates:
   (struct.new $List_int)  ;; allocation tracked
   
5. Block map at method end generates:
   ;; Deallocation inserted here:
   (call $free (local.get $list))
   
6. Result WASM:
   (func $Process
     (local $list (ref $List_int))
     (struct.new $List_int)           ;; new List<int>()
     (local.set $list)
     (call $List_Add $list (i32.const 42))
     (call $Console_WriteLine 
       (call $List_get_Item $list (i32.const 0)))  ;; list[0] access
     (call $free (local.get $list))   ;; CTGC-inserted deallocation
   )
```

## Summary

CRAB's Models are not separate analysis passes - they are integral parts of the MapSet that are consulted during WASM generation. The AutomaticModel computes where to insert memory management, and the ManualModel verifies safety of manual blocks. Maps use these results to generate correct, safe, and efficient WASM code.

This is the CDTk design pattern in action: Models provide semantic intelligence, Maps provide syntactic structure, and together they produce verified WebAssembly output.
