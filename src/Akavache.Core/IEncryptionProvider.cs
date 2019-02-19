using System;

namespace Akavache
{
    public interface IEncryptionProvider
    {
        IObservable<byte[]> EncryptBlock(byte[] block);

        IObservable<byte[]> DecryptBlock(byte[] block);
    }
}
