// Basic C# Language Feature Tests for CRAB
// Tests compilation of fundamental C# language constructs

namespace CRAB.Testing.Language.Basics
{
    /// <summary>
    /// Test: Classes and objects
    /// Expected: Class compilation and instantiation
    /// </summary>
    class ClassTest
    {
        class TestClass
        {
            private int value;
            public string Name { get; set; } = "";
            
            public TestClass(int val)
            {
                value = val;
            }
            
            public int GetValue() => value;
            public void SetValue(int val) => value = val;
        }
        
        static void TestClasses()
        {
            var obj = new TestClass(42);
            obj.Name = "Test";
            int value = obj.GetValue();
            obj.SetValue(100);
        }
    }

    /// <summary>
    /// Test: Inheritance and polymorphism
    /// Expected: Virtual methods and override support
    /// </summary>
    class InheritanceTest
    {
        class BaseClass
        {
            public virtual int Calculate(int x) => x * 2;
        }
        
        class DerivedClass : BaseClass
        {
            public override int Calculate(int x) => x * 3;
        }
        
        static void TestInheritance()
        {
            BaseClass base1 = new BaseClass();
            BaseClass base2 = new DerivedClass();
            
            int result1 = base1.Calculate(10); // 20
            int result2 = base2.Calculate(10); // 30 (polymorphic)
        }
    }

    /// <summary>
    /// Test: Interfaces
    /// Expected: Interface implementation and usage
    /// </summary>
    class InterfaceTest
    {
        interface IProcessor
        {
            int Process(int value);
        }
        
        class Doubler : IProcessor
        {
            public int Process(int value) => value * 2;
        }
        
        class Tripler : IProcessor
        {
            public int Process(int value) => value * 3;
        }
        
        static void TestInterfaces()
        {
            IProcessor processor1 = new Doubler();
            IProcessor processor2 = new Tripler();
            
            int result1 = processor1.Process(10);
            int result2 = processor2.Process(10);
        }
    }

    /// <summary>
    /// Test: Structs and value types
    /// Expected: Struct compilation and usage
    /// </summary>
    class StructTest
    {
        struct Point
        {
            public int X { get; set; }
            public int Y { get; set; }
            
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }
            
            public int DistanceFromOrigin()
            {
                return (int)System.Math.Sqrt(X * X + Y * Y);
            }
        }
        
        static void TestStructs()
        {
            Point p1 = new Point(3, 4);
            Point p2 = p1; // Value copy
            p2.X = 10;
            
            int dist = p1.DistanceFromOrigin();
        }
    }

    /// <summary>
    /// Test: Methods and parameters
    /// Expected: Various parameter passing modes
    /// </summary>
    class MethodTest
    {
        static void ValueParameter(int x)
        {
            x = 42;
        }
        
        static void RefParameter(ref int x)
        {
            x = 42;
        }
        
        static void OutParameter(out int x)
        {
            x = 42;
        }
        
        static void TestMethods()
        {
            int a = 10;
            ValueParameter(a); // a still 10
            
            int b = 10;
            RefParameter(ref b); // b now 42
            
            int c;
            OutParameter(out c); // c now 42
        }
    }

    /// <summary>
    /// Test: Properties and indexers
    /// Expected: Property get/set and indexer support
    /// </summary>
    class PropertyTest
    {
        class Container
        {
            private int[] data = new int[10];
            
            public int Count { get; private set; }
            
            public int this[int index]
            {
                get => data[index];
                set => data[index] = value;
            }
            
            public void Add(int value)
            {
                data[Count++] = value;
            }
        }
        
        static void TestProperties()
        {
            var container = new Container();
            container.Add(10);
            container.Add(20);
            
            int count = container.Count;
            int first = container[0];
            container[1] = 30;
        }
    }

    /// <summary>
    /// Test: Operators and operator overloading
    /// Expected: Custom operator implementation
    /// </summary>
    class OperatorTest
    {
        struct Complex
        {
            public double Real { get; set; }
            public double Imaginary { get; set; }
            
            public static Complex operator +(Complex a, Complex b)
            {
                return new Complex
                {
                    Real = a.Real + b.Real,
                    Imaginary = a.Imaginary + b.Imaginary
                };
            }
            
            public static Complex operator *(Complex a, Complex b)
            {
                return new Complex
                {
                    Real = a.Real * b.Real - a.Imaginary * b.Imaginary,
                    Imaginary = a.Real * b.Imaginary + a.Imaginary * b.Real
                };
            }
        }
        
        static void TestOperators()
        {
            Complex a = new Complex { Real = 1, Imaginary = 2 };
            Complex b = new Complex { Real = 3, Imaginary = 4 };
            
            Complex sum = a + b;
            Complex product = a * b;
        }
    }

    /// <summary>
    /// Test: Control flow statements
    /// Expected: All control flow constructs compile correctly
    /// </summary>
    class ControlFlowTest
    {
        static void TestControlFlow()
        {
            // If-else
            int x = 10;
            if (x > 5)
            {
                x = 5;
            }
            else if (x < 0)
            {
                x = 0;
            }
            else
            {
                x = 1;
            }
            
            // Switch
            switch (x)
            {
                case 0:
                    x = 1;
                    break;
                case 1:
                    x = 2;
                    break;
                default:
                    x = 0;
                    break;
            }
            
            // While
            while (x > 0)
            {
                x--;
            }
            
            // Do-while
            do
            {
                x++;
            } while (x < 10);
            
            // For
            for (int i = 0; i < 10; i++)
            {
                x += i;
            }
            
            // Foreach
            int[] array = { 1, 2, 3, 4, 5 };
            foreach (int n in array)
            {
                x += n;
            }
        }
    }

    /// <summary>
    /// Test: Exception handling
    /// Expected: Try-catch-finally support
    /// </summary>
    class ExceptionTest
    {
        static void TestExceptions()
        {
            try
            {
                int x = 10;
                int y = 0;
                int z = x / y; // Division by zero
            }
            catch (System.DivideByZeroException ex)
            {
                // Handle specific exception
            }
            catch (System.Exception ex)
            {
                // Handle general exception
            }
            finally
            {
                // Always executed
            }
        }
        
        static void TestThrow()
        {
            try
            {
                throw new System.InvalidOperationException("Test exception");
            }
            catch (System.InvalidOperationException)
            {
                // Caught
            }
        }
    }

    /// <summary>
    /// Test: Delegates and events
    /// Expected: Delegate and event compilation
    /// </summary>
    class DelegateTest
    {
        delegate int Operation(int x, int y);
        
        static int Add(int x, int y) => x + y;
        static int Multiply(int x, int y) => x * y;
        
        static void TestDelegates()
        {
            Operation op1 = Add;
            Operation op2 = Multiply;
            
            int result1 = op1(10, 20);
            int result2 = op2(10, 20);
            
            // Multicast delegate
            Operation combined = op1 + op2;
            combined(5, 10);
        }
        
        class EventPublisher
        {
            public event System.Action? OnEvent;
            
            public void Trigger()
            {
                OnEvent?.Invoke();
            }
        }
        
        static void TestEvents()
        {
            var publisher = new EventPublisher();
            publisher.OnEvent += () => { /* Handler 1 */ };
            publisher.OnEvent += () => { /* Handler 2 */ };
            publisher.Trigger();
        }
    }

    /// <summary>
    /// Test: Lambda expressions
    /// Expected: Lambda compilation and closure
    /// </summary>
    class LambdaTest
    {
        static void TestLambdas()
        {
            // Simple lambda
            System.Func<int, int> double1 = x => x * 2;
            int result1 = double1(10);
            
            // Multi-parameter lambda
            System.Func<int, int, int> add = (x, y) => x + y;
            int result2 = add(10, 20);
            
            // Statement lambda
            System.Func<int, int> complex = x =>
            {
                int temp = x * 2;
                temp += 10;
                return temp;
            };
            int result3 = complex(5);
            
            // Closure
            int captured = 100;
            System.Func<int, int> closure = x => x + captured;
            int result4 = closure(10);
        }
    }
}
