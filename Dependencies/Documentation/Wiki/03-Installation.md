# Installation

## Prerequisites

- **.NET 8.0 or later**
- **C# 12 or later**

## Installation Methods

### Method 1: Include Source Directly (Recommended)

CDTk is currently distributed as a single source file for maximum flexibility.

**Step 1:** Clone the repository
```bash
git clone https://github.com/Tristin-Porter/CDTk
```

**Step 2:** Copy `CDTk.cs` to your project
```bash
cp CDTk/Boilerplate/CDTk.cs your-project/
```

**Step 3:** Include it in your `.csproj`
```xml
<ItemGroup>
    <Compile Include="CDTk.cs" />
</ItemGroup>
```

**Step 4:** Start using CDTk
```csharp
using CDTk;

class Tokens : TokenSet
{
    public Token Number = @"\d+";
}
```

### Method 2: Add as Project Reference

If you want to keep CDTk separate:

**Step 1:** Clone CDTk repository
```bash
git clone https://github.com/Tristin-Porter/CDTk ../CDTk
```

**Step 2:** Reference it in your `.csproj`
```xml
<ItemGroup>
    <ProjectReference Include="../CDTk/CDTk.csproj" />
</ItemGroup>
```

## Verify Installation

Create a test file to verify CDTk is working:

```csharp
using System;
using CDTk;

class TestTokens : TokenSet
{
    public Token Number = @"\d+";
    public Token WS = new Token(@"\s+").Ignore();
}

class TestRules : RuleSet
{
    public Rule Root = new Rule("@Number");
}

class TestMaps : MapSet
{
    public Map Root = "Success! Found: {value}";
}

class Program
{
    static void Main()
    {
        var compiler = new Compiler()
            .WithTokens(new TestTokens())
            .WithRules(new TestRules())
            .WithTarget(new TestMaps())
            .Build();

        var result = compiler.Compile("42");
        
        if (!result.Diagnostics.HasErrors)
        {
            Console.WriteLine(result.Output[0]);
            Console.WriteLine("✓ CDTk is installed correctly!");
        }
        else
        {
            Console.WriteLine("✗ Installation issue");
            foreach (var d in result.Diagnostics.Items)
                Console.WriteLine(d.Message);
        }
    }
}
```

Run it:
```bash
dotnet run
```

Expected output:
```
Success! Found: 42
✓ CDTk is installed correctly!
```

## IDE Setup

### Visual Studio 2022

1. Open your solution
2. Right-click project → Add → Existing Item
3. Select `CDTk.cs`
4. IntelliSense will work automatically

### VS Code

1. Ensure C# extension is installed
2. Add `CDTk.cs` to your project folder
3. Restart OmniSharp if needed: `Ctrl+Shift+P` → "Restart OmniSharp"

### Rider

1. Right-click project → Add → Add Files
2. Select `CDTk.cs`
3. IntelliSense works automatically

## Troubleshooting

### "Type or namespace CDTk could not be found"
- Ensure `CDTk.cs` is included in your project
- Check your `.csproj` has the `<Compile Include="CDTk.cs" />` entry
- Rebuild your project

### "Feature 'X' is not available in C# 11"
- Update to C# 12: Add `<LangVersion>12</LangVersion>` to your `.csproj`

### Performance Issues in Debug Mode
- Build in Release mode: `dotnet build -c Release`
- CDTk is optimized for Release builds

## Next Steps

- **[Getting Started](01-GettingStarted.md)** - Your first compiler
- **[Quick Start Tutorial](02-QuickStart.md)** - Build progressively complex compilers
- **[Core Concepts](04-CoreConcepts.md)** - Understand the architecture
