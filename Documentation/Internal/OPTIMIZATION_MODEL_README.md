# CRAB Optimization Tests

This directory contains comprehensive tests for the CRAB Optimization model, which applies memory-safe code transformations to improve performance while maintaining 100% safety guarantees.

## Test Categories

### 1. DeadCodeElimination.cs
Tests for removing unreachable code while preserving side effects and memory safety:
- Unreachable code after return statements
- Unreachable branches with constant conditions
- Dead loops with constant false conditions
- Unused variables
- Dead code with side effects (must be preserved)
- Complex control flow with dead code
- Empty blocks elimination

**Key Safety Property**: CTGC deallocation points must never be eliminated, even in dead code paths.

### 2. ConstantFolding.cs
Tests for evaluating constant expressions at compile-time:
- Arithmetic constant folding (addition, subtraction, multiplication, division, modulo)
- Complex nested expressions
- Boolean constant folding
- Comparison folding
- Constant propagation
- String concatenation folding
- Type casting with constants
- Conditional folding with constant conditions
- Loop unrolling with constant iterations
- Bitwise operations
- Algebraic simplification (x * 1 → x, x + 0 → x, etc.)
- Strength reduction (x * 2 → x << 1)

**Key Safety Property**: Constant folding must preserve memory allocation sizes and CTGC behavior.

### 3. CommonSubexpression.cs
Tests for eliminating redundant computations:
- Simple CSE - same expression computed twice
- CSE in different scopes
- CSE with method calls (must be pure)
- CSE with array access
- CSE with field access
- CSE across basic blocks
- Complex expressions with common subexpressions
- Loop invariant code motion (LICM)
- CSE with side effects (must NOT be eliminated)
- CSE invalidation by reassignment
- CSE with ownership transfer

**Key Safety Property**: CSE must not eliminate computations with side effects or violate ownership semantics.

### 4. Inlining.cs
Tests for function inlining while preserving lifetime constraints:
- Simple pure function inlining
- Multiple calls to same function
- Nested function calls
- Inlining with local variables
- Inlining with memory allocation (CTGC preservation)
- Inlining with conditionals
- DO NOT inline recursive functions
- DO NOT inline large functions
- Inlining with loops
- Inlining with multiple return points
- Inlining with ownership transfer
- Getter/setter inlining
- Generic function inlining
- Cross-boundary inlining (automatic/manual model)
- Inlining with exception handling
- Inlining with ref/out parameters

**Key Safety Property**: Inlining must preserve ownership transfer semantics and lifetime constraints.

### 5. LoopOptimization.cs
Tests for loop-level optimizations:
- Loop Invariant Code Motion (LICM) - simple and nested
- Strength reduction (multiplication to addition)
- Loop unrolling with small constant iterations
- Loop fusion (combining adjacent loops)
- Loop fission (splitting complex loops)
- Induction variable optimization
- Array access optimization
- Bounds check elimination
- While/do-while loop optimization
- Loops with side effects (careful optimization)
- Loops with break/continue
- Loop vectorization candidates
- Memory dependency analysis
- Loop interchange for cache efficiency

**Key Safety Property**: Loop optimizations must preserve per-iteration allocations and CTGC semantics.

### 6. TailCallOptimization.cs
Tests for converting tail calls to jumps to avoid stack overflow:
- Simple tail recursion
- Factorial with tail recursion
- Fibonacci with tail recursion (accumulator pattern)
- Sum list with tail recursion
- Mutual tail recursion (even/odd)
- NOT tail calls - result modified after call
- Tail calls with multiple parameters
- Conditional tail calls
- Tail calls with memory allocation
- Tail calls with ownership transfer
- Loop-to-tail-recursion conversion
- Tail calls with exception handling
- Tail calls with generic types
- State machine from tail recursion
- Continuation-passing style

**Key Safety Property**: Tail call optimization must preserve ownership transfer and CTGC deallocation.

### 7. MemorySafetyPreservation.cs
**CRITICAL TESTS** ensuring all optimizations preserve memory safety:
- CTGC deallocation points must not be optimized away
- Ownership transfer semantics preservation
- Lifetime inference correctness
- Region boundary preservation
- Escape analysis preservation
- Use-after-free must never be introduced
- Double-free must never be introduced
- Model isolation (automatic ↔ manual) preservation
- Async memory tracking preservation
- LINQ optimizations with memory safety
- Exception handling cleanup preservation
- Inlining lifetime constraint preservation
- Loop allocation preservation
- CSE ownership preservation
- Dead code elimination with deallocation preservation
- Side effect preservation
- Tail call ownership preservation
- Vectorization memory semantics
- Strength reduction semantics preservation
- Combined optimizations safety

**Key Safety Property**: This is the most important test file. ALL optimizations must pass these safety tests.

### 8. IntegrationTest.cs
End-to-end tests combining multiple optimizations:
- Real-world computation with all optimizations
- Recursive computation with tail call and inlining
- Data structure operations with memory safety
- Nested loops with multiple optimizations
- Exception handling with optimizations
- Conditional branches with optimizations
- Array operations with optimizations
- Object-oriented code with optimizations
- State machine with tail calls
- Pipeline of transformations
- Memory-intensive with safety preservation
- Generic methods with optimizations
- Functional-style code with optimizations
- Complex control flow with optimizations
- Real-world algorithms (Fibonacci with memoization)
- All optimizations combined in one function

**Key Safety Property**: When multiple optimizations interact, memory safety must still be preserved.

## Running the Tests

### Run all optimization tests:
```bash
./run_tests.sh optimization
```

### Run all tests including optimization:
```bash
./run_tests.sh
```

### Run specific test file:
```bash
dotnet run -- check Testing/Optimization/DeadCodeElimination.cs
```

## Expected Behavior

Each test file contains C# code that should:

1. **Compile successfully** - The CRAB compiler should parse and compile the code
2. **Apply optimizations** - The Optimization model should identify and apply appropriate optimizations
3. **Preserve safety** - All CTGC guarantees, ownership semantics, and lifetime constraints must be preserved
4. **Generate efficient WASM** - The output WASM should be optimized while maintaining correctness

## Safety Invariants

The Optimization model MUST preserve these invariants:

1. **No Use-After-Free**: Objects must not be accessed after deallocation
2. **No Double-Free**: Objects must not be deallocated more than once
3. **Ownership Semantics**: Move semantics and ownership transfer must be preserved
4. **Lifetime Correctness**: Object lifetimes must be correctly inferred and respected
5. **Region Boundaries**: Region-based memory management boundaries must be maintained
6. **Model Isolation**: Automatic and manual memory model boundaries must not be violated
7. **Side Effect Preservation**: Observable side effects must not be eliminated
8. **Deterministic Behavior**: Optimizations must preserve deterministic execution
9. **Exception Safety**: Exception handling and cleanup must work correctly
10. **Async Safety**: Async memory tracking must remain correct

## Implementation Notes

- Tests are written in C# (the only language CRAB supports)
- Each test file focuses on a specific optimization category
- Tests include both positive cases (optimization should apply) and negative cases (optimization should NOT apply)
- MemorySafetyPreservation.cs is the most critical - it verifies all optimizations preserve safety
- IntegrationTest.cs verifies that multiple optimizations work correctly together

## Extending the Tests

To add new optimization tests:

1. Add test cases to the appropriate category file, or
2. Create a new test file for a new optimization category
3. Follow the existing pattern: namespace OptimizationTest, class with descriptive name
4. Include comments explaining what optimization should occur
5. Add negative test cases (scenarios where optimization should NOT apply)
6. Ensure safety properties are verified

## Relationship to CRAB Specification

These tests verify the Optimization model as specified in the CRAB architecture:

- **Section**: Optimization Model
- **Guarantees**: Memory-safe code transformations
- **Properties**: Preserves CTGC, ownership, and deterministic behavior
- **Constraints**: Never weakens memory safety guarantees

## Performance Goals

Optimizations should achieve:

- **Dead Code Elimination**: Remove all provably unreachable code
- **Constant Folding**: Evaluate all constant expressions at compile-time
- **CSE**: Eliminate redundant computations within basic blocks and across blocks
- **Inlining**: Inline small pure functions (< 50 instructions)
- **LICM**: Hoist loop invariants in all analyzable loops
- **Tail Call**: Convert all tail recursive calls to jumps
- **Overall**: 10-30% performance improvement on typical programs

## Test Status

- ✓ Test infrastructure complete
- ✓ All 8 test categories implemented
- ✓ Safety property tests comprehensive
- ✓ Integration tests cover real-world scenarios
- ⏳ Awaiting Optimization model implementation
- ⏳ Awaiting compiler integration

## Contact

For questions about optimization tests, refer to:
- `/Compiler/Models/Optimization.cs` - Optimization model implementation
- `OPTIMIZATION_MODEL_QUICK_START.md` - Quick start guide
- CRAB specification - Complete architecture document
