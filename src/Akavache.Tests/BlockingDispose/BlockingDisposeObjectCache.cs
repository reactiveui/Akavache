// Copyright (c) 2024 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Tests;

internal class BlockingDisposeObjectCache(IObjectBlobCache cache) : BlockingDisposeCache(cache), IObjectBlobCache
{
    public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null) => ((IObjectBlobCache)Inner).InsertObject(key, value, absoluteExpiration);

    public IObservable<T> GetObject<T>(string key) => ((IObjectBlobCache)Inner).GetObject<T>(key);

    public IObservable<IEnumerable<T>> GetAllObjects<T>() => ((IObjectBlobCache)Inner).GetAllObjects<T>();

    public IObservable<Unit> InvalidateObject<T>(string key) => ((IObjectBlobCache)Inner).InvalidateObject<T>(key);

    public IObservable<Unit> InvalidateAllObjects<T>() => ((IObjectBlobCache)Inner).InvalidateAllObjects<T>();

    public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key) => ((IObjectBlobCache)Inner).GetObjectCreatedAt<T>(key);
}