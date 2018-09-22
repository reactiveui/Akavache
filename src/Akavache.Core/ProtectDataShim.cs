using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Akavache
{
    public static class ProtectedData
    {
        /// <summary>
        /// Protects the specified original data.
        /// </summary>
        /// <param name="originalData">The original data.</param>
        /// <param name="entropy">The entropy.</param>
        /// <param name="scope">The scope.</param>
        /// <returns></returns>
        public static byte[] Protect(byte[] originalData, byte[] entropy, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            return originalData;
        }

        /// <summary>
        /// Unprotects the specified original data.
        /// </summary>
        /// <param name="originalData">The original data.</param>
        /// <param name="entropy">The entropy.</param>
        /// <param name="scope">The scope.</param>
        /// <returns></returns>
        public static byte[] Unprotect(byte[] originalData, byte[] entropy, DataProtectionScope scope = DataProtectionScope.CurrentUser)
        {
            return originalData;
        }
    }

    public enum DataProtectionScope
    {
        /// <summary>
        /// The current user
        /// </summary>
        CurrentUser,
    }
}
