// CRAB Test File: ClassHierarchy
// This file contains embedded C#, .sln, and .slnx content

// === BEGIN CSHARP ===
using System;

namespace ClassHierarchy
{
    class Base
    {
        int GetBase()
        {
            return 10;
        }
    }
    
    class Derived
    {
        int GetValue()
        {
            return 20;
        }
    }
    
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Class Hierarchy Test");
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
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "ClassHierarchy", "ClassHierarchy.csproj", "{32345678-1234-1234-1234-123456789ABC}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{32345678-1234-1234-1234-123456789ABC}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{32345678-1234-1234-1234-123456789ABC}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{32345678-1234-1234-1234-123456789ABC}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{32345678-1234-1234-1234-123456789ABC}.Release|Any CPU.Build.0 = Release|Any CPU
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
    <Name>ClassHierarchy</Name>
  </Properties>
  <Project Path="ClassHierarchy.csproj" Name="ClassHierarchy" Type="C#" Id="{32345678-1234-1234-1234-123456789ABC}" />
</Solution>
// === END SLNX ===
