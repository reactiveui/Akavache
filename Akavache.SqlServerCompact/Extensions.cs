using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Reactive;
using System.Reactive.Linq;

namespace Akavache.SqlServerCompact
{
    public static class Extensions
    {
        public static IObservable<Unit> CreateCacheElementTable(this SqlConnection connection)
        {
            return Observable.Return(Unit.Default);
        }

        public static IObservable<Unit> CreateSchemaInfoTable(this SqlConnection connection)
        {
            return Observable.Return(Unit.Default);
        }

        public static IObservable<List<CacheElement>> QueryElement(this SqlConnection connection, string key)
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

        public static IObservable<Unit> ExecuteAsync(this SqlConnection connection, string text)
        {
            return Observable.Return(Unit.Default);
        }

        public static IObservable<T> ExecuteScalarAsync<T>(this SqlConnection connection, string text)
        {
            return Observable.Return(default(T));
        }
    }

    public class CacheElement
    {
        public string Key { get; set; }
        public string TypeName { get; set; }
        public byte[] Value { get; set; }
        public DateTime Expiration { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
