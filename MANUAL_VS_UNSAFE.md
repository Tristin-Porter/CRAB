# Manual vs Unsafe Blocks in CRAB

## Overview

CRAB supports explicit memory management through code blocks, similar to C#'s `unsafe` keyword. However, CRAB uses `manual` as the primary keyword to better reflect that all such code is formally verified for safety.

## Keywords

### `manual` (Primary)

The `manual` keyword is CRAB's primary way to declare explicit memory management blocks:

```csharp
class Example
{
    public void DoWork()
    {
        manual
        {
            // Explicit memory management code
            // Verified by CRAB's ManualModel
            int* ptr = stackalloc int[10];
            ptr[0] = 42;
        }
    }
}
```

### `unsafe` (Deprecated Alias)

For backward compatibility with C# code, CRAB also supports the `unsafe` keyword as an alias for `manual`. However, using `unsafe` will generate a deprecation warning:

```csharp
class Example
{
    public void DoWork()
    {
        unsafe  // ⚠️ Warning: Use 'manual' instead
        {
            int* ptr = stackalloc int[10];
            ptr[0] = 42;
        }
    }
}
```

**Compiler Output:**
```
Warning: Use of deprecated 'unsafe' keyword detected.
         Please use 'manual' keyword instead for explicit memory management.
         The 'unsafe' keyword is supported for compatibility but may be removed in future versions.
✓ Compilation successful: output.wasm
```

## Why "Manual" Instead of "Unsafe"?

### Traditional C# Unsafe

In traditional C#, the `unsafe` keyword means:
- Runtime checks are disabled
- Memory safety is NOT guaranteed
- Undefined behavior is possible
- Developer takes full responsibility

### CRAB Manual

In CRAB, the `manual` keyword means:
- **Formally verified** for safety by ManualModel
- **Guaranteed** no undefined behavior
- **Proven** no memory leaks or use-after-free
- Explicit memory management with safety proofs

The name "manual" better reflects that you're doing manual memory management with **verification**, not truly "unsafe" code.

## Verification Process

When you use a `manual` block, CRAB's ManualModel performs:

1. **Ownership Graph Construction**
   - Tracks all pointers and their ownership
   - Builds lifetime relationships

2. **Abstract Interpretation**
   - Analyzes all possible execution paths
   - Verifies pointer validity on all paths

3. **Symbolic Execution**
   - Proves no undefined behavior can occur
   - Ensures no memory leaks
   - Guarantees no use-after-free

4. **Compilation**
   - Only succeeds if verification passes
   - Generated code is proven safe

## Usage Examples

### Stack Allocation

```csharp
public int Sum(int count)
{
    manual
    {
        int* numbers = stackalloc int[count];
        for (int i = 0; i < count; i++)
        {
            numbers[i] = i;
        }
        
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += numbers[i];
        }
        return sum;
    }
}
```

### Fixed Buffers

```csharp
public struct FastBuffer
{
    manual
    {
        fixed byte data[256];
    }
    
    public byte Get(int index)
    {
        manual
        {
            fixed (byte* ptr = data)
            {
                return ptr[index];
            }
        }
    }
}
```

### Pointer Arithmetic

```csharp
public void ProcessArray(int* source, int* dest, int length)
{
    manual
    {
        for (int i = 0; i < length; i++)
        {
            *(dest + i) = *(source + i) * 2;
        }
    }
}
```

## Migration Guide

### From C# unsafe to CRAB manual

If you're migrating C# code that uses `unsafe`:

1. **Option 1: Automatic (with warnings)**
   - Keep using `unsafe` keyword
   - CRAB will compile it but emit warnings
   - Works immediately, migrate later

2. **Option 2: Search and Replace**
   ```bash
   # Simple find/replace in your codebase
   find . -name "*.cs" -exec sed -i 's/\bunsafe\b/manual/g' {} \;
   ```

3. **Option 3: Manual Migration**
   - Review each `unsafe` block
   - Replace with `manual`
   - Verify compilation succeeds
   - Check that verification passes

### Example Migration

**Before (C#):**
```csharp
public unsafe void OldCode()
{
    int* ptr = stackalloc int[10];
    *ptr = 42;
}
```

**After (CRAB):**
```csharp
public void NewCode()
{
    manual
    {
        int* ptr = stackalloc int[10];
        *ptr = 42;
    }
}
```

## Compilation Behavior

### With `manual` keyword

```bash
$ crab compile example.cs
✓ Compilation successful: output.wasm
```

### With `unsafe` keyword

```bash
$ crab compile example.cs
Warning: Use of deprecated 'unsafe' keyword detected.
         Please use 'manual' keyword instead for explicit memory management.
✓ Compilation successful: output.wasm
```

## Future Direction

The `unsafe` keyword support may be removed in future CRAB versions. The current timeline is:

- **Current (v1.x)**: Both keywords work, `unsafe` shows warning
- **Future (v2.x)**: May make `unsafe` a compilation error
- **Long-term**: Only `manual` keyword supported

It's recommended to migrate to `manual` as soon as practical.

## Comparison Table

| Aspect | C# `unsafe` | CRAB `manual` |
|--------|-------------|---------------|
| Memory safety | Not guaranteed | Formally verified |
| Undefined behavior | Possible | Proven impossible |
| Memory leaks | Possible | Proven impossible |
| Use-after-free | Possible | Proven impossible |
| Runtime checks | Disabled | Compile-time proofs |
| Verification | None | Abstract interpretation |
| Performance | Fast | Fast + verified |

## Related Features

- **CTGC (Compile-Time Garbage Collection)**: Automatic memory management mode
- **ManualModel**: The verification engine for manual blocks
- **Fixed statements**: Pin managed memory for pointer access
- **Stackalloc**: Allocate memory on the stack

## See Also

- [CRAB Memory Models](MEMORY_MODELS.md)
- [Manual Memory Verification](MANUAL_VERIFICATION.md)
- [CTGC Documentation](CTGC.md)
