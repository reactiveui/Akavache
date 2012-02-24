using System;
using ReactiveUI;

namespace Akavache
{
    public static class BlobCache
    {
        static string applicationName;

        static BlobCache()
        {
            if (RxApp.InUnitTestRunner())
            {
                localMachine = new TestBlobCache(RxApp.TaskpoolScheduler);
                userAccount = new TestBlobCache(RxApp.TaskpoolScheduler);
                secure = new TestBlobCache(RxApp.TaskpoolScheduler);
            }
        }

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        public static IBlobCache LocalMachine
        {
            get { return localMachine ?? PersistentBlobCache.LocalMachine; }
            set { localMachine = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static IBlobCache UserAccount
        {
            get { return userAccount ?? PersistentBlobCache.UserAccount; }
            set { userAccount = value; }
        }

#if !SILVERLIGHT
        static ISecureBlobCache secure;

        /// <summary>
        /// 
        /// </summary>
        public static ISecureBlobCache Secure
        {
            get { return secure ?? EncryptedBlobCache.Current; }
            set { secure = value; }
        }
#endif
    }
}