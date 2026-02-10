// Escape Analysis Tests for CRAB Automatic Memory Model (CTGC)
// Tests the compiler's ability to track which objects escape their allocation scope

namespace CRAB.Testing.Automatic.Escape
{
    /// <summary>
    /// Test: Local object that doesn't escape
    /// Expected: Object can be stack-allocated or freed immediately
    /// </summary>
    class NoEscapeTest
    {
        static void TestNoEscape()
        {
            var obj = new EscapeTestClass();
            obj.DoWork();
            // obj doesn't escape - can be optimized
        }
    }

    /// <summary>
    /// Test: Object escapes via return
    /// Expected: Object must be heap-allocated, lifetime extended
    /// </summary>
    class ReturnEscapeTest
    {
        static EscapeTestClass CreateObject()
        {
            var obj = new EscapeTestClass();
            obj.DoWork();
            return obj; // ESCAPES via return
        }
        
        static void UseEscapedObject()
        {
            var obj = CreateObject();
            obj.DoWork();
        }
    }

    /// <summary>
    /// Test: Object escapes via field assignment
    /// Expected: Lifetime tied to containing object
    /// </summary>
    class FieldEscapeTest
    {
        static EscapeContainer container = new EscapeContainer();
        
        static void TestFieldEscape()
        {
            var obj = new EscapeTestClass();
            container.Item = obj; // ESCAPES to field
            // obj lifetime now tied to container
        }
    }

    /// <summary>
    /// Test: Object escapes via out parameter
    /// Expected: Object must outlive current scope
    /// </summary>
    class OutParameterEscapeTest
    {
        static void CreateObject(out EscapeTestClass obj)
        {
            obj = new EscapeTestClass();
            obj.DoWork();
            // ESCAPES via out parameter
        }
        
        static void UseOutParameter()
        {
            EscapeTestClass obj;
            CreateObject(out obj);
            obj.DoWork();
        }
    }

    /// <summary>
    /// Test: Object escapes via closure capture
    /// Expected: Lifetime extended to closure lifetime
    /// </summary>
    class ClosureEscapeTest
    {
        static System.Action CreateClosure()
        {
            var obj = new EscapeTestClass();
            
            return () =>
            {
                obj.DoWork(); // ESCAPES via closure capture
            };
        }
        
        static void UseClosureEscape()
        {
            var action = CreateClosure();
            action(); // Uses captured object
        }
    }

    /// <summary>
    /// Test: Object escapes via collection
    /// Expected: Lifetime tied to collection
    /// </summary>
    class CollectionEscapeTest
    {
        static void TestCollectionEscape()
        {
            var list = new System.Collections.Generic.List<EscapeTestClass>();
            
            var obj = new EscapeTestClass();
            list.Add(obj); // ESCAPES into collection
            
            // obj lifetime now tied to list
        }
    }

    /// <summary>
    /// Test: Object escapes via event handler
    /// Expected: Lifetime extended to event lifetime
    /// </summary>
    class EventEscapeTest
    {
        static event System.Action? OnEvent;
        
        static void TestEventEscape()
        {
            var obj = new EscapeTestClass();
            
            OnEvent += () =>
            {
                obj.DoWork(); // ESCAPES via event handler
            };
            
            // obj must stay alive as long as event handler registered
        }
    }

    /// <summary>
    /// Test: Object doesn't escape in conditional
    /// Expected: Can be optimized if all paths are local
    /// </summary>
    class ConditionalNoEscapeTest
    {
        static void TestConditionalNoEscape(bool condition)
        {
            var obj = new EscapeTestClass();
            
            if (condition)
            {
                obj.DoWork();
            }
            else
            {
                obj.Process();
            }
            
            // obj doesn't escape - all uses are local
        }
    }

    /// <summary>
    /// Test: Partial escape via conditional return
    /// Expected: Conservative analysis assumes escape
    /// </summary>
    class ConditionalEscapeTest
    {
        static EscapeTestClass? TestConditionalEscape(bool condition)
        {
            var obj = new EscapeTestClass();
            obj.DoWork();
            
            if (condition)
            {
                return obj; // ESCAPES on this path
            }
            
            obj.Process();
            return null; // Doesn't escape on this path
            
            // Conservative: assume escape
        }
    }

    /// <summary>
    /// Test: Object escapes via array
    /// Expected: Lifetime tied to array
    /// </summary>
    class ArrayEscapeTest
    {
        static void TestArrayEscape()
        {
            var array = new EscapeTestClass[10];
            
            for (int i = 0; i < 10; i++)
            {
                var obj = new EscapeTestClass { Value = i };
                array[i] = obj; // ESCAPES into array
            }
            
            // All objects' lifetimes tied to array
        }
    }

    /// <summary>
    /// Test: Nested object escape
    /// Expected: Inner object escapes if outer escapes
    /// </summary>
    class NestedEscapeTest
    {
        static EscapeContainer CreateNestedEscape()
        {
            var container = new EscapeContainer();
            var obj = new EscapeTestClass();
            
            container.Item = obj; // obj escapes to container
            return container; // container and obj both escape
        }
    }

    /// <summary>
    /// Test: No escape in loop
    /// Expected: Each iteration allocates and frees locally
    /// </summary>
    class LoopNoEscapeTest
    {
        static void TestLoopNoEscape()
        {
            for (int i = 0; i < 100; i++)
            {
                var obj = new EscapeTestClass { Value = i };
                obj.DoWork();
                // obj doesn't escape - freed each iteration
            }
        }
    }

    /// <summary>
    /// Test: Escape via method parameter (ref)
    /// Expected: Object lifetime must extend beyond call
    /// </summary>
    class RefParameterEscapeTest
    {
        static void ModifyObject(ref EscapeTestClass obj)
        {
            obj = new EscapeTestClass();
            obj.DoWork();
            // ESCAPES via ref parameter
        }
        
        static void UseRefParameter()
        {
            EscapeTestClass obj = new EscapeTestClass();
            ModifyObject(ref obj);
            obj.DoWork();
        }
    }

    /// <summary>
    /// Test: Object escapes via delegate
    /// Expected: Lifetime tied to delegate
    /// </summary>
    class DelegateEscapeTest
    {
        static System.Func<int> CreateDelegate()
        {
            var obj = new EscapeTestClass { Value = 42 };
            
            return () =>
            {
                return obj.Value; // ESCAPES via delegate
            };
        }
        
        static void UseDelegateEscape()
        {
            var func = CreateDelegate();
            int value = func();
        }
    }

    // Helper classes
    class EscapeTestClass
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

    class EscapeContainer
    {
        public EscapeTestClass? Item { get; set; }
        
        public void Process()
        {
            Item?.DoWork();
        }
    }
}
