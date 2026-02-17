# CRAB Compiler - Quick Start Guide

## 🚀 Quick Start (5 Minutes)

### 1. Build the Compiler

```bash
cd /home/runner/work/CRAB/CRAB
dotnet restore
dotnet build
```

### 2. Compile Your First Program

Create a file `hello.cs`:

```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello from CRAB!");
    }
}
```

Compile it:

```bash
dotnet run -- compile hello.cs --verbose
```

### 3. View the Generated WAT

```bash
cat /tmp/debug_output.wat
```

You'll see WebAssembly Text format like:

```wat
(module
  (import "env" "console_log" (func $console_log (param i32) (param i32)))
  (data (i32.const 0) "Hello from CRAB!\00")
  (func $Main
    i32.const 0
    i32.const 16
    call $console_log
  )
  (export "main" (func $Main))
)
```

---

## 📚 Common Tasks

### Compile with Options

```bash
# Verbose output (recommended for debugging)
dotnet run -- compile program.cs --verbose

# Custom output path
dotnet run -- compile program.cs --output my-program.wasm

# Disable optimizations
dotnet run -- compile program.cs --optimize false

# Run verification passes
dotnet run -- compile program.cs --verify
```

### Test Compilation

```bash
# Test with example programs
dotnet run -- compile Testing/TestProjects/HelloWorld.cs --verbose
dotnet run -- compile Testing/TestProjects/Calculator.cs --verbose

# Run the full test suite
dotnet run -- test-suite
```

### Native Compilation (Experimental)

```bash
# Compile to native x86-64 assembly
dotnet run -- compile program.cs --to-asm --arch x86_64 --format native

# Compile to Windows PE executable
dotnet run -- compile program.cs --to-asm --format pe --output program.exe
```

⚠️ **Note**: Native code generation is currently a stub. Use WASM output.

---

## 🔍 Understanding the Output

### Generated Files

After running `dotnet run -- compile program.cs`:

| File | Description |
|------|-------------|
| `output.wasm` | Binary WASM module (228 bytes typical) |
| `output.js` | JavaScript wrapper for loading WASM |
| `output.html` | HTML page to run in browser |
| `/tmp/debug_output.wat` | WAT text format (for debugging) |

### Running the Output

#### Option 1: Browser (Simplest)

```bash
# Open the generated HTML file
firefox output.html
# or
chromium output.html
```

The HTML will load the WASM and run it.

#### Option 2: Node.js

```bash
# Fix the path in output.js first (it references wrong file)
# Then run:
node output.js
```

#### Option 3: WABT Tools (Recommended)

Install WABT (WebAssembly Binary Toolkit):

```bash
# Ubuntu/Debian
sudo apt-get install wabt

# macOS
brew install wabt

# Arch Linux
sudo pacman -S wabt
```

Use it:

```bash
# Convert WAT to WASM (fixes encoding issues)
wat2wasm /tmp/debug_output.wat -o fixed.wasm

# Run with Wasmtime
wasmtime fixed.wasm

# Or with Wasmer
wasmer run fixed.wasm

# Or decompile to WAT again
wasm2wat fixed.wasm
```

---

## 🐛 Debugging

### View Compilation Steps

Always use `--verbose` to see what's happening:

```bash
dotnet run -- compile program.cs --verbose
```

Output shows:
- [1/6] Reading source files
- [2/6] Compiling with CDTk pipeline
- [3/6] Memory analysis
- [4/6] Manual memory verification
- [5/6] WebAssembly generation
- [6/6] Writing output

### Check the WAT

The WAT file shows exactly what WASM instructions are generated:

```bash
cat /tmp/debug_output.wat
```

Look for:
- String literals in `(data ...)` sections
- Function definitions `(func $Name ...)`
- Imports from environment `(import "env" ...)`
- Exports `(export "main" ...)`

### Common Issues

#### 1. "not enough arguments on stack"

**Problem**: WAT has malformed instructions

**Solution**: This is a BADGER encoding bug. Use external tools:

```bash
wat2wasm /tmp/debug_output.wat -o output.wasm
```

#### 2. "function index out of bounds"

**Problem**: WASM binary has wrong function indices

**Solution**: Use WABT `wat2wasm` instead of BADGER's encoder

#### 3. Empty console_log call

**Problem**: WAT has `call $console_log` without arguments

**Cause**: Bug in expression emission (likely fixed)

**Debug**:
```bash
grep -A2 "call \$console_log" /tmp/debug_output.wat
```

Should see:
```wat
i32.const 0      ;; pointer to string
i32.const 12     ;; length of string
call $console_log
```

---

## 📝 Supported C# Features

### ✅ Working

- **Classes**: `class MyClass { ... }`
- **Methods**: `void Method() { ... }`, `int Add(int a, int b) { ... }`
- **Variables**: `int x = 5;`, `string s = "hello";`
- **Arithmetic**: `+`, `-`, `*`, `/` (integers only)
- **Strings**: `"hello"`, `"hello" + " world"`
- **Console I/O**: `Console.WriteLine(...)`, `Console.ReadKey()`
- **Method Calls**: `myObject.Method()`, `Console.WriteLine("x")`

### ❌ Not Yet Supported

- **Generics**: Limited support
- **LINQ**: Not implemented
- **Async/Await**: Not implemented
- **Reflection**: Not implemented
- **Exceptions**: Not implemented
- **Properties**: Partial support
- **Events**: Not implemented
- **Delegates**: Not implemented
- **Nullable Types**: Not implemented
- **Pattern Matching**: Not implemented

---

## 🔧 Advanced Usage

### Modifying WAT Generation

The WAT code generation is in `Compiler/Core/MapSet.cs`.

**Key class**: `WASM : MapSet` (line 1987)

**Example**: Add a new expression type

```csharp
// In WasmEmit.EmitExpression() method
return node.Type switch
{
    // ... existing cases
    "MyNewExpression" => EmitMyNewExpression(node),
    _ => $";; TODO: {node.Type}"
};
```

Then implement:

```csharp
private static string EmitMyNewExpression(AstNode node)
{
    // Generate WAT instructions
    return "i32.const 42";
}
```

### Adding Helper Functions

Helper functions are in the `CompilationUnit` map:

```csharp
sb.AppendLine("  ;; My helper function");
sb.AppendLine("  (func $my_helper (param i32) (result i32)");
sb.AppendLine("    local.get 0");
sb.AppendLine("    i32.const 1");
sb.AppendLine("    i32.add");
sb.AppendLine("  )");
```

### Registering String Literals

Use the `StringRegistry`:

```csharp
// Register a string literal
int stringId = StringRegistry.RegisterString("Hello World!");

// Get its offset in data section
int offset = StringRegistry.GetStringOffset(stringId);

// Generate WAT code to load it
string wat = $"i32.const {offset}\ni32.const {text.Length}";
```

---

## 📊 Performance Tips

### 1. Use Optimizations

Optimizations are enabled by default. To disable:

```bash
dotnet run -- compile program.cs --optimize false
```

The `Optimization` model (in `Compiler/Models/Optimization.cs`) performs:
- Dead code elimination
- Constant folding
- Common subexpression elimination

### 2. Minimize String Operations

String concatenation allocates memory. Minimize in hot loops:

```csharp
// Avoid
for (int i = 0; i < 1000; i++)
    s = s + "x";  // Many allocations

// Better (when CRAB supports StringBuilder)
var sb = new StringBuilder();
for (int i = 0; i < 1000; i++)
    sb.Append("x");
```

### 3. Use Integer Arithmetic

WASM integer operations are fast. Floating point is slower.

---

## 🧪 Testing Your Changes

### Run Test Suite

```bash
# All tests
dotnet run -- test-suite

# Specific test project
dotnet run -- test Testing/TestProjects/Calculator.cs
```

### Add a New Test

1. Create test file in `Testing/TestProjects/`:

```csharp
// Testing/TestProjects/MyTest.cs
using System;

class Program
{
    static void Main()
    {
        int result = 2 + 2;
        Console.WriteLine("Result: " + result);
    }
}
```

2. Compile and verify:

```bash
dotnet run -- compile Testing/TestProjects/MyTest.cs --verbose
cat /tmp/debug_output.wat
```

3. Check the WAT output manually

---

## 📖 Further Reading

- **Architecture Summary**: `CRAB_ARCHITECTURE_SUMMARY.md`
- **Implementation Status**: `FINAL_STATUS.md`
- **WAT to WASM Guide**: `Documentation/WAT_TO_WASM_COMPLETION.md`
- **Architecture Details**: `Documentation/ARCHITECTURE.md`

---

## 🤝 Contributing

### Code Style

- Use C# naming conventions (PascalCase for public, camelCase for private)
- Add XML documentation comments to public methods
- Keep functions focused and small

### Adding New Features

1. **Tokenization**: Add tokens in `Compiler/Core/TokenSet.cs`
2. **Grammar**: Add rules in `Compiler/Core/RuleSet.cs`
3. **Code Gen**: Add maps in `Compiler/Core/MapSet.cs` (WASM class)
4. **Test**: Add test case in `Testing/`

### Example: Adding Boolean Literals

**Step 1**: Tokens already exist (`TrueLiteral`, `FalseLiteral`)

**Step 2**: Rules already exist

**Step 3**: Add to WasmEmit:

```csharp
// In EmitExpression() switch
"TrueLiteral" => "i32.const 1",
"FalseLiteral" => "i32.const 0",
```

**Step 4**: Test:

```csharp
// test.cs
class Program {
    static void Main() {
        bool b = true;
        Console.WriteLine("Value: " + b);
    }
}
```

---

## 🆘 Getting Help

### Check Diagnostics

The compiler reports errors during compilation:

```bash
dotnet run -- compile broken.cs --verbose
```

Look for:
- Syntax errors (from CDTk parser)
- Semantic errors (from analysis models)
- WAT generation errors

### Debug WAT Output

Always check `/tmp/debug_output.wat` to see what was generated:

```bash
# View full output
cat /tmp/debug_output.wat

# Search for specific function
grep -A20 "func \$Main" /tmp/debug_output.wat

# Check data section
grep "data" /tmp/debug_output.wat
```

### Validate WAT

Use WABT to validate:

```bash
wat2wasm /tmp/debug_output.wat -o test.wasm
```

If it fails, the error message shows exactly what's wrong.

---

## ⚡ Tips & Tricks

### 1. Alias the Command

Add to your `.bashrc` or `.zshrc`:

```bash
alias crab='dotnet run --project /path/to/CRAB/CRAB.csproj --'
```

Then use:

```bash
crab compile hello.cs --verbose
```

### 2. Watch Mode for Development

```bash
# Terminal 1: Watch for changes and rebuild
dotnet watch --project CRAB.csproj

# Terminal 2: Test your changes
dotnet run -- compile test.cs --verbose
```

### 3. Quick Test Script

Create `test.sh`:

```bash
#!/bin/bash
dotnet run -- compile "$1" --verbose
cat /tmp/debug_output.wat
```

Use:

```bash
./test.sh hello.cs
```

### 4. Diff WAT Output

```bash
# Save baseline
dotnet run -- compile test.cs --verbose
cp /tmp/debug_output.wat baseline.wat

# Make changes to compiler
# ...

# Compare
dotnet run -- compile test.cs --verbose
diff baseline.wat /tmp/debug_output.wat
```

---

## 📋 Checklist for New Features

- [ ] Add tokens if needed (TokenSet.cs)
- [ ] Add grammar rules if needed (RuleSet.cs)
- [ ] Add WAT emission logic (MapSet.cs)
- [ ] Add test case (Testing/TestProjects/)
- [ ] Compile test case and verify WAT
- [ ] Validate with `wat2wasm`
- [ ] Document in ARCHITECTURE.md
- [ ] Update this guide if user-facing

---

**Happy Compiling! 🦀**
