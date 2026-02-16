// CRAB Standard Library - System.Text.cs
// Text processing and encoding functionality

namespace System.Text
{
    // ===== STRING BUILDER =====
    
    public class StringBuilder
    {
        private char[] buffer;
        private int length;
        private int capacity;
        
        public StringBuilder()
        {
            capacity = 16;
            buffer = new char[capacity];
            length = 0;
        }
        
        public StringBuilder(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");
            
            this.capacity = capacity;
            buffer = new char[capacity];
            length = 0;
        }
        
        public StringBuilder(string value)
        {
            if (value == null)
                value = string.Empty;
            
            length = value.Length;
            capacity = Math.Max(16, length * 2);
            buffer = new char[capacity];
            
            for (int i = 0; i < length; i++)
                buffer[i] = value[i];
        }
        
        public int Length
        {
            get { return length; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value");
                if (value > capacity)
                    EnsureCapacity(value);
                length = value;
            }
        }
        
        public int Capacity
        {
            get { return capacity; }
            set
            {
                if (value < length)
                    throw new ArgumentOutOfRangeException("value");
                if (value != capacity)
                {
                    char[] newBuffer = new char[value];
                    for (int i = 0; i < length; i++)
                        newBuffer[i] = buffer[i];
                    buffer = newBuffer;
                    capacity = value;
                }
            }
        }
        
        public char this[int index]
        {
            get
            {
                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();
                return buffer[index];
            }
            set
            {
                if (index < 0 || index >= length)
                    throw new IndexOutOfRangeException();
                buffer[index] = value;
            }
        }
        
        public StringBuilder Append(char value)
        {
            if (length >= capacity)
                EnsureCapacity(capacity * 2);
            
            buffer[length++] = value;
            return this;
        }
        
        public StringBuilder Append(string value)
        {
            if (value == null)
                return this;
            
            int requiredCapacity = length + value.Length;
            if (requiredCapacity > capacity)
                EnsureCapacity(Math.Max(capacity * 2, requiredCapacity));
            
            for (int i = 0; i < value.Length; i++)
                buffer[length++] = value[i];
            
            return this;
        }
        
        public StringBuilder Append(char[] value)
        {
            if (value == null)
                return this;
            
            return Append(value, 0, value.Length);
        }
        
        public StringBuilder Append(char[] value, int startIndex, int charCount)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0 || charCount < 0 || startIndex + charCount > value.Length)
                throw new ArgumentOutOfRangeException();
            
            int requiredCapacity = length + charCount;
            if (requiredCapacity > capacity)
                EnsureCapacity(Math.Max(capacity * 2, requiredCapacity));
            
            for (int i = 0; i < charCount; i++)
                buffer[length++] = value[startIndex + i];
            
            return this;
        }
        
        public StringBuilder Append(int value)
        {
            return Append(value.ToString());
        }
        
        public StringBuilder Append(long value)
        {
            return Append(value.ToString());
        }
        
        public StringBuilder Append(bool value)
        {
            return Append(value.ToString());
        }
        
        public StringBuilder Append(object value)
        {
            if (value == null)
                return this;
            return Append(value.ToString());
        }
        
        public StringBuilder AppendLine()
        {
            return Append('\n');
        }
        
        public StringBuilder AppendLine(string value)
        {
            Append(value);
            return AppendLine();
        }
        
        public StringBuilder Insert(int index, char value)
        {
            if (index < 0 || index > length)
                throw new ArgumentOutOfRangeException("index");
            
            if (length >= capacity)
                EnsureCapacity(capacity * 2);
            
            for (int i = length; i > index; i--)
                buffer[i] = buffer[i - 1];
            
            buffer[index] = value;
            length++;
            return this;
        }
        
        public StringBuilder Insert(int index, string value)
        {
            if (index < 0 || index > length)
                throw new ArgumentOutOfRangeException("index");
            if (value == null)
                return this;
            
            int requiredCapacity = length + value.Length;
            if (requiredCapacity > capacity)
                EnsureCapacity(Math.Max(capacity * 2, requiredCapacity));
            
            for (int i = length - 1; i >= index; i--)
                buffer[i + value.Length] = buffer[i];
            
            for (int i = 0; i < value.Length; i++)
                buffer[index + i] = value[i];
            
            length += value.Length;
            return this;
        }
        
        public StringBuilder Remove(int startIndex, int length)
        {
            if (startIndex < 0 || length < 0 || startIndex + length > this.length)
                throw new ArgumentOutOfRangeException();
            
            for (int i = startIndex; i < this.length - length; i++)
                buffer[i] = buffer[i + length];
            
            this.length -= length;
            return this;
        }
        
        public StringBuilder Replace(char oldChar, char newChar)
        {
            for (int i = 0; i < length; i++)
            {
                if (buffer[i] == oldChar)
                    buffer[i] = newChar;
            }
            return this;
        }
        
        public StringBuilder Replace(string oldValue, string newValue)
        {
            if (oldValue == null || oldValue.Length == 0)
                throw new ArgumentException("oldValue");
            if (newValue == null)
                newValue = string.Empty;
            
            int index = 0;
            while (index < length)
            {
                bool found = true;
                if (index + oldValue.Length <= length)
                {
                    for (int i = 0; i < oldValue.Length; i++)
                    {
                        if (buffer[index + i] != oldValue[i])
                        {
                            found = false;
                            break;
                        }
                    }
                    
                    if (found)
                    {
                        Remove(index, oldValue.Length);
                        Insert(index, newValue);
                        index += newValue.Length;
                        continue;
                    }
                }
                
                index++;
            }
            
            return this;
        }
        
        public StringBuilder Clear()
        {
            length = 0;
            return this;
        }
        
        public override string ToString()
        {
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = buffer[i];
            return new string(result);
        }
        
        public string ToString(int startIndex, int length)
        {
            if (startIndex < 0 || length < 0 || startIndex + length > this.length)
                throw new ArgumentOutOfRangeException();
            
            char[] result = new char[length];
            for (int i = 0; i < length; i++)
                result[i] = buffer[startIndex + i];
            return new string(result);
        }
        
        private void EnsureCapacity(int min)
        {
            if (capacity < min)
                Capacity = min;
        }
    }
    
    // ===== ENCODING BASE CLASS =====
    
    public abstract class Encoding
    {
        public abstract int GetByteCount(char[] chars, int index, int count);
        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex);
        public abstract int GetCharCount(byte[] bytes, int index, int count);
        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        public abstract int GetMaxByteCount(int charCount);
        public abstract int GetMaxCharCount(int byteCount);
        
        public virtual int GetByteCount(string s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            
            char[] chars = s.ToCharArray();
            return GetByteCount(chars, 0, chars.Length);
        }
        
        public virtual byte[] GetBytes(string s)
        {
            if (s == null)
                throw new ArgumentNullException("s");
            
            int byteCount = GetByteCount(s);
            byte[] bytes = new byte[byteCount];
            char[] chars = s.ToCharArray();
            GetBytes(chars, 0, chars.Length, bytes, 0);
            return bytes;
        }
        
        public virtual byte[] GetBytes(char[] chars)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            
            int byteCount = GetByteCount(chars, 0, chars.Length);
            byte[] bytes = new byte[byteCount];
            GetBytes(chars, 0, chars.Length, bytes, 0);
            return bytes;
        }
        
        public virtual string GetString(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            
            return GetString(bytes, 0, bytes.Length);
        }
        
        public virtual string GetString(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (index < 0 || count < 0 || index + count > bytes.Length)
                throw new ArgumentOutOfRangeException();
            
            int charCount = GetCharCount(bytes, index, count);
            char[] chars = new char[charCount];
            GetChars(bytes, index, count, chars, 0);
            return new string(chars);
        }
        
        public static Encoding UTF8
        {
            get { return new UTF8Encoding(); }
        }
        
        public static Encoding ASCII
        {
            get { return new ASCIIEncoding(); }
        }
        
        public static Encoding Unicode
        {
            get { return new UnicodeEncoding(); }
        }
    }
    
    // ===== UTF8 ENCODING =====
    
    public class UTF8Encoding : Encoding
    {
        public UTF8Encoding()
        {
        }
        
        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (index < 0 || count < 0 || index + count > chars.Length)
                throw new ArgumentOutOfRangeException();
            
            int byteCount = 0;
            for (int i = 0; i < count; i++)
            {
                int c = chars[index + i];
                
                if (c < 0x80)
                    byteCount += 1;
                else if (c < 0x800)
                    byteCount += 2;
                else if (c < 0x10000)
                    byteCount += 3;
                else
                    byteCount += 4;
            }
            
            return byteCount;
        }
        
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (charIndex < 0 || charCount < 0 || charIndex + charCount > chars.Length)
                throw new ArgumentOutOfRangeException();
            
            int bytePos = byteIndex;
            
            for (int i = 0; i < charCount; i++)
            {
                int c = chars[charIndex + i];
                
                if (c < 0x80)
                {
                    bytes[bytePos++] = (byte)c;
                }
                else if (c < 0x800)
                {
                    bytes[bytePos++] = (byte)(0xC0 | (c >> 6));
                    bytes[bytePos++] = (byte)(0x80 | (c & 0x3F));
                }
                else if (c < 0x10000)
                {
                    bytes[bytePos++] = (byte)(0xE0 | (c >> 12));
                    bytes[bytePos++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                    bytes[bytePos++] = (byte)(0x80 | (c & 0x3F));
                }
                else
                {
                    bytes[bytePos++] = (byte)(0xF0 | (c >> 18));
                    bytes[bytePos++] = (byte)(0x80 | ((c >> 12) & 0x3F));
                    bytes[bytePos++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                    bytes[bytePos++] = (byte)(0x80 | (c & 0x3F));
                }
            }
            
            return bytePos - byteIndex;
        }
        
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (index < 0 || count < 0 || index + count > bytes.Length)
                throw new ArgumentOutOfRangeException();
            
            int charCount = 0;
            int i = 0;
            
            while (i < count)
            {
                byte b = bytes[index + i];
                
                if ((b & 0x80) == 0)
                    i += 1;
                else if ((b & 0xE0) == 0xC0)
                    i += 2;
                else if ((b & 0xF0) == 0xE0)
                    i += 3;
                else if ((b & 0xF8) == 0xF0)
                    i += 4;
                else
                    i += 1; // Invalid sequence
                
                charCount++;
            }
            
            return charCount;
        }
        
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (byteIndex < 0 || byteCount < 0 || byteIndex + byteCount > bytes.Length)
                throw new ArgumentOutOfRangeException();
            
            int charPos = charIndex;
            int i = 0;
            
            while (i < byteCount)
            {
                byte b = bytes[byteIndex + i];
                
                if ((b & 0x80) == 0)
                {
                    chars[charPos++] = (char)b;
                    i += 1;
                }
                else if ((b & 0xE0) == 0xC0)
                {
                    if (i + 1 < byteCount)
                    {
                        int c = ((b & 0x1F) << 6) | (bytes[byteIndex + i + 1] & 0x3F);
                        chars[charPos++] = (char)c;
                    }
                    i += 2;
                }
                else if ((b & 0xF0) == 0xE0)
                {
                    if (i + 2 < byteCount)
                    {
                        int c = ((b & 0x0F) << 12) | ((bytes[byteIndex + i + 1] & 0x3F) << 6) | (bytes[byteIndex + i + 2] & 0x3F);
                        chars[charPos++] = (char)c;
                    }
                    i += 3;
                }
                else if ((b & 0xF8) == 0xF0)
                {
                    // 4-byte sequence - simplified handling
                    chars[charPos++] = '?';
                    i += 4;
                }
                else
                {
                    chars[charPos++] = '?';
                    i += 1;
                }
            }
            
            return charPos - charIndex;
        }
        
        public override int GetMaxByteCount(int charCount)
        {
            return charCount * 4;
        }
        
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }
    }
    
    // ===== ASCII ENCODING =====
    
    public class ASCIIEncoding : Encoding
    {
        public ASCIIEncoding()
        {
        }
        
        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (index < 0 || count < 0 || index + count > chars.Length)
                throw new ArgumentOutOfRangeException();
            
            return count;
        }
        
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (charIndex < 0 || charCount < 0 || charIndex + charCount > chars.Length)
                throw new ArgumentOutOfRangeException();
            
            for (int i = 0; i < charCount; i++)
            {
                char c = chars[charIndex + i];
                bytes[byteIndex + i] = (byte)(c < 128 ? c : '?');
            }
            
            return charCount;
        }
        
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (index < 0 || count < 0 || index + count > bytes.Length)
                throw new ArgumentOutOfRangeException();
            
            return count;
        }
        
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (byteIndex < 0 || byteCount < 0 || byteIndex + byteCount > bytes.Length)
                throw new ArgumentOutOfRangeException();
            
            for (int i = 0; i < byteCount; i++)
            {
                byte b = bytes[byteIndex + i];
                chars[charIndex + i] = (char)(b < 128 ? b : '?');
            }
            
            return byteCount;
        }
        
        public override int GetMaxByteCount(int charCount)
        {
            return charCount;
        }
        
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount;
        }
    }
    
    // ===== UNICODE ENCODING (UTF-16) =====
    
    public class UnicodeEncoding : Encoding
    {
        private bool bigEndian;
        
        public UnicodeEncoding() : this(false)
        {
        }
        
        public UnicodeEncoding(bool bigEndian)
        {
            this.bigEndian = bigEndian;
        }
        
        public override int GetByteCount(char[] chars, int index, int count)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (index < 0 || count < 0 || index + count > chars.Length)
                throw new ArgumentOutOfRangeException();
            
            return count * 2;
        }
        
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (charIndex < 0 || charCount < 0 || charIndex + charCount > chars.Length)
                throw new ArgumentOutOfRangeException();
            
            int bytePos = byteIndex;
            for (int i = 0; i < charCount; i++)
            {
                char c = chars[charIndex + i];
                if (bigEndian)
                {
                    bytes[bytePos++] = (byte)(c >> 8);
                    bytes[bytePos++] = (byte)(c & 0xFF);
                }
                else
                {
                    bytes[bytePos++] = (byte)(c & 0xFF);
                    bytes[bytePos++] = (byte)(c >> 8);
                }
            }
            
            return bytePos - byteIndex;
        }
        
        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (index < 0 || count < 0 || index + count > bytes.Length)
                throw new ArgumentOutOfRangeException();
            
            return count / 2;
        }
        
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            if (bytes == null)
                throw new ArgumentNullException("bytes");
            if (chars == null)
                throw new ArgumentNullException("chars");
            if (byteIndex < 0 || byteCount < 0 || byteIndex + byteCount > bytes.Length)
                throw new ArgumentOutOfRangeException();
            
            int charPos = charIndex;
            for (int i = 0; i < byteCount; i += 2)
            {
                if (i + 1 < byteCount)
                {
                    if (bigEndian)
                        chars[charPos++] = (char)((bytes[byteIndex + i] << 8) | bytes[byteIndex + i + 1]);
                    else
                        chars[charPos++] = (char)(bytes[byteIndex + i] | (bytes[byteIndex + i + 1] << 8));
                }
            }
            
            return charPos - charIndex;
        }
        
        public override int GetMaxByteCount(int charCount)
        {
            return charCount * 2;
        }
        
        public override int GetMaxCharCount(int byteCount)
        {
            return byteCount / 2;
        }
    }
    
    // ===== REGULAR EXPRESSIONS (SIMPLIFIED) =====
    
    public class Regex
    {
        private string pattern;
        
        public Regex(string pattern)
        {
            if (pattern == null)
                throw new ArgumentNullException("pattern");
            this.pattern = pattern;
        }
        
        public bool IsMatch(string input)
        {
            if (input == null)
                return false;
            
            // Simplified pattern matching - only supports literal strings
            return input.Contains(pattern);
        }
        
        public Match Match(string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            
            int index = input.IndexOf(pattern);
            if (index >= 0)
                return new Match(true, pattern, index);
            
            return new Match(false, "", -1);
        }
        
        public string Replace(string input, string replacement)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (replacement == null)
                replacement = "";
            
            return input.Replace(pattern, replacement);
        }
        
        public string[] Split(string input)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            
            // Simplified split - only works for single character patterns
            if (pattern.Length == 1)
                return input.Split(pattern[0]);
            
            return new string[] { input };
        }
        
        public static bool IsMatch(string input, string pattern)
        {
            Regex regex = new Regex(pattern);
            return regex.IsMatch(input);
        }
        
        public static Match Match(string input, string pattern)
        {
            Regex regex = new Regex(pattern);
            return regex.Match(input);
        }
        
        public static string Replace(string input, string pattern, string replacement)
        {
            Regex regex = new Regex(pattern);
            return regex.Replace(input, replacement);
        }
        
        public static string[] Split(string input, string pattern)
        {
            Regex regex = new Regex(pattern);
            return regex.Split(input);
        }
    }
    
    public class Match
    {
        private bool success;
        private string value;
        private int index;
        
        internal Match(bool success, string value, int index)
        {
            this.success = success;
            this.value = value;
            this.index = index;
        }
        
        public bool Success
        {
            get { return success; }
        }
        
        public string Value
        {
            get { return value; }
        }
        
        public int Index
        {
            get { return index; }
        }
        
        public int Length
        {
            get { return value.Length; }
        }
    }
}
