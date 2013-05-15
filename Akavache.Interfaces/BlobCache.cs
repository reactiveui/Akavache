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

            // XXX: This is a hella hack
            var mutableRegistration = RxApp.DependencyResolver as IMutableDependencyResolver;
            mutableRegistration.RegisterAkavache();
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
                {
                    throw new Exception("Make sure to set BlobCache.ApplicationName on startup");
                }

                return applicationName;
            }
            set { applicationName = value; }
        }

        static Lazy<IBlobCache> defaultLocalMachineOverride;
        static IBlobCache localMachine;

        static Lazy<IBlobCache> defaultUserAccountOverride;
        static IBlobCache userAccount;

        /// <summary>
        /// The local machine cache. Store data here that is unrelated to the
        /// user account or shouldn't be uploaded to other machines (i.e.
        /// image cache data)
        /// </summary>
        public static IBlobCache LocalMachine
        {
            get { return localMachine ?? 
                (defaultLocalMachineOverride != null ? defaultLocalMachineOverride.Value : null) ??
                PersistentBlobCache.LocalMachine; }
            set { localMachine = value; }
        }

        /// <summary>
        /// The user account cache. Store data here that is associated with
        /// the user; in large organizations, this data will be synced to all
        /// machines via NT Roaming Profiles.
        /// </summary>
        public static IBlobCache UserAccount
        {
            get { return userAccount ?? 
                (defaultUserAccountOverride != null ? defaultUserAccountOverride.Value : null) ??
                PersistentBlobCache.UserAccount; }
            set { userAccount = value; }
        }

        static Lazy<ISecureBlobCache> defaultSecureOverride;
        static ISecureBlobCache secure;

        /// <summary>
        /// An IBlobCache that is encrypted - store sensitive data in this
        /// cache such as login information.
        /// </summary>
        public static ISecureBlobCache Secure
        {
            get { return secure ?? 
                (defaultSecureOverride != null ? defaultSecureOverride.Value : null) ??
                EncryptedBlobCache.Current; }
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
