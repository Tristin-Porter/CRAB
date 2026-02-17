using System.IO;
using CRAB.Testing;

namespace CRAB;

/// <summary>
/// Test command - generates and builds the HelloWorld test project
/// </summary>
class Test : Command
{
    public Test()
    {
        Name = "test";
        Description = "Generate and build the HelloWorld test project.";
        
        SupportedFlags["verbose"] = "Enable verbose output.";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        bool verbose = flags.ContainsKey("verbose");
        
        System.Console.WriteLine("=".PadRight(70, '='));
        System.Console.WriteLine("CRAB Test - HelloWorld Project");
        System.Console.WriteLine("=".PadRight(70, '='));
        System.Console.WriteLine();
        
        // Generate the HelloWorld project
        System.Console.WriteLine("[1/2] Generating HelloWorld project...");
        try
        {
            HelloWorld.GenerateProject();
            System.Console.WriteLine("      ✓ Project generated successfully");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"      ✗ Failed to generate project: {ex.Message}");
            return;
        }
        
        // Build the HelloWorld project
        System.Console.WriteLine("\n[2/2] Building HelloWorld project...");
        
        string projectPath = Path.Combine(Directory.GetCurrentDirectory(), "HelloWorld");
        
        var buildCommand = new Build();
        var buildFlags = new Dictionary<string, string?>
        {
            ["project"] = projectPath,
            ["config"] = "debug"
        };
        
        if (verbose)
            buildFlags["verbose"] = null;
        
        buildCommand.Execute(Array.Empty<string>(), buildFlags);
        
        // Check if output was created
        string expectedOutput = Path.Combine(projectPath, "bin", "Debug", "crab1.0", "HelloWorld.wat");
        
        if (File.Exists(expectedOutput))
        {
            System.Console.WriteLine();
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine($"✓ Test completed successfully!");
            System.Console.WriteLine($"  Output: {expectedOutput}");
            System.Console.WriteLine("=".PadRight(70, '='));
        }
        else
        {
            System.Console.WriteLine();
            System.Console.WriteLine("=".PadRight(70, '='));
            System.Console.WriteLine("✗ Test failed - expected output not found");
            System.Console.WriteLine($"  Expected: {expectedOutput}");
            System.Console.WriteLine("=".PadRight(70, '='));
        }
    }
}
