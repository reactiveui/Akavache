// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace Akavache.Sqlite3;

internal partial class SqliteOperationQueue
{
    private const string NullKey = "___THIS_IS_THE_NULL_KEY_HOPE_NOBODY_PICKS_IT_FFS_____";

    internal static List<OperationQueueItem> CoalesceOperations(List<OperationQueueItem> inputItems)
    {
        // Happy path, nothing to coalesce.
        if (inputItems.Count <= 1)
        {
            return inputItems;
        }

        var ret = new List<OperationQueueItem>();

        if (inputItems.Any(x => x.OperationType is OperationType.GetKeysSqliteOperation or OperationType.InvalidateAllSqliteOperation))
        {
            // NB: GetAllKeys and InvalidateAll results are highly dependent
            // on ordering, and we're not smart enough at the moment to return
            // the right results.
            return inputItems;
        }

        // 1. GroupBy key, then by original order
        var groupedOps = new Dictionary<string, List<OperationQueueItem>>();
        foreach (var v in inputItems)
        {
            var key = GetKeyFromTuple(v) ?? NullKey;
            if (!groupedOps.ContainsKey(key))
            {
                groupedOps.Add(key, new());
            }

            var list = groupedOps[key];
            list.Add(v);
        }

        // 2. Simple dedup of multiple people asking for same thing
        var toDedup = new[]
        {
            OperationType.BulkInvalidateSqliteOperation,
            OperationType.BulkInsertSqliteOperation,
            OperationType.BulkSelectSqliteOperation,
        };

        foreach (var key in groupedOps.Keys.ToList())
        {
            // NB: We generally don't want to optimize any op that doesn't
            // have a key
            if (key is NullKey)
            {
                continue;
            }

            groupedOps[key] = toDedup
                .Aggregate(
                    (IEnumerable<OperationQueueItem>)groupedOps[key],
                    MultipleOpsTurnIntoSingleOp)
                .ToList();
        }

        while (groupedOps.Count > 0)
        {
            var toProcess = new List<OperationQueueItem>();
            var toRemove = new List<string>();

            // 3. Take all the *first* items from every group
            foreach (var key in groupedOps.Keys)
            {
                var list = groupedOps[key];

                toProcess.Add(list[0]);
                list.RemoveAt(0);
                if (list.Count == 0)
                {
                    toRemove.Add(key);
                }
            }

            // 4. Group by request type (insert, etc)
            var finalItems = CoalesceUnrelatedItems(toProcess);

            // 5. Yield the ops out
            ret.AddRange(finalItems);

            foreach (var v in toRemove)
            {
                groupedOps.Remove(v);
            }
        }

        return ret;
    }

    private static IEnumerable<OperationQueueItem> CoalesceUnrelatedItems(IEnumerable<OperationQueueItem> items) =>
        items.GroupBy(x => x.OperationType)
            .SelectMany(group => group.Key switch
            {
                OperationType.BulkSelectSqliteOperation => new[] { GroupUnrelatedSelects(group) },
                OperationType.BulkInsertSqliteOperation => new[] { GroupUnrelatedInserts(group) },
                OperationType.BulkInvalidateSqliteOperation => new[] { GroupUnrelatedDeletes(group) },
                _ => (IEnumerable<OperationQueueItem>)group
            });

    private static IEnumerable<OperationQueueItem> MultipleOpsTurnIntoSingleOp(IEnumerable<OperationQueueItem> itemsWithSameKey, OperationType opTypeToDedup)
    {
        var currentWrites = default(List<OperationQueueItem>);

        foreach (var item in itemsWithSameKey)
        {
            if (item.OperationType == opTypeToDedup)
            {
                currentWrites ??= new();
                currentWrites.Add(item);
                continue;
            }

            if (currentWrites is not null)
            {
                yield return currentWrites.Count == 1
                    ? currentWrites[0]
                    : new(
                        CombineSubjectsByOperation(
                            currentWrites[0].Completion,
                            currentWrites.Skip(1).Select(x => x.Completion),
                            opTypeToDedup),
                        currentWrites[0].Parameters)
                    {
                        OperationType = currentWrites[0].OperationType,
                    };

                currentWrites = null;
            }

            yield return item;
        }

        if (currentWrites is not null)
        {
            yield return new(
                CombineSubjectsByOperation(
                    currentWrites[0].Completion, currentWrites.Skip(1).Select(x => x.Completion), opTypeToDedup),
                currentWrites[0].Parameters)
            {
                OperationType = currentWrites[0].OperationType,
            };
        }
    }

    private static OperationQueueItem GroupUnrelatedSelects(IEnumerable<OperationQueueItem> unrelatedSelects)
    {
        var elementMap = new Dictionary<string, AsyncSubject<IEnumerable<CacheElement>>>();

        var operationQueueItems = unrelatedSelects.ToList();
        if (operationQueueItems.Count == 1)
        {
            return operationQueueItems[0];
        }

        foreach (var v in operationQueueItems)
        {
            var key = v.ParametersAsKeys?.First();

            if (key is null)
            {
                continue;
            }

            elementMap[key] = v.CompletionAsElements;
        }

        var ret = OperationQueueItem.CreateSelect(OperationType.BulkSelectSqliteOperation, elementMap.Keys);

        ret.CompletionAsElements.Subscribe(
            items =>
            {
                var resultMap = items.ToDictionary(k => k.Key, v => v);
                foreach (var v in elementMap.Keys)
                {
                    try
                    {
                        elementMap[v].OnNext(resultMap.ContainsKey(v)
                            ? EnumerableEx.Return(resultMap[v])
                            : Enumerable.Empty<CacheElement>());

                        elementMap[v].OnCompleted();
                    }
                    catch (KeyNotFoundException)
                    {
                        // I don't know what to do here but since an exception is swallowed anyway,
                        // lets not stop the remaining elements to be stuck in an in-completed way
                    }
                }
            },
            ex =>
            {
                foreach (var v in elementMap.Values)
                {
                    v.OnError(ex);
                }
            },
            () =>
            {
                foreach (var v in elementMap.Values)
                {
                    v.OnCompleted();
                }
            });

        return ret;
    }

    private static OperationQueueItem GroupUnrelatedInserts(IEnumerable<OperationQueueItem> unrelatedInserts)
    {
        var operationQueueItems = unrelatedInserts.ToList();
        if (operationQueueItems.Count == 1)
        {
            return operationQueueItems[0];
        }

        var subj = new AsyncSubject<Unit>();
        var elements = operationQueueItems.SelectMany(x =>
        {
            subj.Subscribe(x.CompletionAsUnit);
            return x.ParametersAsElements ?? Enumerable.Empty<CacheElement>();
        }).ToList();

        return OperationQueueItem.CreateInsert(
            OperationType.BulkInsertSqliteOperation, elements, subj);

        /* Unmerged change from project 'Akavache.Sqlite3 (uap10.0.16299)'
        Before:
            }

            [SuppressMessage("Design", "CA2000: Dispose variable", Justification = "Ownership transferred.")]
            private static OperationQueueItem GroupUnrelatedDeletes(IEnumerable<OperationQueueItem> unrelatedDeletes)
        After:
            }

            private static OperationQueueItem GroupUnrelatedDeletes(IEnumerable<OperationQueueItem> unrelatedDeletes)
        */
    }

    private static OperationQueueItem GroupUnrelatedDeletes(IEnumerable<OperationQueueItem> unrelatedDeletes)
    {
        var subj = new AsyncSubject<Unit>();
        var operationQueueItems = unrelatedDeletes.ToList();
        if (operationQueueItems.Count == 1)
        {
            return operationQueueItems[0];
        }

        var elements = operationQueueItems.SelectMany(x =>
        {
            subj.Subscribe(x.CompletionAsUnit);
            return x.ParametersAsKeys ?? Enumerable.Empty<string>();
        }).ToList();

        return OperationQueueItem.CreateInvalidate(
            OperationType.BulkInvalidateSqliteOperation, elements, subj);
    }

    private static string? GetKeyFromTuple(OperationQueueItem item)
    {
        // NB: This method assumes that the input tuples only have a
        // single item, which the OperationQueue input methods guarantee
        switch (item.OperationType)
        {
            case OperationType.BulkInsertSqliteOperation when item.ParametersAsElements is not null:
                return item.ParametersAsElements.First().Key;
            case OperationType.BulkInvalidateByTypeSqliteOperation when item.ParametersAsKeys is not null:
            case OperationType.BulkInvalidateSqliteOperation when item.ParametersAsKeys is not null:
            case OperationType.BulkSelectSqliteOperation when item.ParametersAsKeys is not null:
            case OperationType.BulkSelectByTypeSqliteOperation when item.ParametersAsKeys is not null:
                return item.ParametersAsKeys.First();
            case OperationType.GetKeysSqliteOperation:
            case OperationType.InvalidateAllSqliteOperation:
            case OperationType.VacuumSqliteOperation:
            case OperationType.DeleteExpiredSqliteOperation:
            case OperationType.DoNothing:
                return default;
            default:
                throw new ArgumentException("Unknown operation", nameof(item));
        }
    }

    private static object CombineSubjectsByOperation(object source, IEnumerable<object>? subjects, OperationType opType)
    {
        var subjectsList = (subjects ?? Enumerable.Empty<object>()).ToList();
        switch (opType)
        {
            case OperationType.BulkSelectSqliteOperation:
                return CombineSubjects(
                    (AsyncSubject<IEnumerable<CacheElement>>)source,
                    subjectsList.Cast<AsyncSubject<IEnumerable<CacheElement>>>());
            case OperationType.BulkInsertSqliteOperation:
            case OperationType.BulkInvalidateSqliteOperation:
                return CombineSubjects(
                    (AsyncSubject<Unit>)source,
                    subjectsList.Cast<AsyncSubject<Unit>>());
            default:
                throw new ArgumentException("Invalid operation type", nameof(opType));
        }
    }

    private static AsyncSubject<T> CombineSubjects<T>(AsyncSubject<T> source, IEnumerable<AsyncSubject<T>> subjs)
    {
        foreach (var v in subjs)
        {
            source.Subscribe(v);
        }

        return source;
    }
}
