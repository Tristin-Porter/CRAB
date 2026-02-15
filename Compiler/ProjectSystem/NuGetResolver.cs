using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CRAB.ProjectSystem;

/// <summary>
/// Resolves NuGet package references to assembly paths.
/// </summary>
public class NuGetResolver
{
    private string? _globalPackagesPath;
    
    /// <summary>
    /// Get the global NuGet packages directory.
    /// </summary>
    public string GetGlobalPackagesPath()
    {
        if (_globalPackagesPath != null)
            return _globalPackagesPath;
        
        // Try environment variable first
        var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (!string.IsNullOrEmpty(nugetPackages) && Directory.Exists(nugetPackages))
        {
            _globalPackagesPath = nugetPackages;
            return _globalPackagesPath;
        }
        
        // Default locations based on OS
        string? defaultPath = null;
        if (OperatingSystem.IsWindows())
        {
            defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        }
        
        if (defaultPath != null && Directory.Exists(defaultPath))
        {
            _globalPackagesPath = defaultPath;
            return _globalPackagesPath;
        }
        
        // Fallback - try to find it
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var fallback = Path.Combine(home, ".nuget", "packages");
        _globalPackagesPath = fallback;
        return _globalPackagesPath;
    }
    
    /// <summary>
    /// Resolve a package reference to assembly paths.
    /// </summary>
    public List<string> ResolvePackage(PackageReference package, string targetFramework)
    {
        var assemblies = new List<string>();
        
        try
        {
            var globalPackages = GetGlobalPackagesPath();
            if (!Directory.Exists(globalPackages))
            {
                System.Console.WriteLine($"Warning: NuGet global packages directory not found: {globalPackages}");
                return assemblies;
            }
            
            // Package directory structure: {packageName}/{version}/lib/{framework}/*.dll
            var packageDir = Path.Combine(globalPackages, package.Name.ToLowerInvariant(), package.Version.ToLowerInvariant());
            
            if (!Directory.Exists(packageDir))
            {
                System.Console.WriteLine($"Warning: Package not found: {package.Name} {package.Version} at {packageDir}");
                return assemblies;
            }
            
            // Look for lib folder
            var libDir = Path.Combine(packageDir, "lib");
            if (!Directory.Exists(libDir))
            {
                // Some packages might have ref folder
                libDir = Path.Combine(packageDir, "ref");
            }
            
            if (!Directory.Exists(libDir))
            {
                System.Console.WriteLine($"Warning: No lib or ref folder found in package {package.Name}");
                return assemblies;
            }
            
            // Find best matching framework folder
            var frameworkDir = FindBestFrameworkMatch(libDir, targetFramework);
            if (frameworkDir == null)
            {
                System.Console.WriteLine($"Warning: No compatible framework found in package {package.Name} for {targetFramework}");
                return assemblies;
            }
            
            // Get all DLL files from the framework directory
            var dllFiles = Directory.GetFiles(frameworkDir, "*.dll", SearchOption.TopDirectoryOnly);
            assemblies.AddRange(dllFiles);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Error resolving package {package.Name}: {ex.Message}");
        }
        
        return assemblies;
    }
    
    /// <summary>
    /// Find the best matching framework folder for the target framework.
    /// </summary>
    private string? FindBestFrameworkMatch(string libDir, string targetFramework)
    {
        if (!Directory.Exists(libDir))
            return null;
        
        var frameworkFolders = Directory.GetDirectories(libDir);
        if (frameworkFolders.Length == 0)
            return null;
        
        // Extract framework version (e.g., "net8.0" -> 8.0, "netstandard2.0" -> 2.0)
        var targetVersion = ExtractFrameworkVersion(targetFramework);
        var isNetStandard = targetFramework.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase);
        var isNetCore = targetFramework.StartsWith("net", StringComparison.OrdinalIgnoreCase) && 
                       !targetFramework.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
                       !targetFramework.StartsWith("netframework", StringComparison.OrdinalIgnoreCase);
        
        // Try exact match first
        foreach (var folder in frameworkFolders)
        {
            var folderName = Path.GetFileName(folder);
            if (folderName.Equals(targetFramework, StringComparison.OrdinalIgnoreCase))
                return folder;
        }
        
        // Find compatible frameworks
        var compatibleFolders = new List<(string folder, double version)>();
        
        foreach (var folder in frameworkFolders)
        {
            var folderName = Path.GetFileName(folder);
            var version = ExtractFrameworkVersion(folderName);
            
            // Check compatibility
            bool compatible = false;
            
            if (isNetCore && folderName.StartsWith("net", StringComparison.OrdinalIgnoreCase) &&
                !folderName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) &&
                !folderName.StartsWith("netframework", StringComparison.OrdinalIgnoreCase))
            {
                // .NET Core/5+/6+/7+/8+ compatibility
                if (version <= targetVersion)
                    compatible = true;
            }
            else if (folderName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase))
            {
                // .NET Standard is compatible with .NET Core and .NET 5+
                if (version <= 2.1) // Most common .NET Standard versions
                    compatible = true;
            }
            
            if (compatible)
            {
                compatibleFolders.Add((folder, version));
            }
        }
        
        // Return the highest compatible version
        if (compatibleFolders.Count > 0)
        {
            return compatibleFolders.OrderByDescending(f => f.version).First().folder;
        }
        
        // Fallback: return any netstandard2.0 or netstandard2.1
        var netstandard = frameworkFolders.FirstOrDefault(f => 
            Path.GetFileName(f).Equals("netstandard2.0", StringComparison.OrdinalIgnoreCase) ||
            Path.GetFileName(f).Equals("netstandard2.1", StringComparison.OrdinalIgnoreCase));
        
        if (netstandard != null)
            return netstandard;
        
        // Last resort: return first folder
        return frameworkFolders[0];
    }
    
    /// <summary>
    /// Extract numeric version from framework string.
    /// </summary>
    private double ExtractFrameworkVersion(string framework)
    {
        // Extract numeric part (e.g., "net8.0" -> 8.0, "netstandard2.1" -> 2.1)
        var digits = new string(framework.Where(c => char.IsDigit(c) || c == '.').ToArray());
        
        if (double.TryParse(digits, out var version))
            return version;
        
        return 0.0;
    }
    
    /// <summary>
    /// Resolve all package references for a project.
    /// </summary>
    public Dictionary<string, List<string>> ResolveAllPackages(ProjectFile project)
    {
        var result = new Dictionary<string, List<string>>();
        
        var targetFramework = project.TargetFramework;
        if (string.IsNullOrEmpty(targetFramework) && project.TargetFrameworks.Count > 0)
        {
            targetFramework = project.TargetFrameworks[0];
        }
        
        if (string.IsNullOrEmpty(targetFramework))
        {
            System.Console.WriteLine("Warning: No target framework specified, using net8.0");
            targetFramework = "net8.0";
        }
        
        foreach (var package in project.PackageReferences)
        {
            var assemblies = ResolvePackage(package, targetFramework);
            if (assemblies.Count > 0)
            {
                result[package.Name] = assemblies;
            }
        }
        
        return result;
    }
}
