// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Akavache.Tests
{
    internal class BlockingDisposeCache : IBlobCache
    {
        public BlockingDisposeCache(IBlobCache cache)
        {
            BlobCache.EnsureInitialized();
            Inner = cache;
        }

        public IObservable<Unit> Shutdown => Inner.Shutdown;

        public IScheduler Scheduler => Inner.Scheduler;

        public DateTimeKind? ForcedDateTimeKind
        {
            get => Inner.ForcedDateTimeKind;
            set => Inner.ForcedDateTimeKind = value;
        }

        protected IBlobCache Inner { get; }

        public virtual void Dispose()
        {
            Inner.Dispose();
            Inner.Shutdown.Wait();
        }

        public IObservable<Unit> Insert(string key, byte[] data, DateTimeOffset? absoluteExpiration = null)
        {
            return Inner.Insert(key, data, absoluteExpiration);
        }

        public IObservable<byte[]> Get(string key)
        {
            return Inner.Get(key);
        }

        public IObservable<IEnumerable<string>> GetAllKeys()
        {
            return Inner.GetAllKeys();
        }

        public IObservable<DateTimeOffset?> GetCreatedAt(string key)
        {
            return Inner.GetCreatedAt(key);
        }

        public IObservable<Unit> Flush()
        {
            return Inner.Flush();
        }

        public IObservable<Unit> Invalidate(string key)
        {
            return Inner.Invalidate(key);
        }

        public IObservable<Unit> InvalidateAll()
        {
            return Inner.InvalidateAll();
        }

        public IObservable<Unit> Vacuum()
        {
            return Inner.Vacuum();
        }
    }
}
