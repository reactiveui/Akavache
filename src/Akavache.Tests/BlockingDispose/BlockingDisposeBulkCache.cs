// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;

namespace Akavache.Tests
{
    internal class BlockingDisposeBulkCache : BlockingDisposeCache, IObjectBulkBlobCache
    {
        public BlockingDisposeBulkCache(IBlobCache inner)
            : base(inner)
        {
        }

        public IObservable<Unit> Insert(IDictionary<string, byte[]> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            return Inner.Insert(keyValuePairs, absoluteExpiration);
        }

        public IObservable<IDictionary<string, byte[]>> Get(IEnumerable<string> keys)
        {
            return Inner.Get(keys);
        }

        public IObservable<IDictionary<string, DateTimeOffset?>> GetCreatedAt(IEnumerable<string> keys)
        {
            return Inner.GetCreatedAt(keys);
        }

        public IObservable<Unit> Invalidate(IEnumerable<string> keys)
        {
            return Inner.Invalidate(keys);
        }

        public IObservable<Unit> InsertObjects<T>(IDictionary<string, T> keyValuePairs, DateTimeOffset? absoluteExpiration = null)
        {
            return Inner.InsertObjects(keyValuePairs, absoluteExpiration);
        }

        public IObservable<IDictionary<string, T>> GetObjects<T>(IEnumerable<string> keys)
        {
            return Inner.GetObjects<T>(keys);
        }

        public IObservable<Unit> InvalidateObjects<T>(IEnumerable<string> keys)
        {
            return Inner.InvalidateObjects<T>(keys);
        }

        public IObservable<Unit> InsertObject<T>(string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            return Inner.InsertObject<T>(key, value, absoluteExpiration);
        }

        public IObservable<T> GetObject<T>(string key)
        {
            return Inner.GetObject<T>(key);
        }

        public IObservable<DateTimeOffset?> GetObjectCreatedAt<T>(string key)
        {
            return Inner.GetObjectCreatedAt<T>(key);
        }

        public IObservable<IEnumerable<T>> GetAllObjects<T>()
        {
            return Inner.GetAllObjects<T>();
        }

        public IObservable<Unit> InvalidateObject<T>(string key)
        {
            return Inner.InvalidateObject<T>(key);
        }

        public IObservable<Unit> InvalidateAllObjects<T>()
        {
            return Inner.InvalidateAllObjects<T>();
        }
    }
}
