using System;
using System.IO;
using TheFactory.FileSystem;
using FileAccess = TheFactory.FileSystem.FileAccess;
using FileMode = TheFactory.FileSystem.FileMode;
using FileShare = TheFactory.FileSystem.FileShare;

namespace Akavache.Tests
{
    public class DesktopFileSystem : IFileSystem
    {
        public Stream GetStream(string path, FileMode mode, FileAccess access, FileShare share)
        {
            var fileMode = Map(mode);
            var fileAccess = Map(access);
            var fileShare = Map(share);

            return File.Open(path, fileMode, fileAccess, fileShare);
        }

        static System.IO.FileShare Map(FileShare access)
        {
            var fileShare = default(System.IO.FileShare);

            if (access == FileShare.Read) fileShare = System.IO.FileShare.Read;
            else if (access == FileShare.None) fileShare = System.IO.FileShare.None;

            return fileShare;
        }

        static System.IO.FileAccess Map(FileAccess access)
        {
            var fileAccess = default(System.IO.FileAccess);

            if (access == FileAccess.Read) fileAccess = System.IO.FileAccess.Read;
            else if (access == FileAccess.Write) fileAccess = System.IO.FileAccess.Write;

            return fileAccess;
        }

        static System.IO.FileMode Map(FileMode mode)
        {
            var fileMode = default(System.IO.FileMode);

            if (mode == FileMode.Append) fileMode = System.IO.FileMode.Append;
            else if (mode == FileMode.Create) fileMode = System.IO.FileMode.Create;
            else if (mode == FileMode.Open) fileMode = System.IO.FileMode.Open;
            else if (mode == FileMode.OpenOrCreate) fileMode = System.IO.FileMode.OpenOrCreate;

            return fileMode;
        }

        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public IDisposable FileLock(string path)
        {
            return new FileStream(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Read, System.IO.FileShare.None);
        }

        public void Remove(string path)
        {
            File.Delete(path);
        }

        public void RemoveDirectory(string path)
        {
            Directory.Delete(path);
        }

        public void Move(string fromPath, string toPath)
        {
            File.Move(fromPath, toPath);
        }
    }
}
