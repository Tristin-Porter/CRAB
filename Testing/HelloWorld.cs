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
    /// Creates: HelloWorld/HelloWorld.slnx, HelloWorld/HelloWorld.sln, HelloWorld/Program.cs
    /// </summary>
    public static void GenerateProject()
    {
        string workingDir = Directory.GetCurrentDirectory();
        string projectDir = Path.Combine(workingDir, "HelloWorld");
        
        // Create project directory
        Directory.CreateDirectory(projectDir);
        
        // Generate HelloWorld.slnx
        string slnxPath = Path.Combine(projectDir, "HelloWorld.slnx");
        string slnxContent = GenerateSlnxContent();
        File.WriteAllText(slnxPath, slnxContent);
        
        // Generate HelloWorld.sln
        string slnPath = Path.Combine(projectDir, "HelloWorld.sln");
        string slnContent = GenerateSlnContent();
        File.WriteAllText(slnPath, slnContent);
        
        // Generate Program.cs
        string programPath = Path.Combine(projectDir, "Program.cs");
        string programContent = GenerateProgramContent();
        File.WriteAllText(programPath, programContent);
    }
    
    private static string GenerateSlnxContent()
    {
        return @"<Solution>
  <Project Path=""HelloWorld.csproj"" />
</Solution>";
    }
    
    private static string GenerateSlnContent()
    {
        return @"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""HelloWorld"", ""HelloWorld.csproj"", ""{12345678-1234-1234-1234-123456789012}""
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{12345678-1234-1234-1234-123456789012}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{12345678-1234-1234-1234-123456789012}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{12345678-1234-1234-1234-123456789012}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{12345678-1234-1234-1234-123456789012}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
EndGlobal
".TrimStart();
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
