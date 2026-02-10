// LINQ Optimization Tests for CRAB Automatic Memory Model (CTGC)
// Tests CTGC optimization of LINQ queries

namespace CRAB.Testing.Automatic.LINQ
{
    /// <summary>
    /// Test: Simple LINQ query optimization
    /// Expected: Intermediate allocations minimized
    /// </summary>
    class SimpleLINQTest
    {
        static void TestSimpleQuery()
        {
            var numbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            var evens = numbers
                .Where(x => x % 2 == 0)
                .Select(x => x * 2);
            
            foreach (var n in evens)
            {
                // Process
            }
            
            // All LINQ intermediate objects deallocated
        }
    }

    /// <summary>
    /// Test: Chained LINQ operations
    /// Expected: Pipeline optimized, minimal allocations
    /// </summary>
    class ChainedLINQTest
    {
        static void TestChainedOperations()
        {
            var numbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            var result = numbers
                .Where(x => x > 3)
                .Select(x => x * 2)
                .OrderBy(x => x)
                .Take(5);
            
            var list = result.ToList();
            
            // Intermediate objects deallocated, list remains
        }
    }

    /// <summary>
    /// Test: LINQ with object allocations
    /// Expected: Objects in LINQ pipeline managed correctly
    /// </summary>
    class LINQObjectsTest
    {
        static void TestLINQWithObjects()
        {
            var items = new LINQTestClass[]
            {
                new LINQTestClass { Value = 1 },
                new LINQTestClass { Value = 2 },
                new LINQTestClass { Value = 3 }
            };
            
            var result = items
                .Where(x => x.Value > 1)
                .Select(x => new LINQTestClass { Value = x.Value * 2 });
            
            foreach (var item in result)
            {
                item.DoWork();
            }
            
            // All objects deallocated
        }
    }

    /// <summary>
    /// Test: LINQ query with closure
    /// Expected: Closure captures optimized
    /// </summary>
    class LINQClosureTest
    {
        static void TestLINQClosure()
        {
            int threshold = 5;
            var numbers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            
            var filtered = numbers
                .Where(x => x > threshold); // Captures threshold
            
            foreach (var n in filtered)
            {
                // Process
            }
            
            // Closure and intermediate objects deallocated
        }
    }

    /// <summary>
    /// Test: Nested LINQ queries
    /// Expected: All levels managed correctly
    /// </summary>
    class NestedLINQTest
    {
        static void TestNestedQueries()
        {
            var groups = new int[][]
            {
                new int[] { 1, 2, 3 },
                new int[] { 4, 5, 6 },
                new int[] { 7, 8, 9 }
            };
            
            var result = groups
                .SelectMany(g => g
                    .Where(x => x % 2 == 0)
                    .Select(x => x * 2));
            
            var list = result.ToList();
            
            // All nested intermediate objects deallocated
        }
    }

    /// <summary>
    /// Test: LINQ with materialization
    /// Expected: ToList/ToArray creates final allocation
    /// </summary>
    class LINQMaterializationTest
    {
        static void TestMaterialization()
        {
            var numbers = new int[] { 1, 2, 3, 4, 5 };
            
            var list = numbers
                .Where(x => x > 2)
                .Select(x => x * 2)
                .ToList(); // Materializes to list
            
            // Intermediate objects deallocated
            // list remains until scope exit
        }
    }

    /// <summary>
    /// Test: LINQ aggregate operations
    /// Expected: Aggregations optimized, minimal allocations
    /// </summary>
    class LINQAggregateTest
    {
        static void TestAggregate()
        {
            var numbers = new int[] { 1, 2, 3, 4, 5 };
            
            int sum = numbers.Sum();
            int max = numbers.Max();
            double avg = numbers.Average();
            
            // No persistent allocations from aggregates
        }
    }

    /// <summary>
    /// Test: LINQ GroupBy operation
    /// Expected: Groups managed efficiently
    /// </summary>
    class LINQGroupByTest
    {
        static void TestGroupBy()
        {
            var items = new LINQTestClass[]
            {
                new LINQTestClass { Value = 1, Category = "A" },
                new LINQTestClass { Value = 2, Category = "B" },
                new LINQTestClass { Value = 3, Category = "A" },
                new LINQTestClass { Value = 4, Category = "B" }
            };
            
            var groups = items
                .GroupBy(x => x.Category);
            
            foreach (var group in groups)
            {
                foreach (var item in group)
                {
                    item.DoWork();
                }
            }
            
            // All group objects deallocated
        }
    }

    /// <summary>
    /// Test: LINQ Join operation
    /// Expected: Join results managed efficiently
    /// </summary>
    class LINQJoinTest
    {
        static void TestJoin()
        {
            var left = new LINQTestClass[]
            {
                new LINQTestClass { Value = 1, Category = "A" },
                new LINQTestClass { Value = 2, Category = "B" }
            };
            
            var right = new LINQTestClass[]
            {
                new LINQTestClass { Value = 3, Category = "A" },
                new LINQTestClass { Value = 4, Category = "B" }
            };
            
            var joined = left.Join(
                right,
                l => l.Category,
                r => r.Category,
                (l, r) => new { Left = l, Right = r }
            );
            
            foreach (var pair in joined)
            {
                pair.Left.DoWork();
                pair.Right.DoWork();
            }
            
            // Join results deallocated
        }
    }

    /// <summary>
    /// Test: Deferred execution optimization
    /// Expected: Query not executed until enumerated
    /// </summary>
    class DeferredExecutionTest
    {
        static void TestDeferredExecution()
        {
            var numbers = new int[] { 1, 2, 3, 4, 5 };
            
            // Query defined but not executed
            var query = numbers
                .Where(x => x > 2)
                .Select(x => x * 2);
            
            // Executed here
            foreach (var n in query)
            {
                // Process
            }
            
            // Query objects deallocated
        }
    }

    /// <summary>
    /// Test: LINQ with let clause
    /// Expected: Temporary variables managed correctly
    /// </summary>
    class LINQLetClauseTest
    {
        static void TestLetClause()
        {
            var numbers = new int[] { 1, 2, 3, 4, 5 };
            
            var query = from n in numbers
                        let squared = n * n
                        let cubed = squared * n
                        where cubed > 50
                        select new { Number = n, Cubed = cubed };
            
            foreach (var item in query)
            {
                // Process
            }
            
            // All temporary variables deallocated
        }
    }

    /// <summary>
    /// Test: Multiple enumerations of same query
    /// Expected: Each enumeration managed independently
    /// </summary>
    class MultipleEnumerationsTest
    {
        static void TestMultipleEnumerations()
        {
            var numbers = new int[] { 1, 2, 3, 4, 5 };
            
            var query = numbers.Where(x => x > 2);
            
            // First enumeration
            foreach (var n in query)
            {
                // Process
            }
            
            // Second enumeration
            foreach (var n in query)
            {
                // Process
            }
            
            // Query objects deallocated
        }
    }

    /// <summary>
    /// Test: LINQ with custom IEnumerable
    /// Expected: Custom enumerators managed correctly
    /// </summary>
    class CustomEnumerableTest
    {
        static void TestCustomEnumerable()
        {
            var custom = new CustomEnumerable();
            
            var query = custom
                .Where(x => x > 5)
                .Select(x => x * 2);
            
            foreach (var n in query)
            {
                // Process
            }
            
            // Custom enumerable and query objects deallocated
        }
    }

    // Helper classes
    class LINQTestClass
    {
        public int Value { get; set; }
        public string Category { get; set; } = "";
        
        public void DoWork()
        {
            Value += 10;
        }
    }

    class CustomEnumerable : System.Collections.Generic.IEnumerable<int>
    {
        public System.Collections.Generic.IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < 10; i++)
                yield return i;
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
