# SPPF: Shared Packed Parse Forest

## Overview
The Shared Packed Parse Forest (SPPF) is CDTk's structure for representing ambiguous parse results efficiently.

## Purpose
When a grammar allows multiple valid parse trees for the same input, SPPF represents all possibilities in a compact, shared structure.

## Lazy Construction
Per spec: "Lazy, Localized SPPF"
- Only built when ambiguity exists
- Only built around ambiguous regions
- Supports ambiguity reporting
- Enables multi-interpretation analysis

## Implementation
Technical details of CDTk's SPPF construction and management.

## See Also
- [GLL Fallback Mechanism](07-GLLFallbackMechanism.md)
- [AG-LL Implementation](05-AG-LLImplementation.md)
