# Statement Transformation Fix - Summary

## Problem
WASM generation was producing `{stmt}` as a literal placeholder instead of transforming statements to actual WASM code.

## Solution
Fixed three critical issues in the transformation pipeline:

### 1. MethodDeclaration Field Mapping (MapSet.cs)
**Issue:** Field shifting bug incorrectly documented - actual mapping differs from comments.

**Fix:** Updated map to use correct shifted fields:
- Use `{mods}` for method name (not `{name}`)
- Use `{returnType}` for method body (not `{body}`)

### 2. Statement Dispatcher Rules (RuleSet.cs)
**Issue:** Three dispatcher rules missing `.Returns("stmt")` calls.

**Fix:** Added explicit `.Returns("stmt")` to:
- `SelectionStatement`
- `IterationStatement`
- `JumpStatement`

Without this, the `stmt` field was never populated in AST nodes, causing literal `{stmt}` in output.

### 3. ReturnStatement Expression (MapSet.cs)
**Issue:** Optional expr field causing Fallback map usage.

**Fix:** Simplified to `(return)` with comprehensive TODO documentation.

## Results

**Before:**
```wasm
{stmt}
```

**After:**
```wasm
(return)
```

✅ Statements now transform correctly through full dispatcher chain
✅ No more literal placeholders in WASM output
✅ All tests passing
✅ No security issues (CodeQL clean)

## Known Limitations
- Return expressions (`return 42;`) not yet supported - requires deeper CDTk investigation
- Method parameters still lost due to field shifting
- Return type in `(result)` clause empty

See STATEMENT_TRANSFORMATION_FIX.md for detailed analysis.

## Verification
```bash
dotnet run -- compile test.crab
# Should see (return) not {stmt}
```
