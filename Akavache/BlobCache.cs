using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using Newtonsoft.Json;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Akavache
{
    // Ignore this man behind the curtain
    internal interface IWantsToRegisterStuff
    {
        void Register();
    }

    public static class BlobCache
    {
        static IServiceProvider serviceProvider;
        static string applicationName;
        static ISecureBlobCache perSession = new TestBlobCache(Scheduler.Immediate);

        static BlobCache()
        {
            if (RxApp.InUnitTestRunner())
            {
                localMachine = new TestBlobCache(RxApp.TaskpoolScheduler);
                userAccount = new TestBlobCache(RxApp.TaskpoolScheduler);
                secure = new TestBlobCache(RxApp.TaskpoolScheduler);
                return;
            }
            
            var namespaces = AttemptToEarlyLoadAkavacheDLLs();

            foreach(var ns in namespaces) {
                #if WINRT
                var assm = typeof (BlobCache).GetTypeInfo().Assembly;
                #else
                var assm = Assembly.GetExecutingAssembly();
                #endif
                var fullName = typeof(BlobCache).AssemblyQualifiedName;
                var targetType = ns + ".ServiceLocationRegistration";
                fullName = fullName.Replace("Akavache.BlobCache", targetType);
                fullName = fullName.Replace(assm.FullName, assm.FullName.Replace("Akavache", ns));
                
                var registerTypeClass = Reflection.ReallyFindType(fullName, false);
                if (registerTypeClass != null) {
                    var registerer = (IWantsToRegisterStuff) Activator.CreateInstance(registerTypeClass);
                    registerer.Register();
                }
            }
        }

        public static IServiceProvider ServiceProvider
        {
            get { return serviceProvider; }
            set {
                serviceProvider = value;
                JsonObjectConverter = value != null ? new JsonObjectConverter(value) : null;
            }
        }

        internal static JsonObjectConverter JsonObjectConverter { get; private set; }

        /// <summary>
        /// Your application's name. Set this at startup, this defines where
        /// your data will be stored (usually at %AppData%\[ApplicationName])
        /// </summary>
        public static string ApplicationName
        {
            get
            {
                if (applicationName == null)
                {
                    throw new Exception("Make sure to set BlobCache.ApplicationName on startup");
                }
                return applicationName;
            }
            set { applicationName = value; }
        }

        static IBlobCache localMachine;
        static IBlobCache userAccount;

        /// <summary>
        /// The local machine cache. Store data here that is unrelated to the
        /// user account or shouldn't be uploaded to other machines (i.e.
        /// image cache data)
        /// </summary>
        public static IBlobCache LocalMachine
        {
            get { return localMachine ?? PersistentBlobCache.LocalMachine; }
            set { localMachine = value; }
        }

        /// <summary>
        /// The user account cache. Store data here that is associated with
        /// the user; in large organizations, this data will be synced to all
        /// machines via NT Roaming Profiles.
        /// </summary>
        public static IBlobCache UserAccount
        {
            get { return userAccount ?? PersistentBlobCache.UserAccount; }
            set { userAccount = value; }
        }

        static ISecureBlobCache secure;

        /// <summary>
        /// An IBlobCache that is encrypted - store sensitive data in this
        /// cache such as login information.
        /// </summary>
        public static ISecureBlobCache Secure
        {
            get { return secure ?? EncryptedBlobCache.Current; }
            set { secure = value; }
        }

        /// <summary>
        /// An IBlobCache that simply stores data in memory. Data stored in
        /// this cache will be lost when the application restarts.
        /// </summary>
        public static ISecureBlobCache InMemory
        {
            get { return perSession; }
            set { perSession = value; }
        }

        /// <summary>
        /// This method shuts down all of the blob caches. Make sure call it
        /// on app exit and await / Wait() on it!
        /// </summary>
        /// <returns>A Task representing when all caches have finished shutting
        /// down.</returns>
#if WP7
        public static IObservable<Unit> Shutdown()
#else
        public static Task Shutdown()
#endif
        {
            var toDispose = new[] { LocalMachine, UserAccount, Secure, InMemory, };

            var ret = toDispose.Select(x =>
            {
                x.Dispose();
                return x.Shutdown;
            }).Merge().ToList().Select(_ => Unit.Default);

#if WP7
            return ret;
#else
            return ret.ToTask();
#endif
        }

        public static JsonSerializerSettings SerializerSettings { get; set; }

        static IEnumerable<string> AttemptToEarlyLoadAkavacheDLLs()
        {
            var guiLibs = new[] {
                "Akavache.Mac",
                "Akavache.Mobile",
                "Akavache.Sqlite3",
            };
            
            #if WINRT || WP8 || SILVERLIGHT
            // NB: WinRT hates your Freedom
            return new[] {"Akavache.Mobile", "Akavache.Sqlite3", };
            #else
            var name = Assembly.GetExecutingAssembly().GetName();
            var suffix = GetArchSuffixForPath(Assembly.GetExecutingAssembly().Location);
            
            return guiLibs.SelectMany(x => {
                var fullName = String.Format("{0}{1}, Version={2}, Culture=neutral, PublicKeyToken=null", x, suffix, name.Version.ToString());
                
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                if (String.IsNullOrEmpty(assemblyLocation))
                    return Enumerable.Empty<string>();
                
                var path = Path.Combine(Path.GetDirectoryName(assemblyLocation), x + suffix + ".dll");
                if (!File.Exists(path) && !RxApp.InUnitTestRunner()) {
                    LogHost.Default.Debug("Couldn't find {0}", path);
                    return Enumerable.Empty<string>();
                }
                
                try {
                    Assembly.Load(fullName);
                    return new[] {x};
                } catch (Exception ex) {
                    LogHost.Default.DebugException("Couldn't load " + x, ex);
                    return Enumerable.Empty<string>();
                }
            });
            #endif
        }
        
        static string GetArchSuffixForPath(string path)
        {
            var re = new Regex(@"(_[A-Za-z0-9]+)\.");
            var m = re.Match(Path.GetFileName(path));
            return m.Success ? m.Groups[1].Value : "";
        }
    }
}
