using System;

abstract class Animal
{
    public abstract string MakeSound();
}

class Dog : Animal
{
    public override string MakeSound()
    {
        return "Woof!";
    }
}

class Cat : Animal
{
    public override string MakeSound()
    {
        return "Meow!";
    }
}

class Program
{
    static void Main()
    {
        Animal dog = new Dog();
        Animal cat = new Cat();
        Console.WriteLine($"Dog says: {dog.MakeSound()}");
        Console.WriteLine($"Cat says: {cat.MakeSound()}");
    }
}