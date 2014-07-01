using System.Data;
using System.Data.SqlServerCe;
using System.Threading.Tasks;

namespace Akavache.SqlServerCompact
{
    internal static class Ensure
    {
        public static Task IsOpen(SqlCeConnection connection)
        {
            if (connection.State == ConnectionState.Open)
            {
                return Task.FromResult(0);
            }

            return connection.OpenAsync();
        }
    }
}