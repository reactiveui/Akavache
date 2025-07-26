// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using ReactiveMarbles.CacheDatabase.Core;

namespace ReactiveMarbles.CacheDatabase.NewtonsoftJson.Bson;

/// <summary>
/// Extension methods for BSON serialization and deserialization.
/// </summary>
public static class BsonObjectExtensions
{
    /// <summary>
    /// Insert an object into the cache, serializing it as BSON.
    /// </summary>
    /// <typeparam name="T">The type of object to insert.</typeparam>
    /// <param name="blobCache">The blob cache to insert the object into.</param>
    /// <param name="key">The key to associate with the object.</param>
    /// <param name="value">The object to insert.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>An observable that signals when the operation is complete.</returns>
    public static IObservable<Unit> InsertObjectAsBson<T>(this IBlobCache blobCache, string key, T value, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return Observable.Start(
            () =>
            {
                var settings = GetBsonSettings();
                using var ms = new MemoryStream();
                using var writer = new BsonDataWriter(ms);
                var serializer = JsonSerializer.Create(settings);
                serializer.Serialize(writer, value);
                return ms.ToArray();
            },
            DefaultScheduler.Instance)
            .SelectMany(bytes => blobCache.Insert(key, bytes, absoluteExpiration));
    }

    /// <summary>
    /// Get an object from the cache, deserializing it from BSON.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="blobCache">The blob cache to retrieve the object from.</param>
    /// <param name="key">The key associated with the object.</param>
    /// <returns>An observable that emits the deserialized object.</returns>
    public static IObservable<T> GetObjectFromBson<T>(this IBlobCache blobCache, string key)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return blobCache.Get(key)
            .SelectMany(data =>
            {
                if (data == null)
                {
                    return Observable.Throw<T>(new ArgumentNullException(nameof(data)));
                }

                return Observable.Start(
                    () =>
                    {
                        var settings = GetBsonSettings();
                        using var ms = new MemoryStream(data);
                        using var reader = new BsonDataReader(ms);
                        var serializer = JsonSerializer.Create(settings);
                        return serializer.Deserialize<T>(reader)!;
                    },
                    DefaultScheduler.Instance);
            });
    }

    /// <summary>
    /// Get an object from the cache, deserializing it from BSON, or return a default value if not found.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="blobCache">The blob cache to retrieve the object from.</param>
    /// <param name="key">The key associated with the object.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <returns>An observable that emits the deserialized object or the default value.</returns>
    public static IObservable<T> GetOrCreateObjectFromBson<T>(this IBlobCache blobCache, string key, Func<T> defaultValue)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (defaultValue is null)
        {
            throw new ArgumentNullException(nameof(defaultValue));
        }

        return blobCache.GetObjectFromBson<T>(key)
            .Catch<T, Exception>(_ =>
            {
                var value = defaultValue();
                return blobCache.InsertObjectAsBson(key, value).Select(_ => value);
            });
    }

    /// <summary>
    /// Retrieve an object from the cache and fetch it from the web if it's not present or expired.
    /// The object will be serialized as BSON.
    /// </summary>
    /// <typeparam name="T">The type of object to retrieve.</typeparam>
    /// <param name="blobCache">The blob cache to use.</param>
    /// <param name="key">The key associated with the object.</param>
    /// <param name="fetchFunc">A function to fetch the object if not in cache.</param>
    /// <param name="absoluteExpiration">An optional expiration date.</param>
    /// <returns>An observable that emits the object from cache or fetcher.</returns>
    public static IObservable<T> GetAndFetchLatestFromBson<T>(this IBlobCache blobCache, string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (fetchFunc is null)
        {
            throw new ArgumentNullException(nameof(fetchFunc));
        }

        var fetch = fetchFunc()
            .SelectMany(value => blobCache.InsertObjectAsBson(key, value, absoluteExpiration).Select(_ => value))
            .Replay(1)
            .RefCount();

        var cached = blobCache.GetObjectFromBson<T>(key).Catch<T, Exception>(_ => Observable.Empty<T>());

        return cached.Concat(fetch).Take(1).Concat(fetch);
    }

    /// <summary>
    /// Convert an object to BSON bytes.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize.</typeparam>
    /// <param name="value">The object to serialize.</param>
    /// <returns>The BSON representation as byte array.</returns>
    public static byte[] ToBson<T>(this T value)
    {
        var settings = GetBsonSettings();
        using var ms = new MemoryStream();
        using var writer = new BsonDataWriter(ms);
        var serializer = JsonSerializer.Create(settings);
        serializer.Serialize(writer, value);
        return ms.ToArray();
    }

    /// <summary>
    /// Convert BSON bytes to an object.
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to.</typeparam>
    /// <param name="data">The BSON data.</param>
    /// <returns>The deserialized object.</returns>
    public static T FromBson<T>(this byte[] data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var settings = GetBsonSettings();
        using var ms = new MemoryStream(data);
        using var reader = new BsonDataReader(ms);
        var serializer = JsonSerializer.Create(settings);
        return serializer.Deserialize<T>(reader)!;
    }

    /// <summary>
    /// Get all objects of a specific type stored with BSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of objects to retrieve.</typeparam>
    /// <param name="blobCache">The blob cache to retrieve objects from.</param>
    /// <returns>An observable that emits all objects of type T.</returns>
    public static IObservable<T> GetAllObjectsFromBson<T>(this IBlobCache blobCache)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.GetAllKeys()
            .SelectMany(key => blobCache.GetObjectFromBson<T>(key)
                .Catch<T, Exception>(_ => Observable.Empty<T>()));
    }

    /// <summary>
    /// Invalidate an object stored with BSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of object to invalidate.</typeparam>
    /// <param name="blobCache">The blob cache to invalidate the object from.</param>
    /// <param name="key">The key of the object to invalidate.</param>
    /// <returns>An observable that signals when the operation is complete.</returns>
    public static IObservable<Unit> InvalidateObjectFromBson<T>(this IBlobCache blobCache, string key)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return blobCache.Invalidate(key);
    }

    /// <summary>
    /// Invalidate all objects of a specific type stored with BSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of objects to invalidate.</typeparam>
    /// <param name="blobCache">The blob cache to invalidate objects from.</param>
    /// <returns>An observable that signals when the operation is complete.</returns>
    public static IObservable<Unit> InvalidateAllObjectsFromBson<T>(this IBlobCache blobCache)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        return blobCache.GetAllKeys()
            .SelectMany(key => blobCache.GetObjectFromBson<T>(key)
                .SelectMany(_ => blobCache.Invalidate(key))
                .Catch<Unit, Exception>(_ => Observable.Return(Unit.Default)))
            .TakeLast(1)
            .DefaultIfEmpty(Unit.Default);
    }

    /// <summary>
    /// Invalidate multiple objects stored with BSON serialization.
    /// </summary>
    /// <typeparam name="T">The type of objects to invalidate.</typeparam>
    /// <param name="blobCache">The blob cache to invalidate objects from.</param>
    /// <param name="keys">The keys of the objects to invalidate.</param>
    /// <returns>An observable that signals when the operation is complete.</returns>
    public static IObservable<Unit> InvalidateObjectsFromBson<T>(this IBlobCache blobCache, IEnumerable<string> keys)
    {
        if (blobCache is null)
        {
            throw new ArgumentNullException(nameof(blobCache));
        }

        if (keys is null)
        {
            throw new ArgumentNullException(nameof(keys));
        }

        return blobCache.Invalidate(keys);
    }

    private static JsonSerializerSettings GetBsonSettings()
    {
        return new JsonSerializerSettings
        {
            ContractResolver = new DateTimeContractResolver(),
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
        };
    }
}
