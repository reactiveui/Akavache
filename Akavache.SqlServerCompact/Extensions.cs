using System;
using System.Data.SqlServerCe;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache.SqlServerCompact
{
    internal static partial class Extensions
    {
        internal static IObservable<Unit> ExecuteAsync(this SqlCeConnection connection, string text)
        {
            return Observable.Return(Unit.Default);
        }

        internal static IObservable<T> ExecuteScalarAsync<T>(this SqlCeConnection connection, string text)
        {
            return Observable.Return(default(T));
        }
    }
}
