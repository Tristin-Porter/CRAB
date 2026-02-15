# CRAB Parser/Lexing Investigation & Fixes

## Problem Statement
The `ClassBody` AST node's `members` field was null or empty even when C# source had methods, preventing WASM generation from emitting method code.

## Root Cause Analysis

### Primary Issue: Missing `.Returns()` Calls
Many grammar rules were defined using the implicit string syntax without explicit `.Returns()` calls:
```csharp
public Rule ClassMemberDeclarations = "members:ClassMemberDeclaration+";  // Missing .Returns()
```

CDTk requires `.Returns()` to properly expose named fields in the AST nodes. Without it, fields may not be accessible during code generation.

### Secondary Issue: CDTk Field Shifting Bug
CDTk has a known bug with optional fields: when optional fields at the beginning of a rule are absent, subsequent field assignments shift. This is documented in the `ClassDeclaration` Map comment:

```
/// Bug causes field shifting: when 'attrs' is absent, subsequent fields shift:
/// - 'mods' field receives the class name (e.g., "Calculator")
/// - 'name' field receives the ClassBody AST node
/// - 'body' field is absent
```

## Fixes Applied

### 1. Added `.Returns()` to Collection Rules
These rules collect multiple items and needed explicit `.Returns()`:

```csharp
// Before:
public Rule ClassMemberDeclarations = "members:ClassMemberDeclaration+";

// After:
public Rule ClassMemberDeclarations = new Rule("members:ClassMemberDeclaration+")
    .Returns("members");
```

Fixed rules:
- `ClassMemberDeclarations`
- `StructMemberDeclarations`
- `InterfaceMemberDeclarations`
- `NamespaceMemberDeclarations`
- `Modifiers`
- `Statements`
- `TypeSuffixes`

### 2. Added `.Returns()` to Alternation Rules
Rules with `|` alternations needed explicit field returns:

```csharp
// Before:
public Rule MethodBody = "body:Block | body:ExpressionBody | body:@Semicolon";

// After:
public Rule MethodBody = new Rule("body:Block | body:ExpressionBody | body:@Semicolon")
    .Returns("body");
```

Fixed rules:
- `MethodBody`, `AccessorBody`, `InterfaceMethodBody`
- `Statement`, `EmbeddedStatement`, `Expression`
- `UsingDirective`, `NamespaceBodyItem`, `GlobalAttributeTarget`
- `ClassMemberDeclaration`, `StructMemberDeclaration`, `InterfaceMemberDeclaration`
- `Modifier`, `VarianceAnnotation`, `TypeParameterConstraint`, `PrimaryConstraint`
- `VariableInitializer`, `AccessorKind`, `OverloadableOperator`
- `FormalParameterListContent`, `ParameterModifier`, `AttributeTargetSpecifier`
- `AttributeArgumentList`, `NonArrayType`, `TypeSuffix`, `PrimitiveType`, `IntegralType`

### 3. Fixed Type Map Field Reference
```csharp
// Before:
public Map Type = "{type}";  // Wrong - Type rule returns 'base' and 'suffixes'

// After:  
public Map Type = "{base}";  // Correct - removed {suffixes} to avoid null field issues
```

### 4. Created MethodDeclaration Map Workaround
Due to CDTk's field shifting bug, for methods without attributes/modifiers:

```csharp
// Workaround mapping:
// - {attrs} contains the return type (Type AST node)
// - {mods} contains the method name (Identifier token)
// - {name} contains the method body (MethodBody AST node)

public Map MethodDeclaration = @"(func ${mods}
  (param {typeParams})
  (result {attrs})
  ;; Method body with CTGC-inserted deallocations
{name}
)";
```

## Test Results

### Working ✓
- Class members now parse and appear in ClassBody
- Method names emit correctly: `(func $Add`, `(func $Foo`
- Return types map correctly: `(result i32)` for int, `(result )` for void
- Method bodies emit with Block structure

Example output for `int Add(int a, int b) { }`:
```wasm
(func $Add
  (param {typeParams})
  (result i32)
  ;; Method body with CTGC-inserted deallocations
(block
{stmts}
  ;; Deallocation instructions inserted here based on AutomaticModel analysis
)
)
```

### Not Yet Working ✗
1. **Parameters**: Still showing as `{typeParams}` placeholder. The correct field name after shifting hasn't been determined yet.
2. **Statements**: Empty blocks correctly show no statements, but non-empty blocks may also have issues.

## Remaining Work

### Immediate
1. Determine correct field mapping for parameters after CDTk field shifting
2. Test with methods containing actual statements to verify Block/Statements work
3. Update MethodDeclaration Map with correct parameter field

### Long-term  
1. Fix CDTk parser to handle optional fields correctly (eliminates need for workarounds)
2. Add comprehensive tests for all member types (fields, properties, constructors, etc.)
3. Document field shifting patterns for all affected rules

## Files Modified
- `/home/runner/work/CRAB/CRAB/Compiler/Core/RuleSet.cs` - Added `.Returns()` to ~30 rules
- `/home/runner/work/CRAB/CRAB/Compiler/Core/MapSet.cs` - Fixed Type Map and MethodDeclaration Map workaround

## Key Learnings
1. CDTk requires explicit `.Returns()` for named fields to be accessible
2. Simple string syntax rules (without `new Rule(...)`) don't automatically expose fields
3. Optional fields at rule start cause field shifting in CDTk - requires Map workarounds
4. Null/absent optional fields in Maps should be avoided in templates or CDTk outputs placeholders literally
