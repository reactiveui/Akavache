using System;
using System.Collections.Generic;
using System.Text;
using Splat;
using Akavache.Sqlite3;
using Akavache.Core;

namespace Akavache
{
    [Preserve(AllMembers = true)]
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
