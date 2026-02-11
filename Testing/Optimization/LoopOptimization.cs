// Loop Optimization Tests
// Verifies loop invariant code motion, strength reduction, and other loop optimizations

namespace OptimizationTest
{
    class LoopOptimizationTest
    {
        // Test 1: Loop Invariant Code Motion (LICM) - simple
        static int SimpleLICM()
        {
            int x = 10;
            int y = 20;
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                int invariant = x + y;  // Loop invariant - should be hoisted
                sum += invariant * i;
            }
            
            return sum;
        }
        
        // Test 2: LICM with nested loops
        static int NestedLICM()
        {
            int x = 10;
            int y = 20;
            int sum = 0;
            
            for (int i = 0; i < 10; i++)
            {
                int outer_invariant = x * 2;  // Invariant in outer loop
                
                for (int j = 0; j < 10; j++)
                {
                    int inner_invariant = y * 3;  // Invariant in inner loop
                    sum += outer_invariant + inner_invariant + i + j;
                }
            }
            
            return sum;
        }
        
        // Test 3: Strength reduction - multiplication to addition
        static int StrengthReduction()
        {
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                sum += i * 5;  // Could be replaced with addition
            }
            
            return sum;
        }
        
        // Test 4: Loop unrolling - small constant iterations
        static int LoopUnrolling()
        {
            int sum = 0;
            
            // Small constant loop - candidate for unrolling
            for (int i = 0; i < 4; i++)
            {
                sum += i * i;
            }
            
            return sum;
        }
        
        // Test 5: Loop fusion - combining adjacent loops
        static int LoopFusion()
        {
            var array = new int[100];
            
            // First loop
            for (int i = 0; i < 100; i++)
            {
                array[i] = i;
            }
            
            // Second loop - could be fused with first
            for (int i = 0; i < 100; i++)
            {
                array[i] *= 2;
            }
            
            return array[50];
        }
        
        // Test 6: Loop fission - splitting complex loop
        static void LoopFission()
        {
            var array1 = new int[100];
            var array2 = new int[100];
            
            // Complex loop that could be split
            for (int i = 0; i < 100; i++)
            {
                array1[i] = i * 2;      // Independent computation
                array2[i] = i * 3;      // Independent computation
            }
        }
        
        // Test 7: LICM with memory allocation
        static int LICMWithAllocation()
        {
            int sum = 0;
            var obj = new MyClass();  // Allocation outside loop
            obj.value = 10;
            
            for (int i = 0; i < 100; i++)
            {
                int invariant = obj.value * 2;  // Invariant read
                sum += invariant + i;
            }
            
            return sum;
        }
        
        // Test 8: Do NOT hoist allocations from loops (lifetime issues)
        static void NoHoistAllocation()
        {
            for (int i = 0; i < 10; i++)
            {
                var obj = new MyClass();  // Must allocate each iteration
                obj.value = i;
                obj.DoWork();
                // obj deallocated at end of iteration
            }
        }
        
        // Test 9: LICM with conditional
        static int LICMWithConditional(bool condition)
        {
            int x = 10;
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                if (condition)  // Condition is loop invariant
                {
                    sum += x * 2;  // Could be hoisted if condition is invariant
                }
                else
                {
                    sum += x * 3;
                }
            }
            
            return sum;
        }
        
        // Test 10: Induction variable optimization
        static int InductionVariable()
        {
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                int j = i * 2;  // Derived induction variable
                int k = i + 10; // Another derived induction variable
                sum += j + k;
            }
            
            return sum;
        }
        
        // Test 11: Loop with array access optimization
        static int ArrayAccessOptimization()
        {
            var array = new int[100];
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                array[i] = i;
            }
            
            for (int i = 0; i < 100; i++)
            {
                sum += array[i];  // Sequential access - predictable
            }
            
            return sum;
        }
        
        // Test 12: Loop with bounds check elimination
        static int BoundsCheckElimination()
        {
            var array = new int[100];
            int sum = 0;
            
            // Bounds checks can be eliminated - i is provably in range
            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }
            
            return sum;
        }
        
        // Test 13: While loop optimization
        static int WhileLoopOptimization()
        {
            int i = 0;
            int sum = 0;
            int invariant = 10 * 5;  // Loop invariant
            
            while (i < 100)
            {
                sum += invariant + i;
                i++;
            }
            
            return sum;
        }
        
        // Test 14: Do-while loop optimization
        static int DoWhileOptimization()
        {
            int i = 0;
            int sum = 0;
            int invariant = 20;
            
            do
            {
                sum += invariant * 2;  // Invariant
                i++;
            } while (i < 100);
            
            return sum;
        }
        
        // Test 15: Loop with side effects - careful optimization
        static int LoopWithSideEffects()
        {
            var obj = new MyClass();
            int sum = 0;
            
            for (int i = 0; i < 10; i++)
            {
                obj.Increment();  // Side effect - must preserve
                sum += obj.value;
            }
            
            return sum;
        }
        
        // Test 16: Loop with break - optimization must preserve semantics
        static int LoopWithBreak()
        {
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                sum += i;
                if (sum > 1000)
                    break;
            }
            
            return sum;
        }
        
        // Test 17: Loop with continue
        static int LoopWithContinue()
        {
            int sum = 0;
            
            for (int i = 0; i < 100; i++)
            {
                if (i % 2 == 0)
                    continue;
                sum += i;
            }
            
            return sum;
        }
        
        // Test 18: Loop vectorization candidate
        static void VectorizationCandidate()
        {
            var a = new int[100];
            var b = new int[100];
            var c = new int[100];
            
            // Simple element-wise operation - vectorization candidate
            for (int i = 0; i < 100; i++)
            {
                c[i] = a[i] + b[i];
            }
        }
        
        // Test 19: Loop with memory dependency - cannot vectorize
        static void MemoryDependency()
        {
            var array = new int[100];
            
            // Memory dependency prevents vectorization
            for (int i = 1; i < 100; i++)
            {
                array[i] = array[i - 1] + 1;
            }
        }
        
        // Test 20: Loop interchange for cache efficiency
        static void LoopInterchange()
        {
            var matrix = new int[100][];
            for (int i = 0; i < 100; i++)
            {
                matrix[i] = new int[100];
            }
            
            // Nested loops - order might be interchanged for cache efficiency
            for (int i = 0; i < 100; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    matrix[i][j] = i + j;
                }
            }
        }
    }
    
    class MyClass
    {
        public int value;
        
        public void DoWork()
        {
            value = 42;
        }
        
        public void Increment()
        {
            value++;
        }
    }
}
