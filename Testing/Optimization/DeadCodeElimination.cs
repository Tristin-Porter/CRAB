// Dead Code Elimination Tests
// Verifies that unreachable code is eliminated while preserving side effects and memory safety

namespace OptimizationTest
{
    class DeadCodeEliminationTest
    {
        // Test 1: Simple unreachable code after return
        static int UnreachableAfterReturn()
        {
            var obj = new MyClass();
            obj.DoWork();
            return 42;
            
            // Dead code - should be eliminated
            var dead = new MyClass();
            dead.DoWork();
            return 0;
        }
        
        // Test 2: Unreachable branch - constant condition
        static void UnreachableBranch()
        {
            var obj = new MyClass();
            
            if (true)
            {
                obj.DoWork();
            }
            else
            {
                // Dead code - should be eliminated
                var dead = new MyClass();
                dead.DoWork();
            }
        }
        
        // Test 3: Dead code in loop with constant false condition
        static void UnreachableLoop()
        {
            var obj = new MyClass();
            obj.DoWork();
            
            // Dead loop - should be eliminated
            while (false)
            {
                var dead = new MyClass();
                dead.DoWork();
            }
        }
        
        // Test 4: Unused variable that's never read
        static void UnusedVariable()
        {
            var used = new MyClass();
            used.DoWork();
            int result = used.GetValue();
            
            // Dead variable - should be eliminated if no side effects
            var unused = new MyClass();
            
            // Use result to prevent it from being optimized away
            if (result > 0)
            {
                used.DoWork();
            }
        }
        
        // Test 5: Dead code with side effects must be preserved
        static void DeadCodeWithSideEffects()
        {
            var obj = new MyClass();
            
            if (true)
            {
                obj.DoWork();
                return;
            }
            
            // Dead but has side effect - deallocation must still occur
            var obj2 = new MyClass();
            obj2.DoWorkWithSideEffect();  // Side effect must be preserved
        }
        
        // Test 6: Complex control flow
        static int ComplexControlFlow(bool condition)
        {
            var obj = new MyClass();
            
            if (condition)
            {
                obj.DoWork();
                return 1;
            }
            else
            {
                obj.DoWork();
                return 2;
            }
            
            // Dead code after all branches return
            var dead = new MyClass();
            return 0;
        }
        
        // Test 7: Switch with all paths returning
        static int SwitchAllReturn(int value)
        {
            var obj = new MyClass();
            
            switch (value)
            {
                case 0:
                    obj.DoWork();
                    return 0;
                case 1:
                    obj.DoWork();
                    return 1;
                default:
                    obj.DoWork();
                    return -1;
            }
            
            // Dead code - all switch paths return
            var dead = new MyClass();
            return 99;
        }
        
        // Test 8: Empty blocks should be removed
        static void EmptyBlocks()
        {
            var obj = new MyClass();
            obj.DoWork();
            
            // Empty block
            {
            }
            
            // Block with only dead code
            {
                if (false)
                {
                    var dead = new MyClass();
                }
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
        
        public int GetValue()
        {
            return value;
        }
        
        public void DoWorkWithSideEffect()
        {
            // Simulate side effect like I/O or global state mutation
            value = 100;
            System.Console.WriteLine("Side effect occurred");
        }
    }
}
