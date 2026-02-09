# Architecture

## High-Level Pipeline

BADGER's compilation pipeline consists of four sequential stages:

```
WAT Input → Parsing → Lowering → Encoding → Container Emission
```

### Stage 1: WAT Parsing

**Input**: WAT text (standard WebAssembly Text format)  
**Tool**: CDTk with WAT grammar  
**Output**: WAT AST/model (modules, functions, locals, blocks, instructions)

The WAT parser is built using CDTk, a modern compiler toolkit for C#. It defines:
- Complete token set for all WAT keywords and instructions
- Grammar rules for module structure, functions, control flow
- AST representation of parsed WAT

See: [WAT Parsing](03-WAT-Parsing.md)

### Stage 2: WAT → Architecture Lowering

**Input**: WAT model  
**Output**: Architecture-specific assembly (plain text)

Each architecture implements lowering logic that:
- Maps stack-based WAT semantics to register/stack architecture
- Builds control-flow graphs and basic blocks
- Assigns locals to registers and/or stack slots
- Selects appropriate instructions for each WAT operation
- Generates function prologue/epilogue

The lowering layer is architecture-specific and modular. Each architecture defines its own:
- Register allocation strategy
- Calling convention
- Stack frame layout
- Instruction selection rules

See: [Target Architectures](04-Target-Architectures.md)

### Stage 3: Assembly Encoding

**Input**: Canonical assembly dialect (plain text)  
**Output**: Raw machine code bytes

The assembler component:
- Tokenizes assembly text
- Parses instructions and operands
- Maintains a symbol table for labels
- Performs two-pass assembly:
  - **Pass 1**: Compute label addresses and instruction sizes
  - **Pass 2**: Encode instructions and patch branch/call offsets

Each architecture defines:
- Opcode tables
- Encoding rules (ModRM/SIB for x86, etc.)
- Immediate and displacement formats
- Endianness

See: [Assembly Encoding](10-Assembly-Encoding.md)

### Stage 4: Container Emission

**Input**: Machine code bytes  
**Output**: Native flat binary or PE binary

The container emitter packages machine code appropriately:

**Native Format**:
- Raw machine code with no headers
- Entrypoint at offset 0
- For bootloaders, QEMU, or SHARK

**PE Format**:
- Minimal DOS stub and PE headers
- Single code section
- Defined entrypoint
- For Windows execution

See: [Container Formats](11-Container-Formats.md)

## Component Organization

```
BADGER/
├── Program.cs              # Main entry point and CLI
├── Architectures/          # Architecture-specific lowering and encoding
│   ├── x86_64.cs
│   ├── x86_32.cs
│   ├── x86_16.cs
│   ├── ARM64.cs
│   └── ARM32.cs
├── Containers/             # Binary format emitters
│   ├── Native.cs
│   └── PE.cs
├── Testing/                # Comprehensive test suite
│   ├── TestRunner.cs
│   ├── WATParserTests.cs
│   ├── LoweringTests.cs
│   ├── AssemblyEncodingTests.cs
│   ├── ContainerTests.cs
│   └── IntegrationTests.cs
└── Dependencies/           # CDTk and documentation
    ├── Boilerplate/
    └── Documentation/
```

## Data Flow

1. **User provides**: WAT file + architecture choice + container format
2. **Parser produces**: WAT AST
3. **Lowering produces**: Assembly text for target architecture
4. **Encoder produces**: Machine code bytes
5. **Container emitter produces**: Final binary file

## Extension Points

To add a new architecture:

1. Create `Architectures/NewArch.cs`
2. Implement `WATToNewArchMapSet : MapSet` for lowering
3. Implement `Assembler` class with `Assemble(string) → byte[]` method
4. Add architecture to Program.cs switch statement
5. Add tests to Testing/ folder

To add a new container format:

1. Create `Containers/NewFormat.cs`
2. Implement `Emit(byte[] machineCode) → byte[]` method
3. Add format to Program.cs switch statement
4. Add tests to Testing/ folder
