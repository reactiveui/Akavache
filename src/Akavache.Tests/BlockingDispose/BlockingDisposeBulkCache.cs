// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

internal class BlockingDisposeBulkCache(IBlobCache inner) : BlockingDisposeCache(inner), IObjectBulkBlobCache
{
    public IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null) => Inner.Insert(keyValuePairs, absoluteExpiration);

    public IObservable<IDictionary<string, byte[]>> Get(IEnumerable<string> keys) => Inner.Get(keys);

    public IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys) => Inner.GetCreatedAt(keys);

    public IObservable<Unit> Invalidate(IEnumerable<string> keys) => Inner.Invalidate(keys);

    public IObservable<Unit> InsertObjects<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null) => Inner.InsertObjects(keyValuePairs, absoluteExpiration);

    public IObservable<IDictionary<string, T>> GetObjects<T>(IEnumerable<string> keys) => Inner.GetObjects<T>(keys);

    public IObservable<Unit> InvalidateObjects<T>(IEnumerable<string> keys) => Inner.InvalidateObjects<T>(keys);

    public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null) => Inner.InsertObject<T>(key, value, absoluteExpiration);

    public IObservable<T> GetObject<T>(string key) => Inner.GetObject<T>(key);

    public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key) => Inner.GetObjectCreatedAt<T>(key);

    public IObservable<IEnumerable<T>> GetAllObjects<T>() => Inner.GetAllObjects<T>();

    public IObservable<Unit> InvalidateObject<T>(string key) => Inner.InvalidateObject<T>(key);

    public IObservable<Unit> InvalidateAllObjects<T>() => Inner.InvalidateAllObjects<T>();
}