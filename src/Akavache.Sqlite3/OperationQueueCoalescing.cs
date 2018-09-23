using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akavache.Sqlite3.Internal;
using Splat;
using SQLitePCL;
using AsyncLock = Akavache.Sqlite3.Internal.AsyncLock;

namespace Akavache.Sqlite3
{
    internal partial class SqliteOperationQueue : IEnableLogger, IDisposable
    {
        private const string nullKey = "___THIS_IS_THE_NULL_KEY_HOPE_NOBODY_PICKS_IT_FFS_____";

        internal static List<OperationQueueItem> CoalesceOperations(List<OperationQueueItem> inputItems)
        {
            // Happy path, nothing to coalesce.
            if (inputItems.Count <= 1) {
                return inputItems;
            }

            var ret = new List<OperationQueueItem>();

            if (inputItems.Any(x => x.OperationType == OperationType.GetKeysSqliteOperation ||
                x.OperationType == OperationType.InvalidateAllSqliteOperation)) {
                // NB: GetAllKeys and InvalidateAll results are highly dependent 
                // on ordering, and we're not smart enough at the moment to return
                // the right results.
                return inputItems;
            }

            // 1. GroupBy key, then by original order
            var groupedOps = new Dictionary<string, List<OperationQueueItem>>();
            foreach (var v in inputItems) {
                var key = GetKeyFromTuple(v) ?? nullKey;
                if (!groupedOps.ContainsKey(key)) {
                    groupedOps.Add(key, new List<OperationQueueItem>());
                }

                var list = groupedOps[key];
                list.Add(v);
            }

            // 2. Simple dedup of multiple people asking for same thing
            var toDedup = new[] {
                OperationType.BulkInvalidateSqliteOperation,
                OperationType.BulkInsertSqliteOperation,
                OperationType.BulkSelectSqliteOperation
            };

            foreach (var key in groupedOps.Keys.ToList()) {
                // NB: We generally don't want to optimize any op that doesn't
                // have a key
                if (key == nullKey) {
                    continue;
                }

                groupedOps[key] = toDedup
                    .Aggregate((IEnumerable<OperationQueueItem>)groupedOps[key],
                        (acc, x) => MultipleOpsTurnIntoSingleOp(acc, x))
                    .ToList();
            }

            while (groupedOps.Count > 0) {
                var toProcess = new List<OperationQueueItem>();
                var toRemove = new List<string>();

                // 3. Take all the *first* items from every group 
                foreach (var key in groupedOps.Keys) {
                    var list = groupedOps[key];

                    toProcess.Add(list[0]);
                    list.RemoveAt(0);
                    if (list.Count == 0) {
                        toRemove.Add(key);
                    }
                }

                // 4. Group by request type (insert, etc)
                var finalItems = CoalesceUnrelatedItems(toProcess);

                // 5. Yield the ops out
                ret.AddRange(finalItems);

                foreach (var v in toRemove) {
                    groupedOps.Remove(v);
                }
            }

            return ret;
        }

        private static IEnumerable<OperationQueueItem> CoalesceUnrelatedItems(IEnumerable<OperationQueueItem> items)
        {
            return items.GroupBy(x => x.OperationType)
                .SelectMany(group => {
                    switch (group.Key) {
                        case OperationType.BulkSelectSqliteOperation:
                            return new[] { GroupUnrelatedSelects(group) };
                        case OperationType.BulkInsertSqliteOperation:
                            return new[] { GroupUnrelatedInserts(group) };
                        case OperationType.BulkInvalidateSqliteOperation:
                            return new[] { GroupUnrelatedDeletes(group) };
                        default:
                            return (IEnumerable<OperationQueueItem>)group;
                    }
                });
        }

        private static IEnumerable<OperationQueueItem> MultipleOpsTurnIntoSingleOp(IEnumerable<OperationQueueItem> itemsWithSameKey, OperationType opTypeToDedup)
        {
            var currentWrites = default(List<OperationQueueItem>);

            foreach (var item in itemsWithSameKey) {
                if (item.OperationType == opTypeToDedup) {
                    currentWrites = currentWrites ?? new List<OperationQueueItem>();
                    currentWrites.Add(item);
                    continue;
                }

                if (currentWrites != null) {
                    if (currentWrites.Count == 1) {
                        yield return currentWrites[0];
                    } else {
                        yield return new OperationQueueItem()
                        {
                            OperationType = currentWrites[0].OperationType,
                            Parameters = currentWrites[0].Parameters,
                            Completion = CombineSubjectsByOperation(
                                currentWrites[0].Completion, currentWrites.Skip(1).Select(x => x.Completion), opTypeToDedup),
                        };
                    }

                    currentWrites = null;
                }

                yield return item;
            }

            if (currentWrites != null) {
                yield return new OperationQueueItem()
                {
                    OperationType = currentWrites[0].OperationType,
                    Parameters = currentWrites[0].Parameters,
                    Completion = CombineSubjectsByOperation(
                        currentWrites[0].Completion, currentWrites.Skip(1).Select(x => x.Completion), opTypeToDedup),
                };
            }
        }

        private static OperationQueueItem GroupUnrelatedSelects(IEnumerable<OperationQueueItem> unrelatedSelects)
        {
            var elementMap = new Dictionary<string, AsyncSubject<IEnumerable<CacheElement>>>();

            if (unrelatedSelects.Count() == 1) {
                return unrelatedSelects.First();
            }

            foreach (var v in unrelatedSelects) {
                var key = v.ParametersAsKeys.First();
                elementMap[key] = v.CompletionAsElements;
            }

            var ret = OperationQueueItem.CreateSelect(OperationType.BulkSelectSqliteOperation, elementMap.Keys);

            ret.CompletionAsElements.Subscribe(items => {
                var resultMap = items.ToDictionary(k => k.Key, v => v);
                foreach (var v in elementMap.Keys) {
                    if (resultMap.ContainsKey(v)) {
                        elementMap[v].OnNext(EnumerableEx.Return(resultMap[v]));
                    } else {
                        elementMap[v].OnNext(Enumerable.Empty<CacheElement>());
                    }

                    elementMap[v].OnCompleted();
                }
            },
            ex => {
                foreach (var v in elementMap.Values) {
                    v.OnError(ex);
                }
            },
            () => {
                foreach (var v in elementMap.Values) {
                    v.OnCompleted();
                }
            });

            return ret;
        }

        private static OperationQueueItem GroupUnrelatedInserts(IEnumerable<OperationQueueItem> unrelatedInserts)
        {
            if (unrelatedInserts.Count() == 1) {
                return unrelatedInserts.First();
            }

            var subj = new AsyncSubject<Unit>();
            var elements = unrelatedInserts.SelectMany(x => {
                subj.Subscribe(x.CompletionAsUnit);
                return x.ParametersAsElements;
            }).ToList();

            return OperationQueueItem.CreateInsert(
                OperationType.BulkInsertSqliteOperation, elements, subj);
        }

        private static OperationQueueItem GroupUnrelatedDeletes(IEnumerable<OperationQueueItem> unrelatedDeletes)
        {
            var subj = new AsyncSubject<Unit>();
            if (unrelatedDeletes.Count() == 1) {
                return unrelatedDeletes.First();
            }

            var elements = unrelatedDeletes.SelectMany(x => {
                subj.Subscribe(x.CompletionAsUnit);
                return x.ParametersAsKeys;
            }).ToList();

            return OperationQueueItem.CreateInvalidate(
                OperationType.BulkInvalidateSqliteOperation, elements, subj);
        }

        private static string GetKeyFromTuple(OperationQueueItem item)
        {
            // NB: This method assumes that the input tuples only have a 
            // single item, which the OperationQueue input methods guarantee
            switch (item.OperationType) {
                case OperationType.BulkInsertSqliteOperation:
                    return (item.ParametersAsElements).First().Key;
                case OperationType.BulkInvalidateByTypeSqliteOperation:
                case OperationType.BulkInvalidateSqliteOperation:
                case OperationType.BulkSelectSqliteOperation:
                case OperationType.BulkSelectByTypeSqliteOperation:
                    return (item.ParametersAsKeys).First();
                case OperationType.GetKeysSqliteOperation:
                case OperationType.InvalidateAllSqliteOperation:
                case OperationType.VacuumSqliteOperation:
                case OperationType.DeleteExpiredSqliteOperation:
                case OperationType.DoNothing:
                    return default(string);
                default:
                    throw new ArgumentException("Unknown operation");
            }
        }

        private static object CombineSubjectsByOperation(object source, IEnumerable<object> subjs, OperationType opType)
        {
            switch (opType) {
                case OperationType.BulkSelectSqliteOperation:
                    return CombineSubjects<IEnumerable<CacheElement>>(
                        (AsyncSubject<IEnumerable<CacheElement>>)source,
                        subjs.Cast<AsyncSubject<IEnumerable<CacheElement>>>());
                case OperationType.BulkInsertSqliteOperation:
                case OperationType.BulkInvalidateSqliteOperation:
                    return CombineSubjects<Unit>(
                        (AsyncSubject<Unit>)source,
                        subjs.Cast<AsyncSubject<Unit>>());
                default:
                    throw new ArgumentException("Invalid operation type");
            }
        }

        private static AsyncSubject<T> CombineSubjects<T>(AsyncSubject<T> source, IEnumerable<AsyncSubject<T>> subjs)
        {
            foreach (var v in subjs) {
                source.Subscribe(v);
            }

            return source;
        }
    }

    internal static class EnumerableEx
    {
        public static IEnumerable<T> Return<T>(T value)
        {
            yield return value;
        }
    }
}
