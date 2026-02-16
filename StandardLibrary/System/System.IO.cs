// CRAB Standard Library - System.IO.cs
// File system and stream I/O operations

namespace System.IO
{
    // ===== STREAM BASE CLASS =====
    
    public abstract class Stream : IDisposable
    {
        public abstract bool CanRead { get; }
        public abstract bool CanWrite { get; }
        public abstract bool CanSeek { get; }
        public abstract long Length { get; }
        public abstract long Position { get; set; }
        
        public abstract int Read(byte[] buffer, int offset, int count);
        public abstract void Write(byte[] buffer, int offset, int count);
        public abstract long Seek(long offset, SeekOrigin origin);
        public abstract void Flush();
        public abstract void SetLength(long value);
        
        public virtual int ReadByte()
        {
            byte[] buffer = new byte[1];
            int n = Read(buffer, 0, 1);
            return n == 1 ? buffer[0] : -1;
        }
        
        public virtual void WriteByte(byte value)
        {
            byte[] buffer = new byte[1];
            buffer[0] = value;
            Write(buffer, 0, 1);
        }
        
        public virtual void CopyTo(Stream destination)
        {
            CopyTo(destination, 4096);
        }
        
        public virtual void CopyTo(Stream destination, int bufferSize)
        {
            if (destination == null)
                throw new ArgumentNullException("destination");
            if (!CanRead)
                throw new NotSupportedException("Stream does not support reading");
            if (!destination.CanWrite)
                throw new NotSupportedException("Destination stream does not support writing");
            
            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, bytesRead);
            }
        }
        
        public virtual void Close()
        {
            Dispose();
        }
        
        public virtual void Dispose()
        {
            // Override in derived classes
        }
    }
    
    public enum SeekOrigin
    {
        Begin = 0,
        Current = 1,
        End = 2
    }
    
    // ===== MEMORY STREAM =====
    
    public class MemoryStream : Stream
    {
        private byte[] buffer;
        private int position;
        private int length;
        private int capacity;
        private bool writable;
        private bool expandable;
        
        public MemoryStream()
        {
            buffer = new byte[0];
            position = 0;
            length = 0;
            capacity = 0;
            writable = true;
            expandable = true;
        }
        
        public MemoryStream(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");
            
            buffer = new byte[capacity];
            this.capacity = capacity;
            position = 0;
            length = 0;
            writable = true;
            expandable = true;
        }
        
        public MemoryStream(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            
            this.buffer = buffer;
            length = buffer.Length;
            capacity = buffer.Length;
            position = 0;
            writable = true;
            expandable = false;
        }
        
        public MemoryStream(byte[] buffer, bool writable)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            
            this.buffer = buffer;
            length = buffer.Length;
            capacity = buffer.Length;
            position = 0;
            this.writable = writable;
            expandable = false;
        }
        
        public override bool CanRead
        {
            get { return true; }
        }
        
        public override bool CanWrite
        {
            get { return writable; }
        }
        
        public override bool CanSeek
        {
            get { return true; }
        }
        
        public override long Length
        {
            get { return length; }
        }
        
        public override long Position
        {
            get { return position; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                // Allow setting position beyond current length if stream is expandable
                // This is standard behavior for MemoryStream - setting position expands the stream
                if (value > length)
                {
                    if (!writable || !expandable)
                        throw new NotSupportedException("Cannot expand stream");
                    SetLength(value);
                }
                position = (int)value;
            }
        }
        
        public virtual int Capacity
        {
            get { return capacity; }
            set
            {
                if (value < length)
                    throw new ArgumentOutOfRangeException("value");
                if (!expandable)
                    throw new NotSupportedException();
                
                if (value != capacity)
                {
                    byte[] newBuffer = new byte[value];
                    if (length > 0)
                        Array.Copy(buffer, 0, newBuffer, 0, length);
                    buffer = newBuffer;
                    capacity = value;
                }
            }
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            int bytesToRead = Math.Min(count, length - position);
            if (bytesToRead <= 0)
                return 0;
            
            for (int i = 0; i < bytesToRead; i++)
                buffer[offset + i] = this.buffer[position + i];
            
            position += bytesToRead;
            return bytesToRead;
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!writable)
                throw new NotSupportedException("Stream is not writable");
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            int endPosition = position + count;
            if (endPosition > capacity)
            {
                if (expandable)
                {
                    int newCapacity = Math.Max(capacity * 2, endPosition);
                    Capacity = newCapacity;
                }
                else
                    throw new NotSupportedException("Stream cannot expand");
            }
            
            for (int i = 0; i < count; i++)
                this.buffer[position + i] = buffer[offset + i];
            
            position = endPosition;
            if (position > length)
                length = position;
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            int newPosition;
            
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPosition = (int)offset;
                    break;
                case SeekOrigin.Current:
                    newPosition = position + (int)offset;
                    break;
                case SeekOrigin.End:
                    newPosition = length + (int)offset;
                    break;
                default:
                    throw new ArgumentException("Invalid seek origin");
            }
            
            if (newPosition < 0 || newPosition > length)
                throw new IOException("Seek position out of range");
            
            position = newPosition;
            return position;
        }
        
        public override void Flush()
        {
            // No-op for memory stream
        }
        
        public override void SetLength(long value)
        {
            if (!writable)
                throw new NotSupportedException("Stream is not writable");
            if (value < 0 || value > Int32.MaxValue)
                throw new ArgumentOutOfRangeException("value");
            
            int newLength = (int)value;
            if (newLength > capacity)
            {
                if (expandable)
                    Capacity = newLength;
                else
                    throw new NotSupportedException("Stream cannot expand");
            }
            
            length = newLength;
            if (position > length)
                position = length;
        }
        
        public virtual byte[] ToArray()
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
                result[i] = buffer[i];
            return result;
        }
        
        public virtual byte[] GetBuffer()
        {
            return buffer;
        }
    }
    
    // ===== FILE STREAM =====
    
    public class FileStream : Stream
    {
        private string path;
        private FileMode mode;
        private FileAccess access;
        private int handle;
        private long position;
        private long length;
        
        public FileStream(string path, FileMode mode)
            : this(path, mode, mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite)
        {
        }
        
        public FileStream(string path, FileMode mode, FileAccess access)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            
            this.path = path;
            this.mode = mode;
            this.access = access;
            this.handle = -1; // Will be set by WASM file system import
            this.position = 0;
            this.length = 0; // Will be set by WASM file system import
            
            // Actual file opening will be handled by WASM imports
        }
        
        public override bool CanRead
        {
            get { return (access & FileAccess.Read) != 0; }
        }
        
        public override bool CanWrite
        {
            get { return (access & FileAccess.Write) != 0; }
        }
        
        public override bool CanSeek
        {
            get { return true; }
        }
        
        public override long Length
        {
            get { return length; }
        }
        
        public override long Position
        {
            get { return position; }
            set
            {
                Seek(value, SeekOrigin.Begin);
            }
        }
        
        public string Name
        {
            get { return path; }
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException("Stream does not support reading");
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            // Actual read will be handled by WASM imports
            return 0;
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException("Stream does not support writing");
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            // Actual write will be handled by WASM imports
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            // Actual seek will be handled by WASM imports
            return position;
        }
        
        public override void Flush()
        {
            // Flush will be handled by WASM imports
        }
        
        public override void SetLength(long value)
        {
            if (!CanWrite)
                throw new NotSupportedException("Stream does not support writing");
            if (value < 0)
                throw new ArgumentOutOfRangeException("value");
            
            // SetLength will be handled by WASM imports
        }
        
        public override void Dispose()
        {
            if (handle != -1)
            {
                // Close will be handled by WASM imports
                handle = -1;
            }
        }
    }
    
    public enum FileMode
    {
        CreateNew = 1,
        Create = 2,
        Open = 3,
        OpenOrCreate = 4,
        Truncate = 5,
        Append = 6
    }
    
    public enum FileAccess
    {
        Read = 1,
        Write = 2,
        ReadWrite = 3
    }
    
    public enum FileShare
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = 3,
        Delete = 4
    }
    
    // ===== STREAM READER =====
    
    public class StreamReader : IDisposable
    {
        private Stream stream;
        private byte[] buffer;
        private int bufferSize;
        private int bufferPos;
        private int bufferLen;
        private bool disposed;
        
        public StreamReader(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new ArgumentException("Stream must be readable");
            
            this.stream = stream;
            bufferSize = 1024;
            buffer = new byte[bufferSize];
            bufferPos = 0;
            bufferLen = 0;
            disposed = false;
        }
        
        public StreamReader(string path)
            : this(new FileStream(path, FileMode.Open, FileAccess.Read))
        {
        }
        
        public virtual Stream BaseStream
        {
            get { return stream; }
        }
        
        public bool EndOfStream
        {
            get
            {
                if (bufferPos < bufferLen)
                    return false;
                
                int n = stream.Read(buffer, 0, 1);
                if (n > 0)
                {
                    bufferLen = 1;
                    bufferPos = 0;
                    return false;
                }
                
                return true;
            }
        }
        
        public virtual int Read()
        {
            if (bufferPos >= bufferLen)
            {
                bufferLen = stream.Read(buffer, 0, bufferSize);
                bufferPos = 0;
                if (bufferLen == 0)
                    return -1;
            }
            
            return buffer[bufferPos++];
        }
        
        public virtual int Read(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (index < 0 || count < 0 || index + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            int charsRead = 0;
            for (int i = 0; i < count; i++)
            {
                int c = Read();
                if (c == -1)
                    break;
                buffer[index + i] = (char)c;
                charsRead++;
            }
            
            return charsRead;
        }
        
        public virtual string ReadLine()
        {
            char[] lineBuffer = new char[128];
            int linePos = 0;
            
            while (true)
            {
                int c = Read();
                if (c == -1)
                {
                    if (linePos == 0)
                        return null;
                    break;
                }
                
                if (c == '\n')
                    break;
                
                if (c == '\r')
                {
                    int next = Read();
                    if (next != '\n' && next != -1)
                    {
                        // Put it back
                        bufferPos--;
                    }
                    break;
                }
                
                if (linePos >= lineBuffer.Length)
                {
                    char[] newBuffer = new char[lineBuffer.Length * 2];
                    Array.Copy(lineBuffer, 0, newBuffer, 0, linePos);
                    lineBuffer = newBuffer;
                }
                
                lineBuffer[linePos++] = (char)c;
            }
            
            char[] result = new char[linePos];
            for (int i = 0; i < linePos; i++)
                result[i] = lineBuffer[i];
            
            return new string(result);
        }
        
        public virtual string ReadToEnd()
        {
            char[] textBuffer = new char[4096];
            int totalChars = 0;
            
            while (true)
            {
                if (totalChars >= textBuffer.Length)
                {
                    char[] newBuffer = new char[textBuffer.Length * 2];
                    Array.Copy(textBuffer, 0, newBuffer, 0, totalChars);
                    textBuffer = newBuffer;
                }
                
                int c = Read();
                if (c == -1)
                    break;
                
                textBuffer[totalChars++] = (char)c;
            }
            
            char[] result = new char[totalChars];
            for (int i = 0; i < totalChars; i++)
                result[i] = textBuffer[i];
            
            return new string(result);
        }
        
        public virtual void Close()
        {
            Dispose();
        }
        
        public virtual void Dispose()
        {
            if (!disposed)
            {
                stream.Dispose();
                disposed = true;
            }
        }
    }
    
    // ===== STREAM WRITER =====
    
    public class StreamWriter : IDisposable
    {
        private Stream stream;
        private byte[] buffer;
        private int bufferSize;
        private int bufferPos;
        private bool disposed;
        
        public StreamWriter(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanWrite)
                throw new ArgumentException("Stream must be writable");
            
            this.stream = stream;
            bufferSize = 1024;
            buffer = new byte[bufferSize];
            bufferPos = 0;
            disposed = false;
        }
        
        public StreamWriter(string path)
            : this(new FileStream(path, FileMode.Create, FileAccess.Write))
        {
        }
        
        public StreamWriter(string path, bool append)
            : this(new FileStream(path, append ? FileMode.Append : FileMode.Create, FileAccess.Write))
        {
        }
        
        public virtual Stream BaseStream
        {
            get { return stream; }
        }
        
        public bool AutoFlush { get; set; }
        
        public virtual void Write(char value)
        {
            if (bufferPos >= bufferSize)
                Flush();
            
            buffer[bufferPos++] = (byte)value;
            
            if (AutoFlush)
                Flush();
        }
        
        public virtual void Write(string value)
        {
            if (value == null)
                return;
            
            for (int i = 0; i < value.Length; i++)
                Write(value[i]);
        }
        
        public virtual void Write(char[] buffer)
        {
            if (buffer != null)
                Write(buffer, 0, buffer.Length);
        }
        
        public virtual void Write(char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (index < 0 || count < 0 || index + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            for (int i = 0; i < count; i++)
                Write(buffer[index + i]);
        }
        
        public virtual void WriteLine()
        {
            Write('\n');
        }
        
        public virtual void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }
        
        public virtual void WriteLine(char value)
        {
            Write(value);
            WriteLine();
        }
        
        public virtual void Flush()
        {
            if (bufferPos > 0)
            {
                stream.Write(buffer, 0, bufferPos);
                bufferPos = 0;
            }
            stream.Flush();
        }
        
        public virtual void Close()
        {
            Dispose();
        }
        
        public virtual void Dispose()
        {
            if (!disposed)
            {
                Flush();
                stream.Dispose();
                disposed = true;
            }
        }
    }
    
    // ===== FILE STATIC CLASS =====
    
    public static class File
    {
        public static bool Exists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            // Will use WASM file system imports
            return false;
        }
        
        public static void Delete(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path");
            // Will use WASM file system imports
        }
        
        public static void Copy(string sourceFileName, string destFileName)
        {
            Copy(sourceFileName, destFileName, false);
        }
        
        public static void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            if (sourceFileName == null || destFileName == null)
                throw new ArgumentNullException();
            
            using (FileStream source = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read))
            using (FileStream dest = new FileStream(destFileName, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write))
            {
                source.CopyTo(dest);
            }
        }
        
        public static void Move(string sourceFileName, string destFileName)
        {
            if (sourceFileName == null || destFileName == null)
                throw new ArgumentNullException();
            
            Copy(sourceFileName, destFileName);
            Delete(sourceFileName);
        }
        
        public static byte[] ReadAllBytes(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                long length = stream.Length;
                byte[] bytes = new byte[length];
                stream.Read(bytes, 0, (int)length);
                return bytes;
            }
        }
        
        public static string ReadAllText(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                return reader.ReadToEnd();
            }
        }
        
        public static string[] ReadAllLines(string path)
        {
            List<string> lines = new List<string>();
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            return lines.ToArray();
        }
        
        public static void WriteAllBytes(string path, byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                stream.Write(bytes, 0, bytes.Length);
            }
        }
        
        public static void WriteAllText(string path, string contents)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                writer.Write(contents);
            }
        }
        
        public static void WriteAllLines(string path, string[] contents)
        {
            using (StreamWriter writer = new StreamWriter(path))
            {
                for (int i = 0; i < contents.Length; i++)
                {
                    writer.WriteLine(contents[i]);
                }
            }
        }
        
        public static void AppendAllText(string path, string contents)
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.Write(contents);
            }
        }
        
        public static StreamReader OpenText(string path)
        {
            return new StreamReader(path);
        }
        
        public static StreamWriter CreateText(string path)
        {
            return new StreamWriter(path);
        }
        
        public static StreamWriter AppendText(string path)
        {
            return new StreamWriter(path, true);
        }
    }
    
    // ===== DIRECTORY STATIC CLASS =====
    
    public static class Directory
    {
        public static bool Exists(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            // Will use WASM file system imports
            return false;
        }
        
        public static void Create(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path");
            // Will use WASM file system imports
        }
        
        public static void Delete(string path)
        {
            Delete(path, false);
        }
        
        public static void Delete(string path, bool recursive)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path");
            // Will use WASM file system imports
        }
        
        public static string[] GetFiles(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path");
            // Will use WASM file system imports
            return new string[0];
        }
        
        public static string[] GetFiles(string path, string searchPattern)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path");
            // Will use WASM file system imports with pattern matching
            return new string[0];
        }
        
        public static string[] GetDirectories(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path");
            // Will use WASM file system imports
            return new string[0];
        }
        
        public static string GetCurrentDirectory()
        {
            // Will use WASM file system imports
            return "/";
        }
        
        public static void SetCurrentDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("path");
            // Will use WASM file system imports
        }
    }
    
    // ===== PATH STATIC CLASS =====
    
    public static class Path
    {
        public static readonly char DirectorySeparatorChar = '/';
        public static readonly char AltDirectorySeparatorChar = '\\';
        public static readonly char PathSeparator = ':';
        
        public static string Combine(string path1, string path2)
        {
            if (path1 == null || path2 == null)
                throw new ArgumentNullException();
            
            if (path1.Length == 0)
                return path2;
            if (path2.Length == 0)
                return path1;
            
            char lastChar = path1[path1.Length - 1];
            if (lastChar != DirectorySeparatorChar && lastChar != AltDirectorySeparatorChar)
                return path1 + DirectorySeparatorChar + path2;
            
            return path1 + path2;
        }
        
        public static string Combine(params string[] paths)
        {
            if (paths == null)
                throw new ArgumentNullException("paths");
            
            string result = "";
            for (int i = 0; i < paths.Length; i++)
            {
                result = Combine(result, paths[i]);
            }
            return result;
        }
        
        public static string GetFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            int lastSep = path.LastIndexOf(DirectorySeparatorChar);
            if (lastSep < 0)
                lastSep = path.LastIndexOf(AltDirectorySeparatorChar);
            
            return lastSep < 0 ? path : path.Substring(lastSep + 1);
        }
        
        public static string GetFileNameWithoutExtension(string path)
        {
            string fileName = GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
                return fileName;
            
            int dotIndex = fileName.LastIndexOf('.');
            return dotIndex < 0 ? fileName : fileName.Substring(0, dotIndex);
        }
        
        public static string GetExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;
            
            int dotIndex = path.LastIndexOf('.');
            if (dotIndex < 0 || dotIndex == path.Length - 1)
                return string.Empty;
            
            return path.Substring(dotIndex);
        }
        
        public static string GetDirectoryName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
            
            int lastSep = path.LastIndexOf(DirectorySeparatorChar);
            if (lastSep < 0)
                lastSep = path.LastIndexOf(AltDirectorySeparatorChar);
            
            return lastSep <= 0 ? string.Empty : path.Substring(0, lastSep);
        }
        
        public static bool HasExtension(string path)
        {
            return !string.IsNullOrEmpty(GetExtension(path));
        }
        
        public static string ChangeExtension(string path, string extension)
        {
            if (path == null)
                return null;
            
            string dir = GetDirectoryName(path);
            string fileName = GetFileNameWithoutExtension(path);
            
            if (string.IsNullOrEmpty(extension))
                return Combine(dir, fileName);
            
            if (extension[0] != '.')
                extension = "." + extension;
            
            return Combine(dir, fileName + extension);
        }
    }
    
    // ===== EXCEPTIONS =====
    
    public class IOException : Exception
    {
        public IOException() : base("I/O error occurred") { }
        public IOException(string message) : base(message) { }
        public IOException(string message, Exception innerException) : base(message, innerException) { }
    }
    
    public class FileNotFoundException : IOException
    {
        public FileNotFoundException() : base("File not found") { }
        public FileNotFoundException(string message) : base(message) { }
        public FileNotFoundException(string message, string fileName) : base(message + ": " + fileName) { }
    }
    
    public class DirectoryNotFoundException : IOException
    {
        public DirectoryNotFoundException() : base("Directory not found") { }
        public DirectoryNotFoundException(string message) : base(message) { }
    }
    
    public class PathTooLongException : IOException
    {
        public PathTooLongException() : base("Path is too long") { }
        public PathTooLongException(string message) : base(message) { }
    }
    
    public class EndOfStreamException : IOException
    {
        public EndOfStreamException() : base("End of stream reached") { }
        public EndOfStreamException(string message) : base(message) { }
    }
}
