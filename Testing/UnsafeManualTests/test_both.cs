// Test both manual and unsafe blocks
class TestBoth
{
    public static int TestManual()
    {
        manual
        {
            int x = 10;
            return x;
        }
    }
    
    public static int TestUnsafe()
    {
        unsafe
        {
            int y = 20;
            return y;
        }
    }
}
