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
        static internal List<Tuple<OperationType, IEnumerable, object>> CoalesceOperations(List<Tuple<OperationType, IEnumerable, object>> inputItems)
        {
            return inputItems;
        }

        static IEnumerable<string> GetKeysFromTuple(Tuple<OperationType, IEnumerable, object> item)
        {
            switch (item.Item1)
            {
                case OperationType.BulkInsertSqliteOperation:
                    return ((IEnumerable<CacheElement>)item.Item2).Select(x => x.Key);
                case OperationType.BulkInvalidateByTypeSqliteOperation:
                    return ((IEnumerable<string>)item.Item2).Select(_ => default(string));
                case OperationType.BulkInvalidateSqliteOperation:
                    return ((IEnumerable<string>)item.Item2);
                case OperationType.BulkSelectByTypeSqliteOperation:
                    return ((IEnumerable<string>)item.Item2).Select(_ => default(string));
                case OperationType.BulkSelectSqliteOperation:
                    return ((IEnumerable<string>)item.Item2);
                case OperationType.GetKeysSqliteOperation:
                    return new[] { default(string) };
                case OperationType.InvalidateAllSqliteOperation:
                    return new[] { default(string) };
                case OperationType.VacuumSqliteOperation:
                    return new[] { default(string) };
                default:
                    throw new ArgumentException("Unknown operation");
            }
        }
    }
}