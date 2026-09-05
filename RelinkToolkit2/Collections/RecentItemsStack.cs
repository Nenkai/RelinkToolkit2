using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RelinkToolkit2.Collections;

public class RecentItemsStack<T> : IEnumerable<T> where T : notnull
{
    private readonly LinkedList<T> _list = new();
    private readonly Dictionary<T, LinkedListNode<T>> _map;
    private readonly int _maxSize;

    public RecentItemsStack(int maxSize, IEqualityComparer<T>? comparer = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSize);

        _maxSize = maxSize;
        _map = new Dictionary<T, LinkedListNode<T>>(comparer ?? EqualityComparer<T>.Default);
    }

    public int Count => _list.Count;

    public void Push(T item)
    {
        if (_map.TryGetValue(item, out var existingNode))
        {
            _list.Remove(existingNode);
            _list.AddFirst(existingNode);
            return;
        }

        var node = _list.AddFirst(item);
        _map[item] = node;

        if (_list.Count > _maxSize)
        {
            var last = _list.Last!;
            _list.RemoveLast();
            _map.Remove(last.Value);
        }
    }

    public bool Remove(T item)
    {
        if (_map.TryGetValue(item, out var existingNode))
        {
            _list.Remove(existingNode);
            return true;
        }

        return false;
    }

    public T Pop()
    {
        if (_list.Count == 0) 
            throw new InvalidOperationException("Stack is empty.");

        var value = _list.First!.Value;
        _list.RemoveFirst();
        _map.Remove(value);
        return value;
    }

    public T Peek()
    {
        if (_list.Count == 0) throw new InvalidOperationException("Stack is empty.");
        return _list.First!.Value;
    }

    public bool Contains(T item) => _map.ContainsKey(item);

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
