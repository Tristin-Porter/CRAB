# WASM Browser Execution Guide

## Overview

CRAB now automatically generates everything needed to run your compiled C# code in a web browser:
- **output.wasm** - WebAssembly binary (compiled from WAT)
- **output.js** - JavaScript wrapper for loading the WASM module
- **index.html** - HTML page to run the WASM module in your browser
- **output.wat** - WebAssembly text format (for debugging)

## Quick Start

### 1. Build Your Project

```bash
crab build
```

This generates all four files in your `bin/` directory.

### 2. Run in Browser

#### Option A: Open Directly (Simple Projects)
```bash
# On Windows
start bin/index.html

# On macOS
open bin/index.html

# On Linux
xdg-open bin/index.html
```

#### Option B: Use HTTP Server (Recommended)
```bash
# Navigate to your project directory
cd MyProject

# Start a simple HTTP server
python -m http.server 8000

# Or use Node.js
npx serve bin

# Then open in browser:
# http://localhost:8000/bin/index.html
```

### 3. View Results

The HTML page will:
- Load the WASM module
- Execute the `main()` function (if present)
- Display results in the browser console (F12)
- Show status and output on the page

## Generated Files

### output.wasm
Binary WebAssembly format. This is what the browser actually executes.

```bash
# Verify it's a valid WASM file
hexdump -C bin/output.wasm | head -1
# Should show: 00 61 73 6d 01 00 00 00 (magic number + version)
```

### output.js
JavaScript wrapper that:
- Loads the WASM module
- Sets up memory imports
- Calls exported functions
- Handles errors

### index.html
Browser runner with:
- Nice UI with CRAB branding
- Status indicators (loading, success, error)
- Console output display
- Browser console integration

### output.wat
Human-readable WebAssembly text format for debugging.

## Example Projects

### Hello World

```bash
# Create a new console app
crab new console HelloWorld
cd HelloWorld

# Build it
crab build

# Open in browser
python -m http.server 8000
# Navigate to http://localhost:8000/bin/index.html
```

### Test Projects

Run the test suite to generate multiple example projects:

```bash
# Generate test projects with WASM output
crab test --quick --keep --save

# Check generated files
ls -la HelloWorld/bin/
ls -la tests/HelloWorld/wasm/
```

## Browser Console

Open your browser's developer console (F12) to see:
- Module loading status
- Exported functions
- Function results
- Any errors or warnings

## Troubleshooting

### CORS Errors

If you see CORS errors when opening `index.html` directly:

**Solution:** Use an HTTP server (see Quick Start, Option B)

```bash
python -m http.server 8000
```

### Module Not Loading

Check:
1. All files are in the same directory (output.wasm, output.js, index.html)
2. File paths in HTML match actual files
3. Browser console (F12) for specific errors

### Invalid WASM Module

Verify the magic number:
```bash
hexdump -C bin/output.wasm | head -1
# Should start with: 00 61 73 6d
```

## Customization

### Modify the HTML

The generated `index.html` can be customized:

```html
<!-- Add custom styles -->
<style>
    /* Your custom CSS */
</style>

<!-- Add custom JavaScript -->
<script>
    // Your custom code
    loadAndRunWasm('output.wasm').then(instance => {
        // Do something with the instance
        console.log('Custom handler!');
    });
</script>
```

### Modify the JS Wrapper

Edit `output.js` to add custom imports:

```javascript
const imports = {
    env: {
        memory: new WebAssembly.Memory({ initial: 1 }),
        
        // Add custom functions
        log: (value) => console.log('WASM says:', value),
        alert: (value) => alert(value),
    }
};
```

## Integration with Build Systems

### GitHub Pages

1. Build your project
2. Copy `bin/` contents to your GitHub Pages directory
3. Commit and push
4. Access at `https://yourusername.github.io/yourrepo/index.html`

### Static Site Generators

Include the generated files in your static site:

```bash
# Copy to static site directory
cp bin/*.{wasm,js,html} /path/to/static/site/wasm/
```

## Advanced Usage

### Multiple WASM Modules

Load multiple modules:

```javascript
const module1 = await loadWasm('module1.wasm');
const module2 = await loadWasm('module2.wasm');

// Use exports from both modules
module1.exports.function1();
module2.exports.function2();
```

### Memory Inspection

Access WASM linear memory:

```javascript
const instance = await loadAndRunWasm('output.wasm');
const memory = instance.exports.memory || imports.env.memory;
const view = new Uint8Array(memory.buffer);

// Read/write memory
console.log('Byte at offset 0:', view[0]);
view[100] = 42;
```

## Performance

- WASM modules are compiled by the browser's JIT
- Performance is near-native for compute-intensive tasks
- Initial load time is minimal (modules are cached)
- Suitable for:
  - Data processing
  - Scientific computing
  - Games
  - Cryptography
  - Image processing

## See Also

- [WASM JS Emission Guide](WASM_JS_EMISSION_GUIDE.md) - Technical details
- [Test Command Guide](TEST_COMMAND_GUIDE.md) - Running tests
- [Quick Start Guide](QUICK_START.md) - Getting started with CRAB
