using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public class EncryptionProvider : IEncryptionProvider
    {
        /// <summary>
        /// Encrypts the block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IObservable<byte[]> EncryptBlock(byte[] block)
        {
            return Observable.Return(ProtectedData.Protect(block, null, DataProtectionScope.CurrentUser));
        }

        /// <summary>
        /// Decrypts the block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IObservable<byte[]> DecryptBlock(byte[] block)
        {
            return Observable.Return(ProtectedData.Unprotect(block, null, DataProtectionScope.CurrentUser));
        }
    }
}
