# Optimization Test Quick Reference

## Test Files Overview

| File | Size | Tests | Focus |
|------|------|-------|-------|
| DeadCodeElimination.cs | 4.4 KB | 8 | Unreachable code removal |
| ConstantFolding.cs | 5.5 KB | 14 | Compile-time evaluation |
| CommonSubexpression.cs | 6.7 KB | 14 | Redundant computation removal |
| Inlining.cs | 6.9 KB | 18 | Function inlining |
| LoopOptimization.cs | 9.0 KB | 20 | Loop-level optimizations |
| TailCallOptimization.cs | 7.3 KB | 20 | Tail recursion to jumps |
| MemorySafetyPreservation.cs | 11 KB | 20 | **Safety verification** |
| IntegrationTest.cs | 14 KB | 16 | Combined optimizations |

## Running Tests

```bash
# Run all optimization tests
./run_tests.sh optimization

# Run all tests (including optimization)
./run_tests.sh

# Run specific test file
dotnet run -- check Testing/Optimization/DeadCodeElimination.cs

# Run specific category
./run_tests.sh automatic
./run_tests.sh manual
./run_tests.sh language
./run_tests.sh integration
./run_tests.sh wasm
./run_tests.sh optimization
```

## Test Pattern

Each test file follows this structure:

```csharp
// Comment describing the test
namespace OptimizationTest
{
    class TestNameTest
    {
        // Test 1: Description
        static ReturnType TestMethod()
        {
            // Test code
        }
        
        // More tests...
    }
    
    // Helper classes
    class MyClass { }
}
```

## Critical Safety Tests

**MemorySafetyPreservation.cs** is the most important file. It verifies:

1. ✓ CTGC deallocation points preserved
2. ✓ Ownership transfer semantics preserved
3. ✓ Lifetime inference remains correct
4. ✓ Region boundaries respected
5. ✓ Escape analysis preserved
6. ✓ No use-after-free introduced
7. ✓ No double-free introduced
8. ✓ Model isolation preserved
9. ✓ Async memory tracking preserved
10. ✓ All optimizations combined preserve safety

## Test Naming Convention

- **File names**: `[OptimizationType].cs` (e.g., `DeadCodeElimination.cs`)
- **Class names**: `[OptimizationType]Test` (e.g., `DeadCodeEliminationTest`)
- **Method names**: Descriptive of test case (e.g., `UnreachableAfterReturn()`)
- **Namespace**: Always `OptimizationTest`

## What to Test

### For Each Optimization:

1. **Positive Cases**: Where optimization SHOULD apply
   ```csharp
   // Should inline
   static int Add(int a, int b) { return a + b; }
   ```

2. **Negative Cases**: Where optimization should NOT apply
   ```csharp
   // Should NOT inline (recursive)
   static int Factorial(int n) { 
       return n <= 1 ? 1 : n * Factorial(n - 1); 
   }
   ```

3. **Safety Cases**: Verify safety preserved
   ```csharp
   // Must preserve CTGC deallocation
   static void Test() {
       var obj = new MyClass();
       obj.DoWork();
       // Deallocation here must not be eliminated
   }
   ```

4. **Edge Cases**: Boundary conditions
   ```csharp
   // Empty loop
   for (int i = 0; i < 0; i++) { }
   ```

## Adding New Tests

1. Choose appropriate category file or create new one
2. Add test method with descriptive name
3. Add comment explaining what should be optimized
4. Include safety verification if applicable
5. Update README.md if adding new category

## Expected Behavior

Tests should verify:

1. **Compilation**: Code compiles without errors
2. **Optimization**: Appropriate optimization is applied
3. **Safety**: Memory safety invariants preserved
4. **Correctness**: Optimized code has same behavior as original

## Integration with Compiler

Tests are designed to work with:

```
Input: C# source code (test file)
  ↓
Parser: Parse to AST
  ↓
Semantic Analysis: Type checking, ownership analysis
  ↓
Optimization Model: Apply optimizations ← THESE TESTS
  ↓
WASM Generation: Emit optimized WASM
  ↓
Output: Optimized WASM binary
```

## Performance Expectations

Optimizations should achieve:

- **DCE**: Remove all unreachable code (100% elimination)
- **Constant Folding**: Evaluate all constant expressions (100% folding)
- **CSE**: Eliminate 80%+ redundant computations
- **Inlining**: Inline 90%+ of small pure functions
- **LICM**: Hoist 95%+ loop invariants
- **Tail Call**: Convert 100% of tail recursive calls
- **Overall**: 10-30% performance improvement

## Debugging Failed Tests

If a test fails:

1. Check compiler error messages
2. Verify optimization was applied (check generated WASM)
3. Verify safety properties (check CTGC deallocation points)
4. Check for ownership violations
5. Verify deterministic behavior
6. Check exception handling
7. Verify async safety if applicable

## Common Issues

### Issue: Optimization not applied
- Check if safety conditions are met
- Verify function is pure (for inlining/CSE)
- Check if code has side effects
- Verify ownership constraints

### Issue: Safety violation
- Check CTGC deallocation points
- Verify ownership transfer
- Check lifetime inference
- Verify region boundaries

### Issue: Incorrect optimization
- Verify constant folding correctness
- Check CSE invalidation by reassignment
- Verify inlining preserves semantics
- Check loop optimization correctness

## Resources

- **Full Documentation**: `/Testing/Optimization/README.md`
- **Implementation Summary**: `/Testing/Optimization/IMPLEMENTATION_SUMMARY.md`
- **Optimization Model**: `/Compiler/Models/Optimization.cs`
- **Quick Start**: `/OPTIMIZATION_MODEL_QUICK_START.md`
- **CRAB Spec**: `/crab-spec.txt`

## Quick Stats

- **Total Tests**: ~130 test methods
- **Total Scenarios**: ~250+ test scenarios
- **Total Code**: 2,344 lines
- **Categories**: 8 major categories
- **Safety Tests**: 20 dedicated safety tests
- **Integration Tests**: 16 combined optimization tests

## Contact

For questions about optimization tests:
1. Read `/Testing/Optimization/README.md`
2. Check `/Compiler/Models/Optimization.cs`
3. Review CRAB specification
4. Refer to this quick reference
