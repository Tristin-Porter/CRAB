using System;
using System.Collections.Generic;
using System.Linq;
using CDTk;

namespace CRAB.ProjectSystem;

/// <summary>
/// Resolves using directives and type names to fully qualified names.
/// </summary>
public class NamespaceResolver
{
    private readonly Dictionary<string, List<TypeInfo>> _namespaceMap;
    private readonly List<string> _usingNamespaces = new();
    private readonly Dictionary<string, string> _usingAliases = new();
    private readonly List<string> _usingStaticTypes = new();
    
    public NamespaceResolver(Dictionary<string, List<TypeInfo>> namespaceMap)
    {
        _namespaceMap = namespaceMap;
    }
    
    /// <summary>
    /// Process using directives from the AST.
    /// </summary>
    public void ProcessUsingDirectives(AstNode? compilationUnit)
    {
        if (compilationUnit == null)
            return;
        
        try
        {
            // Extract using directives from compilation unit
            var items = compilationUnit["items"] as List<object>;
            if (items == null)
                return;
            
            foreach (var item in items)
            {
                if (item is not AstNode itemNode)
                    continue;
                
                // Check if this is a UsingDirective
                var directive = itemNode["directive"] as AstNode;
                if (directive != null)
                {
                    ProcessSingleUsingDirective(directive);
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Error processing using directives: {ex.Message}");
        }
    }
    
    private void ProcessSingleUsingDirective(AstNode directive)
    {
        try
        {
            // Check for using alias directive (using Alias = Type)
            var aliasObj = directive["alias"];
            var aliasedTypeObj = directive["aliasedType"];
            
            if (aliasObj != null && aliasedTypeObj != null)
            {
                var alias = aliasObj.ToString();
                var aliasedType = ExtractTypeName(aliasedTypeObj as AstNode);
                
                if (!string.IsNullOrEmpty(alias) && !string.IsNullOrEmpty(aliasedType))
                {
                    _usingAliases[alias] = aliasedType;
                }
            }
            // Regular using namespace directive
            else
            {
                var nameObj = directive["name"];
                if (nameObj != null)
                {
                    var name = ExtractQualifiedName(nameObj as AstNode);
                    if (!string.IsNullOrEmpty(name))
                    {
                        _usingNamespaces.Add(name);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Warning: Error processing single using directive: {ex.Message}");
        }
    }
    
    private string ExtractQualifiedName(AstNode? nameNode)
    {
        if (nameNode == null)
            return "";
        
        try
        {
            // Try to extract from parts
            var parts = nameNode["parts"] as List<object>;
            if (parts != null)
            {
                var names = new List<string>();
                foreach (var part in parts)
                {
                    if (part is AstNode partNode)
                    {
                        // Try to get name from the node
                        var partName = partNode["name"]?.ToString();
                        if (!string.IsNullOrEmpty(partName))
                        {
                            names.Add(partName);
                        }
                    }
                    else if (part is string partStr)
                    {
                        names.Add(partStr);
                    }
                }
                if (names.Count > 0)
                {
                    return string.Join(".", names);
                }
            }
            
            // Fallback: try to get name field directly
            var nameObj = nameNode["name"];
            if (nameObj != null)
            {
                return nameObj.ToString() ?? "";
            }
            
            // Last resort: convert node to string
            return nameNode.ToString() ?? "";
        }
        catch
        {
            // Ignore extraction errors
        }
        
        return "";
    }
    
    private string ExtractTypeName(AstNode? typeNode)
    {
        if (typeNode == null)
            return "";
        
        try
        {
            // Simple type name from name field
            var nameObj = typeNode["name"];
            if (nameObj != null)
            {
                return nameObj.ToString() ?? "";
            }
            
            // Qualified name
            var qualifiedNameObj = typeNode["qualifiedName"];
            if (qualifiedNameObj is AstNode qualifiedNode)
            {
                return ExtractQualifiedName(qualifiedNode);
            }
            
            // Fallback: convert to string
            return typeNode.ToString() ?? "";
        }
        catch
        {
            // Ignore extraction errors
        }
        
        return "";
    }
    
    /// <summary>
    /// Resolve a type name to its fully qualified name.
    /// </summary>
    public string? ResolveType(string typeName)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;
        
        // Check if it's already fully qualified
        if (typeName.Contains('.'))
        {
            // Verify it exists
            foreach (var ns in _namespaceMap.Keys)
            {
                var types = _namespaceMap[ns];
                if (types.Any(t => t.FullName == typeName))
                {
                    return typeName;
                }
            }
        }
        
        // Check aliases
        if (_usingAliases.ContainsKey(typeName))
        {
            return _usingAliases[typeName];
        }
        
        // Search in imported namespaces
        foreach (var ns in _usingNamespaces)
        {
            if (_namespaceMap.TryGetValue(ns, out var types))
            {
                var match = types.FirstOrDefault(t => t.Name == typeName);
                if (match != null)
                {
                    return match.FullName;
                }
            }
        }
        
        // Not found
        return null;
    }
    
    /// <summary>
    /// Get all types available in a namespace.
    /// </summary>
    public List<TypeInfo> GetTypesInNamespace(string namespaceName)
    {
        if (_namespaceMap.TryGetValue(namespaceName, out var types))
        {
            return types;
        }
        return new List<TypeInfo>();
    }
    
    /// <summary>
    /// Get all imported namespaces.
    /// </summary>
    public List<string> GetImportedNamespaces()
    {
        return new List<string>(_usingNamespaces);
    }
    
    /// <summary>
    /// Get all using aliases.
    /// </summary>
    public Dictionary<string, string> GetAliases()
    {
        return new Dictionary<string, string>(_usingAliases);
    }
    
    /// <summary>
    /// Validate that all using directives reference valid namespaces.
    /// </summary>
    public List<string> ValidateUsingDirectives()
    {
        var errors = new List<string>();
        
        foreach (var ns in _usingNamespaces)
        {
            if (!_namespaceMap.ContainsKey(ns))
            {
                errors.Add($"Namespace '{ns}' not found in referenced assemblies");
            }
        }
        
        foreach (var alias in _usingAliases)
        {
            var resolvedType = ResolveType(alias.Value);
            if (resolvedType == null)
            {
                errors.Add($"Type '{alias.Value}' in using alias '{alias.Key}' not found");
            }
        }
        
        return errors;
    }
}
