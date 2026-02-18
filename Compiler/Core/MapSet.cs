using CDTk;
using System;
using System.Collections.Generic;
using System.Text;

namespace CRAB;

/// <summary>
/// WASM MapSet - Pure Functional Map API Implementation
/// 
/// Architecture:
/// - All Maps are pure functional formatters
/// - Maps NEVER access AST nodes directly
/// - All semantic info comes from user-defined Models
/// - Child nodes formatted via Func&lt;string&gt; parameters
/// - MapSet references via 'this.OtherMap'
/// </summary>
public class WASM : MapSet
{
    // ============================================================
    // USER-DEFINED SEMANTIC CONTEXT FIELDS
    // ============================================================
    
    /// <summary>
    /// Method metadata indexed by some key mechanism (TBD).
    /// Populated by MethodAnalysisModel before Maps run.
    /// </summary>
    public Dictionary<string, MethodMetadata> Methods { get; set; } = new();
    
    /// <summary>
    /// Minify output flag - controls whitespace in formatting.
    /// </summary>
    public bool Minify { get; set; } = false;
    
    // ============================================================
    // EXAMPLE MAP - MethodDeclaration
    // ============================================================
    
    /// <summary>
    /// Method declaration - Example of pure functional Map.
    /// 
    /// Parameters:
    /// - parameters: Func&lt;string&gt; that calls the Map for the 'parameters' child AST node
    /// - body: Func&lt;string&gt; that calls the Map for the 'body' child AST node
    /// 
    /// Inside the lambda:
    /// - 'this' is the MapSet instance (self-reference)
    /// - Can access: this.Methods, this.Minify, this.OtherMap, etc.
    /// - Can call child Maps: parameters(), body()
    /// - CANNOT access AST: no node.Fields, no node.Type
    /// </summary>
    public Map MethodDeclaration => new Map(
        (Func<string> parameters, Func<string> body) =>
        {
            // Access semantic metadata from user-defined field
            // TODO: Need mechanism to get correct method for current node
            var meta = new MethodMetadata 
            { 
                Name = "placeholder", 
                ResultType = "i32" 
            };
            
            var sb = new StringBuilder();
            sb.Append($"(func ${meta.Name}");
            
            // Call child Map for parameters
            var paramsWat = parameters();
            if (!string.IsNullOrWhiteSpace(paramsWat))
            {
                if (this.Minify)
                    sb.Append(paramsWat);
                else
                    sb.Append("\n  " + paramsWat);
            }
            
            // Add result type if not void
            if (!string.IsNullOrWhiteSpace(meta.ResultType))
            {
                if (this.Minify)
                    sb.Append($"(result {meta.ResultType})");
                else
                    sb.Append($"\n  (result {meta.ResultType})");
            }
            
            // Call child Map for body
            var bodyWat = body();
            if (!string.IsNullOrWhiteSpace(bodyWat))
            {
                if (this.Minify)
                    sb.Append(bodyWat);
                else
                    sb.Append("\n" + bodyWat);
            }
            
            sb.Append(this.Minify ? ")" : "\n)");
            return sb.ToString();
        }
    );
    
    // ============================================================
    // TODO: Add remaining Maps here using same pattern
    // ============================================================
}

// ============================================================
// USER-DEFINED SEMANTIC METADATA CLASSES
// ============================================================

/// <summary>
/// Method metadata - populated by MethodAnalysisModel.
/// </summary>
public class MethodMetadata
{
    public string Name { get; set; } = "";
    public string ResultType { get; set; } = "";
}
