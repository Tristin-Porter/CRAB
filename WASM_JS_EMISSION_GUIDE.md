# BADGER WASM+JS Emission Guide

## Overview

BADGER now supports emitting WebAssembly (WASM) binary format along with JavaScript wrapper code, enabling compilation targets for web browsers and Node.js environments.

## Features

### WASM Binary Emission
- Converts WAT (WebAssembly Text) to WASM binary format
- Valid WASM magic number: `0x00 0x61 0x73 0x6D`
- WASM version 1: `0x01 0x00 0x00 0x00`
- Proper section encoding:
  - Type section (function signatures)
  - Import section (external dependencies)
  - Function section (function indices)
  - Memory section (linear memory)
  - Export section (exported functions)
  - Code section (function bodies)

### JavaScript Wrapper
- Automatic JS wrapper generation
- Browser and Node.js compatibility
- Features:
  - Module loading
  - Memory initialization
  - Export discovery
  - main() function execution
  - Error handling

## Usage

### Command Line

#### Compile WAT to WASM
```bash
# Basic compilation
crab compile input.wat --format wasm

# This generates two files:
# - output.wasm (WebAssembly binary)
# - output.js (JavaScript wrapper)
```

#### From Build Command
```bash
# Build project and emit WASM
crab build MyProject --format wasm

# Outputs in bin/ directory:
# - output.wasm
# - output.js
```

### Programmatic Usage

```csharp
using Badger;

// Compile WAT to WASM
string watText = File.ReadAllText("input.wat");
byte[] wasmBinary = BadgerCompiler.Compile(watText, "wasm", "wasm");

// Or use CompileFile for automatic .js generation
BadgerCompiler.CompileFile("input.wat", "output.wasm", "wasm", "wasm");
// Creates: output.wasm and output.js
```

## JavaScript Wrapper API

The generated JavaScript wrapper provides two functions:

### Browser Environment
```javascript
// Automatically used in browser
loadAndRunWasm('output.wasm')
    .then(instance => {
        console.log('Module loaded');
        // Instance exports are available
    })
    .catch(error => {
        console.error('Failed to load:', error);
    });
```

### Node.js Environment
```javascript
// Automatically used in Node.js
loadAndRunWasmNode('./output.wasm')
    .then(instance => {
        console.log('Module loaded');
        // Instance exports are available
    })
    .catch(error => {
        console.error('Failed to load:', error);
    });
```

### Automatic Environment Detection
The generated wrapper automatically detects the environment:
```javascript
if (typeof window !== 'undefined') {
    // Browser - uses fetch()
    loadAndRunWasm('output.wasm');
} else if (typeof require !== 'undefined') {
    // Node.js - uses fs.readFileSync()
    loadAndRunWasmNode('./output.wasm');
}
```

## Example Workflow

### 1. Create WAT File
```wat
(module
  (func $main (result i32)
    i32.const 42
  )
  (export "main" (func $main))
)
```

### 2. Compile to WASM
```bash
crab compile example.wat --format wasm
```

### 3. Run in Browser
```html
<!DOCTYPE html>
<html>
<head>
    <title>WASM Example</title>
</head>
<body>
    <h1>WASM Test</h1>
    <div id="output"></div>
    <script src="output.js"></script>
    <script>
        loadAndRunWasm('output.wasm').then(instance => {
            const result = instance.exports.main();
            document.getElementById('output').textContent = 
                `main() returned: ${result}`;
        });
    </script>
</body>
</html>
```

### 4. Run in Node.js
```javascript
const fs = require('fs');

async function run() {
    const buffer = fs.readFileSync('./output.wasm');
    const result = await WebAssembly.instantiate(buffer, {
        env: {
            memory: new WebAssembly.Memory({ initial: 1 })
        }
    });
    
    console.log('Exports:', Object.keys(result.instance.exports));
    
    if (result.instance.exports.main) {
        const value = result.instance.exports.main();
        console.log('main() returned:', value);
    }
}

run().catch(console.error);
```

## WASM Binary Format Details

### Magic Number and Version
```
Offset  Value       Description
------  ----------  -----------
0x00    00 61 73 6D Magic number (\0asm)
0x04    01 00 00 00 Version 1
```

### Section Structure
Each section follows this pattern:
```
1 byte:  Section ID
ULEB128: Section size in bytes
N bytes: Section content
```

### Section IDs
```
0:  Custom section
1:  Type section
2:  Import section
3:  Function section
4:  Table section
5:  Memory section
6:  Global section
7:  Export section
8:  Start section
9:  Element section
10: Code section
11: Data section
```

### Value Types
```
0x7F: i32
0x7E: i64
0x7D: f32
0x7C: f64
```

## Testing WASM Output

### Using the Test Command
```bash
# Test WASM emission
crab test --quick --debug

# Save WASM outputs
crab test --save

# Outputs saved to:
# - tests/{project}/wasm/output.wasm
# - tests/{project}/wasm/output.js
```

### Verify WASM Binary
```bash
# Check magic number
hexdump -C output.wasm | head -1
# Should show: 00 61 73 6d 01 00 00 00

# Get file size
ls -lh output.wasm

# Validate with wasm-validate (if available)
wasm-validate output.wasm

# Disassemble with wasm2wat (if available)
wasm2wat output.wasm -o output.wat
```

### Run Tests
```javascript
// test.js
const fs = require('fs');
const assert = require('assert');

async function test() {
    const buffer = fs.readFileSync('./output.wasm');
    
    // Verify magic number
    assert.equal(buffer[0], 0x00);
    assert.equal(buffer[1], 0x61);
    assert.equal(buffer[2], 0x73);
    assert.equal(buffer[3], 0x6D);
    
    // Verify version
    assert.equal(buffer[4], 0x01);
    assert.equal(buffer[5], 0x00);
    assert.equal(buffer[6], 0x00);
    assert.equal(buffer[7], 0x00);
    
    // Try to instantiate
    const result = await WebAssembly.instantiate(buffer, {
        env: { memory: new WebAssembly.Memory({ initial: 1 }) }
    });
    
    console.log('✓ Valid WASM binary');
    console.log('✓ Exports:', Object.keys(result.instance.exports));
    
    return result.instance;
}

test().then(() => console.log('All tests passed!'));
```

## Architecture Support

WASM format is architecture-independent:
```bash
# Architecture parameter ignored for WASM
crab compile input.wat --arch x86_64 --format wasm  # Same as:
crab compile input.wat --arch arm64 --format wasm   # Same output
```

## Comparison with Native Formats

| Feature | Native/PE | WASM |
|---------|-----------|------|
| Architecture | Specific (x86_64, ARM, etc.) | Universal |
| Platform | OS-dependent | Browser/Node.js |
| Execution | Direct CPU | VM/JIT |
| Portability | Low | High |
| Performance | Highest | Near-native |
| Distribution | Platform binaries | Single .wasm file |

## Advanced Features

### Custom Imports
Modify the generated JS wrapper to add custom imports:
```javascript
const imports = {
    env: {
        memory: new WebAssembly.Memory({ initial: 1 }),
        
        // Add custom imports
        log: (value) => console.log('WASM log:', value),
        getCurrentTime: () => Date.now(),
        // ... more functions
    }
};

const result = await WebAssembly.instantiate(buffer, imports);
```

### Memory Access
```javascript
const instance = result.instance;
const memory = instance.exports.memory || imports.env.memory;
const view = new Uint8Array(memory.buffer);

// Read/write memory
view[0] = 42;
console.log('Memory[0]:', view[0]);
```

### Multiple Modules
```javascript
// Load multiple WASM modules
const module1 = await loadWasm('module1.wasm');
const module2 = await loadWasm('module2.wasm');

// Link them via imports
const result2 = await WebAssembly.instantiate(module2Buffer, {
    env: { ...imports.env },
    module1: module1.exports  // Import from module1
});
```

## Performance Considerations

### WASM Compilation
- WASM compilation is fast (typically <50ms)
- Binary format is compact
- No architecture-specific code generation needed

### Runtime Performance
- Browser: JIT-compiled by JavaScript engine
- Node.js: V8 optimizations apply
- Near-native performance for compute-heavy tasks
- Slower startup than native binaries

## Troubleshooting

### Invalid WASM Binary
```
Error: WebAssembly.instantiate(): Invalid magic number
```
**Solution:** Check that output.wasm starts with `00 61 73 6D`

### Module Instantiation Failed
```
Error: WebAssembly.instantiate(): Import #0 "env.memory" is not defined
```
**Solution:** Ensure imports object includes required imports

### CORS Error (Browser)
```
Error: Failed to fetch wasm file
```
**Solution:** Serve files via HTTP server (not file://)
```bash
python -m http.server 8000
# or
npx serve
```

### Node.js Module Error
```
Error: Cannot find module 'fs'
```
**Solution:** Use Node.js version with fs support, not browser

## Future Enhancements

Planned improvements:
- [ ] Full WAT parsing for complex modules
- [ ] Data section support
- [ ] Table section support
- [ ] Multiple memory support
- [ ] WASM SIMD instructions
- [ ] Reference types
- [ ] Bulk memory operations

## References

- [WebAssembly Specification](https://webassembly.github.io/spec/)
- [MDN WebAssembly Documentation](https://developer.mozilla.org/en-US/docs/WebAssembly)
- [WASM Binary Format](https://webassembly.github.io/spec/core/binary/index.html)
- [JavaScript API](https://webassembly.github.io/spec/js-api/index.html)

## See Also

- [Test Command Documentation](TEST_COMMAND.md)
- [BADGER Architecture Guide](BADGER_ARCHITECTURE.md)
- [Build System Documentation](BUILD_SYSTEM.md)
