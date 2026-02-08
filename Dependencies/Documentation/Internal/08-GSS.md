# GSS: Graph-Structured Stack

## Overview
The Graph-Structured Stack (GSS) is a key component of the GLL parser, enabling efficient handling of non-determinism and backtracking.

## Purpose
Unlike a traditional call stack, the GSS allows multiple parse paths to share common stack frames, dramatically reducing memory usage during non-deterministic parsing.

## Implementation
Technical details of CDTk's GSS implementation for the GLL fallback mechanism.

## Performance Characteristics
- Memory efficiency through frame sharing
- Tail-call optimization
- Lazy construction

## See Also
- [GLL Fallback Mechanism](07-GLLFallbackMechanism.md)
- [AG-LL Implementation](05-AG-LLImplementation.md)
