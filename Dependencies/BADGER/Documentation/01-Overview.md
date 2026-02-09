# Overview

## What is BADGER?

BADGER (Better Assembler for Dependable Generation of Efficient Results) is a specialized assembler and WAT-lowering backend designed specifically for the CRAB toolchain. Unlike general-purpose assemblers, BADGER is purpose-built to:

1. Accept standard WAT (WebAssembly Text format) as input
2. Lower WAT to architecture-specific assembly
3. Assemble that assembly into executable machine code
4. Emit binaries in Native (bare metal) or PE (Windows) format

## Purpose and Role

BADGER serves as the final compilation stage in the CRAB toolchain. It bridges the gap between platform-independent WAT and executable machine code for specific architectures and environments.

### Key Responsibilities

- **WAT Parsing**: Parse standard WAT using CDTk grammar
- **Architecture Lowering**: Transform stack-based WAT to register/stack-based architectures
- **Assembly Encoding**: Convert assembly text to machine code bytes
- **Container Emission**: Package machine code in appropriate binary formats

## Design Principles

### 1. Sovereignty and Minimalism

BADGER is not a general-purpose assembler. It only supports:
- The WAT instructions and patterns that CRAB emits
- A minimal, canonical assembly dialect for each architecture
- Two container formats: Native and PE

### 2. Predictability

BADGER's behavior is completely deterministic:
- Same WAT input always produces identical output
- No hidden optimizations or transformations
- Explicit, traceable lowering rules

### 3. Modularity

Each architecture is isolated and self-contained:
- Adding new architectures requires no changes to existing ones
- Each architecture defines its own lowering rules
- Shared infrastructure is minimal and well-defined

## Non-Goals

BADGER explicitly does **not**:
- Support arbitrary user-written assembly
- Emit ELF, .deb, or other Linux formats
- Implement its own WAT parser (uses CDTk)
- Support custom WAT dialects
- Perform complex optimizations (optimization happens in CRAB)

## Architecture Support

BADGER supports five target architectures:

1. **x86_64** (64-bit x86, primary architecture)
2. **x86_32** (32-bit x86)
3. **x86_16** (16-bit x86 for real mode)
4. **ARM64** (64-bit ARM)
5. **ARM32** (32-bit ARM)

## Container Formats

BADGER emits two container formats:

1. **Native**: Flat binary with no headers, for bare metal or bootloaders
2. **PE**: Minimal Portable Executable for Windows

## Implementation Language

BADGER is implemented entirely in C#. No other languages are used in the implementation, though build scripts may exist for automation.
