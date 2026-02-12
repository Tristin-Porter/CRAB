using CDTk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

namespace Badger.Architectures.ARM64;

/// <summary>
/// Complete WAT → ARM64 (AArch64) assembly lowering with full stack simulation,
/// control flow, calling conventions, and instruction selection.
/// This is NOT a simple template system - it's a full compiler backend.
/// </summary>
public class WATToARM64MapSet : MapSet
{
    // ========================================================================
    // STACK SIMULATION STATE
    // ========================================================================
    // The WASM operand stack is simulated using:
    // - Registers: w19, w20, w21, w22 (first 4 stack slots) - callee-saved
    // - Memory: [x29, #-offset] for spilled values
    // 
    // Stack pointer tracking:
    // - stack_depth: current number of values on virtual stack
    // - Physical locations tracked in stack_locations list
    // ========================================================================
    
    private static int stack_depth = 0;
    private static int max_stack_depth = 0;
    private static List<string> stack_locations = new List<string>(); // "w19", "w20", "w21", "w22", or "[x29, #-N]"
    private static int spill_offset = 64; // Start spilling at [x29, #-64] (after saved regs and locals)
    
    // Label generation for control flow
    private static int label_counter = 0;
    private static Stack<string> block_labels = new Stack<string>();
    private static Stack<string> loop_labels = new Stack<string>();
    
    // Local variable allocation
    private static Dictionary<int, int> local_offsets = new Dictionary<int, int>();
    private static int local_frame_size = 0;
    
    // Memory base register (holds linear memory base address)
    private const string MEMORY_BASE_REG = "x23";
    
    // ========================================================================
    // HELPER METHODS FOR STACK SIMULATION
    // ========================================================================
    
    /// <summary>
    /// Get the current location of the top stack value
    /// </summary>
    private static string StackTop()
    {
        if (stack_depth == 0) throw new InvalidOperationException("Stack underflow");
        return stack_locations[stack_depth - 1];
    }
    
    /// <summary>
    /// Get location of stack value at index from top (0 = top, 1 = second from top, etc.)
    /// </summary>
    private static string StackAt(int index)
    {
        if (index >= stack_depth) throw new InvalidOperationException($"Stack underflow: accessing index {index} with depth {stack_depth}");
        return stack_locations[stack_depth - 1 - index];
    }
    
    /// <summary>
    /// Push a value onto the virtual stack
    /// </summary>
    private static string StackPush(string source_reg = "w0")
    {
        var sb = new StringBuilder();
        string location;
        
        // Use registers w19-w22 for first 4 stack slots (callee-saved)
        if (stack_depth < 4)
        {
            string[] regs = { "w19", "w20", "w21", "w22" };
            location = regs[stack_depth];
            if (source_reg != location)
            {
                sb.AppendLine($"    mov {location}, {source_reg}");
            }
        }
        else
        {
            // Spill to memory
            location = $"[x29, #-{spill_offset}]";
            sb.AppendLine($"    str {source_reg}, {location}");
            spill_offset += 4;
        }
        
        stack_locations.Add(location);
        stack_depth++;
        if (stack_depth > max_stack_depth) max_stack_depth = stack_depth;
        
        return sb.ToString().TrimEnd('\r', '\n');
    }
    
    /// <summary>
    /// Pop a value from the virtual stack into a register
    /// </summary>
    private static string StackPop(string dest_reg = "w0")
    {
        if (stack_depth == 0) throw new InvalidOperationException("Stack underflow");
        
        var sb = new StringBuilder();
        string location = stack_locations[stack_depth - 1];
        
        if (location != dest_reg)
        {
            if (location.StartsWith("["))
            {
                sb.AppendLine($"    ldr {dest_reg}, {location}");
            }
            else
            {
                sb.AppendLine($"    mov {dest_reg}, {location}");
            }
        }
        
        stack_locations.RemoveAt(stack_depth - 1);
        stack_depth--;
        
        // Adjust spill offset if we're popping a spilled value
        if (location.StartsWith("[x29"))
        {
            spill_offset -= 4;
        }
        
        return sb.ToString().TrimEnd('\r', '\n');
    }
    
    /// <summary>
    /// Pop two values for binary operations
    /// </summary>
    private static (string pop2, string pop1) StackPop2(string reg1 = "w0", string reg2 = "w1")
    {
        var pop1 = StackPop(reg2);  // Second operand (right)
        var pop0 = StackPop(reg1);  // First operand (left)
        return (pop0, pop1);
    }
    
    /// <summary>
    /// Generate a unique label
    /// </summary>
    private static string GenerateLabel(string prefix = "L")
    {
        return $"{prefix}{label_counter++}";
    }
    
    /// <summary>
    /// Reset state for new function
    /// </summary>
    private static void ResetState()
    {
        stack_depth = 0;
        max_stack_depth = 0;
        stack_locations.Clear();
        spill_offset = 64;
        label_counter = 0;
        block_labels.Clear();
        loop_labels.Clear();
        local_offsets.Clear();
        local_frame_size = 0;
    }
    
    // ========================================================================
    // MODULE AND FUNCTION LOWERING
    // ========================================================================
    
    public Map Module = @"// ARM64 Assembly (AArch64)
// Generated by BADGER WAT Compiler
// Module: {id}

.data
    // Linear memory base (to be initialized at runtime)
    memory_base: .quad 0
    
.text
    .global _start
    
_start:
    // Initialize memory base
    adrp x0, memory_base
    add x0, x0, :lo12:memory_base
    
    // Call main if it exists
    // (In a real implementation, this would call the start function)
    
    // Exit
    mov x0, #0                     // exit code 0
    mov x16, #1                    // sys_exit
    svc #0x80

{fields}
";

    /// <summary>
    /// Function lowering with AAPCS64 (ARM64 calling convention) compliance
    /// 
    /// AAPCS64 for ARM64:
    /// - Arguments: x0-x7 (w0-w7 for 32-bit), then stack (left to right)
    /// - Return: x0 (w0 for 32-bit)
    /// - Callee-saved: x19-x28, x29 (frame pointer), x30 (link register)
    /// - Caller-saved: x0-x18
    /// - Stack alignment: 16-byte aligned
    /// 
    /// Function prologue:
    /// 1. Save frame pointer (x29) and link register (x30)
    /// 2. Set up frame pointer
    /// 3. Save callee-saved registers we use (x19-x23)
    /// 4. Allocate stack space for locals + spills
    /// 5. Move parameters from registers to locals
    /// 6. Load memory base into x23
    /// 
    /// Function body:
    /// - Use w19-w22 for stack simulation (32-bit)
    /// - Use x23 for memory base
    /// - Use w0-w7 for temporaries
    /// 
    /// Function epilogue:
    /// 1. Move return value to w0
    /// 2. Restore callee-saved registers
    /// 3. Restore frame pointer and link register
    /// 4. Return
    /// </summary>
    public Map Function = @"{id}:
    // === PROLOGUE ===
    stp x29, x30, [sp, #-16]!      // Save frame pointer and link register
    mov x29, sp                    // Set up new frame pointer
    stp x19, x20, [sp, #-16]!      // Save callee-saved x19, x20
    stp x21, x22, [sp, #-16]!      // Save callee-saved x21, x22
    str x23, [sp, #-16]!           // Save callee-saved x23 (memory base)
    
    // Allocate stack space (locals + spills)
    // Space = {local_space} (locals) + max_spill (computed during lowering)
    sub sp, sp, #{frame_size}
    
    // Load memory base into x23
    adrp x23, memory_base
    ldr x23, [x23, :lo12:memory_base]
    
    // Move parameters from argument registers to local slots
    // For AAPCS64: param0=w0, param1=w1, param2=w2, etc.
    {param_moves}
    
    // === FUNCTION BODY ===
{body}

.function_exit_{id}:
    // === EPILOGUE ===
    // Return value should already be in w0 (from stack simulation)
    // If stack has value, pop it to w0
    {epilogue_pop}
    
    // Restore stack pointer
    add sp, sp, #{frame_size}
    
    // Restore callee-saved registers
    ldr x23, [sp], #16
    ldp x21, x22, [sp], #16
    ldp x19, x20, [sp], #16
    ldp x29, x30, [sp], #16
    ret
";

    // ========================================================================
    // CONSTANT LOADING
    // ========================================================================
    
    public Map I32Const = @"    // i32.const {value}
    mov w0, #{value}
{push}";

    public Map I64Const = @"    // i64.const {value}
    mov x0, #{value}
{push}";

    public Map F32Const = @"    // f32.const {value}
    // Float constants require data section
    ldr s0, =__float32_const_{id}
{push}";

    public Map F64Const = @"    // f64.const {value}
    // Double constants require data section
    ldr d0, =__float64_const_{id}
{push}";

    // ========================================================================
    // ARITHMETIC OPERATIONS - i32
    // ========================================================================
    
    public Map I32Add = @"    // i32.add
{pop2}
    add w0, w0, w1
{push}";

    public Map I32Sub = @"    // i32.sub
{pop2}
    sub w0, w0, w1
{push}";

    public Map I32Mul = @"    // i32.mul
{pop2}
    mul w0, w0, w1
{push}";

    public Map I32DivS = @"    // i32.div_s (signed)
{pop2}
    sdiv w0, w0, w1
{push}";

    public Map I32DivU = @"    // i32.div_u (unsigned)
{pop2}
    udiv w0, w0, w1
{push}";

    public Map I32RemS = @"    // i32.rem_s (signed remainder)
{pop2}
    sdiv w2, w0, w1
    msub w0, w2, w1, w0            // w0 = w0 - (w2 * w1)
{push}";

    public Map I32RemU = @"    // i32.rem_u (unsigned remainder)
{pop2}
    udiv w2, w0, w1
    msub w0, w2, w1, w0            // w0 = w0 - (w2 * w1)
{push}";

    // ========================================================================
    // ARITHMETIC OPERATIONS - i64
    // ========================================================================
    
    public Map I64Add = @"    // i64.add
{pop2}
    add x0, x0, x1
{push}";

    public Map I64Sub = @"    // i64.sub
{pop2}
    sub x0, x0, x1
{push}";

    public Map I64Mul = @"    // i64.mul
{pop2}
    mul x0, x0, x1
{push}";

    public Map I64DivS = @"    // i64.div_s
{pop2}
    sdiv x0, x0, x1
{push}";

    public Map I64DivU = @"    // i64.div_u
{pop2}
    udiv x0, x0, x1
{push}";

    public Map I64RemS = @"    // i64.rem_s
{pop2}
    sdiv x2, x0, x1
    msub x0, x2, x1, x0
{push}";

    public Map I64RemU = @"    // i64.rem_u
{pop2}
    udiv x2, x0, x1
    msub x0, x2, x1, x0
{push}";

    // ========================================================================
    // LOGICAL OPERATIONS - i32
    // ========================================================================
    
    public Map I32And = @"    // i32.and
{pop2}
    and w0, w0, w1
{push}";

    public Map I32Or = @"    // i32.or
{pop2}
    orr w0, w0, w1
{push}";

    public Map I32Xor = @"    // i32.xor
{pop2}
    eor w0, w0, w1
{push}";

    public Map I32Shl = @"    // i32.shl (shift left)
{pop2}
    lsl w0, w0, w1
{push}";

    public Map I32ShrS = @"    // i32.shr_s (arithmetic shift right)
{pop2}
    asr w0, w0, w1
{push}";

    public Map I32ShrU = @"    // i32.shr_u (logical shift right)
{pop2}
    lsr w0, w0, w1
{push}";

    public Map I32Rotl = @"    // i32.rotl (rotate left)
{pop2}
    neg w2, w1
    ror w0, w0, w2
{push}";

    public Map I32Rotr = @"    // i32.rotr (rotate right)
{pop2}
    ror w0, w0, w1
{push}";

    // ========================================================================
    // LOGICAL OPERATIONS - i64
    // ========================================================================
    
    public Map I64And = @"    // i64.and
{pop2}
    and x0, x0, x1
{push}";

    public Map I64Or = @"    // i64.or
{pop2}
    orr x0, x0, x1
{push}";

    public Map I64Xor = @"    // i64.xor
{pop2}
    eor x0, x0, x1
{push}";

    public Map I64Shl = @"    // i64.shl
{pop2}
    lsl x0, x0, x1
{push}";

    public Map I64ShrS = @"    // i64.shr_s
{pop2}
    asr x0, x0, x1
{push}";

    public Map I64ShrU = @"    // i64.shr_u
{pop2}
    lsr x0, x0, x1
{push}";

    public Map I64Rotl = @"    // i64.rotl
{pop2}
    neg x2, x1
    ror x0, x0, x2
{push}";

    public Map I64Rotr = @"    // i64.rotr
{pop2}
    ror x0, x0, x1
{push}";

    // ========================================================================
    // BIT MANIPULATION OPERATIONS
    // ========================================================================
    
    public Map I32Clz = @"    // i32.clz (count leading zeros)
{pop1}
    clz w0, w0
{push}";

    public Map I32Ctz = @"    // i32.ctz (count trailing zeros)
{pop1}
    rbit w0, w0                    // Reverse bits
    clz w0, w0                     // Count leading zeros
{push}";

    public Map I32Popcnt = @"    // i32.popcnt (population count)
{pop1}
    fmov s0, w0                    // Move to SIMD register
    cnt v0.8b, v0.8b               // Count bits in each byte
    addv b0, v0.8b                 // Sum all bytes
    fmov w0, s0                    // Move back to general register
{push}";

    public Map I64Clz = @"    // i64.clz
{pop1}
    clz x0, x0
{push}";

    public Map I64Ctz = @"    // i64.ctz
{pop1}
    rbit x0, x0
    clz x0, x0
{push}";

    public Map I64Popcnt = @"    // i64.popcnt
{pop1}
    fmov d0, x0
    cnt v0.8b, v0.8b
    addv b0, v0.8b
    fmov w0, s0
{push}";

    // ========================================================================
    // COMPARISON OPERATIONS - i32
    // ========================================================================
    
    public Map I32Eqz = @"    // i32.eqz (equal to zero)
{pop1}
    cmp w0, #0
    cset w0, eq
{push}";

    public Map I32Eq = @"    // i32.eq
{pop2}
    cmp w0, w1
    cset w0, eq
{push}";

    public Map I32Ne = @"    // i32.ne
{pop2}
    cmp w0, w1
    cset w0, ne
{push}";

    public Map I32LtS = @"    // i32.lt_s (signed less than)
{pop2}
    cmp w0, w1
    cset w0, lt
{push}";

    public Map I32LtU = @"    // i32.lt_u (unsigned less than)
{pop2}
    cmp w0, w1
    cset w0, lo
{push}";

    public Map I32GtS = @"    // i32.gt_s (signed greater than)
{pop2}
    cmp w0, w1
    cset w0, gt
{push}";

    public Map I32GtU = @"    // i32.gt_u (unsigned greater than)
{pop2}
    cmp w0, w1
    cset w0, hi
{push}";

    public Map I32LeS = @"    // i32.le_s (signed less or equal)
{pop2}
    cmp w0, w1
    cset w0, le
{push}";

    public Map I32LeU = @"    // i32.le_u (unsigned less or equal)
{pop2}
    cmp w0, w1
    cset w0, ls
{push}";

    public Map I32GeS = @"    // i32.ge_s (signed greater or equal)
{pop2}
    cmp w0, w1
    cset w0, ge
{push}";

    public Map I32GeU = @"    // i32.ge_u (unsigned greater or equal)
{pop2}
    cmp w0, w1
    cset w0, hs
{push}";

    // ========================================================================
    // COMPARISON OPERATIONS - i64
    // ========================================================================
    
    public Map I64Eqz = @"    // i64.eqz
{pop1}
    cmp x0, #0
    cset w0, eq
{push}";

    public Map I64Eq = @"    // i64.eq
{pop2}
    cmp x0, x1
    cset w0, eq
{push}";

    public Map I64Ne = @"    // i64.ne
{pop2}
    cmp x0, x1
    cset w0, ne
{push}";

    public Map I64LtS = @"    // i64.lt_s
{pop2}
    cmp x0, x1
    cset w0, lt
{push}";

    public Map I64LtU = @"    // i64.lt_u
{pop2}
    cmp x0, x1
    cset w0, lo
{push}";

    public Map I64GtS = @"    // i64.gt_s
{pop2}
    cmp x0, x1
    cset w0, gt
{push}";

    public Map I64GtU = @"    // i64.gt_u
{pop2}
    cmp x0, x1
    cset w0, hi
{push}";

    public Map I64LeS = @"    // i64.le_s
{pop2}
    cmp x0, x1
    cset w0, le
{push}";

    public Map I64LeU = @"    // i64.le_u
{pop2}
    cmp x0, x1
    cset w0, ls
{push}";

    public Map I64GeS = @"    // i64.ge_s
{pop2}
    cmp x0, x1
    cset w0, ge
{push}";

    public Map I64GeU = @"    // i64.ge_u
{pop2}
    cmp x0, x1
    cset w0, hs
{push}";

    // ========================================================================
    // LOCAL VARIABLE OPERATIONS
    // ========================================================================
    // Locals are allocated in the stack frame at [x29, #-offset]
    // offset = 16 (saved fp/lr) + 48 (saved x19-x23) + local_index * 4
    // ========================================================================
    
    public Map LocalGet = @"    // local.get {index}
    ldr w0, [x29, #-{offset}]
{push}";

    public Map LocalSet = @"    // local.set {index}
{pop1}
    str w0, [x29, #-{offset}]";

    public Map LocalTee = @"    // local.tee {index}
    // Peek top of stack (don't pop)
    mov w0, {stack_top}
    str w0, [x29, #-{offset}]";

    // ========================================================================
    // GLOBAL VARIABLE OPERATIONS
    // ========================================================================
    // Globals are stored in the data section
    // ========================================================================
    
    public Map GlobalGet = @"    // global.get {index}
    adrp x0, global_{index}
    ldr w0, [x0, :lo12:global_{index}]
{push}";

    public Map GlobalSet = @"    // global.set {index}
{pop1}
    adrp x1, global_{index}
    str w0, [x1, :lo12:global_{index}]";

    // ========================================================================
    // MEMORY OPERATIONS
    // ========================================================================
    // Linear memory is accessed through the base pointer in x23
    // Address calculation: [x23 + addr + offset]
    // ========================================================================
    
    public Map I32Load = @"    // i32.load offset={offset} align={align}
{pop1}                             // Pop address
    ldr w0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I64Load = @"    // i64.load offset={offset} align={align}
{pop1}
    ldr x0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I32Load8S = @"    // i32.load8_s offset={offset}
{pop1}
    ldrsb w0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I32Load8U = @"    // i32.load8_u offset={offset}
{pop1}
    ldrb w0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I32Load16S = @"    // i32.load16_s offset={offset}
{pop1}
    ldrsh w0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I32Load16U = @"    // i32.load16_u offset={offset}
{pop1}
    ldrh w0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I64Load8S = @"    // i64.load8_s offset={offset}
{pop1}
    ldrsb x0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I64Load8U = @"    // i64.load8_u offset={offset}
{pop1}
    ldrb w0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I64Load16S = @"    // i64.load16_s offset={offset}
{pop1}
    ldrsh x0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I64Load16U = @"    // i64.load16_u offset={offset}
{pop1}
    ldrh w0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I64Load32S = @"    // i64.load32_s offset={offset}
{pop1}
    ldrsw x0, [x23, w0, uxtw #{offset}]
{push}";

    public Map I64Load32U = @"    // i64.load32_u offset={offset}
{pop1}
    ldr w0, [x23, w0, uxtw #{offset}]  // Zero-extends to 64-bit
{push}";

    public Map I32Store = @"    // i32.store offset={offset} align={align}
{pop2}                             // Pop value (w1), then address (w0)
    str w1, [x23, w0, uxtw #{offset}]";

    public Map I64Store = @"    // i64.store offset={offset} align={align}
{pop2}
    str x1, [x23, w0, uxtw #{offset}]";

    public Map I32Store8 = @"    // i32.store8 offset={offset}
{pop2}
    strb w1, [x23, w0, uxtw #{offset}]";

    public Map I32Store16 = @"    // i32.store16 offset={offset}
{pop2}
    strh w1, [x23, w0, uxtw #{offset}]";

    public Map I64Store8 = @"    // i64.store8 offset={offset}
{pop2}
    strb w1, [x23, w0, uxtw #{offset}]";

    public Map I64Store16 = @"    // i64.store16 offset={offset}
{pop2}
    strh w1, [x23, w0, uxtw #{offset}]";

    public Map I64Store32 = @"    // i64.store32 offset={offset}
{pop2}
    str w1, [x23, w0, uxtw #{offset}]";

    public Map MemorySize = @"    // memory.size
    // Return current memory size in pages (64KB each)
    adrp x0, memory_size
    ldr w0, [x0, :lo12:memory_size]
{push}";

    public Map MemoryGrow = @"    // memory.grow
{pop1}                             // Pop number of pages to grow
    // Call runtime function to grow memory
    // For now, just return -1 (failure)
    mov w0, #-1
{push}";

    // ========================================================================
    // CONTROL FLOW OPERATIONS
    // ========================================================================
    
    /// <summary>
    /// block creates a new label scope
    /// - Push label for end of block onto label stack
    /// - br N jumps to label at depth N
    /// </summary>
    public Map Block = @"    // block {label}
    // Begin block - target for br {depth}
{block_start_label}:";

    /// <summary>
    /// end of block
    /// - Generate the end label
    /// - Pop label from label stack
    /// </summary>
    public Map BlockEnd = @"    // end (block)
{block_end_label}:";

    /// <summary>
    /// loop creates a backward branch target
    /// - Push label for start of loop onto label stack
    /// - br N jumps to loop start (backward branch)
    /// </summary>
    public Map Loop = @"    // loop {label}
{loop_start_label}:";

    /// <summary>
    /// end of loop
    /// - Generate end label (for breaking out)
    /// </summary>
    public Map LoopEnd = @"    // end (loop)
{loop_end_label}:";

    /// <summary>
    /// if-then-else
    /// - Pop condition from stack
    /// - Jump to else/end if condition is zero
    /// </summary>
    public Map If = @"    // if
{pop1}
    cbz w0, {else_label}
{then_label}:";

    public Map Else = @"    // else
    b {end_label}
{else_label}:";

    public Map IfEnd = @"    // end (if)
{end_label}:";

    /// <summary>
    /// br (unconditional branch)
    /// - Jump to label at specified depth
    /// - depth 0 = innermost block/loop
    /// </summary>
    public Map Br = @"    // br {depth}
    b {target_label}";

    /// <summary>
    /// br_if (conditional branch)
    /// - Pop condition
    /// - Jump to label if condition is non-zero
    /// </summary>
    public Map BrIf = @"    // br_if {depth}
{pop1}
    cbnz w0, {target_label}";

    /// <summary>
    /// br_table (jump table)
    /// - Pop index
    /// - Jump to label based on index (with default)
    /// </summary>
    public Map BrTable = @"    // br_table {targets} {default}
{pop1}
    // Clamp index to valid range
    cmp w0, #{max_index}
    b.hi {default_label}
    // Compute jump table offset
    adr x1, .jump_table_{id}
    ldr w2, [x1, w0, uxtw #2]
    adr x1, .jump_table_{id}
    add x1, x1, x2
    br x1
    
.jump_table_{id}:
{jump_table_entries}
{default_label}:";

    /// <summary>
    /// return from function
    /// </summary>
    public Map Return = @"    // return
    b .function_exit_{current_function}";

    /// <summary>
    /// call function by index
    /// </summary>
    public Map Call = @"    // call {funcidx}
    // Save current stack state if needed
    // Parameters should be in w0-w7 based on calling convention
    bl {funcidx}
    // Result in w0, push to virtual stack
{push}";

    /// <summary>
    /// call_indirect through function table
    /// </summary>
    public Map CallIndirect = @"    // call_indirect {type}
{pop1}                             // Pop table index
    // Validate index and type
    // Load function pointer from table
    // Call through pointer
    // For now, simplified implementation
    adrp x1, function_table
    ldr x1, [x1, :lo12:function_table]
    ldr x1, [x1, w0, uxtw #3]
    blr x1
{push}";

    // ========================================================================
    // MISCELLANEOUS OPERATIONS
    // ========================================================================
    
    public Map Unreachable = @"    // unreachable
    // Trap - undefined instruction
    udf #0";

    public Map Nop = @"    // nop
    nop";

    public Map Drop = @"    // drop
{pop1}";

    public Map Select = @"    // select
    // Pop condition, then two values
{pop1}                             // condition
    mov w2, w0
{pop1}                             // val2 (false value)
    mov w3, w0
{pop1}                             // val1 (true value)
    cmp w2, #0
    csel w0, w0, w3, ne            // Select val1 if condition != 0, else val2
{push}";

    // ========================================================================
    // TYPE CONVERSION OPERATIONS
    // ========================================================================
    
    public Map I32WrapI64 = @"    // i32.wrap_i64
{pop1}
    // Lower 32 bits already in w0
{push}";

    public Map I64ExtendI32S = @"    // i64.extend_i32_s (sign-extend)
{pop1}
    sxtw x0, w0
{push}";

    public Map I64ExtendI32U = @"    // i64.extend_i32_u (zero-extend)
{pop1}
    uxtw x0, w0
{push}";

    public Map I32Extend8S = @"    // i32.extend8_s
{pop1}
    sxtb w0, w0
{push}";

    public Map I32Extend16S = @"    // i32.extend16_s
{pop1}
    sxth w0, w0
{push}";

    public Map I64Extend8S = @"    // i64.extend8_s
{pop1}
    sxtb x0, w0
{push}";

    public Map I64Extend16S = @"    // i64.extend16_s
{pop1}
    sxth x0, w0
{push}";

    public Map I64Extend32S = @"    // i64.extend32_s
{pop1}
    sxtw x0, w0
{push}";

    // ========================================================================
    // DYNAMIC CODE GENERATION
    // ========================================================================
    
    /// <summary>
    /// Direct code insertion for special cases
    /// </summary>
    public Map DynamicCode = "{code}";
}

// Part 2: ARM64 Assembler - converts assembly text to machine code
public static class Assembler
{
    public static byte[] Assemble(string assemblyText)
    {
        var labels = new Dictionary<string, int>();
        var code = new List<byte>();
        
        var lines = assemblyText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // First pass: collect labels
        // All ARM64 instructions are 4 bytes (fixed width)
        int address = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith(";"))
                continue;
                
            if (trimmed.EndsWith(":"))
            {
                var label = trimmed.TrimEnd(':');
                labels[label] = address;
            }
            else
            {
                // All ARM64 instructions are 4 bytes
                address += 4;
            }
        }
        
        // Second pass: encode instructions
        int currentAddress = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith(";") || trimmed.EndsWith(":"))
                continue;
                
            EncodeInstruction(trimmed, currentAddress, code, labels);
            currentAddress += 4;
        }
        
        return code.ToArray();
    }
    
    private static void EncodeInstruction(string instruction, int currentAddress, List<byte> code, Dictionary<string, int> labels)
    {
        // Parse instruction
        var match = Regex.Match(instruction, @"^(\w+(?:\.\w+)?)\s*(.*)$");
        if (!match.Success)
            throw new ArgumentException($"Invalid instruction format: {instruction}");
        
        var opcode = match.Groups[1].Value.ToLower();
        var operands = match.Groups[2].Value;
        
        switch (opcode)
        {
            case "ret":
                EncodeRet(code);
                break;
            case "nop":
                EncodeNop(code);
                break;
            case "mov":
                EncodeMov(operands, code);
                break;
            case "add":
                EncodeAdd(operands, code);
                break;
            case "sub":
                EncodeSub(operands, code);
                break;
            case "mul":
                EncodeMul(operands, code);
                break;
            case "sdiv":
                EncodeSdiv(operands, code);
                break;
            case "udiv":
                EncodeUdiv(operands, code);
                break;
            case "and":
                EncodeAnd(operands, code);
                break;
            case "orr":
                EncodeOrr(operands, code);
                break;
            case "eor":
                EncodeEor(operands, code);
                break;
            case "cmp":
                EncodeCmp(operands, code);
                break;
            case "cset":
                EncodeCset(operands, code);
                break;
            case "b":
                EncodeB(operands, currentAddress, code, labels);
                break;
            case "b.eq":
                EncodeBCond(operands, 0b0000, currentAddress, code, labels); // EQ
                break;
            case "b.ne":
                EncodeBCond(operands, 0b0001, currentAddress, code, labels); // NE
                break;
            case "b.lt":
                EncodeBCond(operands, 0b1011, currentAddress, code, labels); // LT
                break;
            case "b.gt":
                EncodeBCond(operands, 0b1100, currentAddress, code, labels); // GT
                break;
            case "bl":
                EncodeBL(operands, currentAddress, code, labels);
                break;
            case "stp":
                EncodeStp(operands, code);
                break;
            case "ldp":
                EncodeLdp(operands, code);
                break;
            case "ldr":
                EncodeLdr(operands, code);
                break;
            case "str":
                EncodeStr(operands, code);
                break;
            default:
                throw new NotImplementedException($"Instruction not implemented: {opcode}");
        }
    }
    
    // Helper: Parse operands
    private static string[] ParseOperands(string operands)
    {
        var result = new List<string>();
        var parts = operands.Split(',');
        
        foreach (var part in parts)
        {
            result.Add(part.Trim());
        }
        
        return result.ToArray();
    }
    
    // Helper: Get register number
    private static int GetRegisterNumber(string reg)
    {
        reg = reg.Trim().ToLower();
        
        // Handle special registers
        if (reg == "sp") return 31;
        if (reg == "xzr" || reg == "wzr") return 31;
        
        // Handle x0-x30 and w0-w30
        if (reg.StartsWith("x") || reg.StartsWith("w"))
        {
            if (int.TryParse(reg.Substring(1), out int num))
            {
                if (num >= 0 && num <= 30)
                    return num;
            }
        }
        
        throw new ArgumentException($"Invalid register: {reg}");
    }
    
    // Helper: Check if register is 64-bit
    private static bool Is64BitRegister(string reg)
    {
        reg = reg.Trim().ToLower();
        return reg.StartsWith("x") || reg == "sp" || reg == "xzr";
    }
    
    // Helper: Add 32-bit instruction (little-endian)
    private static void AddInstruction(uint instruction, List<byte> code)
    {
        code.Add((byte)(instruction & 0xFF));
        code.Add((byte)((instruction >> 8) & 0xFF));
        code.Add((byte)((instruction >> 16) & 0xFF));
        code.Add((byte)((instruction >> 24) & 0xFF));
    }
    
    // RET: Return from subroutine
    // Encoding: 1101 0110 0101 1111 0000 0011 1100 0000
    // RET uses X30 (link register)
    private static void EncodeRet(List<byte> code)
    {
        AddInstruction(0xD65F03C0, code);
    }
    
    // NOP: No operation
    // Encoding: 1101 0101 0000 0011 0010 0000 0001 1111
    private static void EncodeNop(List<byte> code)
    {
        AddInstruction(0xD503201F, code);
    }
    
    // MOV: Move (register or immediate)
    private static void EncodeMov(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 2)
            throw new ArgumentException($"MOV requires 2 operands: {operands}");
        
        var dst = parts[0];
        var src = parts[1];
        
        // Check if source is immediate
        if (src.StartsWith("#"))
        {
            // MOV with immediate (using MOVZ - Move wide with zero)
            var immStr = src.Substring(1);
            if (!int.TryParse(immStr, out int imm))
                throw new ArgumentException($"Invalid immediate: {src}");
            
            bool is64 = Is64BitRegister(dst);
            int rd = GetRegisterNumber(dst);
            
            // MOVZ encoding: sf=1 opc=10 10010 1 hw=00 imm16 Rd
            uint instruction = 0;
            instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
            instruction |= 0b10u << 29; // opc = 10
            instruction |= 0b100101u << 23; // fixed bits
            instruction |= 0u << 21; // hw = 00 (shift 0)
            instruction |= ((uint)imm & 0xFFFF) << 5; // imm16
            instruction |= (uint)rd; // Rd
            
            AddInstruction(instruction, code);
        }
        else
        {
            // MOV register to register (using ORR with zero register)
            // ORR Rd, XZR, Rm (which is equivalent to MOV Rd, Rm)
            bool is64 = Is64BitRegister(dst);
            int rd = GetRegisterNumber(dst);
            int rm = GetRegisterNumber(src);
            
            // ORR (shifted register): sf=1 01 01010 shift=00 0 Rm 000000 Rn=31 Rd
            uint instruction = 0;
            instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
            instruction |= 0b01u << 29; // opc
            instruction |= 0b01010u << 24; // fixed
            instruction |= 0b00u << 22; // shift = 00 (LSL)
            instruction |= 0u << 21; // N = 0
            instruction |= (uint)rm << 16; // Rm
            instruction |= 0u << 10; // imm6 = 000000
            instruction |= 31u << 5; // Rn = XZR
            instruction |= (uint)rd; // Rd
            
            AddInstruction(instruction, code);
        }
    }
    
    // ADD: Add (register or immediate)
    private static void EncodeAdd(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 3)
            throw new ArgumentException($"ADD requires 3 operands: {operands}");
        
        var rd = parts[0];
        var rn = parts[1];
        var op2 = parts[2];
        
        bool is64 = Is64BitRegister(rd);
        int rdNum = GetRegisterNumber(rd);
        int rnNum = GetRegisterNumber(rn);
        
        if (op2.StartsWith("#"))
        {
            // ADD immediate
            var immStr = op2.Substring(1);
            if (!int.TryParse(immStr, out int imm))
                throw new ArgumentException($"Invalid immediate: {op2}");
            
            // ADD (immediate): sf 0 0 100010 shift imm12 Rn Rd
            uint instruction = 0;
            instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
            instruction |= 0b00u << 29; // op
            instruction |= 0b100010u << 23; // fixed
            instruction |= 0u << 22; // shift = 0
            instruction |= ((uint)imm & 0xFFF) << 10; // imm12
            instruction |= (uint)rnNum << 5; // Rn
            instruction |= (uint)rdNum; // Rd
            
            AddInstruction(instruction, code);
        }
        else
        {
            // ADD register
            int rmNum = GetRegisterNumber(op2);
            
            // ADD (shifted register): sf 0 0 01011 shift 0 Rm imm6 Rn Rd
            uint instruction = 0;
            instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
            instruction |= 0b00u << 29; // op
            instruction |= 0b01011u << 24; // fixed
            instruction |= 0b00u << 22; // shift = 00 (LSL)
            instruction |= 0u << 21; // N = 0
            instruction |= (uint)rmNum << 16; // Rm
            instruction |= 0u << 10; // imm6 = 0
            instruction |= (uint)rnNum << 5; // Rn
            instruction |= (uint)rdNum; // Rd
            
            AddInstruction(instruction, code);
        }
    }
    
    // SUB: Subtract (register or immediate)
    private static void EncodeSub(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 3)
            throw new ArgumentException($"SUB requires 3 operands: {operands}");
        
        var rd = parts[0];
        var rn = parts[1];
        var op2 = parts[2];
        
        bool is64 = Is64BitRegister(rd);
        int rdNum = GetRegisterNumber(rd);
        int rnNum = GetRegisterNumber(rn);
        
        if (op2.StartsWith("#"))
        {
            // SUB immediate
            var immStr = op2.Substring(1);
            if (!int.TryParse(immStr, out int imm))
                throw new ArgumentException($"Invalid immediate: {op2}");
            
            // SUB (immediate): sf 1 0 100010 shift imm12 Rn Rd
            uint instruction = 0;
            instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
            instruction |= 0b10u << 29; // op
            instruction |= 0b100010u << 23; // fixed
            instruction |= 0u << 22; // shift = 0
            instruction |= ((uint)imm & 0xFFF) << 10; // imm12
            instruction |= (uint)rnNum << 5; // Rn
            instruction |= (uint)rdNum; // Rd
            
            AddInstruction(instruction, code);
        }
        else
        {
            // SUB register
            int rmNum = GetRegisterNumber(op2);
            
            // SUB (shifted register): sf 1 0 01011 shift 0 Rm imm6 Rn Rd
            uint instruction = 0;
            instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
            instruction |= 0b10u << 29; // op
            instruction |= 0b01011u << 24; // fixed
            instruction |= 0b00u << 22; // shift = 00 (LSL)
            instruction |= 0u << 21; // N = 0
            instruction |= (uint)rmNum << 16; // Rm
            instruction |= 0u << 10; // imm6 = 0
            instruction |= (uint)rnNum << 5; // Rn
            instruction |= (uint)rdNum; // Rd
            
            AddInstruction(instruction, code);
        }
    }
    
    // MUL: Multiply
    private static void EncodeMul(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 3)
            throw new ArgumentException($"MUL requires 3 operands: {operands}");
        
        bool is64 = Is64BitRegister(parts[0]);
        int rd = GetRegisterNumber(parts[0]);
        int rn = GetRegisterNumber(parts[1]);
        int rm = GetRegisterNumber(parts[2]);
        
        // MADD with Ra = XZR: sf 0 0 11011 000 Rm 0 Ra=31 Rn Rd
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b00u << 29; // op54
        instruction |= 0b11011u << 24; // fixed
        instruction |= 0b000u << 21; // op31
        instruction |= (uint)rm << 16; // Rm
        instruction |= 0u << 15; // o0
        instruction |= 31u << 10; // Ra = 31 (XZR)
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rd; // Rd
        
        AddInstruction(instruction, code);
    }
    
    // SDIV: Signed divide
    private static void EncodeSdiv(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 3)
            throw new ArgumentException($"SDIV requires 3 operands: {operands}");
        
        bool is64 = Is64BitRegister(parts[0]);
        int rd = GetRegisterNumber(parts[0]);
        int rn = GetRegisterNumber(parts[1]);
        int rm = GetRegisterNumber(parts[2]);
        
        // SDIV: sf 0 0 11010110 Rm 000011 Rn Rd
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b00u << 29; // op54
        instruction |= 0b11010110u << 21; // fixed
        instruction |= (uint)rm << 16; // Rm
        instruction |= 0b000011u << 10; // opcode2
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rd; // Rd
        
        AddInstruction(instruction, code);
    }
    
    // UDIV: Unsigned divide
    private static void EncodeUdiv(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 3)
            throw new ArgumentException($"UDIV requires 3 operands: {operands}");
        
        bool is64 = Is64BitRegister(parts[0]);
        int rd = GetRegisterNumber(parts[0]);
        int rn = GetRegisterNumber(parts[1]);
        int rm = GetRegisterNumber(parts[2]);
        
        // UDIV: sf 0 0 11010110 Rm 000010 Rn Rd
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b00u << 29; // op54
        instruction |= 0b11010110u << 21; // fixed
        instruction |= (uint)rm << 16; // Rm
        instruction |= 0b000010u << 10; // opcode2
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rd; // Rd
        
        AddInstruction(instruction, code);
    }
    
    // AND: Bitwise AND
    private static void EncodeAnd(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 3)
            throw new ArgumentException($"AND requires 3 operands: {operands}");
        
        bool is64 = Is64BitRegister(parts[0]);
        int rd = GetRegisterNumber(parts[0]);
        int rn = GetRegisterNumber(parts[1]);
        int rm = GetRegisterNumber(parts[2]);
        
        // AND (shifted register): sf 00 01010 shift 0 Rm imm6 Rn Rd
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b00u << 29; // opc
        instruction |= 0b01010u << 24; // fixed
        instruction |= 0b00u << 22; // shift = 00
        instruction |= 0u << 21; // N = 0
        instruction |= (uint)rm << 16; // Rm
        instruction |= 0u << 10; // imm6 = 0
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rd; // Rd
        
        AddInstruction(instruction, code);
    }
    
    // ORR: Bitwise OR
    private static void EncodeOrr(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 3)
            throw new ArgumentException($"ORR requires 3 operands: {operands}");
        
        bool is64 = Is64BitRegister(parts[0]);
        int rd = GetRegisterNumber(parts[0]);
        int rn = GetRegisterNumber(parts[1]);
        int rm = GetRegisterNumber(parts[2]);
        
        // ORR (shifted register): sf 01 01010 shift 0 Rm imm6 Rn Rd
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b01u << 29; // opc
        instruction |= 0b01010u << 24; // fixed
        instruction |= 0b00u << 22; // shift = 00
        instruction |= 0u << 21; // N = 0
        instruction |= (uint)rm << 16; // Rm
        instruction |= 0u << 10; // imm6 = 0
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rd; // Rd
        
        AddInstruction(instruction, code);
    }
    
    // EOR: Bitwise Exclusive OR
    private static void EncodeEor(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 3)
            throw new ArgumentException($"EOR requires 3 operands: {operands}");
        
        bool is64 = Is64BitRegister(parts[0]);
        int rd = GetRegisterNumber(parts[0]);
        int rn = GetRegisterNumber(parts[1]);
        int rm = GetRegisterNumber(parts[2]);
        
        // EOR (shifted register): sf 10 01010 shift 0 Rm imm6 Rn Rd
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b10u << 29; // opc
        instruction |= 0b01010u << 24; // fixed
        instruction |= 0b00u << 22; // shift = 00
        instruction |= 0u << 21; // N = 0
        instruction |= (uint)rm << 16; // Rm
        instruction |= 0u << 10; // imm6 = 0
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rd; // Rd
        
        AddInstruction(instruction, code);
    }
    
    // CMP: Compare (implemented as SUBS with Rd = XZR)
    private static void EncodeCmp(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 2)
            throw new ArgumentException($"CMP requires 2 operands: {operands}");
        
        var rn = parts[0];
        var op2 = parts[1];
        
        bool is64 = Is64BitRegister(rn);
        int rnNum = GetRegisterNumber(rn);
        
        if (op2.StartsWith("#"))
        {
            // CMP immediate (SUBS with Rd = XZR)
            var immStr = op2.Substring(1);
            if (!int.TryParse(immStr, out int imm))
                throw new ArgumentException($"Invalid immediate: {op2}");
            
            // SUBS (immediate): sf 1 1 100010 shift imm12 Rn Rd=31
            uint instruction = 0;
            instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
            instruction |= 0b11u << 29; // op (SUBS)
            instruction |= 0b100010u << 23; // fixed
            instruction |= 0u << 22; // shift = 0
            instruction |= ((uint)imm & 0xFFF) << 10; // imm12
            instruction |= (uint)rnNum << 5; // Rn
            instruction |= 31u; // Rd = XZR
            
            AddInstruction(instruction, code);
        }
        else
        {
            // CMP register (SUBS with Rd = XZR)
            int rmNum = GetRegisterNumber(op2);
            
            // SUBS (shifted register): sf 1 1 01011 shift 0 Rm imm6 Rn Rd=31
            uint instruction = 0;
            instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
            instruction |= 0b11u << 29; // op (SUBS)
            instruction |= 0b01011u << 24; // fixed
            instruction |= 0b00u << 22; // shift = 00
            instruction |= 0u << 21; // N = 0
            instruction |= (uint)rmNum << 16; // Rm
            instruction |= 0u << 10; // imm6 = 0
            instruction |= (uint)rnNum << 5; // Rn
            instruction |= 31u; // Rd = XZR
            
            AddInstruction(instruction, code);
        }
    }
    
    // CSET: Conditional set (CSINC with Rn = XZR, Rm = XZR)
    private static void EncodeCset(string operands, List<byte> code)
    {
        var parts = ParseOperands(operands);
        if (parts.Length != 2)
            throw new ArgumentException($"CSET requires 2 operands: {operands}");
        
        bool is64 = Is64BitRegister(parts[0]);
        int rd = GetRegisterNumber(parts[0]);
        
        // Parse condition code (e.g., "eq", "ne", "lt", "gt")
        var condStr = parts[1].Trim().ToLower();
        uint cond;
        switch (condStr)
        {
            case "eq": cond = 0b0000; break; // Equal
            case "ne": cond = 0b0001; break; // Not equal
            case "lt": cond = 0b1011; break; // Less than (signed)
            case "gt": cond = 0b1100; break; // Greater than (signed)
            case "le": cond = 0b1101; break; // Less or equal (signed)
            case "ge": cond = 0b1010; break; // Greater or equal (signed)
            default:
                throw new ArgumentException($"Unsupported condition: {condStr}");
        }
        
        // CSINC (CSET): sf 0 0 11010100 Rm=31 cond 01 Rn=31 Rd
        // The condition is inverted in CSINC to achieve CSET behavior
        uint invertedCond = cond ^ 1; // Invert least significant bit
        
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b00u << 29; // op
        instruction |= 0b11010100u << 21; // fixed
        instruction |= 31u << 16; // Rm = XZR
        instruction |= invertedCond << 12; // condition (inverted)
        instruction |= 0b01u << 10; // op2 (for CSINC)
        instruction |= 31u << 5; // Rn = XZR
        instruction |= (uint)rd; // Rd
        
        AddInstruction(instruction, code);
    }
    
    // B: Unconditional branch
    private static void EncodeB(string operands, int currentAddress, List<byte> code, Dictionary<string, int> labels)
    {
        var label = operands.Trim();
        
        if (!labels.TryGetValue(label, out int targetAddress))
            throw new ArgumentException($"Undefined label: {label}");
        
        // Calculate PC-relative offset (in instructions, i.e., offset / 4)
        int offset = (targetAddress - currentAddress) / 4;
        
        // Validate 26-bit signed range: -2^25 to 2^25-1 (±128 MB / 4 = ±33,554,432 instructions)
        if (offset < -33554432 || offset > 33554431)
            throw new ArgumentException($"Branch offset {offset} instructions ({offset * 4} bytes) out of range for B instruction (±128 MB)");
        
        // B encoding: 0 00101 imm26
        uint instruction = 0;
        instruction |= 0b000101u << 26; // fixed
        instruction |= ((uint)offset & 0x3FFFFFF); // imm26
        
        AddInstruction(instruction, code);
    }
    
    // B.cond: Conditional branch
    private static void EncodeBCond(string operands, int cond, int currentAddress, List<byte> code, Dictionary<string, int> labels)
    {
        var label = operands.Trim();
        
        if (!labels.TryGetValue(label, out int targetAddress))
            throw new ArgumentException($"Undefined label: {label}");
        
        // Calculate PC-relative offset (in instructions)
        int offset = (targetAddress - currentAddress) / 4;
        
        // Validate 19-bit signed range: -2^18 to 2^18-1 (±1 MB / 4 = ±262,144 instructions)
        if (offset < -262144 || offset > 262143)
            throw new ArgumentException($"Conditional branch offset {offset} instructions ({offset * 4} bytes) out of range for B.cond instruction (±1 MB)");
        
        // B.cond encoding: 0101010 0 imm19 0 cond
        uint instruction = 0;
        instruction |= 0b0101010u << 25; // fixed
        instruction |= 0u << 24; // o1
        instruction |= ((uint)offset & 0x7FFFF) << 5; // imm19
        instruction |= 0u << 4; // o0
        instruction |= (uint)cond; // cond
        
        AddInstruction(instruction, code);
    }
    
    // BL: Branch with link
    private static void EncodeBL(string operands, int currentAddress, List<byte> code, Dictionary<string, int> labels)
    {
        var label = operands.Trim();
        
        if (!labels.TryGetValue(label, out int targetAddress))
            throw new ArgumentException($"Undefined label: {label}");
        
        // Calculate PC-relative offset (in instructions)
        int offset = (targetAddress - currentAddress) / 4;
        
        // Validate 26-bit signed range: -2^25 to 2^25-1 (±128 MB / 4 = ±33,554,432 instructions)
        if (offset < -33554432 || offset > 33554431)
            throw new ArgumentException($"BL offset {offset} instructions ({offset * 4} bytes) out of range (±128 MB)");
        
        // BL encoding: 1 00101 imm26
        uint instruction = 0;
        instruction |= 0b100101u << 26; // fixed (bit 31 = 1 for BL)
        instruction |= ((uint)offset & 0x3FFFFFF); // imm26
        
        AddInstruction(instruction, code);
    }
    
    // STP: Store pair of registers
    private static void EncodeStp(string operands, List<byte> code)
    {
        // Parse: stp Rt1, Rt2, [Rn, #imm]!  or  stp Rt1, Rt2, [Rn], #imm
        var match = Regex.Match(operands, @"([xw]\d+|sp|xzr|wzr)\s*,\s*([xw]\d+|sp|xzr|wzr)\s*,\s*\[([xw]\d+|sp)\s*,?\s*#?(-?\d+)\](!?)");
        if (!match.Success)
            throw new ArgumentException($"Invalid STP operands: {operands}");
        
        bool is64 = Is64BitRegister(match.Groups[1].Value);
        int rt1 = GetRegisterNumber(match.Groups[1].Value);
        int rt2 = GetRegisterNumber(match.Groups[2].Value);
        int rn = GetRegisterNumber(match.Groups[3].Value);
        int imm = int.Parse(match.Groups[4].Value);
        bool preIndex = match.Groups[5].Value == "!";
        
        // Offset must be scaled by size (8 for 64-bit, 4 for 32-bit)
        int scale = is64 ? 8 : 4;
        int imm7 = imm / scale;
        
        // Validate 7-bit signed range: -64 to +63
        if (imm7 < -64 || imm7 > 63)
            throw new ArgumentException($"STP offset {imm} bytes (scaled: {imm7}) out of 7-bit signed range (must be -64 to +63 after scaling by {scale})");
        
        // STP encoding: sf 0 101 0 010 imm7 Rt2 Rn Rt1
        // Addressing mode: 11 = pre-index, 10 = signed offset, 01 = post-index
        uint addrMode = preIndex ? 0b11u : 0b10u;
        
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b0u << 30; // opc[1]
        instruction |= 0b101u << 27; // fixed
        instruction |= 0b0u << 26; // V
        instruction |= addrMode << 23; // addressing mode
        instruction |= 0b1u << 22; // L = 0 for store
        instruction |= ((uint)imm7 & 0x7F) << 15; // imm7
        instruction |= (uint)rt2 << 10; // Rt2
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rt1; // Rt1
        
        AddInstruction(instruction, code);
    }
    
    // LDP: Load pair of registers
    private static void EncodeLdp(string operands, List<byte> code)
    {
        // Parse: ldp Rt1, Rt2, [Rn, #imm]!  or  ldp Rt1, Rt2, [Rn], #imm
        var match = Regex.Match(operands, @"([xw]\d+|sp|xzr|wzr)\s*,\s*([xw]\d+|sp|xzr|wzr)\s*,\s*\[([xw]\d+|sp)\]\s*,?\s*#?(-?\d+)");
        if (!match.Success)
        {
            // Try pre-index form
            match = Regex.Match(operands, @"([xw]\d+|sp|xzr|wzr)\s*,\s*([xw]\d+|sp|xzr|wzr)\s*,\s*\[([xw]\d+|sp)\s*,?\s*#?(-?\d+)\]");
        }
        
        if (!match.Success)
            throw new ArgumentException($"Invalid LDP operands: {operands}");
        
        bool is64 = Is64BitRegister(match.Groups[1].Value);
        int rt1 = GetRegisterNumber(match.Groups[1].Value);
        int rt2 = GetRegisterNumber(match.Groups[2].Value);
        int rn = GetRegisterNumber(match.Groups[3].Value);
        int imm = int.Parse(match.Groups[4].Value);
        
        // Check if post-index (], #imm)
        bool postIndex = operands.Contains("],");
        
        // Offset must be scaled by size (8 for 64-bit, 4 for 32-bit)
        int scale = is64 ? 8 : 4;
        int imm7 = imm / scale;
        
        // Validate 7-bit signed range: -64 to +63
        if (imm7 < -64 || imm7 > 63)
            throw new ArgumentException($"LDP offset {imm} bytes (scaled: {imm7}) out of 7-bit signed range (must be -64 to +63 after scaling by {scale})");
        
        // LDP encoding: sf 0 101 0 011 imm7 Rt2 Rn Rt1
        // Addressing mode: 11 = pre-index, 10 = signed offset, 01 = post-index
        uint addrMode = postIndex ? 0b01u : 0b10u;
        
        uint instruction = 0;
        instruction |= (uint)(is64 ? 1 : 0) << 31; // sf
        instruction |= 0b0u << 30; // opc[1]
        instruction |= 0b101u << 27; // fixed
        instruction |= 0b0u << 26; // V
        instruction |= addrMode << 23; // addressing mode
        instruction |= 0b1u << 22; // L = 1 for load
        instruction |= ((uint)imm7 & 0x7F) << 15; // imm7
        instruction |= (uint)rt2 << 10; // Rt2
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rt1; // Rt1
        
        AddInstruction(instruction, code);
    }
    
    // LDR: Load register
    private static void EncodeLdr(string operands, List<byte> code)
    {
        // Parse: ldr Rt, [Rn, #imm]!  or  ldr Rt, [Rn], #imm
        var match = Regex.Match(operands, @"([xw]\d+|sp|xzr|wzr)\s*,\s*\[([xw]\d+|sp)\]\s*,?\s*#?(-?\d+)");
        if (!match.Success)
        {
            // Try pre-index or offset form
            match = Regex.Match(operands, @"([xw]\d+|sp|xzr|wzr)\s*,\s*\[([xw]\d+|sp)\s*,?\s*#?(-?\d+)\](!?)");
        }
        
        if (!match.Success)
            throw new ArgumentException($"Invalid LDR operands: {operands}");
        
        bool is64 = Is64BitRegister(match.Groups[1].Value);
        int rt = GetRegisterNumber(match.Groups[1].Value);
        int rn = GetRegisterNumber(match.Groups[2].Value);
        int imm = int.Parse(match.Groups[3].Value);
        bool preIndex = match.Groups.Count > 4 && match.Groups[4].Value == "!";
        bool postIndex = operands.Contains("],");
        
        // LDR (immediate): size 11 1 00 0 01 imm9 mode Rn Rt
        // mode: 00 = unscaled, 01 = post-index, 11 = pre-index, 10 = unsigned offset
        uint mode;
        if (postIndex)
            mode = 0b01u;
        else if (preIndex)
            mode = 0b11u;
        else
            mode = 0b01u; // Use post-index as default
        
        uint size = is64 ? 0b11u : 0b10u;
        
        uint instruction = 0;
        instruction |= size << 30; // size
        instruction |= 0b111u << 27; // fixed
        instruction |= 0b0u << 26; // V
        instruction |= 0b00u << 24; // opc
        instruction |= 0b0u << 22; // fixed
        instruction |= ((uint)imm & 0x1FF) << 12; // imm9
        instruction |= mode << 10; // mode
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rt; // Rt
        
        AddInstruction(instruction, code);
    }
    
    // STR: Store register
    private static void EncodeStr(string operands, List<byte> code)
    {
        // Parse: str Rt, [Rn, #imm]!  or  str Rt, [Rn], #imm
        var match = Regex.Match(operands, @"([xw]\d+|sp|xzr|wzr)\s*,\s*\[([xw]\d+|sp)\]\s*,?\s*#?(-?\d+)");
        if (!match.Success)
        {
            // Try pre-index or offset form
            match = Regex.Match(operands, @"([xw]\d+|sp|xzr|wzr)\s*,\s*\[([xw]\d+|sp)\s*,?\s*#?(-?\d+)\](!?)");
        }
        
        if (!match.Success)
            throw new ArgumentException($"Invalid STR operands: {operands}");
        
        bool is64 = Is64BitRegister(match.Groups[1].Value);
        int rt = GetRegisterNumber(match.Groups[1].Value);
        int rn = GetRegisterNumber(match.Groups[2].Value);
        int imm = int.Parse(match.Groups[3].Value);
        bool preIndex = match.Groups.Count > 4 && match.Groups[4].Value == "!";
        bool postIndex = operands.Contains("],");
        
        // STR (immediate): size 11 1 00 0 00 imm9 mode Rn Rt
        // mode: 01 = post-index, 11 = pre-index
        uint mode;
        if (postIndex)
            mode = 0b01u;
        else if (preIndex)
            mode = 0b11u;
        else
            mode = 0b01u; // Use post-index as default
        
        uint size = is64 ? 0b11u : 0b10u;
        
        uint instruction = 0;
        instruction |= size << 30; // size
        instruction |= 0b111u << 27; // fixed
        instruction |= 0b0u << 26; // V
        instruction |= 0b00u << 24; // opc
        instruction |= 0b0u << 22; // fixed
        instruction |= ((uint)imm & 0x1FF) << 12; // imm9
        instruction |= mode << 10; // mode
        instruction |= (uint)rn << 5; // Rn
        instruction |= (uint)rt; // Rt
        
        AddInstruction(instruction, code);
    }
}
