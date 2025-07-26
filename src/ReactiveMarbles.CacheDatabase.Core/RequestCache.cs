// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Concurrent;

namespace ReactiveMarbles.CacheDatabase.Core;

/// <summary>
/// A cache for deduplicating concurrent requests for the same key.
/// </summary>
public static class RequestCache
{
    private static readonly ConcurrentDictionary<string, IObservable<object>> _inflightRequests = new();

    /// <summary>
    /// Gets or creates a request observable for the specified key and fetch function.
    /// This ensures that multiple concurrent requests for the same key will share the same fetch operation.
    /// </summary>
    /// <typeparam name="T">The type of the object being fetched.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="fetchFunc">The function to fetch the value if not already in flight.</param>
    /// <returns>An observable that represents the shared fetch operation.</returns>
    public static IObservable<T> GetOrCreateRequest<T>(string key, Func<IObservable<T>> fetchFunc)
    {
        if (fetchFunc is null)
        {
            throw new ArgumentNullException(nameof(fetchFunc));
        }

        var requestKey = $"{typeof(T).FullName}:{key}";

        return (IObservable<T>)_inflightRequests.GetOrAdd(requestKey, _ =>
        {
            var observable = fetchFunc().Select(x => (object)x!)
                .Do(
                    onNext: _ => { },
                    onError: _ => RemoveRequestInternal(requestKey),
                    onCompleted: () => RemoveRequestInternal(requestKey))
                .Replay(1)
                .RefCount();

            return observable;
        }).Select(x => (T)x);
    }

    /// <summary>
    /// Clears all in-flight requests. This is primarily for testing purposes.
    /// </summary>
    public static void Clear() => _inflightRequests.Clear();

    /// <summary>
    /// Removes a specific request from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="type">The type of object.</param>
    public static void RemoveRequest(string key, Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var requestKey = $"{type.FullName}:{key}";
        RemoveRequestInternal(requestKey);
    }

    private static void RemoveRequestInternal(string requestKey) => _inflightRequests.TryRemove(requestKey, out var _);
}
