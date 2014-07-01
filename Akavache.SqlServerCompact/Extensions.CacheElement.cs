using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache.SqlServerCompact
{
    public static partial class Extensions
    {
        internal static IObservable<Unit> CreateCacheElementTable(this SqlConnection connection)
        {
            return Observable.Start(() =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE dbo.CacheElement (Key NVARCHAR(max) PRIMARY KEY, TypeName NVARCHAR(max), Value VARBINARY(max) NOT NULL, CreatedAt DATETIME NOT NULL, Expiration DATETIME NOT NULL)";
                command.ExecuteNonQuery();
            });
        }

        internal static IObservable<List<CacheElement>> QueryCacheElement(this SqlConnection connection, string key)
        {
            return Observable.Start(() =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT TOP 1 Key,TypeName,Value,CreatedAt,Expiration FROM CacheElement WHERE Key = @ID";
                command.Parameters.AddWithValue("ID", key);

                var list = new List<CacheElement>();

                using (var result = command.ExecuteReader())
                {
                    if (result.HasRows)
                    {
                        var cacheElement = new CacheElement
                        {
                            Key = result.GetFieldValue<string>(0),
                            TypeName = result.GetFieldValue<string>(1),
                            Value = result.GetFieldValue<byte[]>(2),
                            CreatedAt = result.GetFieldValue<DateTime>(3),
                            Expiration = result.GetFieldValue<DateTime>(4)
                        };
                        list.Add(cacheElement);
                    }
                }
                return list;
            });
        }

    }
}
