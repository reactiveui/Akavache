using System;
using Akavache.Sqlite3;

namespace Akavache.Sqlite3
{
    [Preserve]
    public static class LinkerPreserve
    {
        static LinkerPreserve()
        {
            throw new Exception(typeof(SQLitePersistentBlobCache).FullName);
            throw new Exception(typeof(SqlRawPersistentBlobCache).FullName);
        }
    }

    [System.AttributeUsage(System.AttributeTargets.All)]
    public class PreserveAttribute : Attribute
    {
    }
}
