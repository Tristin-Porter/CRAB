// WASM Output Correctness Tests for CRAB
// Tests that generated WASM is correct and valid

namespace CRAB.Testing.WASM.Correctness
{
    /// <summary>
    /// Test: Simple function compiles to valid WASM
    /// Expected: WASM function with correct signature
    /// </summary>
    class SimpleFunctionTest
    {
        static int Add(int a, int b)
        {
            return a + b;
        }
        
        // Expected WASM:
        // (func $Add (param $a i32) (param $b i32) (result i32)
        //   local.get $a
        //   local.get $b
        //   i32.add
        // )
    }

    /// <summary>
    /// Test: Local variables compile to WASM locals
    /// Expected: WASM locals declared correctly
    /// </summary>
    class LocalVariablesTest
    {
        static int Calculate(int x)
        {
            int temp1 = x * 2;
            int temp2 = temp1 + 10;
            int result = temp2 * 3;
            return result;
        }
        
        // Expected WASM:
        // (func $Calculate (param $x i32) (result i32)
        //   (local $temp1 i32)
        //   (local $temp2 i32)
        //   (local $result i32)
        //   ...
        // )
    }

    /// <summary>
    /// Test: Conditional compiles to br_if
    /// Expected: WASM branch instructions
    /// </summary>
    class ConditionalTest
    {
        static int Max(int a, int b)
        {
            if (a > b)
                return a;
            else
                return b;
        }
        
        // Expected WASM uses: br_if, block, and conditional logic
    }

    /// <summary>
    /// Test: Loop compiles to WASM loop
    /// Expected: WASM loop construct
    /// </summary>
    class LoopTest
    {
        static int Sum(int n)
        {
            int total = 0;
            for (int i = 0; i < n; i++)
            {
                total += i;
            }
            return total;
        }
        
        // Expected WASM:
        // (loop $continue
        //   ...
        //   br_if $continue
        // )
    }

    /// <summary>
    /// Test: Memory access compiles to WASM memory operations
    /// Expected: i32.load, i32.store instructions
    /// </summary>
    class MemoryAccessTest
    {
        static void WriteMemory()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(100);
                
                // Should compile to i32.store
                System.Runtime.InteropServices.Marshal.WriteInt32(buffer, 0, 42);
                
                // Should compile to i32.load
                int value = System.Runtime.InteropServices.Marshal.ReadInt32(buffer, 0);
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
        }
        
        // Expected WASM: i32.load, i32.store with memory offset
    }

    /// <summary>
    /// Test: Function calls compile to WASM call
    /// Expected: call instruction with function index
    /// </summary>
    class FunctionCallTest
    {
        static int Helper(int x)
        {
            return x * 2;
        }
        
        static int Caller(int x)
        {
            int result = Helper(x);
            return result + 10;
        }
        
        // Expected WASM:
        // (func $Caller
        //   local.get $x
        //   call $Helper
        //   i32.const 10
        //   i32.add
        // )
    }

    /// <summary>
    /// Test: Return values compile correctly
    /// Expected: Correct result type in WASM
    /// </summary>
    class ReturnValueTest
    {
        static int ReturnInt() => 42;
        static long ReturnLong() => 9223372036854775807L;
        static void ReturnVoid() { }
        
        // Expected WASM:
        // (func $ReturnInt (result i32) ...)
        // (func $ReturnLong (result i64) ...)
        // (func $ReturnVoid ...)
    }

    /// <summary>
    /// Test: Array access compiles to memory operations
    /// Expected: Correct offset calculations
    /// </summary>
    class ArrayAccessTest
    {
        static void TestArrayAccess()
        {
            int[] array = new int[10];
            array[0] = 42;
            array[5] = 100;
            int value = array[5];
        }
        
        // Expected WASM: Memory operations with computed offsets
    }

    /// <summary>
    /// Test: Struct fields compile to memory layout
    /// Expected: Correct struct layout in linear memory
    /// </summary>
    class StructFieldsTest
    {
        struct Point
        {
            public int X;
            public int Y;
        }
        
        static void TestStruct()
        {
            Point p;
            p.X = 10;
            p.Y = 20;
            int sum = p.X + p.Y;
        }
        
        // Expected WASM: Struct in linear memory with field offsets
    }

    /// <summary>
    /// Test: Constants compile correctly
    /// Expected: i32.const, i64.const, f32.const, f64.const
    /// </summary>
    class ConstantsTest
    {
        static void TestConstants()
        {
            int i = 42;
            long l = 1000000000000L;
            float f = 3.14f;
            double d = 2.71828;
        }
        
        // Expected WASM: const instructions for each type
    }
}
