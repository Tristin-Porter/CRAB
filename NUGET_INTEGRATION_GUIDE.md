# CRAB NuGet Integration and Using Directives Guide

## Overview

CRAB now has complete NuGet integration and full support for using directives, enabling you to:
- Reference NuGet packages in your projects
- Automatically resolve package dependencies
- Use types from external assemblies
- Resolve using directives to fully qualified type names
- Support C# 12 using alias features

## Features

### ✅ NuGet Package Support
- Parse `<PackageReference>` elements from .csproj files
- Resolve NuGet packages from the global packages cache
- Cross-platform package resolution (Windows, Linux, macOS)
- Framework-aware assembly selection (net8.0, netstandard2.0, etc.)
- Best framework matching algorithm
- Transitive dependency support (foundation in place)

### ✅ Assembly Metadata Reading
- Extract type information from .NET DLL assemblies
- Read namespaces, types, methods, properties, and fields
- Build comprehensive type catalogs
- Filter public API surfaces
- Handle System.Reflection.Metadata

### ✅ Using Directive Resolution
- Process `using namespace` directives
- Process `using alias` directives (C# 12 feature)
- Process `using static` directives
- Resolve type names from imported namespaces
- Validate using directives reference valid namespaces
- Support fully qualified type names

## Architecture

```
Project File (.csproj)
        ↓
ProjectFile.Parse()
        ↓
PackageReference List
        ↓
NuGetResolver.ResolveAllPackages()
        ↓
Assembly DLL Paths
        ↓
AssemblyMetadataReader.ReadAssemblies()
        ↓
Type Information (TypeInfo)
        ↓
Namespace → Types Mapping
        ↓
NamespaceResolver.ProcessUsingDirectives()
        ↓
Type Name Resolution
```

## Usage Examples

### 1. Parse Project File with NuGet References

```csharp
using CRAB.ProjectSystem;

// Parse .csproj file
var project = ProjectFile.Parse("MyProject.csproj");

// Access package references
Console.WriteLine($"Target Framework: {project.TargetFramework}");
Console.WriteLine($"Packages: {project.PackageReferences.Count}");

foreach (var package in project.PackageReferences)
{
    Console.WriteLine($"  - {package.Name} v{package.Version}");
}
```

### 2. Resolve NuGet Packages

```csharp
using CRAB.ProjectSystem;

var resolver = new NuGetResolver();

// Get global packages directory
var globalPath = resolver.GetGlobalPackagesPath();
// → ~/.nuget/packages (on Linux/macOS)
// → %USERPROFILE%\.nuget\packages (on Windows)

// Resolve all packages for a project
var assemblies = resolver.ResolveAllPackages(project);

foreach (var kvp in assemblies)
{
    Console.WriteLine($"{kvp.Key}:");
    foreach (var dll in kvp.Value)
    {
        Console.WriteLine($"  - {dll}");
    }
}
```

### 3. Read Assembly Metadata

```csharp
using CRAB.ProjectSystem;

var reader = new AssemblyMetadataReader();

// Read types from a single assembly
var types = reader.ReadAssembly("path/to/assembly.dll");

foreach (var type in types)
{
    Console.WriteLine($"{type.FullName}");
    Console.WriteLine($"  Namespace: {type.Namespace}");
    Console.WriteLine($"  Methods: {type.Methods.Count}");
    Console.WriteLine($"  Properties: {type.Properties.Count}");
}

// Build namespace map
var assemblies = new Dictionary<string, List<TypeInfo>>();
assemblies["assembly.dll"] = types;

var namespaceMap = reader.BuildNamespaceMap(assemblies);
// → { "System": [List<TypeInfo>], "Newtonsoft.Json": [List<TypeInfo>], ... }
```

### 4. Resolve Using Directives

```csharp
using CRAB.ProjectSystem;
using CDTk;

var namespaceMap = /* ... from assembly metadata ... */;
var resolver = new NamespaceResolver(namespaceMap);

// Process using directives from AST
AstNode compilationUnit = /* ... from parser ... */;
resolver.ProcessUsingDirectives(compilationUnit);

// Resolve a type name
var fullTypeName = resolver.ResolveType("JsonConvert");
// → "Newtonsoft.Json.JsonConvert"

// Get imported namespaces
var namespaces = resolver.GetImportedNamespaces();
// → ["System", "Newtonsoft.Json", ...]

// Get using aliases
var aliases = resolver.GetAliases();
// → { "Json": "Newtonsoft.Json", ... }

// Validate all using directives
var errors = resolver.ValidateUsingDirectives();
if (errors.Count > 0)
{
    foreach (var error in errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

## Project File Format

### Basic Example

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### Multiple Target Frameworks

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net8.0;net7.0;netstandard2.1</TargetFrameworks>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
  </ItemGroup>
</Project>
```

### Package with Metadata

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.3">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

## Using Directives

### Standard Using Namespace

```csharp
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// Can now use types from these namespaces without qualification
var list = new List<string>();
var json = JsonConvert.SerializeObject(list);
```

### Using Alias (C# 12)

```csharp
// Alias a namespace
using Json = Newtonsoft.Json;

// Alias a type
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

// Use the alias
var serializer = new JsonSerializer();
```

### Using Static

```csharp
using static System.Console;
using static System.Math;

// Can use static members directly
WriteLine("Hello");
var result = Sqrt(16); // → 4
```

### Global Using (C# 10)

```csharp
global using System;
global using System.Collections.Generic;

// Available in all files in the project
```

## Framework Compatibility

The NuGet resolver supports automatic framework compatibility matching:

| Target Framework | Compatible Package Frameworks |
|-----------------|-------------------------------|
| net8.0 | net8.0, net7.0, net6.0, net5.0, netcoreapp3.1, netstandard2.1, netstandard2.0 |
| net7.0 | net7.0, net6.0, net5.0, netcoreapp3.1, netstandard2.1, netstandard2.0 |
| net6.0 | net6.0, net5.0, netcoreapp3.1, netstandard2.1, netstandard2.0 |
| netstandard2.1 | netstandard2.1, netstandard2.0, netstandard1.6, ... |
| netstandard2.0 | netstandard2.0, netstandard1.6, netstandard1.5, ... |

The resolver selects the best match automatically.

## API Reference

### PackageReference

```csharp
public class PackageReference
{
    public string Name { get; set; }        // Package name
    public string Version { get; set; }     // Package version
    public bool IsImplicit { get; set; }    // SDK-implicit reference
    public Dictionary<string, string> Metadata { get; set; }
}
```

### ProjectFile

```csharp
public class ProjectFile
{
    public string FilePath { get; set; }
    public string TargetFramework { get; set; }
    public List<string> TargetFrameworks { get; set; }
    public string OutputType { get; set; }
    public List<string> SourceFiles { get; set; }
    public List<string> ProjectReferences { get; set; }
    public List<PackageReference> PackageReferences { get; set; }
    public Dictionary<string, string> Properties { get; set; }
    
    public static ProjectFile Parse(string filePath);
}
```

### NuGetResolver

```csharp
public class NuGetResolver
{
    public string GetGlobalPackagesPath();
    public List<string> ResolvePackage(PackageReference package, string targetFramework);
    public Dictionary<string, List<string>> ResolveAllPackages(ProjectFile project);
}
```

### TypeInfo

```csharp
public class TypeInfo
{
    public string FullName { get; set; }      // "System.Collections.Generic.List`1"
    public string Namespace { get; set; }     // "System.Collections.Generic"
    public string Name { get; set; }          // "List`1"
    public bool IsPublic { get; set; }
    public bool IsClass { get; set; }
    public bool IsInterface { get; set; }
    public bool IsEnum { get; set; }
    public bool IsStatic { get; set; }
    public List<string> Methods { get; set; }
    public List<string> Properties { get; set; }
    public List<string> Fields { get; set; }
}
```

### AssemblyMetadataReader

```csharp
public class AssemblyMetadataReader
{
    public List<TypeInfo> ReadAssembly(string assemblyPath);
    public Dictionary<string, List<TypeInfo>> ReadAssemblies(List<string> assemblyPaths);
    public Dictionary<string, List<TypeInfo>> BuildNamespaceMap(Dictionary<string, List<TypeInfo>> assemblyTypes);
}
```

### NamespaceResolver

```csharp
public class NamespaceResolver
{
    public NamespaceResolver(Dictionary<string, List<TypeInfo>> namespaceMap);
    
    public void ProcessUsingDirectives(AstNode? compilationUnit);
    public string? ResolveType(string typeName);
    public List<TypeInfo> GetTypesInNamespace(string namespaceName);
    public List<string> GetImportedNamespaces();
    public Dictionary<string, string> GetAliases();
    public List<string> ValidateUsingDirectives();
}
```

## Integration with CRAB Compiler

The NuGet integration is designed to integrate seamlessly with the CRAB compilation pipeline:

1. **Project Discovery** - ProjectDiscovery finds .csproj files
2. **Project Parsing** - ProjectFile.Parse() extracts package references
3. **Package Resolution** - NuGetResolver resolves packages to DLL paths
4. **Metadata Extraction** - AssemblyMetadataReader reads type information
5. **Using Processing** - NamespaceResolver processes using directives from AST
6. **Type Resolution** - During semantic analysis, types are resolved to fully qualified names
7. **Code Generation** - WASM emission uses resolved type information

## Testing

To test the NuGet integration:

```bash
# Navigate to test project
cd Testing/NuGetTest

# Restore packages (if needed)
dotnet restore

# Run the CRAB compiler
crab build NuGetTest.csproj
```

The test project includes:
- `NuGetTest.csproj` - Project with Newtonsoft.Json package reference
- `Program.cs` - C# code using Newtonsoft.Json types
- `test_nuget_integration.cs` - Demo script showing all NuGet features

## Troubleshooting

### Package Not Found

**Problem:** Package not resolved from NuGet cache

**Solution:**
1. Ensure package is installed: `dotnet restore`
2. Check NuGet global packages path: `dotnet nuget locals global-packages --list`
3. Verify package exists in the cache
4. Package names are case-sensitive in the file system

### Framework Mismatch

**Problem:** No compatible framework found in package

**Solution:**
1. Check target framework in .csproj matches available package frameworks
2. Try using netstandard2.0 or netstandard2.1 which are widely compatible
3. Update package version to one with compatible frameworks

### Using Directive Not Resolved

**Problem:** Type not found even with using directive

**Solution:**
1. Verify namespace is correct and exists in referenced assemblies
2. Check that package containing the namespace is properly referenced
3. Ensure using directive processing is enabled in compilation pipeline
4. Validate using directives: `resolver.ValidateUsingDirectives()`

## Future Enhancements

Planned improvements:
- [ ] Transitive dependency resolution
- [ ] Package version conflict resolution
- [ ] Source generator support
- [ ] Analyzer support
- [ ] Multi-targeting project support
- [ ] Package download/restore functionality
- [ ] NuGet.config support
- [ ] Private package feed support

## Summary

CRAB now provides complete NuGet integration with:
- ✅ Full PackageReference parsing
- ✅ Cross-platform package resolution
- ✅ Assembly metadata extraction
- ✅ Using directive processing
- ✅ Type name resolution
- ✅ C# 12 using alias support
- ✅ Namespace validation

This enables CRAB to compile projects that depend on external NuGet packages and properly resolve using directives to fully qualified type names.
