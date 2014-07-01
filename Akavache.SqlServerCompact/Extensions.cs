using System;
using System.Data.SqlServerCe;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache.SqlServerCompact
{
    public static partial class Extensions
    {
        public static IObservable<Unit> CreateSchemaInfoTable(this SqlCeConnection connection)
        {
            return Observable.Return(Unit.Default);
        }

        public static IObservable<Unit> ExecuteAsync(this SqlCeConnection connection, string text)
        {
            return Observable.Return(Unit.Default);
        }

        public static IObservable<T> ExecuteScalarAsync<T>(this SqlCeConnection connection, string text)
        {
            return Observable.Return(default(T));
        }
    }
}
