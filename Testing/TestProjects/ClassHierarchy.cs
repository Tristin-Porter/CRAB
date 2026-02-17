// CRAB Test File: ClassHierarchy
// Tests class hierarchy and object-oriented features

using System;

namespace ClassHierarchy
{
    class Base
    {
        int GetBase()
        {
            return 10;
        }
    }
    
    class Derived
    {
        int GetValue()
        {
            return 20;
        }
    }
    
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Base: 10, Derived: 20");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
