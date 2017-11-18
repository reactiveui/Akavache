using System;
using Akavache.Sqlite3;
using Akavache.Core;

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
}
