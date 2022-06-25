// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections;

namespace Akavache.Sqlite3;

internal class OperationQueueItem
{
    public OperationQueueItem(object completion, IEnumerable? parameters)
    {
        Completion = completion;
        Parameters = parameters;
    }

    public OperationType OperationType { get; set; }

    public IEnumerable? Parameters { get; set; }

    public object Completion { get; set; }

    public IEnumerable<CacheElement>? ParametersAsElements => (IEnumerable<CacheElement>?)Parameters;

    public IEnumerable<string>? ParametersAsKeys => (IEnumerable<string>?)Parameters;

    public AsyncSubject<Unit> CompletionAsUnit => (AsyncSubject<Unit>)Completion;

    public AsyncSubject<IEnumerable<CacheElement>> CompletionAsElements => (AsyncSubject<IEnumerable<CacheElement>>)Completion;

    public AsyncSubject<IEnumerable<string>> CompletionAsKeys => (AsyncSubject<IEnumerable<string>>)Completion;

    public static OperationQueueItem CreateInsert(OperationType opType, IEnumerable<CacheElement> toInsert, AsyncSubject<Unit>? completion = null) => new(completion ?? new AsyncSubject<Unit>(), toInsert) { OperationType = opType };

    public static OperationQueueItem CreateInvalidate(OperationType opType, IEnumerable<string> toInvalidate, AsyncSubject<Unit>? completion = null) => new(completion ?? new AsyncSubject<Unit>(), toInvalidate) { OperationType = opType };

    public static OperationQueueItem CreateSelect(OperationType opType, IEnumerable<string> toSelect, AsyncSubject<IEnumerable<CacheElement>>? completion = null) => new(completion ?? new AsyncSubject<IEnumerable<CacheElement>>(), toSelect) { OperationType = opType };

    public static OperationQueueItem CreateUnit(OperationType opType, AsyncSubject<Unit>? completion = null) => new(completion ?? new AsyncSubject<Unit>(), null) { OperationType = opType };

    public static OperationQueueItem CreateGetAllKeys() => new(new AsyncSubject<IEnumerable<string>>(), null) { OperationType = OperationType.GetKeysSqliteOperation };
}