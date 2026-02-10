// Deallocation Point Computation Tests for CRAB Automatic Memory Model (CTGC)
// Tests the compiler's ability to compute optimal deallocation points

namespace CRAB.Testing.Automatic.Deallocation
{
    /// <summary>
    /// Test: Last use deallocation
    /// Expected: Objects deallocated immediately after last use
    /// </summary>
    class LastUseDeallocationTest
    {
        static void TestLastUse()
        {
            var obj = new DeallocationTestClass();
            obj.DoWork();
            obj.Process();
            // Last use - deallocation point here
            
            var other = new DeallocationTestClass();
            other.DoWork();
            // other deallocated here
        }
    }

    /// <summary>
    /// Test: Early deallocation optimization
    /// Expected: Objects freed before scope exit if safe
    /// </summary>
    class EarlyDeallocationTest
    {
        static void TestEarlyDeallocation()
        {
            var obj1 = new DeallocationTestClass();
            obj1.DoWork();
            // obj1 last use - can deallocate early
            
            var obj2 = new DeallocationTestClass();
            obj2.DoWork();
            
            var obj3 = new DeallocationTestClass();
            obj3.DoWork();
            
            // obj2 and obj3 deallocated here
        }
    }

    /// <summary>
    /// Test: Conditional deallocation
    /// Expected: Deallocation inserted on all paths
    /// </summary>
    class ConditionalDeallocationTest
    {
        static void TestConditionalDeallocation(bool condition)
        {
            var obj = new DeallocationTestClass();
            
            if (condition)
            {
                obj.DoWork();
                // Deallocation here on this path
                return;
            }
            
            obj.Process();
            // Deallocation here on this path
        }
    }

    /// <summary>
    /// Test: Loop deallocation
    /// Expected: Objects allocated in loops are freed at end of iteration
    /// </summary>
    class LoopDeallocationTest
    {
        static void TestLoopDeallocation()
        {
            for (int i = 0; i < 10; i++)
            {
                var obj = new DeallocationTestClass();
                obj.Value = i;
                obj.DoWork();
                // Deallocation here at end of iteration
            }
        }
        
        static void TestLoopWithBreak()
        {
            for (int i = 0; i < 10; i++)
            {
                var obj = new DeallocationTestClass();
                obj.Value = i;
                
                if (i == 5)
                {
                    // Deallocation before break
                    break;
                }
                
                obj.DoWork();
                // Deallocation here on normal iteration
            }
        }
    }

    /// <summary>
    /// Test: Multiple exit points
    /// Expected: Deallocation on every exit path
    /// </summary>
    class MultipleExitPointsTest
    {
        static int TestMultipleExits(int value)
        {
            var obj = new DeallocationTestClass();
            obj.Value = value;
            
            if (value < 0)
            {
                // Deallocation before return
                return -1;
            }
            
            obj.DoWork();
            
            if (value > 100)
            {
                // Deallocation before return
                return 100;
            }
            
            obj.Process();
            
            // Deallocation before return
            return obj.Value;
        }
    }

    /// <summary>
    /// Test: Exception path deallocation
    /// Expected: Objects deallocated even when exceptions thrown
    /// </summary>
    class ExceptionDeallocationTest
    {
        static void TestExceptionDeallocation()
        {
            var obj1 = new DeallocationTestClass();
            
            try
            {
                var obj2 = new DeallocationTestClass();
                obj2.DoWork();
                
                if (obj2.Value > 50)
                {
                    throw new System.InvalidOperationException();
                }
                
                // obj2 deallocated here on normal path
            }
            catch (System.Exception)
            {
                // obj2 already deallocated before entering catch
            }
            
            obj1.DoWork();
            // obj1 deallocated here
        }
    }

    /// <summary>
    /// Test: Aliasing and deallocation
    /// Expected: Deallocation only after all aliases gone
    /// </summary>
    class AliasingDeallocationTest
    {
        static void TestAliasing()
        {
            var obj = new DeallocationTestClass();
            var alias = obj; // Create alias
            
            obj.DoWork();
            // Cannot deallocate yet - alias still exists
            
            alias.Process();
            // Can deallocate here - no more uses of obj or alias
        }
    }

    /// <summary>
    /// Test: Array deallocation
    /// Expected: Array and elements deallocated together
    /// </summary>
    class ArrayDeallocationTest
    {
        static void TestArrayDeallocation()
        {
            var array = new DeallocationTestClass[5];
            
            for (int i = 0; i < 5; i++)
            {
                array[i] = new DeallocationTestClass { Value = i };
            }
            
            foreach (var item in array)
            {
                item.DoWork();
            }
            
            // Array and all elements deallocated here
        }
    }

    /// <summary>
    /// Test: Nested object deallocation
    /// Expected: Inner objects deallocated with container
    /// </summary>
    class NestedDeallocationTest
    {
        static void TestNestedObjects()
        {
            var container = new DeallocationContainer();
            container.Item1 = new DeallocationTestClass { Value = 1 };
            container.Item2 = new DeallocationTestClass { Value = 2 };
            
            container.Process();
            
            // Container and both items deallocated together
        }
    }

    /// <summary>
    /// Test: Method return and deallocation
    /// Expected: Objects not deallocated if returned
    /// </summary>
    class ReturnValueDeallocationTest
    {
        static DeallocationTestClass CreateObject()
        {
            var obj = new DeallocationTestClass();
            obj.DoWork();
            return obj;
            // obj NOT deallocated - returned to caller
        }
        
        static void UseObject()
        {
            var obj = CreateObject();
            obj.Process();
            // obj deallocated here
        }
    }

    /// <summary>
    /// Test: Closure capture deallocation
    /// Expected: Captured objects kept alive until closure unreachable
    /// </summary>
    class ClosureDeallocationTest
    {
        static void TestClosureCapture()
        {
            var obj = new DeallocationTestClass();
            
            System.Action action = () =>
            {
                obj.DoWork();
            };
            
            action();
            // Cannot deallocate obj yet - captured by closure
            
            action = null;
            // Now obj can be deallocated
        }
    }

    // Helper classes
    class DeallocationTestClass
    {
        public int Value { get; set; }
        
        public void DoWork()
        {
            Value += 10;
        }
        
        public void Process()
        {
            Value *= 2;
        }
    }

    class DeallocationContainer
    {
        public DeallocationTestClass? Item1 { get; set; }
        public DeallocationTestClass? Item2 { get; set; }
        
        public void Process()
        {
            Item1?.DoWork();
            Item2?.DoWork();
        }
    }
}
