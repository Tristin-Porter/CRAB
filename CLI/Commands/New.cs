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

        try
        {
            System.Console.WriteLine($"Creating a new console application '{name}'...");
            
            // Create project directory
            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), name);
            if (Directory.Exists(projectPath))
            {
                System.Console.WriteLine($"Error: Directory '{name}' already exists.");
                return;
            }
            
            Directory.CreateDirectory(projectPath);
            
            // Create Program.cs with console template
            // Note: Limited to parseable constructs due to CDTk GLL parser bug
            // with expression delegation (see STATEMENT_PARSING_INVESTIGATION.md)
            // Can parse method declarations but not expression/statement bodies yet.
            var programContent = @"using System;

namespace " + name + @"
{
    class Program
    {
        static void Main()
        {
        }
        
        int Add(int x, int y)
        {
        }
        
        void Process()
        {
        }
    }
    
    class Calculator
    {
        int Multiply(int a, int b)
        {
        }
    }
}
";
            File.WriteAllText(Path.Combine(projectPath, "Program.cs"), programContent);
            
            // Create .crab project file
            var projectFileContent = @"{
  ""name"": """ + name + @""",
  ""type"": ""console"",
  ""output"": ""bin"",
  ""sources"": [""*.cs""]
}
";
            File.WriteAllText(Path.Combine(projectPath, $"{name}.crab"), projectFileContent);
            
            System.Console.WriteLine($"✓ Created {name}/");
            System.Console.WriteLine($"✓ Created {name}/Program.cs");
            System.Console.WriteLine($"✓ Created {name}/{name}.crab");
            System.Console.WriteLine();
            System.Console.WriteLine("Next steps:");
            System.Console.WriteLine($"  cd {name}");
            System.Console.WriteLine("  crab build");
            System.Console.WriteLine("  crab run bin/Program.wat");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error creating console application: {ex.Message}");
        }
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

        try
        {
            System.Console.WriteLine($"Creating a new empty project '{name}'...");
            
            // Create project directory
            var projectPath = Path.Combine(Directory.GetCurrentDirectory(), name);
            if (Directory.Exists(projectPath))
            {
                System.Console.WriteLine($"Error: Directory '{name}' already exists.");
                return;
            }
            
            Directory.CreateDirectory(projectPath);
            Directory.CreateDirectory(Path.Combine(projectPath, "src"));
            
            // Create .crab project file
            var projectFileContent = @"{
  ""name"": """ + name + @""",
  ""type"": ""library"",
  ""output"": ""bin"",
  ""sources"": [""src/*.cs""]
}
";
            File.WriteAllText(Path.Combine(projectPath, $"{name}.crab"), projectFileContent);
            
            // Create README.md
            var readmeContent = $@"# {name}

A CRAB project.

## Building

```bash
crab build
```

## Structure

- `src/` - Source files
- `bin/` - Build output
";
            File.WriteAllText(Path.Combine(projectPath, "README.md"), readmeContent);
            
            System.Console.WriteLine($"✓ Created {name}/");
            System.Console.WriteLine($"✓ Created {name}/src/");
            System.Console.WriteLine($"✓ Created {name}/{name}.crab");
            System.Console.WriteLine($"✓ Created {name}/README.md");
            System.Console.WriteLine();
            System.Console.WriteLine("Next steps:");
            System.Console.WriteLine($"  cd {name}");
            System.Console.WriteLine("  # Add your .cs files to src/");
            System.Console.WriteLine("  crab build");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error creating project: {ex.Message}");
        }
    }
}
