namespace CRAB;

class New : Command
{
    public New()
    {
        Name = "new";
        Description = "Create a new item.";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        System.Console.WriteLine("You must specify what to create (console, project).");
    }
}

class Console : Command
{
    public Console()
    {
        Name = "Console";
        Description = "Create a new console application.";

        SupportedFlags["name"] = "Name of the console application.";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        string? name = null;

        if (flags.TryGetValue("name", out var flagName))
            name = flagName;
        else if (args.Length > 0)
            name = args[0];

        if (string.IsNullOrWhiteSpace(name))
        {
            System.Console.WriteLine("Console application name required.");
            return;
        }

        System.Console.WriteLine($"Creating a new console application '{name}'...");
    }
}

class Project : Command
{
    public Project()
    {
        Name = "Project";
        Description = "Create a new empty project.";

        SupportedFlags["name"] = "Name of the project.";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        string? name = null;

        if (flags.TryGetValue("name", out var flagName))
            name = flagName;
        else if (args.Length > 0)
            name = args[0];

        if (string.IsNullOrWhiteSpace(name))
        {
            System.Console.WriteLine("Project name required.");
            return;
        }

        System.Console.WriteLine($"Creating a new empty project '{name}'...");
    }
}
