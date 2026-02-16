// CRAB Standard Library - System.Security.cs
// Cryptography, hashing, and security functionality

namespace System.Security.Cryptography
{
    // ===== HASH ALGORITHM BASE CLASS =====
    
    public abstract class HashAlgorithm : IDisposable
    {
        protected int HashSizeValue;
        protected byte[] HashValue;
        
        public virtual int HashSize
        {
            get { return HashSizeValue; }
        }
        
        public virtual byte[] Hash
        {
            get { return HashValue; }
        }
        
        public byte[] ComputeHash(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            return ComputeHash(buffer, 0, buffer.Length);
        }
        
        public byte[] ComputeHash(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            HashCore(buffer, offset, count);
            HashValue = HashFinal();
            return HashValue;
        }
        
        public byte[] ComputeHash(Stream inputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException("inputStream");
            
            byte[] buffer = new byte[4096];
            int bytesRead;
            
            while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                HashCore(buffer, 0, bytesRead);
            }
            
            HashValue = HashFinal();
            return HashValue;
        }
        
        protected abstract void HashCore(byte[] array, int ibStart, int cbSize);
        protected abstract byte[] HashFinal();
        
        public virtual void Initialize()
        {
            HashValue = null;
        }
        
        public virtual void Dispose()
        {
            if (HashValue != null)
            {
                for (int i = 0; i < HashValue.Length; i++)
                    HashValue[i] = 0;
            }
        }
    }
    
    // ===== MD5 HASH ALGORITHM =====
    
    public class MD5 : HashAlgorithm
    {
        private uint[] state;
        private byte[] buffer;
        private uint[] count;
        
        public MD5()
        {
            HashSizeValue = 128;
            state = new uint[4];
            buffer = new byte[64];
            count = new uint[2];
            Initialize();
        }
        
        public static MD5 Create()
        {
            return new MD5();
        }
        
        public override void Initialize()
        {
            state[0] = 0x67452301;
            state[1] = 0xefcdab89;
            state[2] = 0x98badcfe;
            state[3] = 0x10325476;
            count[0] = 0;
            count[1] = 0;
            
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0;
        }
        
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            // Simplified MD5 implementation
            // Full implementation would require complex bit operations
            // This is a stub for CRAB compilation
        }
        
        protected override byte[] HashFinal()
        {
            byte[] hash = new byte[16];
            
            for (int i = 0; i < 4; i++)
            {
                hash[i * 4] = (byte)(state[i] & 0xFF);
                hash[i * 4 + 1] = (byte)((state[i] >> 8) & 0xFF);
                hash[i * 4 + 2] = (byte)((state[i] >> 16) & 0xFF);
                hash[i * 4 + 3] = (byte)((state[i] >> 24) & 0xFF);
            }
            
            return hash;
        }
    }
    
    // ===== SHA1 HASH ALGORITHM =====
    
    public class SHA1 : HashAlgorithm
    {
        private uint[] state;
        private byte[] buffer;
        private long count;
        
        public SHA1()
        {
            HashSizeValue = 160;
            state = new uint[5];
            buffer = new byte[64];
            count = 0;
            Initialize();
        }
        
        public static SHA1 Create()
        {
            return new SHA1();
        }
        
        public override void Initialize()
        {
            state[0] = 0x67452301;
            state[1] = 0xEFCDAB89;
            state[2] = 0x98BADCFE;
            state[3] = 0x10325476;
            state[4] = 0xC3D2E1F0;
            count = 0;
            
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0;
        }
        
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            // Simplified SHA1 implementation stub
        }
        
        protected override byte[] HashFinal()
        {
            byte[] hash = new byte[20];
            
            for (int i = 0; i < 5; i++)
            {
                hash[i * 4] = (byte)((state[i] >> 24) & 0xFF);
                hash[i * 4 + 1] = (byte)((state[i] >> 16) & 0xFF);
                hash[i * 4 + 2] = (byte)((state[i] >> 8) & 0xFF);
                hash[i * 4 + 3] = (byte)(state[i] & 0xFF);
            }
            
            return hash;
        }
    }
    
    // ===== SHA256 HASH ALGORITHM =====
    
    public class SHA256 : HashAlgorithm
    {
        private uint[] state;
        private byte[] buffer;
        private long count;
        
        public SHA256()
        {
            HashSizeValue = 256;
            state = new uint[8];
            buffer = new byte[64];
            count = 0;
            Initialize();
        }
        
        public static SHA256 Create()
        {
            return new SHA256();
        }
        
        public override void Initialize()
        {
            state[0] = 0x6a09e667;
            state[1] = 0xbb67ae85;
            state[2] = 0x3c6ef372;
            state[3] = 0xa54ff53a;
            state[4] = 0x510e527f;
            state[5] = 0x9b05688c;
            state[6] = 0x1f83d9ab;
            state[7] = 0x5be0cd19;
            count = 0;
            
            for (int i = 0; i < buffer.Length; i++)
                buffer[i] = 0;
        }
        
        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            // Simplified SHA256 implementation stub
        }
        
        protected override byte[] HashFinal()
        {
            byte[] hash = new byte[32];
            
            for (int i = 0; i < 8; i++)
            {
                hash[i * 4] = (byte)((state[i] >> 24) & 0xFF);
                hash[i * 4 + 1] = (byte)((state[i] >> 16) & 0xFF);
                hash[i * 4 + 2] = (byte)((state[i] >> 8) & 0xFF);
                hash[i * 4 + 3] = (byte)(state[i] & 0xFF);
            }
            
            return hash;
        }
    }
    
    // ===== SYMMETRIC ALGORITHM BASE CLASS =====
    
    public abstract class SymmetricAlgorithm : IDisposable
    {
        protected int KeySizeValue;
        protected int BlockSizeValue;
        protected byte[] KeyValue;
        protected byte[] IVValue;
        protected CipherMode ModeValue;
        protected PaddingMode PaddingValue;
        
        public virtual int KeySize
        {
            get { return KeySizeValue; }
            set { KeySizeValue = value; }
        }
        
        public virtual int BlockSize
        {
            get { return BlockSizeValue; }
            set { BlockSizeValue = value; }
        }
        
        public virtual byte[] Key
        {
            get { return KeyValue; }
            set { KeyValue = value; }
        }
        
        public virtual byte[] IV
        {
            get { return IVValue; }
            set { IVValue = value; }
        }
        
        public virtual CipherMode Mode
        {
            get { return ModeValue; }
            set { ModeValue = value; }
        }
        
        public virtual PaddingMode Padding
        {
            get { return PaddingValue; }
            set { PaddingValue = value; }
        }
        
        public abstract ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV);
        public abstract ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV);
        
        public virtual ICryptoTransform CreateEncryptor()
        {
            return CreateEncryptor(KeyValue, IVValue);
        }
        
        public virtual ICryptoTransform CreateDecryptor()
        {
            return CreateDecryptor(KeyValue, IVValue);
        }
        
        public virtual void GenerateKey()
        {
            // Generate random key - will use WASM crypto.getRandomValues
            KeyValue = new byte[KeySizeValue / 8];
        }
        
        public virtual void GenerateIV()
        {
            // Generate random IV - will use WASM crypto.getRandomValues
            IVValue = new byte[BlockSizeValue / 8];
        }
        
        public virtual void Dispose()
        {
            if (KeyValue != null)
            {
                for (int i = 0; i < KeyValue.Length; i++)
                    KeyValue[i] = 0;
            }
            if (IVValue != null)
            {
                for (int i = 0; i < IVValue.Length; i++)
                    IVValue[i] = 0;
            }
        }
    }
    
    // ===== AES ALGORITHM =====
    
    public class Aes : SymmetricAlgorithm
    {
        public Aes()
        {
            KeySizeValue = 256;
            BlockSizeValue = 128;
            ModeValue = CipherMode.CBC;
            PaddingValue = PaddingMode.PKCS7;
            
            KeyValue = new byte[32];
            IVValue = new byte[16];
        }
        
        public static Aes Create()
        {
            return new Aes();
        }
        
        public override ICryptoTransform CreateEncryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");
            if (rgbIV == null)
                throw new ArgumentNullException("rgbIV");
            
            return new AesTransform(rgbKey, rgbIV, true);
        }
        
        public override ICryptoTransform CreateDecryptor(byte[] rgbKey, byte[] rgbIV)
        {
            if (rgbKey == null)
                throw new ArgumentNullException("rgbKey");
            if (rgbIV == null)
                throw new ArgumentNullException("rgbIV");
            
            return new AesTransform(rgbKey, rgbIV, false);
        }
        
        private class AesTransform : ICryptoTransform
        {
            private byte[] key;
            private byte[] iv;
            private bool encrypting;
            
            public AesTransform(byte[] key, byte[] iv, bool encrypting)
            {
                this.key = key;
                this.iv = iv;
                this.encrypting = encrypting;
            }
            
            public int InputBlockSize
            {
                get { return 16; }
            }
            
            public int OutputBlockSize
            {
                get { return 16; }
            }
            
            public bool CanTransformMultipleBlocks
            {
                get { return true; }
            }
            
            public bool CanReuseTransform
            {
                get { return false; }
            }
            
            public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
            {
                // AES encryption/decryption would be implemented here
                // This is a stub - actual implementation will use WASM crypto API
                return inputCount;
            }
            
            public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
            {
                byte[] output = new byte[inputCount];
                TransformBlock(inputBuffer, inputOffset, inputCount, output, 0);
                return output;
            }
            
            public void Dispose()
            {
                if (key != null)
                {
                    for (int i = 0; i < key.Length; i++)
                        key[i] = 0;
                }
                if (iv != null)
                {
                    for (int i = 0; i < iv.Length; i++)
                        iv[i] = 0;
                }
            }
        }
    }
    
    // ===== RSA ALGORITHM =====
    
    public class RSA : IDisposable
    {
        protected int KeySizeValue;
        
        public RSA()
        {
            KeySizeValue = 2048;
        }
        
        public static RSA Create()
        {
            return new RSA();
        }
        
        public virtual int KeySize
        {
            get { return KeySizeValue; }
            set { KeySizeValue = value; }
        }
        
        public virtual byte[] Encrypt(byte[] data, bool fOAEP)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            
            // RSA encryption stub - will use WASM crypto API
            return new byte[KeySizeValue / 8];
        }
        
        public virtual byte[] Decrypt(byte[] data, bool fOAEP)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            
            // RSA decryption stub - will use WASM crypto API
            return new byte[0];
        }
        
        public virtual byte[] SignData(byte[] data, HashAlgorithm hash)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (hash == null)
                throw new ArgumentNullException("hash");
            
            byte[] hashValue = hash.ComputeHash(data);
            return SignHash(hashValue);
        }
        
        public virtual bool VerifyData(byte[] data, byte[] signature, HashAlgorithm hash)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            if (signature == null)
                throw new ArgumentNullException("signature");
            if (hash == null)
                throw new ArgumentNullException("hash");
            
            byte[] hashValue = hash.ComputeHash(data);
            return VerifyHash(hashValue, signature);
        }
        
        public virtual byte[] SignHash(byte[] hash)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            
            // RSA signing stub - will use WASM crypto API
            return new byte[KeySizeValue / 8];
        }
        
        public virtual bool VerifyHash(byte[] hash, byte[] signature)
        {
            if (hash == null)
                throw new ArgumentNullException("hash");
            if (signature == null)
                throw new ArgumentNullException("signature");
            
            // RSA verification stub - will use WASM crypto API
            return false;
        }
        
        public virtual void Dispose()
        {
            // Cleanup
        }
    }
    
    // ===== CRYPTO STREAM =====
    
    public class CryptoStream : Stream
    {
        private Stream stream;
        private ICryptoTransform transform;
        private CryptoStreamMode mode;
        private byte[] inputBuffer;
        private int inputBufferIndex;
        private byte[] outputBuffer;
        private int outputBufferIndex;
        private int outputBufferCount;
        
        public CryptoStream(Stream stream, ICryptoTransform transform, CryptoStreamMode mode)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (transform == null)
                throw new ArgumentNullException("transform");
            
            this.stream = stream;
            this.transform = transform;
            this.mode = mode;
            
            inputBuffer = new byte[transform.InputBlockSize];
            inputBufferIndex = 0;
            outputBuffer = new byte[transform.OutputBlockSize];
            outputBufferIndex = 0;
            outputBufferCount = 0;
        }
        
        public override bool CanRead
        {
            get { return mode == CryptoStreamMode.Read; }
        }
        
        public override bool CanWrite
        {
            get { return mode == CryptoStreamMode.Write; }
        }
        
        public override bool CanSeek
        {
            get { return false; }
        }
        
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }
        
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
                throw new NotSupportedException();
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            // Simplified read implementation
            int bytesRead = stream.Read(inputBuffer, 0, inputBuffer.Length);
            if (bytesRead > 0)
            {
                int outputBytes = transform.TransformBlock(inputBuffer, 0, bytesRead, buffer, offset);
                return outputBytes;
            }
            
            return 0;
        }
        
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!CanWrite)
                throw new NotSupportedException();
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException();
            
            // Simplified write implementation
            byte[] output = new byte[count];
            int outputBytes = transform.TransformBlock(buffer, offset, count, output, 0);
            stream.Write(output, 0, outputBytes);
        }
        
        public override void Flush()
        {
            if (mode == CryptoStreamMode.Write)
            {
                byte[] finalBlock = transform.TransformFinalBlock(inputBuffer, 0, inputBufferIndex);
                if (finalBlock.Length > 0)
                    stream.Write(finalBlock, 0, finalBlock.Length);
            }
            stream.Flush();
        }
        
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }
        
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }
        
        public override void Dispose()
        {
            if (transform != null)
            {
                transform.Dispose();
                transform = null;
            }
            if (stream != null)
            {
                stream.Dispose();
                stream = null;
            }
        }
    }
    
    // ===== RANDOM NUMBER GENERATOR =====
    
    public class RandomNumberGenerator : IDisposable
    {
        public RandomNumberGenerator()
        {
        }
        
        public static RandomNumberGenerator Create()
        {
            return new RandomNumberGenerator();
        }
        
        public virtual void GetBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            
            // Will use WASM crypto.getRandomValues
            // For now, use pseudo-random fallback
            Random rng = new Random();
            rng.NextBytes(data);
        }
        
        public virtual void GetNonZeroBytes(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data");
            
            GetBytes(data);
            
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                    data[i] = 1;
            }
        }
        
        public virtual void Dispose()
        {
            // Cleanup
        }
    }
    
    // ===== INTERFACES =====
    
    public interface ICryptoTransform : IDisposable
    {
        int InputBlockSize { get; }
        int OutputBlockSize { get; }
        bool CanTransformMultipleBlocks { get; }
        bool CanReuseTransform { get; }
        int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset);
        byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount);
    }
    
    // ===== ENUMS =====
    
    /// <summary>
    /// Cipher modes for symmetric encryption algorithms.
    /// WARNING: ECB mode is cryptographically insecure and should NEVER be used in production.
    /// Use CBC, OFB, CFB, or CTS instead.
    /// </summary>
    public enum CipherMode
    {
        CBC = 1,        // Cipher Block Chaining - RECOMMENDED for general use
        ECB = 2,        // Electronic Code Book - INSECURE: Reveals patterns in plaintext, vulnerable to attacks
        OFB = 3,        // Output Feedback - Good for streaming data
        CFB = 4,        // Cipher Feedback - Good for streaming data
        CTS = 5         // Cipher Text Stealing - Good for non-block-aligned data
    }
    
    public enum PaddingMode
    {
        None = 1,
        PKCS7 = 2,
        Zeros = 3,
        ANSIX923 = 4,
        ISO10126 = 5
    }
    
    public enum CryptoStreamMode
    {
        Read = 0,
        Write = 1
    }
    
    // ===== CRYPTOGRAPHIC EXCEPTION =====
    
    public class CryptographicException : Exception
    {
        public CryptographicException() : base("A cryptographic error occurred")
        {
        }
        
        public CryptographicException(string message) : base(message)
        {
        }
        
        public CryptographicException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}

namespace System.Security
{
    // ===== SECURE STRING =====
    
    public sealed class SecureString : IDisposable
    {
        private char[] data;
        private int length;
        private bool readOnly;
        
        public SecureString()
        {
            data = new char[16];
            length = 0;
            readOnly = false;
        }
        
        public int Length
        {
            get { return length; }
        }
        
        public void AppendChar(char c)
        {
            if (readOnly)
                throw new InvalidOperationException("SecureString is read-only");
            
            if (length >= data.Length)
            {
                char[] newData = new char[data.Length * 2];
                for (int i = 0; i < length; i++)
                    newData[i] = data[i];
                
                // Clear old array
                for (int i = 0; i < data.Length; i++)
                    data[i] = '\0';
                
                data = newData;
            }
            
            data[length++] = c;
        }
        
        public void Clear()
        {
            if (readOnly)
                throw new InvalidOperationException("SecureString is read-only");
            
            for (int i = 0; i < length; i++)
                data[i] = '\0';
            
            length = 0;
        }
        
        public void MakeReadOnly()
        {
            readOnly = true;
        }
        
        public bool IsReadOnly()
        {
            return readOnly;
        }
        
        public void Dispose()
        {
            Clear();
        }
    }
}
