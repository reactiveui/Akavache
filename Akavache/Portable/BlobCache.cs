using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Threading.Tasks;
using Newtonsoft.Json;
using System.Reactive;
using System.Threading.Tasks;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Splat;

namespace Akavache
{
    public static class BlobCache
    {
        static string applicationName;

        static BlobCache()
        {
            Locator.RegisterResolverCallbackChanged(() => 
            {
                if (Locator.CurrentMutable == null) return;
                Locator.CurrentMutable.InitializeAkavache();
            });
               
            InMemory = new InMemoryBlobCache(Scheduler.Default);
        }

        /// <summary>
        /// Your application's name. Set this at startup, this defines where
        /// your data will be stored (usually at %AppData%\[ApplicationName])
        /// </summary>
        public static string ApplicationName
        {
            get
            {
                if (applicationName == null)
                    throw new Exception("Make sure to set BlobCache.ApplicationName on startup");

                return applicationName;
            }
            set { applicationName = value; }
        }

        static IBlobCache localMachine;
        static IBlobCache userAccount;
        static ISecureBlobCache secure;

        [ThreadStatic] static IBlobCache unitTestLocalMachine;
        [ThreadStatic] static IBlobCache unitTestUserAccount;
        [ThreadStatic] static ISecureBlobCache unitTestSecure;

        /// <summary>
        /// The local machine cache. Store data here that is unrelated to the
        /// user account or shouldn't be uploaded to other machines (i.e.
        /// image cache data)
        /// </summary>
        public static IBlobCache LocalMachine
        {
            get { return unitTestLocalMachine ?? localMachine ?? Locator.Current.GetService<IBlobCache>("LocalMachine"); }
            set 
            {
                if (ModeDetector.InUnitTestRunner())
                {
                    unitTestLocalMachine = value;
                    localMachine = localMachine ?? value;
                }
                else
                {
                    localMachine = value;
                }
            }
        }

        /// <summary>
        /// The user account cache. Store data here that is associated with
        /// the user; in large organizations, this data will be synced to all
        /// machines via NT Roaming Profiles.
        /// </summary>
        public static IBlobCache UserAccount
        {
            get { return unitTestUserAccount ?? userAccount ?? Locator.Current.GetService<IBlobCache>("UserAccount"); }
            set {
                if (ModeDetector.InUnitTestRunner())
                {
                    unitTestUserAccount = value;
                    userAccount = userAccount ?? value;
                }
                else
                {
                    userAccount = value;
                }
            }
        }

        /// <summary>
        /// An IBlobCache that is encrypted - store sensitive data in this
        /// cache such as login information.
        /// </summary>
        public static ISecureBlobCache Secure
        {
            get { return unitTestSecure ?? secure ?? Locator.Current.GetService<ISecureBlobCache>(); }
            set 
            {
                if (ModeDetector.InUnitTestRunner())
                {
                    unitTestSecure = value;
                    secure = secure ?? value;
                }
                else
                {
                    secure = value;
                }
            }
        }

        /// <summary>
        /// An IBlobCache that simply stores data in memory. Data stored in
        /// this cache will be lost when the application restarts.
        /// </summary>
        public static ISecureBlobCache InMemory { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public static void EnsureInitialized()
        {
            // NB: This method doesn't actually do anything, it just ensures 
            // that the static constructor runs
            LogHost.Default.Debug("Initializing Akavache");
        }

        /// <summary>
        /// This method shuts down all of the blob caches. Make sure call it
        /// on app exit and await / Wait() on it!
        /// </summary>
        /// <returns>A Task representing when all caches have finished shutting
        /// down.</returns>
        public static Task Shutdown()
        {
            var toDispose = new[] { LocalMachine, UserAccount, Secure, InMemory, };

            var ret = toDispose.Select(x =>
            {
                x.Dispose();
                return x.Shutdown;
            }).Merge().ToList().Select(_ => Unit.Default);

            return ret.ToTask();
        }

#if PORTABLE
        static IScheduler TaskpoolOverride;
        public static IScheduler TaskpoolScheduler 
        {
            get 
            { 
                var ret = TaskpoolOverride ?? Locator.Current.GetService<IScheduler>("Taskpool"); 
                if (ret == null) 
                {
                    throw new Exception("Can't find a TaskPoolScheduler. You probably accidentally linked to the PCL Akavache in your app.");
                }

                return ret;
            }
            set { TaskpoolOverride = value; }
        }
#else
        static IScheduler TaskpoolOverride;
        public static IScheduler TaskpoolScheduler 
        {
            get { return TaskpoolOverride ?? Locator.Current.GetService<IScheduler>("Taskpool") ?? System.Reactive.Concurrency.TaskPoolScheduler.Default; }
            set { TaskpoolOverride = value; }
        }
#endif
    }
}
