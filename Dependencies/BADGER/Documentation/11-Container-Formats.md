# Container Formats

## Overview

BADGER emits machine code in two container formats:

1. **Native** - Flat binary for bare metal execution
2. **PE** - Portable Executable for Windows

## Format Selection

Specify format using the `--format` flag:

```bash
# Native (bare metal)
dotnet run input.wat -o output.bin --format native

# PE (Windows executable)
dotnet run input.wat -o output.exe --format pe
```

## Native Format

**Purpose**: Bare metal execution  
**Use Cases**: Bootloaders, embedded systems, QEMU, SHARK

**Structure**:
```
[Machine Code]
```

That's it. No headers, no metadata, no relocations.

**Characteristics**:
- Entrypoint at offset 0
- Position-fixed code (no relocations)
- Loaded directly at fixed address
- Smallest possible binary size

**Advantages**:
- Simplest format possible
- Predictable layout
- Direct hardware execution
- No OS dependencies

**Limitations**:
- No symbol information
- No relocations
- Fixed load address required
- Not executable on modern OSes

See: [Native Format](12-Native-Format.md)

## PE Format

**Purpose**: Windows execution  
**Use Cases**: Windows applications, .exe files

**Structure**:
```
[DOS Header]
[DOS Stub]
[PE Signature]
[COFF Header]
[Optional Header]
[Section Table]
[Section Data (.text)]
```

**Characteristics**:
- Valid DOS stub (prints error in DOS)
- PE signature ("PE\0\0")
- Single code section
- Minimal headers
- Defined entrypoint

**Advantages**:
- Executable on Windows
- Standard format
- OS loader support
- Debugger compatible

**Limitations**:
- Larger than raw machine code
- Windows-specific
- Complex header structure

See: [PE Format](13-PE-Format.md)

## Format Comparison

| Feature | Native | PE |
|---------|--------|-----|
| Size | Minimal | ~512 bytes overhead |
| Headers | None | DOS + PE headers |
| Sections | None | .text section |
| OS Support | None | Windows only |
| Debugging | No | Yes (basic) |
| Relocations | No | No (in BADGER) |
| Imports | No | No (in BADGER) |

## Implementation

### Native Emitter

```csharp
public static byte[] Emit(byte[] machineCode)
{
    // Native format = raw machine code
    return machineCode;
}
```

Simple passthrough - no processing needed.

### PE Emitter

```csharp
public static byte[] Emit(byte[] machineCode)
{
    var pe = new List<byte>();
    
    pe.AddRange(CreateDOSHeader());
    pe.AddRange(CreateDOSStub());
    pe.AddRange(PESignature);
    pe.AddRange(CreateCOFFHeader());
    pe.AddRange(CreateOptionalHeader(...));
    pe.AddRange(CreateSectionTable(...));
    
    // Align to file alignment
    AlignTo(pe, 512);
    
    // Add machine code
    pe.AddRange(machineCode);
    
    // Align section
    AlignTo(pe, 512);
    
    return pe.ToArray();
}
```

Generates minimal but valid PE structure.

## Future Formats

BADGER is designed to support only Native and PE. It will **not** support:
- ELF (Linux executable format)
- Mach-O (macOS executable format)
- .deb packages (Linux packages)
- .rpm packages (Linux packages)

This is by design - BADGER targets bare metal and Windows only.

## Testing

Container tests verify:
- Native preserves exact machine code
- PE has valid headers
- PE includes DOS stub
- PE signature is correct
- Machine code is embedded correctly

See: `Testing/ContainerTests.cs`

## Extending

To add a new container format (if requirements change):

1. Create `Containers/NewFormat.cs`
2. Implement `Emit(byte[] machineCode) → byte[]`
3. Add to Program.cs switch statement
4. Add tests to ContainerTests.cs

However, this should only be done if the BADGER specification is updated.
