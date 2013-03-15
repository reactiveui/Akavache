using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;

namespace Akavache
{
    public class SimpleFileSystemProvider : IFileSystemProvider
    {
        readonly KeyedMonitor monitor = new KeyedMonitor();
        readonly object gate = new object();

        public IObservable<byte[]> ReadFileToBytesAsync(string path, IScheduler scheduler)
        {
            return monitor.Try(path, () => ReadFileToBytes(path));
        }

        public IObservable<byte[]> WriteBytesToFileAsync(string path, byte[] data, IScheduler scheduler)
        {
            return monitor.Try(path, (() => WriteBytesToFile(path, data)));
        }

        public IObservable<Unit> DeleteFileAsync(string path)
        {
            return monitor.Try(path, (() => DeleteFile(path)));
        }

        public IObservable<Unit> CreateRecursiveAsync(string path)
        {
            return monitor.Try(path, (() => CreateRecursive(path)));
        }

        byte[] ReadFileToBytes(string path)
        {
            lock (gate)
            {
                using (var memoryStream = new MemoryStream())
                {
#if WP7 || WINDOWS_PHONE
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096))
#else
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false))
#endif
                    {
                        stream.CopyTo(memoryStream);
                    }
                    return memoryStream.ToArray();
                }
            }
        }

        byte[] WriteBytesToFile(string path, byte[] data)
        {
            lock (gate)
            {
                using (var memoryStream = new MemoryStream(data))
#if WP7 || WINDOWS_PHONE
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
#else
                using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, false))
#endif

                {
                    memoryStream.CopyTo(stream);
                }
                return data;
            }
        }

        void DeleteFile(string path)
        {
            lock (gate)
            {
                try
                {
                    // NB: No need to check for existence. If it doesn't exist, this does nothing. No exception is thrown
                    //     http://msdn.microsoft.com/en-us/library/system.io.file.delete.aspx
                    File.Delete(path);
                }
                catch (Exception)
                {
                    // Try again but set the file attribute to normal just in case.
                    File.SetAttributes(path, FileAttributes.Normal);
                    File.Delete(path);
                }
            }
        }

        void CreateRecursive(string path)
        {
            // NB:  Directory.CreateDirectory does nothing if the path exists. 
            //      Also, it's recursive by default.
            lock (gate)
            {
                Directory.CreateDirectory(path);
            }
        }

    }
}
