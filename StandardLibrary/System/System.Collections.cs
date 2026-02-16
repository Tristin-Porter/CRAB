// CRAB Standard Library - System.Collections.cs
// Generic collections framework for CTGC memory model

namespace System.Collections.Generic
{
    // ===== INTERFACES =====
    
    public interface IEnumerable<T>
    {
        IEnumerator<T> GetEnumerator();
    }
    
    public interface IEnumerator<T>
    {
        T Current { get; }
        bool MoveNext();
        void Reset();
    }
    
    public interface ICollection<T> : IEnumerable<T>
    {
        int Count { get; }
        bool IsReadOnly { get; }
        void Add(T item);
        void Clear();
        bool Contains(T item);
        void CopyTo(T[] array, int arrayIndex);
        bool Remove(T item);
    }
    
    public interface IList<T> : ICollection<T>
    {
        T this[int index] { get; set; }
        int IndexOf(T item);
        void Insert(int index, T item);
        void RemoveAt(int index);
    }
    
    public interface IDictionary<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>
    {
        TKey[] Keys { get; }
        TValue[] Values { get; }
        TValue this[TKey key] { get; set; }
        void Add(TKey key, TValue value);
        bool ContainsKey(TKey key);
        bool Remove(TKey key);
        bool TryGetValue(TKey key, out TValue value);
    }
    
    // ===== EQUALITY COMPARER =====
    
    public abstract class EqualityComparer<T>
    {
        private static EqualityComparer<T> defaultComparer;
        
        public static EqualityComparer<T> Default
        {
            get
            {
                if (defaultComparer == null)
                    defaultComparer = new DefaultEqualityComparer<T>();
                return defaultComparer;
            }
        }
        
        public abstract bool Equals(T x, T y);
        public abstract int GetHashCode(T obj);
        
        private class DefaultEqualityComparer<U> : EqualityComparer<U>
        {
            public override bool Equals(U x, U y)
            {
                if (x == null)
                    return y == null;
                if (y == null)
                    return false;
                return x.Equals(y);
            }
            
            public override int GetHashCode(U obj)
            {
                if (obj == null)
                    return 0;
                return obj.GetHashCode();
            }
        }
    }
    
    // ===== LIST<T> =====
    
    public class List<T> : IList<T>
    {
        private T[] items;
        private int count;
        private int capacity;
        
        public List()
        {
            capacity = 4;
            items = new T[capacity];
            count = 0;
        }
        
        public List(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");
            
            this.capacity = capacity;
            items = new T[capacity];
            count = 0;
        }
        
        public int Count
        {
            get { return count; }
        }
        
        public int Capacity
        {
            get { return capacity; }
            set
            {
                if (value < count)
                    throw new ArgumentOutOfRangeException();
                
                if (value != capacity)
                {
                    T[] newItems = new T[value];
                    for (int i = 0; i < count; i++)
                        newItems[i] = items[i];
                    items = newItems;
                    capacity = value;
                }
            }
        }
        
        public bool IsReadOnly
        {
            get { return false; }
        }
        
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();
                return items[index];
            }
            set
            {
                if (index < 0 || index >= count)
                    throw new IndexOutOfRangeException();
                items[index] = value;
            }
        }
        
        public void Add(T item)
        {
            if (count == capacity)
                EnsureCapacity(capacity * 2);
            
            items[count++] = item;
        }
        
        public void Insert(int index, T item)
        {
            if (index < 0 || index > count)
                throw new ArgumentOutOfRangeException();
            
            if (count == capacity)
                EnsureCapacity(capacity * 2);
            
            for (int i = count; i > index; i--)
                items[i] = items[i - 1];
            
            items[index] = item;
            count++;
        }
        
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }
        
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException();
            
            count--;
            for (int i = index; i < count; i++)
                items[i] = items[i + 1];
            
            items[count] = default(T);
        }
        
        public void Clear()
        {
            for (int i = 0; i < count; i++)
                items[i] = default(T);
            count = 0;
        }
        
        public bool Contains(T item)
        {
            return IndexOf(item) >= 0;
        }
        
        public int IndexOf(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < count; i++)
            {
                if (comparer.Equals(items[i], item))
                    return i;
            }
            return -1;
        }
        
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex + count > array.Length)
                throw new ArgumentException();
            
            for (int i = 0; i < count; i++)
                array[arrayIndex + i] = items[i];
        }
        
        public T[] ToArray()
        {
            T[] result = new T[count];
            for (int i = 0; i < count; i++)
                result[i] = items[i];
            return result;
        }
        
        public void Sort()
        {
            // Bubble sort for simplicity
            for (int i = 0; i < count - 1; i++)
            {
                for (int j = 0; j < count - i - 1; j++)
                {
                    if (((IComparable<T>)items[j]).CompareTo(items[j + 1]) > 0)
                    {
                        T temp = items[j];
                        items[j] = items[j + 1];
                        items[j + 1] = temp;
                    }
                }
            }
        }
        
        public void Reverse()
        {
            int left = 0;
            int right = count - 1;
            
            while (left < right)
            {
                T temp = items[left];
                items[left] = items[right];
                items[right] = temp;
                left++;
                right--;
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return new ListEnumerator(this);
        }
        
        private void EnsureCapacity(int min)
        {
            if (capacity < min)
            {
                int newCapacity = capacity == 0 ? 4 : capacity * 2;
                if (newCapacity < min)
                    newCapacity = min;
                Capacity = newCapacity;
            }
        }
        
        private class ListEnumerator : IEnumerator<T>
        {
            private List<T> list;
            private int index;
            private T current;
            
            public ListEnumerator(List<T> list)
            {
                this.list = list;
                index = -1;
                current = default(T);
            }
            
            public T Current
            {
                get { return current; }
            }
            
            public bool MoveNext()
            {
                if (index < list.count - 1)
                {
                    index++;
                    current = list.items[index];
                    return true;
                }
                
                current = default(T);
                return false;
            }
            
            public void Reset()
            {
                index = -1;
                current = default(T);
            }
        }
    }
    
    // ===== DICTIONARY<TKey, TValue> =====
    
    public class Dictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private struct Entry
        {
            public int hashCode;
            public int next;
            public TKey key;
            public TValue value;
        }
        
        private int[] buckets;
        private Entry[] entries;
        private int count;
        private int freeList;
        private int freeCount;
        
        public Dictionary()
        {
            Initialize(4);
        }
        
        public Dictionary(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");
            Initialize(capacity);
        }
        
        public int Count
        {
            get { return count - freeCount; }
        }
        
        public bool IsReadOnly
        {
            get { return false; }
        }
        
        public TKey[] Keys
        {
            get
            {
                TKey[] keys = new TKey[Count];
                int index = 0;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0)
                        keys[index++] = entries[i].key;
                }
                return keys;
            }
        }
        
        public TValue[] Values
        {
            get
            {
                TValue[] values = new TValue[Count];
                int index = 0;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0)
                        values[index++] = entries[i].value;
                }
                return values;
            }
        }
        
        public TValue this[TKey key]
        {
            get
            {
                int i = FindEntry(key);
                if (i >= 0)
                    return entries[i].value;
                throw new KeyNotFoundException();
            }
            set
            {
                Insert(key, value, false);
            }
        }
        
        public void Add(TKey key, TValue value)
        {
            Insert(key, value, true);
        }
        
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }
        
        public bool ContainsKey(TKey key)
        {
            return FindEntry(key) >= 0;
        }
        
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int i = FindEntry(item.Key);
            if (i >= 0)
            {
                EqualityComparer<TValue> comparer = EqualityComparer<TValue>.Default;
                return comparer.Equals(entries[i].value, item.Value);
            }
            return false;
        }
        
        public bool Remove(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            
            EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int bucket = hashCode % buckets.Length;
            int last = -1;
            
            for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                {
                    if (last < 0)
                        buckets[bucket] = entries[i].next;
                    else
                        entries[last].next = entries[i].next;
                    
                    entries[i].hashCode = -1;
                    entries[i].next = freeList;
                    entries[i].key = default(TKey);
                    entries[i].value = default(TValue);
                    freeList = i;
                    freeCount++;
                    return true;
                }
            }
            
            return false;
        }
        
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            int i = FindEntry(key);
            if (i >= 0)
            {
                value = entries[i].value;
                return true;
            }
            value = default(TValue);
            return false;
        }
        
        public void Clear()
        {
            if (count > 0)
            {
                for (int i = 0; i < buckets.Length; i++)
                    buckets[i] = -1;
                
                for (int i = 0; i < count; i++)
                {
                    entries[i] = default(Entry);
                }
                
                freeList = -1;
                count = 0;
                freeCount = 0;
            }
        }
        
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            
            int index = arrayIndex;
            for (int i = 0; i < count; i++)
            {
                if (entries[i].hashCode >= 0)
                    array[index++] = new KeyValuePair<TKey, TValue>(entries[i].key, entries[i].value);
            }
        }
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return new DictionaryEnumerator(this);
        }
        
        private void Initialize(int capacity)
        {
            int size = capacity;
            buckets = new int[size];
            for (int i = 0; i < buckets.Length; i++)
                buckets[i] = -1;
            
            entries = new Entry[size];
            freeList = -1;
        }
        
        private void Insert(TKey key, TValue value, bool add)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            
            EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            int targetBucket = hashCode % buckets.Length;
            
            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                {
                    if (add)
                        throw new ArgumentException("Key already exists");
                    entries[i].value = value;
                    return;
                }
            }
            
            int index;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % buckets.Length;
                }
                index = count;
                count++;
            }
            
            entries[index].hashCode = hashCode;
            entries[index].next = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
        }
        
        private void Resize()
        {
            int newSize = count * 2;
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++)
                newBuckets[i] = -1;
            
            Entry[] newEntries = new Entry[newSize];
            for (int i = 0; i < count; i++)
                newEntries[i] = entries[i];
            
            for (int i = 0; i < count; i++)
            {
                if (newEntries[i].hashCode >= 0)
                {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            
            buckets = newBuckets;
            entries = newEntries;
        }
        
        private int FindEntry(TKey key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
            
            EqualityComparer<TKey> comparer = EqualityComparer<TKey>.Default;
            int hashCode = key.GetHashCode() & 0x7FFFFFFF;
            for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                    return i;
            }
            
            return -1;
        }
        
        private class DictionaryEnumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            private Dictionary<TKey, TValue> dictionary;
            private int index;
            private KeyValuePair<TKey, TValue> current;
            
            public DictionaryEnumerator(Dictionary<TKey, TValue> dictionary)
            {
                this.dictionary = dictionary;
                index = -1;
                current = default(KeyValuePair<TKey, TValue>);
            }
            
            public KeyValuePair<TKey, TValue> Current
            {
                get { return current; }
            }
            
            public bool MoveNext()
            {
                while (++index < dictionary.count)
                {
                    if (dictionary.entries[index].hashCode >= 0)
                    {
                        current = new KeyValuePair<TKey, TValue>(
                            dictionary.entries[index].key,
                            dictionary.entries[index].value);
                        return true;
                    }
                }
                
                current = default(KeyValuePair<TKey, TValue>);
                return false;
            }
            
            public void Reset()
            {
                index = -1;
                current = default(KeyValuePair<TKey, TValue>);
            }
        }
    }
    
    // ===== QUEUE<T> =====
    
    public class Queue<T> : ICollection<T>
    {
        private T[] array;
        private int head;
        private int tail;
        private int count;
        
        public Queue()
        {
            array = new T[4];
        }
        
        public Queue(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");
            array = new T[capacity];
        }
        
        public int Count
        {
            get { return count; }
        }
        
        public bool IsReadOnly
        {
            get { return false; }
        }
        
        public void Enqueue(T item)
        {
            if (count == array.Length)
            {
                int newCapacity = array.Length * 2;
                T[] newArray = new T[newCapacity];
                
                if (head < tail)
                {
                    for (int i = 0; i < count; i++)
                        newArray[i] = array[head + i];
                }
                else
                {
                    int firstPart = array.Length - head;
                    for (int i = 0; i < firstPart; i++)
                        newArray[i] = array[head + i];
                    for (int i = 0; i < tail; i++)
                        newArray[firstPart + i] = array[i];
                }
                
                array = newArray;
                head = 0;
                tail = count;
            }
            
            array[tail] = item;
            tail = (tail + 1) % array.Length;
            count++;
        }
        
        public T Dequeue()
        {
            if (count == 0)
                throw new InvalidOperationException("Queue is empty");
            
            T item = array[head];
            array[head] = default(T);
            head = (head + 1) % array.Length;
            count--;
            return item;
        }
        
        public T Peek()
        {
            if (count == 0)
                throw new InvalidOperationException("Queue is empty");
            return array[head];
        }
        
        public void Add(T item)
        {
            Enqueue(item);
        }
        
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }
        
        public void Clear()
        {
            if (head < tail)
            {
                for (int i = head; i < tail; i++)
                    array[i] = default(T);
            }
            else
            {
                for (int i = head; i < array.Length; i++)
                    array[i] = default(T);
                for (int i = 0; i < tail; i++)
                    array[i] = default(T);
            }
            
            head = 0;
            tail = 0;
            count = 0;
        }
        
        public bool Contains(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            int index = head;
            for (int i = 0; i < count; i++)
            {
                if (comparer.Equals(array[index], item))
                    return true;
                index = (index + 1) % array.Length;
            }
            return false;
        }
        
        public void CopyTo(T[] targetArray, int arrayIndex)
        {
            if (targetArray == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex + count > targetArray.Length)
                throw new ArgumentException();
            
            int index = head;
            for (int i = 0; i < count; i++)
            {
                targetArray[arrayIndex + i] = array[index];
                index = (index + 1) % array.Length;
            }
        }
        
        public T[] ToArray()
        {
            T[] result = new T[count];
            CopyTo(result, 0);
            return result;
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return new QueueEnumerator(this);
        }
        
        private class QueueEnumerator : IEnumerator<T>
        {
            private Queue<T> queue;
            private int index;
            private T current;
            
            public QueueEnumerator(Queue<T> queue)
            {
                this.queue = queue;
                index = -1;
                current = default(T);
            }
            
            public T Current
            {
                get { return current; }
            }
            
            public bool MoveNext()
            {
                if (index < queue.count - 1)
                {
                    index++;
                    int arrayIndex = (queue.head + index) % queue.array.Length;
                    current = queue.array[arrayIndex];
                    return true;
                }
                
                current = default(T);
                return false;
            }
            
            public void Reset()
            {
                index = -1;
                current = default(T);
            }
        }
    }
    
    // ===== STACK<T> =====
    
    public class Stack<T> : ICollection<T>
    {
        private T[] array;
        private int count;
        
        public Stack()
        {
            array = new T[4];
            count = 0;
        }
        
        public Stack(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");
            array = new T[capacity];
            count = 0;
        }
        
        public int Count
        {
            get { return count; }
        }
        
        public bool IsReadOnly
        {
            get { return false; }
        }
        
        public void Push(T item)
        {
            if (count == array.Length)
            {
                T[] newArray = new T[array.Length * 2];
                for (int i = 0; i < count; i++)
                    newArray[i] = array[i];
                array = newArray;
            }
            
            array[count++] = item;
        }
        
        public T Pop()
        {
            if (count == 0)
                throw new InvalidOperationException("Stack is empty");
            
            T item = array[--count];
            array[count] = default(T);
            return item;
        }
        
        public T Peek()
        {
            if (count == 0)
                throw new InvalidOperationException("Stack is empty");
            return array[count - 1];
        }
        
        public void Add(T item)
        {
            Push(item);
        }
        
        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }
        
        public void Clear()
        {
            for (int i = 0; i < count; i++)
                array[i] = default(T);
            count = 0;
        }
        
        public bool Contains(T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < count; i++)
            {
                if (comparer.Equals(array[i], item))
                    return true;
            }
            return false;
        }
        
        public void CopyTo(T[] targetArray, int arrayIndex)
        {
            if (targetArray == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex + count > targetArray.Length)
                throw new ArgumentException();
            
            for (int i = 0; i < count; i++)
                targetArray[arrayIndex + i] = array[count - i - 1];
        }
        
        public T[] ToArray()
        {
            T[] result = new T[count];
            for (int i = 0; i < count; i++)
                result[i] = array[count - i - 1];
            return result;
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return new StackEnumerator(this);
        }
        
        private class StackEnumerator : IEnumerator<T>
        {
            private Stack<T> stack;
            private int index;
            private T current;
            
            public StackEnumerator(Stack<T> stack)
            {
                this.stack = stack;
                index = stack.count;
                current = default(T);
            }
            
            public T Current
            {
                get { return current; }
            }
            
            public bool MoveNext()
            {
                if (index > 0)
                {
                    index--;
                    current = stack.array[index];
                    return true;
                }
                
                current = default(T);
                return false;
            }
            
            public void Reset()
            {
                index = stack.count;
                current = default(T);
            }
        }
    }
    
    // ===== HASHSET<T> =====
    
    public class HashSet<T> : ICollection<T>
    {
        private struct Slot
        {
            public int hashCode;
            public int next;
            public T value;
        }
        
        private int[] buckets;
        private Slot[] slots;
        private int count;
        private int freeList;
        
        public HashSet()
        {
            Initialize(4);
        }
        
        public HashSet(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");
            Initialize(capacity);
        }
        
        public int Count
        {
            get { return count; }
        }
        
        public bool IsReadOnly
        {
            get { return false; }
        }
        
        public bool Add(T item)
        {
            return AddIfNotPresent(item);
        }
        
        void ICollection<T>.Add(T item)
        {
            Add(item);
        }
        
        public bool Remove(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            int hashCode = item.GetHashCode() & 0x7FFFFFFF;
            int bucket = hashCode % buckets.Length;
            int last = -1;
            
            for (int i = buckets[bucket] - 1; i >= 0; last = i, i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, item))
                {
                    if (last < 0)
                        buckets[bucket] = slots[i].next + 1;
                    else
                        slots[last].next = slots[i].next;
                    
                    slots[i].hashCode = -1;
                    slots[i].value = default(T);
                    slots[i].next = freeList;
                    freeList = i;
                    return true;
                }
            }
            
            return false;
        }
        
        public bool Contains(T item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            int hashCode = item.GetHashCode() & 0x7FFFFFFF;
            for (int i = buckets[hashCode % buckets.Length] - 1; i >= 0; i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, item))
                    return true;
            }
            
            return false;
        }
        
        public void Clear()
        {
            if (count > 0)
            {
                for (int i = 0; i < buckets.Length; i++)
                    buckets[i] = 0;
                
                for (int i = 0; i < count; i++)
                    slots[i] = default(Slot);
                
                freeList = -1;
                count = 0;
            }
        }
        
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex + count > array.Length)
                throw new ArgumentException();
            
            int index = arrayIndex;
            for (int i = 0; i < count; i++)
            {
                if (slots[i].hashCode >= 0)
                    array[index++] = slots[i].value;
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return new HashSetEnumerator(this);
        }
        
        private void Initialize(int capacity)
        {
            int size = capacity;
            buckets = new int[size];
            slots = new Slot[size];
            freeList = -1;
        }
        
        private bool AddIfNotPresent(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            int hashCode = value.GetHashCode() & 0x7FFFFFFF;
            int bucket = hashCode % buckets.Length;
            
            for (int i = buckets[bucket] - 1; i >= 0; i = slots[i].next)
            {
                if (slots[i].hashCode == hashCode && comparer.Equals(slots[i].value, value))
                    return false;
            }
            
            int index;
            if (freeList >= 0)
            {
                index = freeList;
                freeList = slots[index].next;
            }
            else
            {
                if (count == slots.Length)
                {
                    Resize();
                    bucket = hashCode % buckets.Length;
                }
                index = count;
                count++;
            }
            
            slots[index].hashCode = hashCode;
            slots[index].value = value;
            slots[index].next = buckets[bucket] - 1;
            buckets[bucket] = index + 1;
            
            return true;
        }
        
        private void Resize()
        {
            int newSize = count * 2;
            int[] newBuckets = new int[newSize];
            Slot[] newSlots = new Slot[newSize];
            
            for (int i = 0; i < count; i++)
                newSlots[i] = slots[i];
            
            for (int i = 0; i < count; i++)
            {
                int bucket = newSlots[i].hashCode % newSize;
                newSlots[i].next = newBuckets[bucket] - 1;
                newBuckets[bucket] = i + 1;
            }
            
            buckets = newBuckets;
            slots = newSlots;
        }
        
        private class HashSetEnumerator : IEnumerator<T>
        {
            private HashSet<T> set;
            private int index;
            private T current;
            
            public HashSetEnumerator(HashSet<T> set)
            {
                this.set = set;
                index = -1;
                current = default(T);
            }
            
            public T Current
            {
                get { return current; }
            }
            
            public bool MoveNext()
            {
                while (++index < set.count)
                {
                    if (set.slots[index].hashCode >= 0)
                    {
                        current = set.slots[index].value;
                        return true;
                    }
                }
                
                current = default(T);
                return false;
            }
            
            public void Reset()
            {
                index = -1;
                current = default(T);
            }
        }
    }
    
    // ===== KEYVALUEPAIR<TKey, TValue> =====
    
    public struct KeyValuePair<TKey, TValue>
    {
        private TKey key;
        private TValue value;
        
        public KeyValuePair(TKey key, TValue value)
        {
            this.key = key;
            this.value = value;
        }
        
        public TKey Key
        {
            get { return key; }
        }
        
        public TValue Value
        {
            get { return value; }
        }
        
        public override string ToString()
        {
            return "[" + key.ToString() + ", " + value.ToString() + "]";
        }
    }
    
    // ===== LINKED LIST<T> =====
    
    public class LinkedList<T> : ICollection<T>
    {
        private LinkedListNode<T> head;
        private LinkedListNode<T> tail;
        private int count;
        
        public LinkedList()
        {
            head = null;
            tail = null;
            count = 0;
        }
        
        public int Count
        {
            get { return count; }
        }
        
        public bool IsReadOnly
        {
            get { return false; }
        }
        
        public LinkedListNode<T> First
        {
            get { return head; }
        }
        
        public LinkedListNode<T> Last
        {
            get { return tail; }
        }
        
        public void AddFirst(T value)
        {
            LinkedListNode<T> node = new LinkedListNode<T>(this, value);
            if (head == null)
            {
                head = node;
                tail = node;
            }
            else
            {
                node.next = head;
                head.previous = node;
                head = node;
            }
            count++;
        }
        
        public void AddLast(T value)
        {
            LinkedListNode<T> node = new LinkedListNode<T>(this, value);
            if (tail == null)
            {
                head = node;
                tail = node;
            }
            else
            {
                node.previous = tail;
                tail.next = node;
                tail = node;
            }
            count++;
        }
        
        public void Add(T item)
        {
            AddLast(item);
        }
        
        public bool Remove(T item)
        {
            LinkedListNode<T> node = Find(item);
            if (node != null)
            {
                RemoveNode(node);
                return true;
            }
            return false;
        }
        
        public void RemoveFirst()
        {
            if (head == null)
                throw new InvalidOperationException("List is empty");
            RemoveNode(head);
        }
        
        public void RemoveLast()
        {
            if (tail == null)
                throw new InvalidOperationException("List is empty");
            RemoveNode(tail);
        }
        
        public LinkedListNode<T> Find(T value)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            LinkedListNode<T> node = head;
            while (node != null)
            {
                if (comparer.Equals(node.Value, value))
                    return node;
                node = node.next;
            }
            return null;
        }
        
        public bool Contains(T item)
        {
            return Find(item) != null;
        }
        
        public void Clear()
        {
            LinkedListNode<T> current = head;
            while (current != null)
            {
                LinkedListNode<T> temp = current;
                current = current.next;
                temp.Invalidate();
            }
            
            head = null;
            tail = null;
            count = 0;
        }
        
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (arrayIndex < 0 || arrayIndex + count > array.Length)
                throw new ArgumentException();
            
            LinkedListNode<T> node = head;
            while (node != null)
            {
                array[arrayIndex++] = node.Value;
                node = node.next;
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            return new LinkedListEnumerator(this);
        }
        
        private void RemoveNode(LinkedListNode<T> node)
        {
            if (node.previous != null)
                node.previous.next = node.next;
            else
                head = node.next;
            
            if (node.next != null)
                node.next.previous = node.previous;
            else
                tail = node.previous;
            
            node.Invalidate();
            count--;
        }
        
        private class LinkedListEnumerator : IEnumerator<T>
        {
            private LinkedList<T> list;
            private LinkedListNode<T> node;
            private T current;
            
            public LinkedListEnumerator(LinkedList<T> list)
            {
                this.list = list;
                node = null;
                current = default(T);
            }
            
            public T Current
            {
                get { return current; }
            }
            
            public bool MoveNext()
            {
                if (node == null)
                    node = list.head;
                else
                    node = node.next;
                
                if (node != null)
                {
                    current = node.Value;
                    return true;
                }
                
                current = default(T);
                return false;
            }
            
            public void Reset()
            {
                node = null;
                current = default(T);
            }
        }
    }
    
    public class LinkedListNode<T>
    {
        internal LinkedList<T> list;
        internal LinkedListNode<T> next;
        internal LinkedListNode<T> previous;
        private T value;
        
        internal LinkedListNode(LinkedList<T> list, T value)
        {
            this.list = list;
            this.value = value;
            this.next = null;
            this.previous = null;
        }
        
        public LinkedList<T> List
        {
            get { return list; }
        }
        
        public LinkedListNode<T> Next
        {
            get { return next; }
        }
        
        public LinkedListNode<T> Previous
        {
            get { return previous; }
        }
        
        public T Value
        {
            get { return value; }
            set { this.value = value; }
        }
        
        internal void Invalidate()
        {
            list = null;
            next = null;
            previous = null;
        }
    }
    
    // ===== EXCEPTIONS =====
    
    public class KeyNotFoundException : Exception
    {
        public KeyNotFoundException() : base("The given key was not present in the dictionary") { }
        public KeyNotFoundException(string message) : base(message) { }
    }
}
