using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRAB;

/// <summary>
/// WASM Intermediate Representation (IR) types for typed MapSet output.
/// Replaces string templates with strongly-typed IR nodes that can be:
/// - Serialized to WAT (WebAssembly Text format)
/// - Encoded to binary WASM
/// - Optimized and transformed
/// - Validated for correctness
/// </summary>

// ============================================================
// WASM Instruction IR
// ============================================================

/// <summary>
/// Represents a single WASM instruction with opcode and operands.
/// This is the fundamental unit of WASM code generation.
/// </summary>
public class WasmInstruction
{
    /// <summary>The WASM opcode for this instruction.</summary>
    public OpCode OpCode { get; }

    /// <summary>Operands for this instruction (immediates, labels, etc).</summary>
    public object[] Operands { get; }

    /// <summary>Optional comment for debugging/readability.</summary>
    public string? Comment { get; set; }

    public WasmInstruction(OpCode opCode, params object[] operands)
    {
        OpCode = opCode;
        Operands = operands ?? Array.Empty<object>();
    }

    /// <summary>Convert instruction to WAT text format.</summary>
    public string ToWat()
    {
        var sb = new StringBuilder();
        sb.Append(OpCode.ToWatString());

        if (Operands.Length > 0)
        {
            sb.Append(' ');
            sb.Append(string.Join(" ", Operands.Select(FormatOperand)));
        }

        if (!string.IsNullOrEmpty(Comment))
        {
            sb.Append($" ;; {Comment}");
        }

        return sb.ToString();
    }

    private string FormatOperand(object operand)
    {
        return operand switch
        {
            string s => s,
            int i => i.ToString(),
            long l => l.ToString(),
            float f => FormatFloat(f),
            double d => FormatDouble(d),
            WasmType t => t.ToWatString(),
            _ => operand.ToString() ?? ""
        };
    }

    private string FormatFloat(float value)
    {
        if (float.IsNaN(value)) return "nan";
        if (float.IsPositiveInfinity(value)) return "inf";
        if (float.IsNegativeInfinity(value)) return "-inf";
        return value.ToString("G9");
    }

    private string FormatDouble(double value)
    {
        if (double.IsNaN(value)) return "nan";
        if (double.IsPositiveInfinity(value)) return "inf";
        if (double.IsNegativeInfinity(value)) return "-inf";
        return value.ToString("G17");
    }

    public override string ToString() => ToWat();
}

/// <summary>
/// Sequence of WASM instructions (function body, block, etc).
/// </summary>
public class WasmInstructionSequence
{
    public List<WasmInstruction> Instructions { get; } = new List<WasmInstruction>();

    public void Add(WasmInstruction instruction)
    {
        Instructions.Add(instruction);
    }

    public void AddRange(IEnumerable<WasmInstruction> instructions)
    {
        Instructions.AddRange(instructions);
    }

    /// <summary>Convert sequence to WAT text format with indentation.</summary>
    public string ToWat(int indentLevel = 0)
    {
        var indent = new string(' ', indentLevel * 2);
        return string.Join("\n", Instructions.Select(i => indent + i.ToWat()));
    }

    public override string ToString() => ToWat();
}

// ============================================================
// WASM OpCode Enumeration
// ============================================================

/// <summary>
/// WASM MVP instruction opcodes.
/// Covers control flow, numeric operations, memory, and variables.
/// </summary>
public enum OpCode
{
    // Control Flow
    Unreachable,
    Nop,
    Block,
    Loop,
    If,
    Else,
    End,
    Br,
    BrIf,
    BrTable,
    Return,
    Call,
    CallIndirect,

    // Parametric
    Drop,
    Select,

    // Variable Access
    LocalGet,
    LocalSet,
    LocalTee,
    GlobalGet,
    GlobalSet,

    // Memory
    I32Load,
    I64Load,
    F32Load,
    F64Load,
    I32Load8S,
    I32Load8U,
    I32Load16S,
    I32Load16U,
    I64Load8S,
    I64Load8U,
    I64Load16S,
    I64Load16U,
    I64Load32S,
    I64Load32U,
    I32Store,
    I64Store,
    F32Store,
    F64Store,
    I32Store8,
    I32Store16,
    I64Store8,
    I64Store16,
    I64Store32,
    MemorySize,
    MemoryGrow,

    // Constants
    I32Const,
    I64Const,
    F32Const,
    F64Const,

    // I32 Operations
    I32Eqz,
    I32Eq,
    I32Ne,
    I32LtS,
    I32LtU,
    I32GtS,
    I32GtU,
    I32LeS,
    I32LeU,
    I32GeS,
    I32GeU,

    I32Clz,
    I32Ctz,
    I32Popcnt,
    I32Add,
    I32Sub,
    I32Mul,
    I32DivS,
    I32DivU,
    I32RemS,
    I32RemU,
    I32And,
    I32Or,
    I32Xor,
    I32Shl,
    I32ShrS,
    I32ShrU,
    I32Rotl,
    I32Rotr,

    // I64 Operations
    I64Eqz,
    I64Eq,
    I64Ne,
    I64LtS,
    I64LtU,
    I64GtS,
    I64GtU,
    I64LeS,
    I64LeU,
    I64GeS,
    I64GeU,

    I64Clz,
    I64Ctz,
    I64Popcnt,
    I64Add,
    I64Sub,
    I64Mul,
    I64DivS,
    I64DivU,
    I64RemS,
    I64RemU,
    I64And,
    I64Or,
    I64Xor,
    I64Shl,
    I64ShrS,
    I64ShrU,
    I64Rotl,
    I64Rotr,

    // F32 Operations
    F32Eq,
    F32Ne,
    F32Lt,
    F32Gt,
    F32Le,
    F32Ge,

    F32Abs,
    F32Neg,
    F32Ceil,
    F32Floor,
    F32Trunc,
    F32Nearest,
    F32Sqrt,
    F32Add,
    F32Sub,
    F32Mul,
    F32Div,
    F32Min,
    F32Max,
    F32Copysign,

    // F64 Operations
    F64Eq,
    F64Ne,
    F64Lt,
    F64Gt,
    F64Le,
    F64Ge,

    F64Abs,
    F64Neg,
    F64Ceil,
    F64Floor,
    F64Trunc,
    F64Nearest,
    F64Sqrt,
    F64Add,
    F64Sub,
    F64Mul,
    F64Div,
    F64Min,
    F64Max,
    F64Copysign,

    // Conversions
    I32WrapI64,
    I32TruncF32S,
    I32TruncF32U,
    I32TruncF64S,
    I32TruncF64U,
    I64ExtendI32S,
    I64ExtendI32U,
    I64TruncF32S,
    I64TruncF32U,
    I64TruncF64S,
    I64TruncF64U,
    F32ConvertI32S,
    F32ConvertI32U,
    F32ConvertI64S,
    F32ConvertI64U,
    F32DemoteF64,
    F64ConvertI32S,
    F64ConvertI32U,
    F64ConvertI64S,
    F64ConvertI64U,
    F64PromoteF32,
    I32ReinterpretF32,
    I64ReinterpretF64,
    F32ReinterpretI32,
    F64ReinterpretI64,
}

/// <summary>
/// Extension methods for OpCode to convert to WAT string format.
/// </summary>
public static class OpCodeExtensions
{
    public static string ToWatString(this OpCode opCode)
    {
        // Convert PascalCase enum to dot.notation
        var name = opCode.ToString();
        
        // Handle special cases
        return name switch
        {
            // Control Flow
            "Unreachable" => "unreachable",
            "Nop" => "nop",
            "Block" => "block",
            "Loop" => "loop",
            "If" => "if",
            "Else" => "else",
            "End" => "end",
            "Br" => "br",
            "BrIf" => "br_if",
            "BrTable" => "br_table",
            "Return" => "return",
            "Call" => "call",
            "CallIndirect" => "call_indirect",

            // Parametric
            "Drop" => "drop",
            "Select" => "select",

            // Variables
            "LocalGet" => "local.get",
            "LocalSet" => "local.set",
            "LocalTee" => "local.tee",
            "GlobalGet" => "global.get",
            "GlobalSet" => "global.set",

            // Memory
            "MemorySize" => "memory.size",
            "MemoryGrow" => "memory.grow",

            // Convert I32Add -> i32.add, etc.
            _ => ConvertToWatNotation(name)
        };
    }

    private static string ConvertToWatNotation(string name)
    {
        // Convert I32Add -> i32.add
        // Convert I64TruncF32S -> i64.trunc_f32_s
        var sb = new StringBuilder();
        
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            
            if (i > 0 && char.IsUpper(c))
            {
                // Check if previous was lowercase or this starts a new word
                if (char.IsLower(name[i - 1]))
                {
                    sb.Append('.');
                }
                else if (i + 1 < name.Length && char.IsLower(name[i + 1]))
                {
                    // Start of new word in sequence of caps (e.g., "TruncF32" -> trunc_f32)
                    if (sb.Length > 0 && sb[sb.Length - 1] != '.')
                    {
                        sb.Append('_');
                    }
                }
            }
            
            sb.Append(char.ToLower(c));
        }
        
        var result = sb.ToString();
        
        // Fix specific patterns that occur when converting compound opcodes
        // e.g., "I64TruncF32S" -> "i64_trunc_f32_s" -> "i64.trunc_f32_s"
        // These patterns appear because of the transition between type prefix and operation name
        result = result.Replace("_s_", "_");  // Fix I32TruncF32S pattern
        result = result.Replace("_u_", "_");  // Fix I32TruncF32U pattern
        
        return result;
    }
}

// ============================================================
// WASM Type System
// ============================================================

/// <summary>
/// WASM value types (MVP).
/// </summary>
public enum WasmType
{
    I32,
    I64,
    F32,
    F64,
    Void
}

/// <summary>
/// Extension methods for WasmType.
/// </summary>
public static class WasmTypeExtensions
{
    public static string ToWatString(this WasmType type)
    {
        return type switch
        {
            WasmType.I32 => "i32",
            WasmType.I64 => "i64",
            WasmType.F32 => "f32",
            WasmType.F64 => "f64",
            WasmType.Void => "",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    /// <summary>Map C# type to WASM type.</summary>
    public static WasmType FromCSharpType(string csharpType)
    {
        return csharpType switch
        {
            "int" => WasmType.I32,
            "uint" => WasmType.I32,
            "byte" => WasmType.I32,
            "sbyte" => WasmType.I32,
            "short" => WasmType.I32,
            "ushort" => WasmType.I32,
            "bool" => WasmType.I32,
            "char" => WasmType.I32,
            
            "long" => WasmType.I64,
            "ulong" => WasmType.I64,
            
            "float" => WasmType.F32,
            "double" => WasmType.F64,
            
            "void" => WasmType.Void,
            
            _ => WasmType.I32 // Default to i32 for unknown types
        };
    }
}

// ============================================================
// WASM Module Structure IR
// ============================================================

/// <summary>
/// WASM function signature (type).
/// </summary>
public class WasmFunctionType
{
    public List<WasmType> Parameters { get; } = new List<WasmType>();
    public List<WasmType> Results { get; } = new List<WasmType>();

    public string ToWat()
    {
        var sb = new StringBuilder();
        sb.Append("(func");

        if (Parameters.Count > 0)
        {
            foreach (var param in Parameters)
            {
                sb.Append($" (param {param.ToWatString()})");
            }
        }

        if (Results.Count > 0)
        {
            foreach (var result in Results)
            {
                sb.Append($" (result {result.ToWatString()})");
            }
        }

        sb.Append(')');
        return sb.ToString();
    }
}

/// <summary>
/// WASM function definition.
/// </summary>
public class WasmFunction
{
    public string Name { get; set; } = "";
    public WasmFunctionType Type { get; } = new WasmFunctionType();
    public List<(string Name, WasmType Type)> Locals { get; } = new List<(string, WasmType)>();
    public WasmInstructionSequence Body { get; } = new WasmInstructionSequence();
    public bool IsExport { get; set; }

    public string ToWat(int indentLevel = 1)
    {
        var sb = new StringBuilder();
        var indent = new string(' ', indentLevel * 2);

        // Function declaration
        sb.Append(indent);
        sb.Append($"(func ${Name}");

        // Parameters
        foreach (var param in Type.Parameters)
        {
            sb.Append($" (param {param.ToWatString()})");
        }

        // Result
        if (Type.Results.Count > 0)
        {
            foreach (var result in Type.Results)
            {
                sb.Append($" (result {result.ToWatString()})");
            }
        }

        sb.AppendLine();

        // Locals
        if (Locals.Count > 0)
        {
            foreach (var local in Locals)
            {
                sb.AppendLine($"{indent}  (local ${local.Name} {local.Type.ToWatString()})");
            }
        }

        // Body
        if (Body.Instructions.Count > 0)
        {
            sb.AppendLine(Body.ToWat(indentLevel + 1));
        }

        sb.Append(indent);
        sb.Append(')');

        return sb.ToString();
    }

    public override string ToString() => ToWat();
}

/// <summary>
/// WASM module (complete compilation unit).
/// </summary>
public class WasmModule
{
    public List<WasmFunction> Functions { get; } = new List<WasmFunction>();
    public List<string> Exports { get; } = new List<string>();
    public int MemoryPages { get; set; } = 1;

    public string ToWat()
    {
        var sb = new StringBuilder();
        sb.AppendLine("(module");

        // Memory
        sb.AppendLine($"  (memory {MemoryPages})");

        // Functions
        foreach (var func in Functions)
        {
            sb.AppendLine(func.ToWat());
        }

        // Exports
        foreach (var export in Exports)
        {
            sb.AppendLine($"  (export \"{export}\" (func ${export}))");
        }

        sb.Append(')');
        return sb.ToString();
    }

    public override string ToString() => ToWat();
}

// ============================================================
// Binary Encoding (for .wasm output)
// ============================================================

/// <summary>
/// Encoder for binary WASM format.
/// Converts WasmModule IR to .wasm binary.
/// </summary>
public class WasmBinaryEncoder
{
    private readonly List<byte> _buffer = new List<byte>();

    public byte[] Encode(WasmModule module)
    {
        _buffer.Clear();

        // WASM magic number: 0x00 0x61 0x73 0x6D
        _buffer.AddRange(new byte[] { 0x00, 0x61, 0x73, 0x6D });

        // WASM version: 1
        _buffer.AddRange(new byte[] { 0x01, 0x00, 0x00, 0x00 });

        // Type section
        EncodeTypeSection(module);

        // Function section
        EncodeFunctionSection(module);

        // Memory section
        EncodeMemorySection(module);

        // Export section
        EncodeExportSection(module);

        // Code section
        EncodeCodeSection(module);

        return _buffer.ToArray();
    }

    private void EncodeTypeSection(WasmModule module)
    {
        // Section 1: Type
        _buffer.Add(0x01);

        var types = new List<WasmFunctionType>();
        foreach (var func in module.Functions)
        {
            types.Add(func.Type);
        }

        var sectionData = new List<byte>();
        EncodeU32((uint)types.Count, sectionData);

        foreach (var type in types)
        {
            sectionData.Add(0x60); // func type
            EncodeU32((uint)type.Parameters.Count, sectionData);
            foreach (var param in type.Parameters)
            {
                sectionData.Add(EncodeValueType(param));
            }
            EncodeU32((uint)type.Results.Count, sectionData);
            foreach (var result in type.Results)
            {
                sectionData.Add(EncodeValueType(result));
            }
        }

        EncodeU32((uint)sectionData.Count, _buffer);
        _buffer.AddRange(sectionData);
    }

    private void EncodeFunctionSection(WasmModule module)
    {
        // Section 3: Function
        _buffer.Add(0x03);

        var sectionData = new List<byte>();
        EncodeU32((uint)module.Functions.Count, sectionData);

        for (int i = 0; i < module.Functions.Count; i++)
        {
            EncodeU32((uint)i, sectionData); // type index
        }

        EncodeU32((uint)sectionData.Count, _buffer);
        _buffer.AddRange(sectionData);
    }

    private void EncodeMemorySection(WasmModule module)
    {
        // Section 5: Memory
        _buffer.Add(0x05);

        var sectionData = new List<byte>();
        EncodeU32(1, sectionData); // 1 memory

        sectionData.Add(0x00); // no max
        EncodeU32((uint)module.MemoryPages, sectionData);

        EncodeU32((uint)sectionData.Count, _buffer);
        _buffer.AddRange(sectionData);
    }

    private void EncodeExportSection(WasmModule module)
    {
        if (module.Exports.Count == 0) return;

        // Section 7: Export
        _buffer.Add(0x07);

        var sectionData = new List<byte>();
        EncodeU32((uint)module.Exports.Count, sectionData);

        for (int i = 0; i < module.Exports.Count; i++)
        {
            var name = module.Exports[i];
            EncodeString(name, sectionData);
            sectionData.Add(0x00); // func export
            EncodeU32((uint)i, sectionData); // func index
        }

        EncodeU32((uint)sectionData.Count, _buffer);
        _buffer.AddRange(sectionData);
    }

    private void EncodeCodeSection(WasmModule module)
    {
        // Section 10: Code
        _buffer.Add(0x0A);

        var sectionData = new List<byte>();
        EncodeU32((uint)module.Functions.Count, sectionData);

        foreach (var func in module.Functions)
        {
            var funcData = new List<byte>();

            // Locals
            EncodeU32((uint)func.Locals.Count, funcData);
            foreach (var local in func.Locals)
            {
                EncodeU32(1, funcData); // count
                funcData.Add(EncodeValueType(local.Type));
            }

            // Body instructions
            foreach (var instr in func.Body.Instructions)
            {
                EncodeInstruction(instr, funcData);
            }

            // End
            funcData.Add(0x0B);

            EncodeU32((uint)funcData.Count, sectionData);
            sectionData.AddRange(funcData);
        }

        EncodeU32((uint)sectionData.Count, _buffer);
        _buffer.AddRange(sectionData);
    }

    private void EncodeInstruction(WasmInstruction instr, List<byte> buffer)
    {
        // Simplified instruction encoding
        buffer.Add(GetOpCodeByte(instr.OpCode));

        // Encode operands based on opcode
        switch (instr.OpCode)
        {
            case OpCode.I32Const:
                EncodeI32((int)instr.Operands[0], buffer);
                break;
            case OpCode.I64Const:
                EncodeI64((long)instr.Operands[0], buffer);
                break;
            case OpCode.LocalGet:
            case OpCode.LocalSet:
            case OpCode.LocalTee:
                EncodeU32(Convert.ToUInt32(instr.Operands[0]), buffer);
                break;
            // Add more operand encodings as needed
        }
    }

    private byte GetOpCodeByte(OpCode opCode)
    {
        // Simplified opcode mapping for basic WASM MVP instructions
        // NOTE: This is a partial stub implementation for demonstration.
        // A complete implementation would map all 256+ WASM opcodes.
        // Unmapped opcodes return 0x00 (Unreachable) as a safe default that
        // will cause WASM validation to fail rather than silently executing wrong code.
        
        return opCode switch
        {
            OpCode.Unreachable => 0x00,
            OpCode.Nop => 0x01,
            OpCode.Return => 0x0F,
            OpCode.LocalGet => 0x20,
            OpCode.LocalSet => 0x21,
            OpCode.I32Const => 0x41,
            OpCode.I32Add => 0x6A,
            OpCode.End => 0x0B,
            
            // TODO: Add complete opcode mappings for production use
            // For now, unmapped opcodes return 0x00 to fail WASM validation
            _ => 0x00  // Unreachable - causes WASM validation failure
        };
    }

    private byte EncodeValueType(WasmType type)
    {
        return type switch
        {
            WasmType.I32 => 0x7F,
            WasmType.I64 => 0x7E,
            WasmType.F32 => 0x7D,
            WasmType.F64 => 0x7C,
            _ => 0x7F
        };
    }

    private void EncodeU32(uint value, List<byte> buffer)
    {
        // LEB128 unsigned
        do
        {
            byte b = (byte)(value & 0x7F);
            value >>= 7;
            if (value != 0)
                b |= 0x80;
            buffer.Add(b);
        } while (value != 0);
    }

    private void EncodeI32(int value, List<byte> buffer)
    {
        // LEB128 signed
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

    private void EncodeI64(long value, List<byte> buffer)
    {
        // LEB128 signed 64-bit
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

    private void EncodeString(string value, List<byte> buffer)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        EncodeU32((uint)bytes.Length, buffer);
        buffer.AddRange(bytes);
    }
}
