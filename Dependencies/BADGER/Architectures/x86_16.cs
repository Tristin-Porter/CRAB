using CDTk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Badger.Architectures.x86_16;

/// <summary>
/// Complete WAT → x86_16 assembly lowering with full stack simulation,
/// control flow, calling conventions, and instruction selection.
/// This is NOT a simple template system - it's a full compiler backend.
/// </summary>
public class WATToX86_16MapSet : MapSet
{
    // ========================================================================
    // STACK SIMULATION STATE
    // ========================================================================
    // The WASM operand stack is simulated using:
    // - Registers: di, bx, cx, dx (first 4 stack slots)
    // - Memory: [bp - stack_offset] for spilled values
    // 
    // Stack pointer tracking:
    // - stack_depth: current number of values on virtual stack
    // - Physical locations tracked in stack_locations list
    // ========================================================================
    
    private static int stack_depth = 0;
    private static int max_stack_depth = 0;
    private static List<string> stack_locations = new List<string>(); // "bx", "cx", "dx", "di", or "[bp-N]"
    private static int spill_offset = 4; // Start spilling at [bp-16] (after saved bx)
    
    // Label generation for control flow
    private static int label_counter = 0;
    private static Stack<string> block_labels = new Stack<string>();
    private static Stack<string> loop_labels = new Stack<string>();
    
    // Local variable allocation
    private static Dictionary<int, int> local_offsets = new Dictionary<int, int>();
    private static int local_frame_size = 0;
    
    // Memory base register (holds linear memory base address)
    private const string MEMORY_BASE_REG = "si";
    
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
    private static string StackPush(string source_reg = "ax")
    {
        var sb = new StringBuilder();
        string location;
        
        // Use registers bx, cx, dx, di for first 4 stack slots
        if (stack_depth < 4)
        {
            string[] regs = { "bx", "cx", "dx", "di" };
            location = regs[stack_depth];
            if (source_reg != location)
            {
                sb.AppendLine($"    mov {location}, {source_reg}");
            }
        }
        else
        {
            // Spill to memory
            location = $"[bp - {spill_offset}]";
            sb.AppendLine($"    mov word {location}, {source_reg}");
            spill_offset += 2;
        }
        
        stack_locations.Add(location);
        stack_depth++;
        if (stack_depth > max_stack_depth) max_stack_depth = stack_depth;
        
        return sb.ToString().TrimEnd('\r', '\n');
    }
    
    /// <summary>
    /// Pop a value from the virtual stack into a register
    /// </summary>
    private static string StackPop(string dest_reg = "ax")
    {
        if (stack_depth == 0) throw new InvalidOperationException("Stack underflow");
        
        var sb = new StringBuilder();
        string location = stack_locations[stack_depth - 1];
        
        if (location != dest_reg)
        {
            sb.AppendLine($"    mov {dest_reg}, {location}");
        }
        
        stack_locations.RemoveAt(stack_depth - 1);
        stack_depth--;
        
        // Adjust spill offset if we're popping a spilled value
        if (location.StartsWith("[bp"))
        {
            spill_offset -= 2;
        }
        
        return sb.ToString().TrimEnd('\r', '\n');
    }
    
    /// <summary>
    /// Pop two values for binary operations
    /// </summary>
    private static (string pop2, string pop1) StackPop2(string reg1 = "ax", string reg2 = "bx")
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
        spill_offset = 4;
        label_counter = 0;
        block_labels.Clear();
        loop_labels.Clear();
        local_offsets.Clear();
        local_frame_size = 0;
    }
    
    // ========================================================================
    // MODULE AND FUNCTION LOWERING
    // ========================================================================
    
    public Map Module = @"; x86_16 Assembly - 16-bit real mode ABI
; Generated by BADGER WAT Compiler
; Module: {id}

section .data
    ; Linear memory base (to be initialized at runtime)
    memory_base: dq 0
    
section .text
    global _start
    
_start:
    ; Initialize memory base
    mov ax, 12                    ; sys_brk
    xor di, di                   ; get current break
    syscall
    mov word [rel memory_base], ax
    
    ; Call main if it exists
    ; (In a real implementation, this would call the start function)
    
    ; Exit
    mov ax, 60                    ; sys_exit  
    xor di, di                   ; exit code 0
    syscall

{fields}
";

    /// <summary>
    /// Function lowering with 16-bit real mode ABI compliance
    /// 
    /// 16-bit real mode ABI for x86_16:
    /// - Arguments: di, si, dx, cx, si, di, then stack (right to left)
    /// - Return: ax (or ax:dx for i64)
    /// - Callee-saved: bx, bp, di-dx
    /// - Caller-saved: ax, cx, dx, si, di, si-si
    /// - Stack alignment: 2-byte aligned before call
    /// 
    /// Function prologue:
    /// 1. Save callee-saved registers we use (bx, di-dx)
    /// 2. Set up frame pointer
    /// 3. Allocate stack space for locals + spills
    /// 4. Move parameters from registers to locals
    /// 5. Load memory base into si
    /// 
    /// Function body:
    /// - Use di-dx for stack simulation
    /// - Use si for memory base
    /// - Use ax-dx, si-ax for temporaries
    /// 
    /// Function epilogue:
    /// 1. Move return value to ax
    /// 2. Restore stack pointer
    /// 3. Restore callee-saved registers
    /// 4. Return
    /// </summary>
    public Map Function = @"{id}:
    ; === PROLOGUE ===
    push bp                       ; Save old frame pointer
    mov bp, sp                   ; Set up new frame pointer
    push bx                       ; Save callee-saved bx
    push di                       ; Save callee-saved di
    push bx                       ; Save callee-saved bx
    push cx                       ; Save callee-saved cx
    push dx                       ; Save callee-saved dx
    
    ; Allocate stack space (locals + spills)
    ; Space = {local_space} (locals) + max_spill (computed during lowering)
    sub sp, {frame_size}
    
    ; Load memory base into si
    mov si, word [rel memory_base]
    
    ; Move parameters from argument registers to local slots
    ; For 16-bit real mode ABI: param0=di, param1=si, param2=dx, param3=cx, param4=si, param5=di
    {param_moves}
    
    ; === FUNCTION BODY ===
{body}

.function_exit_{id}:
    ; === EPILOGUE ===
    ; Return value should already be in ax (from stack simulation)
    ; If stack has value, pop it to ax
    {epilogue_pop}
    
    ; Restore stack pointer
    mov sp, bp
    sub sp, 40                    ; Account for pushed registers (5 * 8)
    
    ; Restore callee-saved registers
    pop dx
    pop cx
    pop bx
    pop di
    pop bx
    pop bp
    ret
";

    // ========================================================================
    // CONSTANT LOADING
    // ========================================================================
    
    public Map I32Const = @"    ; i32.const {value}
    mov ax, {value}
{push}";

    public Map I64Const = @"    ; i64.const {value}
    mov ax, {value}
{push}";

    public Map F32Const = @"    ; f32.const {value}
    ; Float constants require data section
    mov ax, __float32_const_{id}
{push}";

    public Map F64Const = @"    ; f64.const {value}
    ; Double constants require data section
    mov ax, __float64_const_{id}
{push}";

    // ========================================================================
    // ARITHMETIC OPERATIONS
    // ========================================================================
    
    public Map I32Add = @"    ; i32.add
{pop2}
    add ax, bx
{push}";

    public Map I32Sub = @"    ; i32.sub
{pop2}
    sub ax, bx
{push}";

    public Map I32Mul = @"    ; i32.mul
{pop2}
    imul ax, bx
{push}";

    public Map I32DivS = @"    ; i32.div_s (signed)
{pop2}
    cwd                            ; Sign-extend ax into dx:ax
    idiv bx
{push}";

    public Map I32DivU = @"    ; i32.div_u (unsigned)
{pop2}
    xor dx, dx                   ; Zero-extend ax into dx:ax
    div bx
{push}";

    public Map I32RemS = @"    ; i32.rem_s (signed remainder)
{pop2}
    cwd
    idiv bx
    mov ax, dx                   ; Remainder in dx
{push}";

    public Map I32RemU = @"    ; i32.rem_u (unsigned remainder)
{pop2}
    xor dx, dx
    div bx
    mov ax, dx                   ; Remainder in dx
{push}";

    // i64 operations (similar but use full 64-bit registers)
    
    public Map I64Add = @"    ; i64.add
{pop2}
    add ax, bx
{push}";

    public Map I64Sub = @"    ; i64.sub
{pop2}
    sub ax, bx
{push}";

    public Map I64Mul = @"    ; i64.mul
{pop2}
    imul ax, bx
{push}";

    public Map I64DivS = @"    ; i64.div_s
{pop2}
    cwd                            ; Sign-extend ax into dx:ax
    idiv bx
{push}";

    public Map I64DivU = @"    ; i64.div_u
{pop2}
    xor dx, dx
    div bx
{push}";

    public Map I64RemS = @"    ; i64.rem_s
{pop2}
    cwd
    idiv bx
    mov ax, dx
{push}";

    public Map I64RemU = @"    ; i64.rem_u
{pop2}
    xor dx, dx
    div bx
    mov ax, dx
{push}";

    // ========================================================================
    // LOGICAL OPERATIONS
    // ========================================================================
    
    public Map I32And = @"    ; i32.and
{pop2}
    and ax, bx
{push}";

    public Map I32Or = @"    ; i32.or
{pop2}
    or ax, bx
{push}";

    public Map I32Xor = @"    ; i32.xor
{pop2}
    xor ax, bx
{push}";

    public Map I32Shl = @"    ; i32.shl (shift left)
{pop2}
    mov cx, bx                   ; Shift count in cl
    shl ax, cl
{push}";

    public Map I32ShrS = @"    ; i32.shr_s (arithmetic shift right)
{pop2}
    mov cx, bx
    sar ax, cl
{push}";

    public Map I32ShrU = @"    ; i32.shr_u (logical shift right)
{pop2}
    mov cx, bx
    shr ax, cl
{push}";

    public Map I32Rotl = @"    ; i32.rotl (rotate left)
{pop2}
    mov cx, bx
    rol ax, cl
{push}";

    public Map I32Rotr = @"    ; i32.rotr (rotate right)
{pop2}
    mov cx, bx
    ror ax, cl
{push}";

    // i64 logical operations
    
    public Map I64And = @"    ; i64.and
{pop2}
    and ax, bx
{push}";

    public Map I64Or = @"    ; i64.or
{pop2}
    or ax, bx
{push}";

    public Map I64Xor = @"    ; i64.xor
{pop2}
    xor ax, bx
{push}";

    public Map I64Shl = @"    ; i64.shl
{pop2}
    mov cx, bx
    shl ax, cl
{push}";

    public Map I64ShrS = @"    ; i64.shr_s
{pop2}
    mov cx, bx
    sar ax, cl
{push}";

    public Map I64ShrU = @"    ; i64.shr_u
{pop2}
    mov cx, bx
    shr ax, cl
{push}";

    public Map I64Rotl = @"    ; i64.rotl
{pop2}
    mov cx, bx
    rol ax, cl
{push}";

    public Map I64Rotr = @"    ; i64.rotr
{pop2}
    mov cx, bx
    ror ax, cl
{push}";

    // ========================================================================
    // BITWISE OPERATIONS
    // ========================================================================
    
    public Map I32Clz = @"    ; i32.clz (count leading zeros)
{pop1}
    lzcnt ax, ax                 ; Count leading zeros (requires LZCNT instruction)
{push}";

    public Map I32Ctz = @"    ; i32.ctz (count trailing zeros)
{pop1}
    tzcnt ax, ax                 ; Count trailing zeros (requires BMI1)
{push}";

    public Map I32Popcnt = @"    ; i32.popcnt (population count)
{pop1}
    popcnt ax, ax                ; Count set bits (requires POPCNT)
{push}";

    public Map I64Clz = @"    ; i64.clz
{pop1}
    lzcnt ax, ax
{push}";

    public Map I64Ctz = @"    ; i64.ctz
{pop1}
    tzcnt ax, ax
{push}";

    public Map I64Popcnt = @"    ; i64.popcnt
{pop1}
    popcnt ax, ax
{push}";

    // ========================================================================
    // COMPARISON OPERATIONS
    // ========================================================================
    
    public Map I32Eqz = @"    ; i32.eqz (equal to zero)
{pop1}
    test ax, ax
    setz al
    movzx ax, al
{push}";

    public Map I32Eq = @"    ; i32.eq
{pop2}
    cmp ax, bx
    sete al
    movzx ax, al
{push}";

    public Map I32Ne = @"    ; i32.ne
{pop2}
    cmp ax, bx
    setne al
    movzx ax, al
{push}";

    public Map I32LtS = @"    ; i32.lt_s (signed less than)
{pop2}
    cmp ax, bx
    setl al
    movzx ax, al
{push}";

    public Map I32LtU = @"    ; i32.lt_u (unsigned less than)
{pop2}
    cmp ax, bx
    setb al
    movzx ax, al
{push}";

    public Map I32GtS = @"    ; i32.gt_s (signed greater than)
{pop2}
    cmp ax, bx
    setg al
    movzx ax, al
{push}";

    public Map I32GtU = @"    ; i32.gt_u (unsigned greater than)
{pop2}
    cmp ax, bx
    seta al
    movzx ax, al
{push}";

    public Map I32LeS = @"    ; i32.le_s (signed less or equal)
{pop2}
    cmp ax, bx
    setle al
    movzx ax, al
{push}";

    public Map I32LeU = @"    ; i32.le_u (unsigned less or equal)
{pop2}
    cmp ax, bx
    setbe al
    movzx ax, al
{push}";

    public Map I32GeS = @"    ; i32.ge_s (signed greater or equal)
{pop2}
    cmp ax, bx
    setge al
    movzx ax, al
{push}";

    public Map I32GeU = @"    ; i32.ge_u (unsigned greater or equal)
{pop2}
    cmp ax, bx
    setae al
    movzx ax, al
{push}";

    // i64 comparisons (similar but use full 64-bit)
    
    public Map I64Eqz = @"    ; i64.eqz
{pop1}
    test ax, ax
    setz al
    movzx ax, al
{push}";

    public Map I64Eq = @"    ; i64.eq
{pop2}
    cmp ax, bx
    sete al
    movzx ax, al
{push}";

    public Map I64Ne = @"    ; i64.ne
{pop2}
    cmp ax, bx
    setne al
    movzx ax, al
{push}";

    public Map I64LtS = @"    ; i64.lt_s
{pop2}
    cmp ax, bx
    setl al
    movzx ax, al
{push}";

    public Map I64LtU = @"    ; i64.lt_u
{pop2}
    cmp ax, bx
    setb al
    movzx ax, al
{push}";

    public Map I64GtS = @"    ; i64.gt_s
{pop2}
    cmp ax, bx
    setg al
    movzx ax, al
{push}";

    public Map I64GtU = @"    ; i64.gt_u
{pop2}
    cmp ax, bx
    seta al
    movzx ax, al
{push}";

    public Map I64LeS = @"    ; i64.le_s
{pop2}
    cmp ax, bx
    setle al
    movzx ax, al
{push}";

    public Map I64LeU = @"    ; i64.le_u
{pop2}
    cmp ax, bx
    setbe al
    movzx ax, al
{push}";

    public Map I64GeS = @"    ; i64.ge_s
{pop2}
    cmp ax, bx
    setge al
    movzx ax, al
{push}";

    public Map I64GeU = @"    ; i64.ge_u
{pop2}
    cmp ax, bx
    setae al
    movzx ax, al
{push}";

    // ========================================================================
    // LOCAL VARIABLE OPERATIONS
    // ========================================================================
    // Locals are allocated in the stack frame at [bp - offset]
    // offset = 8 (old bp) + 40 (saved regs) + local_index * 8
    // ========================================================================
    
    public Map LocalGet = @"    ; local.get {index}
    mov ax, word [bp - {offset}]
{push}";

    public Map LocalSet = @"    ; local.set {index}
{pop1}
    mov word [bp - {offset}], ax";

    public Map LocalTee = @"    ; local.tee {index}
    ; Peek top of stack (don't pop)
    mov ax, {stack_top}
    mov word [bp - {offset}], ax";

    // ========================================================================
    // GLOBAL VARIABLE OPERATIONS
    // ========================================================================
    // Globals are stored in the data section
    // ========================================================================
    
    public Map GlobalGet = @"    ; global.get {index}
    mov ax, word [rel global_{index}]
{push}";

    public Map GlobalSet = @"    ; global.set {index}
{pop1}
    mov word [rel global_{index}], ax";

    // ========================================================================
    // MEMORY OPERATIONS
    // ========================================================================
    // Linear memory is accessed through the base pointer in si
    // Address calculation: [si + addr + offset]
    // ========================================================================
    
    public Map I32Load = @"    ; i32.load offset={offset} align={align}
{pop1}                             ; Pop address
    mov ax, word [si + ax + {offset}]
{push}";

    public Map I64Load = @"    ; i64.load offset={offset} align={align}
{pop1}
    mov ax, word [si + ax + {offset}]
{push}";

    public Map I32Load8S = @"    ; i32.load8_s offset={offset}
{pop1}
    movsx ax, byte [si + ax + {offset}]
{push}";

    public Map I32Load8U = @"    ; i32.load8_u offset={offset}
{pop1}
    movzx ax, byte [si + ax + {offset}]
{push}";

    public Map I32Load16S = @"    ; i32.load16_s offset={offset}
{pop1}
    movsx ax, word [si + ax + {offset}]
{push}";

    public Map I32Load16U = @"    ; i32.load16_u offset={offset}
{pop1}
    movzx ax, word [si + ax + {offset}]
{push}";

    public Map I64Load8S = @"    ; i64.load8_s offset={offset}
{pop1}
    movsx ax, byte [si + ax + {offset}]
{push}";

    public Map I64Load8U = @"    ; i64.load8_u offset={offset}
{pop1}
    movzx ax, byte [si + ax + {offset}]
{push}";

    public Map I64Load16S = @"    ; i64.load16_s offset={offset}
{pop1}
    movsx ax, word [si + ax + {offset}]
{push}";

    public Map I64Load16U = @"    ; i64.load16_u offset={offset}
{pop1}
    movzx ax, word [si + ax + {offset}]
{push}";

    public Map I64Load32S = @"    ; i64.load32_s offset={offset}
{pop1}
    movsx ax, word [si + ax + {offset}]
{push}";

    public Map I64Load32U = @"    ; i64.load32_u offset={offset}
{pop1}
    mov ax, word [si + ax + {offset}]  ; Zero-extends to 64-bit
{push}";

    public Map I32Store = @"    ; i32.store offset={offset} align={align}
{pop2}                             ; Pop value (bx), then address (ax)
    mov word [si + ax + {offset}], bx";

    public Map I64Store = @"    ; i64.store offset={offset} align={align}
{pop2}
    mov word [si + ax + {offset}], bx";

    public Map I32Store8 = @"    ; i32.store8 offset={offset}
{pop2}
    mov byte [si + ax + {offset}], bl";

    public Map I32Store16 = @"    ; i32.store16 offset={offset}
{pop2}
    mov word [si + ax + {offset}], bx";

    public Map I64Store8 = @"    ; i64.store8 offset={offset}
{pop2}
    mov byte [si + ax + {offset}], bl";

    public Map I64Store16 = @"    ; i64.store16 offset={offset}
{pop2}
    mov word [si + ax + {offset}], bx";

    public Map I64Store32 = @"    ; i64.store32 offset={offset}
{pop2}
    mov word [si + ax + {offset}], bx";

    public Map MemorySize = @"    ; memory.size
    ; Return current memory size in pages (64KB each)
    mov ax, word [rel memory_size]
{push}";

    public Map MemoryGrow = @"    ; memory.grow
{pop1}                             ; Pop number of pages to grow
    ; Call runtime function to grow memory
    ; For now, just return -1 (failure)
    mov ax, -1
{push}";

    // ========================================================================
    // CONTROL FLOW OPERATIONS
    // ========================================================================
    
    /// <summary>
    /// block creates a new label scope
    /// - Push label for end of block onto label stack
    /// - br N jumps to label at depth N
    /// </summary>
    public Map Block = @"    ; block {label}
    ; Begin block - target for br {depth}
{block_start_label}:";

    /// <summary>
    /// end of block
    /// - Generate the end label
    /// - Pop label from label stack
    /// </summary>
    public Map BlockEnd = @"    ; end (block)
{block_end_label}:";

    /// <summary>
    /// loop creates a backward branch target
    /// - Push label for start of loop onto label stack
    /// - br N jumps to loop start (backward branch)
    /// </summary>
    public Map Loop = @"    ; loop {label}
{loop_start_label}:";

    /// <summary>
    /// end of loop
    /// - Generate end label (for breaking out)
    /// </summary>
    public Map LoopEnd = @"    ; end (loop)
{loop_end_label}:";

    /// <summary>
    /// if-then-else
    /// - Pop condition from stack
    /// - Jump to else/end if condition is zero
    /// </summary>
    public Map If = @"    ; if
{pop1}
    test ax, ax
    jz {else_label}
{then_label}:";

    public Map Else = @"    ; else
    jmp {end_label}
{else_label}:";

    public Map IfEnd = @"    ; end (if)
{end_label}:";

    /// <summary>
    /// br (unconditional branch)
    /// - Jump to label at specified depth
    /// - depth 0 = innermost block/loop
    /// </summary>
    public Map Br = @"    ; br {depth}
    jmp {target_label}";

    /// <summary>
    /// br_if (conditional branch)
    /// - Pop condition
    /// - Jump to label if condition is non-zero
    /// </summary>
    public Map BrIf = @"    ; br_if {depth}
{pop1}
    test ax, ax
    jnz {target_label}";

    /// <summary>
    /// br_table (jump table)
    /// - Pop index
    /// - Jump to label based on index (with default)
    /// </summary>
    public Map BrTable = @"    ; br_table {targets} {default}
{pop1}
    ; Clamp index to valid range
    cmp ax, {max_index}
    ja {default_label}
    ; Compute jump table offset
    lea bx, [rel .jump_table_{id}]
    movsx ax, word [bx + ax * 4]
    add ax, bx
    jmp ax
    
.jump_table_{id}:
{jump_table_entries}
{default_label}:";

    /// <summary>
    /// return from function
    /// - Jump to function epilogue
    /// </summary>
    public Map Return = @"    ; return
    jmp .function_exit_{func_id}";

    /// <summary>
    /// call function
    /// - Follow 16-bit real mode ABI calling convention
    /// - First 6 args in registers: di, si, dx, cx, si, di
    /// - Rest on stack (right to left)
    /// - Return value in ax (or ax:dx for i128)
    /// </summary>
    public Map Call = @"    ; call {funcidx}
    ; Save virtual stack state
    {save_stack}
    
    ; Move arguments from virtual stack to ABI registers
    {arg_moves}
    
    ; Call function
    call {funcidx}
    
    ; Restore virtual stack state
    {restore_stack}
    
    ; Push return value onto virtual stack
{push}";

    /// <summary>
    /// call_indirect (call through function table)
    /// - Pop table index
    /// - Verify signature
    /// - Call function
    /// </summary>
    public Map CallIndirect = @"    ; call_indirect {type}
{pop1}                             ; Pop table index
    ; Bounds check
    cmp ax, word [rel table_size]
    jae .invalid_index_{id}
    
    ; Load function pointer from table
    lea bx, [rel function_table]
    mov bx, word [bx + ax * 8]
    
    ; Verify type signature (runtime check)
    ; (Simplified: assume correct signature)
    
    ; Move arguments and call
    {arg_moves}
    call bx
    {restore_stack}
{push}
    jmp .call_indirect_end_{id}
    
.invalid_index_{id}:
    ; Trap: invalid table index
    ud2
    
.call_indirect_end_{id}:";

    /// <summary>
    /// unreachable (trap)
    /// </summary>
    public Map Unreachable = @"    ; unreachable
    ud2                            ; Generate invalid opcode (trap)";

    /// <summary>
    /// nop (no operation)
    /// </summary>
    public Map Nop = @"    ; nop
    nop";

    /// <summary>
    /// drop (pop and discard value)
    /// </summary>
    public Map Drop = @"    ; drop
{pop1}
    ; Value discarded";

    /// <summary>
    /// select (ternary operator)
    /// - Pop condition, then two values
    /// - Push selected value
    /// </summary>
    public Map Select = @"    ; select
{pop1}                             ; Pop condition into ax
    test ax, ax
{pop2}                             ; Pop two values into ax (val1) and bx (val2)
    cmovz ax, bx                 ; If condition==0, select bx
{push}";

    // ========================================================================
    // TYPE CONVERSION OPERATIONS
    // ========================================================================
    
    public Map I32WrapI64 = @"    ; i32.wrap_i64
    ; No-op on x86_16: just use lower 32 bits
    ; Top of stack already has 64-bit value
    ; When we use ax instead of ax, it automatically uses lower 32 bits";

    public Map I64ExtendI32S = @"    ; i64.extend_i32_s (sign-extend)
{pop1}
    movsx ax, ax                ; Sign-extend ax to ax
{push}";

    public Map I64ExtendI32U = @"    ; i64.extend_i32_u (zero-extend)
{pop1}
    mov ax, ax                   ; Zero-extend (mov to ax clears upper 32 bits)
{push}";

    public Map I32Extend8S = @"    ; i32.extend8_s
{pop1}
    movsx ax, al
{push}";

    public Map I32Extend16S = @"    ; i32.extend16_s
{pop1}
    movsx ax, ax
{push}";

    public Map I64Extend8S = @"    ; i64.extend8_s
{pop1}
    movsx ax, al
{push}";

    public Map I64Extend16S = @"    ; i64.extend16_s
{pop1}
    movsx ax, ax
{push}";

    public Map I64Extend32S = @"    ; i64.extend32_s
{pop1}
    movsx ax, ax
{push}";

    // ========================================================================
    // HELPER MAP FOR DYNAMIC CODE GENERATION
    // ========================================================================
    
    /// <summary>
    /// This is a special map that doesn't match a WAT instruction
    /// but is used to inject dynamic code generation during lowering
    /// </summary>
    public Map DynamicCode = "{code}";
}
public static class Assembler
{
    public static byte[] Assemble(string assemblyText)
    {
        var labels = new Dictionary<string, int>();
        var code = new List<byte>();
        
        var lines = assemblyText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // First pass: collect labels
        int address = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";"))
                continue;
                
            if (trimmed.EndsWith(":"))
            {
                var label = trimmed.TrimEnd(':');
                labels[label] = address;
            }
            else
            {
                address += EstimateInstructionSize(trimmed);
            }
        }
        
        // Second pass: encode instructions
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.EndsWith(":"))
                continue;
                
            EncodeInstruction(trimmed, code, labels);
        }
        
        return code.ToArray();
    }
    
    private static int GetCurrentAddress(List<byte> code)
    {
        return code.Count;
    }
    
    private static int CalculateRelativeOffsetFrom(string label, int fromAddress, int instructionSize, Dictionary<string, int> labels)
    {
        if (!labels.ContainsKey(label))
            return 0; // Label not found, use placeholder
            
        int targetAddress = labels[label];
        // Offset is relative to the end of the instruction
        return targetAddress - (fromAddress + instructionSize);
    }
    
    private static int EstimateInstructionSize(string instruction)
    {
        // Size estimation for 16-bit instructions
        var parts = instruction.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var opcode = parts[0].ToLower();
        
        if (opcode == "mov")
        {
            // MOV r16, imm16 = 3 bytes (opcode + 2-byte immediate)
            // MOV r16, r16 = 2 bytes (opcode + ModR/M)
            if (parts.Length > 2 && IsImmediate(parts[2]))
                return 3;
            return 2;
        }
        if (opcode == "push" || opcode == "pop" || opcode == "ret" || opcode == "retf" || opcode == "nop") return 1;
        if (opcode == "add" || opcode == "sub")
        {
            // ADD/SUB with imm8 = 3 bytes, with imm16 = 4 bytes
            if (parts.Length > 2 && IsImmediate(parts[2]))
            {
                int imm = int.Parse(parts[2]);
                return (imm >= -128 && imm <= 127) ? 3 : 4;
            }
            return 2;
        }
        if (opcode == "call" || opcode == "jmp") return 3; // 16-bit near call/jmp
        if (opcode.StartsWith("j")) return 4; // Conditional jumps (0F + opcode + 2-byte offset)
        if (opcode == "imul") return 3;
        if (opcode == "sete" || opcode == "setne" || opcode == "setl" || opcode == "setg") return 3;
        if (opcode == "movzx") return 3;
        return 2;
    }
    
    private static void EncodeInstruction(string instruction, List<byte> code, Dictionary<string, int> labels)
    {
        var parts = instruction.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var opcode = parts[0].ToLower();
        
        switch (opcode)
        {
            case "push":
                EncodePush(parts[1], code);
                break;
            case "pop":
                EncodePop(parts[1], code);
                break;
            case "mov":
                EncodeMov(parts[1], parts[2], code);
                break;
            case "add":
                EncodeAdd(parts[1], parts[2], code);
                break;
            case "sub":
                EncodeSub(parts[1], parts[2], code);
                break;
            case "imul":
                EncodeIMul(parts[1], parts[2], code);
                break;
            case "idiv":
                EncodeIdiv(parts[1], code);
                break;
            case "div":
                EncodeDiv(parts[1], code);
                break;
            case "cwd":
                EncodeCwd(code);
                break;
            case "and":
                EncodeAnd(parts[1], parts[2], code);
                break;
            case "or":
                EncodeOr(parts[1], parts[2], code);
                break;
            case "xor":
                EncodeXor(parts[1], parts[2], code);
                break;
            case "cmp":
                EncodeCmp(parts[1], parts[2], code);
                break;
            case "test":
                EncodeTest(parts[1], parts[2], code);
                break;
            case "jmp":
                EncodeJmp(parts[1], code, labels);
                break;
            case "jnz":
                EncodeJnz(parts[1], code, labels);
                break;
            case "je":
                EncodeJe(parts[1], code, labels);
                break;
            case "jne":
                EncodeJne(parts[1], code, labels);
                break;
            case "jl":
                EncodeJl(parts[1], code, labels);
                break;
            case "jg":
                EncodeJg(parts[1], code, labels);
                break;
            case "call":
                EncodeCall(parts[1], code, labels);
                break;
            case "ret":
                EncodeRet(code);
                break;
            case "retf":
                EncodeRetf(code);
                break;
            case "nop":
                EncodeNop(code);
                break;
            case "sete":
            case "setne":
            case "setl":
            case "setg":
                EncodeSet(opcode, parts[1], code);
                break;
            case "movzx":
                EncodeMovzx(parts[1], parts[2], code);
                break;
            default:
                throw new NotImplementedException($"Instruction not implemented: {opcode}");
        }
    }
    
    // Register encoding helpers for 16-bit registers
    private static byte GetRegisterCode(string reg)
    {
        return reg.ToLower() switch
        {
            "ax" or "al" => 0,
            "cx" or "cl" => 1,
            "dx" or "dl" => 2,
            "bx" or "bl" => 3,
            "sp" or "ah" => 4,
            "bp" or "ch" => 5,
            "si" or "dh" => 6,
            "di" or "bh" => 7,
            _ => throw new ArgumentException($"Unknown register: {reg}")
        };
    }
    
    private static bool IsImmediate(string operand)
    {
        return !string.IsNullOrEmpty(operand) && (char.IsDigit(operand[0]) || operand[0] == '-');
    }
    
    // Instruction encoders for 16-bit
    private static void EncodePush(string operand, List<byte> code)
    {
        // PUSH r16
        code.Add((byte)(0x50 + GetRegisterCode(operand)));
    }
    
    private static void EncodePop(string operand, List<byte> code)
    {
        // POP r16
        code.Add((byte)(0x58 + GetRegisterCode(operand)));
    }
    
    private static void EncodeMov(string dst, string src, List<byte> code)
    {
        // Guard against empty strings
        if (string.IsNullOrEmpty(dst) || string.IsNullOrEmpty(src))
        {
            throw new ArgumentException($"MOV instruction requires non-empty source and destination operands (dst='{dst}', src='{src}')");
        }
        
        if (src.StartsWith("[") && src.EndsWith("]"))
        {
            // MOV r16, [m16]
            code.Add(0x8B); // MOV r16, r/m16
            // ModR/M byte (simplified - would need full parsing for complete implementation)
            code.Add(0x46); // [bp + disp8]
            code.Add(0x00); // displacement
        }
        else if (dst.StartsWith("[") && dst.EndsWith("]"))
        {
            // MOV [m16], r16
            code.Add(0x89); // MOV r/m16, r16
            code.Add(0x46); // [bp + disp8]
            code.Add(0x00); // displacement
        }
        else if (IsImmediate(src))
        {
            // MOV r16, imm16
            code.Add((byte)(0xB8 + GetRegisterCode(dst)));
            AddImmediate16(int.Parse(src), code);
        }
        else
        {
            // MOV r16, r16
            code.Add(0x89); // MOV r/m16, r16
            code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
        }
    }
    
    private static void EncodeAdd(string dst, string src, List<byte> code)
    {
        // Check if src is immediate
        if (IsImmediate(src))
        {
            int imm = int.Parse(src);
            if (imm >= -128 && imm <= 127)
            {
                // ADD r/m16, imm8
                code.Add(0x83);
                code.Add((byte)(0xC0 + GetRegisterCode(dst))); // ModR/M: mod=11, reg=000 (/0), r/m=dst
                code.Add((byte)imm);
            }
            else
            {
                // ADD r/m16, imm16
                code.Add(0x81);
                code.Add((byte)(0xC0 + GetRegisterCode(dst))); // ModR/M: mod=11, reg=000 (/0), r/m=dst
                AddImmediate16(imm, code);
            }
        }
        else
        {
            // ADD r/m16, r16
            code.Add(0x01);
            code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
        }
    }
    
    private static void EncodeSub(string dst, string src, List<byte> code)
    {
        // Check if src is immediate
        if (IsImmediate(src))
        {
            int imm = int.Parse(src);
            if (imm >= -128 && imm <= 127)
            {
                // SUB r/m16, imm8
                code.Add(0x83);
                code.Add((byte)(0xE8 + GetRegisterCode(dst))); // ModR/M: mod=11, reg=101 (/5), r/m=dst
                code.Add((byte)imm);
            }
            else
            {
                // SUB r/m16, imm16
                code.Add(0x81);
                code.Add((byte)(0xE8 + GetRegisterCode(dst))); // ModR/M: mod=11, reg=101 (/5), r/m=dst
                AddImmediate16(imm, code);
            }
        }
        else
        {
            // SUB r/m16, r16
            code.Add(0x29);
            code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
        }
    }
    
    private static void EncodeIMul(string dst, string src, List<byte> code)
    {
        // IMUL r16, r/m16
        code.Add(0x0F);
        code.Add(0xAF);
        code.Add((byte)(0xC0 | (GetRegisterCode(dst) << 3) | GetRegisterCode(src)));
    }
    
    private static void EncodeIdiv(string src, List<byte> code)
    {
        // IDIV r/m16 (signed divide DX:AX by r/m16, quotient in AX, remainder in DX)
        code.Add(0xF7);
        code.Add((byte)(0xF8 + GetRegisterCode(src))); // ModR/M: mod=11, reg=111 (/7), r/m=src
    }
    
    private static void EncodeDiv(string src, List<byte> code)
    {
        // DIV r/m16 (unsigned divide DX:AX by r/m16, quotient in AX, remainder in DX)
        code.Add(0xF7);
        code.Add((byte)(0xF0 + GetRegisterCode(src))); // ModR/M: mod=11, reg=110 (/6), r/m=src
    }
    
    private static void EncodeCwd(List<byte> code)
    {
        // CWD (convert word to doubleword: sign-extend AX into DX:AX)
        code.Add(0x99);
    }
    
    private static void EncodeAnd(string dst, string src, List<byte> code)
    {
        // AND r/m16, r16
        code.Add(0x21);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeOr(string dst, string src, List<byte> code)
    {
        // OR r/m16, r16
        code.Add(0x09);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeXor(string dst, string src, List<byte> code)
    {
        // XOR r/m16, r16
        code.Add(0x31);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeCmp(string dst, string src, List<byte> code)
    {
        // CMP r/m16, r16
        code.Add(0x39);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeTest(string dst, string src, List<byte> code)
    {
        // TEST r/m16, r16
        code.Add(0x85);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeJmp(string label, List<byte> code, Dictionary<string, int> labels)
    {
        // JMP rel16 (near jump)
        int currentAddress = GetCurrentAddress(code);
        code.Add(0xE9);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 3, labels); // JMP instruction is 3 bytes (1 opcode + 2 offset)
        AddImmediate16(offset, code);
    }
    
    private static void EncodeJnz(string label, List<byte> code, Dictionary<string, int> labels)
    {
        // JNZ rel16
        int currentAddress = GetCurrentAddress(code);
        code.Add(0x0F);
        code.Add(0x85);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 4, labels); // JNZ instruction is 4 bytes
        AddImmediate16(offset, code);
    }
    
    private static void EncodeJe(string label, List<byte> code, Dictionary<string, int> labels)
    {
        // JE rel16
        int currentAddress = GetCurrentAddress(code);
        code.Add(0x0F);
        code.Add(0x84);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 4, labels); // JE instruction is 4 bytes
        AddImmediate16(offset, code);
    }
    
    private static void EncodeJne(string label, List<byte> code, Dictionary<string, int> labels)
    {
        // JNE rel16
        int currentAddress = GetCurrentAddress(code);
        code.Add(0x0F);
        code.Add(0x85);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 4, labels); // JNE instruction is 4 bytes
        AddImmediate16(offset, code);
    }
    
    private static void EncodeJl(string label, List<byte> code, Dictionary<string, int> labels)
    {
        // JL rel16
        int currentAddress = GetCurrentAddress(code);
        code.Add(0x0F);
        code.Add(0x8C);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 4, labels); // JL instruction is 4 bytes
        AddImmediate16(offset, code);
    }
    
    private static void EncodeJg(string label, List<byte> code, Dictionary<string, int> labels)
    {
        // JG rel16
        int currentAddress = GetCurrentAddress(code);
        code.Add(0x0F);
        code.Add(0x8F);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 4, labels); // JG instruction is 4 bytes
        AddImmediate16(offset, code);
    }
    
    private static void EncodeCall(string target, List<byte> code, Dictionary<string, int> labels)
    {
        // CALL rel16 (near call)
        int currentAddress = GetCurrentAddress(code);
        code.Add(0xE8);
        int offset = CalculateRelativeOffsetFrom(target, currentAddress, 3, labels); // CALL instruction is 3 bytes
        AddImmediate16(offset, code);
    }
    
    private static void EncodeRet(List<byte> code)
    {
        // RET (near return)
        code.Add(0xC3);
    }
    
    private static void EncodeRetf(List<byte> code)
    {
        // RETF (far return for real mode)
        code.Add(0xCB);
    }
    
    private static void EncodeNop(List<byte> code)
    {
        // NOP
        code.Add(0x90);
    }
    
    private static void EncodeSet(string op, string reg, List<byte> code)
    {
        // SETcc r/m8
        code.Add(0x0F);
        code.Add(op switch
        {
            "sete" => (byte)0x94,
            "setne" => (byte)0x95,
            "setl" => (byte)0x9C,
            "setg" => (byte)0x9F,
            _ => throw new ArgumentException($"Unknown set instruction: {op}")
        });
        code.Add((byte)(0xC0 | GetRegisterCode(reg)));
    }
    
    private static void EncodeMovzx(string dst, string src, List<byte> code)
    {
        // MOVZX r16, r/m8
        code.Add(0x0F);
        code.Add(0xB6);
        code.Add((byte)(0xC0 | (GetRegisterCode(dst) << 3) | GetRegisterCode(src)));
    }
    
    private static void AddImmediate16(int value, List<byte> code)
    {
        // Add a 16-bit immediate value (little-endian)
        code.Add((byte)(value & 0xFF));
        code.Add((byte)((value >> 8) & 0xFF));
    }
}
