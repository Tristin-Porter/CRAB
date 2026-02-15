using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace CRAB.ProjectSystem;

/// <summary>
/// Represents type information extracted from an assembly.
/// </summary>
public class TypeInfo
{
    public string FullName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool IsClass { get; set; }
    public bool IsInterface { get; set; }
    public bool IsEnum { get; set; }
    public bool IsStatic { get; set; }
    public List<string> Methods { get; set; } = new();
    public List<string> Properties { get; set; } = new();
    public List<string> Fields { get; set; } = new();
}

/// <summary>
/// Reads metadata from .NET assemblies to extract type information.
/// </summary>
public class AssemblyMetadataReader
{
    /// <summary>
    /// Read type information from an assembly DLL.
    /// </summary>
    public List<TypeInfo> ReadAssembly(string assemblyPath)
    {
        var types = new List<TypeInfo>();
        
        try
        {
            if (!File.Exists(assemblyPath))
            {
                System.Console.WriteLine($"Warning: Assembly not found: {assemblyPath}");
                return types;
            }
            
            using var fileStream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(fileStream);
            
            if (!peReader.HasMetadata)
            {
                System.Console.WriteLine($"Warning: No metadata in assembly: {assemblyPath}");
                return types;
            }
            
            var metadataReader = peReader.GetMetadataReader();
            
            // Read type definitions
            foreach (var typeDefHandle in metadataReader.TypeDefinitions)
            {
                try
                {
                    var typeDef = metadataReader.GetTypeDefinition(typeDefHandle);
                    
                    // Skip compiler-generated types and nested types (for now)
                    if (typeDef.Attributes.HasFlag(TypeAttributes.NestedPublic) ||
                        typeDef.Attributes.HasFlag(TypeAttributes.NestedPrivate) ||
                        typeDef.Attributes.HasFlag(TypeAttributes.NestedFamily) ||
                        typeDef.Attributes.HasFlag(TypeAttributes.NestedAssembly))
                    {
                        continue;
                    }
                    
                    var namespaceName = metadataReader.GetString(typeDef.Namespace);
                    var typeName = metadataReader.GetString(typeDef.Name);
                    
                    // Skip compiler-generated types
                    if (typeName.Contains("<") || typeName.Contains(">") || string.IsNullOrEmpty(namespaceName))
                        continue;
                    
                    var typeInfo = new TypeInfo
                    {
                        Namespace = namespaceName,
                        Name = typeName,
                        FullName = string.IsNullOrEmpty(namespaceName) ? typeName : $"{namespaceName}.{typeName}",
                        IsPublic = typeDef.Attributes.HasFlag(TypeAttributes.Public),
                        IsClass = typeDef.Attributes.HasFlag(TypeAttributes.Class) && !typeDef.Attributes.HasFlag(TypeAttributes.Interface),
                        IsInterface = typeDef.Attributes.HasFlag(TypeAttributes.Interface),
                        IsEnum = false, // Will be determined by base type
                        IsStatic = typeDef.Attributes.HasFlag(TypeAttributes.Abstract) && typeDef.Attributes.HasFlag(TypeAttributes.Sealed)
                    };
                    
                    // Read methods
                    foreach (var methodHandle in typeDef.GetMethods())
                    {
                        try
                        {
                            var method = metadataReader.GetMethodDefinition(methodHandle);
                            var methodName = metadataReader.GetString(method.Name);
                            
                            // Skip special methods like constructors, getters, setters
                            if (methodName.StartsWith(".") || methodName.StartsWith("get_") || 
                                methodName.StartsWith("set_") || methodName.StartsWith("add_") || 
                                methodName.StartsWith("remove_"))
                                continue;
                            
                            if (method.Attributes.HasFlag(MethodAttributes.Public))
                            {
                                typeInfo.Methods.Add(methodName);
                            }
                        }
                        catch
                        {
                            // Skip problematic methods
                        }
                    }
                    
                    // Read properties
                    foreach (var propertyHandle in typeDef.GetProperties())
                    {
                        try
                        {
                            var property = metadataReader.GetPropertyDefinition(propertyHandle);
                            var propertyName = metadataReader.GetString(property.Name);
                            typeInfo.Properties.Add(propertyName);
                        }
                        catch
                        {
                            // Skip problematic properties
                        }
                    }
                    
                    // Read fields
                    foreach (var fieldHandle in typeDef.GetFields())
                    {
                        try
                        {
                            var field = metadataReader.GetFieldDefinition(fieldHandle);
                            var fieldName = metadataReader.GetString(field.Name);
                            
                            if (field.Attributes.HasFlag(FieldAttributes.Public))
                            {
                                typeInfo.Fields.Add(fieldName);
                            }
                        }
                        catch
                        {
                            // Skip problematic fields
                        }
                    }
                    
                    types.Add(typeInfo);
                }
                catch (Exception ex)
                {
                    // Skip problematic type definitions
                    System.Console.WriteLine($"Warning: Error reading type in {Path.GetFileName(assemblyPath)}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Error reading assembly {assemblyPath}: {ex.Message}");
        }
        
        return types;
    }
    
    /// <summary>
    /// Read all types from multiple assemblies.
    /// </summary>
    public Dictionary<string, List<TypeInfo>> ReadAssemblies(List<string> assemblyPaths)
    {
        var result = new Dictionary<string, List<TypeInfo>>();
        
        foreach (var path in assemblyPaths)
        {
            var types = ReadAssembly(path);
            if (types.Count > 0)
            {
                result[path] = types;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Build a namespace-to-types mapping.
    /// </summary>
    public Dictionary<string, List<TypeInfo>> BuildNamespaceMap(Dictionary<string, List<TypeInfo>> assemblyTypes)
    {
        var namespaceMap = new Dictionary<string, List<TypeInfo>>();
        
        foreach (var assembly in assemblyTypes.Values)
        {
            foreach (var type in assembly)
            {
                if (!namespaceMap.ContainsKey(type.Namespace))
                {
                    namespaceMap[type.Namespace] = new List<TypeInfo>();
                }
                namespaceMap[type.Namespace].Add(type);
            }
        }
        
        return namespaceMap;
    }
}
