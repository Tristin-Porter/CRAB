// Test unsafe block (should compile with warning)
class TestUnsafe
{
    public static int TestUnsafeBlock()
    {
        unsafe
        {
            int x = 42;
            return x;
        }
    }
}
