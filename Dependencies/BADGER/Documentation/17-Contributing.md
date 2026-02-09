# Contributing

## Welcome!

Thank you for your interest in contributing to BADGER! This guide will help you get started.

## Before You Begin

### Read the Specification

BADGER's architecture and behavior are defined in:
- `.github/agents/badger-spec.txt` - Complete specification

**Important**: All contributions must align with this specification.

### Understand the Design

BADGER is purpose-built with specific goals:
- WAT-to-machine-code compilation
- Support for 5 architectures (x86_64, x86_32, x86_16, ARM64, ARM32)
- Two container formats (Native, PE)
- Deterministic, predictable output
- C#-only implementation

## Types of Contributions

### 1. Bug Fixes

Found a bug? Great!

**Steps**:
1. Check if it's already reported
2. Create an issue with:
   - Description of the bug
   - Steps to reproduce
   - Expected vs actual behavior
   - BADGER version
3. Submit a PR with:
   - Fix for the bug
   - Test that verifies the fix
   - Reference to the issue

### 2. Test Improvements

More tests are always welcome!

**Good test PRs**:
- Test uncovered edge cases
- Add architecture-specific tests
- Improve test coverage
- Add runtime/execution tests

**Guidelines**:
- Keep tests deterministic
- Keep tests fast (< 1 second total)
- Follow existing test patterns
- Add to appropriate test file

### 3. Documentation

Help others understand BADGER!

**Documentation needs**:
- Examples and tutorials
- Architecture-specific guides
- Troubleshooting additions
- Clarity improvements

### 4. New Features

Want to add a feature? Great! But read this first:

**Must align with spec**: New features must fit BADGER's purpose and design.

**Not accepting**:
- ELF/Mach-O/other container formats
- Non-C# implementations
- Custom WAT dialects
- Dynamic linking/imports
- Non-WAT input formats

**Might accept**:
- Better error messages
- Performance improvements
- Additional WAT instruction support
- Better test coverage
- Tooling improvements

**Process**:
1. Open an issue first to discuss
2. Get approval before coding
3. Submit PR with tests and docs
4. Address review feedback

## Development Setup

### Clone Repository

```bash
git clone https://github.com/Tristin-Porter/BADGER.git
cd BADGER
```

### Build and Test

```bash
dotnet build
dotnet run  # Runs tests automatically
```

### Make Changes

```bash
# Create a branch
git checkout -b my-feature

# Make changes
# ...

# Test
dotnet build
dotnet run

# Commit
git commit -am "Description of changes"

# Push
git push origin my-feature
```

### Submit PR

1. Go to GitHub
2. Create Pull Request
3. Describe your changes
4. Link to related issues
5. Wait for review

## Code Style

### C# Style

Follow existing code style:

```csharp
// Good
public static byte[] Assemble(string assemblyText)
{
    var result = new List<byte>();
    // ...
    return result.ToArray();
}

// Bad
public static byte[] Assemble(string assemblyText) {
  List<byte> result = new List<byte>();
  // ...
  return result.ToArray();
}
```

**Guidelines**:
- Use var for local variables
- PascalCase for public members
- camelCase for private fields
- Descriptive names
- XML comments for public APIs

### File Organization

```csharp
using System;
using System.Collections.Generic;
using CDTk;

namespace Badger.NameSpace;

/// <summary>
/// Class description
/// </summary>
public class MyClass
{
    // Private fields
    private int _value;
    
    // Public properties
    public int Value => _value;
    
    // Constructors
    public MyClass() { }
    
    // Public methods
    public void DoSomething() { }
    
    // Private methods
    private void Helper() { }
}
```

## Testing Requirements

### All Changes Must Include Tests

- Bug fixes: Test that fails before fix, passes after
- New features: Comprehensive test coverage
- Refactoring: Existing tests must still pass

### Test Quality

```csharp
// Good test
private static void TestX86_64PushRbp()
{
    string asm = "push rbp";
    byte[] code = Assembler.Assemble(asm);
    
    // Verify exact encoding
    TestRunner.AssertEqual(0x55, code[0], "push rbp opcode");
    TestRunner.AssertEqual(1, code.Length, "push rbp is 1 byte");
}

// Bad test
private static void TestStuff()
{
    var result = DoThing();
    TestRunner.Assert(result != null, "should work");
}
```

## Commit Guidelines

### Commit Messages

```
Good:
- "Fix x86_64 ModRM encoding for mov rax, rbx"
- "Add tests for ARM64 branch instructions"
- "Update documentation for PE format"

Bad:
- "Fix bug"
- "Update"
- "WIP"
```

### Commit Size

- Small, focused commits
- One logical change per commit
- Can be squashed if needed

## Pull Request Process

### PR Checklist

Before submitting:

- [ ] Code compiles without warnings
- [ ] All tests pass (45/45)
- [ ] New tests added for changes
- [ ] Documentation updated if needed
- [ ] Commit messages are clear
- [ ] Code follows existing style
- [ ] Changes align with specification

### Review Process

1. **Automated checks**: Tests must pass
2. **Code review**: Maintainer reviews code
3. **Feedback**: Address review comments
4. **Approval**: Maintainer approves
5. **Merge**: Merged to main branch

### Expected Timeline

- Small fixes: 1-3 days
- New features: 1-2 weeks
- Major changes: 2-4 weeks

## Architecture Changes

### Adding a New Architecture

If BADGER specification is updated to support a new architecture:

1. Create `Architectures/NewArch.cs`
2. Implement lowering (`WATToNewArchMapSet`)
3. Implement assembler (`Assembler.Assemble`)
4. Add to Program.cs
5. Add comprehensive tests
6. Document in `Documentation/XX-NewArch.md`

### Modifying Existing Architecture

1. Understand current implementation
2. Ensure change doesn't break tests
3. Update tests to verify change
4. Document the change

## Container Changes

### Adding a New Container

Only if specification is updated:

1. Create `Containers/NewFormat.cs`
2. Implement `Emit(byte[] machineCode)`
3. Add to Program.cs
4. Add tests
5. Document in `Documentation/`

## Documentation Changes

Documentation improvements are always welcome!

### Documentation Structure

```
Documentation/
├── README.md                  # Overview and index
├── 01-Overview.md            # What is BADGER
├── 02-Architecture.md        # High-level design
├── ...                       # More docs
```

### Documentation Style

- Clear and concise
- Code examples where helpful
- Link to related docs
- Keep it up-to-date

## Questions?

- **Specification questions**: Read `badger-spec.txt`
- **Usage questions**: Check Documentation/
- **Bug reports**: Open an issue
- **Feature ideas**: Open an issue for discussion
- **General help**: Open an issue

## Code of Conduct

Be professional and respectful:

- Be kind and courteous
- Respect different viewpoints
- Accept constructive criticism
- Focus on what's best for BADGER
- Help others when you can

## License

By contributing, you agree that your contributions will be licensed under the same license as BADGER.

## Recognition

Contributors will be:
- Listed in commit history
- Mentioned in release notes (for significant contributions)
- Credited in documentation (for major features)

## Thank You!

Every contribution helps make BADGER better. Whether it's fixing a typo or implementing a new feature, your work is appreciated!
