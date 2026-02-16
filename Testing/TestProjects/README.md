# CRAB Test Projects

This folder contains test project definitions used by the `crab test` command.

## Format

Each test file (e.g., `HelloWorld.cs`, `Calculator.cs`) contains:

1. **C# Source Code** - The program to compile and test
2. **Solution File (.sln)** - Visual Studio solution configuration
3. **Solution XML (.slnx)** - XML-based solution configuration

The sections are delimited by special markers:
- `// === BEGIN CSHARP ===` ... `// === END CSHARP ===`
- `// === BEGIN SLN ===` ... `// === END SLN ===`
- `// === BEGIN SLNX ===` ... `// === END SLNX ===`

## Available Tests

- **HelloWorld** - Simple program that prints "Hello World!"
- **Calculator** - Arithmetic operations test
- **ClassHierarchy** - Class inheritance test
- **GenericCollections** - Generic container test

## Usage

Run all tests:
```bash
crab test
```

Run a specific test:
```bash
crab test --name HelloWorld
```

Save test outputs:
```bash
crab test --save
```
