// Lifetime Inference Tests for CRAB Automatic Memory Model (CTGC)
// Tests the compiler's ability to infer object lifetimes correctly

namespace CRAB.Testing.Automatic.Lifetime
{
    /// <summary>
    /// Test: Single allocation with simple lifetime
    /// Expected: Compiler infers lifetime ends at scope exit
    /// </summary>
    class SingleAllocationTest
    {
        static void TestSingleAllocation()
        {
            var obj = new TestClass();
            obj.DoWork();
            // Lifetime ends here - deallocation inserted by CTGC
        }
    }

    /// <summary>
    /// Test: Multiple allocations with independent lifetimes
    /// Expected: Each object has its own inferred lifetime
    /// </summary>
    class MultipleAllocationsTest
    {
        static void TestMultipleAllocations()
        {
            var obj1 = new TestClass();
            var obj2 = new TestClass();
            var obj3 = new TestClass();
            
            obj1.DoWork();
            obj2.DoWork();
            obj3.DoWork();
            
            // All lifetimes end here - deallocations inserted
        }
    }

    /// <summary>
    /// Test: Nested scopes with different lifetimes
    /// Expected: Inner scopes end lifetimes earlier
    /// </summary>
    class NestedScopesTest
    {
        static void TestNestedScopes()
        {
            var outer = new TestClass();
            
            {
                var inner1 = new TestClass();
                inner1.DoWork();
                // inner1 lifetime ends here
            }
            
            {
                var inner2 = new TestClass();
                inner2.DoWork();
                // inner2 lifetime ends here
            }
            
            outer.DoWork();
            // outer lifetime ends here
        }
    }

    /// <summary>
    /// Test: Conditional allocations
    /// Expected: Lifetime analysis handles all control flow paths
    /// </summary>
    class ConditionalAllocationsTest
    {
        static void TestConditionalAllocation(bool condition)
        {
            TestClass? obj = null;
            
            if (condition)
            {
                obj = new TestClass();
                obj.DoWork();
                // obj lifetime ends here if allocated
            }
            
            // No deallocation needed here since obj is already freed
        }
        
        static void TestIfElseAllocation(bool condition)
        {
            TestClass obj;
            
            if (condition)
            {
                obj = new TestClass();
            }
            else
            {
                obj = new TestClass();
            }
            
            obj.DoWork();
            // obj lifetime ends here - from either branch
        }
    }

    /// <summary>
    /// Test: Loop allocations
    /// Expected: Each iteration creates and destroys objects
    /// </summary>
    class LoopAllocationsTest
    {
        static void TestForLoop()
        {
            for (int i = 0; i < 10; i++)
            {
                var obj = new TestClass();
                obj.DoWork();
                // obj lifetime ends at end of each iteration
            }
        }
        
        static void TestWhileLoop()
        {
            int count = 0;
            while (count < 10)
            {
                var obj = new TestClass();
                obj.DoWork();
                count++;
                // obj lifetime ends here
            }
        }
        
        static void TestForeachLoop()
        {
            var items = new int[] { 1, 2, 3, 4, 5 };
            
            foreach (var item in items)
            {
                var obj = new TestClass();
                obj.Value = item;
                // obj lifetime ends here
            }
        }
    }

    /// <summary>
    /// Test: Method return value lifetimes
    /// Expected: Returned objects have extended lifetime to caller scope
    /// </summary>
    class ReturnValueLifetimeTest
    {
        static TestClass CreateObject()
        {
            var obj = new TestClass();
            obj.DoWork();
            return obj;
            // obj lifetime extended - not freed here
        }
        
        static void UseReturnedObject()
        {
            var obj = CreateObject();
            obj.DoWork();
            // obj lifetime ends here
        }
    }

    /// <summary>
    /// Test: Field assignment lifetimes
    /// Expected: Objects assigned to fields have lifetime tied to containing object
    /// </summary>
    class FieldAssignmentLifetimeTest
    {
        static void TestFieldAssignment()
        {
            var container = new Container();
            container.Item = new TestClass(); // Item lifetime tied to container
            container.Process();
            // Both container and container.Item freed here
        }
    }

    /// <summary>
    /// Test: Array element lifetimes
    /// Expected: Array elements have lifetime tied to array
    /// </summary>
    class ArrayLifetimeTest
    {
        static void TestArrayAllocation()
        {
            var array = new TestClass[5];
            
            for (int i = 0; i < 5; i++)
            {
                array[i] = new TestClass();
            }
            
            // All array elements freed with array
        }
    }

    /// <summary>
    /// Test: Exception handling and lifetimes
    /// Expected: Objects are freed even when exceptions occur
    /// </summary>
    class ExceptionLifetimeTest
    {
        static void TestTryCatchLifetime()
        {
            try
            {
                var obj = new TestClass();
                obj.DoWork();
                // May throw
                obj.ThrowIfNeeded();
                // obj freed here if no exception
            }
            catch (Exception)
            {
                // obj already freed before entering catch
            }
        }
        
        static void TestFinallyLifetime()
        {
            TestClass? obj = null;
            try
            {
                obj = new TestClass();
                obj.DoWork();
            }
            finally
            {
                // obj is still valid here but will be freed after finally
            }
            // obj freed here
        }
    }

    /// <summary>
    /// Test: Lambda capture lifetimes
    /// Expected: Captured variables have extended lifetimes
    /// </summary>
    class LambdaLifetimeTest
    {
        static void TestLambdaCapture()
        {
            var obj = new TestClass();
            
            Action action = () =>
            {
                obj.DoWork(); // obj captured - lifetime extended
            };
            
            action();
            // obj freed here after lambda no longer used
        }
    }

    // Helper classes for testing
    class TestClass
    {
        public int Value { get; set; }
        
        public void DoWork()
        {
            Value = 42;
        }
        
        public void ThrowIfNeeded()
        {
            if (Value > 100)
                throw new InvalidOperationException();
        }
    }

    class Container
    {
        public TestClass? Item { get; set; }
        
        public void Process()
        {
            Item?.DoWork();
        }
    }
}
