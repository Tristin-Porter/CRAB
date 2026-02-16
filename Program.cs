using CRAB;
using CDTk;

// CRAB CLI Entry Point
// This is the main entry point for the CRAB compiler command-line interface.

class Program
{
    static void Main(string[] args)
    {
        var registry = new Registry()
            .Register(new Help())
            .Register(new Compile())
            .Register(new Build())
            .Register(new Run())
            .Register(new Test())
            .Register(new TestSuite())
            .Register(new New()
                .AddSub(new CRAB.Console()));
        
        if (args.Length == 0)
        {
            System.Console.WriteLine("CRAB - C# to WebAssembly Compiler");
            System.Console.WriteLine("Usage: crab <command> [options]");
            System.Console.WriteLine();
            System.Console.WriteLine("Commands:");
            foreach (var cmd in registry.AllCommands)
            {
                System.Console.WriteLine($"  {cmd.Name,-15} {cmd.Description}");
            }
            System.Console.WriteLine();
            System.Console.WriteLine("Run 'crab help <command>' for more information on a command.");
            return;
        }
        
        registry.Run(string.Join(" ", args));
    }
}
