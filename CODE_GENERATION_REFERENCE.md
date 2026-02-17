# CRAB Compiler - Code Generation Technical Reference

## Overview

This document provides detailed technical information about how CRAB generates code in different formats: WAT (WebAssembly Text), WASM (WebAssembly Binary), and PE (Portable Executable).

---

## Table of Contents

1. [WAT Generation Pipeline](#wat-generation-pipeline)
2. [WASM Binary Generation](#wasm-binary-generation)
3. [PE Format Generation](#pe-format-generation)
4. [Code Examples](#code-examples)
5. [Technical Details](#technical-details)

---

## WAT Generation Pipeline

### Location & Entry Point

**File**: `Compiler/Core/MapSet.cs`  
**Class**: `WASM : MapSet` (line 1987)  
**Entry Method**: `CompilationUnit` map (line 2092)

### Pipeline Flow

```
C# AST (from CDTk)
    ↓
WASM.CompilationUnit map
    ↓
Process each AST node type
    ↓
WasmEmit.EmitExpression()
    ↓
Generate WAT instructions
    ↓
Combine into complete module
    ↓
WAT text output
```

### Key Components

#### 1. StringRegistry (Line 10-48)

Manages string literals for the data section:

```csharp
public static class StringRegistry
{
    private static Dictionary<int, string> strings = new();
    private static Dictionary<int, int> offsets = new();
    private static int nextId = 0;
    private static int currentOffset = 0;
    
    public static int RegisterString(string text)
    {
        var id = nextId++;
        strings[id] = text;
        offsets[id] = currentOffset;
        currentOffset += text.Length + 1; // +1 for null terminator
        return id;
    }
}
```

**Purpose**: Track all string literals and their memory locations  
**Used by**: Data section generation, string literal emission

#### 2. LocalVariableRegistry (Line 54-93)

Manages function-local variables:

```csharp
public static class LocalVariableRegistry
{
    private static Dictionary<string, string> variables = new();
    
    public static void RegisterVariable(string name, string wasmType)
    {
        if (!variables.ContainsKey(name))
        {
            variables[name] = wasmType;
            currentFunctionVars.Add(name);
        }
    }
}
```

**Purpose**: Track variable names and WASM types (i32, i64, f32, f64)  
**Used by**: Local variable declarations, variable access

#### 3. WasmEmit Static Helper (Line 100+)

Core WAT instruction emitter:

```csharp
public static class WasmEmit
{
    public static string EmitExpression(object? exprNode)
    {
        if (!(exprNode is AstNode node))
            return "";
        
        return node.Type switch
        {
            "DecimalIntegerLiteral" => EmitIntegerLiteral(node),
            "StringLiteral" => EmitStringLiteral(node),
            "AdditiveExpression" => EmitBinaryExpression(node, "+"),
            "InvocationExpression" => EmitInvocationExpression(node),
            // ... 40+ more cases
        };
    }
}
```

**Purpose**: Convert C# AST nodes to WAT instructions  
**Used by**: All code generation maps

### WAT Module Structure

The generated WAT follows this structure:

```wat
(module
  ;; 1. IMPORTS
  (import "env" "memory" (memory 1))
  (import "env" "console_log" (func $console_log (param i32) (param i32)))
  (import "env" "console_readkey" (func $console_readkey (result i32)))
  
  ;; 2. GLOBALS
  (global $heap_ptr (mut i32) (i32.const <calculated_start>))
  
  ;; 3. HELPER FUNCTIONS
  (func $alloc (param $size i32) (result i32) ...)
  (func $string_concat (param i32 i32 i32 i32) (result i32 i32) ...)
  (func $int_to_string (param i32) (result i32 i32) ...)
  
  ;; 4. DATA SECTION
  (data (i32.const 0) "Hello World!\00")
  (data (i32.const 13) "Another string\00")
  
  ;; 5. USER FUNCTIONS
  (func $Main ...)
  (func $Add (param $a i32) (param $b i32) (result i32) ...)
  
  ;; 6. EXPORTS
  (export "main" (func $Main))
)
```

### Code Generation Examples

#### Example 1: Integer Literal

**C# Input**:
```csharp
int x = 42;
```

**AST Node**: `DecimalIntegerLiteral { lexeme: "42" }`

**WAT Output**:
```wat
i32.const 42
```

**Code** (MapSet.cs, EmitIntegerLiteral):
```csharp
private static string EmitIntegerLiteral(AstNode node)
{
    var lexeme = node.Fields.ContainsKey("lexeme") 
        ? node.Fields["lexeme"]?.ToString() ?? "0" 
        : "0";
    return $"i32.const {lexeme}";
}
```

#### Example 2: String Literal

**C# Input**:
```csharp
string s = "Hello";
```

**AST Node**: `StringLiteral { lexeme: "\"Hello\"" }`

**WAT Output**:
```wat
;; string "Hello" at offset 0, length 5
i32.const 0
i32.const 5
```

**Code** (MapSet.cs, EmitStringLiteral):
```csharp
private static string EmitStringLiteral(AstNode node)
{
    var lexeme = node.Fields["lexeme"]?.ToString() ?? "";
    var text = lexeme.Trim('"').Replace("\\n", "\n").Replace("\\t", "\t");
    
    int stringId = StringRegistry.RegisterString(text);
    int offset = StringRegistry.GetStringOffset(stringId);
    
    return $";; string \"{text}\" at offset {offset}, length {text.Length}\n" +
           $"i32.const {offset}\n" +
           $"i32.const {text.Length}";
}
```

**Data Section Entry**:
```wat
(data (i32.const 0) "Hello\00")
```

#### Example 3: Binary Expression (Addition)

**C# Input**:
```csharp
int result = 21 + 21;
```

**AST Node**: 
```
AdditiveExpression {
  left: DecimalIntegerLiteral { lexeme: "21" },
  op: AdditiveOperator { lexeme: "+" },
  right: DecimalIntegerLiteral { lexeme: "21" }
}
```

**WAT Output**:
```wat
i32.const 21
i32.const 21
i32.add
```

**Code** (MapSet.cs, EmitBinaryExpression):
```csharp
private static string EmitBinaryExpression(AstNode node, string defaultOp)
{
    var left = node.Fields["left"];
    var right = node.Fields["right"];
    var op = defaultOp;
    
    // Extract operator if present
    if (node.Fields.ContainsKey("op") && node.Fields["op"] is AstNode opNode)
    {
        op = opNode.Fields["lexeme"]?.ToString() ?? defaultOp;
    }
    
    // Emit left operand, right operand, then operator
    var leftCode = EmitExpression(left);
    var rightCode = EmitExpression(right);
    var opCode = MapOperator(op);
    
    return $"{leftCode}\n{rightCode}\n{opCode}";
}

private static string MapOperator(string op)
{
    return op switch
    {
        "+" => "i32.add",
        "-" => "i32.sub",
        "*" => "i32.mul",
        "/" => "i32.div_s",
        "%" => "i32.rem_s",
        _ => "i32.add"
    };
}
```

#### Example 4: Method Call

**C# Input**:
```csharp
Console.WriteLine("Test");
```

**AST Node**:
```
InvocationExpression {
  target: MemberAccessExpression {
    base: Identifier { lexeme: "Console" },
    member: Identifier { lexeme: "WriteLine" }
  },
  arguments: [ StringLiteral { lexeme: "\"Test\"" } ]
}
```

**WAT Output**:
```wat
;; string "Test" at offset 0, length 4
i32.const 0
i32.const 4
call $console_log
```

**Code** (MapSet.cs, EmitInvocationExpression):
```csharp
private static string EmitInvocationExpression(AstNode node)
{
    // Extract method name
    var methodName = ExtractMethodName(node);
    
    if (methodName == "Console.WriteLine")
    {
        // Get arguments
        var args = ExtractArguments(node);
        if (args.Count > 0)
        {
            var argCode = EmitExpression(args[0]);
            return $"{argCode}\ncall $console_log";
        }
        return "call $console_log";
    }
    
    // ... other method cases
}
```

#### Example 5: String Concatenation

**C# Input**:
```csharp
string result = "Hello " + "World";
```

**AST Node**:
```
AdditiveExpression {
  left: StringLiteral { lexeme: "\"Hello \"" },
  op: AdditiveOperator { lexeme: "+" },
  right: StringLiteral { lexeme: "\"World\"" }
}
```

**WAT Output**:
```wat
;; string "Hello " at offset 0, length 6
i32.const 0
i32.const 6
;; string "World" at offset 7, length 5
i32.const 7
i32.const 5
call $string_concat
```

**Code**: Uses type inference to detect string operations:

```csharp
private static bool IsStringExpression(object? expr)
{
    if (expr is AstNode node)
    {
        if (node.Type == "StringLiteral")
            return true;
        
        // Check for string operations
        if (node.Type == "InvocationExpression")
        {
            var methodName = ExtractMethodName(node);
            if (methodName == "string.Concat")
                return true;
        }
    }
    return false;
}
```

### Helper Functions

The compiler generates these helper functions in every module:

#### 1. Memory Allocator ($alloc)

```wat
(func $alloc (param $size i32) (result i32)
  (local $ptr i32)
  global.get $heap_ptr
  local.set $ptr
  global.get $heap_ptr
  local.get $size
  i32.add
  global.set $heap_ptr
  local.get $ptr
)
```

**Purpose**: Bump allocator for dynamic memory  
**Algorithm**: Return current heap pointer, advance by size  
**Used by**: String concatenation, object allocation

#### 2. String Concatenation ($string_concat)

```wat
(func $string_concat 
  (param $left_ptr i32) (param $left_len i32) 
  (param $right_ptr i32) (param $right_len i32) 
  (result i32) (result i32)
  
  ;; Allocate new string
  local.get $left_len
  local.get $right_len
  i32.add
  call $alloc
  
  ;; Copy left string (loop omitted for brevity)
  ;; Copy right string (loop omitted for brevity)
  
  ;; Return (new_ptr, total_len)
)
```

**Purpose**: Concatenate two strings  
**Algorithm**: Allocate new buffer, copy both strings, return (ptr, len)  
**Returns**: Multi-value return (pointer, length)

#### 3. Integer to String ($int_to_string)

```wat
(func $int_to_string (param $value i32) (result i32) (result i32)
  ;; Handle zero case
  ;; Handle negative numbers
  ;; Count digits
  ;; Allocate memory
  ;; Convert digits to ASCII
  ;; Return (ptr, len)
)
```

**Purpose**: Convert integer to string  
**Algorithm**: 
1. Special case for zero
2. Handle negative (add '-' prefix)
3. Count digits (divide by 10 repeatedly)
4. Allocate buffer
5. Fill buffer right-to-left (digit % 10 + '0')

**Returns**: Multi-value return (pointer, length)

### Data Section Generation

**Code** (MapSet.cs, GenerateDataSection):

```csharp
public static string GenerateDataSection()
{
    var sb = new StringBuilder();
    
    foreach (var kvp in StringRegistry.GetAllStrings())
    {
        int stringId = kvp.Key;
        string text = kvp.Value;
        int offset = StringRegistry.GetStringOffset(stringId);
        
        // Escape special characters
        string escaped = text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
        
        sb.AppendLine($"  (data (i32.const {offset}) \"{escaped}\\00\")");
    }
    
    return sb.ToString();
}
```

**Output Example**:
```wat
(data (i32.const 0) "Hello World!\00")
(data (i32.const 13) "Press any key\00")
(data (i32.const 27) "Result: \00")
```

### Heap Initialization

**Code** (MapSet.cs, CompilationUnit):

```csharp
// Calculate heap start after all string literals
int heapStart = StringRegistry.GetAllStrings().Count > 0 
    ? StringRegistry.GetAllStrings()
        .Max(kvp => StringRegistry.GetStringOffset(kvp.Key) + kvp.Value.Length + 1)
    : 0;

// Align to 4-byte boundary
heapStart = (heapStart + 3) & ~3;

sb.AppendLine($"  (global $heap_ptr (mut i32) (i32.const {heapStart}))");
```

**Purpose**: Initialize heap pointer after string data to prevent corruption  
**Algorithm**: Find max string offset, add length, align to 4 bytes

---

## WASM Binary Generation

### Location & Entry Point

**File**: `Dependencies/BADGER/Containers/WasmJS.cs`  
**Class**: `WasmJS` (static)  
**Entry Method**: `Emit(string watText, string wasmFileName)`

### Binary Format

WASM uses a binary format with the following structure:

```
┌─────────────────────────────┐
│ Magic Number: 0x00 61 73 6D │  "\0asm"
├─────────────────────────────┤
│ Version: 0x01 00 00 00      │  Version 1
├─────────────────────────────┤
│ Type Section (ID: 1)        │  Function signatures
├─────────────────────────────┤
│ Import Section (ID: 2)      │  Imports from env
├─────────────────────────────┤
│ Function Section (ID: 3)    │  Function type indices
├─────────────────────────────┤
│ Global Section (ID: 6)      │  Global variables
├─────────────────────────────┤
│ Export Section (ID: 7)      │  Exported functions
├─────────────────────────────┤
│ Code Section (ID: 10)       │  Function bodies
├─────────────────────────────┤
│ Data Section (ID: 11)       │  String literals
└─────────────────────────────┘
```

### Conversion Process

**Code** (WasmJS.cs, ConvertWatToWasm):

```csharp
private static byte[] ConvertWatToWasm(string watText)
{
    var wasm = new List<byte>();
    
    // Magic number "\0asm"
    wasm.AddRange(new byte[] { 0x00, 0x61, 0x73, 0x6D });
    
    // Version 1
    wasm.AddRange(new byte[] { 0x01, 0x00, 0x00, 0x00 });
    
    // Parse WAT to extract module info
    var moduleInfo = ParseWatModule(watText);
    
    // Emit sections in order
    if (moduleInfo.TypeSection.Count > 0)
        EmitTypeSection(wasm, moduleInfo.TypeSection);
    
    if (moduleInfo.ImportSection.Count > 0)
        EmitImportSection(wasm, moduleInfo.ImportSection);
    
    // ... more sections
    
    return wasm.ToArray();
}
```

### Section Encoding

Each section has this format:

```
┌────────────────┐
│ Section ID (1) │  1 byte: type of section
├────────────────┤
│ Size (ULEB128) │  Variable: size of payload
├────────────────┤
│ Payload        │  N bytes: section content
└────────────────┘
```

**Example**: Type Section

```csharp
private static void EmitTypeSection(List<byte> wasm, List<FunctionType> types)
{
    var sectionData = new List<byte>();
    
    // Count of types
    WriteULEB128(sectionData, (uint)types.Count);
    
    foreach (var type in types)
    {
        sectionData.Add(0x60);  // func type
        
        // Parameters
        WriteULEB128(sectionData, (uint)type.Parameters.Count);
        foreach (var param in type.Parameters)
            sectionData.Add(GetValueTypeByte(param));
        
        // Results
        WriteULEB128(sectionData, (uint)type.Results.Count);
        foreach (var result in type.Results)
            sectionData.Add(GetValueTypeByte(result));
    }
    
    // Write section: ID=1, size, data
    wasm.Add(1);
    WriteULEB128(wasm, (uint)sectionData.Count);
    wasm.AddRange(sectionData);
}
```

### JavaScript Wrapper

**Code** (WasmJS.cs, GenerateJavaScriptWrapper):

```javascript
// Generated JavaScript
const fs = require('fs');
const wasmBuffer = fs.readFileSync('output.wasm');

const memory = new WebAssembly.Memory({ initial: 1 });

const importObject = {
  env: {
    memory: memory,
    console_log: (ptr, len) => {
      const bytes = new Uint8Array(memory.buffer, ptr, len);
      const text = new TextDecoder().decode(bytes);
      console.log(text);
    },
    console_readkey: () => {
      // Stub - returns 0
      return 0;
    }
  }
};

WebAssembly.instantiate(wasmBuffer, importObject).then(result => {
  result.instance.exports.main();
});
```

### Known Issues

⚠️ **S-Expression Parser Bug**: The BADGER WAT parser has issues with nested structures.

**Problem**:
```wat
(func $example
  (local $x i32)    ;; Parser sees this as local declaration...
  local.get $x      ;; ...but treats "local" here as instruction!
)
```

**Workaround**: Use external tools like WABT:
```bash
wat2wasm /tmp/debug_output.wat -o output.wasm
```

---

## PE Format Generation

### Location & Entry Point

**File**: `Dependencies/BADGER/Containers/PE.cs`  
**Class**: `PE` (static)  
**Entry Method**: `Emit(byte[] machineCode)`

### PE Structure

The Portable Executable format has this structure:

```
┌─────────────────────────────────┐
│ DOS Header (64 bytes)           │  MZ header
├─────────────────────────────────┤
│ DOS Stub (64 bytes)             │  "This program cannot..."
├─────────────────────────────────┤
│ PE Signature (4 bytes)          │  "PE\0\0"
├─────────────────────────────────┤
│ COFF Header (20 bytes)          │  Machine type, sections
├─────────────────────────────────┤
│ Optional Header (240 bytes)     │  Entry point, image base
├─────────────────────────────────┤
│ Section Table (40 bytes)        │  .text section info
├─────────────────────────────────┤
│ [Padding to 512 bytes]          │  File alignment
├─────────────────────────────────┤
│ Code Section (.text)            │  Machine code
├─────────────────────────────────┤
│ [Padding to 512 bytes]          │  Section alignment
└─────────────────────────────────┘
```

### DOS Header

**Code** (PE.cs, CreateDOSHeader):

```csharp
private static byte[] CreateDOSHeader()
{
    var header = new byte[64];
    
    // Magic number "MZ"
    header[0] = 0x4D;  // 'M'
    header[1] = 0x5A;  // 'Z'
    
    // Bytes on last page
    header[2] = 0x90;
    header[3] = 0x00;
    
    // Pages in file
    header[4] = 0x03;
    header[5] = 0x00;
    
    // ... other fields
    
    // PE header offset (at byte 60-63)
    header[60] = 0x80;  // PE at offset 128
    header[61] = 0x00;
    header[62] = 0x00;
    header[63] = 0x00;
    
    return header;
}
```

### COFF Header

**Code** (PE.cs, CreateCOFFHeader):

```csharp
private static byte[] CreateCOFFHeader()
{
    var header = new byte[20];
    
    // Machine (0x8664 = AMD64/x86-64)
    header[0] = 0x64;
    header[1] = 0x86;
    
    // Number of sections
    header[2] = 0x01;
    header[3] = 0x00;
    
    // Size of optional header (240 bytes for PE32+)
    header[16] = 0xF0;
    header[17] = 0x00;
    
    // Characteristics (0x22 = executable, large address aware)
    header[18] = 0x22;
    header[19] = 0x00;
    
    return header;
}
```

### Optional Header

**Code** (PE.cs, CreateOptionalHeader):

```csharp
private static byte[] CreateOptionalHeader(int imageBase, int entryPointRVA, int codeSize)
{
    var header = new byte[240];
    
    // Magic (0x20B = PE32+)
    header[0] = 0x0B;
    header[1] = 0x02;
    
    // Size of code
    WriteInt32(header, 4, codeSize);
    
    // Address of entry point
    WriteInt32(header, 16, entryPointRVA);
    
    // Image base (64-bit)
    WriteInt64(header, 24, imageBase);
    
    // Section alignment
    WriteInt32(header, 32, 0x1000);
    
    // File alignment
    WriteInt32(header, 36, 0x200);
    
    // Subsystem (3 = console)
    header[68] = 0x03;
    header[69] = 0x00;
    
    return header;
}
```

### Code Section

**Code** (PE.cs, CreateCodeSection):

```csharp
private static byte[] CreateCodeSection(int codeSize)
{
    var section = new byte[40];
    
    // Name (".text")
    section[0] = (byte)'.';
    section[1] = (byte)'t';
    section[2] = (byte)'e';
    section[3] = (byte)'x';
    section[4] = (byte)'t';
    
    // Virtual size
    WriteInt32(section, 8, codeSize);
    
    // Virtual address (RVA)
    WriteInt32(section, 12, 0x1000);
    
    // Size of raw data (aligned)
    int alignedSize = ((codeSize + 511) / 512) * 512;
    WriteInt32(section, 16, alignedSize);
    
    // Pointer to raw data
    WriteInt32(section, 20, 0x200);
    
    // Characteristics (0x60000020 = code, executable, readable)
    WriteInt32(section, 36, 0x60000020);
    
    return section;
}
```

### Usage Example

```csharp
// Machine code from architecture backend
byte[] machineCode = GenerateX86_64Code(watText);

// Wrap in PE format
byte[] peFile = PE.Emit(machineCode);

// Write to disk
File.WriteAllBytes("program.exe", peFile);
```

### Limitations

⚠️ **Native code generation is a stub**: The x86-64/ARM backends don't fully implement WAT-to-assembly conversion yet.

**Current behavior**:
```csharp
// BadgerCompiler.Compile()
return ExitCode42Binary();  // Returns a minimal "exit 42" program
```

---

## Code Examples

### Complete Example: Hello World

**C# Input**:
```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello World!");
    }
}
```

**Generated WAT** (`/tmp/debug_output.wat`):
```wat
(module
  ;; Imports
  (import "env" "memory" (memory 1))
  (import "env" "console_log" (func $console_log (param i32) (param i32)))
  (import "env" "console_readkey" (func $console_readkey (result i32)))

  ;; Global heap pointer
  (global $heap_ptr (mut i32) (i32.const 16))

  ;; Helper functions
  (func $alloc (param $size i32) (result i32)
    (local $ptr i32)
    global.get $heap_ptr
    local.set $ptr
    global.get $heap_ptr
    local.get $size
    i32.add
    global.set $heap_ptr
    local.get $ptr
  )

  ;; Data section
  (data (i32.const 0) "Hello World!\00")

  ;; Main function
  (func $Main
    i32.const 0
    i32.const 12
    call $console_log
  )

  ;; Exports
  (export "main" (func $Main))
)
```

**Compilation**:
```bash
dotnet run -- compile hello.cs --verbose
```

**Output files**:
- `output.wasm` - Binary WASM (228 bytes)
- `output.js` - JS wrapper (3987 bytes)
- `output.html` - HTML loader (1400 bytes)
- `/tmp/debug_output.wat` - WAT text (1794 chars)

---

## Technical Details

### Type Mappings

| C# Type | WASM Type | Notes |
|---------|-----------|-------|
| `int` | `i32` | 32-bit signed integer |
| `uint` | `i32` | Treated as signed |
| `long` | `i64` | 64-bit integer |
| `float` | `f32` | 32-bit float |
| `double` | `f64` | 64-bit float |
| `bool` | `i32` | 0 = false, 1 = true |
| `string` | `(i32, i32)` | (pointer, length) pair |
| `object` | `i32` | Pointer to heap |

### Instruction Mappings

| C# Operation | WASM Instruction | Example |
|-------------|------------------|---------|
| `a + b` (int) | `i32.add` | `i32.const 1; i32.const 2; i32.add` |
| `a - b` (int) | `i32.sub` | `i32.const 5; i32.const 3; i32.sub` |
| `a * b` (int) | `i32.mul` | `i32.const 6; i32.const 7; i32.mul` |
| `a / b` (int) | `i32.div_s` | `i32.const 10; i32.const 2; i32.div_s` |
| `a % b` (int) | `i32.rem_s` | `i32.const 10; i32.const 3; i32.rem_s` |
| `a == b` | `i32.eq` | `i32.const 5; i32.const 5; i32.eq` |
| `a < b` | `i32.lt_s` | `i32.const 3; i32.const 5; i32.lt_s` |
| `str1 + str2` | `call $string_concat` | Multi-value call |

### Memory Layout

```
Address 0:     ┌─────────────────────┐
               │  String Literals    │
               │  (Data Section)     │
               ├─────────────────────┤
               │  [Padding]          │  Align to 4 bytes
Heap Start:    ├─────────────────────┤
               │  Dynamic Allocations│
               │  (from $alloc)      │
               │  - String concat    │
               │  - int_to_string    │
               │  - Objects          │
               │  ↓ Grows downward   │
               └─────────────────────┘
```

### Calling Conventions

**WASM uses a stack-based calling convention**:

1. **Arguments**: Push onto stack before call
2. **Call**: `call $function_name`
3. **Results**: Pop from stack after call

**Example**:
```wat
;; Call: console_log("Hello", 5)
i32.const 0      ;; Push arg1 (pointer)
i32.const 5      ;; Push arg2 (length)
call $console_log ;; Call function (consumes 2 args)
;; No results (void function)
```

**Multi-value returns** (WASM MVP extension):
```wat
;; Call: (ptr, len) = string_concat(ptr1, len1, ptr2, len2)
i32.const 0      ;; arg1
i32.const 5      ;; arg2
i32.const 6      ;; arg3
i32.const 6      ;; arg4
call $string_concat  ;; Returns 2 values
;; Stack now has: [ptr] [len]
```

### ULEB128 Encoding

WASM uses **ULEB128** (Unsigned Little-Endian Base 128) for variable-length integers:

**Algorithm**:
```csharp
private static void WriteULEB128(List<byte> buffer, uint value)
{
    do
    {
        byte b = (byte)(value & 0x7F);
        value >>= 7;
        if (value != 0)
            b |= 0x80;  // More bytes follow
        buffer.Add(b);
    } while (value != 0);
}
```

**Examples**:
- `0` → `0x00`
- `127` → `0x7F`
- `128` → `0x80 0x01`
- `16384` → `0x80 0x80 0x01`

---

## Summary

### WAT Generation
- **Fully implemented** in `Compiler/Core/MapSet.cs`
- High-quality, standards-compliant output
- Supports strings, arithmetic, method calls
- Production-ready

### WASM Binary
- **Partially implemented** in `Dependencies/BADGER/Containers/WasmJS.cs`
- Has S-expression parsing bugs
- **Workaround**: Use WABT's `wat2wasm` tool

### PE Format
- **Fully implemented** structure in `Dependencies/BADGER/Containers/PE.cs`
- Native code generation is stub-only
- **Future work**: Implement WAT-to-x86/ARM compiler

### Recommendations
1. **Use CRAB for WAT generation** ✅
2. **Use WABT for WASM binary** ✅
3. **Native compilation**: Wait for BADGER improvements ⏳

---

**For more information**:
- Architecture: `CRAB_ARCHITECTURE_SUMMARY.md`
- Quick Start: `QUICK_START_GUIDE.md`
- Status: `FINAL_STATUS.md`
