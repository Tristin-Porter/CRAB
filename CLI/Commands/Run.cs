using System.IO;
using System.Diagnostics;
using Badger;

namespace CRAB;

/// <summary>
/// Run command - compiles WAT to native assembly via BADGER and executes it
/// </summary>
class Run : Command
{
    public Run()
    {
        Name = "run";
        Description = "Run a compiled WebAssembly file by compiling to native assembly via BADGER.";
        
        SupportedFlags["input"] = "Input WebAssembly file (WAT) or project directory.";
        SupportedFlags["arch"] = "Target architecture (x86_64, x86_32, x86_16, arm64, arm32, default: x86_64).";
        SupportedFlags["format"] = "Output format (native, pe, default: native).";
        SupportedFlags["args"] = "Arguments to pass to the program.";
        SupportedFlags["verbose"] = "Enable verbose output.";
        SupportedFlags["keep-temp"] = "Keep temporary executable files after execution.";
    }

    public override void Execute(string[] args, Dictionary<string, string?> flags)
    {
        // Parse input
        string? inputPath = null;
        if (flags.TryGetValue("input", out var flagInput))
            inputPath = flagInput;
        else if (args.Length > 0)
            inputPath = args[0];

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            System.Console.WriteLine("Error: Input WAT file or project directory required.");
            System.Console.WriteLine("Usage: run <file.wat> [--arch x86_64] [--format native] [--args \"...\"]");
            return;
        }

        // Determine WAT file path
        string watFile;
        if (File.Exists(inputPath))
        {
            watFile = inputPath;
        }
        else if (Directory.Exists(inputPath))
        {
            // Look for output.wat or output.wasm in bin directory
            watFile = Path.Combine(inputPath, "bin", "output.wat");
            if (!File.Exists(watFile))
            {
                watFile = Path.Combine(inputPath, "bin", "output.wasm");
            }
            if (!File.Exists(watFile))
            {
                System.Console.WriteLine($"Error: No compiled output found in bin directory");
                System.Console.WriteLine("Hint: Run 'build' first to compile the project.");
                return;
            }
        }
        else
        {
            System.Console.WriteLine($"Error: Input path '{inputPath}' does not exist.");
            return;
        }

        // Parse architecture and format
        string architecture = "x86_64";
        if (flags.TryGetValue("arch", out var flagArch) && !string.IsNullOrWhiteSpace(flagArch))
            architecture = flagArch.ToLower();

        string format = "native";
        if (flags.TryGetValue("format", out var flagFormat) && !string.IsNullOrWhiteSpace(flagFormat))
            format = flagFormat.ToLower();

        // Parse program arguments
        string programArgs = "";
        if (flags.TryGetValue("args", out var flagArgs) && !string.IsNullOrWhiteSpace(flagArgs))
            programArgs = flagArgs;

        bool verbose = flags.ContainsKey("verbose");
        bool keepTemp = flags.ContainsKey("keep-temp");

        if (verbose)
        {
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine("CRAB Runner (WAT -> Native via BADGER)");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine($"Input:      {watFile}");
            System.Console.WriteLine($"Arch:       {architecture}");
            System.Console.WriteLine($"Format:     {format}");
            System.Console.WriteLine($"Arguments:  {programArgs}");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine();
        }

        try
        {
            // Read WAT file
            if (verbose) System.Console.WriteLine("[1/3] Reading WAT file...");
            string watContent = File.ReadAllText(watFile);
            
            if (verbose) System.Console.WriteLine($"      Read {watContent.Length} characters");

            // Compile WAT to native assembly using BADGER
            if (verbose) System.Console.WriteLine($"\n[2/3] Compiling WAT to {GetArchitectureDisplayName(architecture)} assembly via BADGER...");
            
            byte[] nativeBinary;
            try
            {
                nativeBinary = BadgerCompiler.Compile(watContent, architecture, format);
            }
            catch (Exception badgerEx)
            {
                System.Console.WriteLine($"Error: BADGER compilation failed - {badgerEx.Message}");
                if (verbose)
                {
                    System.Console.WriteLine("\nStack trace:");
                    System.Console.WriteLine(badgerEx.StackTrace);
                }
                return;
            }

            if (verbose) System.Console.WriteLine($"      Compiled {nativeBinary.Length} bytes of native code");

            // Write temporary executable
            string tempExecutable = Path.GetTempFileName();
            
            // Determine file extension based on format and OS
            if (format == "pe" || (Environment.OSVersion.Platform == PlatformID.Win32NT))
            {
                // Windows PE executable
                tempExecutable = Path.ChangeExtension(tempExecutable, ".exe");
            }
            
            File.WriteAllBytes(tempExecutable, nativeBinary);

            // Make executable on Unix-like systems
            if (Environment.OSVersion.Platform == PlatformID.Unix || 
                Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                try
                {
                    var chmodInfo = new ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x {tempExecutable}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using var chmodProcess = Process.Start(chmodInfo);
                    chmodProcess?.WaitForExit();
                }
                catch
                {
                    // Ignore chmod errors
                }
            }

            if (verbose) System.Console.WriteLine($"      Wrote temporary executable: {tempExecutable}");

            // Execute the native binary
            if (verbose) System.Console.WriteLine("\n[3/3] Executing native program...");
            if (verbose) System.Console.WriteLine("=".PadRight(60, '='));

            var startInfo = new ProcessStartInfo
            {
                FileName = tempExecutable,
                Arguments = programArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                System.Console.WriteLine("Error: Failed to start executable.");
                return;
            }

            // Forward output in real-time
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

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();

            if (verbose)
            {
                System.Console.WriteLine("=".PadRight(60, '='));
                System.Console.WriteLine($"Process exited with code: {process.ExitCode}");
                System.Console.WriteLine("=".PadRight(60, '='));
            }

            // Clean up temporary file
            if (!keepTemp)
            {
                try
                {
                    File.Delete(tempExecutable);
                    if (verbose) System.Console.WriteLine($"\nCleaned up temporary file: {tempExecutable}");
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            else if (verbose)
            {
                System.Console.WriteLine($"\nKept temporary file: {tempExecutable}");
            }

            if (process.ExitCode != 0)
            {
                System.Console.WriteLine($"\nWarning: Program exited with code: {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: Failed to run program - {ex.Message}");
            if (verbose)
            {
                System.Console.WriteLine("\nStack trace:");
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }

    private string GetArchitectureDisplayName(string architecture)
    {
        return architecture.ToLower() switch
        {
            "x86_64" => "x86-64",
            "x86_32" => "x86-32",
            "x86_16" => "x86-16",
            "arm64" => "ARM64",
            "arm32" => "ARM32",
            _ => architecture.ToUpper()
        };
    }
}
