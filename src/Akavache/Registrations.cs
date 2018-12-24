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
            SQLitePCL.Batteries_V2.Init();
        }

        public static void Start(string applicationName)
        {
            Akavache.Sqlite3.Registrations.Start(applicationName, () => SQLitePCL.Batteries_V2.Init());
        }
    }
}
