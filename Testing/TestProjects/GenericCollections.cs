// CRAB Test File: GenericCollections
// Tests generic collections and containers

using System;

namespace GenericCollections
{
    class Container
    {
        int GetData()
        {
            return 100;
        }
    }
    
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Container Data: 100");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
