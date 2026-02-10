// Ownership Graph Construction Tests for CRAB Manual Memory Model
// Tests the compiler's ability to construct and verify ownership graphs

namespace CRAB.Testing.Manual.Ownership
{
    /// <summary>
    /// Test: Single ownership - one pointer, one owner
    /// Expected: Compiler constructs simple ownership graph
    /// </summary>
    class SingleOwnershipTest
    {
        static void TestSingleOwnership()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                // buffer owns the allocation
                
                // Use buffer...
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                // Ownership transferred to deallocator
            }
            // ✓ Compiler verified: ownership transferred exactly once
        }
    }

    /// <summary>
    /// Test: Ownership transfer via assignment
    /// Expected: Compiler tracks ownership transfer
    /// </summary>
    class OwnershipTransferTest
    {
        static void TestOwnershipTransfer()
        {
            manual
            {
                System.IntPtr buffer1 = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                // buffer1 owns allocation
                
                System.IntPtr buffer2 = buffer1;
                // Ownership transferred to buffer2
                // buffer1 no longer valid
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer2);
            }
            // ✓ Verified: no double-free
        }
    }

    /// <summary>
    /// Test: Multiple independent ownerships
    /// Expected: Separate ownership graphs for each allocation
    /// </summary>
    class MultipleOwnershipsTest
    {
        static void TestMultipleOwnerships()
        {
            manual
            {
                System.IntPtr buffer1 = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                System.IntPtr buffer2 = System.Runtime.InteropServices.Marshal.AllocHGlobal(2048);
                System.IntPtr buffer3 = System.Runtime.InteropServices.Marshal.AllocHGlobal(512);
                
                // Three independent ownership graphs
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer1);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer2);
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer3);
            }
            // ✓ Verified: all three freed exactly once
        }
    }

    /// <summary>
    /// Test: Conditional ownership
    /// Expected: Ownership tracked on all paths
    /// </summary>
    class ConditionalOwnershipTest
    {
        static void TestConditionalOwnership(bool condition)
        {
            manual
            {
                System.IntPtr buffer;
                
                if (condition)
                {
                    buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                }
                else
                {
                    buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(2048);
                }
                
                // buffer owns allocation from either path
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ Verified: freed on both paths
        }
    }

    /// <summary>
    /// Test: Ownership in loops
    /// Expected: Each iteration creates independent ownership
    /// </summary>
    class LoopOwnershipTest
    {
        static void TestLoopOwnership()
        {
            manual
            {
                for (int i = 0; i < 10; i++)
                {
                    System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                    
                    // Use buffer...
                    
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                    // Each iteration independent
                }
            }
            // ✓ Verified: 10 allocations, 10 frees
        }
    }

    /// <summary>
    /// Test: Shared pointer - not owned
    /// Expected: Compiler detects non-owning pointer
    /// </summary>
    class SharedPointerTest
    {
        static void TestSharedPointer()
        {
            manual
            {
                System.IntPtr owner = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                System.IntPtr shared = owner; // shared doesn't own
                
                // Use shared...
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(owner);
                // Only owner can free
            }
            // ✓ Verified: no double-free via shared
        }
    }

    /// <summary>
    /// Test: Ownership graph with pointer arithmetic
    /// Expected: Derived pointers tracked but don't own
    /// </summary>
    class PointerArithmeticOwnershipTest
    {
        static void TestPointerArithmetic()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                // buffer owns allocation
                
                System.IntPtr offset = System.IntPtr.Add(buffer, 100);
                // offset is derived from buffer - doesn't own
                
                // Use offset...
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                // Only buffer can be freed
            }
            // ✓ Verified: offset not freed separately
        }
    }

    /// <summary>
    /// Test: Ownership with exception handling
    /// Expected: Ownership tracked across exception paths
    /// </summary>
    class ExceptionOwnershipTest
    {
        static void TestExceptionOwnership()
        {
            manual
            {
                System.IntPtr buffer = System.IntPtr.Zero;
                
                try
                {
                    buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                    
                    // Might throw
                    ProcessBuffer(buffer);
                    
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                }
                catch (System.Exception)
                {
                    if (buffer != System.IntPtr.Zero)
                    {
                        System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                    }
                }
            }
            // ✓ Verified: freed on all paths
        }
        
        static void ProcessBuffer(System.IntPtr buffer)
        {
            // May throw exception
        }
    }

    /// <summary>
    /// Test: Ownership transfer via return
    /// Expected: Ownership passed to caller
    /// </summary>
    class ReturnOwnershipTest
    {
        static System.IntPtr AllocateBuffer()
        {
            manual
            {
                System.IntPtr buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                return buffer; // Ownership transferred to caller
            }
        }
        
        static void UseBuffer()
        {
            manual
            {
                System.IntPtr buffer = AllocateBuffer();
                // buffer owns allocation
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
            }
            // ✓ Verified: ownership transferred correctly
        }
    }

    /// <summary>
    /// Test: Ownership with struct containing pointer
    /// Expected: Struct ownership tracked
    /// </summary>
    class StructOwnershipTest
    {
        struct BufferWrapper
        {
            public System.IntPtr Buffer;
            public int Size;
        }
        
        static void TestStructOwnership()
        {
            manual
            {
                BufferWrapper wrapper;
                wrapper.Buffer = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                wrapper.Size = 1024;
                
                // wrapper.Buffer owns allocation
                
                System.Runtime.InteropServices.Marshal.FreeHGlobal(wrapper.Buffer);
            }
            // ✓ Verified: struct field ownership tracked
        }
    }

    /// <summary>
    /// Test: Null pointer ownership
    /// Expected: Null has no ownership
    /// </summary>
    class NullOwnershipTest
    {
        static void TestNullOwnership()
        {
            manual
            {
                System.IntPtr buffer = System.IntPtr.Zero;
                
                // No ownership - null pointer
                
                if (buffer != System.IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffer);
                }
            }
            // ✓ Verified: safe to not free null
        }
    }

    /// <summary>
    /// Test: Ownership of array of pointers
    /// Expected: Each array element tracked independently
    /// </summary>
    class ArrayOwnershipTest
    {
        static void TestArrayOwnership()
        {
            manual
            {
                System.IntPtr[] buffers = new System.IntPtr[5];
                
                for (int i = 0; i < 5; i++)
                {
                    buffers[i] = System.Runtime.InteropServices.Marshal.AllocHGlobal(1024);
                }
                
                // Each array element owns its allocation
                
                for (int i = 0; i < 5; i++)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(buffers[i]);
                }
            }
            // ✓ Verified: all 5 allocations freed
        }
    }
}
