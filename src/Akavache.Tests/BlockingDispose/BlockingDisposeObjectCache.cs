// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;

namespace Akavache.Tests
{
    internal class BlockingDisposeObjectCache : BlockingDisposeCache, IObjectBlobCache
    {
        public BlockingDisposeObjectCache(IObjectBlobCache cache)
            : base(cache)
        {
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            return ((IObjectBlobCache)Inner).InsertObject(key, value, absoluteExpiration);
        }

        public IObservable<T> GetObject<T>(string key)
        {
            return ((IObjectBlobCache)Inner).GetObject<T>(key);
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            return ((IObjectBlobCache)Inner).GetAllObjects<T>();
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            return ((IObjectBlobCache)Inner).InvalidateObject<T>(key);
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            return ((IObjectBlobCache)Inner).InvalidateAllObjects<T>();
        }

        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return ((IObjectBlobCache)Inner).GetObjectCreatedAt<T>(key);
        }
    }
}
