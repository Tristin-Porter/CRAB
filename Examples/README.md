# CRAB Examples

This directory contains example projects demonstrating CRAB's capabilities.

## Running Examples

Each example can be built and run in the browser:

```bash
# Navigate to an example
cd HelloWorld

# Build it
crab build

# Run in browser
cd bin
python -m http.server 8000
# Open http://localhost:8000/index.html
```

## Available Examples

### HelloWorld
Basic "Hello World" example showing the minimal CRAB project structure.

### Calculator  
Simple calculator demonstrating method calls and return values.

### ClassHierarchy
Class inheritance and method overriding example.

### GenericCollections
Generic types and collections example.

## Generated Files

After building, each example's `bin/` directory contains:

- **output.wasm** - WebAssembly binary (what browsers execute)
- **output.js** - JavaScript wrapper for loading WASM
- **index.html** - Browser runner with UI
- **output.wat** - WebAssembly text format (for debugging)

## Browser Compatibility

WASM files work in all modern browsers:
- Chrome/Edge 57+
- Firefox 52+
- Safari 11+
- Opera 44+

## Troubleshooting

### CORS Errors
If you get CORS errors opening `index.html` directly, use an HTTP server:

```bash
# Python
python -m http.server 8000

# Node.js
npx serve

# PHP
php -S localhost:8000
```

### Module Loading Errors
Check browser console (F12) for specific error messages.

## Further Reading

- [WASM Browser Guide](../WASM_BROWSER_GUIDE.md) - Complete guide to browser execution
- [Quick Start Guide](../QUICK_START.md) - Getting started with CRAB
- [Test Command Guide](../TEST_COMMAND_GUIDE.md) - Running the test suite
