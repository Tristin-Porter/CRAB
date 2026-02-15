using System;
using Newtonsoft.Json;

namespace NuGetTest
{
    public class Program
    {
        public static void Main()
        {
            var person = new { Name = "John", Age = 30 };
            var json = JsonConvert.SerializeObject(person);
            Console.WriteLine(json);
        }
    }
}
