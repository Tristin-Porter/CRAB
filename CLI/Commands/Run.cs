using System.IO;
using System.Diagnostics;

namespace CRAB;

/// <summary>
/// Run command - runs a compiled WebAssembly file
/// </summary>
class Run : Command
{
    public Run()
    {
        Name = "run";
        Description = "Run a compiled WebAssembly file.";
        
        SupportedFlags["input"] = "Input WebAssembly file or project directory.";
        SupportedFlags["runtime"] = "WebAssembly runtime to use (wasmtime, wasmer, node, default: wasmtime).";
        SupportedFlags["args"] = "Arguments to pass to the WebAssembly module.";
        SupportedFlags["verbose"] = "Enable verbose output.";
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
            System.Console.WriteLine("Error: Input file or project directory required.");
            System.Console.WriteLine("Usage: run <file.wasm> [--runtime wasmtime|wasmer|node] [--args \"...\"]");
            return;
        }

        // Determine WASM file path
        string wasmFile;
        if (File.Exists(inputPath))
        {
            wasmFile = inputPath;
        }
        else if (Directory.Exists(inputPath))
        {
            // Look for output.wasm in bin directory
            wasmFile = Path.Combine(inputPath, "bin", "output.wasm");
            if (!File.Exists(wasmFile))
            {
                System.Console.WriteLine($"Error: No compiled output found at {wasmFile}");
                System.Console.WriteLine("Hint: Run 'build' first to compile the project.");
                return;
            }
        }
        else
        {
            System.Console.WriteLine($"Error: Input path '{inputPath}' does not exist.");
            return;
        }

        // Parse runtime
        string runtime = "wasmtime";
        if (flags.TryGetValue("runtime", out var flagRuntime) && !string.IsNullOrWhiteSpace(flagRuntime))
            runtime = flagRuntime.ToLower();

        // Parse module arguments
        string moduleArgs = "";
        if (flags.TryGetValue("args", out var flagArgs) && !string.IsNullOrWhiteSpace(flagArgs))
            moduleArgs = flagArgs;

        bool verbose = flags.ContainsKey("verbose");

        if (verbose)
        {
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine("CRAB WebAssembly Runner");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine($"File:       {wasmFile}");
            System.Console.WriteLine($"Runtime:    {runtime}");
            System.Console.WriteLine($"Arguments:  {moduleArgs}");
            System.Console.WriteLine("=".PadRight(60, '='));
            System.Console.WriteLine();
        }

        try
        {
            // Check if runtime is available
            if (!IsRuntimeAvailable(runtime))
            {
                System.Console.WriteLine($"Error: Runtime '{runtime}' not found in PATH.");
                System.Console.WriteLine("\nSupported runtimes:");
                System.Console.WriteLine("  - wasmtime: Install from https://wasmtime.dev/");
                System.Console.WriteLine("  - wasmer:   Install from https://wasmer.io/");
                System.Console.WriteLine("  - node:     Requires Node.js with WASM support");
                return;
            }

            // Execute the WebAssembly module
            string runtimeArgs = runtime switch
            {
                "wasmtime" => $"{wasmFile} {moduleArgs}",
                "wasmer" => $"run {wasmFile} {moduleArgs}",
                "node" => $"--experimental-wasm-modules {wasmFile} {moduleArgs}",
                _ => $"{wasmFile} {moduleArgs}"
            };

            var startInfo = new ProcessStartInfo
            {
                FileName = runtime,
                Arguments = runtimeArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                System.Console.WriteLine($"Error: Failed to start runtime '{runtime}'.");
                return;
            }

            // Forward output
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
                System.Console.WriteLine();
                System.Console.WriteLine("=".PadRight(60, '='));
                System.Console.WriteLine($"Process exited with code: {process.ExitCode}");
                System.Console.WriteLine("=".PadRight(60, '='));
            }

            if (process.ExitCode != 0)
            {
                System.Console.WriteLine($"Warning: Process exited with non-zero code: {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error: Failed to run WebAssembly module - {ex.Message}");
            if (verbose)
            {
                System.Console.WriteLine("\nStack trace:");
                System.Console.WriteLine(ex.StackTrace);
            }
        }
    }

    private bool IsRuntimeAvailable(string runtime)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = runtime,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
                return false;

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
