using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Akavache
{
    public static class Encryption
    {
        public static Task<byte[]> EncryptBlock(byte[] block)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            try {
                tcs.TrySetResult(ProtectedData.Protect(block, null, DataProtectionScope.CurrentUser));
            } catch (Exception ex) {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        public static Task<byte[]> DecryptBlock(byte[] block)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            try {
                tcs.TrySetResult(ProtectedData.Unprotect(block, null, DataProtectionScope.CurrentUser));
            } catch (Exception ex) {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }
    }
}