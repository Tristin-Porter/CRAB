// Memory Safety Preservation Tests
// Critical tests ensuring all optimizations preserve CTGC guarantees and ownership semantics

namespace OptimizationTest
{
    class MemorySafetyPreservationTest
    {
        // Test 1: CTGC deallocation points must not be optimized away
        static void PreserveDeallocations()
        {
            var obj1 = new MyClass();
            obj1.DoWork();
            // CTGC should insert deallocation here - must not be eliminated
            
            var obj2 = new MyClass();
            obj2.DoWork();
            // CTGC should insert deallocation here
        }
        
        // Test 2: Ownership transfer semantics must be preserved
        static void PreserveOwnership()
        {
            var obj = new MyClass();
            obj.value = 10;
            
            TransferOwnership(obj);  // obj moved
            
            // obj is moved - cannot access here
            // var x = obj.value;  // Would be error
        }
        
        static void TransferOwnership(MyClass obj)
        {
            obj.DoWork();
            // obj deallocated here
        }
        
        // Test 3: Lifetime inference must remain correct after optimization
        static MyClass PreserveLifetimes()
        {
            var obj = new MyClass();
            obj.value = 42;
            
            if (SomeCondition())
            {
                return obj;  // Lifetime extends to caller
            }
            
            obj.DoWork();
            return obj;
        }
        
        static bool SomeCondition()
        {
            return true;
        }
        
        // Test 4: Region boundaries must be respected
        static void PreserveRegions()
        {
            // Region 1
            {
                var obj1 = new MyClass();
                obj1.DoWork();
                // obj1 deallocated at end of region
            }
            
            // Region 2 - separate from region 1
            {
                var obj2 = new MyClass();
                obj2.DoWork();
                // obj2 deallocated at end of region
            }
        }
        
        // Test 5: Escape analysis results must be preserved
        static void PreserveEscapeAnalysis()
        {
            var local = new MyClass();  // Does not escape
            local.value = 10;
            
            var escaping = new MyClass();  // Escapes via return
            GlobalStore(escaping);
        }
        
        static MyClass globalObj;
        
        static void GlobalStore(MyClass obj)
        {
            globalObj = obj;  // Escapes to global
        }
        
        // Test 6: Use-after-free must never be introduced
        static void NoUseAfterFree()
        {
            var obj = new MyClass();
            obj.value = 10;
            
            int value1 = obj.value;  // OK
            
            // obj lifetime ends here
            TransferOwnership(obj);
            
            // Must not optimize to reuse obj here - would be use-after-free
            // int value2 = obj.value;  // Error
        }
        
        // Test 7: Double-free must never be introduced
        static void NoDoubleFree()
        {
            var obj = new MyClass();
            obj.value = 10;
            
            // Ownership transferred once
            TransferOwnership(obj);
            
            // Must not insert another deallocation here
            // TransferOwnership(obj);  // Would be double-free
        }
        
        // Test 8: Model isolation (automatic ↔ manual) must be preserved
        static void PreserveModelIsolation()
        {
            // Automatic memory model
            var autoObj = new MyClass();
            autoObj.DoWork();
            
            // If this crosses to manual model, boundary must be preserved
            // ManualModel.UseObject(autoObj);
        }
        
        // Test 9: Async memory tracking must be preserved
        static async Task PreserveAsyncMemory()
        {
            var obj = new MyClass();
            obj.value = 10;
            
            await SomeAsyncOperation();
            
            // obj lifetime must span the await
            obj.DoWork();
        }
        
        static async Task SomeAsyncOperation()
        {
            await Task.Delay(100);
        }
        
        // Test 10: LINQ optimizations must preserve memory safety
        static void PreserveLINQSafety()
        {
            var objects = new MyClass[] 
            {
                new MyClass(),
                new MyClass(),
                new MyClass()
            };
            
            // LINQ operations must not violate ownership
            var values = objects.Select(obj => obj.value).ToArray();
            
            // Original objects still valid
            foreach (var obj in objects)
            {
                obj.DoWork();
            }
        }
        
        // Test 11: Exception handling must preserve cleanup
        static void PreserveExceptionCleanup()
        {
            var obj = new MyClass();
            
            try
            {
                obj.DoWork();
                ThrowException();
            }
            catch
            {
                // obj must still be valid and properly cleaned up
                obj.value = 0;
            }
            // obj deallocated here
        }
        
        static void ThrowException()
        {
            throw new Exception("Test");
        }
        
        // Test 12: Inlining must preserve lifetime constraints
        static void PreserveInlineLifetimes()
        {
            var obj = CreateObject();  // Inlining must preserve ownership transfer
            obj.DoWork();
        }
        
        static MyClass CreateObject()
        {
            var obj = new MyClass();
            obj.value = 42;
            return obj;  // Ownership transferred
        }
        
        // Test 13: Loop optimizations must preserve per-iteration allocations
        static void PreserveLoopAllocations()
        {
            for (int i = 0; i < 10; i++)
            {
                var obj = new MyClass();  // New allocation each iteration
                obj.value = i;
                obj.DoWork();
                // obj deallocated at end of each iteration
            }
        }
        
        // Test 14: CSE must not violate ownership
        static void PreserveOwnershipInCSE()
        {
            var obj1 = new MyClass();
            var obj2 = new MyClass();
            
            obj1.value = 10;
            obj2.value = 10;
            
            // Even though values are same, these are different objects
            int a = obj1.value;
            int b = obj2.value;
            
            // Cannot CSE the objects themselves
        }
        
        // Test 15: Dead code elimination must preserve deallocation
        static void PreserveDeallocationInDeadCode()
        {
            var obj = new MyClass();
            obj.value = 10;
            
            if (false)
            {
                // Dead code, but if obj was allocated here,
                // deallocation must still be considered
                var dead = new MyClass();
            }
        }
        
        // Test 16: Constant folding must not skip side effects
        static void PreserveSideEffects()
        {
            var obj = new MyClass();
            
            // Even if condition is constant, side effects must execute
            if (true)
            {
                obj.DoWorkWithSideEffect();
            }
        }
        
        // Test 17: Tail call optimization must preserve ownership
        static void PreserveTailCallOwnership(MyClass obj, int n)
        {
            if (n <= 0)
            {
                obj.DoWork();
                return;
            }
            
            // Tail call must properly transfer ownership
            PreserveTailCallOwnership(obj, n - 1);
        }
        
        // Test 18: Vectorization must preserve memory semantics
        static void PreserveVectorizationSemantics()
        {
            var objects = new MyClass[100];
            for (int i = 0; i < 100; i++)
            {
                objects[i] = new MyClass();
            }
            
            // If vectorized, must maintain individual object lifetimes
            for (int i = 0; i < 100; i++)
            {
                objects[i].value = i;
            }
        }
        
        // Test 19: Strength reduction must not change semantics
        static void PreserveStrengthReductionSemantics()
        {
            var obj = new MyClass();
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                // Strength reduction i * 2 -> i + i must preserve behavior
                sum += i * 2;
            }
            
            obj.value = sum;
        }
        
        // Test 20: All optimizations combined must preserve safety
        static int CombinedOptimizationsSafety(int n)
        {
            // Dead code elimination
            if (false)
            {
                var dead = new MyClass();
            }
            
            // Constant folding
            int constant = 2 + 3;
            
            // CSE
            int cse1 = constant * 2;
            int cse2 = constant * 2;
            
            // Inlining
            int inlined = AddNumbers(cse1, cse2);
            
            // Loop optimization
            int sum = 0;
            for (int i = 0; i < n; i++)
            {
                sum += i;
            }
            
            // Tail call
            return TailRecursive(sum, 0);
        }
        
        static int AddNumbers(int a, int b)
        {
            return a + b;
        }
        
        static int TailRecursive(int n, int acc)
        {
            if (n <= 0)
                return acc;
            return TailRecursive(n - 1, acc + n);
        }
    }
    
    class MyClass
    {
        public int value;
        
        public void DoWork()
        {
            value = 42;
        }
        
        public void DoWorkWithSideEffect()
        {
            System.Console.WriteLine("Side effect");
            value = 100;
        }
    }
}
