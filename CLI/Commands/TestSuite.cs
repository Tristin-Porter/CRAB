using System.Diagnostics;
using System.IO;

namespace CRAB;

/// <summary>
/// TestSuite command - runs the CRAB compiler test suite
/// </summary>
class TestSuite : Command
{
    public TestSuite()
    {
        Name = "test-suite";
        Description = "Run the CRAB compiler test suite (all unit and integration tests).";
        
        SupportedFlags["verbose"] = "Enable verbose output from tests.";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        bool verbose = flags.ContainsKey("verbose");
        
        // Find the testing directory
        string currentDir = Directory.GetCurrentDirectory();
        string testingDir = Path.Combine(currentDir, "testing");
        
        // Check if we're in the CRAB root or need to navigate up
        if (!Directory.Exists(testingDir))
        {
            // Try parent directory (in case we're in a subdirectory)
            string parentDir = Directory.GetParent(currentDir)?.FullName ?? currentDir;
            testingDir = Path.Combine(parentDir, "testing");
            
            if (!Directory.Exists(testingDir))
            {
                System.Console.WriteLine("Error: testing directory not found.");
                System.Console.WriteLine("Please run this command from the CRAB root directory.");
                return;
            }
        }
        
        if (verbose)
        {
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine("CRAB Test Suite");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine($"Test directory: {testingDir}");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine();
        }
        
        // Run the test suite
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run",
                WorkingDirectory = testingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = new Process { StartInfo = startInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    System.Console.WriteLine(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                    System.Console.Error.WriteLine(e.Data);
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            
            if (process.ExitCode != 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Test suite failed with exit code: " + process.ExitCode);
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error running test suite: {ex.Message}");
            if (verbose)
            {
                System.Console.WriteLine("\nStack trace:");
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
