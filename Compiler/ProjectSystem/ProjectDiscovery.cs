using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CRAB.ProjectSystem;

/// <summary>
/// Discovers and loads project/solution files, extracting source files to compile.
/// Supports .sln, .slnx, and .csproj files.
/// </summary>
public class ProjectDiscovery
{
    /// <summary>
    /// Discover projects in the given directory or from a specific solution/project file.
    /// </summary>
    public static DiscoveryResult Discover(string path)
    {
        var result = new DiscoveryResult();
        
        if (File.Exists(path))
        {
            // Specific file provided
            if (path.EndsWith(".sln", StringComparison.OrdinalIgnoreCase))
            {
                result = DiscoverFromSolution(path);
            }
            else if (path.EndsWith(".slnx", StringComparison.OrdinalIgnoreCase))
            {
                result = DiscoverFromSlnx(path);
            }
            else if (path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            {
                result = DiscoverFromProject(path);
            }
            else if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                // Single source file
                result.SourceFiles.Add(path);
                result.ProjectType = "SingleFile";
            }
        }
        else if (Directory.Exists(path))
        {
            // Directory provided - search for solution or project files
            result = DiscoverFromDirectory(path);
        }
        
        return result;
    }
    
    private static DiscoveryResult DiscoverFromDirectory(string directory)
    {
        var result = new DiscoveryResult();
        
        // Priority 1: Look for .sln files
        var slnFiles = Directory.GetFiles(directory, "*.sln", SearchOption.TopDirectoryOnly);
        if (slnFiles.Length > 0)
        {
            return DiscoverFromSolution(slnFiles[0]);
        }
        
        // Priority 2: Look for .slnx files
        var slnxFiles = Directory.GetFiles(directory, "*.slnx", SearchOption.TopDirectoryOnly);
        if (slnxFiles.Length > 0)
        {
            return DiscoverFromSlnx(slnxFiles[0]);
        }
        
        // Priority 3: Look for .csproj files
        var csprojFiles = Directory.GetFiles(directory, "*.csproj", SearchOption.TopDirectoryOnly);
        if (csprojFiles.Length > 0)
        {
            return DiscoverFromProject(csprojFiles[0]);
        }
        
        // Fallback: Gather all .cs files
        var csFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(f =>
            {
                // Get path relative to directory and normalize separators
                var relativePath = Path.GetRelativePath(directory, f).Replace('\\', '/');
                // Exclude files in obj or bin subdirectories (relative to directory root)
                return !relativePath.StartsWith("obj/") && !relativePath.StartsWith("bin/");
            })
            .ToList();
        
        result.SourceFiles.AddRange(csFiles);
        result.ProjectType = "Directory";
        
        return result;
    }
    
    private static DiscoveryResult DiscoverFromSolution(string solutionPath)
    {
        var result = new DiscoveryResult
        {
            SolutionFile = solutionPath,
            ProjectType = "Solution"
        };
        
        var solution = SolutionFile.Parse(solutionPath);
        var solutionDir = Path.GetDirectoryName(solutionPath);
        if (string.IsNullOrEmpty(solutionDir))
        {
            solutionDir = ".";
        }
        
        // Get all C# projects
        var csProjects = solution.Projects.Where(p => p.IsCSProject).ToList();
        
        foreach (var slnProject in csProjects)
        {
            // Make absolute path if it's relative
            var projectPath = slnProject.AbsolutePath;
            if (!Path.IsPathRooted(projectPath))
            {
                projectPath = Path.Combine(solutionDir, projectPath);
            }
            
            if (File.Exists(projectPath))
            {
                result.ProjectFiles.Add(projectPath);
                
                var project = ProjectFile.Parse(projectPath);
                result.SourceFiles.AddRange(project.SourceFiles);
                
                // Add referenced projects recursively
                foreach (var refPath in project.ProjectReferences)
                {
                    if (File.Exists(refPath) && !result.ProjectFiles.Contains(refPath))
                    {
                        result.ProjectFiles.Add(refPath);
                        var refProject = ProjectFile.Parse(refPath);
                        result.SourceFiles.AddRange(refProject.SourceFiles);
                    }
                }
            }
        }
        
        return result;
    }
    
    private static DiscoveryResult DiscoverFromSlnx(string slnxPath)
    {
        var result = new DiscoveryResult
        {
            SolutionFile = slnxPath,
            ProjectType = "SlnxSolution"
        };
        
        var solution = SlnxFile.Parse(slnxPath);
        var solutionDir = Path.GetDirectoryName(slnxPath);
        if (string.IsNullOrEmpty(solutionDir))
        {
            solutionDir = ".";
        }
        
        // Get all C# projects
        var csProjects = solution.Projects.Where(p => p.IsCSProject).ToList();
        
        foreach (var slnxProject in csProjects)
        {
            // Make absolute path if it's relative
            var projectPath = slnxProject.AbsolutePath;
            if (!Path.IsPathRooted(projectPath))
            {
                projectPath = Path.Combine(solutionDir, projectPath);
            }
            
            if (File.Exists(projectPath))
            {
                result.ProjectFiles.Add(projectPath);
                
                var project = ProjectFile.Parse(projectPath);
                result.SourceFiles.AddRange(project.SourceFiles);
                
                // Add referenced projects recursively
                foreach (var refPath in project.ProjectReferences)
                {
                    if (File.Exists(refPath) && !result.ProjectFiles.Contains(refPath))
                    {
                        result.ProjectFiles.Add(refPath);
                        var refProject = ProjectFile.Parse(refPath);
                        result.SourceFiles.AddRange(refProject.SourceFiles);
                    }
                }
            }
        }
        
        return result;
    }
    
    private static DiscoveryResult DiscoverFromProject(string projectPath)
    {
        var result = new DiscoveryResult
        {
            ProjectType = "Project"
        };
        
        result.ProjectFiles.Add(projectPath);
        
        var project = ProjectFile.Parse(projectPath);
        result.SourceFiles.AddRange(project.SourceFiles);
        
        // Add referenced projects recursively
        foreach (var refPath in project.ProjectReferences)
        {
            if (File.Exists(refPath) && !result.ProjectFiles.Contains(refPath))
            {
                result.ProjectFiles.Add(refPath);
                var refProject = ProjectFile.Parse(refPath);
                result.SourceFiles.AddRange(refProject.SourceFiles);
            }
        }
        
        return result;
    }
}

/// <summary>
/// Result of project discovery containing all discovered files.
/// </summary>
public class DiscoveryResult
{
    public string ProjectType { get; set; } = "";
    public string SolutionFile { get; set; } = "";
    public List<string> ProjectFiles { get; set; } = new();
    public List<string> SourceFiles { get; set; } = new();
    
    public bool HasProjects => ProjectFiles.Count > 0 || !string.IsNullOrEmpty(SolutionFile);
}
