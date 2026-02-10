// Test automatic memory model (CTGC) allocation and deallocation
// CRAB should infer lifetimes and insert deallocations automatically

namespace AutomaticTest
{
    class MemoryTest
    {
        static void TestAllocation()
        {
            // Single allocation - should be freed at end of scope
            var obj1 = new MyClass();
            obj1.DoWork();
            
            // Multiple allocations
            var obj2 = new MyClass();
            var obj3 = new MyClass();
            
            // Nested scopes
            {
                var obj4 = new MyClass();
                obj4.DoWork();
                // obj4 freed here
            }
            
            // obj1, obj2, obj3 freed here
        }
        
        static void TestConditional()
        {
            // Conditional allocations
            bool condition = true;
            
            if (condition)
            {
                var obj = new MyClass();
                obj.DoWork();
                // obj freed here
            }
        }
        
        static void TestLoop()
        {
            // Loop allocations
            for (int i = 0; i < 10; i++)
            {
                var obj = new MyClass();
                obj.DoWork();
                // obj freed at end of each iteration
            }
        }
    }
    
    class MyClass
    {
        private int value;
        
        public void DoWork()
        {
            value = 42;
        }
    }
}
