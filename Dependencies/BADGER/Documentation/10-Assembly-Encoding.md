# Assembly Encoding

## Overview

Assembly encoding is the process of converting human-readable assembly text into binary machine code. Each architecture implements its own assembler with architecture-specific encoding rules.

## Two-Pass Assembly

BADGER uses a classic two-pass assembly algorithm:

### Pass 1: Label Resolution

**Purpose**: Calculate addresses for all labels and instruction sizes

**Process**:
1. Parse each instruction
2. Determine instruction size (varies by architecture and operands)
3. Assign addresses to labels
4. Build symbol table

**Output**: Symbol table mapping labels to addresses

### Pass 2: Instruction Encoding

**Purpose**: Generate final machine code

**Process**:
1. Parse each instruction again
2. Encode opcode and operands
3. Resolve label references using symbol table
4. Compute branch/call offsets
5. Emit final bytes

**Output**: Binary machine code

## Architecture-Specific Encoding

Each architecture has unique encoding requirements:

### x86 Family (Variable-Length)

x86 instructions have complex, variable-length encoding:

```
[Prefixes] [REX] [Opcode] [ModRM] [SIB] [Displacement] [Immediate]
```

**Challenges**:
- Variable instruction length (1-15 bytes)
- REX prefix for 64-bit operands (x86_64 only)
- ModRM byte for register/memory operands
- SIB byte for complex addressing
- Multiple encoding options for same instruction

**Example**: `mov rax, rbx`
```
48 89 D8
│  │  │
│  │  └─ ModRM byte (11 011 000 = reg, rbx, rax)
│  └──── Opcode (MOV r/m64, r64)
└─────── REX.W prefix (64-bit operand)
```

### ARM Family (Fixed-Length)

ARM instructions are simpler with fixed 4-byte encoding:

```
[31:28] Condition
[27:25] Instruction Type
[24:21] Opcode
[20]    Set Flags
[19:16] First Operand Register
[15:12] Destination Register
[11:0]  Second Operand / Immediate
```

**Benefits**:
- Predictable instruction size
- Simple PC-relative addressing
- Easy branch offset calculation

**Example**: ARM64 `ret`
```
D6 5F 03 C0
│     │  │
└─────┴──┴─ Fixed encoding for RET instruction
```

## Label Resolution

Labels are resolved differently based on usage:

### Absolute Labels (Data References)
```asm
data_label:
    ; data here

mov rax, data_label  ; Absolute address
```

### Relative Labels (Branches)
```asm
loop_start:
    ; code here
    jmp loop_start   ; PC-relative offset
```

**Offset Calculation**:
```
offset = target_address - (current_address + instruction_size)
```

## Instruction Size Calculation

### x86 Family
Size depends on:
- Base opcode (1-3 bytes)
- REX prefix (0-1 bytes)
- ModRM byte (0-1 bytes)
- SIB byte (0-1 bytes)
- Displacement (0, 1, 2, or 4 bytes)
- Immediate (0, 1, 2, 4, or 8 bytes)

### ARM Family
Always 4 bytes per instruction

## Encoding Tables

Each architecture maintains encoding tables:

### x86 Example
```csharp
Dictionary<string, byte> opcodes = new()
{
    ["push"] = 0x50,  // base opcode + reg
    ["pop"]  = 0x58,  // base opcode + reg
    ["ret"]  = 0xC3,
    // ...
};
```

### ARM Example
```csharp
Dictionary<string, uint> opcodes = new()
{
    ["ret"] = 0xD65F03C0,
    ["nop"] = 0xD503201F,
    // ...
};
```

## Operand Encoding

### Register Encoding

Each register maps to a number:

**x86_64**:
- rax = 0, rcx = 1, rdx = 2, rbx = 3
- rsp = 4, rbp = 5, rsi = 6, rdi = 7
- r8-r15 = 8-15

**ARM64**:
- x0-x30 = 0-30
- sp = 31, xzr = 31 (context-dependent)

### Immediate Encoding

Immediates are encoded in little-endian:

**8-bit**: `42` → `2A`  
**16-bit**: `1000` → `E8 03`  
**32-bit**: `1000000` → `40 42 0F 00`

## Example: Complete Encoding

### x86_64: `add rax, 42`

**Assembly**: `add rax, 42`

**Breakdown**:
1. REX.W prefix: `48` (64-bit operand)
2. Opcode: `83` (ADD r/m64, imm8)
3. ModRM: `C0` (register direct, rax)
4. Immediate: `2A` (42 in hex)

**Final**: `48 83 C0 2A`

### ARM64: `add x0, x1, x2`

**Assembly**: `add x0, x1, x2`

**Encoding**:
- Bits [31:21]: `100_0101_1000` (ADD opcode)
- Bits [20:16]: `00010` (x2)
- Bits [15:10]: `000000` (shift amount)
- Bits [9:5]: `00001` (x1)
- Bits [4:0]: `00000` (x0)

**Final**: `8B020020` → `20 00 02 8B` (little-endian)

## Error Handling

The assembler detects and reports:
- Unknown instructions
- Invalid operands
- Undefined labels
- Out-of-range immediates
- Malformed syntax

## Testing

Encoding tests verify exact byte sequences for:
- All supported instructions
- Various operand combinations
- Edge cases (min/max immediates)
- Label resolution accuracy

See: `Testing/AssemblyEncodingTests.cs`
