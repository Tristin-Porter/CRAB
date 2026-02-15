using System;

class Container
{
    public int data;
    
    public Container(int value)
    {
        data = value;
    }
}

class Program
{
    static void Main()
    {
        var container = new Container(42);
        Console.WriteLine($"Container holds: {container.data}");
    }
}