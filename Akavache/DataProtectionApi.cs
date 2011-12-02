// http://www.obviex.com/samples/dpapi.aspx
//
using System;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace Akavache
{
    /// <summary>
    /// Encrypts and decrypts data using DPAPI functions.
    /// </summary>
    public class DataProtectionApi
    {
        // Wrapper for DPAPI CryptProtectData function.
        [DllImport("crypt32.dll",
                    SetLastError = true,
                    CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern
            bool CryptProtectData(ref DATA_BLOB pPlainText,
                                        string szDescription,
                                    ref DATA_BLOB pEntropy,
                                        IntPtr pReserved,
                                    ref CRYPTPROTECT_PROMPTSTRUCT pPrompt,
                                        int dwFlags,
                                    ref DATA_BLOB pCipherText);

        // Wrapper for DPAPI CryptUnprotectData function.
        [DllImport("crypt32.dll",
                    SetLastError = true,
                    CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern
            bool CryptUnprotectData(ref DATA_BLOB pCipherText,
                                    ref string pszDescription,
                                    ref DATA_BLOB pEntropy,
                                        IntPtr pReserved,
                                    ref CRYPTPROTECT_PROMPTSTRUCT pPrompt,
                                        int dwFlags,
                                    ref DATA_BLOB pPlainText);

        // BLOB structure used to pass data to DPAPI functions.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct DATA_BLOB
        {
            public int cbData;
            public IntPtr pbData;
        }

        // Prompt structure to be used for required parameters.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct CRYPTPROTECT_PROMPTSTRUCT
        {
            public int cbSize;
            public int dwPromptFlags;
            public IntPtr hwndApp;
            public string szPrompt;
        }

        // Wrapper for the NULL handle or pointer.
        static private IntPtr NullPtr = ((IntPtr)((int)(0)));

        // DPAPI key initialization flags.
        private const int CRYPTPROTECT_UI_FORBIDDEN = 0x1;
        private const int CRYPTPROTECT_LOCAL_MACHINE = 0x4;

        /// <summary>
        /// Initializes empty prompt structure.
        /// </summary>
        /// <param name="ps">
        /// Prompt parameter (which we do not actually need).
        /// </param>
        private static void InitPrompt(ref CRYPTPROTECT_PROMPTSTRUCT ps)
        {
            ps.cbSize = Marshal.SizeOf(
                                      typeof(CRYPTPROTECT_PROMPTSTRUCT));
            ps.dwPromptFlags = 0;
            ps.hwndApp = NullPtr;
            ps.szPrompt = null;
        }

        /// <summary>
        /// Initializes a BLOB structure from a byte array.
        /// </summary>
        /// <param name="data">
        /// Original data in a byte array format.
        /// </param>
        /// <param name="blob">
        /// Returned blob structure.
        /// </param>
        private static void InitBLOB(byte[] data, ref DATA_BLOB blob)
        {
            // Use empty array for null parameter.
            if (data == null)
                data = new byte[0];

            // Allocate memory for the BLOB data.
            blob.pbData = Marshal.AllocHGlobal(data.Length);

            // Make sure that memory allocation was successful.
            if (blob.pbData == IntPtr.Zero)
                throw new Exception(
                    "Unable to allocate data buffer for BLOB structure.");

            // Specify number of bytes in the BLOB.
            blob.cbData = data.Length;

            // Copy data from original source to the BLOB structure.
            Marshal.Copy(data, 0, blob.pbData, data.Length);
        }

        // Flag indicating the type of key. DPAPI terminology refers to
        // key types as user store or machine store.
        public enum KeyType { UserKey = 1, MachineKey };

        // It is reasonable to set default key type to user key.
        private static KeyType defaultKeyType = KeyType.UserKey;

        /// <summary>
        /// Calls DPAPI CryptProtectData function to encrypt a plaintext
        /// string value with a user-specific key. This function does not
        /// specify data description and additional entropy.
        /// </summary>
        /// <param name="plainText">
        /// Plaintext data to be encrypted.
        /// </param>
        /// <returns>
        /// Encrypted value in a base64-encoded format.
        /// </returns>
        public static string Encrypt(string plainText)
        {
            return Encrypt(defaultKeyType, plainText, String.Empty,
                            String.Empty);
        }

        /// <summary>
        /// Calls DPAPI CryptProtectData function to encrypt a plaintext
        /// string value. This function does not specify data description
        /// and additional entropy.
        /// </summary>
        /// <param name="keyType">
        /// Defines type of encryption key to use. When user key is
        /// specified, any application running under the same user account
        /// as the one making this call, will be able to decrypt data.
        /// Machine key will allow any application running on the same
        /// computer where data were encrypted to perform decryption.
        /// Note: If optional entropy is specifed, it will be required
        /// for decryption.
        /// </param>
        /// <param name="plainText">
        /// Plaintext data to be encrypted.
        /// </param>
        /// <returns>
        /// Encrypted value in a base64-encoded format.
        /// </returns>
        public static string Encrypt(KeyType keyType, string plainText)
        {
            return Encrypt(keyType, plainText, String.Empty,
                            String.Empty);
        }

        /// <summary>
        /// Calls DPAPI CryptProtectData function to encrypt a plaintext
        /// string value. This function does not specify data description.
        /// </summary>
        /// <param name="keyType">
        /// Defines type of encryption key to use. When user key is
        /// specified, any application running under the same user account
        /// as the one making this call, will be able to decrypt data.
        /// Machine key will allow any application running on the same
        /// computer where data were encrypted to perform decryption.
        /// Note: If optional entropy is specifed, it will be required
        /// for decryption.
        /// </param>
        /// <param name="plainText">
        /// Plaintext data to be encrypted.
        /// </param>
        /// <param name="entropy">
        /// Optional entropy which - if specified - will be required to
        /// perform decryption.
        /// </param>
        /// <returns>
        /// Encrypted value in a base64-encoded format.
        /// </returns>
        public static string Encrypt(KeyType keyType,
                                     string plainText,
                                     string entropy)
        {
            return Encrypt(keyType, plainText, entropy, String.Empty);
        }

        /// <summary>
        /// Calls DPAPI CryptProtectData function to encrypt a plaintext
        /// string value.
        /// </summary>
        /// <param name="keyType">
        /// Defines type of encryption key to use. When user key is
        /// specified, any application running under the same user account
        /// as the one making this call, will be able to decrypt data.
        /// Machine key will allow any application running on the same
        /// computer where data were encrypted to perform decryption.
        /// Note: If optional entropy is specifed, it will be required
        /// for decryption.
        /// </param>
        /// <param name="plainText">
        /// Plaintext data to be encrypted.
        /// </param>
        /// <param name="entropy">
        /// Optional entropy which - if specified - will be required to
        /// perform decryption.
        /// </param>
        /// <param name="description">
        /// Optional description of data to be encrypted. If this value is
        /// specified, it will be stored along with encrypted data and
        /// returned as a separate value during decryption.
        /// </param>
        /// <returns>
        /// Encrypted value in a base64-encoded format.
        /// </returns>
        public static string Encrypt(KeyType keyType,
                                     string plainText,
                                     string entropy,
                                     string description)
        {
            // Make sure that parameters are valid.
            if (plainText == null) plainText = String.Empty;
            if (entropy == null) entropy = String.Empty;

            // Call encryption routine and convert returned bytes into
            // a base64-encoded value.
            return Convert.ToBase64String(
                    Encrypt(keyType,
                            Encoding.UTF8.GetBytes(plainText),
                            Encoding.UTF8.GetBytes(entropy),
                            description));
        }

        /// <summary>
        /// Calls DPAPI CryptProtectData function to encrypt an array of
        /// plaintext bytes.
        /// </summary>
        /// <param name="keyType">
        /// Defines type of encryption key to use. When user key is
        /// specified, any application running under the same user account
        /// as the one making this call, will be able to decrypt data.
        /// Machine key will allow any application running on the same
        /// computer where data were encrypted to perform decryption.
        /// Note: If optional entropy is specifed, it will be required
        /// for decryption.
        /// </param>
        /// <param name="plainTextBytes">
        /// Plaintext data to be encrypted.
        /// </param>
        /// <param name="entropyBytes">
        /// Optional entropy which - if specified - will be required to
        /// perform decryption.
        /// </param>
        /// <param name="description">
        /// Optional description of data to be encrypted. If this value is
        /// specified, it will be stored along with encrypted data and
        /// returned as a separate value during decryption.
        /// </param>
        /// <returns>
        /// Encrypted value.
        /// </returns>
        public static byte[] Encrypt(KeyType keyType,
                                     byte[] plainTextBytes,
                                     byte[] entropyBytes,
                                     string description)
        {
            // Make sure that parameters are valid.
            if (plainTextBytes == null) plainTextBytes = new byte[0];
            if (entropyBytes == null) entropyBytes = new byte[0];
            if (description == null) description = String.Empty;

            // Create BLOBs to hold data.
            DATA_BLOB plainTextBlob = new DATA_BLOB();
            DATA_BLOB cipherTextBlob = new DATA_BLOB();
            DATA_BLOB entropyBlob = new DATA_BLOB();

            // We only need prompt structure because it is a required
            // parameter.
            CRYPTPROTECT_PROMPTSTRUCT prompt =
                                      new CRYPTPROTECT_PROMPTSTRUCT();
            InitPrompt(ref prompt);

            try
            {
                // Convert plaintext bytes into a BLOB structure.
                try
                {
                    InitBLOB(plainTextBytes, ref plainTextBlob);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "Cannot initialize plaintext BLOB.", ex);
                }

                // Convert entropy bytes into a BLOB structure.
                try
                {
                    InitBLOB(entropyBytes, ref entropyBlob);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "Cannot initialize entropy BLOB.", ex);
                }

                // Disable any types of UI.
                int flags = CRYPTPROTECT_UI_FORBIDDEN;

                // When using machine-specific key, set up machine flag.
                if (keyType == KeyType.MachineKey)
                    flags |= CRYPTPROTECT_LOCAL_MACHINE;

                // Call DPAPI to encrypt data.
                bool success = CryptProtectData(ref plainTextBlob,
                                                    description,
                                                ref entropyBlob,
                                                    IntPtr.Zero,
                                                ref prompt,
                                                    flags,
                                                ref cipherTextBlob);
                // Check the result.
                if (!success)
                {
                    // If operation failed, retrieve last Win32 error.
                    int errCode = Marshal.GetLastWin32Error();

                    // Win32Exception will contain error message corresponding
                    // to the Windows error code.
                    throw new Exception(
                        "CryptProtectData failed.", new Win32Exception(errCode));
                }

                // Allocate memory to hold ciphertext.
                byte[] cipherTextBytes = new byte[cipherTextBlob.cbData];

                // Copy ciphertext from the BLOB to a byte array.
                Marshal.Copy(cipherTextBlob.pbData,
                                cipherTextBytes,
                                0,
                                cipherTextBlob.cbData);

                // Return the result.
                return cipherTextBytes;
            }
            catch (Exception ex)
            {
                throw new Exception("DPAPI was unable to encrypt data.", ex);
            }
            // Free all memory allocated for BLOBs.
            finally
            {
                if (plainTextBlob.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(plainTextBlob.pbData);

                if (cipherTextBlob.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(cipherTextBlob.pbData);

                if (entropyBlob.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(entropyBlob.pbData);
            }
        }

        /// <summary>
        /// Calls DPAPI CryptUnprotectData to decrypt ciphertext bytes.
        /// This function does not use additional entropy and does not
        /// return data description.
        /// </summary>
        /// <param name="cipherText">
        /// Encrypted data formatted as a base64-encoded string.
        /// </param>
        /// <returns>
        /// Decrypted data returned as a UTF-8 string.
        /// </returns>
        /// <remarks>
        /// When decrypting data, it is not necessary to specify which
        /// type of encryption key to use: user-specific or
        /// machine-specific; DPAPI will figure it out by looking at
        /// the signature of encrypted data.
        /// </remarks>
        public static string Decrypt(string cipherText)
        {
            string description;

            return Decrypt(cipherText, String.Empty, out description);
        }

        /// <summary>
        /// Calls DPAPI CryptUnprotectData to decrypt ciphertext bytes.
        /// This function does not use additional entropy.
        /// </summary>
        /// <param name="cipherText">
        /// Encrypted data formatted as a base64-encoded string.
        /// </param>
        /// <param name="description">
        /// Returned description of data specified during encryption.
        /// </param>
        /// <returns>
        /// Decrypted data returned as a UTF-8 string.
        /// </returns>
        /// <remarks>
        /// When decrypting data, it is not necessary to specify which
        /// type of encryption key to use: user-specific or
        /// machine-specific; DPAPI will figure it out by looking at
        /// the signature of encrypted data.
        /// </remarks>
        public static string Decrypt(string cipherText,
                                     out string description)
        {
            return Decrypt(cipherText, String.Empty, out description);
        }

        /// <summary>
        /// Calls DPAPI CryptUnprotectData to decrypt ciphertext bytes.
        /// </summary>
        /// <param name="cipherText">
        /// Encrypted data formatted as a base64-encoded string.
        /// </param>
        /// <param name="entropy">
        /// Optional entropy, which is required if it was specified during
        /// encryption.
        /// </param>
        /// <param name="description">
        /// Returned description of data specified during encryption.
        /// </param>
        /// <returns>
        /// Decrypted data returned as a UTF-8 string.
        /// </returns>
        /// <remarks>
        /// When decrypting data, it is not necessary to specify which
        /// type of encryption key to use: user-specific or
        /// machine-specific; DPAPI will figure it out by looking at
        /// the signature of encrypted data.
        /// </remarks>
        public static string Decrypt(string cipherText,
                                         string entropy,
                                     out string description)
        {
            // Make sure that parameters are valid.
            if (entropy == null) entropy = String.Empty;

            return Encoding.UTF8.GetString(
                        Decrypt(Convert.FromBase64String(cipherText),
                                    Encoding.UTF8.GetBytes(entropy),
                                out description));
        }

        /// <summary>
        /// Calls DPAPI CryptUnprotectData to decrypt ciphertext bytes.
        /// </summary>
        /// <param name="cipherTextBytes">
        /// Encrypted data.
        /// </param>
        /// <param name="entropyBytes">
        /// Optional entropy, which is required if it was specified during
        /// encryption.
        /// </param>
        /// <param name="description">
        /// Returned description of data specified during encryption.
        /// </param>
        /// <returns>
        /// Decrypted data bytes.
        /// </returns>
        /// <remarks>
        /// When decrypting data, it is not necessary to specify which
        /// type of encryption key to use: user-specific or
        /// machine-specific; DPAPI will figure it out by looking at
        /// the signature of encrypted data.
        /// </remarks>
        public static byte[] Decrypt(byte[] cipherTextBytes,
                                         byte[] entropyBytes,
                                     out string description)
        {
            // Create BLOBs to hold data.
            DATA_BLOB plainTextBlob = new DATA_BLOB();
            DATA_BLOB cipherTextBlob = new DATA_BLOB();
            DATA_BLOB entropyBlob = new DATA_BLOB();

            // We only need prompt structure because it is a required
            // parameter.
            CRYPTPROTECT_PROMPTSTRUCT prompt =
                                      new CRYPTPROTECT_PROMPTSTRUCT();
            InitPrompt(ref prompt);

            // Initialize description string.
            description = String.Empty;

            try
            {
                // Convert ciphertext bytes into a BLOB structure.
                try
                {
                    InitBLOB(cipherTextBytes, ref cipherTextBlob);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "Cannot initialize ciphertext BLOB.", ex);
                }

                // Convert entropy bytes into a BLOB structure.
                try
                {
                    InitBLOB(entropyBytes, ref entropyBlob);
                }
                catch (Exception ex)
                {
                    throw new Exception(
                        "Cannot initialize entropy BLOB.", ex);
                }

                // Disable any types of UI. CryptUnprotectData does not
                // mention CRYPTPROTECT_LOCAL_MACHINE flag in the list of
                // supported flags so we will not set it up.
                int flags = CRYPTPROTECT_UI_FORBIDDEN;

                // Call DPAPI to decrypt data.
                bool success = CryptUnprotectData(ref cipherTextBlob,
                                                  ref description,
                                                  ref entropyBlob,
                                                      IntPtr.Zero,
                                                  ref prompt,
                                                      flags,
                                                  ref plainTextBlob);

                // Check the result.
                if (!success)
                {
                    // If operation failed, retrieve last Win32 error.
                    int errCode = Marshal.GetLastWin32Error();

                    // Win32Exception will contain error message corresponding
                    // to the Windows error code.
                    throw new Exception(
                        "CryptUnprotectData failed.", new Win32Exception(errCode));
                }

                // Allocate memory to hold plaintext.
                byte[] plainTextBytes = new byte[plainTextBlob.cbData];

                // Copy ciphertext from the BLOB to a byte array.
                Marshal.Copy(plainTextBlob.pbData,
                             plainTextBytes,
                             0,
                             plainTextBlob.cbData);

                // Return the result.
                return plainTextBytes;
            }
            catch (Exception ex)
            {
                throw new Exception("DPAPI was unable to decrypt data.", ex);
            }
            // Free all memory allocated for BLOBs.
            finally
            {
                if (plainTextBlob.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(plainTextBlob.pbData);

                if (cipherTextBlob.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(cipherTextBlob.pbData);

                if (entropyBlob.pbData != IntPtr.Zero)
                    Marshal.FreeHGlobal(entropyBlob.pbData);
            }
        }
    }

}

///// <summary>
///// Demonstrates the use of DPAPI functions to encrypt and decrypt data.
///// </summary>
//public class DPAPITest
//{
//    /// <summary>
//    /// The main entry point for the application.
//    /// </summary>
//    [STAThread]
//    static void Main(string[] args)
//    {
//        try
//        {
//            string text = "Hello, world!";
//            string entropy = null;
//            string description;

//            Console.WriteLine("Plaintext: {0}\r\n", text);

//            // Call DPAPI to encrypt data with user-specific key.
//            string encrypted = DPAPI.Encrypt(DPAPI.KeyType.UserKey,
//                                              text,
//                                              entropy,
//                                              "My Data");
//            Console.WriteLine("Encrypted: {0}\r\n", encrypted);

//            // Call DPAPI to decrypt data.
//            string decrypted = DPAPI.Decrypt(encrypted,
//                                                entropy,
//                                            out description);
//            Console.WriteLine("Decrypted: {0} <<<{1}>>>\r\n",
//                               decrypted, description);
//        }
//        catch (Exception ex)
//        {
//            while (ex != null)
//            {
//                Console.WriteLine(ex.Message);
//                ex = ex.InnerException;
//            }
//        }
//    }
//}
