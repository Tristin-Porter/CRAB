using System;
using System.Collections.Generic;
using System.Text;

namespace Badger.Containers;

/// <summary>
/// PE (Portable Executable) container emitter.
/// Produces minimal PE executables for Windows with:
/// - Valid DOS stub and PE header
/// - Single code section
/// - Defined entrypoint
/// - No imports, relocations, or debug info (initial version)
/// </summary>
public static class PE
{
    public static byte[] Emit(byte[] machineCode)
    {
        var pe = new List<byte>();
        
        // DOS Header (64 bytes)
        pe.AddRange(CreateDOSHeader());
        
        // DOS Stub (minimal)
        pe.AddRange(CreateDOSStub());
        
        // PE Signature (4 bytes: "PE\0\0")
        int peSignatureOffset = pe.Count;
        pe.AddRange(new byte[] { 0x50, 0x45, 0x00, 0x00 });
        
        // COFF Header (20 bytes)
        pe.AddRange(CreateCOFFHeader());
        
        // Optional Header (224 bytes for PE32+)
        int imageBase = 0x400000;
        int entryPointRVA = 0x1000; // Code starts at RVA 0x1000
        pe.AddRange(CreateOptionalHeader(imageBase, entryPointRVA, machineCode.Length));
        
        // Section Table (40 bytes per section)
        pe.AddRange(CreateCodeSection(machineCode.Length));
        
        // Align to file alignment (512 bytes)
        while (pe.Count % 512 != 0)
            pe.Add(0);
        
        // Code section data
        pe.AddRange(machineCode);
        
        // Align to section alignment
        while (pe.Count % 512 != 0)
            pe.Add(0);
        
        return pe.ToArray();
    }
    
    private static byte[] CreateDOSHeader()
    {
        var header = new byte[64];
        
        // Magic number "MZ"
        header[0] = 0x4D; // 'M'
        header[1] = 0x5A; // 'Z'
        
        // Bytes on last page
        header[2] = 0x90;
        header[3] = 0x00;
        
        // Pages in file
        header[4] = 0x03;
        header[5] = 0x00;
        
        // Header paragraphs
        header[8] = 0x04;
        header[9] = 0x00;
        
        // Max extra paragraphs
        header[10] = 0x00;
        header[11] = 0x00;
        
        // Initial SP
        header[16] = 0xB8;
        header[17] = 0x00;
        
        // Initial IP
        header[20] = 0x00;
        header[21] = 0x00;
        
        // Relocation table offset
        header[24] = 0x40;
        header[25] = 0x00;
        
        // PE header offset (at byte 60-63)
        header[60] = 0x80; // PE header at offset 128
        header[61] = 0x00;
        header[62] = 0x00;
        header[63] = 0x00;
        
        return header;
    }
    
    private static byte[] CreateDOSStub()
    {
        var stub = new byte[64];
        
        // Minimal DOS stub that prints error and exits
        // "This program cannot be run in DOS mode.\r\r\n$"
        var message = Encoding.ASCII.GetBytes("This program cannot be run in DOS mode.\r\r\n$");
        Array.Copy(message, 0, stub, 0, Math.Min(message.Length, 64));
        
        return stub;
    }
    
    private static byte[] CreateCOFFHeader()
    {
        var header = new byte[20];
        
        // Machine (0x8664 = AMD64/x86-64)
        header[0] = 0x64;
        header[1] = 0x86;
        
        // Number of sections
        header[2] = 0x01;
        header[3] = 0x00;
        
        // TimeDateStamp (0)
        header[4] = 0x00;
        header[5] = 0x00;
        header[6] = 0x00;
        header[7] = 0x00;
        
        // Pointer to symbol table (0)
        header[8] = 0x00;
        header[9] = 0x00;
        header[10] = 0x00;
        header[11] = 0x00;
        
        // Number of symbols (0)
        header[12] = 0x00;
        header[13] = 0x00;
        header[14] = 0x00;
        header[15] = 0x00;
        
        // Size of optional header (240 bytes for PE32+)
        header[16] = 0xF0;
        header[17] = 0x00;
        
        // Characteristics (0x22 = executable, large address aware)
        header[18] = 0x22;
        header[19] = 0x00;
        
        return header;
    }
    
    private static byte[] CreateOptionalHeader(int imageBase, int entryPointRVA, int codeSize)
    {
        var header = new byte[240];
        
        // Magic (0x20B = PE32+)
        header[0] = 0x0B;
        header[1] = 0x02;
        
        // Linker version
        header[2] = 0x0E;
        header[3] = 0x00;
        
        // Size of code
        WriteInt32(header, 4, codeSize);
        
        // Size of initialized data
        WriteInt32(header, 8, 0);
        
        // Size of uninitialized data
        WriteInt32(header, 12, 0);
        
        // Address of entry point
        WriteInt32(header, 16, entryPointRVA);
        
        // Base of code
        WriteInt32(header, 20, 0x1000);
        
        // Image base (64-bit)
        WriteInt64(header, 24, imageBase);
        
        // Section alignment
        WriteInt32(header, 32, 0x1000);
        
        // File alignment
        WriteInt32(header, 36, 0x200);
        
        // OS version
        header[40] = 0x05;
        header[41] = 0x00;
        header[42] = 0x02;
        header[43] = 0x00;
        
        // Image version
        header[44] = 0x00;
        header[45] = 0x00;
        header[46] = 0x00;
        header[47] = 0x00;
        
        // Subsystem version
        header[48] = 0x05;
        header[49] = 0x00;
        header[50] = 0x02;
        header[51] = 0x00;
        
        // Size of image
        WriteInt32(header, 56, 0x3000);
        
        // Size of headers
        WriteInt32(header, 60, 0x200);
        
        // Subsystem (3 = console)
        header[68] = 0x03;
        header[69] = 0x00;
        
        // DLL characteristics
        header[70] = 0x00;
        header[71] = 0x00;
        
        // Stack reserve/commit
        WriteInt64(header, 72, 0x100000);
        WriteInt64(header, 80, 0x1000);
        
        // Heap reserve/commit
        WriteInt64(header, 88, 0x100000);
        WriteInt64(header, 96, 0x1000);
        
        // Number of data directories
        WriteInt32(header, 108, 16);
        
        return header;
    }
    
    private static byte[] CreateCodeSection(int codeSize)
    {
        var section = new byte[40];
        
        // Name (".text")
        section[0] = (byte)'.';
        section[1] = (byte)'t';
        section[2] = (byte)'e';
        section[3] = (byte)'x';
        section[4] = (byte)'t';
        
        // Virtual size
        WriteInt32(section, 8, codeSize);
        
        // Virtual address
        WriteInt32(section, 12, 0x1000);
        
        // Size of raw data
        int alignedSize = ((codeSize + 511) / 512) * 512;
        WriteInt32(section, 16, alignedSize);
        
        // Pointer to raw data
        WriteInt32(section, 20, 0x200);
        
        // Characteristics (0x60000020 = code, executable, readable)
        WriteInt32(section, 36, 0x60000020);
        
        return section;
    }
    
    private static void WriteInt32(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
    }
    
    private static void WriteInt64(byte[] buffer, int offset, long value)
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
