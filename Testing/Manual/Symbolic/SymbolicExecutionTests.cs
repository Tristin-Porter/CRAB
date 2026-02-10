// Symbolic Execution Tests for CRAB Manual Memory Model
// Tests the compiler's symbolic execution engine for verifying manual memory

namespace CRAB.Testing.Manual.Symbolic
{
    /// <summary>
    /// Test: Simple path symbolic execution
    /// Expected: Single path verified symbolically
    /// </summary>
    class SimplePathTest
    {
        static void TestSimplePath()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                
                // Symbolic execution: buffer = Alloc(1024)
                
                WriteToBuffer(buffer, 0, 42);
                
                // Symbolic execution: Write(buffer, 0, 42) - verified safe
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                
                // Symbolic execution: Free(buffer) - verified safe
            }
            // ✓ Symbolically verified: no violations
        }
        
        static void WriteToBuffer(System.IntPtr buffer, int offset, byte value)
        {
            System.Runtime.InteropServices.Marshal.WriteByte(buffer, offset, value);
        }
    }

    /// <summary>
    /// Test: Conditional path exploration
    /// Expected: Both paths symbolically executed and verified
    /// </summary>
    class ConditionalPathTest
    {
        static void TestConditionalPath(bool condition)
        {
            manual
            {
                System.IntPtr buffer;
                int size;
                
                if (condition)
                {
                    // Path 1: symbolic state {buffer = Alloc(1024), size = 1024}
                    buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                    size = 1024;
                }
                else
                {
                    // Path 2: symbolic state {buffer = Alloc(2048), size = 2048}
                    buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(2048);
                    size = 2048;
                }
                
                // Merge: {buffer = Alloc(1024 | 2048), size = (1024 | 2048)}
                
                // Verify access within bounds on both paths
                if (size > 500)
                {
                    System.Runtime.InteropServices.Marshal.WriteByte(buffer, 500, 42);
                }
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ Both paths verified safe
        }
    }

    /// <summary>
    /// Test: Loop symbolic execution with bounded iterations
    /// Expected: Loop unrolled symbolically up to bound
    /// </summary>
    class LoopSymbolicTest
    {
        static void TestLoopSymbolic()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                
                // Symbolic execution unrolls loop
                for (int i = 0; i < 10; i++)
                {
                    // Iteration 0: Write(buffer, 0, val)
                    // Iteration 1: Write(buffer, 1, val)
                    // ...
                    // Iteration 9: Write(buffer, 9, val)
                    System.Runtime.InteropServices.Marshal.WriteByte(buffer, i, (byte)i);
                }
                
                // All iterations verified within bounds
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ Loop verified safe
        }
    }

    /// <summary>
    /// Test: Symbolic bounds checking
    /// Expected: Compiler verifies array accesses are within bounds
    /// </summary>
    class BoundsCheckingTest
    {
        static void TestBoundsChecking()
        {
            manual
            {
                int size = 1024;
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
                
                // Symbolic: buffer allocated [0, 1023]
                
                int offset = 100;
                
                // Symbolic: offset = 100
                // Verify: 0 <= 100 < 1024 ✓
                System.Runtime.InteropServices.Marshal.WriteByte(buffer, offset, 42);
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ Access verified in bounds
        }
    }

    /// <summary>
    /// Test: Symbolic verification of no use-after-free
    /// Expected: Compiler detects and prevents use after free
    /// </summary>
    class UseAfterFreeDetectionTest
    {
        static void TestUseAfterFreeDetection()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                
                // Symbolic: buffer = Alloc(1024), state = Valid
                
                System.Runtime.InteropServices.Marshal.WriteByte(buffer, 0, 42);
                
                // Symbolic: Write(buffer), verified buffer is Valid
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                
                // Symbolic: buffer state = Freed
                
                // Compiler would prevent:
                // Marshal.WriteByte(buffer, 1, 43); // ERROR: Use after free
            }
            // ✓ Use-after-free prevented
        }
    }

    /// <summary>
    /// Test: Symbolic verification of no double-free
    /// Expected: Compiler detects and prevents double-free
    /// </summary>
    class DoubleFreeDetectionTest
    {
        static void TestDoubleFreeDetection()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                
                // Symbolic: buffer = Alloc(1024), state = Valid
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                
                // Symbolic: buffer state = Freed
                
                // Compiler would prevent:
                // Marshal.FreeHGlobal(buffer); // ERROR: Double-free
            }
            // ✓ Double-free prevented
        }
    }

    /// <summary>
    /// Test: Symbolic execution with pointer arithmetic
    /// Expected: Derived pointer bounds verified
    /// </summary>
    class PointerArithmeticSymbolicTest
    {
        static void TestPointerArithmetic()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                
                // Symbolic: buffer = Alloc(1024), valid range [0, 1023]
                
                System.IntPtr offset = System.IntPtr.Add(buffer, 100);
                
                // Symbolic: offset = buffer + 100, valid range [100, 1023]
                
                System.Runtime.InteropServices.Marshal.WriteByte(offset, 0, 42);
                
                // Symbolic: Write(offset + 0 = buffer + 100)
                // Verify: 100 < 1024 ✓
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ Pointer arithmetic verified safe
        }
    }

    /// <summary>
    /// Test: Symbolic path merging
    /// Expected: Multiple paths merged into unified symbolic state
    /// </summary>
    class PathMergingTest
    {
        static void TestPathMerging(bool cond1, bool cond2)
        {
            manual
            {
                System.IntPtr buffer;
                
                if (cond1)
                {
                    if (cond2)
                    {
                        // Path 1: buffer = Alloc(512)
                        buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(512);
                    }
                    else
                    {
                        // Path 2: buffer = Alloc(1024)
                        buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                    }
                }
                else
                {
                    // Path 3: buffer = Alloc(2048)
                    buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(2048);
                }
                
                // Merged: buffer = Alloc(512 | 1024 | 2048)
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ All paths verified and merged
        }
    }

    /// <summary>
    /// Test: Symbolic execution with function calls
    /// Expected: Function calls symbolically inlined and verified
    /// </summary>
    class FunctionCallSymbolicTest
    {
        static void TestFunctionCall()
        {
            manual
            {
                System.IntPtr buffer = AllocateBuffer(1024);
                
                // Symbolic: buffer = result of AllocateBuffer
                
                ProcessBuffer(buffer, 100);
                
                // Symbolic: ProcessBuffer verified safe
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ Function calls symbolically verified
        }
        
        static System.IntPtr AllocateBuffer(int size)
        {
            return System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
        }
        
        static void ProcessBuffer(System.IntPtr buffer, int value)
        {
            System.Runtime.InteropServices.Marshal.WriteInt32(buffer, 0, value);
        }
    }

    /// <summary>
    /// Test: Symbolic execution with constraints
    /// Expected: Constraints propagated and verified
    /// </summary>
    class ConstraintPropagationTest
    {
        static void TestConstraintPropagation(int size)
        {
            manual
            {
                // Constraint: size > 0
                if (size <= 0)
                    return;
                
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
                
                // Symbolic: buffer = Alloc(size), where size > 0
                
                int offset = size / 2;
                
                // Symbolic: offset = size / 2
                // Constraint: 0 < offset < size (because size > 0)
                
                System.Runtime.InteropServices.Marshal.WriteByte(buffer, offset, 42);
                
                // Verify: offset < size ✓ (from constraint)
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ Constraints verified
        }
    }

    /// <summary>
    /// Test: Symbolic execution with exception paths
    /// Expected: Exception paths symbolically verified
    /// </summary>
    class ExceptionPathSymbolicTest
    {
        static void TestExceptionPath()
        {
            manual
            {
                System.IntPtr buffer = System.IntPtr.Zero;
                
                try
                {
                    buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                    
                    // Symbolic: Path 1 (normal): buffer = Alloc(1024)
                    
                    MayThrow();
                    
                    // Symbolic: Path 2 (exception): buffer = Alloc(1024), exception thrown
                    
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                    
                    // Symbolic: Path 1 continues here
                }
                catch (System.Exception)
                {
                    // Symbolic: Path 2 enters here
                    
                    if (buffer != System.IntPtr.Zero)
                    {
                        System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                    }
                }
            }
            // ✓ Both paths verified safe
        }
        
        static void MayThrow()
        {
            // May throw exception
        }
    }

    /// <summary>
    /// Test: Symbolic null pointer handling
    /// Expected: Null checks verified symbolically
    /// </summary>
    class NullPointerSymbolicTest
    {
        static void TestNullPointer(bool allocate)
        {
            manual
            {
                System.IntPtr buffer = System.IntPtr.Zero;
                
                // Symbolic: buffer = Null
                
                if (allocate)
                {
                    buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                    
                    // Symbolic: Path 1: buffer = Alloc(1024), not null
                }
                
                // Symbolic: Merge: buffer = Null | Alloc(1024)
                
                if (buffer != System.IntPtr.Zero)
                {
                    // Symbolic: refined to buffer = Alloc(1024)
                    
                    System.Runtime.InteropServices.Marshal.WriteByte(buffer, 0, 42);
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                }
            }
            // ✓ Null handling verified
        }
    }
}
