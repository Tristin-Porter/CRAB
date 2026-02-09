# Troubleshooting

## Build Issues

### .NET SDK Not Found

**Problem**: `dotnet: command not found`

**Solution**:
```bash
# Install .NET 10.0 SDK
# See: https://dotnet.microsoft.com/download
```

### NuGet Package Restore Failed

**Problem**: Cannot restore CDTk package

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Rebuild
dotnet build
```

### Compilation Errors

**Problem**: C# compilation errors

**Solution**:
```bash
# Clean build
dotnet clean
dotnet build

# Check .NET version
dotnet --version  # Should be 10.0+
```

## Runtime Issues

### Test Failures

**Problem**: Tests failing after changes

**Checklist**:
1. Read error message carefully
2. Check which test failed
3. Verify expected behavior
4. Review recent changes
5. Run `dotnet clean && dotnet build`

### Unknown Architecture Error

**Problem**: `Unknown architecture: xxx`

**Solution**:
```bash
# Use valid architecture
dotnet run input.wat --arch x86_64  # ✓
dotnet run input.wat --arch x86     # ✗ (use x86_32)

# Valid architectures:
# - x86_64
# - x86_32
# - x86_16
# - arm64
# - arm32
```

### Unknown Format Error

**Problem**: `Unknown format: xxx`

**Solution**:
```bash
# Use valid format
dotnet run input.wat --format native  # ✓
dotnet run input.wat --format elf     # ✗ (not supported)

# Valid formats:
# - native
# - pe
```

## WAT Input Issues

### Parse Errors

**Problem**: WAT parsing fails

**Causes**:
- Invalid WAT syntax
- Custom WAT dialect (not supported)
- Unsupported WAT features

**Solution**:
- Verify WAT is standard WebAssembly Text format
- Check for syntax errors
- Test with minimal WAT first

### Missing File

**Problem**: `File not found: input.wat`

**Solution**:
```bash
# Use absolute or relative path
dotnet run ./input.wat -o output.bin

# Check file exists
ls input.wat
```

## Output Issues

### Binary Not Created

**Problem**: No output file generated

**Checklist**:
1. Check for error messages
2. Verify write permissions
3. Check disk space
4. Try different output path

**Solution**:
```bash
# Specify output explicitly
dotnet run input.wat -o ./output.bin

# Check permissions
ls -l .  # Should be writable
```

### Binary Won't Execute (Native)

**Problem**: Native binary doesn't run

**Expected**: Native binaries are for bare metal/emulators, not normal OSes

**Solution**:
```bash
# Run in QEMU
qemu-system-x86_64 -kernel output.bin

# Or use PE format for Windows
dotnet run input.wat -o output.exe --format pe
```

### Binary Won't Execute (PE)

**Problem**: PE executable won't run on Windows

**Checklist**:
1. Verify file is actually PE (starts with "MZ")
2. Check architecture matches OS (x86_64 for 64-bit Windows)
3. Ensure no antivirus blocking
4. Try running from command line

**Solution**:
```cmd
# Check file type
xxd output.exe | head -1
# Should show: 00000000: 4d5a ... ("MZ")

# Run from cmd
output.exe

# Check with dumpbin
dumpbin /headers output.exe
```

## Assembly Encoding Issues

### Incorrect Machine Code

**Problem**: Generated machine code doesn't match expected

**Debug Steps**:
1. Enable verbose output (modify Program.cs)
2. Compare with test cases
3. Verify instruction encoding tables
4. Check architecture documentation

### Label Resolution Errors

**Problem**: Branches jump to wrong address

**Causes**:
- Incorrect offset calculation
- Label defined after use
- Two-pass assembly not working

**Solution**:
- Ensure labels are defined before use
- Check symbol table contents
- Verify pass 1 calculates addresses correctly

## Container Format Issues

### Invalid PE File

**Problem**: Windows says file is not a valid executable

**Checks**:
```bash
# Verify DOS header
xxd output.exe | head -1
# Should start with: 4d5a

# Verify PE signature
xxd -s 0x80 -l 4 output.exe
# Should show: 5045 0000

# Check with dumpbin
dumpbin /headers output.exe
```

**Solution**:
- Verify PE tests pass
- Check PE emitter code
- Compare with valid PE file

### Native Binary Too Small

**Problem**: Native binary is suspiciously small

**Cause**: Machine code might be incomplete

**Solution**:
```bash
# Check actual size
ls -lh output.bin

# Verify contains code
xxd output.bin | head

# Should see valid opcodes, not just zeros
```

## Performance Issues

### Slow Compilation

**Problem**: BADGER takes too long

**Normal**: BADGER should compile in < 1 second

**Causes**:
- Large WAT input
- Debug build
- Antivirus scanning

**Solution**:
```bash
# Build in release mode
dotnet build -c Release
dotnet run -c Release input.wat -o output.bin

# Disable antivirus temporarily
# (if it's scanning every file access)
```

## Architecture-Specific Issues

### x86_64 Issues

**REX Prefix Problems**: Verify 0x48 prefix for 64-bit operations

**ModRM Encoding**: Check register encoding tables

### ARM Issues

**Endianness**: ARM is little-endian (bytes reversed)

**Alignment**: Instructions must be 4-byte aligned

### x86_16 Issues

**Segment Registers**: Not fully implemented in BADGER

**Real Mode**: Limited to 1MB address space

## Common Error Messages

### "Stack underflow"

**Cause**: WAT tries to pop from empty stack

**Solution**: Fix WAT to push before pop

### "Unknown instruction"

**Cause**: Assembly instruction not in encoding table

**Solution**: Verify instruction is supported for architecture

### "Undefined label"

**Cause**: Label used but never defined

**Solution**: Define all labels before use

## Getting Help

If you can't resolve an issue:

1. **Check this guide** thoroughly
2. **Review documentation** for your use case
3. **Check test suite** for examples
4. **Open an issue** on GitHub with:
   - BADGER version
   - Input WAT (if possible)
   - Command used
   - Full error output
   - Expected vs actual behavior

## Debugging Tips

### Enable Verbose Output

Modify `Program.cs` to print intermediate stages:

```csharp
Console.WriteLine("Generated assembly:");
Console.WriteLine(assemblyText);

Console.WriteLine("Machine code:");
Console.WriteLine(BitConverter.ToString(machineCode));
```

### Hexdump Output

```bash
# View first 256 bytes
xxd -l 256 output.bin

# View with addresses
xxd output.bin | less

# Compare two files
diff <(xxd file1.bin) <(xxd file2.bin)
```

### Test Minimal Cases

Start with simplest possible input:

```wasm
(module
  (func $test (result i32)
    i32.const 42
  )
)
```

Then add complexity incrementally.

## Known Limitations

1. **No ELF support** - By design, only Native and PE
2. **No imports** - Cannot call external functions
3. **Minimal WAT** - Full WAT support in progress
4. **No optimization** - Outputs exactly what you specify
5. **No relocation** - Native binaries are position-fixed

These are intentional design choices, not bugs.
