// CRAB Standard Library - System.Console.cs
// Console I/O functionality to prevent PE window auto-close

namespace System
{
    public static class Console
    {
        // Console output methods
        public static void Write(string value)
        {
            // WASM implementation will use imported console.log
            // For PE, this will use Windows console API
        }
        
        public static void Write(char value)
        {
            // Write single character
        }
        
        public static void Write(int value)
        {
            // Write integer
        }
        
        public static void Write(long value)
        {
            // Write long integer
        }
        
        public static void Write(bool value)
        {
            // Write boolean
        }
        
        public static void Write(object value)
        {
            if (value != null)
            {
                Write(value.ToString());
            }
        }
        
        public static void WriteLine()
        {
            // Write newline
        }
        
        public static void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }
        
        public static void WriteLine(char value)
        {
            Write(value);
            WriteLine();
        }
        
        public static void WriteLine(int value)
        {
            Write(value);
            WriteLine();
        }
        
        public static void WriteLine(long value)
        {
            Write(value);
            WriteLine();
        }
        
        public static void WriteLine(bool value)
        {
            Write(value);
            WriteLine();
        }
        
        public static void WriteLine(object value)
        {
            Write(value);
            WriteLine();
        }
        
        // Console input methods - these prevent PE window auto-close
        public static string ReadLine()
        {
            // Read a line of text from console
            // For PE, this will use Windows console API and wait for user input
            // This prevents the window from closing
            return "";
        }
        
        public static int Read()
        {
            // Read single character as int
            // Returns -1 on end of stream
            return -1;
        }
        
        public static ConsoleKeyInfo ReadKey()
        {
            // Read a key press - this is critical for preventing PE window auto-close
            // For PE, this will use Windows console API (ReadConsoleInput)
            // For WASM, this will wait for keyboard input
            // NOTE: This is a placeholder implementation. The actual implementation
            // will be provided by the CRAB compiler's PE and WASM backends.
            return new ConsoleKeyInfo('\0', ConsoleKey.None, false, false, false);
        }
        
        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            // Read a key press with option to intercept (not display)
            return ReadKey();
        }
        
        // Console properties
        public static ConsoleColor ForegroundColor { get; set; }
        public static ConsoleColor BackgroundColor { get; set; }
        
        public static void Clear()
        {
            // Clear console screen
        }
        
        public static void ResetColor()
        {
            // Reset console colors to defaults
        }
    }
    
    // ConsoleKeyInfo structure
    public struct ConsoleKeyInfo
    {
        private char keyChar;
        private ConsoleKey key;
        private bool shift;
        private bool alt;
        private bool control;
        
        public ConsoleKeyInfo(char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
        {
            this.keyChar = keyChar;
            this.key = key;
            this.shift = shift;
            this.alt = alt;
            this.control = control;
        }
        
        public char KeyChar
        {
            get { return keyChar; }
        }
        
        public ConsoleKey Key
        {
            get { return key; }
        }
        
        public bool Shift
        {
            get { return shift; }
        }
        
        public bool Alt
        {
            get { return alt; }
        }
        
        public bool Control
        {
            get { return control; }
        }
    }
    
    // ConsoleKey enumeration
    public enum ConsoleKey
    {
        None = 0,
        Backspace = 8,
        Tab = 9,
        Enter = 13,
        Escape = 27,
        Spacebar = 32,
        A = 65,
        B = 66,
        C = 67,
        D = 68,
        E = 69,
        F = 70,
        G = 71,
        H = 72,
        I = 73,
        J = 74,
        K = 75,
        L = 76,
        M = 77,
        N = 78,
        O = 79,
        P = 80,
        Q = 81,
        R = 82,
        S = 83,
        T = 84,
        U = 85,
        V = 86,
        W = 87,
        X = 88,
        Y = 89,
        Z = 90,
        D0 = 48,
        D1 = 49,
        D2 = 50,
        D3 = 51,
        D4 = 52,
        D5 = 53,
        D6 = 54,
        D7 = 55,
        D8 = 56,
        D9 = 57,
        F1 = 112,
        F2 = 113,
        F3 = 114,
        F4 = 115,
        F5 = 116,
        F6 = 117,
        F7 = 118,
        F8 = 119,
        F9 = 120,
        F10 = 121,
        F11 = 122,
        F12 = 123,
        LeftArrow = 37,
        UpArrow = 38,
        RightArrow = 39,
        DownArrow = 40
    }
    
    // ConsoleColor enumeration
    public enum ConsoleColor
    {
        Black = 0,
        DarkBlue = 1,
        DarkGreen = 2,
        DarkCyan = 3,
        DarkRed = 4,
        DarkMagenta = 5,
        DarkYellow = 6,
        Gray = 7,
        DarkGray = 8,
        Blue = 9,
        Green = 10,
        Cyan = 11,
        Red = 12,
        Magenta = 13,
        Yellow = 14,
        White = 15
    }
}