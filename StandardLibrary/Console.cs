using System;

namespace CRAB.StandardLibrary;

/// <summary>
/// Basic console operations for CRAB standard library.
/// Provides minimal functionality to print to console and keep console open.
/// </summary>
public static class Console
{
    /// <summary>
    /// Prints a string to the console without a newline.
    /// </summary>
    /// <param name="text">The text to print</param>
    public static void Print(string text)
    {
        // In WASM, this will be implemented as a syscall or import
        // For now, this is a placeholder that the compiler will recognize
        System.Console.Write(text);
    }

    /// <summary>
    /// Prints a string to the console followed by a newline.
    /// </summary>
    /// <param name="text">The text to print</param>
    public static void WriteLine(string text)
    {
        System.Console.WriteLine(text);
    }

    /// <summary>
    /// Prints an empty line to the console.
    /// </summary>
    public static void WriteLine()
    {
        System.Console.WriteLine();
    }

    /// <summary>
    /// Reads a line of input from the console.
    /// Keeps the console open until user provides input.
    /// </summary>
    /// <returns>The line read from console</returns>
    public static string ReadLine()
    {
        return System.Console.ReadLine() ?? string.Empty;
    }

    /// <summary>
    /// Reads a single key from the console.
    /// Useful for keeping console open with "Press any key to continue..."
    /// </summary>
    /// <returns>The key pressed</returns>
    public static ConsoleKeyInfo ReadKey()
    {
        return System.Console.ReadKey();
    }

    /// <summary>
    /// Reads a single key from the console without displaying it.
    /// </summary>
    /// <param name="intercept">Whether to prevent the key from being displayed</param>
    /// <returns>The key pressed</returns>
    public static ConsoleKeyInfo ReadKey(bool intercept)
    {
        return System.Console.ReadKey(intercept);
    }

    /// <summary>
    /// Waits for any key press before continuing.
    /// Common pattern to keep console open.
    /// </summary>
    public static void WaitForKey()
    {
        WriteLine("Press any key to continue...");
        ReadKey(true);
    }

    /// <summary>
    /// Clears the console output.
    /// </summary>
    public static void Clear()
    {
        System.Console.Clear();
    }
}
