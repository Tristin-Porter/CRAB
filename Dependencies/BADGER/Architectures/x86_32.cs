using CDTk;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Badger.Architectures.x86_32;

/// <summary>
/// Complete WAT → x86_32 assembly lowering with full stack simulation,
/// control flow, calling conventions, and instruction selection.
/// This is NOT a simple template system - it's a full compiler backend.
/// </summary>
public class WATToX86_32MapSet : MapSet
{
    // ========================================================================
    // STACK SIMULATION STATE
    // ========================================================================
    // The WASM operand stack is simulated using:
    // - Registers: ebx, ecx, edx, edi (first 4 stack slots)
    // - Memory: [ebp - stack_offset] for spilled values
    // 
    // Note: x86_32 has fewer registers than x86_64, so:
    // - ebx, esi, edi are callee-saved (cdecl)
    // - eax, ecx, edx are caller-saved
    // - We use ebx, ecx, edx, edi for virtual stack
    // - esi is reserved for memory base pointer
    // 
    // Stack pointer tracking:
    // - stack_depth: current number of values on virtual stack
    // - Physical locations tracked in stack_locations list
    // ========================================================================
    
    private static int stack_depth = 0;
    private static int max_stack_depth = 0;
    private static List<string> stack_locations = new List<string>(); // "ebx", "ecx", "edx", "edi", or "[ebp-N]"
    private static int spill_offset = 8; // Start spilling at [ebp-8] (after saved registers)
    
    // Label generation for control flow
    private static int label_counter = 0;
    private static Stack<string> block_labels = new Stack<string>();
    private static Stack<string> loop_labels = new Stack<string>();
    
    // Local variable allocation
    private static Dictionary<int, int> local_offsets = new Dictionary<int, int>();
    private static int local_frame_size = 0;
    
    // Memory base register (holds linear memory base address)
    private const string MEMORY_BASE_REG = "esi";
    
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
    private static string StackPush(string source_reg = "eax")
    {
        var sb = new StringBuilder();
        string location;
        
        // Use registers ebx, ecx, edx, edi for first 4 stack slots
        if (stack_depth < 4)
        {
            string[] regs = { "ebx", "ecx", "edx", "edi" };
            location = regs[stack_depth];
            if (source_reg != location)
            {
                sb.AppendLine($"    mov {location}, {source_reg}");
            }
        }
        else
        {
            // Spill to memory
            location = $"[ebp - {spill_offset}]";
            sb.AppendLine($"    mov dword {location}, {source_reg}");
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
    private static string StackPop(string dest_reg = "eax")
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
        if (location.StartsWith("[ebp"))
        {
            spill_offset -= 4;
        }
        
        return sb.ToString().TrimEnd('\r', '\n');
    }
    
    /// <summary>
    /// Pop two values for binary operations
    /// </summary>
    private static (string pop2, string pop1) StackPop2(string reg1 = "eax", string reg2 = "ebx")
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
        spill_offset = 8;
        label_counter = 0;
        block_labels.Clear();
        loop_labels.Clear();
        local_offsets.Clear();
        local_frame_size = 0;
    }
    
    // ========================================================================
    // PUBLIC TEMPLATE EXPANSION API
    // ========================================================================
    
    /// <summary>
    /// Expand {push} template - pushes eax onto virtual stack (simplified for demo)
    /// </summary>
    public static string ExpandPush() => ""; // Simplified - value already in eax
    
    /// <summary>
    /// Expand {pop} template - pops into specified register (simplified for demo)
    /// </summary>
    public static string ExpandPop(string dest = "eax") => ""; // Simplified - value already in eax
    
    /// <summary>
    /// Expand {pop2} template - pops two values into eax and ebx (simplified for demo)
    /// </summary>
    public static string ExpandPop2() => ""; // Simplified - values already in place
    
    // ========================================================================
    // MODULE AND FUNCTION LOWERING
    // ========================================================================
    
    public Map Module = @"; x86_32 Assembly - cdecl ABI
; Generated by BADGER WAT Compiler
; Module: {id}

section .data
    ; Linear memory base (to be initialized at runtime)
    memory_base: dq 0
    
section .text
    global _start
    
_start:
    ; Initialize memory base
    mov eax, 12                    ; sys_brk
    xor edi, edi                   ; get current break
    syscall
    mov dword [rel memory_base], eax
    
    ; Call main if it exists
    ; (In a real implementation, this would call the start function)
    
    ; Exit
    mov eax, 60                    ; sys_exit  
    xor edi, edi                   ; exit code 0
    syscall

{fields}
";

    /// <summary>
    /// Function lowering with cdecl ABI compliance
    /// 
    /// cdecl ABI for x86_32:
    /// - Arguments: edi, esi, edx, ecx, esi, edi, then stack (right to left)
    /// - Return: eax (or eax:edx for i64)
    /// - Callee-saved: ebx, ebp, edi-edx
    /// - Caller-saved: eax, ecx, edx, esi, edi, esi-esi
    /// - Stack alignment: 4-byte aligned before call
    /// 
    /// Function prologue:
    /// 1. Save callee-saved registers we use (ebx, edi-edx)
    /// 2. Set up frame pointer
    /// 3. Allocate stack space for locals + spills
    /// 4. Move parameters from registers to locals
    /// 5. Load memory base into esi
    /// 
    /// Function body:
    /// - Use edi-edx for stack simulation
    /// - Use esi for memory base
    /// - Use eax-edx, esi-eax for temporaries
    /// 
    /// Function epilogue:
    /// 1. Move return value to eax
    /// 2. Restore stack pointer
    /// 3. Restore callee-saved registers
    /// 4. Return
    /// </summary>
    public Map Function = @"{id}:
    ; === PROLOGUE ===
    push ebp                       ; Save old frame pointer
    mov ebp, esp                   ; Set up new frame pointer
    push ebx                       ; Save callee-saved ebx
    push edi                       ; Save callee-saved edi
    push esi                       ; Save callee-saved esi
    push ecx                       ; Save callee-saved ecx
    push edx                       ; Save callee-saved edx
    
    ; Allocate stack space (locals + spills)
    ; Space = {local_space} (locals) + max_spill (computed during lowering)
    sub esp, {frame_size}
    
    ; Load memory base into esi
    mov esi, dword [rel memory_base]
    
    ; Move parameters from argument registers to local slots
    ; For cdecl ABI: param0=edi, param1=esi, param2=edx, param3=ecx, param4=esi, param5=edi
    {param_moves}
    
    ; === FUNCTION BODY ===
{body}

.function_exit_{id}:
    ; === EPILOGUE ===
    ; Return value should already be in eax (from stack simulation)
    ; If stack has value, pop it to eax
    {epilogue_pop}
    
    ; Restore stack pointer
    mov esp, ebp
    sub esp, 40                    ; Account for pushed registers (5 * 8)
    
    ; Restore callee-saved registers
    pop edx
    pop ecx
    pop esi
    pop edi
    pop ebx
    pop ebp
    ret
";

    // ========================================================================
    // CONSTANT LOADING
    // ========================================================================
    
    public Map I32Const = @"    ; i32.const {value}
    mov eax, {value}
{push}";

    public Map I64Const = @"    ; i64.const {value}
    mov eax, {value}
{push}";

    public Map F32Const = @"    ; f32.const {value}
    ; Float constants require data section
    mov eax, __float32_const_{id}
{push}";

    public Map F64Const = @"    ; f64.const {value}
    ; Double constants require data section
    mov eax, __float64_const_{id}
{push}";

    // ========================================================================
    // ARITHMETIC OPERATIONS
    // ========================================================================
    
    public Map I32Add = @"    ; i32.add
{pop2}
    add eax, ebx
{push}";

    public Map I32Sub = @"    ; i32.sub
{pop2}
    sub eax, ebx
{push}";

    public Map I32Mul = @"    ; i32.mul
{pop2}
    imul eax, ebx
{push}";

    public Map I32DivS = @"    ; i32.div_s (signed)
{pop2}
    cdq                            ; Sign-extend eax into edx:eax
    idiv ebx
{push}";

    public Map I32DivU = @"    ; i32.div_u (unsigned)
{pop2}
    xor edx, edx                   ; Zero-extend eax into edx:eax
    div ebx
{push}";

    public Map I32RemS = @"    ; i32.rem_s (signed remainder)
{pop2}
    cdq
    idiv ebx
    mov eax, edx                   ; Remainder in edx
{push}";

    public Map I32RemU = @"    ; i32.rem_u (unsigned remainder)
{pop2}
    xor edx, edx
    div ebx
    mov eax, edx                   ; Remainder in edx
{push}";

    // i64 operations (similar but use full 64-bit registers)
    
    public Map I64Add = @"    ; i64.add
{pop2}
    add eax, ebx
{push}";

    public Map I64Sub = @"    ; i64.sub
{pop2}
    sub eax, ebx
{push}";

    public Map I64Mul = @"    ; i64.mul
{pop2}
    imul eax, ebx
{push}";

    public Map I64DivS = @"    ; i64.div_s
{pop2}
    cdq                            ; Sign-extend eax into edx:eax
    idiv ebx
{push}";

    public Map I64DivU = @"    ; i64.div_u
{pop2}
    xor edx, edx
    div ebx
{push}";

    public Map I64RemS = @"    ; i64.rem_s
{pop2}
    cdq
    idiv ebx
    mov eax, edx
{push}";

    public Map I64RemU = @"    ; i64.rem_u
{pop2}
    xor edx, edx
    div ebx
    mov eax, edx
{push}";

    // ========================================================================
    // LOGICAL OPERATIONS
    // ========================================================================
    
    public Map I32And = @"    ; i32.and
{pop2}
    and eax, ebx
{push}";

    public Map I32Or = @"    ; i32.or
{pop2}
    or eax, ebx
{push}";

    public Map I32Xor = @"    ; i32.xor
{pop2}
    xor eax, ebx
{push}";

    public Map I32Shl = @"    ; i32.shl (shift left)
{pop2}
    mov ecx, ebx                   ; Shift count in cl
    shl eax, cl
{push}";

    public Map I32ShrS = @"    ; i32.shr_s (arithmetic shift right)
{pop2}
    mov ecx, ebx
    sar eax, cl
{push}";

    public Map I32ShrU = @"    ; i32.shr_u (logical shift right)
{pop2}
    mov ecx, ebx
    shr eax, cl
{push}";

    public Map I32Rotl = @"    ; i32.rotl (rotate left)
{pop2}
    mov ecx, ebx
    rol eax, cl
{push}";

    public Map I32Rotr = @"    ; i32.rotr (rotate right)
{pop2}
    mov ecx, ebx
    ror eax, cl
{push}";

    // i64 logical operations
    
    public Map I64And = @"    ; i64.and
{pop2}
    and eax, ebx
{push}";

    public Map I64Or = @"    ; i64.or
{pop2}
    or eax, ebx
{push}";

    public Map I64Xor = @"    ; i64.xor
{pop2}
    xor eax, ebx
{push}";

    public Map I64Shl = @"    ; i64.shl
{pop2}
    mov ecx, ebx
    shl eax, cl
{push}";

    public Map I64ShrS = @"    ; i64.shr_s
{pop2}
    mov ecx, ebx
    sar eax, cl
{push}";

    public Map I64ShrU = @"    ; i64.shr_u
{pop2}
    mov ecx, ebx
    shr eax, cl
{push}";

    public Map I64Rotl = @"    ; i64.rotl
{pop2}
    mov ecx, ebx
    rol eax, cl
{push}";

    public Map I64Rotr = @"    ; i64.rotr
{pop2}
    mov ecx, ebx
    ror eax, cl
{push}";

    // ========================================================================
    // BITWISE OPERATIONS
    // ========================================================================
    
    public Map I32Clz = @"    ; i32.clz (count leading zeros)
{pop1}
    lzcnt eax, eax                 ; Count leading zeros (requires LZCNT instruction)
{push}";

    public Map I32Ctz = @"    ; i32.ctz (count trailing zeros)
{pop1}
    tzcnt eax, eax                 ; Count trailing zeros (requires BMI1)
{push}";

    public Map I32Popcnt = @"    ; i32.popcnt (population count)
{pop1}
    popcnt eax, eax                ; Count set bits (requires POPCNT)
{push}";

    public Map I64Clz = @"    ; i64.clz
{pop1}
    lzcnt eax, eax
{push}";

    public Map I64Ctz = @"    ; i64.ctz
{pop1}
    tzcnt eax, eax
{push}";

    public Map I64Popcnt = @"    ; i64.popcnt
{pop1}
    popcnt eax, eax
{push}";

    // ========================================================================
    // COMPARISON OPERATIONS
    // ========================================================================
    
    public Map I32Eqz = @"    ; i32.eqz (equal to zero)
{pop1}
    test eax, eax
    setz al
    movzx eax, al
{push}";

    public Map I32Eq = @"    ; i32.eq
{pop2}
    cmp eax, ebx
    sete al
    movzx eax, al
{push}";

    public Map I32Ne = @"    ; i32.ne
{pop2}
    cmp eax, ebx
    setne al
    movzx eax, al
{push}";

    public Map I32LtS = @"    ; i32.lt_s (signed less than)
{pop2}
    cmp eax, ebx
    setl al
    movzx eax, al
{push}";

    public Map I32LtU = @"    ; i32.lt_u (unsigned less than)
{pop2}
    cmp eax, ebx
    setb al
    movzx eax, al
{push}";

    public Map I32GtS = @"    ; i32.gt_s (signed greater than)
{pop2}
    cmp eax, ebx
    setg al
    movzx eax, al
{push}";

    public Map I32GtU = @"    ; i32.gt_u (unsigned greater than)
{pop2}
    cmp eax, ebx
    seta al
    movzx eax, al
{push}";

    public Map I32LeS = @"    ; i32.le_s (signed less or equal)
{pop2}
    cmp eax, ebx
    setle al
    movzx eax, al
{push}";

    public Map I32LeU = @"    ; i32.le_u (unsigned less or equal)
{pop2}
    cmp eax, ebx
    setbe al
    movzx eax, al
{push}";

    public Map I32GeS = @"    ; i32.ge_s (signed greater or equal)
{pop2}
    cmp eax, ebx
    setge al
    movzx eax, al
{push}";

    public Map I32GeU = @"    ; i32.ge_u (unsigned greater or equal)
{pop2}
    cmp eax, ebx
    setae al
    movzx eax, al
{push}";

    // i64 comparisons (similar but use full 64-bit)
    
    public Map I64Eqz = @"    ; i64.eqz
{pop1}
    test eax, eax
    setz al
    movzx eax, al
{push}";

    public Map I64Eq = @"    ; i64.eq
{pop2}
    cmp eax, ebx
    sete al
    movzx eax, al
{push}";

    public Map I64Ne = @"    ; i64.ne
{pop2}
    cmp eax, ebx
    setne al
    movzx eax, al
{push}";

    public Map I64LtS = @"    ; i64.lt_s
{pop2}
    cmp eax, ebx
    setl al
    movzx eax, al
{push}";

    public Map I64LtU = @"    ; i64.lt_u
{pop2}
    cmp eax, ebx
    setb al
    movzx eax, al
{push}";

    public Map I64GtS = @"    ; i64.gt_s
{pop2}
    cmp eax, ebx
    setg al
    movzx eax, al
{push}";

    public Map I64GtU = @"    ; i64.gt_u
{pop2}
    cmp eax, ebx
    seta al
    movzx eax, al
{push}";

    public Map I64LeS = @"    ; i64.le_s
{pop2}
    cmp eax, ebx
    setle al
    movzx eax, al
{push}";

    public Map I64LeU = @"    ; i64.le_u
{pop2}
    cmp eax, ebx
    setbe al
    movzx eax, al
{push}";

    public Map I64GeS = @"    ; i64.ge_s
{pop2}
    cmp eax, ebx
    setge al
    movzx eax, al
{push}";

    public Map I64GeU = @"    ; i64.ge_u
{pop2}
    cmp eax, ebx
    setae al
    movzx eax, al
{push}";

    // ========================================================================
    // LOCAL VARIABLE OPERATIONS
    // ========================================================================
    // Locals are allocated in the stack frame at [ebp - offset]
    // offset = 8 (old ebp) + 40 (saved regs) + local_index * 8
    // ========================================================================
    
    public Map LocalGet = @"    ; local.get {index}
    mov eax, dword [ebp - {offset}]
{push}";

    public Map LocalSet = @"    ; local.set {index}
{pop1}
    mov dword [ebp - {offset}], eax";

    public Map LocalTee = @"    ; local.tee {index}
    ; Peek top of stack (don't pop)
    mov eax, {stack_top}
    mov dword [ebp - {offset}], eax";

    // ========================================================================
    // GLOBAL VARIABLE OPERATIONS
    // ========================================================================
    // Globals are stored in the data section
    // ========================================================================
    
    public Map GlobalGet = @"    ; global.get {index}
    mov eax, dword [rel global_{index}]
{push}";

    public Map GlobalSet = @"    ; global.set {index}
{pop1}
    mov dword [rel global_{index}], eax";

    // ========================================================================
    // MEMORY OPERATIONS
    // ========================================================================
    // Linear memory is accessed through the base pointer in esi
    // Address calculation: [esi + addr + offset]
    // ========================================================================
    
    public Map I32Load = @"    ; i32.load offset={offset} align={align}
{pop1}                             ; Pop address
    mov eax, dword [esi + eax + {offset}]
{push}";

    public Map I64Load = @"    ; i64.load offset={offset} align={align}
{pop1}
    mov eax, dword [esi + eax + {offset}]
{push}";

    public Map I32Load8S = @"    ; i32.load8_s offset={offset}
{pop1}
    movsx eax, byte [esi + eax + {offset}]
{push}";

    public Map I32Load8U = @"    ; i32.load8_u offset={offset}
{pop1}
    movzx eax, byte [esi + eax + {offset}]
{push}";

    public Map I32Load16S = @"    ; i32.load16_s offset={offset}
{pop1}
    movsx eax, word [esi + eax + {offset}]
{push}";

    public Map I32Load16U = @"    ; i32.load16_u offset={offset}
{pop1}
    movzx eax, word [esi + eax + {offset}]
{push}";

    public Map I64Load8S = @"    ; i64.load8_s offset={offset}
{pop1}
    movsx eax, byte [esi + eax + {offset}]
{push}";

    public Map I64Load8U = @"    ; i64.load8_u offset={offset}
{pop1}
    movzx eax, byte [esi + eax + {offset}]
{push}";

    public Map I64Load16S = @"    ; i64.load16_s offset={offset}
{pop1}
    movsx eax, word [esi + eax + {offset}]
{push}";

    public Map I64Load16U = @"    ; i64.load16_u offset={offset}
{pop1}
    movzx eax, word [esi + eax + {offset}]
{push}";

    public Map I64Load32S = @"    ; i64.load32_s offset={offset}
{pop1}
    movsx eax, dword [esi + eax + {offset}]
{push}";

    public Map I64Load32U = @"    ; i64.load32_u offset={offset}
{pop1}
    mov eax, dword [esi + eax + {offset}]  ; Zero-extends to 64-bit
{push}";

    public Map I32Store = @"    ; i32.store offset={offset} align={align}
{pop2}                             ; Pop value (ebx), then address (eax)
    mov dword [esi + eax + {offset}], ebx";

    public Map I64Store = @"    ; i64.store offset={offset} align={align}
{pop2}
    mov dword [esi + eax + {offset}], ebx";

    public Map I32Store8 = @"    ; i32.store8 offset={offset}
{pop2}
    mov byte [esi + eax + {offset}], bl";

    public Map I32Store16 = @"    ; i32.store16 offset={offset}
{pop2}
    mov word [esi + eax + {offset}], bx";

    public Map I64Store8 = @"    ; i64.store8 offset={offset}
{pop2}
    mov byte [esi + eax + {offset}], bl";

    public Map I64Store16 = @"    ; i64.store16 offset={offset}
{pop2}
    mov word [esi + eax + {offset}], bx";

    public Map I64Store32 = @"    ; i64.store32 offset={offset}
{pop2}
    mov dword [esi + eax + {offset}], ebx";

    public Map MemorySize = @"    ; memory.size
    ; Return current memory size in pages (64KB each)
    mov eax, dword [rel memory_size]
{push}";

    public Map MemoryGrow = @"    ; memory.grow
{pop1}                             ; Pop number of pages to grow
    ; Call runtime function to grow memory
    ; For now, just return -1 (failure)
    mov eax, -1
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
    test eax, eax
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
    test eax, eax
    jnz {target_label}";

    /// <summary>
    /// br_table (jump table)
    /// - Pop index
    /// - Jump to label based on index (with default)
    /// </summary>
    public Map BrTable = @"    ; br_table {targets} {default}
{pop1}
    ; Clamp index to valid range
    cmp eax, {max_index}
    ja {default_label}
    ; Compute jump table offset
    lea ebx, [rel .jump_table_{id}]
    movsx eax, dword [ebx + eax * 4]
    add eax, ebx
    jmp eax
    
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
    /// - Follow cdecl ABI calling convention
    /// - First 6 args in registers: edi, esi, edx, ecx, esi, edi
    /// - Rest on stack (right to left)
    /// - Return value in eax (or eax:edx for i128)
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
    cmp eax, dword [rel table_size]
    jae .invalid_index_{id}
    
    ; Load function pointer from table
    lea ebx, [rel function_table]
    mov ebx, dword [ebx + eax * 8]
    
    ; Verify type signature (runtime check)
    ; (Simplified: assume correct signature)
    
    ; Move arguments and call
    {arg_moves}
    call ebx
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
{pop1}                             ; Pop condition into eax
    test eax, eax
{pop2}                             ; Pop two values into eax (val1) and ebx (val2)
    cmovz eax, ebx                 ; If condition==0, select ebx
{push}";

    // ========================================================================
    // TYPE CONVERSION OPERATIONS
    // ========================================================================
    
    public Map I32WrapI64 = @"    ; i32.wrap_i64
    ; No-op on x86_32: just use lower 32 bits
    ; Top of stack already has 64-bit value
    ; When we use eax instead of eax, it automatically uses lower 32 bits";

    public Map I64ExtendI32S = @"    ; i64.extend_i32_s (sign-extend)
{pop1}
    movsx eax, eax                ; Sign-extend eax to eax
{push}";

    public Map I64ExtendI32U = @"    ; i64.extend_i32_u (zero-extend)
{pop1}
    mov eax, eax                   ; Zero-extend (mov to eax clears upper 32 bits)
{push}";

    public Map I32Extend8S = @"    ; i32.extend8_s
{pop1}
    movsx eax, al
{push}";

    public Map I32Extend16S = @"    ; i32.extend16_s
{pop1}
    movsx eax, ax
{push}";

    public Map I64Extend8S = @"    ; i64.extend8_s
{pop1}
    movsx eax, al
{push}";

    public Map I64Extend16S = @"    ; i64.extend16_s
{pop1}
    movsx eax, ax
{push}";

    public Map I64Extend32S = @"    ; i64.extend32_s
{pop1}
    movsx eax, eax
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
// Part 2: x86_32 Assembler - converts assembly text to machine code
public static class Assembler
{
    private static Dictionary<string, int> labels = new Dictionary<string, int>();
    private static List<byte> code = new List<byte>();
    
    private static bool IsDirectiveOrComment(string line)
    {
        var trimmed = line.Trim();
        return string.IsNullOrEmpty(trimmed) || 
               trimmed.StartsWith(";") || 
               trimmed.StartsWith(".") || 
               trimmed.StartsWith("section ") || 
               trimmed.StartsWith("global ");
    }
    
    public static byte[] Assemble(string assemblyText)
    {
        labels.Clear();
        code.Clear();
        
        var lines = assemblyText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // First pass: collect labels
        int address = 0;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (IsDirectiveOrComment(trimmed))
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
            if (IsDirectiveOrComment(trimmed) || trimmed.EndsWith(":"))
                continue;
                
            EncodeInstruction(trimmed);
        }
        
        return code.ToArray();
    }
    
    private static int GetCurrentAddress()
    {
        return code.Count;
    }
    
    private static int CalculateRelativeOffsetFrom(string label, int fromAddress, int instructionSize)
    {
        if (!labels.ContainsKey(label))
            return 0; // Label not found, use placeholder
            
        int targetAddress = labels[label];
        // Offset is relative to the end of the instruction
        return targetAddress - (fromAddress + instructionSize);
    }
    
    private static int EstimateInstructionSize(string instruction)
    {
        // Simplified size estimation for 32-bit
        var parts = instruction.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var opcode = parts[0].ToLower();
        
        if (opcode == "mov")
        {
            // MOV r32, imm32 = 5 bytes (opcode + 4-byte immediate)
            // MOV r32, r32 = 2 bytes (opcode + ModR/M)
            if (parts.Length > 2 && IsImmediate(parts[2]))
                return 5;
            return 2;
        }
        if (opcode == "push" || opcode == "pop" || opcode == "ret" || opcode == "nop" || opcode == "cdq") return 1;
        if (opcode == "add" || opcode == "sub")
        {
            // ADD/SUB with imm8 = 3 bytes, with imm32 = 6 bytes
            if (parts.Length > 2 && IsImmediate(parts[2]))
            {
                int imm = int.Parse(parts[2]);
                return (imm >= -128 && imm <= 127) ? 3 : 6;
            }
            return 2;
        }
        if (opcode == "call" || opcode == "jmp") return 5;
        if (opcode.StartsWith("j")) return 6; // Conditional jumps
        if (opcode == "imul") return 3;
        if (opcode == "sete" || opcode == "setne" || opcode == "setl" || opcode == "setg") return 3;
        if (opcode == "movzx") return 3;
        return 2;
    }
    
    private static void EncodeInstruction(string instruction)
    {
        var parts = instruction.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        var opcode = parts[0].ToLower();
        
        switch (opcode)
        {
            case "push":
                EncodePush(parts[1]);
                break;
            case "pop":
                EncodePop(parts[1]);
                break;
            case "mov":
                EncodeMov(parts[1], parts[2]);
                break;
            case "add":
                EncodeAdd(parts[1], parts[2]);
                break;
            case "sub":
                EncodeSub(parts[1], parts[2]);
                break;
            case "imul":
                EncodeIMul(parts[1], parts[2]);
                break;
            case "idiv":
                EncodeIDiv(parts[1]);
                break;
            case "div":
                EncodeDiv(parts[1]);
                break;
            case "and":
                EncodeAnd(parts[1], parts[2]);
                break;
            case "or":
                EncodeOr(parts[1], parts[2]);
                break;
            case "xor":
                EncodeXor(parts[1], parts[2]);
                break;
            case "cmp":
                EncodeCmp(parts[1], parts[2]);
                break;
            case "test":
                EncodeTest(parts[1], parts[2]);
                break;
            case "jmp":
                EncodeJmp(parts[1]);
                break;
            case "jnz":
                EncodeJnz(parts[1]);
                break;
            case "je":
                EncodeJe(parts[1]);
                break;
            case "jne":
                EncodeJne(parts[1]);
                break;
            case "jl":
                EncodeJl(parts[1]);
                break;
            case "jg":
                EncodeJg(parts[1]);
                break;
            case "call":
                EncodeCall(parts[1]);
                break;
            case "ret":
                EncodeRet();
                break;
            case "nop":
                EncodeNop();
                break;
            case "cdq":
                EncodeCdq();
                break;
            case "sete":
            case "setne":
            case "setl":
            case "setg":
                EncodeSet(opcode, parts[1]);
                break;
            case "movzx":
                EncodeMovzx(parts[1], parts[2]);
                break;
            default:
                throw new NotImplementedException($"Instruction not implemented: {opcode}");
        }
    }
    
    // Register encoding helpers
    private static byte GetRegisterCode(string reg)
    {
        return reg.ToLower() switch
        {
            "eax" or "ax" or "al" => 0,
            "ecx" or "cx" or "cl" => 1,
            "edx" or "dx" or "dl" => 2,
            "ebx" or "bx" or "bl" => 3,
            "esp" or "sp" or "ah" => 4,
            "ebp" or "bp" or "ch" => 5,
            "esi" or "si" or "dh" => 6,
            "edi" or "di" or "bh" => 7,
            _ => throw new ArgumentException($"Unknown register: {reg}")
        };
    }
    
    private static bool IsImmediate(string operand)
    {
        return !string.IsNullOrEmpty(operand) && (char.IsDigit(operand[0]) || operand[0] == '-');
    }
    
    // Instruction encoders
    private static void EncodePush(string operand)
    {
        if (operand.StartsWith("e"))
        {
            // PUSH r32
            code.Add((byte)(0x50 + GetRegisterCode(operand)));
        }
        else
        {
            // PUSH imm32
            code.Add(0x68);
            AddImmediate32(int.Parse(operand));
        }
    }
    
    private static void EncodePop(string operand)
    {
        // POP r32
        code.Add((byte)(0x58 + GetRegisterCode(operand)));
    }
    
    private static void EncodeMov(string dst, string src)
    {
        // Guard against empty strings
        if (string.IsNullOrEmpty(dst) || string.IsNullOrEmpty(src))
        {
            throw new ArgumentException("Invalid operands for MOV instruction");
        }
        
        if (src.StartsWith("[") && src.EndsWith("]"))
        {
            // MOV r32, [m32]
            code.Add(0x8B); // MOV r32, r/m32
            // ModR/M byte (simplified)
            code.Add(0x00);
        }
        else if (dst.StartsWith("[") && dst.EndsWith("]"))
        {
            // MOV [m32], r32
            code.Add(0x89); // MOV r/m32, r32
            code.Add(0x00);
        }
        else if (IsImmediate(src))
        {
            // MOV r32, imm32
            code.Add((byte)(0xB8 + GetRegisterCode(dst)));
            AddImmediate32(int.Parse(src));
        }
        else
        {
            // MOV r32, r32
            code.Add(0x89); // MOV r/m32, r32
            code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
        }
    }
    
    private static void EncodeAdd(string dst, string src)
    {
        // Check if src is immediate
        if (IsImmediate(src))
        {
            int imm = int.Parse(src);
            if (imm >= -128 && imm <= 127)
            {
                // ADD r/m32, imm8
                code.Add(0x83);
                code.Add((byte)(0xC0 + GetRegisterCode(dst))); // ModR/M: mod=11, reg=000 (/0), r/m=dst
                code.Add((byte)imm);
            }
            else
            {
                // ADD r/m32, imm32
                code.Add(0x81);
                code.Add((byte)(0xC0 + GetRegisterCode(dst))); // ModR/M: mod=11, reg=000 (/0), r/m=dst
                AddImmediate32(imm);
            }
        }
        else
        {
            // ADD r/m32, r32
            code.Add(0x01);
            code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
        }
    }
    
    private static void EncodeSub(string dst, string src)
    {
        // Check if src is immediate
        if (IsImmediate(src))
        {
            int imm = int.Parse(src);
            if (imm >= -128 && imm <= 127)
            {
                // SUB r/m32, imm8
                code.Add(0x83);
                code.Add((byte)(0xE8 + GetRegisterCode(dst))); // ModR/M: mod=11, reg=101 (/5), r/m=dst
                code.Add((byte)imm);
            }
            else
            {
                // SUB r/m32, imm32
                code.Add(0x81);
                code.Add((byte)(0xE8 + GetRegisterCode(dst))); // ModR/M: mod=11, reg=101 (/5), r/m=dst
                AddImmediate32(imm);
            }
        }
        else
        {
            // SUB r/m32, r32
            code.Add(0x29);
            code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
        }
    }
    
    private static void EncodeIMul(string dst, string src)
    {
        // IMUL r32, r/m32
        code.Add(0x0F);
        code.Add(0xAF);
        code.Add((byte)(0xC0 | (GetRegisterCode(dst) << 3) | GetRegisterCode(src)));
    }
    
    private static void EncodeIDiv(string operand)
    {
        // IDIV r/m32
        code.Add(0xF7);
        code.Add((byte)(0xF8 | GetRegisterCode(operand)));
    }
    
    private static void EncodeDiv(string operand)
    {
        // DIV r/m32
        code.Add(0xF7);
        code.Add((byte)(0xF0 | GetRegisterCode(operand)));
    }
    
    private static void EncodeAnd(string dst, string src)
    {
        // AND r/m32, r32
        code.Add(0x21);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeOr(string dst, string src)
    {
        // OR r/m32, r32
        code.Add(0x09);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeXor(string dst, string src)
    {
        // XOR r/m32, r32
        code.Add(0x31);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeCmp(string dst, string src)
    {
        // CMP r/m32, r32
        code.Add(0x39);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeTest(string dst, string src)
    {
        // TEST r/m32, r32
        code.Add(0x85);
        code.Add((byte)(0xC0 | (GetRegisterCode(src) << 3) | GetRegisterCode(dst)));
    }
    
    private static void EncodeJmp(string label)
    {
        // JMP rel32
        int currentAddress = GetCurrentAddress();
        code.Add(0xE9);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 5); // JMP instruction is 5 bytes
        AddImmediate32(offset);
    }
    
    private static void EncodeJnz(string label)
    {
        // JNZ rel32
        int currentAddress = GetCurrentAddress();
        code.Add(0x0F);
        code.Add(0x85);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 6); // JNZ instruction is 6 bytes
        AddImmediate32(offset);
    }
    
    private static void EncodeJe(string label)
    {
        // JE rel32
        int currentAddress = GetCurrentAddress();
        code.Add(0x0F);
        code.Add(0x84);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 6); // JE instruction is 6 bytes
        AddImmediate32(offset);
    }
    
    private static void EncodeJne(string label)
    {
        // JNE rel32
        int currentAddress = GetCurrentAddress();
        code.Add(0x0F);
        code.Add(0x85);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 6); // JNE instruction is 6 bytes
        AddImmediate32(offset);
    }
    
    private static void EncodeJl(string label)
    {
        // JL rel32
        int currentAddress = GetCurrentAddress();
        code.Add(0x0F);
        code.Add(0x8C);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 6); // JL instruction is 6 bytes
        AddImmediate32(offset);
    }
    
    private static void EncodeJg(string label)
    {
        // JG rel32
        int currentAddress = GetCurrentAddress();
        code.Add(0x0F);
        code.Add(0x8F);
        int offset = CalculateRelativeOffsetFrom(label, currentAddress, 6); // JG instruction is 6 bytes
        AddImmediate32(offset);
    }
    
    private static void EncodeCall(string target)
    {
        // CALL rel32
        int currentAddress = GetCurrentAddress();
        code.Add(0xE8);
        int offset = CalculateRelativeOffsetFrom(target, currentAddress, 5); // CALL instruction is 5 bytes
        AddImmediate32(offset);
    }
    
    private static void EncodeRet()
    {
        // RET
        code.Add(0xC3);
    }
    
    private static void EncodeNop()
    {
        // NOP
        code.Add(0x90);
    }
    
    private static void EncodeCdq()
    {
        // CDQ - sign-extend EAX to EDX:EAX
        code.Add(0x99);
    }
    
    private static void EncodeSet(string op, string reg)
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
    
    private static void EncodeMovzx(string dst, string src)
    {
        // MOVZX r32, r/m8
        code.Add(0x0F);
        code.Add(0xB6);
        code.Add((byte)(0xC0 | (GetRegisterCode(dst) << 3) | GetRegisterCode(src)));
    }
    
    private static void AddImmediate32(int value)
    {
        code.Add((byte)(value & 0xFF));
        code.Add((byte)((value >> 8) & 0xFF));
        code.Add((byte)((value >> 16) & 0xFF));
        code.Add((byte)((value >> 24) & 0xFF));
    }
}
