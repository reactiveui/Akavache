using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Reactive;
using System.Reactive.Concurrency;

namespace Akavache
{
    public class IsolatedStorageProvider : IFileSystemProvider
    {
        public IObservable<byte[]> ReadFileToBytesAsync(string path, IScheduler scheduler)
        {
            return Utility.Try(() => ReadFileToBytes(path));
        }

        public IObservable<byte[]> WriteBytesToFileAsync(string path, byte[] data, IScheduler scheduler)
        {
            return Utility.Try(() => WriteBytesToFile(path, data));
        }

        public IObservable<Unit> DeleteFileAsync(string path)
        {
            return Utility.Try(() => DeleteFile(path));
        }

        public IObservable<Unit> CreateRecursiveAsync(string path)
        {
            return Utility.Try(() => CreateRecursive(path));
        }

        byte[] ReadFileToBytes(string path)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var fs = IsolatedStorageFile.GetUserStoreForApplication())
                using (var stream = fs.OpenFile(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    stream.CopyTo(memoryStream);
                }
                return memoryStream.ToArray();
            }
        }

        byte[] WriteBytesToFile(string path, byte[] data)
        {
            using (var memoryStream = new MemoryStream(data))
            using (var fs = IsolatedStorageFile.GetUserStoreForApplication())
            using (var stream = fs.OpenFile(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                memoryStream.CopyTo(stream);
            }
            return data;
        }

        void DeleteFile(string path)
        {
            using (var fs = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (!fs.FileExists(path))
                {
                    return;
                }

                try
                {
                    fs.DeleteFile(path);
                }
                catch (FileNotFoundException) { }
            }
        }

        void CreateRecursive(string path)
        {
            using (var fs = IsolatedStorageFile.GetUserStoreForApplication())
            {
                string acc = "";
                foreach (var x in path.Split(Path.DirectorySeparatorChar))
                {
                    var directory = Path.Combine(acc, x);

                    if (directory[directory.Length - 1] == Path.VolumeSeparatorChar)
                    {
                        directory += Path.DirectorySeparatorChar;
                    }

                    if (!fs.DirectoryExists(directory))
                    {
                        fs.CreateDirectory(directory);
                    }

                    acc = directory;
                }
            }

        }
    }
}
