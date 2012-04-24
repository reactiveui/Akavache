using System;
using System.Collections.Generic;
using System.Linq;

namespace Akavache
{
    public class LockedDictionary<TKey, TVal> : IDictionary<TKey, TVal>
    {
        Dictionary<TKey, TVal> _inner = new Dictionary<TKey, TVal>();

        public LockedDictionary(IDictionary<TKey, TVal> val = null)
        {
            if (val != null)
            {
                _inner = val.ToDictionary(x => x.Key, x => x.Value);
            }
        }

        public void Add(TKey key, TVal value)
        {
            lock (_inner)
            {
                _inner.Add(key, value);
            }
        }

        public bool ContainsKey(TKey key)
        {
            lock (_inner)
            {
                return _inner.ContainsKey(key);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                lock (_inner)
                {
                    return _inner.Keys.ToArray();
                }
            }
        }

        public bool Remove(TKey key)
        {
            lock (_inner)
            {
                return _inner.Remove(key);
            }
        }

        public bool TryGetValue(TKey key, out TVal value)
        {
            lock (_inner)
            {
                return _inner.TryGetValue(key, out value);
            }
        }

        public ICollection<TVal> Values
        {
            get
            {
                lock (_inner)
                {
                    return _inner.Values.ToArray();
                }
            }
        }

        public TVal this[TKey key]
        {
            get
            {
                lock (_inner)
                {
                    return _inner[key];
                }
            }
            set
            {
                lock (_inner)
                {
                    _inner[key] = value;
                }
            }
        }

        public void Add(KeyValuePair<TKey, TVal> item)
        {
            lock (_inner)
            {
                _inner.Add(item.Key, item.Value);
            }
        }

        public void Clear()
        {
            lock (_inner)
            {
                _inner.Clear();
            }
        }

        public bool Contains(KeyValuePair<TKey, TVal> item)
        {
            lock (_inner)
            {
                var inner = _inner as IDictionary<TKey, TVal>;
                return (inner.Contains(item));
            }
        }

        public void CopyTo(KeyValuePair<TKey, TVal>[] array, int arrayIndex)
        {
            lock (_inner)
            {
                var inner = _inner as IDictionary<TKey, TVal>;
                inner.CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (_inner)
                {
                    return _inner.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TVal> item)
        {
            lock (_inner)
            {
                var inner = _inner as IDictionary<TKey, TVal>;
                return inner.Remove(item);
            }
        }

        public IEnumerator<KeyValuePair<TKey, TVal>> GetEnumerator()
        {
            lock (_inner)
            {
                return _inner.ToList().GetEnumerator();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            lock (_inner)
            {
                return _inner.ToArray().GetEnumerator();
            }
        }
    }

    public class ConcurrentDictionary<TKey, TVal> : LockedDictionary<TKey, TVal>
    {
        public ConcurrentDictionary(IDictionary<TKey, TVal> startingValues = null) : base(startingValues)
        {
        }

        public bool TryRemove(TKey key, out TVal value)
        {
            if (!ContainsKey(key))
            {
                value = default(TVal);
                return false;
            }

            value = this[key];
            Remove(key);
            return true;
        }

        public TVal GetOrAdd(TKey key, Func<TKey, TVal> factory)
        {
            if (ContainsKey(key))
            {
                return this[key];
            }

            var ret = factory(key);
            this[key] = ret;
            return ret;
        }
    }
}