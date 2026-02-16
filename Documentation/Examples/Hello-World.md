# Hello World Examples

## Basic Hello World

```csharp
using System;

class Program
{
    static void Main()
    {
        Console.WriteLine("Hello from CRAB!");
    }
}
```

**Compile:**
```bash
crab compile HelloWorld.cs
```

**Run:**
```bash
node output.js
```

## Hello World with Input

```csharp
using System;

class Program
{
    static void Main()
    {
        Console.Write("Enter your name: ");
        string name = Console.ReadLine();
        Console.WriteLine($"Hello, {name}!");
    }
}
```

## Hello World with Arguments

```csharp
using System;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            Console.WriteLine($"Hello, {args[0]}!");
        }
        else
        {
            Console.WriteLine("Hello, World!");
        }
    }
}
```

**Run with arguments:**
```bash
crab run HelloWorld.cs --args Alice
```

## Hello World Class

```csharp
using System;

class Greeter
{
    private string name;
    
    public Greeter(string name)
    {
        this.name = name;
    }
    
    public void Greet()
    {
        Console.WriteLine($"Hello, {name}!");
    }
}

class Program
{
    static void Main()
    {
        var greeter = new Greeter("CRAB");
        greeter.Greet();
    }
}
```

## Hello World with Loop

```csharp
using System;

class Program
{
    static void Main()
    {
        string[] greetings = { "Hello", "Hi", "Greetings", "Welcome" };
        
        foreach (var greeting in greetings)
        {
            Console.WriteLine($"{greeting} from CRAB!");
        }
    }
}
```

## Multi-File Hello World

**Greeter.cs:**
```csharp
using System;

public class Greeter
{
    public static void SayHello(string name)
    {
        Console.WriteLine($"Hello, {name}!");
    }
}
```

**Program.cs:**
```csharp
class Program
{
    static void Main()
    {
        Greeter.SayHello("World");
    }
}
```

**Compile:**
```bash
crab compile Greeter.cs Program.cs
```

## Hello World with Generics

```csharp
using System;

class Greeter<T>
{
    private T value;
    
    public Greeter(T value)
    {
        this.value = value;
    }
    
    public void Greet()
    {
        Console.WriteLine($"Hello, {value}!");
    }
}

class Program
{
    static void Main()
    {
        var stringGreeter = new Greeter<string>("World");
        stringGreeter.Greet();
        
        var intGreeter = new Greeter<int>(42);
        intGreeter.Greet();
    }
}
```

## Hello World with LINQ

```csharp
using System;
using System.Linq;

class Program
{
    static void Main()
    {
        var names = new[] { "Alice", "Bob", "Charlie" };
        
        var greetings = names.Select(name => $"Hello, {name}!");
        
        foreach (var greeting in greetings)
        {
            Console.WriteLine(greeting);
        }
    }
}
```

## Hello World with Async

```csharp
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        await GreetAsync();
    }
    
    static async Task GreetAsync()
    {
        await Task.Delay(1000);
        Console.WriteLine("Hello from CRAB (async)!");
    }
}
```

## Hello World with Interface

```csharp
using System;

interface IGreeter
{
    void Greet();
}

class EnglishGreeter : IGreeter
{
    public void Greet()
    {
        Console.WriteLine("Hello!");
    }
}

class SpanishGreeter : IGreeter
{
    public void Greet()
    {
        Console.WriteLine("¡Hola!");
    }
}

class Program
{
    static void Main()
    {
        IGreeter[] greeters = { new EnglishGreeter(), new SpanishGreeter() };
        
        foreach (var greeter in greeters)
        {
            greeter.Greet();
        }
    }
}
```

## Hello World with Pattern Matching

```csharp
using System;

class Program
{
    static void Main()
    {
        object[] items = { "World", 42, true, 3.14 };
        
        foreach (var item in items)
        {
            var message = item switch
            {
                string s => $"Hello, {s}!",
                int i => $"Hello, number {i}!",
                bool b => $"Hello, {(b ? "true" : "false")}!",
                double d => $"Hello, {d}!",
                _ => "Hello, unknown!"
            };
            
            Console.WriteLine(message);
        }
    }
}
```

## Hello World with Records

```csharp
using System;

record Person(string Name, int Age);

class Program
{
    static void Main()
    {
        var person = new Person("Alice", 30);
        Console.WriteLine($"Hello, {person.Name} (age {person.Age})!");
    }
}
```

## Hello World with Properties

```csharp
using System;

class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    
    public string Greeting => $"Hello, I'm {Name} and I'm {Age} years old!";
}

class Program
{
    static void Main()
    {
        var person = new Person { Name = "Bob", Age = 25 };
        Console.WriteLine(person.Greeting);
    }
}
```

## Compiled Output

Each example compiles to:
- `output.wasm` - WebAssembly binary
- `output.js` - JavaScript loader
- `index.html` - HTML wrapper

**Run in browser:**
```bash
# Open index.html in a browser
```

**Run with Node.js:**
```bash
node output.js
```

**Compile to native:**
```bash
# Windows PE
crab compile --arch x86_64 --format pe --output hello.exe HelloWorld.cs

# Linux ELF
crab compile --arch x86_64 --format elf --output hello HelloWorld.cs
```

## See Also

- [Getting Started](../Guides/Getting-Started.md) - Quick start guide
- [Language Features](../Guides/Language-Features.md) - C# language support
- [Advanced Examples](Advanced-Features.md) - Complex examples
