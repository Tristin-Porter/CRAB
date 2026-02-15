// Test manual block (should compile without warning)
class TestManual
{
    public static int TestManualBlock()
    {
        manual
        {
            int x = 42;
            return x;
        }
    }
}
