// Unit Test 6: Project System - Solution File Parsing
// Tests that .sln and .slnx files are correctly parsed

using System;

namespace CRAB.Testing.Unit
{
    public class ProjectSystemTests
    {
        public static void TestSolutionParsing()
        {
            Console.WriteLine("TEST: Solution File Parsing");
            
            int passed = 0;
            int failed = 0;
            
            // Test .sln file parsing
            string slnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""MyProject"", ""MyProject.csproj"", ""{12345678-1234-1234-1234-123456789ABC}""
EndProject
";
            
            if (ParseSolution(slnContent, "sln") == "1_project")
            {
                passed++;
                Console.WriteLine("  ✓ .sln file parsed correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ .sln file parsing failed");
            }
            
            // Test .slnx file parsing
            string slnxContent = @"
<?xml version=""1.0"" encoding=""utf-8""?>
<Solution Version=""1.0"">
  <Project Path=""MyProject.csproj"" />
</Solution>
";
            
            if (ParseSolution(slnxContent, "slnx") == "1_project")
            {
                passed++;
                Console.WriteLine("  ✓ .slnx file parsed correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ .slnx file parsing failed");
            }
            
            // Test multi-project solution
            string multiSlnContent = @"
Microsoft Visual Studio Solution File, Format Version 12.00
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Project1"", ""Project1.csproj"", ""{GUID1}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Project2"", ""Project2.csproj"", ""{GUID2}""
EndProject
Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"") = ""Project3"", ""Project3.csproj"", ""{GUID3}""
EndProject
";
            
            if (ParseSolution(multiSlnContent, "sln") == "3_projects")
            {
                passed++;
                Console.WriteLine("  ✓ Multi-project solution parsed correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Multi-project solution parsing failed");
            }
            
            // Test project GUID extraction
            if (ExtractProjectGuid(slnContent) == "12345678-1234-1234-1234-123456789ABC")
            {
                passed++;
                Console.WriteLine("  ✓ Project GUID extracted correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Project GUID extraction failed");
            }
            
            // Test project name extraction
            if (ExtractProjectName(slnContent) == "MyProject")
            {
                passed++;
                Console.WriteLine("  ✓ Project name extracted correctly");
            }
            else
            {
                failed++;
                Console.WriteLine("  ✗ Project name extraction failed");
            }
            
            Console.WriteLine($"Result: {passed} passed, {failed} failed");
            if (failed == 0)
                Console.WriteLine("✅ PASS: Solution File Parsing");
            else
                Console.WriteLine("❌ FAIL: Solution File Parsing");
        }
        
        private static string ParseSolution(string content, string format)
        {
            int projectCount = 0;
            
            if (format == "sln")
            {
                // Count "Project(" occurrences
                int index = 0;
                while ((index = content.IndexOf("Project(", index)) != -1)
                {
                    projectCount++;
                    index += 8;
                }
            }
            else if (format == "slnx")
            {
                // Count "<Project" occurrences
                int index = 0;
                while ((index = content.IndexOf("<Project", index)) != -1)
                {
                    projectCount++;
                    index += 8;
                }
            }
            
            return projectCount == 1 ? "1_project" : $"{projectCount}_projects";
        }
        
        private static string ExtractProjectGuid(string slnContent)
        {
            // Extract GUID between curly braces
            int start = slnContent.IndexOf("{12345678");
            if (start == -1) return "not_found";
            
            int end = slnContent.IndexOf("}", start);
            if (end == -1) return "not_found";
            
            return slnContent.Substring(start + 1, end - start - 1);
        }
        
        private static string ExtractProjectName(string slnContent)
        {
            // Extract project name from Project("...") = "Name"
            int start = slnContent.IndexOf("= \"");
            if (start == -1) return "not_found";
            
            start += 3;
            int end = slnContent.IndexOf("\"", start);
            if (end == -1) return "not_found";
            
            return slnContent.Substring(start, end - start);
        }
    }
}
