using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using Splat;
using Akavache;

namespace Akavache.Sqlite3
{
    public class Registrations : IWantsToRegisterStuff
    {
        public void Register(IMutableDependencyResolver resolver)
        {
            resolver.RegisterLazySingleton(() => new SQLiteBlobCacheProvider(), typeof(IBlobCacheProvider), null);
        }
    }
}
