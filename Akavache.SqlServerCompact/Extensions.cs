using System;
using System.Data.SqlClient;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache.SqlServerCompact
{
    public static partial class Extensions
    {
        public static IObservable<Unit> CreateSchemaInfoTable(this SqlConnection connection)
        {
            return Observable.Return(Unit.Default);
        }

        public static IObservable<Unit> ExecuteAsync(this SqlConnection connection, string text)
        {
            return Observable.Return(Unit.Default);
        }

        public static IObservable<T> ExecuteScalarAsync<T>(this SqlConnection connection, string text)
        {
            return Observable.Return(default(T));
        }
    }
}
