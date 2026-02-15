using System;
using System.Collections.Generic;
using System.IO;
using CRAB.ProjectSystem;

// Test script to demonstrate NuGet integration
public class NuGetIntegrationDemo
{
    public static void Main()
    {
        Console.WriteLine("=== NuGet Integration Demo ===\n");
        
        // 1. Parse project file
        var projectPath = "Testing/NuGetTest/NuGetTest.csproj";
        if (!File.Exists(projectPath))
        {
            projectPath = "NuGetTest.csproj";
        }
        
        Console.WriteLine($"1. Parsing project file: {projectPath}");
        var project = ProjectFile.Parse(projectPath);
        
        Console.WriteLine($"   Target Framework: {project.TargetFramework}");
        Console.WriteLine($"   Package References: {project.PackageReferences.Count}");
        
        foreach (var pkg in project.PackageReferences)
        {
            Console.WriteLine($"     - {pkg.Name} v{pkg.Version}");
        }
        
        // 2. Resolve NuGet packages
        Console.WriteLine("\n2. Resolving NuGet packages...");
        var resolver = new NuGetResolver();
        var globalPkgPath = resolver.GetGlobalPackagesPath();
        Console.WriteLine($"   Global packages path: {globalPkgPath}");
        
        var resolved = resolver.ResolveAllPackages(project);
        Console.WriteLine($"   Resolved {resolved.Count} packages:");
        
        foreach (var kvp in resolved)
        {
            Console.WriteLine($"     {kvp.Key}:");
            foreach (var assembly in kvp.Value)
            {
                Console.WriteLine($"       - {Path.GetFileName(assembly)}");
            }
        }
        
        // 3. Read assembly metadata
        Console.WriteLine("\n3. Reading assembly metadata...");
        var metadataReader = new AssemblyMetadataReader();
        
        foreach (var kvp in resolved)
        {
            foreach (var assemblyPath in kvp.Value)
            {
                var types = metadataReader.ReadAssembly(assemblyPath);
                if (types.Count > 0)
                {
                    Console.WriteLine($"   {Path.GetFileName(assemblyPath)}: {types.Count} types");
                    
                    // Show first 3 types
                    foreach (var type in types.Take(3))
                    {
                        Console.WriteLine($"     - {type.FullName}");
                    }
                    if (types.Count > 3)
                    {
                        Console.WriteLine($"     ... and {types.Count - 3} more");
                    }
                }
            }
        }
        
        // 4. Build namespace map
        Console.WriteLine("\n4. Building namespace map...");
        var allTypes = new Dictionary<string, List<TypeInfo>>();
        
        foreach (var kvp in resolved)
        {
            foreach (var assemblyPath in kvp.Value)
            {
                var types = metadataReader.ReadAssembly(assemblyPath);
                if (types.Count > 0)
                {
                    allTypes[assemblyPath] = types;
                }
            }
        }
        
        var namespaceMap = metadataReader.BuildNamespaceMap(allTypes);
        Console.WriteLine($"   Found {namespaceMap.Count} namespaces");
        
        foreach (var ns in namespaceMap.Keys.Take(5))
        {
            Console.WriteLine($"     - {ns} ({namespaceMap[ns].Count} types)");
        }
        if (namespaceMap.Count > 5)
        {
            Console.WriteLine($"     ... and {namespaceMap.Count - 5} more");
        }
        
        // 5. Test using directive resolution
        Console.WriteLine("\n5. Testing using directive resolution...");
        var nsResolver = new NamespaceResolver(namespaceMap);
        
        // Simulate adding using directives
        Console.WriteLine("   (Note: In actual use, using directives are extracted from AST)");
        Console.WriteLine("   Example using directives:");
        Console.WriteLine("     using System;");
        Console.WriteLine("     using Newtonsoft.Json;");
        
        Console.WriteLine("\n=== Demo Complete ===");
    }
}
