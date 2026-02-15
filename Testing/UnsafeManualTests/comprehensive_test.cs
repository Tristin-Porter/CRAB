// Comprehensive test demonstrating manual and unsafe block support
// This file tests that both keywords work correctly

class ComprehensiveTest
{
    // Test 1: Simple manual block
    public static int TestManual()
    {
        manual
        {
            int x = 10;
            int y = 20;
            return x + y;
        }
    }
    
    // Test 2: Simple unsafe block (should warn)
    public static int TestUnsafe()
    {
        unsafe
        {
            int x = 10;
            int y = 20;
            return x + y;
        }
    }
    
    // Test 3: Another manual block
    public static int TestManualAgain()
    {
        manual
        {
            int result = 42;
            return result;
        }
    }
    
    // Test 4: Another unsafe block (should warn)
    public static int TestUnsafeAgain()
    {
        unsafe
        {
            int result = 42;
            return result;
        }
    }
}
