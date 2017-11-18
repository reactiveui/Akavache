using System;
using System.Collections.Generic;
using System.Text;
using Splat;

namespace Akavache
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(IMutableDependencyResolver resolverToUse)
        {
            Akavache.Sqlite3.SqlLite.Start(DefaultBundle);
        }

        public static void DefaultBundle()
        {
            SQLitePCL.Batteries_V2.Init();
        }
    }
}
