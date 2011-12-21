using System;
using System.IO;

namespace Akavache
{
    public class SimpleFilesystemProvider : IFilesystemProvider
    {
        public IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return Utility.SafeOpenFileAsync(path, mode, access, share);
        }

        public void CreateRecursive(string path)
        {
            Utility.CreateRecursive(new DirectoryInfo(path));
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }
    }
}