# CRAB Testing Suite

This directory contains comprehensive tests for the CRAB compiler.

## Test Organization

### 1. Automatic/ - Automatic Memory Model Tests
Tests for Compile-Time Garbage Collection (CTGC):
- **Lifetime**: Lifetime inference tests
- **Regions**: Region analysis tests
- **Allocation**: Allocation tracking tests
- **Deallocation**: Deallocation point computation tests
- **Safety**: Memory safety verification tests
- **Escape**: Escape analysis tests
- **Async**: Async/await pattern tests
- **LINQ**: LINQ query optimization tests

### 2. Manual/ - Manual Memory Model Tests
Tests for manual memory verification:
- **Ownership**: Ownership graph construction tests
- **Symbolic**: Symbolic execution tests
- **Verification**: Safety property verification tests
- **Isolation**: Model isolation enforcement tests
- **Pointers**: Pointer operation tests

### 3. Language/ - C# Language Feature Tests
Tests for C# language support:
- **Basics**: Basic language features (classes, methods, etc.)
- **Generics**: Generic type tests
- **Async**: Async/await compilation tests
- **LINQ**: LINQ query compilation tests
- **Pattern**: Pattern matching tests
- **Records**: Record type tests
- **Modern**: C# 7-13 feature tests

### 4. Integration/ - Integration Tests
End-to-end compilation tests:
- **SimplePrograms**: Simple complete programs
- **RealWorld**: Real-world code examples
- **Performance**: Performance benchmarks
- **Compatibility**: .NET compatibility tests

### 5. WASM/ - WebAssembly Output Tests
WASM generation and validation tests:
- **Correctness**: WASM output correctness
- **Validation**: WASM MVP compliance
- **Execution**: WASM execution tests
- **Size**: Output size tests

## Running Tests

```bash
# Run all tests
./run_tests.sh

# Run specific category
./run_tests.sh automatic
./run_tests.sh manual
./run_tests.sh language
./run_tests.sh integration
./run_tests.sh wasm

# Test individual file
dotnet run -- check Testing/Automatic/Lifetime/LifetimeInferenceTests.cs
```

## Test Guidelines

1. **Comprehensive Coverage**: Every compiler feature must have tests
2. **Safety Focus**: Emphasize memory safety verification
3. **Positive & Negative**: Test both valid and invalid inputs
4. **Performance**: Include performance regression tests
5. **Documentation**: Each test should be self-documenting

## Test Status

- ✅ Frontend (Tokens, Rules, MapSet): Tested via existing summaries
- ✅ Automatic Memory Model: Comprehensive tests created
  - ✅ Lifetime inference
  - ✅ Region analysis
  - ✅ Deallocation computation
  - ✅ Safety verification
  - ✅ Escape analysis
  - ✅ Async/await memory management
  - ✅ LINQ optimization
- ✅ Manual Memory Model: Comprehensive tests created
  - ✅ Ownership graph construction
  - ✅ Symbolic execution
  - ✅ Safety property verification
  - ✅ Model isolation
  - ✅ Pointer operations
- ✅ Language Features: Comprehensive tests created
  - ✅ Basic language features
  - ✅ Generics
  - ✅ Modern C# features
- ✅ Integration: Real-world tests created
  - ✅ Complete programs
  - ✅ Data structures
- ✅ WASM Output: Validation tests created
  - ✅ Correctness tests
  - ✅ MVP compliance tests

## Example Test Structure

```csharp
[TestClass]
public class LifetimeInferenceTests
{
    [TestMethod]
    public void SimpleAllocation_InfersCorrectLifetime()
    {
        var code = @"
            void Test() {
                var obj = new MyClass();
                obj.DoWork();
            }";
        
        var result = CompileAndAnalyze(code);
        
        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.Allocations.Count);
        Assert.IsNotNull(result.Allocations[0].Lifetime);
    }
}
```

## Contributing Tests

When adding new compiler features:
1. Add corresponding tests in appropriate category
2. Ensure tests cover edge cases
3. Update this README with test descriptions
4. Run full test suite before committing
