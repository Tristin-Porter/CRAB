using System;

namespace TestDebug
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello from CRAB!");
            
            if (args.Length > 0)
            {
                Console.WriteLine("Arguments:");
                foreach (var arg in args)
                {
                    Console.WriteLine($"  {arg}");
                }
            }
        }
    }
}
