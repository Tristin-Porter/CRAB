using System;
using System.Collections.Generic;

namespace Badger.Containers;

/// <summary>
/// ELF (Executable and Linkable Format) container emitter.
/// Produces minimal ELF64 executables for Linux/Unix with:
/// - Valid ELF header
/// - Program header for code segment
/// - Executable permissions
/// </summary>
public static class ELF
{
    public static byte[] Emit(byte[] machineCode)
    {
        var elf = new List<byte>();
        
        // ELF Header (64 bytes for ELF64)
        elf.AddRange(CreateELFHeader(machineCode.Length));
        
        // Program Header (56 bytes for ELF64)
        elf.AddRange(CreateProgramHeader(machineCode.Length));
        
        // Machine code
        elf.AddRange(machineCode);
        
        return elf.ToArray();
    }
    
    private static byte[] CreateELFHeader(int codeSize)
    {
        var header = new byte[64];
        
        // ELF magic number
        header[0] = 0x7F; // DEL
        header[1] = (byte)'E';
        header[2] = (byte)'L';
        header[3] = (byte)'F';
        
        // Class (2 = 64-bit)
        header[4] = 0x02;
        
        // Data encoding (1 = little-endian)
        header[5] = 0x01;
        
        // Version (1 = current)
        header[6] = 0x01;
        
        // OS/ABI (0 = System V)
        header[7] = 0x00;
        
        // ABI version (0)
        header[8] = 0x00;
        
        // Padding (7 bytes)
        for (int i = 9; i < 16; i++)
            header[i] = 0x00;
        
        // Type (2 = executable)
        WriteUInt16(header, 16, 0x02);
        
        // Machine (0x3E = x86-64)
        WriteUInt16(header, 18, 0x3E);
        
        // Version (1)
        WriteUInt32(header, 20, 0x01);
        
        // Entry point (virtual address where execution starts)
        // Base address + ELF header + Program header
        WriteUInt64(header, 24, 0x400000 + 64 + 56);
        
        // Program header offset (starts right after ELF header)
        WriteUInt64(header, 32, 64);
        
        // Section header offset (0 = no section headers)
        WriteUInt64(header, 40, 0);
        
        // Flags (0)
        WriteUInt32(header, 48, 0);
        
        // ELF header size (64 bytes)
        WriteUInt16(header, 52, 64);
        
        // Program header entry size (56 bytes for ELF64)
        WriteUInt16(header, 54, 56);
        
        // Number of program headers (1)
        WriteUInt16(header, 56, 1);
        
        // Section header entry size (0)
        WriteUInt16(header, 58, 0);
        
        // Number of section headers (0)
        WriteUInt16(header, 60, 0);
        
        // Section header string table index (0)
        WriteUInt16(header, 62, 0);
        
        return header;
    }
    
    private static byte[] CreateProgramHeader(int codeSize)
    {
        var header = new byte[56];
        
        // Type (1 = PT_LOAD, loadable segment)
        WriteUInt32(header, 0, 0x01);
        
        // Flags (5 = PF_R | PF_X, readable and executable)
        WriteUInt32(header, 4, 0x05);
        
        // Offset in file (0, starts at beginning)
        WriteUInt64(header, 8, 0);
        
        // Virtual address (0x400000 = standard Linux load address)
        WriteUInt64(header, 16, 0x400000);
        
        // Physical address (same as virtual)
        WriteUInt64(header, 24, 0x400000);
        
        // Size in file (ELF header + Program header + code)
        int fileSize = 64 + 56 + codeSize;
        WriteUInt64(header, 32, (ulong)fileSize);
        
        // Size in memory (same as file size)
        WriteUInt64(header, 40, (ulong)fileSize);
        
        // Alignment (0x1000 = 4KB page alignment)
        WriteUInt64(header, 48, 0x1000);
        
        return header;
    }
    
    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
    }
    
    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }
    
    private static void WriteUInt64(byte[] buffer, int offset, ulong value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        buffer[offset + 4] = (byte)((value >> 32) & 0xFF);
        buffer[offset + 5] = (byte)((value >> 40) & 0xFF);
        buffer[offset + 6] = (byte)((value >> 48) & 0xFF);
        buffer[offset + 7] = (byte)((value >> 56) & 0xFF);
    }
}
