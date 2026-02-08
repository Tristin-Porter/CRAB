namespace CRAB
{
    class Help : Command
    {
        public Help()
        {
            Name = "help";
            Description = "Display help information about commands.";
        }
        public override void Execute(string[] args, Dictionary<string, string?> flags)
        {
            if (args.Length == 0)
            {
                System.Console.WriteLine("Available commands:");
                foreach (var cmd in Registry.Instance.Commands.Values)
                {
                    System.Console.WriteLine($"  {cmd.Name} - {cmd.Description}");
                }
                System.Console.WriteLine("Use 'help [command]' for more details on a specific command.");
            }
            else
            {
                string commandName = args[0];
                if (Registry.Instance.Commands.TryGetValue(commandName, out var cmd))
                {
                    System.Console.WriteLine($"{cmd.Name} - {cmd.Description}");
                    if (cmd.SupportedFlags.Count > 0)
                    {
                        System.Console.WriteLine("Supported flags:");
                        foreach (var flag in cmd.SupportedFlags)
                        {
                            System.Console.WriteLine($"  --{flag.Key} - {flag.Value}");
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("No flags supported for this command.");
                    }
                }
                else
                {
                    System.Console.WriteLine($"Command '{commandName}' not found.");
                }
            }
        }
    }
}