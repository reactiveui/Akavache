using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public class WP8EncryptionProvider : IEncryptionProvider
    {
        public IObservable<byte[]> EncryptBlock(byte[] block)
        {
            return Observable.Return(ProtectedData.Protect(block, null));
        }

        public IObservable<byte[]> DecryptBlock(byte[] block)
        {
            return Observable.Return(ProtectedData.Unprotect(block, null));
        }
    }
}