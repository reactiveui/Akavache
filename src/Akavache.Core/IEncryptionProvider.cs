using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public interface IEncryptionProvider
    {
        /// <summary>
        /// Encrypts the block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        IObservable<byte[]> EncryptBlock(byte[] block);

        /// <summary>
        /// Decrypts the block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        IObservable<byte[]> DecryptBlock(byte[] block);
    }
}
