using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Akavache.SqlServerCompact
{
    internal static partial class Extensions
    {
        internal static async Task<bool> CacheElementsTableExists(this SqlCeConnection connection)
        {
            await Ensure.IsOpen(connection);

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CacheElement'";
                using (var reader = command.ExecuteReader())
                {
                    var hasRows = reader.Read();
                    return hasRows;
                }
            }
        }

        internal static IObservable<Unit> CreateCacheElementTable(this SqlCeConnection connection)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE CacheElement ([Key] NVARCHAR(4000) NOT NULL, TypeName NVARCHAR(4000) NULL, Value image NOT NULL, CreatedAt DATETIME NOT NULL, Expiration DATETIME NOT NULL)";
                    command.ExecuteNonQuery();
                    command.CommandText = "ALTER TABLE [CacheElement] ADD CONSTRAINT [PK_CacheElement] PRIMARY KEY ([Key]);";
                    command.ExecuteNonQuery();
                }
            });
        }

        internal static IObservable<List<CacheElement>> QueryCacheById(this SqlCeConnection connection, string key)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(key, command);
                    var list = new List<CacheElement>();
                    using (var dataReader = command.ExecuteReader())
                    {
                        if (dataReader.Read())
                        {
                            list.Add(CacheElement.FromDataReader(dataReader));
                        }
                    }
                    return list;
                }
            });
        }

        internal static IObservable<List<CacheElement>> QueryCacheById(this SqlCeConnection connection, IEnumerable<string> keys)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                using (var command = connection.CreateCommand())
                {
                    var keyList = keys.ToList();
                    var y = 0;
                    var keyNames = Enumerable.Range(0, keyList.Count)
                        .Select(x => "@k" + x.ToString(CultureInfo.InvariantCulture))
                        .ToList();

                    command.CommandText = String.Format("SELECT [Key],TypeName,Value,CreatedAt,Expiration FROM CacheElement WHERE [Key] IN ({0})", 
                        String.Join(",", keyNames));

                    for (int i=0; i < keyList.Count; i++) 
                    {
                        command.Parameters.AddWithValue(keyNames[i], keyList[i]);
                    }

                    return CacheElement.FromDataReader(command);
                }
            });
        }

        internal static IObservable<List<CacheElement>> QueryCacheByExpiration(this SqlCeConnection connection, DateTime time)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                var command = connection.CreateCommand();
                command.CommandText = "SELECT [Key],TypeName,Value,CreatedAt,Expiration FROM CacheElement WHERE Expiration >= @Expiration";
                command.Parameters.AddWithValue("Expiration", time);
                return CacheElement.FromDataReader(command);
            });
        }

        internal static IObservable<List<CacheElement>> QueryCacheByType<T>(this SqlCeConnection connection)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                var command = connection.CreateCommand();
                command.CommandText = "SELECT [Key],TypeName,Value,CreatedAt,Expiration FROM CacheElement WHERE TypeName = @TypeName";
                command.Parameters.AddWithValue("TypeName", typeof(T).FullName);
                return CacheElement.FromDataReader(command);
            });
        }

        internal static IObservable<Unit> InsertAll(this SqlCeConnection connection, IEnumerable<CacheElement> elements)
        {
            return elements.ToObservable()
                .Select(connection.InsertOrUpdate)
                .Merge(4);
        }

        internal static IObservable<Unit> InsertOrUpdate(this SqlCeConnection connection, CacheElement element)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);
                var items = await connection.QueryCacheById(element.Key);

                if (items.Count > 0)
                {
                    connection.Update(element);
                }
                else
                {
                    await connection.Insert(element);
                }
            });
        }

        internal static async Task Insert(this SqlCeConnection connection, CacheElement element)
        {
            await Ensure.IsOpen(connection);

            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.TableDirect;
                command.CommandText = "CacheElement";

                using (var resultSet = command.ExecuteResultSet(ResultSetOptions.Updatable))
                {
                    
                    var record = resultSet.CreateRecord();
                    record.SetString(0, element.Key);
                    if (String.IsNullOrWhiteSpace(element.TypeName))
                    {
                        record.SetValue(1, DBNull.Value);
                    }
                    else
                    {
                        record.SetString(1, element.TypeName);
                    }
                    record.SetValue(2, element.Value);
                    record.SetDateTime(3, element.CreatedAt);
                    record.SetDateTime(4, element.Expiration);
                    resultSet.Insert(record);
                }
            }
        }

        static void Update(this SqlCeConnection connection, CacheElement element)
        {
            using (var command = connection.CreateCommand())
            {
                PrepareCommand(element.Key, command);
                using (var record = command.ExecuteResultSet(ResultSetOptions.Updatable))
                {
                    if (record.Read())
                    {
                        if (String.IsNullOrWhiteSpace(element.TypeName))
                        {
                            record.SetValue(1, DBNull.Value);
                        }
                        else
                        {
                            record.SetString(1, element.TypeName);
                        }
                        record.SetValue(2, element.Value);
                        record.SetDateTime(3, element.CreatedAt);
                        record.SetDateTime(4, element.Expiration);
                        record.Update();
                    }
                }
            }
        }


        internal static IObservable<Unit> DeleteFromCache(this SqlCeConnection connection, string key)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                using (var command = connection.CreateCommand())
                {
                    PrepareCommand(key, command);
                    using (var record = command.ExecuteResultSet(ResultSetOptions.Updatable))
                    {
                        if (record.Read())
                        {
                            record.Delete();
                        }
                    }
                }
            });
        }

        internal static IObservable<Unit> DeleteAllFromCache(this SqlCeConnection connection)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM CacheElement";
                    command.ExecuteNonQuery();
                }
            });
        }

        internal static IObservable<Unit> DeleteAllFromCache<T>(this SqlCeConnection connection)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM CacheElement WHERE TypeName = @TypeName";
                    command.Parameters.AddWithValue("TypeName", typeof(T).FullName);
                    command.ExecuteNonQuery();
                }
            });
        }

        internal static IObservable<Unit> DeleteExpiredElements(this SqlCeConnection connection, DateTime time)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DELETE FROM CacheElement WHERE Expiration < @expiration";
                    command.Parameters.AddWithValue("expiration", time);
                    command.ExecuteNonQuery();
                }
            });
        }

        // TODO: actually implement this based on notes here: http://technet.microsoft.com/en-us/library/ms172411(v=sql.110).aspx
        internal static IObservable<Unit> Vacuum(this SqlCeConnection connection, DateTime time)
        {
            return Observable.Return(Unit.Default);
        }

        private static void PrepareCommand(string key, SqlCeCommand command)
        {
            command.CommandType = CommandType.TableDirect;
            command.CommandText = "CacheElement";
            command.IndexName = "PK_CacheElement";
            var start = new object[1];
            start[0] = new SqlString(key);
            command.SetRange(DbRangeOptions.Match, start, null);
        }
    }
}
