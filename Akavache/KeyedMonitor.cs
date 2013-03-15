using System;
using System.Collections.Concurrent;
using System.Reactive;

namespace Akavache
{
    public class KeyedMonitor
    {
        readonly ConcurrentDictionary<string, object> keyedGates = new ConcurrentDictionary<string, object>();

        public IObservable<T> Try<T>(string key, Func<T> func)
        {
            return Utility.Try(() =>
            {
                var ticket = new object();
                while (true)
                {
                    var admission = keyedGates.GetOrAdd(key, ticket);
                    lock (admission)
                    {
                        if (admission != ticket)
                        {
                            continue;
                        }
                        try
                        {
                            return func();
                        }
                        finally
                        {
                            keyedGates.Remove(key);
                        }
                    }
                }
            });
        }

        public IObservable<Unit> Try(string key, Action action)
        {
            return Try(key, () =>
            {
                action();
                return Unit.Default;
            });
        }
    }

    static class ConcurrentDictionaryExtensions
    {
        public static void Remove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key)
        {
            TValue removed;
            dict.TryRemove(key, out removed /* ignored */);
        }
    }
}
