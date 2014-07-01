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
            return Observable.Defer(async () =>
            {
                await Ensure.IsOpen(connection);
                var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE dbo.CacheElement (Key NVARCHAR(max) PRIMARY KEY, TypeName NVARCHAR(max), Value VARBINARY(max) NOT NULL, CreatedAt DATETIME NOT NULL, Expiration DATETIME NOT NULL)";
                command.ExecuteNonQuery();

                return Observable.Return(Unit.Default);
            });
        }

        internal static IObservable<List<CacheElement>> QueryCacheById(this SqlConnection connection, string key)
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

        internal static IObservable<List<CacheElement>> QueryCacheByExpiration(this SqlConnection connection, DateTime time)
        {
            return Observable.Start(() =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "SELECT Key,TypeName,Value,CreatedAt,Expiration FROM CacheElement WHERE Expiration >= @Expiration";
                command.Parameters.AddWithValue("Expiration", time);

                var list = new List<CacheElement>();

                using (var result = command.ExecuteReader())
                {
                    if (result.HasRows)
                    {
                        while (result.Read())
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
                }
                return list;
            });
        }

        internal static IObservable<Unit> Insert(this SqlConnection connection, CacheElement element)
        {
            return Observable.Start(() =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO dbo.CacheElement (Key,TypeName,Value,CreatedAt,Expiration) VALUES (@Key, @TypeName, @Value, @CreatedAt, @Expiration)";
                command.Parameters.AddWithValue("@Key", element.Key);
                command.Parameters.AddWithValue("@TypeName", element.TypeName);
                command.Parameters.AddWithValue("@Value", element.Value);
                command.Parameters.AddWithValue("@CreatedAt", element.CreatedAt);
                command.Parameters.AddWithValue("@Expiration", element.Expiration);
                command.ExecuteNonQuery();
            });
        }

        internal static IObservable<Unit> DeleteFromCache(this SqlConnection connection, string key)
        {
            return Observable.Start(() =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM CacheElement WHERE Key = @Key";
                command.Parameters.AddWithValue("@Key", key);
                command.ExecuteNonQuery();
            });
        }

        internal static IObservable<Unit> DeleteAllFromCache(this SqlConnection connection)
        {
            return Observable.Start(() =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM CacheElement";
                command.ExecuteNonQuery();
            });
        }

        internal static IObservable<Unit> DeleteExpiredElements(this SqlConnection connection, DateTime time)
        {
            return Observable.Start(() =>
            {
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM CacheElement WHERE Expiration < @expiration";
                command.Parameters.AddWithValue("expiration", time);
                command.ExecuteNonQuery();
            });
        }

        // TODO: actually implement this based on notes here: http://technet.microsoft.com/en-us/library/ms172411(v=sql.110).aspx
        internal static IObservable<Unit> Vacuum(this SqlConnection connection, DateTime time)
        {
            return Observable.Return(Unit.Default);
        }
    }
}
