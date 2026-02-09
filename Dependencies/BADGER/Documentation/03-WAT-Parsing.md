# WAT Parsing

## Overview

BADGER uses CDTk (Compiler Description Toolkit) to parse standard WebAssembly Text format (WAT). The parser implementation is in `Program.cs` and defines:

1. Complete token set (`WATTokens`)
2. Complete grammar rules (`WATRules`)

## Token Definitions

BADGER defines tokens for all standard WAT constructs:

### Module Structure Tokens
- `module`, `func`, `param`, `result`, `local`
- `type`, `import`, `export`
- `memory`, `data`, `table`, `elem`
- `global`, `mut`, `start`

### Control Flow Tokens
- `block`, `loop`, `if`, `then`, `else`, `end`
- `br`, `br_if`, `br_table`, `return`
- `call`, `call_indirect`

### Variable Instructions
- `local.get`, `local.set`, `local.tee`
- `global.get`, `global.set`

### Numeric Instructions (i32)
- `i32.const`, `i32.add`, `i32.sub`, `i32.mul`
- `i32.div_s`, `i32.div_u`, `i32.rem_s`, `i32.rem_u`
- `i32.and`, `i32.or`, `i32.xor`, `i32.shl`, `i32.shr_s`, `i32.shr_u`
- `i32.eq`, `i32.ne`, `i32.lt_s`, `i32.lt_u`, `i32.gt_s`, `i32.gt_u`
- And many more...

### Numeric Instructions (i64, f32, f64)
Similar complete sets for i64, f32, and f64 types.

### Memory Instructions
- `i32.load`, `i64.load`, `f32.load`, `f64.load`
- `i32.store`, `i64.store`, `f32.store`, `f64.store`
- Load/store variants for different sizes and signedness
- `memory.size`, `memory.grow`

### Parametric Instructions
- `drop`, `select`, `nop`, `unreachable`

### Value Types
- `i32`, `i64`, `f32`, `f64`, `funcref`, `externref`

### Literals
- Identifiers: `$[a-zA-Z_][a-zA-Z0-9_]*`
- Integers: decimal and hexadecimal
- Floats: decimal and hexadecimal
- Strings

## Grammar Rules

BADGER defines complete grammar rules for WAT structure:

### Module Structure
```
Module: (module id? ModuleField*)
ModuleField: FunctionDef | TypeDef | Import | Export | ...
```

### Function Definitions
```
FunctionDef: (func id? TypeUse? Param* Result* Local* Instruction*)
Param: (param id? ValueType)
Result: (result ValueType)
Local: (local id? ValueType)
```

### Instructions
```
Instruction: ControlInstr | NumericInstr | VariableInstr | MemoryInstr | ParametricInstr

ControlInstr: Block | Loop | If | Br | BrIf | ...
NumericInstr: I32Instr | I64Instr | F32Instr | F64Instr | ConversionInstr
VariableInstr: local.get | local.set | global.get | ...
```

## Usage in BADGER

Currently, BADGER scaffolds the complete WAT grammar but the lowering pipeline generates assembly directly for demonstration purposes. The full CDTk pipeline with WAT parsing, AST construction, and lowering is ready to be integrated.

### Example WAT

```wasm
(module
  (func $add (param $a i32) (param $b i32) (result i32)
    local.get $a
    local.get $b
    i32.add
  )
  (export "add" (func $add))
)
```

## Future Integration

The complete WAT grammar is defined and ready. Future work will:
1. Parse WAT using CDTk
2. Build complete AST
3. Walk AST to generate architecture-specific assembly
4. Replace the current direct assembly generation

## WAT Specification Compliance

BADGER follows the official WebAssembly text format specification:
- No custom dialect
- No extensions or modifications
- Standard semantics preserved

## Testing

WAT parser tests verify:
- Token definitions are complete
- Grammar rules are defined
- All WAT constructs can be represented

See: [Testing](15-Testing.md)
