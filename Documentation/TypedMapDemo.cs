using CDTk;
using CRAB;
using System;

/// <summary>
/// Demonstration of the new Typed Map API.
/// This example shows how to use typed Maps with WASM IR output.
/// </summary>
public class TypedMapDemo : MapSet
{
    // ============================================================
    // Example 1: Simple Typed Map (Emit only)
    // ============================================================
    
    // This map generates a WasmInstruction directly from an AST node
    // No string templates, no placeholder substitution
    public Map<AstNode, WasmInstruction> IntegerLiteral_Typed = TypedMap.For<WasmInstruction>()
        .Emit(node =>
        {
            var value = int.Parse(node["value"] as string ?? "0");
            return new WasmInstruction(OpCode.I32Const, value) 
            { 
                Comment = $"literal {value}" 
            };
        });
    
    // ============================================================
    // Example 2: Sequence of Instructions
    // ============================================================
    
    // Generate a sequence of WASM instructions
    public Map<AstNode, WasmInstructionSequence> BinaryExpression_Typed = TypedMap.For<WasmInstructionSequence>()
        .Emit(node =>
        {
            var seq = new WasmInstructionSequence();
            
            // Emit left operand
            seq.Add(new WasmInstruction(OpCode.LocalGet, 0) { Comment = "left operand" });
            
            // Emit right operand
            seq.Add(new WasmInstruction(OpCode.LocalGet, 1) { Comment = "right operand" });
            
            // Emit operator
            var op = node["op"] as string ?? "+";
            var opCode = op switch
            {
                "+" => OpCode.I32Add,
                "-" => OpCode.I32Sub,
                "*" => OpCode.I32Mul,
                "/" => OpCode.I32DivS,
                _ => OpCode.Nop
            };
            
            seq.Add(new WasmInstruction(opCode) { Comment = $"operator {op}" });
            
            return seq;
        });
    
    // ============================================================
    // Example 3: Complete Function
    // ============================================================
    
    // Generate a complete WasmFunction structure
    public Map<AstNode, WasmFunction> SimpleFunction_Typed = TypedMap.For<WasmFunction>()
        .Emit(node =>
        {
            var func = new WasmFunction
            {
                Name = node["name"] as string ?? "unnamed",
                IsExport = true
            };
            
            // Add parameters
            func.Type.Parameters.Add(WasmType.I32);
            func.Type.Parameters.Add(WasmType.I32);
            
            // Add return type
            func.Type.Results.Add(WasmType.I32);
            
            // Build body
            func.Body.Add(new WasmInstruction(OpCode.LocalGet, 0));
            func.Body.Add(new WasmInstruction(OpCode.LocalGet, 1));
            func.Body.Add(new WasmInstruction(OpCode.I32Add));
            func.Body.Add(new WasmInstruction(OpCode.Return));
            
            return func;
        });
    
    // ============================================================
    // Example 4: String Output (Backward Compatible)
    // ============================================================
    
    // Typed maps can also output strings for backward compatibility
    public Map<AstNode, string> SimpleStatement_Typed = TypedMap.For<string>()
        .Emit(node =>
        {
            var expr = node["expr"] as string ?? "";
            return $"{expr}\n";
        });
    
    // ============================================================
    // Comparison: Old String-Based Map
    // ============================================================
    
    // Old API still works - these are equivalent for simple cases
    public Map IntegerLiteral_OldStyle = "i32.const {value}";
    public Map SimpleStatement_OldStyle = "{expr}\n";
}

/// <summary>
/// Usage examples for the typed Map API.
/// </summary>
public class TypedMapUsageExamples
{
    public static void Example1_BasicUsage()
    {
        // Create a simple AST node
        var node = new AstNode("IntegerLiteral");
        node["value"] = "42";
        
        // Create MapSet
        var maps = new TypedMapDemo();
        
        // Generate WasmInstruction using typed map
        // Note: This requires direct access to the map, not Transform()
        // Transform() is for string output compatibility
        
        System.Console.WriteLine("Example 1: Basic Typed Map Usage");
        System.Console.WriteLine("================================");
    }
    
    public static void Example2_CompleteFunction()
    {
        // Create function node
        var node = new AstNode("SimpleFunction");
        node["name"] = "add";
        
        var maps = new TypedMapDemo();
        
        // The typed map would generate a WasmFunction
        // which can be converted to WAT:
        // (func $add (param i32) (param i32) (result i32)
        //   local.get 0
        //   local.get 1
        //   i32.add
        //   return
        // )
        
        System.Console.WriteLine("Example 2: Complete Function");
        System.Console.WriteLine("============================");
    }
    
    public static void Example3_CompleteModule()
    {
        // Build a complete WASM module
        var module = new WasmModule();
        
        // Add a function
        var func = new WasmFunction { Name = "add", IsExport = true };
        func.Type.Parameters.Add(WasmType.I32);
        func.Type.Parameters.Add(WasmType.I32);
        func.Type.Results.Add(WasmType.I32);
        func.Body.Add(new WasmInstruction(OpCode.LocalGet, 0));
        func.Body.Add(new WasmInstruction(OpCode.LocalGet, 1));
        func.Body.Add(new WasmInstruction(OpCode.I32Add));
        
        module.Functions.Add(func);
        module.Exports.Add("add");
        
        // Convert to WAT
        var wat = module.ToWat();
        System.Console.WriteLine("Example 3: Complete Module");
        System.Console.WriteLine("===========================");
        System.Console.WriteLine(wat);
        
        // Or encode to binary
        var encoder = new WasmBinaryEncoder();
        var binary = encoder.Encode(module);
        System.Console.WriteLine($"\nBinary size: {binary.Length} bytes");
    }
}
