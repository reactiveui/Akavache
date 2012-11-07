namespace Akavache
{
    public static class ProtectedData
    {
        public static byte[] Protect(byte[] originalData, byte[] entropy)
        {
            return originalData;
        }

        public static byte[] Unprotect(byte[] originalData, byte[] entropy)
        {
            return originalData;
        }
    }
}