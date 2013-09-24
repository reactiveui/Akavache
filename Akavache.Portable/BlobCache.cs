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
    public static class BlobCache
    {
        static string applicationName;

        static BlobCache()
        {
            // XXX: Everything is dumb. This is to trick RxUI into running its setup stuff in the ctor.
            LogHost.Default.Debug("Scheduler is {0}, Dep Resolver is {1}", RxApp.TaskpoolScheduler, RxApp.DependencyResolver);

            if (RxApp.DependencyResolver.GetService<IAkavacheHttpMixin>() == null && RxApp.MutableResolver != null)
            {
                RxApp.MutableResolver.InitializeAkavache();
            }
                
            InMemory = new TestBlobCache(RxApp.TaskpoolScheduler);
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
            get { return unitTestLocalMachine ?? localMachine ?? RxApp.DependencyResolver.GetService<IBlobCache>("LocalMachine"); }
            set 
            {
                if (RxApp.InUnitTestRunner())
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
            get { return unitTestUserAccount ?? userAccount ?? RxApp.DependencyResolver.GetService<IBlobCache>("UserAccount"); }
            set {
                if (RxApp.InUnitTestRunner())
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
            get { return unitTestSecure ?? secure ?? RxApp.DependencyResolver.GetService<ISecureBlobCache>(); }
            set 
            {
                if (RxApp.InUnitTestRunner())
                {
                    secure = value;
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
    }
}
