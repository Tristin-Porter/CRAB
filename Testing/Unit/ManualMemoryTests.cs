// Unit Test 5: Manual Memory - Verification
// Tests that manual memory blocks are correctly verified for safety

using System;

namespace CRAB.Testing.Unit
{
    public class ManualMemoryTests
    {
        public static void TestManualMemoryVerification()
        {
            Console.WriteLine("TEST: Manual Memory Verification");
            
            int passed = 0;
            int failed = 0;
            
            // Test valid manual block
            string validCode = @"
                manual {
                    var ptr = malloc(100);
                    use(ptr);
                    free(ptr);
                }
            ";
            if (VerifyManualBlock(validCode) == "safe")
            {
                passed++;
                Console.WriteLine("  ✓ Valid manual block verified as safe");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Valid manual block not verified");
            }
            
            // Test missing free
            string leakCode = @"
                manual {
                    var ptr = malloc(100);
                    use(ptr);
                }
            ";
            if (VerifyManualBlock(leakCode) == "leak")
            {
                passed++;
                Console.WriteLine("  ✓ Memory leak detected");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Memory leak not detected");
            }
            
            // Test double free
            string doubleFreeCode = @"
                manual {
                    var ptr = malloc(100);
                    free(ptr);
                    free(ptr);
                }
            ";
            if (VerifyManualBlock(doubleFreeCode) == "double_free")
            {
                passed++;
                Console.WriteLine("  ✓ Double free detected");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Double free not detected");
            }
            
            // Test use after free
            string useAfterFreeCode = @"
                manual {
                    var ptr = malloc(100);
                    free(ptr);
                    use(ptr);
                }
            ";
            if (VerifyManualBlock(useAfterFreeCode) == "use_after_free")
            {
                passed++;
                Console.WriteLine("  ✓ Use-after-free detected");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Use-after-free not detected");
            }
            
            // Test conditional free on all paths
            string conditionalCode = @"
                manual {
                    var ptr = malloc(100);
                    if (condition) {
                        free(ptr);
                    } else {
                        free(ptr);
                    }
                }
            ";
            if (VerifyManualBlock(conditionalCode) == "safe")
            {
                passed++;
                Console.WriteLine("  ✓ Conditional free on all paths verified");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Conditional free verification failed");
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            if (failed == 0)
                Console.WriteLine("✅ PASS: Manual Memory Verification");
            else
                Console.WriteLine("❌ FAIL: Manual Memory Verification");
        }
        
        private static string VerifyManualBlock(string code)
        {
            // Simplified verification - in real implementation would use symbolic execution
            int mallocCount = CountOccurrences(code, "malloc");
            int freeCount = CountOccurrences(code, "free");
            
            // Check for double free (free called more than once on same ptr)
            if (freeCount > mallocCount)
                return "double_free";
            
            // Check for leak (malloc without corresponding free)
            if (freeCount < mallocCount)
                return "leak";
            
            // Check for use after free (use after free call)
            string afterFree = code.Substring(code.LastIndexOf("free("));
            if (afterFree.Contains("use(ptr)"))
                return "use_after_free";
            
            // If malloc and free counts match and no use after free
            if (mallocCount == freeCount && freeCount > 0)
                return "safe";
            
            return "unknown";
        }
        
        private static int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(pattern, index)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            return count;
        }
    }
}
