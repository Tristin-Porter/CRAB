using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CRAB.ProjectSystem;

/// <summary>
/// Represents a parsed Visual Studio solution file (.sln).
/// </summary>
public class SolutionFile
{
    public string FilePath { get; set; } = "";
    public string FormatVersion { get; set; } = "";
    public string VisualStudioVersion { get; set; } = "";
    public string MinimumVisualStudioVersion { get; set; } = "";
    public List<SolutionProject> Projects { get; set; } = new();
    public Dictionary<string, string> GlobalProperties { get; set; } = new();
    
    /// <summary>
    /// Parse a .sln file and extract project information.
    /// </summary>
    public static SolutionFile Parse(string filePath)
    {
        var solution = new SolutionFile { FilePath = filePath };
        var lines = File.ReadAllLines(filePath);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            
            // Parse header information
            if (line.StartsWith("Microsoft Visual Studio Solution File"))
            {
                // Extract format version from ", Format Version X.XX"
                var parts = line.Split(',');
                if (parts.Length > 1)
                {
                    var versionPart = parts[1].Trim();
                    if (versionPart.StartsWith("Format Version"))
                    {
                        solution.FormatVersion = versionPart.Replace("Format Version", "").Trim();
                    }
                }
            }
            else if (line.StartsWith("VisualStudioVersion"))
            {
                var parts = line.Split('=');
                if (parts.Length > 1)
                {
                    solution.VisualStudioVersion = parts[1].Trim();
                }
            }
            else if (line.StartsWith("MinimumVisualStudioVersion"))
            {
                var parts = line.Split('=');
                if (parts.Length > 1)
                {
                    solution.MinimumVisualStudioVersion = parts[1].Trim();
                }
            }
            // Parse project entries
            else if (line.StartsWith("Project("))
            {
                var project = ParseProjectLine(line, Path.GetDirectoryName(filePath) ?? "");
                if (project != null)
                {
                    solution.Projects.Add(project);
                }
            }
        }
        
        return solution;
    }
    
    private static SolutionProject? ParseProjectLine(string line, string solutionDir)
    {
        try
        {
            // Format: Project("{TYPE-GUID}") = "Name", "Path", "{Project-GUID}"
            // Example: Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CRAB", "CRAB.csproj", "{7479FA3A-EC7B-1F5D-752E-D5AF27F00F65}"
            
            // Extract all quoted strings
            var quotedParts = new List<string>();
            var inQuote = false;
            var currentQuoted = "";
            
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == '"')
                {
                    if (inQuote)
                    {
                        quotedParts.Add(currentQuoted);
                        currentQuoted = "";
                        inQuote = false;
                    }
                    else
                    {
                        inQuote = true;
                    }
                }
                else if (inQuote)
                {
                    currentQuoted += line[i];
                }
            }
            
            // We expect at least 2 quoted strings: name and path
            // quotedParts[0] might be the type GUID, then name, then path
            if (quotedParts.Count >= 2)
            {
                // Find the name and path (skip GUIDs)
                string? name = null;
                string? relativePath = null;
                
                for (int i = 0; i < quotedParts.Count; i++)
                {
                    var part = quotedParts[i];
                    
                    // Skip GUIDs (they contain dashes and braces)
                    if (part.StartsWith("{") || part.Contains("-"))
                        continue;
                    
                    // First non-GUID is the name
                    if (name == null)
                    {
                        name = part;
                        continue;
                    }
                    
                    // Second non-GUID is the path (ends with .csproj, .vbproj, etc.)
                    if (relativePath == null && (part.EndsWith(".csproj") || part.EndsWith(".vbproj") || 
                                                  part.EndsWith(".fsproj") || part.EndsWith(".vcxproj")))
                    {
                        relativePath = part;
                        break;
                    }
                }
                
                if (name != null && relativePath != null)
                {
                    var absolutePath = relativePath;
                    
                    // Make absolute path if relative and solutionDir is provided
                    if (!string.IsNullOrEmpty(solutionDir) && !Path.IsPathRooted(relativePath))
                    {
                        absolutePath = Path.Combine(solutionDir, relativePath);
                    }
                    
                    // Extract project GUID (last GUID in curly braces)
                    var lastGuidStart = line.LastIndexOf("{");
                    var lastGuidEnd = line.LastIndexOf("}");
                    var projectGuid = "";
                    if (lastGuidStart >= 0 && lastGuidEnd > lastGuidStart)
                    {
                        projectGuid = line.Substring(lastGuidStart, lastGuidEnd - lastGuidStart + 1);
                    }
                    
                    // Extract type GUID (first GUID in curly braces)
                    var firstGuidStart = line.IndexOf("{");
                    var firstGuidEnd = line.IndexOf("}");
                    var typeGuid = "";
                    if (firstGuidStart >= 0 && firstGuidEnd > firstGuidStart)
                    {
                        typeGuid = line.Substring(firstGuidStart, firstGuidEnd - firstGuidStart + 1);
                    }
                    
                    return new SolutionProject
                    {
                        TypeGuid = typeGuid,
                        Name = name,
                        RelativePath = relativePath,
                        AbsolutePath = absolutePath,
                        ProjectGuid = projectGuid
                    };
                }
            }
        }
        catch
        {
            // Ignore parsing errors for individual project lines
        }
        
        return null;
    }
}

/// <summary>
/// Represents a project reference in a solution file.
/// </summary>
public class SolutionProject
{
    public string TypeGuid { get; set; } = "";
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";
    public string AbsolutePath { get; set; } = "";
    public string ProjectGuid { get; set; } = "";
    
    public bool IsCSProject => RelativePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase);
    public bool IsFSharpProject => RelativePath.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase);
    public bool IsVBProject => RelativePath.EndsWith(".vbproj", StringComparison.OrdinalIgnoreCase);
}
