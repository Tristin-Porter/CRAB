using System;

namespace CRAB.Testing;

/// <summary>
/// Test to verify BADGER integration is working correctly
/// </summary>
public class BADGERIntegrationTest
{
    public static void TestBADGERAPI()
    {
        Console.WriteLine("Testing BADGER Integration...");
        
        // Test 1: Verify BADGER namespace is accessible
        try
        {
            var testWAT = "(module (func $test (result i32) i32.const 42))";
            
            // This should compile without errors when CRAB builds successfully
            byte[] result = Badger.BadgerCompiler.Compile(testWAT, "x86_64", "native");
            
            Console.WriteLine($"✓ BADGER API accessible");
            Console.WriteLine($"✓ Compiled {testWAT.Length} chars of WAT to {result.Length} bytes");
            Console.WriteLine("✓ BADGER integration test PASSED");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ BADGER integration test FAILED: {ex.Message}");
            throw;
        }
    }
}
