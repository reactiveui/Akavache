using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
#if NETFX_CORE
using Windows.Storage;
#else

#endif

namespace Akavache
{
    public class SimpleFilesystemProvider : IFilesystemProvider
    {
#if NETFX_CORE
        public IObservable<Stream> SafeOpenFileAsync(string path, FileAccessMode access, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, access, scheduler).Select(x => (Stream)x);
        }

        public void CreateRecursive(string path)
        {
            Utility.CreateRecursive(path);
        }

        public async void Delete(string path)
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(path);
            folder.DeleteAsync();
        }
#else
        public IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, mode, access, share, scheduler).Select(x => (Stream) x);
        }
        
        public void CreateRecursive(string path)
        {
            Utility.CreateRecursive(new DirectoryInfo(path));
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }
#endif


    }
}