# PE Format

## Overview

PE (Portable Executable) is the executable format for Windows. BADGER emits minimal PE files with just enough structure to be valid and executable.

## Structure

```
[0x0000] DOS Header (64 bytes)
[0x0040] DOS Stub (64 bytes)
[0x0080] PE Signature (4 bytes: "PE\0\0")
[0x0084] COFF Header (20 bytes)
[0x0098] Optional Header (240 bytes for PE32+)
[0x0188] Section Table (40 bytes per section)
[0x0200] Code Section (.text)
```

## DOS Header

The first 64 bytes form the DOS header:

```
Offset  Size  Description
0x00    2     Magic number: "MZ" (0x4D 0x5A)
0x02    58    DOS header fields (mostly unused)
0x3C    4     Offset to PE header (typically 0x80)
```

**Magic Number**: Every PE file starts with "MZ" (Mark Zbikowski's initials).

**PE Offset**: Tells DOS where the real PE header is located.

## DOS Stub

A small DOS program (64 bytes) that prints an error message when run in DOS mode:

```
"This program cannot be run in DOS mode."
```

Modern systems ignore this, but it's required for PE validity.

## PE Signature

4 bytes: `50 45 00 00` ("PE\0\0")

This identifies the file as a PE executable.

## COFF Header

20 bytes of machine and file metadata:

```
Offset  Size  Description
0x00    2     Machine type (0x8664 = AMD64/x86-64)
0x02    2     Number of sections (1 for BADGER)
0x04    4     Timestamp (0 in BADGER)
0x08    4     Symbol table pointer (0)
0x0C    4     Number of symbols (0)
0x10    2     Optional header size (240 for PE32+)
0x12    2     Characteristics (0x22 = executable, large address aware)
```

**Machine Types**:
- 0x8664 = x86_64 (AMD64)
- 0x014C = x86_32 (i386)
- 0xAA64 = ARM64
- 0x01C4 = ARM32

## Optional Header

240 bytes for PE32+ (x86_64), 224 bytes for PE32:

### Standard Fields
```
Offset  Size  Description
0x00    2     Magic (0x20B = PE32+, 0x10B = PE32)
0x02    2     Linker version
0x04    4     Code size
0x08    4     Initialized data size
0x0C    4     Uninitialized data size
0x10    4     Entry point RVA (0x1000 in BADGER)
0x14    4     Base of code (0x1000)
```

### Windows-Specific Fields
```
Offset  Size  Description
0x18    8     Image base (0x400000)
0x20    4     Section alignment (0x1000)
0x24    4     File alignment (0x200 = 512 bytes)
0x28    4     OS version (5.2)
0x2C    4     Image version (0.0)
0x30    4     Subsystem version (5.2)
0x38    4     Image size
0x3C    4     Headers size (0x200)
0x44    2     Subsystem (3 = console)
0x48    8     Stack reserve
0x50    8     Stack commit
0x58    8     Heap reserve
0x60    8     Heap commit
0x6C    4     Number of data directories (16)
```

### Data Directories
16 entries (8 bytes each) for imports, exports, resources, etc.  
BADGER sets all to zero (no imports/exports in minimal PE).

## Section Table

40 bytes per section:

```
Offset  Size  Description
0x00    8     Name (".text\0\0\0")
0x08    4     Virtual size (actual code size)
0x0C    4     Virtual address (RVA, typically 0x1000)
0x10    4     Size of raw data (aligned to file alignment)
0x14    4     Pointer to raw data (typically 0x200)
0x18    4     Pointer to relocations (0)
0x1C    4     Pointer to line numbers (0)
0x20    2     Number of relocations (0)
0x22    2     Number of line numbers (0)
0x24    4     Characteristics (0x60000020 = code, executable, readable)
```

## Code Section

The actual machine code, aligned to file alignment (512 bytes):

```
[0x0200] Machine code
[...]    Padding to next 512-byte boundary
```

## Alignment

PE files use two alignment values:

**File Alignment** (512 bytes):
- How sections are aligned on disk
- Headers padded to 512 bytes
- Sections padded to 512 bytes

**Section Alignment** (4096 bytes):
- How sections are aligned in memory
- Must be >= file alignment
- Typically 4KB page size

## Memory Layout

When loaded, the PE file is mapped to memory:

```
0x400000        Image base
0x401000        .text section (RVA 0x1000)
0x401000        First instruction (entrypoint)
```

## Creating PE Files

```bash
dotnet run program.wat -o program.exe --arch x86_64 --format pe
```

## Minimal PE

BADGER creates truly minimal PE files:
- No imports (cannot call Windows APIs)
- No exports
- No relocations
- No resources
- No debug info
- No TLS
- Single code section

This is intentional - BADGER outputs are for CRAB-generated code, not general Windows programming.

## Running PE Files

```cmd
# On Windows
program.exe

# With Wine on Linux
wine program.exe
```

## Verification

Verify PE structure:

```bash
# Check DOS header
xxd program.exe | head -1
# Should show: 00000000: 4d5a ...  ("MZ")

# Check PE signature at offset 0x80
xxd -s 0x80 -l 4 program.exe
# Should show: 00000080: 5045 0000  ("PE\0\0")
```

## Advanced Tools

Analyze PE structure:

```bash
# Linux
objdump -x program.exe
readelf -h program.exe  # Won't work, PE is not ELF

# Windows
dumpbin /headers program.exe
```

## Testing

PE tests verify:
- DOS header magic "MZ"
- PE signature "PE\0\0"
- Valid COFF header
- Correct machine type
- Code section present
- Machine code embedded correctly

See: `Testing/ContainerTests.cs`

## References

- Microsoft PE/COFF Specification
- PE Format documentation on OSDev wiki
- Windows SDK documentation
