// Integration Tests for Optimizations
// End-to-end tests combining multiple optimization techniques

namespace OptimizationTest
{
    class IntegrationTest
    {
        // Test 1: Real-world computation with all optimizations
        static int ComplexComputation(int n)
        {
            // Constants - should be folded
            const int MULTIPLIER = 10;
            const int OFFSET = 5;
            
            int result = 0;
            
            // Loop with invariant code motion, CSE, and strength reduction
            for (int i = 0; i < n; i++)
            {
                // Loop invariant - should be hoisted
                int invariant = MULTIPLIER * OFFSET;
                
                // CSE - computed multiple times
                int common = (i + invariant) * 2;
                
                result += common;
                result += common;  // Duplicate - should use CSE
                
                // Dead code
                if (false)
                {
                    var dead = new MyClass();
                }
            }
            
            return result;
        }
        
        // Test 2: Recursive computation with tail call and inlining
        static int RecursiveComputation(int n)
        {
            return RecursiveHelper(n, 0);
        }
        
        static int RecursiveHelper(int n, int acc)
        {
            // Constant folding in condition
            if (n <= 0 + 0)
                return acc;
            
            // Inline candidate
            int value = ComputeValue(n);
            
            // Tail call
            return RecursiveHelper(n - 1, acc + value);
        }
        
        static int ComputeValue(int x)
        {
            // Simple function - inline candidate
            return x * 2 + 1;
        }
        
        // Test 3: Data structure operations with memory safety
        static int DataStructureOptimization()
        {
            var list = new List<MyClass>();
            
            // Loop unrolling candidate
            for (int i = 0; i < 10; i++)
            {
                var obj = new MyClass();
                obj.value = i;
                list.Add(obj);
            }
            
            int sum = 0;
            
            // LINQ optimization
            foreach (var obj in list)
            {
                // CSE - obj.value accessed multiple times
                sum += obj.value + obj.value;
            }
            
            return sum;
        }
        
        // Test 4: Nested loops with multiple optimizations
        static int NestedLoopOptimization()
        {
            const int SIZE = 10;
            int sum = 0;
            
            for (int i = 0; i < SIZE; i++)
            {
                // Outer loop invariant
                int outer_inv = SIZE * 2;
                
                for (int j = 0; j < SIZE; j++)
                {
                    // Inner loop invariant
                    int inner_inv = SIZE * 3;
                    
                    // CSE
                    int common = (i + j) * (i + j);
                    
                    sum += outer_inv + inner_inv + common;
                }
            }
            
            return sum;
        }
        
        // Test 5: Exception handling with optimizations
        static int ExceptionHandlingOptimization()
        {
            var obj = new MyClass();
            int result = 0;
            
            try
            {
                // Constant folding
                int constant = 10 + 20;
                
                // CSE
                int cse = constant * 2;
                result += cse + cse;
                
                // Conditional with constant condition
                if (true)
                {
                    obj.DoWork();
                }
                
                // Dead code
                if (false)
                {
                    throw new Exception("Never thrown");
                }
            }
            catch
            {
                result = -1;
            }
            finally
            {
                // Cleanup preserved
                obj.value = 0;
            }
            
            return result;
        }
        
        // Test 6: Conditional branches with optimizations
        static int ConditionalOptimization(bool flag)
        {
            var obj = new MyClass();
            int result = 0;
            
            // Constant folding in conditions
            if (2 + 2 == 4)
            {
                result += 10;
            }
            
            // CSE across branches
            int common = obj.value * 2;
            
            if (flag)
            {
                result += common;
            }
            else
            {
                result += common;  // Same computation
            }
            
            // Dead branch
            if (false)
            {
                var dead = new MyClass();
                result = -1;
            }
            
            return result;
        }
        
        // Test 7: Array operations with optimizations
        static int ArrayOptimization()
        {
            var array = new int[100];
            
            // Loop fusion candidate
            for (int i = 0; i < 100; i++)
            {
                array[i] = i;
            }
            
            for (int i = 0; i < 100; i++)
            {
                array[i] *= 2;
            }
            
            int sum = 0;
            
            // Bounds check elimination
            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }
            
            return sum;
        }
        
        // Test 8: Object-oriented code with optimizations
        static int OOPOptimization()
        {
            var calculator = new Calculator();
            
            // Inline candidates
            int a = calculator.Add(5, 3);
            int b = calculator.Multiply(2, 4);
            int c = calculator.Add(5, 3);  // CSE with pure method
            
            // Constant folding
            int constant = 10 + 20;
            
            // Strength reduction
            int doubled = constant * 2;
            
            return a + b + c + doubled;
        }
        
        // Test 9: State machine with tail calls
        static int StateMachineOptimization(int state, int counter)
        {
            // Tail call optimization for state machine
            switch (state)
            {
                case 0:
                    if (counter > 100)
                        return counter;
                    return StateMachineOptimization(1, counter + 1);
                    
                case 1:
                    if (counter > 200)
                        return counter;
                    return StateMachineOptimization(2, counter + 2);
                    
                case 2:
                    if (counter > 300)
                        return counter;
                    return StateMachineOptimization(0, counter + 3);
                    
                default:
                    return counter;
            }
        }
        
        // Test 10: Pipeline of transformations
        static int PipelineOptimization(int input)
        {
            // Constant folding
            int step1 = input + (5 + 10);
            
            // CSE
            int step2 = (step1 * 2) + (step1 * 2);
            
            // Inlining
            int step3 = Transform(step2);
            
            // Loop optimization
            int step4 = 0;
            for (int i = 0; i < 10; i++)
            {
                step4 += step3;  // Loop invariant
            }
            
            return step4;
        }
        
        static int Transform(int x)
        {
            return x + 100;  // Inline candidate
        }
        
        // Test 11: Memory-intensive with safety preservation
        static void MemoryIntensiveOptimization()
        {
            var objects = new List<MyClass>();
            
            // Allocations in loop
            for (int i = 0; i < 100; i++)
            {
                var obj = new MyClass();
                obj.value = i;
                objects.Add(obj);
                
                // Dead code
                if (false)
                {
                    var dead = new MyClass();
                }
            }
            
            // Process objects
            foreach (var obj in objects)
            {
                // Inline candidate
                ProcessObject(obj);
            }
            
            // Cleanup must be preserved
        }
        
        static void ProcessObject(MyClass obj)
        {
            obj.value *= 2;
        }
        
        // Test 12: Generic methods with optimizations
        static T GenericOptimization<T>(T value, int n)
        {
            // Constant folding
            int iterations = 5 + 5;
            
            T result = value;
            
            // Loop unrolling candidate
            for (int i = 0; i < iterations; i++)
            {
                result = Identity(result);  // Inline candidate
            }
            
            return result;
        }
        
        static T Identity<T>(T value)
        {
            return value;
        }
        
        // Test 13: Functional-style code with optimizations
        static int FunctionalStyleOptimization()
        {
            var numbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            // LINQ pipeline - various optimizations apply
            var result = numbers
                .Where(x => x % 2 == 0)     // Filter
                .Select(x => x * 2)         // Map - could be fused
                .Sum();                      // Reduce
            
            return result;
        }
        
        // Test 14: Complex control flow with optimizations
        static int ComplexControlFlow(int a, int b, int c)
        {
            int result = 0;
            
            // Constant conditions
            if (true && (a > 0))
            {
                result += a;
            }
            
            if (false || (b > 0))
            {
                result += b;
            }
            
            // CSE in different branches
            int common = a + b;
            
            if (c > 0)
            {
                result += common;
            }
            else
            {
                result -= common;
            }
            
            // Loop with break
            for (int i = 0; i < 100; i++)
            {
                result += i;
                if (result > 1000)
                    break;
            }
            
            return result;
        }
        
        // Test 15: Real-world algorithm - Fibonacci with memoization
        static int FibonacciOptimized(int n)
        {
            if (n <= 1)
                return n;
            
            var memo = new int[n + 1];
            memo[0] = 0;
            memo[1] = 1;
            
            for (int i = 2; i <= n; i++)
            {
                // CSE - memo[i-1] and memo[i-2]
                memo[i] = memo[i - 1] + memo[i - 2];
            }
            
            return memo[n];
        }
        
        // Test 16: All optimizations combined in one function
        static int AllOptimizationsCombined(int n)
        {
            // 1. Constant folding
            const int CONSTANT = 10 + 5;
            
            // 2. Dead code elimination
            if (false)
            {
                var dead = new MyClass();
                return -1;
            }
            
            // 3. CSE
            int cse = CONSTANT * 2;
            int result = cse + cse;
            
            // 4. Loop optimization (LICM, strength reduction)
            for (int i = 0; i < n; i++)
            {
                int invariant = CONSTANT * 3;  // Hoist
                result += invariant + (i * 2);  // Strength reduce
            }
            
            // 5. Inlining
            result += InlineMe(result);
            
            // 6. Tail call
            return TailRecursive(result, n);
        }
        
        static int InlineMe(int x)
        {
            return x + 10;
        }
        
        static int TailRecursive(int acc, int n)
        {
            if (n <= 0)
                return acc;
            return TailRecursive(acc + n, n - 1);
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
    
    class List<T>
    {
        private T[] items = new T[100];
        private int count = 0;
        
        public void Add(T item)
        {
            items[count++] = item;
        }
        
        public T this[int index]
        {
            get { return items[index]; }
        }
    }
}
