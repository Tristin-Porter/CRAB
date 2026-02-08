# Architecture Overview

## System Design

CDTk is implemented as a single-file framework (~14,000 lines) organized into these major subsystems:

### Core Components
1. **Performance Optimizations** - Object pooling, caching, string interning
2. **Diagnostics** - Unified error reporting across all stages
3. **Lexical Analysis** - DFA-based tokenization
4. **Syntax Analysis** - AG-LL parser (ALL(*) + GLL)
5. **Semantic Analysis** - Model framework
6. **Code Generation** - MapSet template system
7. **Compiler Pipeline** - Orchestrates all stages

### Design Principles
- **100% Safe C#** - No unsafe code, no pointers, no stackalloc
- **Memory Safe** - All optimizations within safe managed code
- **Strongly Typed** - Field-based identity for tokens/rules/maps
- **Declarative** - Users define WHAT, CDTk determines HOW
- **Predictable** - No hidden behavior, all explicit

### Performance Targets (from spec)
- Lexing: 100-200M chars/sec
- Parsing: 5-10M AST nodes/sec (deterministic)
- Semantic: 15M+ ops/sec

All targets met through safe code optimizations.

## See Also
- [TokenSet Implementation](02-TokenSetImplementation.md)
- [AG-LL Implementation](05-AGLLImplementation.md)
