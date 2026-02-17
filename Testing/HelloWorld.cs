using System.IO;
using System.Text;

namespace CRAB.Testing;

/// <summary>
/// HelloWorld test project generator.
/// Generates a simple HelloWorld C# project for testing CRAB compilation.
/// </summary>
public static class HelloWorld
{
    /// <summary>
    /// Generates the HelloWorld test project in the current working directory.
    /// Creates: HelloWorld/Program.cs
    /// </summary>
    public static void GenerateProject()
    {
        string workingDir = Directory.GetCurrentDirectory();
        string projectDir = Path.Combine(workingDir, "HelloWorld");
        
        // Create project directory
        Directory.CreateDirectory(projectDir);
        
        // Generate Program.cs
        string programPath = Path.Combine(projectDir, "Program.cs");
        string programContent = GenerateProgramContent();
        File.WriteAllText(programPath, programContent);
    }
    
    private static string GenerateProgramContent()
    {
        return @"using System;

class Program
{
    static void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}
";
    }
}
