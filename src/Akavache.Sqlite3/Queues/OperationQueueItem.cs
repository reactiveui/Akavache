// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Subjects;

namespace Akavache.Sqlite3
{
    internal class OperationQueueItem
    {
        public OperationType OperationType { get; set; }

        public IEnumerable Parameters { get; set; }

        public object Completion { get; set; }

        public IEnumerable<CacheElement> ParametersAsElements => (IEnumerable<CacheElement>)Parameters;

        public IEnumerable<string> ParametersAsKeys => (IEnumerable<string>)Parameters;

        public AsyncSubject<Unit> CompletionAsUnit => (AsyncSubject<Unit>)Completion;

        public AsyncSubject<IEnumerable<CacheElement>> CompletionAsElements => (AsyncSubject<IEnumerable<CacheElement>>)Completion;

        public AsyncSubject<IEnumerable<string>> CompletionAsKeys => (AsyncSubject<IEnumerable<string>>)Completion;

        public static OperationQueueItem CreateInsert(OperationType opType, IEnumerable<CacheElement> toInsert, AsyncSubject<Unit> completion = null)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toInsert, Completion = completion ?? new AsyncSubject<Unit>() };
        }

        public static OperationQueueItem CreateInvalidate(OperationType opType, IEnumerable<string> toInvalidate, AsyncSubject<Unit> completion = null)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toInvalidate, Completion = completion ?? new AsyncSubject<Unit>() };
        }

        public static OperationQueueItem CreateSelect(OperationType opType, IEnumerable<string> toSelect, AsyncSubject<IEnumerable<CacheElement>> completion = null)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = toSelect, Completion = completion ?? new AsyncSubject<IEnumerable<CacheElement>>() };
        }

        public static OperationQueueItem CreateUnit(OperationType opType, AsyncSubject<Unit> completion = null)
        {
            return new OperationQueueItem() { OperationType = opType, Parameters = null, Completion = completion ?? new AsyncSubject<Unit>() };
        }

        public static OperationQueueItem CreateGetAllKeys()
        {
            return new OperationQueueItem() { OperationType = OperationType.GetKeysSqliteOperation, Parameters = null, Completion = new AsyncSubject<IEnumerable<string>>() };
        }
    }
}