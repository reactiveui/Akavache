using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Akavache.SqlServerCompact
{
    internal static class Ensure
    {
        public static Task IsOpen(SqlConnection connection)
        {
            if (connection.State == ConnectionState.Open) return Task.FromResult(0);
            return connection.OpenAsync();
        }
    }
}