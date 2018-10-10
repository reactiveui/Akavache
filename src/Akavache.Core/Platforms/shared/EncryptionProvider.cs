
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
#if PORTABLE ||NET_461
        public class EncryptionProvider : IEncryptionProvider
    {
        public IObservable<byte[]> EncryptBlock(byte[] block)
        {
            return Observable.Return(System.Security.Cryptography.ProtectedData.Protect(block, null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
        }

        public IObservable<byte[]> DecryptBlock(byte[] block)
        {
            return Observable.Return(System.Security.Cryptography.ProtectedData.Unprotect(block, null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
        }
    }
#else
    public class EncryptionProvider : IEncryptionProvider
    {
        public IObservable<byte[]> EncryptBlock(byte[] block)
        {
            return Observable.Return(ProtectedData.Protect(block, null, DataProtectionScope.CurrentUser));
        }

        public IObservable<byte[]> DecryptBlock(byte[] block)
        {
            return Observable.Return(ProtectedData.Unprotect(block, null, DataProtectionScope.CurrentUser));
        }
    }
#endif

}
