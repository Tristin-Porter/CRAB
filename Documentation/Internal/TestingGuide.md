# CRAB Testing Guide

## Overview

This guide explains how to run, write, and contribute tests for the CRAB compiler.

## Test Organization

The CRAB test suite is organized into five main categories:

```
Testing/
├── Automatic/          # CTGC tests
│   ├── Lifetime/
│   ├── Regions/
│   ├── Deallocation/
│   ├── Safety/
│   ├── Escape/
│   ├── Async/
│   └── LINQ/
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
└── WASM/               # WASM output tests
    ├── Correctness/
    ├── Validation/
    ├── Execution/
    └── Size/
```

## Running Tests

### Run All Tests

```bash
# From CRAB root directory
./run_tests.sh
```

### Run Specific Test Category

```bash
# Run automatic memory tests
./run_tests.sh automatic

# Run manual memory tests
./run_tests.sh manual

# Run language feature tests
./run_tests.sh language

# Run integration tests
./run_tests.sh integration

# Run WASM tests
./run_tests.sh wasm
```

### Run Individual Test File

```bash
# Compile and verify a specific test
dotnet run -- check Testing/Automatic/Lifetime/LifetimeInferenceTests.cs
```

## Test Infrastructure

### Test Runner

CRAB includes a custom test runner (`Testing/TestRunner.cs`) that provides:

- Test discovery and execution
- Result reporting
- Timing information
- Assertion utilities

### Using the Test Runner

```csharp
using CRAB.Testing;

// Create a test
class MyTest : CRABTest
{
    public override string TestName => "My Test";
    public override string Category => "Automatic";
    
    public override TestResult Run()
    {
        try
        {
            // Test code here
            Assert.AreEqual(42, GetValue());
            return Success("Test passed");
        }
        catch (AssertionException ex)
        {
            return Failure(ex.Message);
        }
    }
}

// Run tests
var runner = new TestRunner();
runner.RegisterTest(new MyTest());
var results = runner.RunAll();
results.Print();
```

### Assertion Methods

```csharp
// Boolean assertions
Assert.IsTrue(condition, "message");
Assert.IsFalse(condition, "message");

// Equality assertions
Assert.AreEqual(expected, actual, "message");
Assert.AreNotEqual(notExpected, actual, "message");

// Null assertions
Assert.IsNull(value, "message");
Assert.IsNotNull(value, "message");

// Exception assertions
Assert.Throws<InvalidOperationException>(() => {
    // Code that should throw
}, "message");

// String assertions
Assert.Contains(text, substring, "message");
```

## Writing Tests

### Test Structure

Each test should follow this structure:

```csharp
/// <summary>
/// Test: [Brief description]
/// Expected: [Expected behavior]
/// </summary>
class TestName
{
    static void TestMethod()
    {
        // Arrange: Set up test data
        var input = CreateTestData();
        
        // Act: Execute code under test
        var result = ProcessData(input);
        
        // Assert: Verify results (done by compiler)
        // For CRAB tests, compilation success = test pass
    }
}
```

### Automatic Memory Model Tests

Test CTGC functionality:

```csharp
namespace CRAB.Testing.Automatic.Lifetime
{
    /// <summary>
    /// Test: Object lifetime inferred correctly
    /// Expected: Object freed at scope exit
    /// </summary>
    class SimpleLifetimeTest
    {
        static void TestLifetime()
        {
            var obj = new TestClass();
            obj.DoWork();
            // CTGC should free obj here
        }
    }
}
```

**Verification:** Compile the test. If compilation succeeds, CTGC analysis passed.

### Manual Memory Model Tests

Test manual memory verification:

```csharp
namespace CRAB.Testing.Manual.Ownership
{
    /// <summary>
    /// Test: Ownership transferred correctly
    /// Expected: No double-free, no leak
    /// </summary>
    class OwnershipTransferTest
    {
        static void TestTransfer()
        {
            manual
            {
                IntPtr buffer = Marshal.AllocHGlobal(1024);
                
                // Transfer ownership
                IntPtr owner = buffer;
                
                // Free via owner
                Marshal.FreeHGlobal(owner);
            }
            // ✓ Verified: No double-free
        }
    }
}
```

**Verification:** Compiler verifies safety properties. Compilation success = verification passed.

### Language Feature Tests

Test C# language support:

```csharp
namespace CRAB.Testing.Language.Generics
{
    /// <summary>
    /// Test: Generic class compilation
    /// Expected: Generic instantiation works
    /// </summary>
    class GenericClassTest
    {
        class Container<T>
        {
            private T value;
            public T GetValue() => value;
        }
        
        static void TestGeneric()
        {
            var intContainer = new Container<int>();
            var stringContainer = new Container<string>();
        }
    }
}
```

**Verification:** Successful compilation and correct code generation.

### Integration Tests

Test complete programs:

```csharp
namespace CRAB.Testing.Integration.RealWorld
{
    /// <summary>
    /// Test: Complete calculator program
    /// Expected: All operations work correctly
    /// </summary>
    class CalculatorTest
    {
        class Calculator
        {
            public int Add(int a, int b) => a + b;
            public int Subtract(int a, int b) => a - b;
        }
        
        static void TestCalculator()
        {
            var calc = new Calculator();
            int sum = calc.Add(10, 20);     // 30
            int diff = calc.Subtract(30, 15); // 15
        }
    }
}
```

**Verification:** Program compiles and generated WASM is correct.

### WASM Tests

Test WASM output correctness:

```csharp
namespace CRAB.Testing.WASM.Correctness
{
    /// <summary>
    /// Test: Function compiles to correct WASM
    /// Expected: Valid WASM function signature
    /// </summary>
    class FunctionTest
    {
        static int Add(int a, int b)
        {
            return a + b;
        }
        
        // Expected WASM:
        // (func $Add (param i32 i32) (result i32)
        //   local.get 0
        //   local.get 1
        //   i32.add
        // )
    }
}
```

**Verification:** Inspect generated WASM (`--emit-text`) and verify correctness.

## Test Coverage

### Current Coverage

- ✅ **Automatic Memory Model**: Comprehensive CTGC tests
- ✅ **Manual Memory Model**: Ownership and verification tests
- ✅ **Language Features**: Basic C#, generics tests
- ✅ **Integration**: Data structures and real-world examples
- ✅ **WASM Output**: Correctness and validation tests

### Coverage Goals

- **Statement Coverage**: > 90%
- **Branch Coverage**: > 85%
- **Feature Coverage**: 100% of supported features
- **Safety Coverage**: 100% of safety properties

## Continuous Integration

### CI Pipeline

```yaml
# .github/workflows/test.yml
name: CRAB Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: '10.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --configuration Release
      - name: Run tests
        run: ./run_tests.sh
```

## Performance Testing

### Performance Benchmarks

```csharp
namespace CRAB.Testing.Integration.Performance
{
    class PerformanceBenchmark
    {
        static void BenchmarkAllocation()
        {
            var start = DateTime.Now;
            
            for (int i = 0; i < 1000000; i++)
            {
                var obj = new TestClass();
                obj.DoWork();
                // CTGC should optimize this
            }
            
            var duration = DateTime.Now - start;
            // Verify: duration < threshold
        }
    }
}
```

### Running Benchmarks

```bash
# Run performance tests
./run_tests.sh performance

# Profile with WASM runtime
wasmtime --profile performance.wasm
```

## Debugging Tests

### Verbose Output

```bash
# Enable verbose compilation
dotnet run -- compile --verbose Testing/Test.cs
```

### Emit IR

```bash
# Emit intermediate representation
dotnet run -- compile --emit-ir Testing/Test.cs
cat Test.ir
```

### Emit WAT

```bash
# Emit WASM text format
dotnet run -- compile --emit-text Testing/Test.cs
cat Test.wat
```

## Adding New Tests

### Step 1: Choose Category

Determine which category your test belongs to:
- **Automatic**: Tests CTGC functionality
- **Manual**: Tests manual memory verification
- **Language**: Tests C# language feature
- **Integration**: Tests complete programs
- **WASM**: Tests WASM output

### Step 2: Create Test File

```bash
# Create test file in appropriate directory
touch Testing/Automatic/Lifetime/MyNewTest.cs
```

### Step 3: Write Test

Follow the test structure guidelines above.

### Step 4: Verify Test

```bash
# Compile and check test
dotnet run -- check Testing/Automatic/Lifetime/MyNewTest.cs
```

### Step 5: Register Test

Update test runner or discovery mechanism.

### Step 6: Document Test

Add comments explaining:
- What is being tested
- Expected behavior
- How verification works

## Test Best Practices

### 1. Test One Thing

Each test should verify a single feature or property.

❌ **Bad:**
```csharp
static void TestEverything()
{
    TestLifetime();
    TestOwnership();
    TestGenerics();
    // Too much in one test
}
```

✅ **Good:**
```csharp
static void TestLifetime() { /* ... */ }
static void TestOwnership() { /* ... */ }
static void TestGenerics() { /* ... */ }
```

### 2. Use Descriptive Names

Test names should describe what is being tested.

❌ **Bad:** `Test1`, `Test2`, `MyTest`

✅ **Good:** `SingleAllocationLifetime`, `OwnershipTransfer`, `GenericClassInstantiation`

### 3. Document Expected Behavior

Always include comments explaining expected behavior.

```csharp
/// <summary>
/// Test: Object allocated in loop freed each iteration
/// Expected: No memory leaks, 10 allocations, 10 frees
/// </summary>
```

### 4. Test Edge Cases

Include tests for:
- Empty inputs
- Null values
- Boundary conditions
- Error conditions

### 5. Keep Tests Independent

Tests should not depend on each other or shared state.

## Troubleshooting Tests

### Test Fails to Compile

1. Check error message
2. Verify syntax
3. Ensure feature is supported
4. Check memory model isolation

### Test Compiles but Incorrect Behavior

1. Emit IR and inspect
2. Emit WAT and verify WASM
3. Run with verbose output
4. Compare with expected behavior

### Performance Test Fails

1. Check optimization level
2. Profile with WASM tools
3. Verify test assumptions
4. Compare with baseline

## Contributing Tests

We welcome test contributions! To contribute:

1. Fork the repository
2. Create a test branch
3. Write comprehensive tests
4. Ensure all tests pass
5. Submit a pull request

See [ContributingGuide.md](ContributingGuide.md) for more details.

## See Also

- [Architecture Overview](ArchitectureOverview.md) - CRAB architecture
- [Compiler Pipeline](CompilerPipeline.md) - Compilation stages
- [Contributing Guide](ContributingGuide.md) - How to contribute
- [Test Results](../../Testing/TEST_RESULTS.md) - Latest test results
