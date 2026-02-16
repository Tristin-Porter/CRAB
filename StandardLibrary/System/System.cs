// CRAB Standard Library - System.cs
// Core System namespace types and functionality

namespace System
{
    // Basic value types
    public struct Boolean
    {
        private int value;
        
        public Boolean(bool value)
        {
            this.value = value ? 1 : 0;
        }
        
        public bool Value
        {
            get { return value != 0; }
        }
    }
    
    public struct Int32
    {
        private int value;
        
        public Int32(int value)
        {
            this.value = value;
        }
        
        public int Value
        {
            get { return value; }
        }
    }
    
    public struct Int64
    {
        private long value;
        
        public Int64(long value)
        {
            this.value = value;
        }
        
        public long Value
        {
            get { return value; }
        }
    }
    
    public struct Char
    {
        private int value;
        
        public Char(char value)
        {
            this.value = (int)value;
        }
        
        public char Value
        {
            get { return (char)value; }
        }
    }
    
    // String type
    public class String
    {
        private char[] data;
        private int length;
        
        public String(char[] chars)
        {
            data = chars;
            length = chars.Length;
        }
        
        public int Length
        {
            get { return length; }
        }
        
        public char this[int index]
        {
            get { return data[index]; }
        }
    }
    
    // Object base class
    public class Object
    {
        public Object()
        {
        }
        
        public virtual string ToString()
        {
            return "System.Object";
        }
        
        public virtual bool Equals(object obj)
        {
            return false;
        }
        
        public virtual int GetHashCode()
        {
            return 0;
        }
    }
    
    // Exception types
    public class Exception
    {
        private string message;
        
        public Exception()
        {
            message = "An exception occurred";
        }
        
        public Exception(string message)
        {
            this.message = message;
        }
        
        public string Message
        {
            get { return message; }
        }
    }
    
    public class NullReferenceException : Exception
    {
        public NullReferenceException() : base("Object reference not set to an instance of an object")
        {
        }
        
        public NullReferenceException(string message) : base(message)
        {
        }
    }
    
    public class IndexOutOfRangeException : Exception
    {
        public IndexOutOfRangeException() : base("Index was outside the bounds of the array")
        {
        }
        
        public IndexOutOfRangeException(string message) : base(message)
        {
        }
    }
    
    public class InvalidOperationException : Exception
    {
        public InvalidOperationException() : base("Operation is not valid due to the current state of the object")
        {
        }
        
        public InvalidOperationException(string message) : base(message)
        {
        }
    }
    
    // Array type
    public class Array
    {
        private int length;
        
        public int Length
        {
            get { return length; }
        }
    }
    
    // Void type
    public struct Void
    {
    }
    
    // Guid type
    public struct Guid
    {
        private long data1;
        private long data2;
        
        public Guid(long data1, long data2)
        {
            this.data1 = data1;
            this.data2 = data2;
        }
        
        public static Guid NewGuid()
        {
            // Simple pseudo-random GUID generation
            long ticks = DateTime.UtcNow.Ticks;
            return new Guid(ticks, ticks ^ 0x123456789ABCDEF0);
        }
        
        public string ToString(string format)
        {
            return "{00000000-0000-0000-0000-000000000000}";
        }
    }
    
    // DateTime type
    public struct DateTime
    {
        private long ticks;
        
        public DateTime(long ticks)
        {
            this.ticks = ticks;
        }
        
        public long Ticks
        {
            get { return ticks; }
        }
        
        public static DateTime UtcNow
        {
            get { return new DateTime(0); }
        }
    }
    
    // Basic delegates
    public delegate void Action();
    public delegate void Action<T>(T obj);
    public delegate TResult Func<TResult>();
    public delegate TResult Func<T, TResult>(T arg);
}