using System;
using System.Data.SqlServerCe;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Akavache.SqlServerCompact
{
    internal static partial class Extensions
    {
        internal static IObservable<Unit> CreateSchemaInfoTable(this SqlCeConnection connection)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                var command = connection.CreateCommand();
                command.CommandText = "CREATE TABLE SchemaInfo ([Version] int NOT NULL)";
                command.ExecuteNonQuery();
            });
        }

        internal static async Task<int> GetSchemaVersion(this SqlCeConnection connection)
        {
            await Ensure.IsOpen(connection);

            var command = connection.CreateCommand();
            command.CommandText = "SELECT TOP 1 Version from SchemaInfo ORDER BY Version DESC";
            return await command.ExecuteNonQueryAsync();
        }

        internal static IObservable<Unit> InsertSchemaVersion(this SqlCeConnection connection, int version)
        {
            return Observable.StartAsync(async () =>
            {
                await Ensure.IsOpen(connection);

                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO SchemaInfo (Version) VALUES (@Version)";
                command.Parameters.AddWithValue("Version", version);
                await command.ExecuteNonQueryAsync();
            });
        }
    }
}
