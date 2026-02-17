// CRAB Test File: Calculator
// Simple calculator that adds two numbers

using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Simple Calculator");
        Console.WriteLine("================");
        Console.WriteLine("");
        
        // For now using hardcoded values until Console.ReadLine() is implemented
        int num1 = 5;
        int num2 = 3;
        int result = num1 + num2;
        
        Console.WriteLine("Calculating: 5 + 3");
        Console.WriteLine("Result: 8");
        Console.WriteLine("");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
