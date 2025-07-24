// Copyright (c) 2025 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Akavache.Sqlite3;

using DynamicData;

#pragma warning disable CS4014 // Await on awaitable items. -- We don't wait on the observables.

namespace Akavache.Tests;

/// <summary>
/// Tests associated with the <see cref="SqliteOperationQueue"/> method.
/// </summary>
public class CoalescerTests
{
    /// <summary>
    /// Tests for a single item.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task SingleItem()
    {
        var fixture = new SqliteOperationQueue();
        fixture.Select(["Foo"]);

        var queue = fixture.DumpQueue();
        var subj = queue[0].CompletionAsElements;
        subj.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var output).Subscribe();
        Assert.Equal(0, output.Count);

        var result = SqliteOperationQueue.CoalesceOperations(queue);

        Assert.Equal(1, result.Count);
        Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].OperationType);

        // Make sure the input gets a result when we signal the output's subject
        var outSub = result[0].CompletionAsElements;

        Assert.Equal(0, output.Count);
        outSub.OnNext([new CacheElement { Key = "Foo" }]);
        outSub.OnCompleted();
        await Task.Delay(500);
        Assert.Equal(1, output.Count);
    }

    /// <summary>
    /// Tests for a unrelated items.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task UnrelatedItems()
    {
        var fixture = new SqliteOperationQueue();
        fixture.Select(["Foo"]);
        fixture.Insert([new CacheElement { Key = "Bar" }]);
        fixture.Invalidate(["Baz"]);

        var queue = fixture.DumpQueue();
        var subj = queue[0].CompletionAsElements;
        subj.ToObservableChangeSet(ImmediateScheduler.Instance).Bind(out var output).Subscribe();
        Assert.Equal(0, output.Count);

        var result = SqliteOperationQueue.CoalesceOperations(queue);

        Assert.Equal(3, result.Count);
        Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].OperationType);

        // Make sure the input gets a result when we signal the output's subject
        var outSub = result[0].CompletionAsElements;

        Assert.Equal(0, output.Count);
        outSub.OnNext([new CacheElement { Key = "Foo" }]);
        outSub.OnCompleted();
        await Task.Delay(500);
        Assert.Equal(1, output.Count);
    }

    /// <summary>
    /// Tests for unrelated selected.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task CoalesceUnrelatedSelects()
    {
        var fixture = new SqliteOperationQueue();

        fixture.Select(["Foo"]);
        fixture.Select(["Bar"]);
        fixture.Invalidate(["Bamf"]);
        fixture.Select(["Baz"]);

        var queue = fixture.DumpQueue();
        queue.Where(x => x.OperationType == OperationType.BulkSelectSqliteOperation)
            .Select(x => x.CompletionAsElements)
            .Merge()
            .ToObservableChangeSet(ImmediateScheduler.Instance)
            .Bind(out var output)
            .Subscribe();
        var result = SqliteOperationQueue.CoalesceOperations(queue);

        Assert.Equal(2, result.Count);

        var item = result.Single(x => x.OperationType == OperationType.BulkSelectSqliteOperation);
        Assert.Equal(OperationType.BulkSelectSqliteOperation, item.OperationType);
        Assert.Equal(3, item.ParametersAsKeys.Count());

        // All three of the input Selects should get a value when we signal
        // our output Select
        var outSub = item.CompletionAsElements;
        var fakeResult = new[]
        {
            new CacheElement { Key = "Foo" },
            new CacheElement { Key = "Bar" },
            new CacheElement { Key = "Baz" },
        };

        Assert.Equal(0, output.Count);
        outSub.OnNext(fakeResult);
        outSub.OnCompleted();
        await Task.Delay(1000);
        Assert.Equal(3, output.Count);
    }

    /// <summary>
    /// Tests to make sure the de-duplication of related selects works.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task DedupRelatedSelects()
    {
        var fixture = new SqliteOperationQueue();
        fixture.Select(["Foo"]);
        fixture.Select(["Foo"]);
        fixture.Select(["Bar"]);
        fixture.Select(["Foo"]);

        var queue = fixture.DumpQueue();
        queue.Where(x => x.OperationType == OperationType.BulkSelectSqliteOperation)
            .Select(x => x.CompletionAsElements)
            .Merge()
            .ToObservableChangeSet(ImmediateScheduler.Instance)
            .Bind(out var output)
            .Subscribe();
        var result = SqliteOperationQueue.CoalesceOperations(queue);

        Assert.Equal(1, result.Count);
        Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].OperationType);
        Assert.Equal(2, result[0].ParametersAsKeys.Count());

        var fakeResult = new[]
        {
            new CacheElement { Key = "Foo" },
            new CacheElement { Key = "Bar" },
        };

        var outSub = result[0].CompletionAsElements;

        Assert.Equal(0, output.Count);
        outSub.OnNext(fakeResult);
        outSub.OnCompleted();
        await Task.Delay(1000);
        Assert.Equal(4, output.Count);
    }

    /// <summary>
    /// Tests to make sure that interpolated operations don't get de-duplicated.
    /// </summary>
    [Fact]
    public void InterpolatedOpsDontGetDeduped()
    {
        var fixture = new SqliteOperationQueue();
        fixture.Select(["Foo"]);
        fixture.Insert([new CacheElement { Key = "Foo", Value = [1, 2, 3] }]);
        fixture.Select(["Foo"]);
        fixture.Insert([new CacheElement { Key = "Foo", Value = [4, 5, 6] }]);

        var queue = fixture.DumpQueue();
        var result = SqliteOperationQueue.CoalesceOperations(queue);

        Assert.Equal(4, result.Count);
        Assert.Equal(OperationType.BulkSelectSqliteOperation, result[0].OperationType);
        Assert.Equal(OperationType.BulkInsertSqliteOperation, result[1].OperationType);
        Assert.Equal(OperationType.BulkSelectSqliteOperation, result[2].OperationType);
        Assert.Equal(OperationType.BulkInsertSqliteOperation, result[3].OperationType);

        Assert.Equal(1, result[1].ParametersAsElements.First().Value[0]);
        Assert.Equal(4, result[3].ParametersAsElements.First().Value[0]);
    }

    /// <summary>
    /// Tests to make sure that grouped requests with different keys return empty results.
    /// </summary>
    /// <returns>A task to monitor the progress.</returns>
    [Fact]
    public async Task GroupedRequestsWithDifferentKeysReturnEmptyResultIfItemsDontExist()
    {
        using (Utility.WithEmptyDirectory(out var path))
        using (var cache = new SQLitePersistentBlobCache(Path.Combine(path, "sqlite.db")))
        {
            var queue = new SqliteOperationQueue(cache.Connection, BlobCache.TaskpoolScheduler);
            var request = queue.Select(["Foo"]);
            var unrelatedRequest = queue.Select(["Bar"]);

            cache.ReplaceOperationQueue(queue);

            Assert.Equal(0, (await request).Count());
            Assert.Equal(0, (await unrelatedRequest).Count());
        }
    }
}
