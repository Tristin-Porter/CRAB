# CRAB Compiler Test Suite

This directory contains the comprehensive test suite for the CRAB compiler.

## Running Tests

### CRAB CLI Test Command (Recommended)

The easiest way to test CRAB is using the built-in test command:

```bash
# Comprehensive test - all architectures and formats
crab test

# Quick single-architecture test
crab test --quick

# Test with specific architecture
crab test --quick --arch arm64 --format pe

# Verbose output
crab test --verbose
```

**Comprehensive Test Output:**
```
CRAB Compiler - Comprehensive Test Suite
======================================================================
Testing x86_64 (native)           ✅ PASS
Testing x86_64 (pe)               ✅ PASS
Testing x86_32 (native)           ✅ PASS
Testing x86_32 (pe)               ✅ PASS
Testing x86_16 (native)           ✅ PASS
Testing arm64 (native)            ✅ PASS
Testing arm64 (pe)                ✅ PASS
Testing arm32 (native)            ✅ PASS
Testing arm32 (pe)                ✅ PASS

COMPREHENSIVE TEST SUMMARY
======================================================================
Total tests:  9
Passed:       9
Failed:       0
Success rate: 100.0%
```

### Unit Test Suite

Run all unit tests from the CRAB root directory:

```bash
cd testing
dotnet run
```

Or from anywhere:

```bash
dotnet run --project testing/testing.csproj
```

### Test Suites

The test suite includes 6 comprehensive test categories:

1. **Token/Lexer Tests** (`TokenTests.cs`)
   - Keyword tokenization
   - Identifier recognition
   - Literal parsing (integers, floats, strings, characters)
   - Operator tokenization
   - Comment handling

2. **Parser/Grammar Tests** (`ParserTests.cs`)
   - Class declarations
   - Method declarations
   - Property declarations
   - Expressions
   - Statements
   - Generics
   - Async/await
   - Manual memory blocks

3. **CTGC Automatic Memory Model Tests** (`CTGCTests.cs`)
   - Lifetime inference
   - Region analysis
   - Allocation tracking
   - Deallocation placement
   - Memory safety verification
   - Lambda closures
   - Generic type lifetimes

4. **Manual Memory Verification Tests** (`ManualMemoryTests.cs`)
   - Ownership tracking
   - Aliasing analysis
   - Escape analysis
   - Model isolation
   - Pointer safety
   - Stack allocation

5. **WASM Code Generation Tests** (`WASMGenerationTests.cs`)
   - Module structure
   - Function generation
   - Memory management
   - Control flow
   - Expressions
   - Type mapping

6. **End-to-End Integration Tests** (`IntegrationTests.cs`)
   - Hello World compilation
   - Compilation with arguments
   - Multi-file compilation
   - Project build

## Test Output

The test runner provides clear, formatted output:

```
╔════════════════════════════════════════════════════════════╗
║              CRAB Compiler Test Suite                     ║
╚════════════════════════════════════════════════════════════╝

=== Token/Lexer Tests ===
Testing keywords...
  ✓ Keywords test passed
...

╔════════════════════════════════════════════════════════════╗
║                      Test Summary                          ║
╚════════════════════════════════════════════════════════════╝
  Total test suites: 6
  Passed: 6
  Failed: 0

✅ All tests passed!
```

## Exit Codes

- **0**: All tests passed
- **1**: One or more tests failed

## Project Structure

```
testing/
├── README.md                    # This file
├── testing.csproj              # Test project configuration
├── TestRunner.cs               # Main test runner
├── TokenTests.cs               # Lexer/tokenizer tests
├── ParserTests.cs              # Parser/grammar tests
├── CTGCTests.cs                # CTGC automatic memory tests
├── ManualMemoryTests.cs        # Manual memory verification tests
├── WASMGenerationTests.cs      # Code generation tests
└── IntegrationTests.cs         # End-to-end integration tests
```

## Adding New Tests

To add new tests:

1. Create a new test class in this directory
2. Add a `RunAll()` method that executes all tests
3. Register the test suite in `TestRunner.cs`
4. Follow the existing test pattern for consistent output

Example:

```csharp
public class MyNewTests
{
    public void RunAll()
    {
        Console.WriteLine("=== My New Tests ===\n");
        TestSomething();
        // ... more tests
        Console.WriteLine("\n✓ All my new tests passed!\n");
    }
    
    private void TestSomething()
    {
        Console.WriteLine("Testing something...");
        // Test implementation
        Console.WriteLine("  ✓ Something test passed");
    }
}
```

Then in `TestRunner.cs`:

```csharp
var testSuites = new Action[]
{
    // ... existing tests
    () => new MyNewTests().RunAll(),
};
```

## CI/CD Integration

These tests are designed to be run in CI/CD pipelines. The exit code indicates success/failure, making it easy to integrate with build systems.

## Notes

- All test files have their standalone `Main()` methods commented out to avoid conflicts
- The `TestRunner.cs` serves as the single entry point for all tests
- Tests reference the main CRAB project via project reference
- Tests validate core functionality without requiring full compilation pipeline
