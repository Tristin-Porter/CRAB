// Real-World Integration Tests for CRAB
// Tests complete, practical programs

namespace CRAB.Testing.Integration.RealWorld
{
    /// <summary>
    /// Test: Simple calculator program
    /// Expected: Complete calculator compiles and works
    /// </summary>
    class CalculatorTest
    {
        class Calculator
        {
            public int Add(int a, int b) => a + b;
            public int Subtract(int a, int b) => a - b;
            public int Multiply(int a, int b) => a * b;
            public int Divide(int a, int b) => b != 0 ? a / b : 0;
            
            public int Calculate(string operation, int a, int b)
            {
                return operation switch
                {
                    "add" => Add(a, b),
                    "subtract" => Subtract(a, b),
                    "multiply" => Multiply(a, b),
                    "divide" => Divide(a, b),
                    _ => 0
                };
            }
        }
        
        static void TestCalculator()
        {
            var calc = new Calculator();
            
            int sum = calc.Add(10, 20);
            int diff = calc.Subtract(30, 15);
            int product = calc.Multiply(5, 6);
            int quotient = calc.Divide(100, 5);
            
            int result = calc.Calculate("multiply", 7, 8);
        }
    }

    /// <summary>
    /// Test: Linked list data structure
    /// Expected: Complete linked list implementation works
    /// </summary>
    class LinkedListTest
    {
        class Node<T>
        {
            public T Value { get; set; }
            public Node<T>? Next { get; set; }
            
            public Node(T value)
            {
                Value = value;
            }
        }
        
        class LinkedList<T>
        {
            private Node<T>? head;
            private int count;
            
            public void Add(T value)
            {
                var newNode = new Node<T>(value);
                
                if (head == null)
                {
                    head = newNode;
                }
                else
                {
                    Node<T> current = head;
                    while (current.Next != null)
                    {
                        current = current.Next;
                    }
                    current.Next = newNode;
                }
                
                count++;
            }
            
            public T? Get(int index)
            {
                if (index < 0 || index >= count)
                    return default(T);
                
                Node<T>? current = head;
                for (int i = 0; i < index; i++)
                {
                    current = current?.Next;
                }
                
                return current != null ? current.Value : default(T);
            }
            
            public bool Remove(T value)
            {
                if (head == null)
                    return false;
                
                if (head.Value?.Equals(value) == true)
                {
                    head = head.Next;
                    count--;
                    return true;
                }
                
                Node<T> current = head;
                while (current.Next != null)
                {
                    if (current.Next.Value?.Equals(value) == true)
                    {
                        current.Next = current.Next.Next;
                        count--;
                        return true;
                    }
                    current = current.Next;
                }
                
                return false;
            }
            
            public int Count => count;
        }
        
        static void TestLinkedList()
        {
            var list = new LinkedList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);
            
            int? value = list.Get(1);
            bool removed = list.Remove(20);
            int count = list.Count;
        }
    }

    /// <summary>
    /// Test: Binary search tree
    /// Expected: Complete BST implementation works
    /// </summary>
    class BinarySearchTreeTest
    {
        class TreeNode
        {
            public int Value { get; set; }
            public TreeNode? Left { get; set; }
            public TreeNode? Right { get; set; }
            
            public TreeNode(int value)
            {
                Value = value;
            }
        }
        
        class BinarySearchTree
        {
            private TreeNode? root;
            
            public void Insert(int value)
            {
                if (root == null)
                {
                    root = new TreeNode(value);
                }
                else
                {
                    InsertRecursive(root, value);
                }
            }
            
            private void InsertRecursive(TreeNode node, int value)
            {
                if (value < node.Value)
                {
                    if (node.Left == null)
                        node.Left = new TreeNode(value);
                    else
                        InsertRecursive(node.Left, value);
                }
                else
                {
                    if (node.Right == null)
                        node.Right = new TreeNode(value);
                    else
                        InsertRecursive(node.Right, value);
                }
            }
            
            public bool Search(int value)
            {
                return SearchRecursive(root, value);
            }
            
            private bool SearchRecursive(TreeNode? node, int value)
            {
                if (node == null)
                    return false;
                
                if (value == node.Value)
                    return true;
                
                if (value < node.Value)
                    return SearchRecursive(node.Left, value);
                else
                    return SearchRecursive(node.Right, value);
            }
        }
        
        static void TestBinarySearchTree()
        {
            var bst = new BinarySearchTree();
            bst.Insert(50);
            bst.Insert(30);
            bst.Insert(70);
            bst.Insert(20);
            bst.Insert(40);
            
            bool found = bst.Search(40);
            bool notFound = bst.Search(100);
        }
    }

    /// <summary>
    /// Test: JSON-like object parser (simplified)
    /// Expected: String parsing and object construction
    /// </summary>
    class SimpleParserTest
    {
        class Token
        {
            public string Type { get; set; } = "";
            public string Value { get; set; } = "";
        }
        
        class Lexer
        {
            private string input;
            private int position;
            
            public Lexer(string input)
            {
                this.input = input;
                this.position = 0;
            }
            
            public Token? NextToken()
            {
                if (position >= input.Length)
                    return null;
                
                char current = input[position];
                
                if (char.IsWhiteSpace(current))
                {
                    position++;
                    return NextToken();
                }
                
                if (char.IsDigit(current))
                {
                    int start = position;
                    while (position < input.Length && char.IsDigit(input[position]))
                    {
                        position++;
                    }
                    return new Token
                    {
                        Type = "NUMBER",
                        Value = input.Substring(start, position - start)
                    };
                }
                
                if (current == '+' || current == '-' || current == '*' || current == '/')
                {
                    position++;
                    return new Token
                    {
                        Type = "OPERATOR",
                        Value = current.ToString()
                    };
                }
                
                position++;
                return null;
            }
        }
        
        static void TestParser()
        {
            var lexer = new Lexer("10 + 20 * 30");
            
            Token? token;
            while ((token = lexer.NextToken()) != null)
            {
                // Process token
            }
        }
    }

    /// <summary>
    /// Test: Stack data structure with operations
    /// Expected: Complete stack implementation
    /// </summary>
    class StackTest
    {
        class Stack<T>
        {
            private T[] items = new T[100];
            private int top = -1;
            
            public void Push(T item)
            {
                if (top < 99)
                {
                    items[++top] = item;
                }
            }
            
            public T? Pop()
            {
                if (top >= 0)
                {
                    return items[top--];
                }
                return default(T);
            }
            
            public T? Peek()
            {
                if (top >= 0)
                {
                    return items[top];
                }
                return default(T);
            }
            
            public bool IsEmpty => top < 0;
            public int Count => top + 1;
        }
        
        static void TestStack()
        {
            var stack = new Stack<int>();
            stack.Push(10);
            stack.Push(20);
            stack.Push(30);
            
            int? top = stack.Peek();
            int? popped = stack.Pop();
            bool empty = stack.IsEmpty;
            int count = stack.Count;
        }
    }
}
