using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace WootMouseRemap.Utilities
{
    /// <summary>
    /// Thread-safe collection wrapper with additional safety features
    /// </summary>
    public class ThreadSafeCollection<T> : ICollection<T>, IDisposable
    {
        private readonly ConcurrentBag<T> _items = new();
        private readonly ReaderWriterLockSlim _lock = new();
        private volatile bool _disposed = false;

        public int Count => _items.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            ThrowIfDisposed();
            _items.Add(item);
        }

        public bool Remove(T item)
        {
            ThrowIfDisposed();
            
            _lock.EnterWriteLock();
            try
            {
                var items = _items.ToArray();
                var newItems = items.Where(x => !EqualityComparer<T>.Default.Equals(x, item)).ToArray();
                
                if (newItems.Length == items.Length)
                    return false;

                // Rebuild collection
                _items.Clear();
                foreach (var newItem in newItems)
                {
                    _items.Add(newItem);
                }
                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Clear()
        {
            ThrowIfDisposed();
            
            _lock.EnterWriteLock();
            try
            {
                while (_items.TryTake(out _)) { }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public bool Contains(T item)
        {
            ThrowIfDisposed();
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowIfDisposed();
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            ThrowIfDisposed();
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T[] ToArray()
        {
            ThrowIfDisposed();
            return _items.ToArray();
        }

        public List<T> ToList()
        {
            ThrowIfDisposed();
            return _items.ToList();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeCollection<T>));
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _lock?.Dispose();
        }
    }

    /// <summary>
    /// Thread-safe dictionary with additional safety features
    /// </summary>
    public class ThreadSafeDictionary<TKey, TValue> : IDisposable where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dictionary = new();
        private volatile bool _disposed = false;

        public TValue this[TKey key]
        {
            get
            {
                ThrowIfDisposed();
                return _dictionary[key];
            }
            set
            {
                ThrowIfDisposed();
                _dictionary[key] = value;
            }
        }

        public int Count => _dictionary.Count;
        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;

        public bool TryAdd(TKey key, TValue value)
        {
            ThrowIfDisposed();
            return _dictionary.TryAdd(key, value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            ThrowIfDisposed();
            return _dictionary.TryGetValue(key, out value!);
        }

        public bool TryRemove(TKey key, out TValue value)
        {
            ThrowIfDisposed();
            return _dictionary.TryRemove(key, out value!);
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
        {
            ThrowIfDisposed();
            return _dictionary.GetOrAdd(key, valueFactory);
        }

        public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            ThrowIfDisposed();
            return _dictionary.AddOrUpdate(key, addValue, updateValueFactory);
        }

        public bool ContainsKey(TKey key)
        {
            ThrowIfDisposed();
            return _dictionary.ContainsKey(key);
        }

        public void Clear()
        {
            ThrowIfDisposed();
            _dictionary.Clear();
        }

        public KeyValuePair<TKey, TValue>[] ToArray()
        {
            ThrowIfDisposed();
            return _dictionary.ToArray();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeDictionary<TKey, TValue>));
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }

    /// <summary>
    /// Thread-safe event aggregator for decoupled communication
    /// </summary>
    public class ThreadSafeEventAggregator : IDisposable
    {
        private readonly ConcurrentDictionary<Type, ConcurrentBag<object>> _handlers = new();
        private volatile bool _disposed = false;

        public void Subscribe<T>(Action<T> handler)
        {
            ThrowIfDisposed();
            var handlers = _handlers.GetOrAdd(typeof(T), _ => new ConcurrentBag<object>());
            handlers.Add(handler);
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            ThrowIfDisposed();
            
            if (_handlers.TryGetValue(typeof(T), out var handlers))
            {
                // Note: ConcurrentBag doesn't support removal, so we'd need a different approach
                // For now, we'll mark handlers as removed using a wrapper
                var wrapper = new RemovedHandlerWrapper<T>(handler);
                handlers.Add(wrapper);
            }
        }

        public void Publish<T>(T eventData)
        {
            ThrowIfDisposed();
            
            if (!_handlers.TryGetValue(typeof(T), out var handlers))
                return;

            var activeHandlers = handlers.OfType<Action<T>>()
                .Where(h => !(h is RemovedHandlerWrapper<T>))
                .ToArray();

            foreach (var handler in activeHandlers)
            {
                try
                {
                    handler(eventData);
                }
                catch (Exception ex)
                {
                    // Log error but continue with other handlers
                    WootMouseRemap.Logger.Error($"Error in event handler for {typeof(T).Name}", ex);
                }
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ThreadSafeEventAggregator));
        }

        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _handlers.Clear();
        }

        private class RemovedHandlerWrapper<T>
        {
            public Action<T> Handler { get; }
            
            public RemovedHandlerWrapper(Action<T> handler)
            {
                Handler = handler;
            }
        }
    }
}