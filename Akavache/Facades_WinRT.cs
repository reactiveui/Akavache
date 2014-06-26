using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Reflection;
using Windows.Security.Cryptography.DataProtection;
using Windows.Storage;

namespace Akavache
{
    public static class Encryption
    {
        public static async Task<byte[]> EncryptBlock(byte[] block)
        {
            var dpapi = new DataProtectionProvider("LOCAL=user");
            var ret = await dpapi.ProtectAsync(block.AsBuffer());
            return ret.ToArray();
        }

        public static async Task<byte[]> DecryptBlock(byte[] block)
        {
            // Do not include a protectionDescriptor
            // http://msdn.microsoft.com/en-us/library/windows/apps/windows.security.cryptography.dataprotection.dataprotectionprovider.unprotectasync.aspx
            var dpapi = new DataProtectionProvider();
            var ret = await dpapi.UnprotectAsync(block.AsBuffer());
            return ret.ToArray();
        }
    }
}