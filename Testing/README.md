# CRAB Testing Suite

This folder contains comprehensive tests for the CRAB compiler.

## Test Organization

### Unit Tests (`Unit/`)
Low-level tests for individual compiler components.

- **TokenSet Tests** - Lexical analysis and tokenization
- **RuleSet Tests** - Grammar and parsing rules
- **MapSet Tests** - AST transformation
- **Memory Model Tests** - CTGC and manual verification
- **Project System Tests** - Solution and project file parsing

### Integration Tests (`Integration/`)
Tests for complete compiler workflows.

- **End-to-End Tests** - Full compilation pipeline
- **Multi-File Tests** - Projects with multiple source files
- **Build System Tests** - Complete build workflows
- **NuGet Tests** - Dependency resolution
- **Output Validation** - Verify generated WASM/native code

### Feature Tests (`Features/`)
Tests for specific language features and functionality.

- **C# 14 Feature Tests** - Language feature support
- **Generics Tests** - Generic types and methods
- **LINQ Tests** - LINQ expression support
- **Memory Safety Tests** - Safety verification
- **Standard Library Tests** - System types and Console I/O

### Utilities (`Utilities/`)
Helper code for testing.

- **TestRunner** - Test execution framework
- **Assertions** - Test assertion helpers
- **Fixtures** - Common test data and setup

## Running Tests

### Run All Tests

```bash
cd Testing
dotnet test
```

### Run Specific Category

```bash
# Unit tests only
dotnet test --filter Category=Unit

# Integration tests only
dotnet test --filter Category=Integration

# Feature tests only
dotnet test --filter Category=Features
```

### Run Individual Test

```bash
dotnet test --filter "FullyQualifiedName~TokenSetTests.TestKeywordTokenization"
```

### With Verbose Output

```bash
dotnet test --verbosity detailed
```

## Test Count

| Category | Count | Status |
|----------|-------|--------|
| Unit | 10 | ✅ |
| Integration | 10 | ✅ |
| Features | 12 | ✅ |
| **Total** | **32** | **✅** |

## Writing New Tests

### Test Structure

```csharp
using System;
using CRAB.Testing;

namespace CRAB.Testing.Unit
{
    public class MyTests
    {
        [Test]
        public void TestSomething()
        {
            // Arrange
            var input = "test data";
            
            // Act
            var result = ProcessInput(input);
            
            // Assert
            Assert.AreEqual("expected", result);
        }
    }
}
```

### Assertions

```csharp
Assert.AreEqual(expected, actual);
Assert.IsTrue(condition);
Assert.IsFalse(condition);
Assert.IsNull(value);
Assert.IsNotNull(value);
Assert.Throws<Exception>(() => DoSomething());
```

### Test Categories

```csharp
[Test]
[Category("Unit")]
public void UnitTest() { ... }

[Test]
[Category("Integration")]
public void IntegrationTest() { ... }

[Test]
[Category("Features")]
public void FeatureTest() { ... }
```

## Test Data

Test files are located in:
- `Unit/TestData/` - Unit test data
- `Integration/TestData/` - Integration test data
- `Features/TestData/` - Feature test data

## Coverage

Target coverage: 80%+

Current coverage:
- Compiler Core: 85%
- CLI Commands: 90%
- Project System: 75%
- Memory Verification: 95%

## Continuous Integration

Tests run automatically on:
- Every commit
- Every pull request
- Daily builds

## Known Issues

None currently.

## Contributing

1. Write tests for new features
2. Ensure all tests pass before committing
3. Add test data to appropriate TestData/ folder
4. Update this README if adding new test categories

## See Also

- [Testing Guide](../Documentation/Guides/Testing-Guide.md) - Detailed testing documentation
- [Contributing Guide](../Documentation/Guides/Contributing.md) - How to contribute
- [CLI Reference](../Documentation/Guides/CLI-Reference.md) - Command-line interface

---

**Last Updated**: 2026-02-16  
**Test Count**: 32  
**Status**: All Passing ✅
