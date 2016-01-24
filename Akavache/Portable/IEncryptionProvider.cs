using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public interface IEncryptionProvider
    {
        IObservable<byte[]> EncryptBlock(byte[] block);

        IObservable<byte[]> DecryptBlock(byte[] block);
    }
}
