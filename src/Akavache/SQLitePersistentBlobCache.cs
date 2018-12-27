using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Text;

namespace Akavache.Sqlite3
{
    /// <summary>
    /// The main purpose of this class is to ensure older packages upgrade without breaking.
    /// Existing installs of Akavache use a linker class referencing typeof(Akavache.Sqlite3.SQLitePersistentBlobCache)
    /// This ensures that static analysis won't link these dlls out
    /// 
    /// This library was added to provide a default bundle implementation using the bundle_e_sqlite3 bundle.
    /// Thus this class was moved here so it provides the hook for the linker and then registers and inits the sqlraw bundle
    /// </summary>
    public class SQLitePersistentBlobCache : SqlRawPersistentBlobCache
    {
        public SQLitePersistentBlobCache(string databaseFile, IScheduler scheduler = null) : base(databaseFile, scheduler)
        {
        }
    }
}
