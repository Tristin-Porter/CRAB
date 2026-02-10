// Simple Hello World program for CRAB compiler testing
// This tests basic class declarations, methods, and console output

using System;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, CRAB!");
            
            // Test automatic memory management (CTGC)
            var greeting = new Greeter();
            greeting.SayHello("World");
        }
    }
    
    class Greeter
    {
        public void SayHello(string name)
        {
            Console.WriteLine($"Hello, {name}!");
        }
    }
}
