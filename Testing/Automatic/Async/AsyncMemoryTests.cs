// Async/Await Memory Tests for CRAB Automatic Memory Model (CTGC)
// Tests CTGC handling of async/await patterns

namespace CRAB.Testing.Automatic.Async
{
    /// <summary>
    /// Test: Simple async method with local allocation
    /// Expected: Objects managed across await boundaries
    /// </summary>
    class SimpleAsyncTest
    {
        static async System.Threading.Tasks.Task TestSimpleAsync()
        {
            var obj = new AsyncTestClass();
            obj.DoWork();
            
            await System.Threading.Tasks.Task.Delay(100);
            
            obj.Process();
            // obj deallocated here
        }
    }

    /// <summary>
    /// Test: Object allocated before await
    /// Expected: Lifetime extends across await
    /// </summary>
    class BeforeAwaitTest
    {
        static async System.Threading.Tasks.Task TestBeforeAwait()
        {
            var obj = new AsyncTestClass();
            obj.DoWork();
            
            await System.Threading.Tasks.Task.Delay(100);
            
            obj.Process(); // Must still be valid
            // Deallocated here
        }
    }

    /// <summary>
    /// Test: Object allocated after await
    /// Expected: Normal lifetime after await completes
    /// </summary>
    class AfterAwaitTest
    {
        static async System.Threading.Tasks.Task TestAfterAwait()
        {
            await System.Threading.Tasks.Task.Delay(100);
            
            var obj = new AsyncTestClass();
            obj.DoWork();
            obj.Process();
            // Deallocated here
        }
    }

    /// <summary>
    /// Test: Multiple awaits with shared object
    /// Expected: Object lifetime spans all awaits
    /// </summary>
    class MultipleAwaitsTest
    {
        static async System.Threading.Tasks.Task TestMultipleAwaits()
        {
            var obj = new AsyncTestClass();
            
            await System.Threading.Tasks.Task.Delay(100);
            obj.DoWork();
            
            await System.Threading.Tasks.Task.Delay(100);
            obj.Process();
            
            await System.Threading.Tasks.Task.Delay(100);
            obj.DoWork();
            
            // Deallocated here
        }
    }

    /// <summary>
    /// Test: Async method returning object
    /// Expected: Object lifetime extended to caller
    /// </summary>
    class AsyncReturnTest
    {
        static async System.Threading.Tasks.Task<AsyncTestClass> CreateAsync()
        {
            await System.Threading.Tasks.Task.Delay(100);
            
            var obj = new AsyncTestClass();
            obj.DoWork();
            
            return obj; // Lifetime extended
        }
        
        static async System.Threading.Tasks.Task UseAsyncReturn()
        {
            var obj = await CreateAsync();
            obj.Process();
            // obj deallocated here
        }
    }

    /// <summary>
    /// Test: Conditional await with allocations
    /// Expected: Correct lifetime management on all paths
    /// </summary>
    class ConditionalAsyncTest
    {
        static async System.Threading.Tasks.Task TestConditionalAsync(bool condition)
        {
            var obj = new AsyncTestClass();
            
            if (condition)
            {
                await System.Threading.Tasks.Task.Delay(100);
                obj.DoWork();
            }
            else
            {
                await System.Threading.Tasks.Task.Delay(50);
                obj.Process();
            }
            
            // obj deallocated here on both paths
        }
    }

    /// <summary>
    /// Test: Async loop with allocations
    /// Expected: Each iteration managed correctly
    /// </summary>
    class AsyncLoopTest
    {
        static async System.Threading.Tasks.Task TestAsyncLoop()
        {
            for (int i = 0; i < 10; i++)
            {
                var obj = new AsyncTestClass { Value = i };
                
                await System.Threading.Tasks.Task.Delay(10);
                
                obj.DoWork();
                // obj deallocated at end of iteration
            }
        }
    }

    /// <summary>
    /// Test: Nested async calls
    /// Expected: Correct lifetime management across call chain
    /// </summary>
    class NestedAsyncTest
    {
        static async System.Threading.Tasks.Task InnerAsync()
        {
            var inner = new AsyncTestClass();
            await System.Threading.Tasks.Task.Delay(50);
            inner.DoWork();
            // inner deallocated here
        }
        
        static async System.Threading.Tasks.Task OuterAsync()
        {
            var outer = new AsyncTestClass();
            
            await InnerAsync();
            
            outer.DoWork();
            // outer deallocated here
        }
    }

    /// <summary>
    /// Test: Async exception handling
    /// Expected: Objects deallocated even with exceptions
    /// </summary>
    class AsyncExceptionTest
    {
        static async System.Threading.Tasks.Task TestAsyncException()
        {
            var obj = new AsyncTestClass();
            
            try
            {
                await System.Threading.Tasks.Task.Delay(100);
                
                if (obj.Value > 50)
                    throw new System.InvalidOperationException();
                    
                obj.DoWork();
            }
            catch (System.Exception)
            {
                // obj already deallocated
            }
        }
    }

    /// <summary>
    /// Test: Task.WhenAll with multiple allocations
    /// Expected: All objects managed correctly
    /// </summary>
    class WhenAllTest
    {
        static async System.Threading.Tasks.Task TestWhenAll()
        {
            var obj1 = new AsyncTestClass();
            var obj2 = new AsyncTestClass();
            var obj3 = new AsyncTestClass();
            
            await System.Threading.Tasks.Task.WhenAll(
                ProcessAsync(obj1),
                ProcessAsync(obj2),
                ProcessAsync(obj3)
            );
            
            // All objects deallocated here
        }
        
        static async System.Threading.Tasks.Task ProcessAsync(AsyncTestClass obj)
        {
            await System.Threading.Tasks.Task.Delay(100);
            obj.DoWork();
        }
    }

    /// <summary>
    /// Test: Task.WhenAny with allocations
    /// Expected: Objects deallocated after WhenAny completes
    /// </summary>
    class WhenAnyTest
    {
        static async System.Threading.Tasks.Task TestWhenAny()
        {
            var obj1 = new AsyncTestClass();
            var obj2 = new AsyncTestClass();
            
            var completed = await System.Threading.Tasks.Task.WhenAny(
                ProcessAsync(obj1, 100),
                ProcessAsync(obj2, 200)
            );
            
            await completed; // Wait for the winner
            
            // Both objects deallocated here
        }
        
        static async System.Threading.Tasks.Task ProcessAsync(AsyncTestClass obj, int delay)
        {
            await System.Threading.Tasks.Task.Delay(delay);
            obj.DoWork();
        }
    }

    /// <summary>
    /// Test: Async state machine memory management
    /// Expected: State machine properly manages object lifetimes
    /// </summary>
    class AsyncStateMachineTest
    {
        static async System.Threading.Tasks.Task TestStateMachine()
        {
            var obj1 = new AsyncTestClass();
            
            await System.Threading.Tasks.Task.Delay(100);
            // State machine suspends here - obj1 must remain valid
            
            var obj2 = new AsyncTestClass();
            
            await System.Threading.Tasks.Task.Delay(100);
            // Both obj1 and obj2 must remain valid
            
            obj1.DoWork();
            obj2.DoWork();
            
            // Both deallocated here
        }
    }

    // Helper class
    class AsyncTestClass
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
}
