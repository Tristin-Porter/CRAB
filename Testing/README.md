# CRAB Compiler Test Suite

This directory contains comprehensive tests for the CRAB compiler.

## Test Categories

### 1. TokenTests.cs
Tests the lexer/tokenizer:
- Keyword recognition
- Identifier parsing
- Literal handling (integers, floats, strings, characters)
- Operator tokenization
- Comment handling
- Preprocessor directive parsing
- Whitespace handling

### 2. ParserTests.cs
Tests the parser/grammar:
- Class declarations
- Method declarations
- Property declarations
- Expression parsing
- Statement parsing
- Generic type declarations
- Async/await syntax
- Manual memory blocks

### 3. CTGCTests.cs
Tests the Compile-Time Garbage Collection (CTGC) automatic memory model:
- Lifetime inference
- Region analysis
- Allocation tracking
- Deallocation placement
- Memory safety verification (no leaks, no use-after-free)
- Lambda closure handling
- Generic type lifetime analysis

### 4. ManualMemoryTests.cs
Tests the manual memory verification model:
- Ownership tracking
- Aliasing analysis
- Escape analysis
- Model isolation (automatic/manual separation)
- Pointer safety verification
- Stack allocation handling

### 5. WASMGenerationTests.cs
Tests WASM code generation:
- Module structure generation
- Function lowering
- Memory management code
- Control flow translation
- Expression evaluation
- Type mapping (C# to WASM types)

### 6. IntegrationTests.cs
End-to-end integration tests:
- Hello World compilation
- Programs with arguments
- Multi-file compilation
- Project build system
- Complete compilation pipeline

## Running Tests

### Run All Tests
```bash
dotnet run --project Testing/TestRunner.cs
```

### Run Individual Test Suite
```bash
dotnet run --project Testing/TokenTests.cs
dotnet run --project Testing/ParserTests.cs
dotnet run --project Testing/CTGCTests.cs
dotnet run --project Testing/ManualMemoryTests.cs
dotnet run --project Testing/WASMGenerationTests.cs
dotnet run --project Testing/IntegrationTests.cs
```

## Test Architecture

Tests are organized following these principles:

1. **Unit Tests**: TokenTests, ParserTests test individual components
2. **Semantic Tests**: CTGCTests, ManualMemoryTests test memory models
3. **Generation Tests**: WASMGenerationTests test code generation
4. **Integration Tests**: IntegrationTests test the complete pipeline

Each test file is self-contained and can run independently.

## Adding New Tests

To add new tests:

1. Create a new test class in this directory
2. Follow the naming convention: `*Tests.cs`
3. Implement a `RunAll()` method that runs all tests
4. Add the test suite to `TestRunner.cs`
5. Document the test in this README

## Test Assertions

Tests use simple assertion helpers:
- `AssertTokenizes()` - Verify token recognition
- `AssertParses()` - Verify grammar parsing
- `AssertCTGCAnalysis()` - Verify CTGC analysis
- `AssertManualAnalysis()` - Verify manual memory analysis
- `AssertGeneratesWASM()` - Verify WASM generation
- `AssertCompilationSucceeds()` - Verify full compilation

## Notes

- Tests are designed to validate the CRAB specification compliance
- Each test validates a specific aspect of the compiler
- Tests document expected behavior and serve as examples
- The test suite ensures CRAB maintains its safety guarantees
