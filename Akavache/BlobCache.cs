using System;

namespace Akavache
{
    public static class BlobCache
    {
        public static IBlobCache LocalMachine { get { return PersistentBlobCache.LocalMachine; } }
        public static IBlobCache UserAccount { get { return PersistentBlobCache.UserAccount; } }
        public static ISecureBlobCache Secure { get { return EncryptedBlobCache.Current; } }
    }
}