// Common Subexpression Elimination (CSE) Tests
// Verifies that redundant computations are eliminated

namespace OptimizationTest
{
    class CommonSubexpressionTest
    {
        // Test 1: Simple CSE - same expression computed twice
        static int SimpleCSE()
        {
            int x = 10;
            int y = 20;
            
            int a = x + y;  // First computation
            int b = x * 2;
            int c = x + y;  // Same as a - should reuse
            
            return a + b + c;
        }
        
        // Test 2: CSE in different scopes
        static int ScopedCSE()
        {
            int x = 10;
            int y = 20;
            
            int result = 0;
            
            {
                int temp = x + y;
                result += temp;
            }
            
            {
                int temp = x + y;  // Same computation - could be hoisted
                result += temp;
            }
            
            return result;
        }
        
        // Test 3: CSE with method calls (must be pure)
        static int MethodCallCSE()
        {
            var obj = new Calculator();
            
            int a = obj.Add(5, 3);   // First call
            int b = obj.Multiply(2, 4);
            int c = obj.Add(5, 3);   // Duplicate call - could be eliminated if pure
            
            return a + b + c;
        }
        
        // Test 4: CSE with array access
        static int ArrayAccessCSE()
        {
            var array = new int[] { 1, 2, 3, 4, 5 };
            int index = 2;
            
            int a = array[index] + array[index];  // array[index] computed twice
            int b = array[index] * 2;             // array[index] computed again
            
            return a + b;
        }
        
        // Test 5: CSE with field access
        static int FieldAccessCSE()
        {
            var obj = new MyClass();
            obj.value = 42;
            
            int a = obj.value + obj.value;  // obj.value read twice
            int b = obj.value * 2;          // obj.value read again
            
            return a + b;
        }
        
        // Test 6: CSE across basic blocks
        static int CrossBlockCSE(bool condition)
        {
            int x = 10;
            int y = 20;
            
            int result = x + y;  // First computation
            
            if (condition)
            {
                result += x + y;  // Same computation
            }
            else
            {
                result -= x + y;  // Same computation
            }
            
            return result;
        }
        
        // Test 7: CSE with complex expressions
        static int ComplexCSE()
        {
            int a = 5;
            int b = 10;
            int c = 15;
            
            int x = (a + b) * (c - a);  // First computation
            int y = (a + b) + c;        // (a + b) is common subexpression
            int z = (c - a) + b;        // (c - a) is common subexpression
            int w = (a + b) * (c - a);  // Entire expression is duplicate
            
            return x + y + z + w;
        }
        
        // Test 8: CSE with memory allocations (must preserve safety)
        static int AllocationCSE()
        {
            var obj1 = new MyClass();
            obj1.value = 10;
            
            var obj2 = new MyClass();
            obj2.value = 20;
            
            // These are NOT common subexpressions - different objects
            int a = obj1.value + obj2.value;
            int b = obj1.value + obj2.value;  // Same values but must re-read
            
            return a + b;
        }
        
        // Test 9: Loop invariant code motion (LICM)
        static int LoopInvariantCSE()
        {
            int x = 10;
            int y = 20;
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                // x + y is loop invariant - should be hoisted
                sum += (x + y) * i;
            }
            
            return sum;
        }
        
        // Test 10: CSE with side effects - must NOT be eliminated
        static int NoCSEWithSideEffects()
        {
            var obj = new MyClass();
            
            int a = obj.GetValueWithSideEffect();  // Has side effect
            int b = obj.GetValueWithSideEffect();  // Must NOT be eliminated
            
            return a + b;
        }
        
        // Test 11: CSE with reassignment
        static int ReassignmentInvalidatesCSE()
        {
            int x = 10;
            int y = 20;
            
            int a = x + y;  // First computation
            
            x = 30;  // Reassignment invalidates CSE
            
            int b = x + y;  // Different result - NOT a CSE
            
            return a + b;
        }
        
        // Test 12: CSE with nested expressions
        static int NestedCSE()
        {
            int a = 5;
            int b = 10;
            
            int x = (a + b) + ((a + b) * 2);  // (a + b) computed twice
            int y = (a + b) - 5;              // (a + b) computed again
            
            return x + y;
        }
        
        // Test 13: Address-taken variables - CSE must be careful
        static int AddressTakenCSE()
        {
            int x = 10;
            int y = 20;
            
            int result = x + y;
            
            // Simulate address-taken scenario
            ModifyByRef(ref x);
            
            // CSE invalid after address-taken variable modified
            int result2 = x + y;
            
            return result + result2;
        }
        
        static void ModifyByRef(ref int value)
        {
            value += 5;
        }
        
        // Test 14: CSE with ownership transfer
        static int OwnershipTransferCSE()
        {
            var obj1 = new MyClass();
            var obj2 = new MyClass();
            
            obj1.value = 10;
            obj2.value = 20;
            
            int a = obj1.value + obj2.value;
            
            // Transfer ownership (simplified)
            var obj3 = obj1;  // obj1 moved
            
            // Cannot CSE with obj1 after move
            int b = obj3.value + obj2.value;
            
            return a + b;
        }
    }
    
    class MyClass
    {
        public int value;
        
        public int GetValueWithSideEffect()
        {
            value++;  // Side effect - modifies state
            return value;
        }
    }
    
    class Calculator
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
        
        public int Multiply(int a, int b)
        {
            return a * b;
        }
    }
}
