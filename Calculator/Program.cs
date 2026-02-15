using System;

class Calculator
{
    public int x;
    public int y;
    
    public int Add()
    {
        return x + y;
    }
}

class Program
{
    static void Main()
    {
        var calc = new Calculator { x = 5, y = 3 };
        Console.WriteLine($"Calculator: {calc.x} + {calc.y} = {calc.Add()}");
    }
}