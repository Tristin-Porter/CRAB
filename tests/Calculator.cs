// CRAB Test File: Calculator
// This file contains embedded C#, .sln, and .slnx content

// === BEGIN CSHARP ===
using System;

namespace Calculator
{
    class Calculator
    {
        int Add(int a, int b)
        {
            return a + b;
        }
        
        int Multiply(int a, int b)
        {
            return a * b;
        }
        
        int Subtract(int a, int b)
        {
            return a - b;
        }
    }
    
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Calculator Test");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
// === END CSHARP ===

// === BEGIN SLN ===
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.0.0
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Calculator", "Calculator.csproj", "{22345678-1234-1234-1234-123456789ABC}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{22345678-1234-1234-1234-123456789ABC}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{22345678-1234-1234-1234-123456789ABC}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{22345678-1234-1234-1234-123456789ABC}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{22345678-1234-1234-1234-123456789ABC}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
EndGlobal
// === END SLN ===

// === BEGIN SLNX ===
<?xml version="1.0" encoding="utf-8"?>
<Solution Version="1.0">
  <Properties>
    <Name>Calculator</Name>
  </Properties>
  <Project Path="Calculator.csproj" Name="Calculator" Type="C#" Id="{22345678-1234-1234-1234-123456789ABC}" />
</Solution>
// === END SLNX ===
