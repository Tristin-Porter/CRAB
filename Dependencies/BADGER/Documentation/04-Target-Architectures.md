# Target Architectures

BADGER supports five target architectures, each with its own lowering rules, instruction encoding, and register conventions.

## Supported Architectures

1. **[x86_64](05-x86_64.md)** - 64-bit x86 (primary architecture)
2. **[x86_32](06-x86_32.md)** - 32-bit x86
3. **[x86_16](07-x86_16.md)** - 16-bit x86 (real mode)
4. **[ARM64](08-ARM64.md)** - 64-bit ARM (AArch64)
5. **[ARM32](09-ARM32.md)** - 32-bit ARM

## Architecture Selection

Specify target architecture using the `--arch` flag:

```bash
dotnet run input.wat -o output.bin --arch x86_64
dotnet run input.wat -o output.bin --arch arm64
```

## Common Lowering Patterns

All architectures implement similar lowering patterns:

### Stack Simulation

WAT uses a stack-based execution model. Each architecture simulates this using:
- Registers for first N stack slots (fastest)
- Memory spills for additional values

### Function Prologue/Epilogue

Each architecture generates appropriate prologue/epilogue code:
- Save callee-saved registers
- Allocate stack frame
- Set up base pointer
- Restore registers and return

### Instruction Selection

WAT instructions are mapped to native instructions:
- `i32.add` → `add` instruction
- `i32.const` → `mov` with immediate
- `local.get` → `mov` from stack slot or register
- Control flow → conditional/unconditional branches

### Control Flow

Block structures are lowered to labels and branches:
- `block` → labels for block entry/exit
- `loop` → backward branch target
- `br` → unconditional branch
- `br_if` → conditional branch

## Architecture-Specific Features

### x86 Family (x86_64, x86_32, x86_16)

**Common Features**:
- CISC instruction set
- Complex addressing modes
- Variable-length instruction encoding
- ModRM/SIB byte encoding

**Differences**:
- Register sizes (64-bit, 32-bit, 16-bit)
- Available registers (r8-r15 only in x86_64)
- Address size prefixes

### ARM Family (ARM64, ARM32)

**Common Features**:
- RISC instruction set
- Fixed-width instructions (4 bytes)
- Load/store architecture
- Conditional execution

**Differences**:
- Register count (31 vs 16 general-purpose)
- Register naming (x0-x30 vs r0-r15)
- Instruction encoding details

## Modularity

Each architecture is implemented in a separate file with two main components:

1. **Lowering (`WATToXXXMapSet`)**: WAT → Assembly transformation
2. **Assembler (`Assembler.Assemble`)**: Assembly → Machine code

This separation ensures:
- New architectures can be added without modifying existing ones
- Each architecture maintains its own encoding tables
- Shared logic is minimal

## Testing

Each architecture has comprehensive tests covering:
- Prologue/epilogue generation
- Instruction encoding
- Assembly generation
- End-to-end compilation

## Performance Considerations

### Primary Architecture

x86_64 is the primary and most optimized architecture. It receives:
- Most thorough testing
- Best register allocation
- Most complete instruction coverage

### Other Architectures

Other architectures are fully functional but may have:
- Simpler register allocation
- Subset of possible optimizations
- Focus on correctness over performance

This is intentional - BADGER prioritizes predictability and correctness.
