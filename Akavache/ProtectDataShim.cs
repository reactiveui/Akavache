using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Akavache
{
    public static class ProtectedData
    {
        public static byte[] Protect(byte[] originalData, byte[] entropy, DataProtectionScope scope)
        {
            return originalData;
        }

        public static byte[] Unprotect(byte[] originalData, byte[] entropy, DataProtectionScope scope)
        {
            return originalData;
        }
    }

    public enum DataProtectionScope {
        CurrentUser,
    }
}