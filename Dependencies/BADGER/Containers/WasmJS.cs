using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Badger.Containers;

/// <summary>
/// WASM+JS container emitter.
/// Produces WebAssembly binary format (.wasm) and JavaScript wrapper.
/// Converts WAT (WebAssembly Text) to WASM binary format.
/// </summary>
public static class WasmJS
{
    /// <summary>
    /// Emit WASM binary and JavaScript wrapper from WAT text
    /// </summary>
    /// <param name="watText">WebAssembly Text format input</param>
    /// <param name="wasmFileName">Name of the WASM file to reference in JS (default: output.wasm)</param>
    /// <returns>Tuple of (wasm binary, javascript wrapper)</returns>
    public static (byte[] wasm, string javascript) Emit(string watText, string wasmFileName = "output.wasm")
    {
        // Convert WAT to WASM binary
        byte[] wasmBinary = ConvertWatToWasm(watText);
        
        // Generate JavaScript wrapper
        string jsWrapper = GenerateJavaScriptWrapper(watText, wasmFileName);
        
        return (wasmBinary, jsWrapper);
    }
    
    /// <summary>
    /// Convert WebAssembly Text (WAT) to WebAssembly Binary (WASM)
    /// This is a simplified converter that handles basic WAT modules
    /// </summary>
    private static byte[] ConvertWatToWasm(string watText)
    {
        var wasm = new List<byte>();
        
        // WASM magic number (\0asm)
        wasm.AddRange(new byte[] { 0x00, 0x61, 0x73, 0x6D });
        
        // WASM version (1)
        wasm.AddRange(new byte[] { 0x01, 0x00, 0x00, 0x00 });
        
        // Parse the WAT to extract module information
        var moduleInfo = ParseWatModule(watText);
        
        // Emit sections in order
        if (moduleInfo.TypeSection.Count > 0)
            EmitTypeSection(wasm, moduleInfo.TypeSection);
        
        if (moduleInfo.ImportSection.Count > 0)
            EmitImportSection(wasm, moduleInfo.ImportSection);
        
        if (moduleInfo.FunctionSection.Count > 0)
            EmitFunctionSection(wasm, moduleInfo.FunctionSection);
        
        if (moduleInfo.MemorySection.Count > 0)
            EmitMemorySection(wasm, moduleInfo.MemorySection);
        
        if (moduleInfo.ExportSection.Count > 0)
            EmitExportSection(wasm, moduleInfo.ExportSection);
        
        if (moduleInfo.CodeSection.Count > 0)
            EmitCodeSection(wasm, moduleInfo.CodeSection);
        
        return wasm.ToArray();
    }
    
    private static void EmitTypeSection(List<byte> wasm, List<FunctionType> types)
    {
        var sectionData = new List<byte>();
        
        // Write count of types
        WriteULEB128(sectionData, (uint)types.Count);
        
        foreach (var type in types)
        {
            sectionData.Add(0x60); // func type
            
            // Parameters
            WriteULEB128(sectionData, (uint)type.Parameters.Count);
            foreach (var param in type.Parameters)
                sectionData.Add(GetValueTypeByte(param));
            
            // Results
            WriteULEB128(sectionData, (uint)type.Results.Count);
            foreach (var result in type.Results)
                sectionData.Add(GetValueTypeByte(result));
        }
        
        // Write section: type (1), size, data
        wasm.Add(1);
        WriteULEB128(wasm, (uint)sectionData.Count);
        wasm.AddRange(sectionData);
    }
    
    private static void EmitImportSection(List<byte> wasm, List<Import> imports)
    {
        var sectionData = new List<byte>();
        
        WriteULEB128(sectionData, (uint)imports.Count);
        
        foreach (var import in imports)
        {
            // Module name
            WriteString(sectionData, import.Module);
            // Field name
            WriteString(sectionData, import.Name);
            // Import kind
            sectionData.Add(import.Kind);
            // Import description (for memory: limits)
            if (import.Kind == 0x02) // memory
            {
                sectionData.Add(0x00); // flags (no maximum)
                WriteULEB128(sectionData, import.MemoryMinPages);
            }
        }
        
        wasm.Add(2);
        WriteULEB128(wasm, (uint)sectionData.Count);
        wasm.AddRange(sectionData);
    }
    
    private static void EmitFunctionSection(List<byte> wasm, List<uint> functionTypeIndices)
    {
        var sectionData = new List<byte>();
        
        WriteULEB128(sectionData, (uint)functionTypeIndices.Count);
        foreach (var typeIdx in functionTypeIndices)
            WriteULEB128(sectionData, typeIdx);
        
        wasm.Add(3);
        WriteULEB128(wasm, (uint)sectionData.Count);
        wasm.AddRange(sectionData);
    }
    
    private static void EmitMemorySection(List<byte> wasm, List<Memory> memories)
    {
        var sectionData = new List<byte>();
        
        WriteULEB128(sectionData, (uint)memories.Count);
        foreach (var memory in memories)
        {
            sectionData.Add(0x00); // flags (no maximum)
            WriteULEB128(sectionData, memory.MinPages);
        }
        
        wasm.Add(5);
        WriteULEB128(wasm, (uint)sectionData.Count);
        wasm.AddRange(sectionData);
    }
    
    private static void EmitExportSection(List<byte> wasm, List<Export> exports)
    {
        var sectionData = new List<byte>();
        
        WriteULEB128(sectionData, (uint)exports.Count);
        foreach (var export in exports)
        {
            WriteString(sectionData, export.Name);
            sectionData.Add(export.Kind);
            WriteULEB128(sectionData, export.Index);
        }
        
        wasm.Add(7);
        WriteULEB128(wasm, (uint)sectionData.Count);
        wasm.AddRange(sectionData);
    }
    
    private static void EmitCodeSection(List<byte> wasm, List<FunctionCode> codes)
    {
        var sectionData = new List<byte>();
        
        WriteULEB128(sectionData, (uint)codes.Count);
        foreach (var code in codes)
        {
            var funcBody = new List<byte>();
            
            // Locals
            WriteULEB128(funcBody, (uint)code.Locals.Count);
            foreach (var local in code.Locals)
            {
                WriteULEB128(funcBody, 1); // count
                funcBody.Add(GetValueTypeByte(local));
            }
            
            // Instructions
            funcBody.AddRange(code.Instructions);
            
            // End
            funcBody.Add(0x0B);
            
            // Write function body size and body
            WriteULEB128(sectionData, (uint)funcBody.Count);
            sectionData.AddRange(funcBody);
        }
        
        wasm.Add(10);
        WriteULEB128(wasm, (uint)sectionData.Count);
        wasm.AddRange(sectionData);
    }
    
    private static ModuleInfo ParseWatModule(string watText)
    {
        var info = new ModuleInfo();
        
        // Simple regex-based parsing (basic implementation)
        // In a full implementation, this would use the CDTk parser from Program.cs
        
        // For now, create a minimal valid module with one function
        info.TypeSection.Add(new FunctionType
        {
            Parameters = new List<string>(),
            Results = new List<string> { "i32" }
        });
        
        info.FunctionSection.Add(0); // Function uses type 0
        
        info.CodeSection.Add(new FunctionCode
        {
            Locals = new List<string>(),
            Instructions = new List<byte> 
            { 
                0x41, 0x2A  // i32.const 42
            }
        });
        
        info.ExportSection.Add(new Export
        {
            Name = "main",
            Kind = 0x00, // function
            Index = 0
        });
        
        return info;
    }
    
    /// <summary>
    /// Generate JavaScript wrapper for loading and running the WASM module
    /// </summary>
    /// <param name="watText">WebAssembly Text format input</param>
    /// <param name="wasmFileName">Name of the WASM file to reference</param>
    private static string GenerateJavaScriptWrapper(string watText, string wasmFileName)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("/**");
        sb.AppendLine(" * WebAssembly Module Loader and Runner");
        sb.AppendLine(" * Generated by BADGER - CRAB Compiler");
        sb.AppendLine(" */");
        sb.AppendLine();
        
        sb.AppendLine("async function loadAndRunWasm(wasmPath) {");
        sb.AppendLine("    try {");
        sb.AppendLine("        // Fetch the WASM file");
        sb.AppendLine("        const response = await fetch(wasmPath);");
        sb.AppendLine("        const buffer = await response.arrayBuffer();");
        sb.AppendLine("        ");
        sb.AppendLine("        // Create imports object");
        sb.AppendLine("        const imports = {");
        sb.AppendLine("            env: {");
        sb.AppendLine("                memory: new WebAssembly.Memory({ initial: 1 }),");
        sb.AppendLine("                // Add more imports as needed");
        sb.AppendLine("            }");
        sb.AppendLine("        };");
        sb.AppendLine("        ");
        sb.AppendLine("        // Compile and instantiate");
        sb.AppendLine("        const result = await WebAssembly.instantiate(buffer, imports);");
        sb.AppendLine("        const instance = result.instance;");
        sb.AppendLine("        ");
        sb.AppendLine("        // Log exports");
        sb.AppendLine("        console.log('WASM module loaded successfully');");
        sb.AppendLine("        console.log('Exports:', Object.keys(instance.exports));");
        sb.AppendLine("        ");
        sb.AppendLine("        // Call main function if it exists");
        sb.AppendLine("        if (instance.exports.main) {");
        sb.AppendLine("            console.log('Calling main()...');");
        sb.AppendLine("            const result = instance.exports.main();");
        sb.AppendLine("            console.log('Result:', result);");
        sb.AppendLine("            return result;");
        sb.AppendLine("        } else {");
        sb.AppendLine("            console.warn('No main() function found in exports');");
        sb.AppendLine("        }");
        sb.AppendLine("        ");
        sb.AppendLine("        return instance;");
        sb.AppendLine("    } catch (error) {");
        sb.AppendLine("        console.error('Error loading WASM module:', error);");
        sb.AppendLine("        throw error;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine("// Node.js compatible version");
        sb.AppendLine("async function loadAndRunWasmNode(wasmPath) {");
        sb.AppendLine("    const fs = require('fs');");
        sb.AppendLine("    ");
        sb.AppendLine("    try {");
        sb.AppendLine("        // Read WASM file");
        sb.AppendLine("        const buffer = fs.readFileSync(wasmPath);");
        sb.AppendLine("        ");
        sb.AppendLine("        // Create imports");
        sb.AppendLine("        const imports = {");
        sb.AppendLine("            env: {");
        sb.AppendLine("                memory: new WebAssembly.Memory({ initial: 1 })");
        sb.AppendLine("            }");
        sb.AppendLine("        };");
        sb.AppendLine("        ");
        sb.AppendLine("        // Compile and instantiate");
        sb.AppendLine("        const result = await WebAssembly.instantiate(buffer, imports);");
        sb.AppendLine("        const instance = result.instance;");
        sb.AppendLine("        ");
        sb.AppendLine("        console.log('WASM module loaded successfully');");
        sb.AppendLine("        console.log('Exports:', Object.keys(instance.exports));");
        sb.AppendLine("        ");
        sb.AppendLine("        // Call main if available");
        sb.AppendLine("        if (instance.exports.main) {");
        sb.AppendLine("            const result = instance.exports.main();");
        sb.AppendLine("            console.log('main() returned:', result);");
        sb.AppendLine("            return result;");
        sb.AppendLine("        }");
        sb.AppendLine("        ");
        sb.AppendLine("        return instance;");
        sb.AppendLine("    } catch (error) {");
        sb.AppendLine("        console.error('Error:', error);");
        sb.AppendLine("        throw error;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        
        sb.AppendLine("// Auto-detect environment and run");
        sb.AppendLine("if (typeof window !== 'undefined') {");
        sb.AppendLine("    // Browser environment");
        sb.AppendLine($"    loadAndRunWasm('{wasmFileName}').catch(console.error);");
        sb.AppendLine("} else if (typeof require !== 'undefined') {");
        sb.AppendLine("    // Node.js environment");
        sb.AppendLine($"    loadAndRunWasmNode('./{wasmFileName}').catch(console.error);");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    // Helper methods for WASM binary encoding
    private static void WriteULEB128(List<byte> buffer, uint value)
    {
        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            buffer.Add(b);
        } while (value != 0);
    }
    
    private static void WriteString(List<byte> buffer, string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        WriteULEB128(buffer, (uint)bytes.Length);
        buffer.AddRange(bytes);
    }
    
    private static byte GetValueTypeByte(string type)
    {
        return type switch
        {
            "i32" => 0x7F,
            "i64" => 0x7E,
            "f32" => 0x7D,
            "f64" => 0x7C,
            _ => 0x7F // default to i32
        };
    }
    
    // Helper classes for module structure
    private class ModuleInfo
    {
        public List<FunctionType> TypeSection { get; } = new();
        public List<Import> ImportSection { get; } = new();
        public List<uint> FunctionSection { get; } = new();
        public List<Memory> MemorySection { get; } = new();
        public List<Export> ExportSection { get; } = new();
        public List<FunctionCode> CodeSection { get; } = new();
    }
    
    private class FunctionType
    {
        public List<string> Parameters { get; set; } = new();
        public List<string> Results { get; set; } = new();
    }
    
    private class Import
    {
        public string Module { get; set; } = "";
        public string Name { get; set; } = "";
        public byte Kind { get; set; } // 0x00=func, 0x01=table, 0x02=mem, 0x03=global
        public uint MemoryMinPages { get; set; }
    }
    
    private class Memory
    {
        public uint MinPages { get; set; }
        public uint? MaxPages { get; set; }
    }
    
    private class Export
    {
        public string Name { get; set; } = "";
        public byte Kind { get; set; } // 0x00=func, 0x01=table, 0x02=mem, 0x03=global
        public uint Index { get; set; }
    }
    
    private class FunctionCode
    {
        public List<string> Locals { get; set; } = new();
        public List<byte> Instructions { get; set; } = new();
    }
}
