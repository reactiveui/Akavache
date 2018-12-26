using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Reflection;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;
using System.Reactive.Windows.Foundation;

namespace Akavache
{
    public class WinRTEncryptionProvider : IEncryptionProvider
    {
        public IObservable<byte[]> EncryptBlock(byte[] block)
        {
            var dpapi = new DataProtectionProvider("LOCAL=user");
            return dpapi.ProtectAsync(block.AsBuffer()).ToObservable().Select(b => b.ToArray());
        }

        public IObservable<byte[]> DecryptBlock(byte[] block)
        {
            // Do not include a protectionDescriptor
            // http://msdn.microsoft.com/en-us/library/windows/apps/windows.security.cryptography.dataprotection.dataprotectionprovider.unprotectasync.aspx
            var dpapi = new DataProtectionProvider();
            return dpapi.UnprotectAsync(block.AsBuffer()).ToObservable().Select(b => b.ToArray());
        }
    }
}