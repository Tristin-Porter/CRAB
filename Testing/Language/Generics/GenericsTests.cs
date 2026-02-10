// Generics Tests for CRAB
// Tests generic type compilation and usage

namespace CRAB.Testing.Language.Generics
{
    /// <summary>
    /// Test: Generic classes
    /// Expected: Compilation of generic class definitions and usage
    /// </summary>
    class GenericClassTest
    {
        class GenericContainer<T>
        {
            private T value;
            
            public GenericContainer(T initialValue)
            {
                value = initialValue;
            }
            
            public T GetValue() => value;
            public void SetValue(T newValue) => value = newValue;
        }
        
        static void TestGenericClass()
        {
            var intContainer = new GenericContainer<int>(42);
            int intValue = intContainer.GetValue();
            intContainer.SetValue(100);
            
            var stringContainer = new GenericContainer<string>("Hello");
            string stringValue = stringContainer.GetValue();
            stringContainer.SetValue("World");
        }
    }

    /// <summary>
    /// Test: Generic methods
    /// Expected: Generic method compilation with type inference
    /// </summary>
    class GenericMethodTest
    {
        static T Identity<T>(T value) => value;
        
        static void Swap<T>(ref T a, ref T b)
        {
            T temp = a;
            a = b;
            b = temp;
        }
        
        static void TestGenericMethods()
        {
            int x = Identity(42);
            string s = Identity("Hello");
            
            int a = 10, b = 20;
            Swap(ref a, ref b);
        }
    }

    /// <summary>
    /// Test: Generic constraints
    /// Expected: where clause constraint support
    /// </summary>
    class GenericConstraintsTest
    {
        interface IComparable<T>
        {
            int CompareTo(T other);
        }
        
        class SortableList<T> where T : IComparable<T>
        {
            private T[] items = new T[100];
            private int count;
            
            public void Add(T item)
            {
                items[count++] = item;
            }
            
            public void Sort()
            {
                for (int i = 0; i < count - 1; i++)
                {
                    for (int j = i + 1; j < count; j++)
                    {
                        if (items[i].CompareTo(items[j]) > 0)
                        {
                            T temp = items[i];
                            items[i] = items[j];
                            items[j] = temp;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Test: Multiple type parameters
    /// Expected: Support for multiple generic parameters
    /// </summary>
    class MultipleTypeParametersTest
    {
        class KeyValuePair<TKey, TValue>
        {
            public TKey Key { get; set; }
            public TValue Value { get; set; }
            
            public KeyValuePair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
        
        class Dictionary<TKey, TValue>
        {
            private KeyValuePair<TKey, TValue>[] items = new KeyValuePair<TKey, TValue>[100];
            private int count;
            
            public void Add(TKey key, TValue value)
            {
                items[count++] = new KeyValuePair<TKey, TValue>(key, value);
            }
            
            public TValue? Get(TKey key)
            {
                for (int i = 0; i < count; i++)
                {
                    if (items[i].Key?.Equals(key) == true)
                        return items[i].Value;
                }
                return default(TValue);
            }
        }
        
        static void TestMultipleTypeParameters()
        {
            var dict = new Dictionary<string, int>();
            dict.Add("one", 1);
            dict.Add("two", 2);
            int? value = dict.Get("one");
        }
    }

    /// <summary>
    /// Test: Generic interfaces
    /// Expected: Generic interface implementation
    /// </summary>
    class GenericInterfaceTest
    {
        interface IContainer<T>
        {
            void Add(T item);
            T Get(int index);
            int Count { get; }
        }
        
        class List<T> : IContainer<T>
        {
            private T[] items = new T[100];
            private int count;
            
            public void Add(T item)
            {
                items[count++] = item;
            }
            
            public T Get(int index) => items[index];
            
            public int Count => count;
        }
        
        static void TestGenericInterface()
        {
            IContainer<int> container = new List<int>();
            container.Add(10);
            container.Add(20);
            int value = container.Get(0);
        }
    }

    /// <summary>
    /// Test: Generic inheritance
    /// Expected: Generic base class inheritance
    /// </summary>
    class GenericInheritanceTest
    {
        class BaseContainer<T>
        {
            protected T[] items = new T[100];
            protected int count;
            
            public virtual void Add(T item)
            {
                items[count++] = item;
            }
        }
        
        class ExtendedContainer<T> : BaseContainer<T>
        {
            public override void Add(T item)
            {
                base.Add(item);
                // Additional logic
            }
            
            public T[] GetAll()
            {
                T[] result = new T[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = items[i];
                }
                return result;
            }
        }
    }

    /// <summary>
    /// Test: Nested generics
    /// Expected: Generic types within generic types
    /// </summary>
    class NestedGenericsTest
    {
        class Outer<T>
        {
            class Inner<U>
            {
                public T OuterValue { get; set; }
                public U InnerValue { get; set; }
                
                public Inner(T outer, U inner)
                {
                    OuterValue = outer;
                    InnerValue = inner;
                }
            }
            
            public Inner<U> CreateInner<U>(T outer, U inner)
            {
                return new Inner<U>(outer, inner);
            }
        }
        
        static void TestNestedGenerics()
        {
            var outer = new Outer<int>();
            var inner = outer.CreateInner(42, "Hello");
        }
    }
}
