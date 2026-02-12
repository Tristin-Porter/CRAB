# Testing

## Overview

BADGER includes a comprehensive test suite with 45+ tests covering all major subsystems. Tests run automatically at startup to verify system integrity.

## Test Categories

### 1. WAT Parser Tests (6 tests)

Verify that the WAT grammar and tokens are correctly defined.

**Tests**:
- WAT tokens are defined
- WAT grammar rules are defined
- WAT module structure
- WAT instruction tokens exist
- WAT type tokens exist
- WAT control flow tokens exist

**Purpose**: Ensure CDTk-based parser can handle all WAT constructs.

### 2. Architecture Lowering Tests (11 tests)

Verify that WAT-to-assembly lowering works for all architectures.

**Tests per architecture**:
- Function prologue/epilogue generation
- i32.const lowering
- i32.add lowering (x86_64 only)

**Architectures tested**:
- x86_64
- x86_32
- x86_16
- ARM64
- ARM32

**Purpose**: Ensure lowering rules generate correct assembly structure.

### 3. Assembly Encoding Tests (18 tests)

Verify exact machine code generation for each architecture.

**x86_64 tests**:
- Encode mov rax, rbx
- Encode push rbp
- Encode pop rbp
- Encode ret
- Encode add rax, rbx
- Encode mov rax, immediate

**x86_32 tests**:
- Encode mov eax, ebx
- Encode push ebp
- Encode ret

**x86_16 tests**:
- Encode mov ax, bx
- Encode push bp
- Encode ret

**ARM64 tests**:
- Encode mov x0, x1
- Encode add x0, x1, x2
- Encode ret (with exact byte verification)

**ARM32 tests**:
- Encode mov r0, r1
- Encode add r0, r1, r2
- Encode bx lr

**Purpose**: Ensure assemblers generate correct machine code bytes.

### 4. Container Emission Tests (6 tests)

Verify binary container generation.

**Native tests**:
- Emit flat binary
- Preserve machine code exactly

**PE tests**:
- Emit valid PE structure
- DOS header present
- PE signature present
- Code section present and contains machine code

**Purpose**: Ensure containers are valid and contain correct code.

### 5. Integration Tests (4 tests)

End-to-end pipeline tests.

**Tests**:
- Simple x86_64 program end-to-end
- x86_64 Native container end-to-end
- x86_64 PE container end-to-end
- All architectures can assemble

**Purpose**: Verify complete pipeline from assembly to binary.

## Running Tests

Tests run automatically:

```bash
dotnet build
dotnet run
```

Output:

```
================================================================================
BADGER Test Suite
================================================================================

WAT Parser Tests:
----------------
  ✓ WAT tokens are defined
  ✓ WAT grammar rules are defined
  ...

================================================================================
Test Results: 45/45 passed, 0 failed
================================================================================
```

## Test Infrastructure

### TestRunner

Central test runner with utilities:

```csharp
TestRunner.RunTest(string name, Action test)
TestRunner.Assert(bool condition, string message)
TestRunner.AssertEqual<T>(T expected, T actual, string message)
TestRunner.AssertArrayEqual(byte[] expected, byte[] actual, string message)
```

### Test Organization

```
Testing/
├── TestRunner.cs              # Test infrastructure
├── WATParserTests.cs          # WAT parser tests
├── LoweringTests.cs           # Lowering tests
├── AssemblyEncodingTests.cs   # Encoding tests
├── ContainerTests.cs          # Container tests
└── IntegrationTests.cs        # End-to-end tests
```

## Test Characteristics

### Deterministic

All tests are deterministic:
- Same input produces same output
- No randomness or timing dependencies
- Exact byte comparisons

### Fast

Tests complete in < 1 second total:
- No I/O except test data
- No external dependencies
- Minimal computation

### Comprehensive

Tests cover:
- All major code paths
- Edge cases
- Error conditions
- All architectures
- All container formats

## Adding Tests

To add a new test:

1. Choose appropriate test file
2. Add test method
3. Call `TestRunner.RunTest` in `RunTests()`
4. Use assertions to verify behavior

Example:

```csharp
public static class MyTests
{
    public static void RunTests()
    {
        TestRunner.RunTest("My test", TestMyFeature);
    }
    
    private static void TestMyFeature()
    {
        var result = MyFunction();
        TestRunner.AssertEqual(42, result, "Should return 42");
    }
}
```

## Test Data

### Known-Good Machine Code

Tests use known-good byte sequences:

```csharp
// x86_64: push rbp
byte[] expected = { 0x55 };
byte[] actual = Assembler.Assemble("push rbp");
TestRunner.AssertArrayEqual(expected, actual, "push rbp encoding");
```

### Test Assembly Programs

```csharp
string asm = @"
main:
    push rbp
    mov rbp, rsp
    pop rbp
    ret
";
byte[] code = Assembler.Assemble(asm);
TestRunner.Assert(code.Length > 0, "Should generate code");
```

## Continuous Verification

Tests verify BADGER maintains its invariants:

1. **WAT compliance** - Standard WAT is supported
2. **Determinism** - Same input → same output
3. **Correctness** - Generated code matches specification
4. **Completeness** - All architectures function
5. **Format compliance** - Containers are valid

## Future Testing

Planned additions:

- Runtime tests (execute generated code)
- QEMU integration tests
- Performance benchmarks
- Stress tests (large programs)
- Fuzzing (random valid WAT)

## Test Coverage

Current coverage:

- ✓ WAT parser structure
- ✓ All architectures compile
- ✓ Instruction encoding (sample)
- ✓ Container format structure
- ✓ End-to-end pipeline
- ⧗ Full WAT instruction coverage
- ⧗ Runtime execution tests
- ⧗ Performance testing

Legend: ✓ = Implemented, ⧗ = Future work

## Debugging Failed Tests

If a test fails:

1. **Read error message**: Tells you what failed
2. **Check expected vs actual**: Understand the difference
3. **Isolate the test**: Run just that test
4. **Add debug output**: Print intermediate values
5. **Compare with spec**: Verify expected behavior
6. **Fix and re-test**: Ensure fix doesn't break other tests

## Best Practices

1. **Test early** - Write tests before implementation
2. **Test thoroughly** - Cover edge cases
3. **Test independently** - Each test standalone
4. **Test deterministically** - No random behavior
5. **Test quickly** - Keep tests fast

## Assertions

Use appropriate assertions:

```csharp
// Boolean conditions
TestRunner.Assert(value > 0, "Value should be positive");

// Exact equality
TestRunner.AssertEqual(42, value, "Should equal 42");

// Byte array equality
TestRunner.AssertArrayEqual(expected, actual, "Bytes should match");
```

## Test Naming

Test names should be descriptive:

✓ Good: "x86_64: Encode mov rax, rbx"  
✗ Bad: "Test1"

✓ Good: "PE: DOS header present"  
✗ Bad: "Check PE"
