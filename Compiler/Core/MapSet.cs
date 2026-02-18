using CDTk;
using System;
using System.Collections.Generic;

namespace CRAB;

public class WASM : MapSet
{
    public Dictionary<string, MethodMetadata> Methods { get; set; } = new();
    public bool Minify { get; set; } = false;
    
    private static WASMTokens T = new WASMTokens();
    
    public Map MethodDeclaration => new Map(
        (Func<string> parameters, Func<string> body) =>
        {
            var meta = new MethodMetadata 
            { 
                Name = "placeholder", 
                ResultType = "i32" 
            };
            
            var result = T.LeftParen.Pattern + 
                        T.KwFunc.Pattern + " " +
                        T.Dollar.Pattern + meta.Name;
            
            var paramsWat = parameters();
            if (!string.IsNullOrWhiteSpace(paramsWat))
            {
                result += this.Minify ? paramsWat : "\n  " + paramsWat;
            }
            
            if (!string.IsNullOrWhiteSpace(meta.ResultType))
            {
                result += this.Minify 
                    ? T.LeftParen.Pattern + T.KwResult.Pattern + " " + meta.ResultType + T.RightParen.Pattern
                    : "\n  " + T.LeftParen.Pattern + T.KwResult.Pattern + " " + meta.ResultType + T.RightParen.Pattern;
            }
            
            var bodyWat = body();
            if (!string.IsNullOrWhiteSpace(bodyWat))
            {
                result += this.Minify ? bodyWat : "\n" + bodyWat;
            }
            
            result += this.Minify ? T.RightParen.Pattern : "\n" + T.RightParen.Pattern;
            return result;
        }
    );
}

public class MethodMetadata
{
    public string Name { get; set; } = "";
    public string ResultType { get; set; } = "";
}
