using System;
using System.Data.SqlServerCe;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache.SqlServerCompact
{
    internal static partial class Extensions
    {
        internal static IObservable<Unit> CreateSchemaInfoTable(this SqlCeConnection connection)
        {
            return Observable.Return(Unit.Default);
        }
    }
}
