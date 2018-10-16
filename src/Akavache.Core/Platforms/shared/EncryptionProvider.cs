
using System;
using System.Reactive.Linq;
#if NET_461
using RxCrypt = System.Security.Cryptography;
#else
using RxCrypt = Akavache;
#endif

namespace Akavache
{
        public class EncryptionProvider : IEncryptionProvider
    {
        public IObservable<byte[]> EncryptBlock(byte[] block)
        {
            return Observable.Return(RxCrypt.ProtectedData.Protect(block, null, RxCrypt.DataProtectionScope.CurrentUser));
        }

        public IObservable<byte[]> DecryptBlock(byte[] block)
        {
            return Observable.Return(RxCrypt.ProtectedData.Unprotect(block, null, RxCrypt.DataProtectionScope.CurrentUser));
        }
    }


}
