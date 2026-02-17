// CRAB Test File: Calculator
// Simple calculator that takes two numbers as input and adds them

using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Simple Calculator");
        Console.WriteLine("================");
        Console.WriteLine("");
        
        Console.WriteLine("Enter first number: ");
        string input1 = Console.ReadLine();
        int num1 = int.Parse(input1);
        
        Console.WriteLine("Enter second number: ");
        string input2 = Console.ReadLine();
        int num2 = int.Parse(input2);
        
        int result = num1 + num2;
        
        Console.WriteLine("");
        Console.WriteLine("Result: " + num1 + " + " + num2 + " = " + result);
        Console.WriteLine("");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}
