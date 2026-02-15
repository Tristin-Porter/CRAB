using CRAB.StandardLibrary;

/// <summary>
/// Example demonstrating interactive console input/output
/// Shows how to keep console open and read user input
/// </summary>
class Program
{
    static void Main()
    {
        Console.WriteLine("=================================");
        Console.WriteLine("CRAB Interactive Console Example");
        Console.WriteLine("=================================");
        Console.WriteLine();
        
        Console.Print("Enter your name: ");
        string name = Console.ReadLine();
        
        Console.WriteLine();
        Console.WriteLine($"Hello, {name}! Welcome to CRAB.");
        Console.WriteLine();
        
        Console.WriteLine("Press any key to clear the console...");
        Console.ReadKey(true);
        
        Console.Clear();
        
        Console.WriteLine("Console cleared!");
        Console.WriteLine();
        Console.WaitForKey();
    }
}
