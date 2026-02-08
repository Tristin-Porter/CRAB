namespace CRAB;

public struct InputColors
{
    public ConsoleColor Command;
    public ConsoleColor Subcommand;
    public ConsoleColor Other;
}

public static class Formatting
{
    public enum TokenType
    {
        Command,
        Subcommand,
        Other
    }

    public static TokenType Classify(string token, Registry registry)
    {
        token = token.ToLower();

        if (registry.Commands.ContainsKey(token))
            return TokenType.Command;

        foreach (var cmd in registry.AllCommands)
            if (cmd.Subcommands.ContainsKey(token))
                return TokenType.Subcommand;

        return TokenType.Other;
    }

    public static void RenderColored(string text, Registry registry, InputColors colors)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        int index = 0;

        foreach (var part in parts)
        {
            var type = Classify(part, registry);

            switch (type)
            {
                case TokenType.Command:
                    System.Console.ForegroundColor = colors.Command;
                    break;

                case TokenType.Subcommand:
                    System.Console.ForegroundColor = colors.Subcommand;
                    break;

                default:
                    System.Console.ForegroundColor = colors.Other;
                    break;
            }

            System.Console.Write(part);
            System.Console.ResetColor();

            index += part.Length;

            if (index < text.Length)
            {
                System.Console.Write(" ");
                index++;
            }
        }
    }
}
