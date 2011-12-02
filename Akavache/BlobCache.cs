using System;

namespace Akavache
{
    public static class BlobCache
    {
        static string applicationName;
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
        static ISecureBlobCache secure;

        public static IBlobCache LocalMachine
        {
            get { return localMachine ?? PersistentBlobCache.LocalMachine; }
            set { localMachine = value; }
        }

        public static IBlobCache UserAccount
        {
            get { return userAccount ?? PersistentBlobCache.UserAccount; }
            set { userAccount = value; }
        }

        public static ISecureBlobCache Secure
        {
            get { return secure ?? EncryptedBlobCache.Current; }
            set { secure = value; }
        }
    }
}