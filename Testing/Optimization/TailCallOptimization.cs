// Tail Call Optimization Tests
// Verifies that tail calls are converted to jumps to avoid stack overflow

namespace OptimizationTest
{
    class TailCallOptimizationTest
    {
        // Test 1: Simple tail recursion
        static int SimpleTailRecursion(int n, int acc)
        {
            if (n <= 0)
                return acc;
            
            return SimpleTailRecursion(n - 1, acc + n);  // Tail call
        }
        
        // Test 2: Factorial with tail recursion
        static int FactorialTail(int n, int acc)
        {
            if (n <= 1)
                return acc;
            
            return FactorialTail(n - 1, n * acc);  // Tail call
        }
        
        // Test 3: Fibonacci with tail recursion (using accumulator pattern)
        static int FibonacciTail(int n, int a, int b)
        {
            if (n == 0)
                return a;
            if (n == 1)
                return b;
            
            return FibonacciTail(n - 1, b, a + b);  // Tail call
        }
        
        // Test 4: Sum list with tail recursion
        static int SumListTail(int[] array, int index, int acc)
        {
            if (index >= array.Length)
                return acc;
            
            return SumListTail(array, index + 1, acc + array[index]);  // Tail call
        }
        
        // Test 5: Mutual tail recursion - even/odd
        static bool IsEven(int n)
        {
            if (n == 0)
                return true;
            
            return IsOdd(n - 1);  // Tail call to another function
        }
        
        static bool IsOdd(int n)
        {
            if (n == 0)
                return false;
            
            return IsEven(n - 1);  // Tail call to another function
        }
        
        // Test 6: NOT a tail call - result is modified
        static int NotTailCall(int n)
        {
            if (n <= 0)
                return 1;
            
            return n * NotTailCall(n - 1);  // NOT tail call - multiplication after
        }
        
        // Test 7: NOT a tail call - additional computation
        static int NotTailCall2(int n)
        {
            if (n <= 0)
                return 0;
            
            return NotTailCall2(n - 1) + n;  // NOT tail call - addition after
        }
        
        // Test 8: Tail call with multiple parameters
        static int MultiParam(int a, int b, int c)
        {
            if (a <= 0)
                return b + c;
            
            return MultiParam(a - 1, b + 1, c + 2);  // Tail call
        }
        
        // Test 9: Conditional tail call
        static int ConditionalTail(int n, bool flag)
        {
            if (n <= 0)
                return 0;
            
            if (flag)
                return ConditionalTail(n - 1, !flag);  // Tail call
            else
                return ConditionalTail(n - 2, !flag);  // Tail call
        }
        
        // Test 10: Tail call in else branch
        static int ElseBranchTail(int n)
        {
            if (n <= 0)
            {
                return 0;
            }
            else
            {
                return ElseBranchTail(n - 1);  // Tail call in else
            }
        }
        
        // Test 11: Tail call with memory allocation
        static MyClass TailWithAllocation(int n)
        {
            if (n <= 0)
                return new MyClass();
            
            // Note: This is complex - optimization must preserve CTGC
            return TailWithAllocation(n - 1);  // Tail call, returns object
        }
        
        // Test 12: Tail call with ownership transfer
        static void TailWithOwnership(MyClass obj, int n)
        {
            if (n <= 0)
            {
                obj.DoWork();
                return;
            }
            
            TailWithOwnership(obj, n - 1);  // Tail call, transfers ownership
        }
        
        // Test 13: Loop converted to tail recursion (or vice versa)
        static int LoopAsTailRecursion(int n)
        {
            return LoopHelper(n, 0);
        }
        
        static int LoopHelper(int n, int acc)
        {
            if (n <= 0)
                return acc;
            
            return LoopHelper(n - 1, acc + n);  // Could be optimized to loop
        }
        
        // Test 14: Tail call with try-catch - must be careful
        static int TailWithException(int n)
        {
            if (n <= 0)
                return 0;
            
            try
            {
                return TailWithException(n - 1);  // Tail call in try block
            }
            catch
            {
                return -1;
            }
        }
        
        // Test 15: Tail call with multiple exit points
        static int MultipleExits(int n)
        {
            if (n <= 0)
                return 0;
            
            if (n == 1)
                return 1;
            
            if (n % 2 == 0)
                return MultipleExits(n / 2);  // Tail call
            else
                return MultipleExits(n * 3 + 1);  // Tail call (Collatz sequence)
        }
        
        // Test 16: Tail call elimination with large recursion depth
        static int DeepRecursion(int n)
        {
            return DeepRecursionHelper(n, 0);
        }
        
        static int DeepRecursionHelper(int n, int acc)
        {
            if (n <= 0)
                return acc;
            
            // Without TCO, this would stack overflow for large n
            return DeepRecursionHelper(n - 1, acc + 1);
        }
        
        // Test 17: Tail call with generic types
        static T GenericTail<T>(T value, int n)
        {
            if (n <= 0)
                return value;
            
            return GenericTail(value, n - 1);  // Tail call with generic
        }
        
        // Test 18: State machine from tail recursion
        static int StateMachine(int state, int counter)
        {
            switch (state)
            {
                case 0:
                    if (counter > 100)
                        return counter;
                    return StateMachine(1, counter + 1);  // Tail call
                    
                case 1:
                    if (counter > 200)
                        return counter;
                    return StateMachine(2, counter + 2);  // Tail call
                    
                case 2:
                    if (counter > 300)
                        return counter;
                    return StateMachine(0, counter + 3);  // Tail call
                    
                default:
                    return counter;
            }
        }
        
        // Test 19: Tail call with ref parameters
        static void TailWithRef(ref int value, int n)
        {
            if (n <= 0)
                return;
            
            value += n;
            TailWithRef(ref value, n - 1);  // Tail call with ref
        }
        
        // Test 20: Continuation-passing style
        static int CPS(int n, Func<int, int> continuation)
        {
            if (n <= 0)
                return continuation(0);
            
            return CPS(n - 1, x => continuation(x + n));  // Tail call
        }
    }
    
    class MyClass
    {
        public int value;
        
        public void DoWork()
        {
            value = 42;
        }
    }
}
