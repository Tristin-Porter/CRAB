using System;
using System.Collections.Generic;

namespace Badger.Containers;

/// <summary>
/// Native (bare metal) container emitter.
/// Produces flat binaries with no headers, relocations, or metadata.
/// Entrypoint is at offset 0.
/// </summary>
public static class Native
{
    public static byte[] Emit(byte[] machineCode)
    {
        // Native format is just the raw machine code
        // No headers, no relocations, no metadata
        // Entrypoint is at offset 0
        return machineCode;
    }
}
