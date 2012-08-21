using System;
using System.Reactive.Concurrency;
using ReactiveUI;

namespace Akavache
{
    public static class BlobCache
    {
        static IObjectFactory objectFactory;
        static string applicationName;
        static ISecureBlobCache perSession = new TestBlobCache(Scheduler.Immediate);

        static BlobCache()
        {
            if (RxApp.InUnitTestRunner())
            {
                localMachine = new TestBlobCache(RxApp.TaskpoolScheduler);
                userAccount = new TestBlobCache(RxApp.TaskpoolScheduler);
                secure = new TestBlobCache(RxApp.TaskpoolScheduler);
            }
        }

        public static IObjectFactory ObjectFactory
        {
            get { return objectFactory; }
            set
            {
                if (value != null)
                {
                    JsonObjectConverter = new JsonObjectConverter(value);
                }
                else
                {
                    JsonObjectConverter = null;
                }
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
    }
}
