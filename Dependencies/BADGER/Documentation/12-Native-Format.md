# Native Format

## Overview

Native format is the simplest possible binary format - raw machine code with no headers, metadata, or relocations.

## Structure

```
Offset  Content
0x0000  First instruction (entrypoint)
0x0001  ...machine code continues...
...     ...until end of code...
```

The entire file is executable machine code.

## Characteristics

### No Headers
- No magic numbers
- No file format metadata
- No version information
- No section tables

### Fixed Entrypoint
- Execution starts at offset 0
- First byte must be valid instruction
- No entry point table or indirect jumps

### Position-Fixed
- Code assumes specific load address
- No relocations or position-independent code
- Absolute addresses baked into instructions

## Use Cases

### Bootloaders
```asm
; Loaded at 0x7C00 by BIOS
org 0x7C00
mov si, message
call print_string
; ...
```

### Embedded Systems
```asm
; Loaded at 0x00000000 on microcontroller
reset_vector:
    ldr sp, =stack_top
    bl main
```

### QEMU Testing
```bash
# Run flat binary directly
qemu-system-x86_64 -kernel output.bin
```

### SHARK Environment
SHARK (CRAB's runtime) can load Native binaries directly.

## Creating Native Binaries

```bash
# From WAT
dotnet run program.wat -o program.bin --arch x86_64 --format native

# From assembly (future)
dotnet run program.asm -o program.bin --arch x86_64 --format native
```

## Loading Native Binaries

Native binaries must be loaded at their expected address:

### QEMU
```bash
qemu-system-x86_64 -kernel program.bin
```

### Custom Loader
```c
void* load_address = (void*)0x100000;
FILE* f = fopen("program.bin", "rb");
fread(load_address, 1, file_size, f);
((void(*)())load_address)();  // Jump to entrypoint
```

## Advantages

1. **Minimal Size**: Only machine code, no overhead
2. **Predictable**: Exact layout known
3. **Fast Loading**: No parsing required
4. **Direct Execution**: No translation needed

## Limitations

1. **No OS Support**: Modern OSes won't execute these
2. **No Debugging**: No symbol information
3. **Fixed Address**: Cannot be relocated
4. **No Imports**: Cannot call external functions

## Technical Details

### Entrypoint

The first instruction is the entrypoint:

```asm
; This is executed first
main:
    push rbp
    mov rbp, rsp
    ; ... program ...
```

Compiles to:

```
55              push rbp
48 89 E5        mov rbp, rsp
...
```

The byte `55` is at offset 0 and executes first.

### No Return

Native programs typically don't "return". They either:
- Loop forever (embedded systems)
- Halt the processor (`hlt` instruction)
- Jump to known address (bootloader chain-loading)

### Memory Layout

The loader decides memory layout:

```
Load Address: 0x100000

0x100000  First instruction
0x100001  ...
0x100050  Data section (if any)
...
```

## Example: Hello World (x86_64 Bare Metal)

```asm
; Assumes VGA text buffer at 0xB8000
start:
    mov rax, 0xB8000        ; VGA buffer
    mov rbx, message
    call print_string
    hlt

print_string:
    ; Print null-terminated string to VGA
    lodsb
    test al, al
    jz done
    mov [rax], al
    inc rax
    mov byte [rax], 0x0F    ; White on black
    inc rax
    jmp print_string
done:
    ret

message:
    db "Hello, World!", 0
```

This compiles to a Native binary that runs on bare x86_64 hardware.

## Verification

To verify a Native binary:

```bash
# Hexdump first few bytes
xxd output.bin | head

# Expected: Valid machine code
00000000: 5548 89e5 ...
```

The output should be recognizable machine code for the target architecture.

## Testing

BADGER tests verify:
- Native.Emit() returns exact machine code
- No headers are added
- Binary is identical to input machine code

See: `Testing/ContainerTests.cs`
