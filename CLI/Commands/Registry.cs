namespace CRAB;

public abstract class Command
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";

    // flagName -> description
    public Dictionary<string, string> SupportedFlags { get; set; } = new();

    // subcommandName -> Command
    public Dictionary<string, Command> Subcommands { get; set; } = new();

    public Command AddSub(Command sub)
    {
        Subcommands[sub.Name.ToLower()] = sub;
        return this;
    }

    public abstract void Execute(string[] args, Dictionary<string, string?> flags);
}

public class Registry
{
    public static Registry? Instance { get; private set; }
    public Registry()
    {
        Instance = this;
    }

    private readonly Dictionary<string, Command> _commands = new();

    public Registry Register(Command command)
    {
        _commands[command.Name.ToLower()] = command;
        return this;
    }

    // ------------------------------
    // Levenshtein distance
    // ------------------------------
    private static int Distance(string a, string b)
    {
        int[,] d = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }

        return d[a.Length, b.Length];
    }

    // ------------------------------
    // Suggest command/subcommand
    // ------------------------------
    private static string? Suggest(string input, Dictionary<string, Command> options)
    {
        input = input.ToLower();

        // 1. Strong prefix match
        var prefixMatches = options.Keys
            .Where(k => k.StartsWith(input))
            .ToList();

        if (prefixMatches.Count == 1)
            return prefixMatches[0];

        if (prefixMatches.Count > 1)
            return prefixMatches.OrderBy(k => k.Length).First();

        // 2. Levenshtein fallback
        string? best = null;
        int bestScore = int.MaxValue;

        foreach (var kv in options)
        {
            var name = kv.Key;
            var desc = kv.Value.Description.ToLower();

            int scoreName = Distance(input, name);

            int scoreDesc = int.MaxValue;
            foreach (var word in desc.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                int s = Distance(input, word);
                if (s < scoreDesc) scoreDesc = s;
            }

            int score = Math.Min(scoreName, scoreDesc);

            if (score < bestScore)
            {
                bestScore = score;
                best = name;
            }
        }

        return bestScore <= 3 ? best : null;
    }


    // ------------------------------
    // ⭐ Suggest flags
    // ------------------------------
    private static string? SuggestFlag(string input, Command cmd)
    {
        string? best = null;
        int bestScore = int.MaxValue;

        foreach (var flag in cmd.SupportedFlags.Keys)
        {
            int score = Distance(input, flag);
            if (score < bestScore)
            {
                bestScore = score;
                best = flag;
            }
        }

        return bestScore <= 3 ? best : null;
    }

    public void Run(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        parts[0] = parts[0].ToLower();

        // Unknown command → suggest
        if (!_commands.TryGetValue(parts[0], out var cmd))
        {
            var suggestion = Suggest(parts[0], _commands);

            if (suggestion != null)
                System.Console.WriteLine($"Unknown command '{parts[0]}'. Did you mean '{suggestion}'?");
            else
                System.Console.WriteLine("Unknown command.");

            return;
        }

        // Subcommand?
        if (parts.Length > 1)
        {
            parts[1] = parts[1].ToLower();

            if (!cmd.Subcommands.TryGetValue(parts[1], out var sub))
            {
                var suggestion = Suggest(parts[1], cmd.Subcommands);

                if (suggestion != null)
                    System.Console.WriteLine($"Unknown subcommand '{parts[1]}'. Did you mean '{suggestion}'?");
                else
                    System.Console.WriteLine("Unknown subcommand.");

                return;
            }

            var (args, flags) = Parse(sub, parts[2..]);
            SafeExecute(sub, args, flags);
            return;
        }

        // No subcommand
        {
            var (args, flags) = Parse(cmd, parts[1..]);
            SafeExecute(cmd, args, flags);
        }
    }

    private static void SafeExecute(Command cmd, string[] args, Dictionary<string, string?> flags)
    {
        // Abort if parser signaled an error
        if (flags.ContainsKey("__error"))
            return;

        try { cmd.Execute(args, flags); }
        catch { }
    }

    private static (string[] args, Dictionary<string, string?> flags)
        Parse(Command cmd, string[] parts)
    {
        var args = new List<string>();
        var flags = new Dictionary<string, string?>();

        if (parts == null || parts.Length == 0)
            return (Array.Empty<string>(), flags);

        for (int i = 0; i < parts.Length; i++)
        {
            var p = parts[i];

            if (string.IsNullOrWhiteSpace(p))
                continue;

            if (p.StartsWith("--"))
            {
                var name = p[2..].ToLower();

                if (!cmd.SupportedFlags.ContainsKey(name))
                {
                    var suggestion = SuggestFlag(name, cmd);

                    if (suggestion != null)
                        System.Console.WriteLine($"Unknown flag '--{name}'. Did you mean '--{suggestion}'?");
                    else
                        System.Console.WriteLine($"Unknown flag '--{name}'.");

                    // Abort parsing and signal failure
                    return (Array.Empty<string>(), new Dictionary<string, string?> { ["__error"] = "flag" });
                }

                string? value = null;

                if (i + 1 < parts.Length && !parts[i + 1].StartsWith("--"))
                {
                    value = parts[i + 1];
                    i++;
                }

                flags[name] = value;
            }
            else
            {
                args.Add(p);
            }
        }

        return (args.ToArray(), flags);
    }

    public IReadOnlyDictionary<string, Command> Commands => _commands;
    public IEnumerable<Command> AllCommands => _commands.Values;
}