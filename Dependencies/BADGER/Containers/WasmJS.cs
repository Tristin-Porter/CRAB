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
        
        if (moduleInfo.DataSection.Count > 0)
            EmitDataSection(wasm, moduleInfo.DataSection);
        
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
            // Import description
            if (import.Kind == 0x00) // function
            {
                WriteULEB128(sectionData, import.TypeIndex);
            }
            else if (import.Kind == 0x02) // memory
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
    
    private static void EmitDataSection(List<byte> wasm, List<DataSegment> dataSegments)
    {
        var sectionData = new List<byte>();
        
        WriteULEB128(sectionData, (uint)dataSegments.Count);
        foreach (var data in dataSegments)
        {
            sectionData.Add(0x00); // memory index (always 0 for MVP)
            
            // Offset expression (i32.const)
            sectionData.Add(0x41); // i32.const opcode
            EncodeSLEB128(sectionData, (int)data.Offset);
            sectionData.Add(0x0B); // end opcode
            
            // Data bytes
            WriteULEB128(sectionData, (uint)data.Data.Length);
            sectionData.AddRange(data.Data);
        }
        
        wasm.Add(11); // data section id
        WriteULEB128(wasm, (uint)sectionData.Count);
        wasm.AddRange(sectionData);
    }
    
    private static ModuleInfo ParseWatModule(string watText)
    {
        var info = new ModuleInfo();
        
        // Parse WAT text format into module structure
        // WAT is S-expression based format: (module ...)
        
        // Remove comments and normalize whitespace
        watText = RemoveWatComments(watText);
        
        // Parse the module
        var tokens = TokenizeWat(watText);
        if (tokens.Count == 0) return info;
        
        // Find module boundaries  
        int moduleStart = -1;
        for (int i = 0; i < tokens.Count - 1; i++)
        {
            if (tokens[i] == "(" && tokens[i + 1] == "module")
            {
                moduleStart = i;
                break;
            }
        }
        if (moduleStart == -1) return info;
        
        // Parse sections within module
        int pos = moduleStart + 2; // Skip ( and module keyword
        while (pos < tokens.Count)
        {
            if (tokens[pos] == ")")
                break;
                
            if (tokens[pos] == "(" && pos + 1 < tokens.Count)
            {
                string keyword = tokens[pos + 1];
                if (keyword == "import")
                {
                    pos = ParseImport(tokens, pos, info);
                }
                else if (keyword == "memory")
                {
                    pos = ParseMemory(tokens, pos, info);
                }
                else if (keyword == "func")
                {
                    pos = ParseFunction(tokens, pos, info);
                }
                else if (keyword == "export")
                {
                    pos = ParseExport(tokens, pos, info);
                }
                else if (keyword == "data")
                {
                    pos = ParseData(tokens, pos, info);
                }
                else
                {
                    pos = SkipToClosingParen(tokens, pos);
                }
            }
            else
            {
                pos++;
            }
        }
        
        return info;
    }
    
    private static string RemoveWatComments(string wat)
    {
        // Remove line comments (;;...)
        var lines = wat.Split('\n');
        var result = new StringBuilder();
        foreach (var line in lines)
        {
            int commentPos = line.IndexOf(";;");
            if (commentPos >= 0)
                result.AppendLine(line.Substring(0, commentPos));
            else
                result.AppendLine(line);
        }
        return result.ToString();
    }
    
    private static List<string> TokenizeWat(string wat)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        bool inString = false;
        
        for (int i = 0; i < wat.Length; i++)
        {
            char c = wat[i];
            
            if (c == '"')
            {
                if (inString)
                {
                    current.Append(c);
                    tokens.Add(current.ToString());
                    current.Clear();
                    inString = false;
                }
                else
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    current.Append(c);
                    inString = true;
                }
            }
            else if (inString)
            {
                current.Append(c);
            }
            else if (c == '(' || c == ')')
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
                tokens.Add(c.ToString());
            }
            else if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }
        
        if (current.Length > 0)
            tokens.Add(current.ToString());
        
        return tokens;
    }
    
    private static int FindToken(List<string> tokens, int start, string target)
    {
        for (int i = start; i < tokens.Count; i++)
        {
            if (tokens[i] == target)
                return i;
        }
        return -1;
    }
    
    private static int ParseImport(List<string> tokens, int pos, ModuleInfo info)
    {
        // pos points to '(', next is 'import'
        int startPos = pos;
        pos += 2; // skip ( and import
        
        var import_ = new Import();
        
        if (pos < tokens.Count && tokens[pos].StartsWith("\""))
            import_.Module = tokens[pos++].Trim('"');
        
        if (pos < tokens.Count && tokens[pos].StartsWith("\""))
            import_.Name = tokens[pos++].Trim('"');
        
        // Parse import kind
        if (pos < tokens.Count && tokens[pos] == "(")
        {
            pos++;
            if (pos < tokens.Count)
            {
                string kind = tokens[pos++];
                if (kind == "func")
                {
                    import_.Kind = 0x00;
                    
                    // Register function name if present
                    if (pos < tokens.Count && tokens[pos].StartsWith("$"))
                    {
                        string funcName = tokens[pos++];
                        info.Symbols.Functions[funcName] = (uint)info.Symbols.Functions.Count;
                    }
                    
                    // Parse function type signature and add to type section
                    var funcType = new FunctionType();
                    
                    while (pos < tokens.Count && tokens[pos] == "(")
                    {
                        int paramStart = pos;
                        pos++;
                        if (pos < tokens.Count)
                        {
                            if (tokens[pos] == "param")
                            {
                                pos++;
                                while (pos < tokens.Count && tokens[pos] != ")")
                                {
                                    if (IsValueType(tokens[pos]))
                                        funcType.Parameters.Add(tokens[pos++]);
                                    else
                                        pos++;
                                }
                                pos++; // skip )
                            }
                            else if (tokens[pos] == "result")
                            {
                                pos++;
                                while (pos < tokens.Count && tokens[pos] != ")")
                                {
                                    if (IsValueType(tokens[pos]))
                                        funcType.Results.Add(tokens[pos++]);
                                    else
                                        pos++;
                                }
                                pos++; // skip )
                            }
                            else
                            {
                                // Not param/result, back up
                                pos = paramStart;
                                break;
                            }
                        }
                    }
                    
                    // Add function type to type section if it has a signature
                    if (funcType.Parameters.Count > 0 || funcType.Results.Count > 0)
                    {
                        import_.TypeIndex = (uint)info.TypeSection.Count;
                        info.TypeSection.Add(funcType);
                    }
                }
                else if (kind == "memory")
                {
                    import_.Kind = 0x02;
                    if (pos < tokens.Count && tokens[pos].Length > 0 && char.IsDigit(tokens[pos][0]))
                        import_.MemoryMinPages = uint.Parse(tokens[pos++]);
                }
            }
        }
        
        info.ImportSection.Add(import_);
        
        // Skip to the closing paren of the entire import
        return SkipToClosingParen(tokens, startPos);
    }
    
    private static int ParseMemory(List<string> tokens, int pos, ModuleInfo info)
    {
        // pos points to '(', next is 'memory'
        int startPos = pos;
        pos += 2; // skip ( and memory
        
        var memory = new Memory();
        if (pos < tokens.Count && tokens[pos].Length > 0 && char.IsDigit(tokens[pos][0]))
            memory.MinPages = uint.Parse(tokens[pos++]);
        
        if (pos < tokens.Count && tokens[pos] != ")" && tokens[pos].Length > 0 && char.IsDigit(tokens[pos][0]))
            memory.MaxPages = uint.Parse(tokens[pos++]);
        
        info.MemorySection.Add(memory);
        return SkipToClosingParen(tokens, startPos);
    }
    
    private static int ParseFunction(List<string> tokens, int pos, ModuleInfo info)
    {
        // pos points to '(', next is 'func'
        int startPos = pos;
        pos += 2; // skip ( and func
        
        var funcType = new FunctionType();
        var funcCode = new FunctionCode();
        var instructions = new List<byte>();
        
        // Clear local symbol table for this function
        info.Symbols.ClearLocals();
        uint localIndex = 0;
        
        // Skip/register function name if present
        if (pos < tokens.Count && tokens[pos].StartsWith("$"))
        {
            string funcName = tokens[pos++];
            uint funcIndex = (uint)(info.Symbols.Functions.Count + info.FunctionSection.Count);
            info.Symbols.Functions[funcName] = funcIndex;
        }
        
        // Parse parameters and results
        while (pos < tokens.Count && tokens[pos] == "(")
        {
            pos++;
            if (pos >= tokens.Count) break;
            
            if (tokens[pos] == "param")
            {
                pos++;
                while (pos < tokens.Count && tokens[pos] != ")")
                {
                    if (tokens[pos].StartsWith("$"))
                    {
                        string paramName = tokens[pos++];
                        info.Symbols.Locals[paramName] = localIndex++;
                    }
                    if (pos < tokens.Count && IsValueType(tokens[pos]))
                        funcType.Parameters.Add(tokens[pos++]);
                }
                pos++; // skip )
            }
            else if (tokens[pos] == "result")
            {
                pos++;
                while (pos < tokens.Count && tokens[pos] != ")")
                {
                    if (IsValueType(tokens[pos]))
                        funcType.Results.Add(tokens[pos++]);
                }
                pos++; // skip )
            }
            else if (tokens[pos] == "local")
            {
                pos++;
                while (pos < tokens.Count && tokens[pos] != ")")
                {
                    if (tokens[pos].StartsWith("$"))
                    {
                        string localName = tokens[pos++];
                        info.Symbols.Locals[localName] = localIndex++;
                    }
                    if (pos < tokens.Count && IsValueType(tokens[pos]))
                        funcCode.Locals.Add(tokens[pos++]);
                }
                pos++; // skip )
            }
            else
            {
                // Not a param/result/local, back up
                pos--;
                break;
            }
        }
        
        // Parse instructions
        while (pos < tokens.Count && tokens[pos] != ")")
        {
            if (tokens[pos] == "(")
            {
                pos++; // skip nested constructs
            }
            else
            {
                var instr = tokens[pos++];
                EncodeInstruction(instr, tokens, ref pos, instructions, info.Symbols);
            }
        }
        
        funcCode.Instructions = instructions;
        
        // Add to module info
        info.TypeSection.Add(funcType);
        info.FunctionSection.Add((uint)(info.TypeSection.Count - 1));
        info.CodeSection.Add(funcCode);
        
        return SkipToClosingParen(tokens, startPos);
    }
    
    private static int ParseExport(List<string> tokens, int pos, ModuleInfo info)
    {
        // pos points to '(', next is 'export'
        int startPos = pos;
        pos += 2; // skip ( and export
        
        var export_ = new Export();
        
        if (pos < tokens.Count && tokens[pos].StartsWith("\""))
            export_.Name = tokens[pos++].Trim('"');
        
        if (pos < tokens.Count && tokens[pos] == "(")
        {
            pos++;
            if (pos < tokens.Count)
            {
                string kind = tokens[pos++];
                if (kind == "func")
                {
                    export_.Kind = 0x00;
                    if (pos < tokens.Count && tokens[pos].StartsWith("$"))
                    {
                        // Function reference - resolve from symbol table
                        string funcName = tokens[pos++];
                        export_.Index = info.Symbols.Functions.ContainsKey(funcName) 
                            ? info.Symbols.Functions[funcName]
                            : (uint)(info.FunctionSection.Count > 0 ? info.FunctionSection.Count - 1 : 0);
                    }
                    else if (pos < tokens.Count && tokens[pos].Length > 0 && char.IsDigit(tokens[pos][0]))
                    {
                        export_.Index = uint.Parse(tokens[pos++]);
                    }
                }
            }
        }
        
        info.ExportSection.Add(export_);
        return SkipToClosingParen(tokens, startPos);
    }
    
    private static int ParseData(List<string> tokens, int pos, ModuleInfo info)
    {
        // (data (i32.const offset) "string")
        int startPos = pos;
        pos += 2; // skip ( and data
        
        var dataSegment = new DataSegment();
        
        // Parse offset expression
        if (pos < tokens.Count && tokens[pos] == "(")
        {
            pos++; // skip (
            if (pos < tokens.Count && tokens[pos] == "i32.const")
            {
                pos++; // skip i32.const
                if (pos < tokens.Count && tokens[pos].Length > 0 && char.IsDigit(tokens[pos][0]))
                {
                    dataSegment.Offset = uint.Parse(tokens[pos++]);
                }
                pos++; // skip )
            }
        }
        
        // Parse data string
        if (pos < tokens.Count && tokens[pos].StartsWith("\""))
        {
            string dataStr = tokens[pos++].Trim('"');
            // Handle escape sequences
            dataStr = dataStr.Replace("\\00", "\0")
                            .Replace("\\n", "\n")
                            .Replace("\\r", "\r")
                            .Replace("\\t", "\t")
                            .Replace("\\\"", "\"")
                            .Replace("\\\\", "\\");
            dataSegment.Data = Encoding.UTF8.GetBytes(dataStr);
        }
        
        info.DataSection.Add(dataSegment);
        
        return SkipToClosingParen(tokens, startPos);
    }
    
    private static bool IsValueType(string token)
    {
        return token == "i32" || token == "i64" || token == "f32" || token == "f64";
    }
    
    private static int SkipToClosingParen(List<string> tokens, int pos)
    {
        // pos should point to the opening '('
        if (pos >= tokens.Count || tokens[pos] != "(")
            return pos;
            
        int depth = 1; // We're starting inside the paren
        pos++; // Move past the opening paren
        
        while (pos < tokens.Count)
        {
            if (tokens[pos] == "(")
                depth++;
            else if (tokens[pos] == ")")
            {
                depth--;
                if (depth == 0)
                    return pos + 1; // Return position after the closing paren
            }
            pos++;
        }
        return pos;
    }
    
    private static void EncodeInstruction(string instr, List<string> tokens, ref int pos, List<byte> output, SymbolTable symbols)
    {
        // Encode WAT instructions to WASM binary opcodes
        switch (instr)
        {
            case "i32.const":
                output.Add(0x41);
                if (pos < tokens.Count && !tokens[pos].StartsWith("(") && tokens[pos] != ")")
                {
                    int value = int.Parse(tokens[pos++]);
                    EncodeSLEB128(output, value);
                }
                break;
            case "i32.add":
                output.Add(0x6A);
                break;
            case "i32.sub":
                output.Add(0x6B);
                break;
            case "i32.mul":
                output.Add(0x6C);
                break;
            case "local.get":
                output.Add(0x20);
                if (pos < tokens.Count && !tokens[pos].StartsWith("(") && tokens[pos] != ")")
                {
                    if (tokens[pos].StartsWith("$"))
                    {
                        string localName = tokens[pos++];
                        uint idx = symbols.Locals.ContainsKey(localName) ? symbols.Locals[localName] : 0;
                        WriteULEB128ToList(output, idx);
                    }
                    else
                    {
                        uint idx = uint.Parse(tokens[pos++]);
                        WriteULEB128ToList(output, idx);
                    }
                }
                break;
            case "local.set":
                output.Add(0x21);
                if (pos < tokens.Count && !tokens[pos].StartsWith("(") && tokens[pos] != ")")
                {
                    if (tokens[pos].StartsWith("$"))
                    {
                        string localName = tokens[pos++];
                        uint idx = symbols.Locals.ContainsKey(localName) ? symbols.Locals[localName] : 0;
                        WriteULEB128ToList(output, idx);
                    }
                    else
                    {
                        uint idx = uint.Parse(tokens[pos++]);
                        WriteULEB128ToList(output, idx);
                    }
                }
                break;
            case "call":
                output.Add(0x10);
                if (pos < tokens.Count && !tokens[pos].StartsWith("(") && tokens[pos] != ")")
                {
                    if (tokens[pos].StartsWith("$"))
                    {
                        string funcName = tokens[pos++];
                        uint idx = symbols.Functions.ContainsKey(funcName) ? symbols.Functions[funcName] : 0;
                        WriteULEB128ToList(output, idx);
                    }
                    else
                    {
                        uint idx = uint.Parse(tokens[pos++]);
                        WriteULEB128ToList(output, idx);
                    }
                }
                break;
            case "return":
                output.Add(0x0F);
                break;
            case "nop":
                output.Add(0x01);
                break;
            // Add more opcodes as needed
        }
    }
    
    private static void EncodeSLEB128(List<byte> buffer, int value)
    {
        bool more = true;
        while (more)
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            
            if ((value == 0 && (b & 0x40) == 0) || (value == -1 && (b & 0x40) != 0))
            {
                more = false;
            }
            else
            {
                b |= 0x80;
            }
            
            buffer.Add(b);
        }
    }
    
    private static void WriteULEB128ToList(List<byte> buffer, uint value)
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
        sb.AppendLine("                console_log: (offset) => {");
        sb.AppendLine("                    // Read string from memory at offset");
        sb.AppendLine("                    const memory = imports.env.memory;");
        sb.AppendLine("                    const bytes = new Uint8Array(memory.buffer, offset);");
        sb.AppendLine("                    let str = '';");
        sb.AppendLine("                    for (let i = 0; bytes[i] !== 0; i++) {");
        sb.AppendLine("                        str += String.fromCharCode(bytes[i]);");
        sb.AppendLine("                    }");
        sb.AppendLine("                    console.log(str);");
        sb.AppendLine("                }");
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
        sb.AppendLine("                memory: new WebAssembly.Memory({ initial: 1 }),");
        sb.AppendLine("                console_log: (offset) => {");
        sb.AppendLine("                    // Read string from memory at offset");
        sb.AppendLine("                    const memory = imports.env.memory;");
        sb.AppendLine("                    const bytes = new Uint8Array(memory.buffer, offset);");
        sb.AppendLine("                    let str = '';");
        sb.AppendLine("                    for (let i = 0; bytes[i] !== 0; i++) {");
        sb.AppendLine("                        str += String.fromCharCode(bytes[i]);");
        sb.AppendLine("                    }");
        sb.AppendLine("                    console.log(str);");
        sb.AppendLine("                }");
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
        public List<DataSegment> DataSection { get; } = new();
        public SymbolTable Symbols { get; } = new();
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
        public uint TypeIndex { get; set; }
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
    
    private class DataSegment
    {
        public uint Offset { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }
    
    private class SymbolTable
    {
        public Dictionary<string, uint> Functions { get; } = new();
        public Dictionary<string, uint> Locals { get; } = new();
        public Dictionary<string, uint> Globals { get; } = new();
        
        public void Clear()
        {
            Functions.Clear();
            Locals.Clear();
            Globals.Clear();
        }
        
        public void ClearLocals()
        {
            Locals.Clear();
        }
    }
}
