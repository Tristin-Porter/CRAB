// Constant Folding and Propagation Tests
// Verifies that constant expressions are evaluated at compile-time

namespace OptimizationTest
{
    class ConstantFoldingTest
    {
        // Test 1: Simple arithmetic constant folding
        static int ArithmeticFolding()
        {
            // All of these should be folded to constants
            int a = 2 + 3;           // -> 5
            int b = 10 - 4;          // -> 6
            int c = 5 * 6;           // -> 30
            int d = 100 / 10;        // -> 10
            int e = 17 % 5;          // -> 2
            
            return a + b + c + d + e;  // -> 53
        }
        
        // Test 2: Complex nested expressions
        static int ComplexExpressions()
        {
            int result = ((2 + 3) * (4 + 5)) - (10 / 2);  // -> 40
            return result;
        }
        
        // Test 3: Boolean constant folding
        static bool BooleanFolding()
        {
            bool a = true && true;    // -> true
            bool b = true || false;   // -> true
            bool c = false && true;   // -> false
            bool d = !false;          // -> true
            
            return a && b && d && !c;  // -> true
        }
        
        // Test 4: Comparison folding
        static bool ComparisonFolding()
        {
            bool a = 5 > 3;           // -> true
            bool b = 10 < 20;         // -> true
            bool c = 7 == 7;          // -> true
            bool d = 4 != 5;          // -> true
            bool e = 15 >= 15;        // -> true
            bool f = 8 <= 10;         // -> true
            
            return a && b && c && d && e && f;  // -> true
        }
        
        // Test 5: Constant propagation
        static int ConstantPropagation()
        {
            int x = 10;
            int y = 20;
            int z = x + y;  // x and y are constants, so z = 30
            
            return z * 2;   // 30 * 2 = 60
        }
        
        // Test 6: String constant folding
        static string StringFolding()
        {
            string s1 = "Hello" + " " + "World";  // -> "Hello World"
            return s1;
        }
        
        // Test 7: Mixed types with casting
        static double TypeCasting()
        {
            double d = (double)(5 + 3) / 2;  // -> 4.0
            return d;
        }
        
        // Test 8: Conditional with constant condition
        static int ConditionalFolding()
        {
            var obj = new MyClass();
            
            // Constant condition - one branch should be eliminated
            int result;
            if (true)
            {
                result = 42;
                obj.DoWork();
            }
            else
            {
                result = 0;  // Dead code
                obj.DoWork();
            }
            
            return result;  // -> 42
        }
        
        // Test 9: Loop with constant iterations
        static int LoopUnrolling()
        {
            int sum = 0;
            
            // Constant iteration count - could be unrolled
            for (int i = 0; i < 5; i++)
            {
                sum += i;  // 0 + 1 + 2 + 3 + 4 = 10
            }
            
            return sum;
        }
        
        // Test 10: Bitwise operations
        static int BitwiseOperations()
        {
            int a = 0xFF & 0x0F;      // -> 0x0F
            int b = 0xF0 | 0x0F;      // -> 0xFF
            int c = 0xFF ^ 0xAA;      // -> 0x55
            int d = ~0;               // -> -1
            int e = 1 << 3;           // -> 8
            int f = 16 >> 2;          // -> 4
            
            return a + b + c + e + f;  // Folded to constant
        }
        
        // Test 11: Constant folding with memory allocation (must preserve CTGC)
        static int ConstantWithAllocation()
        {
            const int SIZE = 10 + 5;  // -> 15
            var array = new int[SIZE];
            
            // SIZE is constant, allocation size is known
            for (int i = 0; i < SIZE; i++)
            {
                array[i] = i;
            }
            
            return array[SIZE - 1];  // -> 14
        }
        
        // Test 12: Short-circuit evaluation
        static bool ShortCircuit()
        {
            bool result = true || ExpensiveCall();  // ExpensiveCall eliminated
            return result;
        }
        
        static bool ExpensiveCall()
        {
            // This should be eliminated by short-circuit
            var obj = new MyClass();
            obj.DoWork();
            return false;
        }
        
        // Test 13: Algebraic simplification
        static int AlgebraicSimplification()
        {
            int x = 10;
            int a = x * 1;        // -> x
            int b = x + 0;        // -> x
            int c = x - 0;        // -> x
            int d = x * 0;        // -> 0
            int e = 0 + x;        // -> x
            
            return a + b + c + e;  // d eliminated as it's 0
        }
        
        // Test 14: Strength reduction
        static int StrengthReduction()
        {
            int x = 10;
            int a = x * 2;    // Could be x + x or x << 1
            int b = x * 4;    // Could be x << 2
            int c = x / 2;    // Could be x >> 1
            
            return a + b + c;
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
