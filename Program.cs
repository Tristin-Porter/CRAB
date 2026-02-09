using CDTk;

namespace CRAB;

class Program
{
    public static void Main(string[] args)
    {
        var CRAB = new Compiler()
            .WithTokens(new Tokens())
            .WithRules(new Rules())
            .WithTarget(new WASM())
            .Build();
        
        // Graceful Ctrl+C
        System.Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            Environment.Exit(0);
        };

        // Detect light mode (very simple heuristic)
        bool lightMode =
            System.Console.BackgroundColor == ConsoleColor.White ||
            System.Console.BackgroundColor == ConsoleColor.Gray ||
            System.Console.BackgroundColor == ConsoleColor.Yellow;

        // CRAB color palette
        var colors = new InputColors
        {
            Command = ConsoleColor.DarkRed,                 // BRICK RED
            Subcommand = lightMode ? ConsoleColor.Black     // MILKY WHITE fallback
                                   : ConsoleColor.White,    // MILKY WHITE
            Other = ConsoleColor.Gray                       // Neutral
        };

        var registry = new Registry()
            .Register(
                new New()
                    .AddSub(new Console())
                    .AddSub(new Project())
            )
            .Register(new Compile())
            .Register(new Build())
            .Register(new Run())
            .Register(new Help());

        // If command-line arguments are provided, execute directly (non-interactive mode)
        if (args.Length > 0)
        {
            var commandLine = string.Join(" ", args);
            registry.Run(commandLine);
            return;
        }

        // Interactive mode
        while (true)
        {
            System.Console.Write("> ");
            var input = ReadColoredInput(registry, colors);

            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input == "^C")
                break;

            registry.Run(input);
        }
    }

    static string ReadColoredInput(Registry registry, InputColors colors)
    {
        var buffer = new List<char>();

        while (true)
        {
            var key = System.Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                System.Console.WriteLine();
                return new string(buffer.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Count > 0)
                    buffer.RemoveAt(buffer.Count - 1);
            }
            else
            {
                buffer.Add(key.KeyChar);
            }

            // Re-render
            System.Console.Write("\r> ");
            System.Console.ForegroundColor = ConsoleColor.Gray;

            var text = new string(buffer.ToArray());
            Formatting.RenderColored(text, registry, colors);

            System.Console.ResetColor();
            System.Console.Write("   ");
            System.Console.Write("\r> ");
            Formatting.RenderColored(text, registry, colors);
        }
    }
}
