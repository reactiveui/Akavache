using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace Akavache
{
    internal interface IWantsToRegisterStuff
    {
        void Register(Action<Func<object>, Type, string> registerFunction);
    }

    public static class DependencyResolverMixin
    {
        public static void RegisterAkavache(this IMutableDependencyResolver This)
        {
            var namespaces = new[] { 
                "Akavache",
                "Akavache.Mac",
                "Akavache.Mobile",
                "Akavache.Sqlite3",
            };

            var fdr = typeof(DependencyResolverMixin);

            var assmName = new AssemblyName(
                fdr.AssemblyQualifiedName.Replace(fdr.FullName + ", ", ""));

            foreach (var ns in namespaces) 
            {
                var targetType = ns + ".Registrations";
                string fullName = targetType + ", " + assmName.FullName.Replace(assmName.Name, ns);

                var registerTypeClass = Reflection.ReallyFindType(fullName, false);
                if (registerTypeClass == null) return;

                var registerer = (IWantsToRegisterStuff)Activator.CreateInstance(registerTypeClass);
                registerer.Register((f, t, s) => This.Register(f, t, s));
            };
        }
    }
}
