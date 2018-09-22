using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Windows.Foundation;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;

namespace Akavache
{
    public class WinRTEncryptionProvider : IEncryptionProvider
    {
        /// <summary>
        /// Encrypts the block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IObservable<byte[]> EncryptBlock(byte[] block)
        {
            var dpapi = new DataProtectionProvider("LOCAL=user");
            return dpapi.ProtectAsync(block.AsBuffer()).ToObservable().Select(b => b.ToArray());
        }

        /// <summary>
        /// Decrypts the block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IObservable<byte[]> DecryptBlock(byte[] block)
        {
            // Do not include a protectionDescriptor http://msdn.microsoft.com/en-us/library/windows/apps/windows.security.cryptography.dataprotection.dataprotectionprovider.unprotectasync.aspx
            var dpapi = new DataProtectionProvider();
            return dpapi.UnprotectAsync(block.AsBuffer()).ToObservable().Select(b => b.ToArray());
        }
    }
}
