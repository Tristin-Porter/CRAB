using CDTk;

namespace CRAB.ProjectSystem;

/// <summary>
/// Token set for parsing Visual Studio Solution (.sln) files.
/// Solution files have a specific line-based format with key-value pairs.
/// </summary>
public class SolutionTokens : TokenSet
{
    // ============================================================
    // WHITESPACE & COMMENTS (Ignored)
    // ============================================================
    
    public Token Whitespace = new Token(@"[ \t]+").Ignore();
    public Token Newline = new Token(@"[\r\n]+");
    
    // Comment lines start with #
    public Token Comment = new Token(@"#[^\r\n]*").Ignore();
    
    // ============================================================
    // KEYWORDS & IDENTIFIERS
    // ============================================================
    
    // Solution file header markers
    public Token MicrosoftVisualStudioSolutionFile = @"Microsoft Visual Studio Solution File";
    public Token VisualStudioVersion = @"VisualStudioVersion";
    public Token MinimumVisualStudioVersion = @"MinimumVisualStudioVersion";
    
    // Section keywords
    public Token Project = @"Project";
    public Token EndProject = @"EndProject";
    public Token Global = @"Global";
    public Token EndGlobal = @"EndGlobal";
    public Token GlobalSection = @"GlobalSection";
    public Token EndGlobalSection = @"EndGlobalSection";
    
    // Section types
    public Token SolutionConfigurationPlatforms = @"SolutionConfigurationPlatforms";
    public Token ProjectConfigurationPlatforms = @"ProjectConfigurationPlatforms";
    public Token SolutionProperties = @"SolutionProperties";
    public Token ExtensibilityGlobals = @"ExtensibilityGlobals";
    public Token NestedProjects = @"NestedProjects";
    
    // Keywords
    public Token PreSolution = @"preSolution";
    public Token PostSolution = @"postSolution";
    public Token ActiveCfg = @"ActiveCfg";
    public Token Build = @"Build\.0";
    public Token HideSolutionNode = @"HideSolutionNode";
    
    // ============================================================
    // LITERALS
    // ============================================================
    
    // GUIDs in format {XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}
    public Token Guid = @"\{[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}\}";
    
    // Version numbers (e.g., 12.00, 18.3.11312.210)
    public Token Version = @"\d+\.\d+(\.\d+)*";
    
    // String literals (quoted paths and names)
    public Token StringLiteral = @"""[^""]*""";
    
    // Identifiers (configuration names, property names, etc.)
    public Token Identifier = @"[a-zA-Z_][a-zA-Z0-9_]*";
    
    // Paths (file paths with backslashes and forward slashes)
    public Token FilePath = @"[a-zA-Z0-9_\.\\/\-]+\.(csproj|vbproj|fsproj|vcxproj)";
    
    // ============================================================
    // OPERATORS & DELIMITERS
    // ============================================================
    
    public Token EqualsSign = @"=";
    public Token Comma = @",";
    public Token Dot = @"\.";
    public Token Pipe = @"\|";
    public Token OpenParen = @"\(";
    public Token CloseParen = @"\)";
    
    // Special markers
    public Token Hash = @"#";
}
