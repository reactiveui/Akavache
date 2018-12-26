using System;
using System.Collections.Generic;
using System.Text;

namespace Akavache.Sqlite3
{
    public static class SqlLite
    {
        public static void Start(Action bundleRegistration)
        {
            _ = bundleRegistration ?? throw new ArgumentNullException(nameof(bundleRegistration));
            bundleRegistration();
        }
    }
}
