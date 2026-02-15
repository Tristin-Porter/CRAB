using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CRAB.ProjectSystem;

/// <summary>
/// Represents a NuGet package reference.
/// </summary>
public class PackageReference
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public bool IsImplicit { get; set; } = false;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Represents a parsed C# project file (.csproj).
/// </summary>
public class ProjectFile
{
    public string FilePath { get; set; } = "";
    public string TargetFramework { get; set; } = "";
    public List<string> TargetFrameworks { get; set; } = new();
    public string OutputType { get; set; } = "";
    public List<string> SourceFiles { get; set; } = new();
    public List<string> ProjectReferences { get; set; } = new();
    public List<PackageReference> PackageReferences { get; set; } = new();
    public Dictionary<string, string> Properties { get; set; } = new();
    
    /// <summary>
    /// Parse a .csproj file and extract build information.
    /// </summary>
    public static ProjectFile Parse(string filePath)
    {
        var project = new ProjectFile { FilePath = filePath };
        var projectDir = Path.GetDirectoryName(filePath) ?? "";
        
        try
        {
            var doc = XDocument.Load(filePath);
            var root = doc.Root;
            
            if (root == null) return project;
            
            // Parse PropertyGroup elements
            foreach (var propertyGroup in root.Elements("PropertyGroup"))
            {
                // Target framework(s)
                var targetFramework = propertyGroup.Element("TargetFramework")?.Value;
                if (!string.IsNullOrEmpty(targetFramework))
                {
                    project.TargetFramework = targetFramework;
                    project.TargetFrameworks.Add(targetFramework);
                }
                
                var targetFrameworks = propertyGroup.Element("TargetFrameworks")?.Value;
                if (!string.IsNullOrEmpty(targetFrameworks))
                {
                    project.TargetFrameworks.AddRange(targetFrameworks.Split(';', StringSplitOptions.RemoveEmptyEntries));
                }
                
                // Output type
                var outputType = propertyGroup.Element("OutputType")?.Value;
                if (!string.IsNullOrEmpty(outputType))
                {
                    project.OutputType = outputType;
                }
                
                // Store all properties
                foreach (var element in propertyGroup.Elements())
                {
                    if (!string.IsNullOrEmpty(element.Value))
                    {
                        project.Properties[element.Name.LocalName] = element.Value;
                    }
                }
            }
            
            // Parse ItemGroup elements for source files and references
            var compileExclusions = new List<string>();
            
            foreach (var itemGroup in root.Elements("ItemGroup"))
            {
                // Compile Remove items (exclusions)
                foreach (var compileRemove in itemGroup.Elements("Compile").Where(e => e.Attribute("Remove") != null))
                {
                    var remove = compileRemove.Attribute("Remove")?.Value;
                    if (!string.IsNullOrEmpty(remove))
                    {
                        compileExclusions.Add(remove);
                    }
                }
                
                // Compile items (source files)
                foreach (var compile in itemGroup.Elements("Compile").Where(e => e.Attribute("Include") != null))
                {
                    var include = compile.Attribute("Include")?.Value;
                    if (!string.IsNullOrEmpty(include))
                    {
                        var absolutePath = Path.Combine(projectDir, include);
                        project.SourceFiles.Add(absolutePath);
                    }
                }
                
                // Project references
                foreach (var projectRef in itemGroup.Elements("ProjectReference"))
                {
                    var include = projectRef.Attribute("Include")?.Value;
                    if (!string.IsNullOrEmpty(include))
                    {
                        var absolutePath = Path.Combine(projectDir, include);
                        project.ProjectReferences.Add(absolutePath);
                    }
                }
                
                // Package references (NuGet packages)
                foreach (var packageRef in itemGroup.Elements("PackageReference"))
                {
                    var include = packageRef.Attribute("Include")?.Value;
                    var version = packageRef.Attribute("Version")?.Value;
                    
                    if (!string.IsNullOrEmpty(include))
                    {
                        var package = new PackageReference
                        {
                            Name = include,
                            Version = version ?? "",
                            IsImplicit = false
                        };
                        
                        // Parse child elements for additional metadata
                        foreach (var element in packageRef.Elements())
                        {
                            if (!string.IsNullOrEmpty(element.Value))
                            {
                                package.Metadata[element.Name.LocalName] = element.Value;
                            }
                        }
                        
                        // Check if Version is a child element instead of attribute
                        if (string.IsNullOrEmpty(package.Version))
                        {
                            var versionElement = packageRef.Element("Version");
                            if (versionElement != null)
                            {
                                package.Version = versionElement.Value;
                            }
                        }
                        
                        project.PackageReferences.Add(package);
                    }
                }
            }
            
            // If no explicit Compile items, use SDK-style default (all .cs files)
            if (project.SourceFiles.Count == 0 && IsSdkStyleProject(doc))
            {
                // SDK-style projects automatically include all .cs files
                var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories)
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && 
                                !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                    .ToList();
                
                // Apply exclusions
                var filteredFiles = new List<string>();
                foreach (var file in csFiles)
                {
                    var relativePath = Path.GetRelativePath(projectDir, file);
                    // Normalize to forward slashes for consistent pattern matching
                    var normalizedPath = relativePath.Replace(Path.DirectorySeparatorChar, '/');
                    bool excluded = false;
                    
                    foreach (var exclusion in compileExclusions)
                    {
                        // Normalize pattern to forward slashes for consistent matching
                        var pattern = exclusion.Replace("**", "*").Replace("\\", "/");
                        
                        if (pattern.EndsWith("/*"))
                        {
                            // Directory exclusion
                            var dir = pattern.Substring(0, pattern.Length - 2);
                            if (normalizedPath.StartsWith(dir + "/", StringComparison.OrdinalIgnoreCase))
                            {
                                excluded = true;
                                break;
                            }
                        }
                        else if (pattern.Contains("*"))
                        {
                            // Glob pattern - simple contains check for now
                            var parts = pattern.Split('*');
                            bool matches = true;
                            foreach (var part in parts.Where(p => !string.IsNullOrEmpty(p)))
                            {
                                if (!normalizedPath.Contains(part, StringComparison.OrdinalIgnoreCase))
                                {
                                    matches = false;
                                    break;
                                }
                            }
                            if (matches)
                            {
                                excluded = true;
                                break;
                            }
                        }
                        else if (normalizedPath.Equals(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            excluded = true;
                            break;
                        }
                    }
                    
                    if (!excluded)
                    {
                        filteredFiles.Add(file);
                    }
                }
                
                project.SourceFiles.AddRange(filteredFiles);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Error parsing project file {filePath}: {ex.Message}");
        }
        
        return project;
    }
    
    private static bool IsSdkStyleProject(XDocument doc)
    {
        // SDK-style projects have Sdk attribute on Project element
        return doc.Root?.Attribute("Sdk") != null;
    }
}
