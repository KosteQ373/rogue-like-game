using System;
using System.Collections.Generic;

namespace rogue_like;

public class ObjectPool<T> where T : IPoolable, new()
{
    private readonly List<T> _items;
    private readonly Func<T> _factory;

    public ObjectPool(int initialCapacity, Func<T>? factory = null)
    {
        _factory = factory ?? (() => new T());
        _items = new List<T>(initialCapacity);
        for (int i = 0; i < initialCapacity; i++)
        {
            var item = _factory();
            item.IsActive = false;
            _items.Add(item);
        }
    }

    public T Get()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (!_items[i].IsActive)
            {
                _items[i].Reset();
                _items[i].IsActive = true;
                return _items[i];
            }
        }

        var newItem = _factory();
        newItem.Reset();
        newItem.IsActive = true;
        _items.Add(newItem);
        return newItem;
    }

    public void Return(T item)
    {
        item.Reset();
        item.IsActive = false;
    }

    public List<T> Items => _items;
}
