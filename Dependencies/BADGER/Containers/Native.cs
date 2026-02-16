using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Badger.Containers;

/// <summary>
/// Native container emitter.
/// Produces platform-appropriate executable binaries:
/// - ELF on Linux/Unix
/// - PE on Windows
/// - Raw machine code as fallback
/// </summary>
public static class Native
{
    public static byte[] Emit(byte[] machineCode)
    {
        // Detect platform and emit appropriate format
        if (IsWindowsPlatform())
        {
            // Windows: use PE format
            return PE.Emit(machineCode);
        }
        else if (IsUnixLikePlatform())
        {
            // Unix-like: use ELF format
            return ELF.Emit(machineCode);
        }
        else
        {
            // Fallback: raw machine code for unknown platforms
            return machineCode;
        }
    }
    
    private static bool IsWindowsPlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }
    
    private static bool IsUnixLikePlatform()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || 
               RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ||
               RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    }
}
