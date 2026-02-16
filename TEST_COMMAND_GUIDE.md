# Enhanced Test Command Guide

## Overview

The CRAB test command provides comprehensive testing capabilities with performance metrics, debug logging, and support for multiple output formats including WASM.

## Features

### Comprehensive Testing
- Tests across all architectures (x86_64, x86_32, x86_16, ARM64, ARM32)
- Tests both native and PE formats
- Tests WASM output
- Automatic test project generation
- Build verification
- Output validation

### Performance Metrics
- Compilation time tracking
- Per-test timing information
- Performance summary (average, min, max)
- Displayed in debug mode

### Debug Logging
- Timestamped log entries
- Categorized logging (INFO, DEBUG)
- Detailed execution traces
- Saved to log files with --save flag

### Output Organization
- Structured output directories
- Organized by test project
- Separate folders for wasm, binaries, logs

## Usage

### Basic Commands

```bash
# Run quick test (single architecture)
crab test --quick

# Run comprehensive test (all architectures + WASM)
crab test

# Test specific project
crab test --name HelloWorld

# Enable debug logging
crab test --debug

# Save all outputs
crab test --save

# Keep generated projects
crab test --keep
```

### Flag Reference

| Flag | Description | Example |
|------|-------------|---------|
| `--name` | Test specific project | `--name Calculator` |
| `--quick` | Single architecture only | `--quick` |
| `--save` | Save all outputs | `--save` |
| `--keep` | Keep test projects | `--keep` |
| `--verbose` | Verbose output | `--verbose` |
| `--debug` | Debug logging + performance | `--debug` |
| `--arch` | Architecture (with --quick) | `--arch arm64` |
| `--format` | Format (with --quick) | `--format pe` |

## Test Projects

### Available Projects
1. **HelloWorld** - Prints "Hello World!"
2. **Calculator** - Prints "Calculator: 5 + 3 = 8"
3. **ClassHierarchy** - Prints "Base: 10, Derived: 20"
4. **GenericCollections** - Prints "Container Data: 100"

Each test project has unique output to verify compilation success.

### Custom Project
```bash
# Test all projects
crab test

# Test one project
crab test --name HelloWorld

# Test and keep project files
crab test --name Calculator --keep
```

## Output Examples

### Quick Test
```
======================================================================
CRAB Compiler - Comprehensive Test Suite
======================================================================
Projects:   HelloWorld
Mode:       Quick (single architecture)
======================================================================

══════════════════════════════════════════════════════════════════════
Testing Project: HelloWorld
══════════════════════════════════════════════════════════════════════
Generating HelloWorld...
Building HelloWorld...
[1/3] Compiling source files...
Running test project...
✓ HelloWorld completed successfully

======================================================================
COMPREHENSIVE TEST SUITE SUMMARY
======================================================================
Total projects:    1
Successful:        1
Total tests:       1
Passed tests:      1
Success rate:      100.0%
======================================================================

✓ All tests passed!
```

### Comprehensive Test
```
══════════════════════════════════════════════════════════════════════
Testing Project: HelloWorld
══════════════════════════════════════════════════════════════════════

[3/3] Running comprehensive test suite...

  Testing x86_64 (native)       ✅ PASS (45ms)
  Testing x86_64 (pe)           ✅ PASS (42ms)
  Testing x86_32 (native)       ✅ PASS (38ms)
  Testing x86_32 (pe)           ✅ PASS (41ms)
  Testing arm64 (native)        ✅ PASS (52ms)
  Testing arm64 (pe)            ✅ PASS (48ms)
  Testing arm32 (native)        ✅ PASS (43ms)
  Testing arm32 (pe)            ✅ PASS (45ms)
  Testing x86_16 (native)       ✅ PASS (35ms)

  Testing WASM Output:
  Testing WASM (wasm)           ✅ PASS (12ms)

Attempting to run on current platform...
  Detected platform: x86_64
  ✅ Execution successful on x86_64

✓ HelloWorld completed successfully

======================================================================
COMPREHENSIVE TEST SUITE SUMMARY
======================================================================
Total projects:    1
Successful:        1
Total tests:       10
Passed tests:      10
Success rate:      100.0%
======================================================================

✓ All tests passed!
```

### Debug Mode Output
```
======================================================================
CRAB Compiler - Comprehensive Test Suite
======================================================================
Projects:   HelloWorld
Mode:       Comprehensive (all architectures)
Debug:      True
======================================================================

  Testing x86_64 (native)       ✅ PASS (45ms)
  Testing x86_64 (pe)           ✅ PASS (42ms)
  ...

Performance Summary:
  Format           Average Time    Min Time    Max Time
  ----------------------------------------------------------------
  All formats            38.2ms        12ms        52ms
```

## Debug Logging

### Log Levels

#### INFO Level
General execution flow and milestones:
```
[20:15:42.001] INFO: Starting test suite execution with 1 projects
[20:15:42.125] INFO: Step 1: Generating HelloWorld project
[20:15:42.567] INFO: Step 2: Building test project
[20:15:43.234] INFO: Step 3: Running comprehensive test suite
[20:15:43.456] INFO: Test x86_64/native PASSED in 45ms
```

#### DEBUG Level
Detailed execution information:
```
[20:15:42.234] DEBUG: Generated HelloWorld project at /path/to/HelloWorld
[20:15:42.345] DEBUG: Testing x86_64/native
[20:15:42.456] DEBUG: Saved x86_64/native binary to /path/to/binaries
[20:15:42.567] DEBUG: Performance: Avg=38.2ms, Min=12ms, Max=52ms
```

### Log Files

With `--save` flag, logs are saved to:
```
tests/
  {ProjectName}/
    wasm/
      output.wasm
      output.js
    binaries/
      x86_64_native.bin
      x86_64_pe.exe
      arm64_native.bin
      ...
    logs/
      info.log      # INFO level logs
      debug.log     # DEBUG level logs
```

### Reading Logs
```bash
# View INFO log
cat tests/HelloWorld/logs/info.log

# View DEBUG log with timestamps
cat tests/HelloWorld/logs/debug.log

# Filter for errors
grep ERROR tests/HelloWorld/logs/debug.log

# Count test executions
grep "Testing " tests/HelloWorld/logs/debug.log | wc -l
```

## Performance Metrics

### Timing Information

Each test displays compilation time:
```
Testing x86_64 (native)       ✅ PASS (45ms)
```

### Performance Summary

In debug mode, see aggregate statistics:
```
Performance Summary:
  Format           Average Time    Min Time    Max Time
  ----------------------------------------------------------------
  All formats            38.2ms        12ms        52ms
```

### Interpreting Results

| Time Range | Performance |
|------------|-------------|
| < 20ms | Excellent |
| 20-50ms | Good |
| 50-100ms | Acceptable |
| > 100ms | May need optimization |

## WASM Testing

### WASM Output Test
```
Testing WASM Output:
  Testing WASM (wasm)           ✅ PASS (12ms)
    Saved 234 bytes to output.wasm
    Saved JS wrapper to output.js
```

### Verification
The test:
1. Compiles WAT to WASM binary
2. Validates WASM magic number
3. Generates JavaScript wrapper
4. Saves both files (with --save)

### Manual Verification
```bash
# After test with --save
cd tests/HelloWorld/wasm/

# Check WASM magic number
hexdump -C output.wasm | head -1
# Should show: 00 61 73 6d 01 00 00 00

# Check JS wrapper exists
ls -lh output.js

# Test in Node.js
node -e "
  const fs = require('fs');
  const buf = fs.readFileSync('output.wasm');
  console.log('WASM size:', buf.length, 'bytes');
  console.log('Magic:', buf.slice(0,4).toString('hex'));
"
```

## Saved Outputs

### Directory Structure
```
tests/
  HelloWorld/
    wasm/
      output.wasm          # WebAssembly binary
      output.js            # JavaScript wrapper
    binaries/
      x86_64_native.bin    # Native x86-64 binary
      x86_64_pe.exe        # Windows PE executable
      x86_32_native.bin    # Native x86-32 binary
      x86_32_pe.exe        # Windows PE 32-bit
      arm64_native.bin     # Native ARM64 binary
      arm64_pe.exe         # ARM64 PE executable
      arm32_native.bin     # Native ARM32 binary
      arm32_pe.exe         # ARM32 PE executable
      x86_16_native.bin    # Native x86-16 binary
    logs/
      info.log             # INFO level logs
      debug.log            # DEBUG level logs
  Calculator/
    ...
  ClassHierarchy/
    ...
```

### Accessing Saved Files
```bash
# List all saved test outputs
ls -R tests/

# Check WASM outputs
ls tests/*/wasm/

# Check binaries
ls tests/*/binaries/

# Read logs
tail -f tests/HelloWorld/logs/debug.log
```

## Advanced Usage

### Custom Architecture
```bash
# Test only ARM64
crab test --quick --arch arm64

# Test ARM64 with PE format
crab test --quick --arch arm64 --format pe
```

### Debugging Failed Tests
```bash
# Run with verbose + debug
crab test --name FailingProject --verbose --debug --keep

# Keep project for inspection
# Outputs stay in FailingProject/ directory

# Check debug logs
cat tests/FailingProject/logs/debug.log

# Inspect generated code
cat FailingProject/Program.cs
```

### Batch Testing
```bash
# Test all projects, save everything
crab test --save --debug

# Review results
ls -lh tests/*/wasm/
ls -lh tests/*/binaries/
```

## Integration with CI/CD

### GitHub Actions Example
```yaml
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
      
      - name: Build
        run: dotnet build
      
      - name: Run Tests
        run: dotnet run -- test --debug --save
      
      - name: Upload Test Outputs
        uses: actions/upload-artifact@v2
        with:
          name: test-outputs
          path: tests/
      
      - name: Check Success Rate
        run: |
          # Parse test results
          grep "Success rate" test-output.txt
```

### Local CI Script
```bash
#!/bin/bash
# run-tests.sh

set -e

echo "Building CRAB..."
dotnet build

echo "Running comprehensive tests..."
dotnet run -- test --debug --save

echo "Checking outputs..."
if [ ! -d "tests" ]; then
    echo "❌ Test outputs not found"
    exit 1
fi

WASM_COUNT=$(find tests -name "*.wasm" | wc -l)
echo "✓ Found $WASM_COUNT WASM files"

BIN_COUNT=$(find tests -name "*.bin" -o -name "*.exe" | wc -l)
echo "✓ Found $BIN_COUNT binary files"

echo "✅ All tests completed successfully"
```

## Troubleshooting

### Test Fails Immediately
```
Error: Failed to generate HelloWorld
```
**Solution:** Check disk space and permissions

### Compilation Timeout
```
Testing x86_64 (native)       ❌ FAIL (30000ms)
```
**Solution:** System may be under load, try again

### WASM Test Fails
```
Testing WASM (wasm)           ❌ FAIL
Error: BadgerCompiler.Compile failed
```
**Solution:** Check WAT file is valid, run with --debug for details

### Logs Not Saved
```
Saved outputs to: tests/HelloWorld
Warning: Failed to save logs
```
**Solution:** Verify write permissions on tests/ directory

## Best Practices

1. **Use --debug for Development**
   - Get detailed timing information
   - See performance metrics
   - Identify bottlenecks

2. **Use --save for CI/CD**
   - Archive test outputs
   - Compare across builds
   - Debug failures

3. **Use --quick for Iteration**
   - Faster feedback loop
   - Test single architecture
   - Verify changes quickly

4. **Review Logs Regularly**
   - Monitor performance trends
   - Identify degradation
   - Track improvements

## See Also

- [WASM+JS Emission Guide](WASM_JS_EMISSION_GUIDE.md)
- [BADGER Architecture](BADGER_ARCHITECTURE.md)
- [Build System Documentation](BUILD_SYSTEM.md)
