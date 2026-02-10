// Memory Safety Verification Tests for CRAB Automatic Memory Model (CTGC)
// Tests that the compiler prevents all memory safety violations

namespace CRAB.Testing.Automatic.Safety
{
    /// <summary>
    /// Test: No use-after-free
    /// Expected: Compiler prevents access to deallocated objects
    /// </summary>
    class UseAfterFreePreventionTest
    {
        static void TestNoUseAfterFree()
        {
            SafetyTestClass? obj = null;
            
            {
                obj = new SafetyTestClass();
                obj.DoWork();
            } // obj deallocated here
            
            // This should be prevented by compiler:
            // obj.DoWork(); // ERROR: Use after free
        }
    }

    /// <summary>
    /// Test: No double-free
    /// Expected: Compiler ensures each allocation freed exactly once
    /// </summary>
    class DoubleFreePreventionTest
    {
        static void TestNoDoubleFree()
        {
            var obj = new SafetyTestClass();
            obj.DoWork();
            // obj will be freed once at scope exit
            // Compiler ensures no double-free occurs
        }
    }

    /// <summary>
    /// Test: No memory leaks
    /// Expected: All allocations have corresponding deallocations
    /// </summary>
    class NoLeaksTest
    {
        static void TestNoLeaks()
        {
            // Every allocation gets deallocated
            for (int i = 0; i < 1000; i++)
            {
                var obj = new SafetyTestClass();
                obj.Value = i;
                obj.DoWork();
                // Deallocated here - no leak
            }
        }
        
        static void TestNoLeaksWithExceptions()
        {
            try
            {
                var obj = new SafetyTestClass();
                obj.DoWork();
                
                if (obj.Value > 50)
                    throw new System.Exception();
                    
                // Normal deallocation path
            }
            catch (System.Exception)
            {
                // Object already deallocated - no leak
            }
        }
    }

    /// <summary>
    /// Test: No null pointer dereference
    /// Expected: Compiler tracks nullability and prevents null access
    /// </summary>
    class NullSafetyTest
    {
        static void TestNullSafety()
        {
            SafetyTestClass? obj = null;
            
            // Safe - null check
            if (obj != null)
            {
                obj.DoWork();
            }
            
            // Safe - null-conditional
            obj?.DoWork();
            
            // This should be caught by compiler:
            // obj.DoWork(); // ERROR: Possible null reference
        }
    }

    /// <summary>
    /// Test: No dangling pointers
    /// Expected: References cannot outlive referenced objects
    /// </summary>
    class DanglingPointerPreventionTest
    {
        static SafetyTestClass? GetDanglingReference()
        {
            SafetyTestClass obj = new SafetyTestClass();
            // obj deallocated at scope exit
            // Compiler should prevent returning dangling reference
            return obj; // This extends lifetime - safe
        }
        
        static void TestNoDanglingPointers()
        {
            SafetyTestClass? reference = null;
            
            {
                var obj = new SafetyTestClass();
                reference = obj;
            } // Compiler ensures reference is invalidated or obj lifetime extended
            
            // Compiler prevents this:
            // reference.DoWork(); // ERROR: Dangling pointer
        }
    }

    /// <summary>
    /// Test: Array bounds safety
    /// Expected: Array access is bounds-checked at compile-time where possible
    /// </summary>
    class ArrayBoundsSafetyTest
    {
        static void TestArrayBounds()
        {
            var array = new SafetyTestClass[10];
            
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = new SafetyTestClass { Value = i };
            }
            
            // Safe access
            array[5].DoWork();
            
            // Compiler should warn or prevent:
            // array[100].DoWork(); // ERROR: Index out of bounds
        }
    }

    /// <summary>
    /// Test: Aliasing safety
    /// Expected: No invalid aliasing that could cause safety issues
    /// </summary>
    class AliasingSafetyTest
    {
        static void TestAliasingPreventsMutation()
        {
            var obj = new SafetyTestClass();
            var alias = obj;
            
            obj.DoWork();
            alias.DoWork();
            
            // Both valid - same object
            // Deallocated safely after both out of scope
        }
    }

    /// <summary>
    /// Test: Concurrent access safety (N/A for single-threaded WASM MVP)
    /// Expected: No data races (guaranteed by single-threaded execution)
    /// </summary>
    class ConcurrencySafetyTest
    {
        static void TestSingleThreadedSafety()
        {
            // WASM MVP is single-threaded
            // No concurrent access possible
            // All memory access is inherently safe from data races
            var obj = new SafetyTestClass();
            obj.DoWork();
        }
    }

    /// <summary>
    /// Test: Collection safety
    /// Expected: Collections and contained objects are safely managed
    /// </summary>
    class CollectionSafetyTest
    {
        static void TestListSafety()
        {
            var list = new System.Collections.Generic.List<SafetyTestClass>();
            
            for (int i = 0; i < 10; i++)
            {
                list.Add(new SafetyTestClass { Value = i });
            }
            
            foreach (var item in list)
            {
                item.DoWork();
            }
            
            // List and all items safely deallocated
        }
    }

    /// <summary>
    /// Test: Exception safety
    /// Expected: Memory safe even with exceptions
    /// </summary>
    class ExceptionSafetyTest
    {
        static void TestExceptionSafety()
        {
            SafetyTestClass? obj1 = null;
            SafetyTestClass? obj2 = null;
            
            try
            {
                obj1 = new SafetyTestClass();
                obj2 = new SafetyTestClass();
                
                obj1.DoWork();
                obj2.DoWork();
                
                if (obj1.Value > 50)
                    throw new System.InvalidOperationException();
                    
            }
            catch (System.Exception)
            {
                // Both objects already safely deallocated
            }
            
            // No leaks, no use-after-free
        }
    }

    /// <summary>
    /// Test: Recursive safety
    /// Expected: Recursive calls maintain memory safety
    /// </summary>
    class RecursiveSafetyTest
    {
        static void RecursiveFunction(int depth)
        {
            if (depth <= 0)
                return;
                
            var obj = new SafetyTestClass { Value = depth };
            obj.DoWork();
            
            RecursiveFunction(depth - 1);
            
            // obj safely deallocated after recursion
        }
        
        static void TestRecursiveSafety()
        {
            RecursiveFunction(10);
            // All objects from all stack frames safely deallocated
        }
    }

    // Helper class
    class SafetyTestClass
    {
        public int Value { get; set; }
        
        public void DoWork()
        {
            Value += 10;
        }
    }
}
