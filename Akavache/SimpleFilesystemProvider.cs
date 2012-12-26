using System;
using System.IO;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;

namespace Akavache
{
    public class SimpleFilesystemProvider : IFilesystemProvider
    {
        public IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler)
        {
            return Utility.SafeOpenFileAsync(path, mode, access, share, scheduler).Select(x => (Stream) x);
        }

        public IObservable<Unit> CreateRecursive(string path)
        {
            return Observable.Start(() => Utility.CreateRecursive(new DirectoryInfo(path)), RxApp.TaskpoolScheduler);
        }

        public IObservable<Unit> Delete(string path)
        {
            return Observable.Start(() => File.Delete(path), RxApp.TaskpoolScheduler);
        }
    }
}