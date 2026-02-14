# CRAB Compiler Test Suite

This directory contains the comprehensive test suite for the CRAB compiler.

## Running Tests

### CRAB CLI Test Command (Recommended)

The easiest way to test CRAB is using the built-in test command:

```bash
# Comprehensive test - all architectures and formats
crab test

# Save all test outputs in organized folders
crab test --save

# Verbose output with detailed logging
crab test --verbose

# Debug mode with maximum detail
crab test --debug

# Save outputs with debug logging
crab test --save --debug

# Quick single-architecture test
crab test --quick

# Test with specific architecture
crab test --quick --arch arm64 --format pe

# Keep the test project after execution
crab test --keep
```

**Test Output Structure (with --save flag):**
```
tests/
└── TestProject/
    ├── wasm/
    │   └── output.wasm          # Compiled WASM output
    ├── binaries/
    │   ├── x86_64_native.bin   # Native x86_64 binary
    │   ├── x86_64_pe.exe       # PE format x86_64 binary
    │   ├── x86_32_native.bin   # Native x86_32 binary
    │   ├── x86_32_pe.exe       # PE format x86_32 binary
    │   ├── x86_16_native.bin   # Native x86_16 binary
    │   ├── arm64_native.bin    # Native ARM64 binary
    │   ├── arm64_pe.exe        # PE format ARM64 binary
    │   ├── arm32_native.bin    # Native ARM32 binary
    │   └── arm32_pe.exe        # PE format ARM32 binary
    └── logs/
        ├── debug.log           # Detailed debug log
        └── info.log            # Information log
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

Attempting to run on current platform...
  Detected platform: x86_64
  ✅ Execution successful on x86_64

COMPREHENSIVE TEST SUMMARY
======================================================================
Total tests:  9
Passed:       9
Failed:       0
Success rate: 100.0%
======================================================================

Saved outputs to: tests/TestProject
  - WASM files in: tests/TestProject/wasm
  - 9 binaries in: tests/TestProject/binaries
  - Logs in: tests/TestProject/logs
```

### Unit Test Suite

Run all unit tests from the CRAB root directory:

```bash
# Run test suite
crab test-suite

# With verbose output
crab test-suite --verbose

# With debug output
crab test-suite --debug

# Save test outputs and logs
crab test-suite --save
```

Or directly with dotnet:

```bash
cd Testing
dotnet run

# With flags
dotnet run -- --save --debug
```

### Test Suites

The test suite includes 7 comprehensive test categories:

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

7. **Comprehensive Tests** (`ComprehensiveTests.cs`)
   - Multiple test projects (Hello World, Calculator, Class Hierarchy, Generic Collections)
   - All architecture compilations (x86_64, x86_32, x86_16, arm64, arm32)
   - All container formats (native, PE)
   - Hardware detection and execution
   - Detailed logging (debug and info levels)

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
║        Comprehensive End-to-End Integration Tests          ║
╚════════════════════════════════════════════════════════════╝

=== Testing Project: HelloWorld ===
  Testing x86_64     (native)  ✅
  Testing x86_64     (pe)      ✅
  Testing x86_32     (native)  ✅
  Testing x86_32     (pe)      ✅
  Testing x86_16     (native)  ✅
  Testing arm64      (native)  ✅
  Testing arm64      (pe)      ✅
  Testing arm32      (native)  ✅
  Testing arm32      (pe)      ✅

=== Hardware Detection and Execution ===
Detected current architecture: x86_64

Attempting to execute HelloWorld on x86_64...
  ✅ Execution successful
  Output: Hello from CRAB!

╔════════════════════════════════════════════════════════════╗
║               Comprehensive Test Summary                   ║
╚════════════════════════════════════════════════════════════╝
Total projects tested:     4
Total compilation tests:   36
Passed:                    36
Failed:                    0
Success rate:              100.0%

✅ All comprehensive tests passed!

╔════════════════════════════════════════════════════════════╗
║                      Test Summary                          ║
╚════════════════════════════════════════════════════════════╝
  Total test suites: 7
  Passed: 7
  Failed: 0

✅ All tests passed!
```

## Logging

When using `--debug` or `--save` flags, the test suite generates detailed logs:

**debug.log** - Contains detailed debug information:
- Timestamps for all operations
- Detailed error messages and stack traces
- Internal state information
- File system operations
- Compilation details

**info.log** - Contains high-level information:
- Test progress
- Major milestones
- Summary information
- Success/failure status

## Exit Codes

- **0**: All tests passed
- **1**: One or more tests failed

## Project Structure

```
Testing/
├── README.md                    # This file
├── Testing.csproj              # Test project configuration
├── TestRunner.cs               # Main test runner with logging
├── TokenTests.cs               # Lexer/tokenizer tests
├── ParserTests.cs              # Parser/grammar tests
├── CTGCTests.cs                # CTGC automatic memory tests
├── ManualMemoryTests.cs        # Manual memory verification tests
├── WASMGenerationTests.cs      # Code generation tests
├── IntegrationTests.cs         # End-to-end integration tests
└── ComprehensiveTests.cs       # Comprehensive multi-project tests
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
var testSuites = new (string Name, Action Action)[]
{
    // ... existing tests
    ("My New Tests", () => new MyNewTests().RunAll()),
};
```

## CI/CD Integration

These tests are designed to be run in CI/CD pipelines. The exit code indicates success/failure, making it easy to integrate with build systems.

## Command-Line Options

**Test Command:**
- `--save` - Save all outputs to organized folders (tests/{name}/wasm, tests/{name}/binaries, tests/{name}/logs)
- `--debug` - Enable debug logging with detailed information
- `--verbose` - Enable verbose output
- `--keep` - Keep the generated test project after execution
- `--quick` - Run quick test (single architecture only)
- `--arch <arch>` - Specify architecture (x86_64, x86_32, x86_16, arm64, arm32)
- `--format <format>` - Specify format (native, pe)

**Test-Suite Command:**
- `--save` - Save test outputs and logs
- `--debug` - Enable debug mode with detailed diagnostics
- `--verbose` - Enable verbose output

## Notes

- All test files have their standalone `Main()` methods commented out to avoid conflicts
- The `TestRunner.cs` serves as the single entry point for all tests
- Tests reference the main CRAB project via project reference
- Tests validate core functionality without requiring full compilation pipeline
- The comprehensive test suite creates multiple projects and tests all architecture/format combinations
- Hardware detection automatically determines which binaries can be executed on the current system
- Logs are timestamped and organized for easy debugging
