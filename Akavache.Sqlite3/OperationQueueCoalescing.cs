using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reactive;
using System.Threading;
using Akavache.Sqlite3.Internal;
using SQLitePCL;
using Splat;

using AsyncLock = Akavache.Sqlite3.Internal.AsyncLock;

namespace Akavache.Sqlite3
{
    partial class SqliteOperationQueue : IEnableLogger, IDisposable
    {
        const string nullKey = "___THIS_IS_THE_NULL_KEY_HOPE_NOBODY_PICKS_IT_FFS_____";

        static internal List<Tuple<OperationType, IEnumerable, object>> CoalesceOperations(List<Tuple<OperationType, IEnumerable, object>> inputItems)
        {
            var ret = new List<Tuple<OperationType, IEnumerable, object>>();

            // 1. GroupBy key, then by original order
            var groupedOps = new Dictionary<string, List<Tuple<OperationType, IEnumerable, object>>>();
            foreach (var v in inputItems)
            {
                var key = GetKeyFromTuple(v) ?? nullKey;
                if (!groupedOps.ContainsKey(key))
                {
                    groupedOps.Add(key, new List<Tuple<OperationType, IEnumerable, object>>());
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

            foreach (var key in groupedOps.Keys)
            {
                // NB: We generally don't want to optimize any op that doesn't
                // have a key
                if (key == nullKey) continue;

                groupedOps[key] = toDedup
                    .Aggregate((IEnumerable<Tuple<OperationType, IEnumerable, object>>)groupedOps[key], 
                        (acc, x) => MultipleOpsTurnIntoSingleOp(acc, x))
                    .ToList();
            }

            while (groupedOps.Count > 0)
            {
                var toProcess = new List<Tuple<OperationType, IEnumerable, object>>();
                var toRemove = new List<string>();

                // 3. Take all the *first* items from every group 
                foreach (var key in groupedOps.Keys)
                {
                    var list = groupedOps[key];

                    toProcess.Add(list[0]);
                    list.RemoveAt(0);
                    if (list.Count == 0) toRemove.Add(key);
                }

                // 4. Group by request type (insert, etc)
                var finalItems = CoalesceUnrelatedItems(toProcess);

                // 5. Yield the ops out
                ret.AddRange(finalItems);

                foreach (var v in toRemove) groupedOps.Remove(v);
            }

            return ret;
        }

        static IEnumerable<Tuple<OperationType, IEnumerable, object>> CoalesceUnrelatedItems(IEnumerable<Tuple<OperationType, IEnumerable, object>> items)
        {
            return items.GroupBy(x => x.Item1)
                .SelectMany(group =>
                {
                    switch (group.Key)
                    {
                        case OperationType.BulkInsertSqliteOperation:
                            return new[] { GroupUnrelatedInserts(group) };
                        case OperationType.BulkInvalidateSqliteOperation:
                            return new[] { GroupUnrelatedDeletes(group) };
                        default:
                            return (IEnumerable<Tuple<OperationType, IEnumerable, object>>)group;
                    }
                });
        }

        static IEnumerable<Tuple<OperationType, IEnumerable, object>> MultipleOpsTurnIntoSingleOp(IEnumerable<Tuple<OperationType, IEnumerable, object>> itemsWithSameKey, OperationType opTypeToDedup)
        {
            var currentWrites = default(List<Tuple<OperationType, IEnumerable, object>>);
            return itemsWithSameKey.SelectMany(op =>
            {
                if (op.Item1 == opTypeToDedup)
                {
                    currentWrites = currentWrites ?? new List<Tuple<OperationType, IEnumerable, object>>();
                    currentWrites.Add(op);

                    return Enumerable.Empty<Tuple<OperationType, IEnumerable, object>>();
                }

                if (currentWrites == null) return new[] { op };

                var firstItem = currentWrites[0];
                var newSubj = CombineSubjects(
                    (AsyncSubject<byte[]>)firstItem.Item3,
                    currentWrites.Skip(1).Select(x => (AsyncSubject<byte[]>)x.Item3));

                currentWrites = null;

                return new[] { 
                    new Tuple<OperationType, IEnumerable, object>(firstItem.Item1, firstItem.Item2, newSubj), 
                    op
                };
            });
        }

        static Tuple<OperationType, IEnumerable, object> GroupUnrelatedSelects(IEnumerable<Tuple<OperationType, IEnumerable, object>> unrelatedInserts)
        {
            throw new NotImplementedException();
        }

        static Tuple<OperationType, IEnumerable, object> GroupUnrelatedInserts(IEnumerable<Tuple<OperationType, IEnumerable, object>> unrelatedInserts)
        {
            var subj = new AsyncSubject<Unit>();

            var elements = unrelatedInserts.SelectMany(x =>
            {
                subj.Subscribe((AsyncSubject<Unit>)x.Item3);
                return (IEnumerable<CacheElement>)x.Item2;
            }).ToList();
 
            return new Tuple<OperationType, IEnumerable, object>(
                OperationType.BulkInsertSqliteOperation, elements, subj);
        }

        static Tuple<OperationType, IEnumerable, object> GroupUnrelatedDeletes(IEnumerable<Tuple<OperationType, IEnumerable, object>> unrelatedDeletes)
        {
            var subj = new AsyncSubject<Unit>();

            var elements = unrelatedDeletes.SelectMany(x =>
            {
                subj.Subscribe((AsyncSubject<Unit>)x.Item3);
                return (IEnumerable<string>)x.Item2;
            }).ToList();
 
            return new Tuple<OperationType, IEnumerable, object>(
                OperationType.BulkInsertSqliteOperation, elements, subj);
        }

        static string GetKeyFromTuple(Tuple<OperationType, IEnumerable, object> item)
        {
            // NB: This method assumes that the input tuples only have a 
            // single item, which the OperationQueue input methods guarantee
            switch (item.Item1)
            {
                case OperationType.BulkInsertSqliteOperation:
                    return ((IEnumerable<CacheElement>)item.Item2).First().Key;
                case OperationType.BulkInvalidateByTypeSqliteOperation:
                    return default(string);
                case OperationType.BulkInvalidateSqliteOperation:
                    return ((IEnumerable<string>)item.Item2).First();
                case OperationType.BulkSelectByTypeSqliteOperation:
                    return default(string);
                case OperationType.BulkSelectSqliteOperation:
                    return ((IEnumerable<string>)item.Item2).First();
                case OperationType.GetKeysSqliteOperation:
                    return default(string);
                case OperationType.InvalidateAllSqliteOperation:
                    return default(string);
                case OperationType.VacuumSqliteOperation:
                    return default(string);
                default:
                    throw new ArgumentException("Unknown operation");
            }
        }

        static AsyncSubject<T> CombineSubjects<T>(AsyncSubject<T> source, IEnumerable<AsyncSubject<T>> subjs)
        {
            foreach (var v in subjs) source.Subscribe(v);
            return source;
        }
    }
}