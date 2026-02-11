// Function Inlining Tests
// Verifies that functions are inlined while preserving lifetime constraints and memory safety

namespace OptimizationTest
{
    class InliningTest
    {
        // Test 1: Simple inline candidate - small pure function
        static int SimpleInline()
        {
            int x = 10;
            int y = 20;
            
            int result = AddNumbers(x, y);  // Should be inlined
            return result;
        }
        
        static int AddNumbers(int a, int b)
        {
            return a + b;
        }
        
        // Test 2: Multiple calls to same function
        static int MultipleCallsInline()
        {
            int a = AddNumbers(5, 3);
            int b = AddNumbers(10, 20);
            int c = AddNumbers(a, b);
            
            return c;
        }
        
        // Test 3: Nested function calls
        static int NestedInline()
        {
            int result = Outer(10);
            return result;
        }
        
        static int Outer(int x)
        {
            return Inner(x) + Inner(x * 2);  // Inner should be inlined
        }
        
        static int Inner(int x)
        {
            return x * x;
        }
        
        // Test 4: Inline with local variables
        static int InlineWithLocals()
        {
            int result = ComputeWithLocals(5);
            return result;
        }
        
        static int ComputeWithLocals(int x)
        {
            int temp1 = x * 2;
            int temp2 = temp1 + 5;
            int temp3 = temp2 * temp1;
            return temp3;
        }
        
        // Test 5: Inline with memory allocation - must preserve CTGC
        static int InlineWithAllocation()
        {
            var obj = CreateAndUse();  // Allocation must be properly tracked
            return obj.GetValue();
        }
        
        static MyClass CreateAndUse()
        {
            var obj = new MyClass();
            obj.value = 42;
            return obj;  // Ownership transferred
        }
        
        // Test 6: Inline with conditional
        static int InlineWithConditional(bool flag)
        {
            int result = ConditionalCompute(flag, 10);
            return result;
        }
        
        static int ConditionalCompute(bool flag, int x)
        {
            if (flag)
            {
                return x * 2;
            }
            else
            {
                return x + 5;
            }
        }
        
        // Test 7: Do NOT inline recursive functions
        static int NoInlineRecursive(int n)
        {
            if (n <= 1)
                return 1;
            return n * NoInlineRecursive(n - 1);  // Recursion - should NOT inline
        }
        
        // Test 8: Do NOT inline large functions
        static int NoInlineLarge()
        {
            return LargeFunction(10);
        }
        
        static int LargeFunction(int x)
        {
            // Large function - might not be worth inlining
            int a = x * 2;
            int b = a + 5;
            int c = b * 3;
            int d = c - 7;
            int e = d * 4;
            int f = e + 9;
            int g = f * 5;
            int h = g - 11;
            int i = h * 6;
            int j = i + 13;
            int k = j * 7;
            int l = k - 15;
            return l;
        }
        
        // Test 9: Inline with loop - must preserve semantics
        static int InlineWithLoop(int n)
        {
            return SumToN(n);
        }
        
        static int SumToN(int n)
        {
            int sum = 0;
            for (int i = 1; i <= n; i++)
            {
                sum += i;
            }
            return sum;
        }
        
        // Test 10: Inline with multiple return points
        static int InlineMultipleReturns(int x)
        {
            return ComputeWithMultipleReturns(x);
        }
        
        static int ComputeWithMultipleReturns(int x)
        {
            if (x < 0)
                return -1;
            if (x == 0)
                return 0;
            if (x < 10)
                return x * 2;
            return x * 3;
        }
        
        // Test 11: Inline with ownership transfer
        static void InlineWithOwnership()
        {
            var obj = new MyClass();
            TransferOwnership(obj);  // obj moved - must track properly
        }
        
        static void TransferOwnership(MyClass obj)
        {
            obj.value = 100;
            // obj deallocated here in callee
        }
        
        // Test 12: Do NOT inline functions with side effects
        static int NoInlineSideEffects()
        {
            return FunctionWithSideEffect(10);
        }
        
        static int FunctionWithSideEffect(int x)
        {
            System.Console.WriteLine("Side effect");  // I/O side effect
            return x * 2;
        }
        
        // Test 13: Inline getter/setter
        static int InlineGetterSetter()
        {
            var obj = new MyClass();
            obj.SetValue(42);  // Should inline
            return obj.GetValue();  // Should inline
        }
        
        // Test 14: Inline with generic types
        static T InlineGeneric<T>(T value)
        {
            return Identity(value);  // Should inline
        }
        
        static T Identity<T>(T value)
        {
            return value;
        }
        
        // Test 15: Cross-boundary inline (automatic/manual model)
        static int CrossBoundaryInline()
        {
            var obj = new MyClass();
            int result = obj.GetValue();
            
            // If GetValue is in manual model, inlining must preserve boundaries
            return result * 2;
        }
        
        // Test 16: Inline with exception handling
        static int InlineWithTryCatch(int x)
        {
            return SafeDivide(100, x);
        }
        
        static int SafeDivide(int a, int b)
        {
            try
            {
                return a / b;
            }
            catch
            {
                return 0;
            }
        }
        
        // Test 17: Inline with ref parameters
        static void InlineWithRef()
        {
            int x = 10;
            IncrementByRef(ref x);
        }
        
        static void IncrementByRef(ref int value)
        {
            value++;
        }
        
        // Test 18: Inline with out parameters
        static int InlineWithOut()
        {
            int result;
            TryCompute(10, out result);
            return result;
        }
        
        static bool TryCompute(int x, out int result)
        {
            result = x * 2;
            return true;
        }
    }
    
    class MyClass
    {
        public int value;
        
        public void SetValue(int v)
        {
            value = v;
        }
        
        public int GetValue()
        {
            return value;
        }
    }
}
