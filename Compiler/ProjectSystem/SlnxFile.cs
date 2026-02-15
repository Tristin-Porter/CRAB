using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace CRAB.ProjectSystem;

/// <summary>
/// Represents a parsed XML-based solution file (.slnx).
/// This is a newer format introduced in Visual Studio 2022.
/// </summary>
public class SlnxFile
{
    public string FilePath { get; set; } = "";
    public string Version { get; set; } = "";
    public List<SlnxProject> Projects { get; set; } = new();
    public Dictionary<string, string> Properties { get; set; } = new();
    
    /// <summary>
    /// Parse a .slnx file and extract project information.
    /// </summary>
    public static SlnxFile Parse(string filePath)
    {
        var solution = new SlnxFile { FilePath = filePath };
        var solutionDir = Path.GetDirectoryName(filePath) ?? "";
        
        try
        {
            var doc = XDocument.Load(filePath);
            var root = doc.Root;
            
            if (root == null) return solution;
            
            // Get version from root element
            var versionAttr = root.Attribute("Version");
            if (versionAttr != null)
            {
                solution.Version = versionAttr.Value;
            }
            
            // Parse Project elements
            foreach (var projectElement in root.Descendants("Project"))
            {
                var path = projectElement.Attribute("Path")?.Value;
                var name = projectElement.Attribute("Name")?.Value;
                var type = projectElement.Attribute("Type")?.Value;
                var id = projectElement.Attribute("Id")?.Value;
                
                if (!string.IsNullOrEmpty(path))
                {
                    var project = new SlnxProject
                    {
                        Name = name ?? Path.GetFileNameWithoutExtension(path),
                        RelativePath = path,
                        AbsolutePath = Path.Combine(solutionDir, path),
                        Type = type ?? "",
                        Id = id ?? ""
                    };
                    
                    solution.Projects.Add(project);
                }
            }
            
            // Parse Properties if any
            foreach (var propertyElement in root.Descendants("Property"))
            {
                var nameAttr = propertyElement.Attribute("Name")?.Value;
                var valueAttr = propertyElement.Attribute("Value")?.Value ?? propertyElement.Value;
                
                if (!string.IsNullOrEmpty(nameAttr) && !string.IsNullOrEmpty(valueAttr))
                {
                    solution.Properties[nameAttr] = valueAttr;
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Error parsing .slnx file {filePath}: {ex.Message}");
        }
        
        return solution;
    }
}

/// <summary>
/// Represents a project reference in a .slnx file.
/// </summary>
public class SlnxProject
{
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string AbsolutePath { get; set; } = "";
    public string Type { get; set; } = "";
    public string Id { get; set; } = "";
    
    public bool IsCSProject => RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    public bool IsFSharpProject => RelativePath.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase);
    public bool IsVBProject => RelativePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase);
}
