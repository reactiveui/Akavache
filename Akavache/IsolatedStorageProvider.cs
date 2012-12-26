using System;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;

namespace Akavache
{
    public class IsolatedStorageProvider : IFilesystemProvider
    {
        public IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler)
        {
            return Observable.Create<Stream>(subj =>
            {
                var disp = new CompositeDisposable();
                IsolatedStorageFile fs = null;
                try
                {
                    fs = IsolatedStorageFile.GetUserStoreForApplication();
                    disp.Add(fs);
                    disp.Add(Observable.Start(() => fs.OpenFile(path, mode, access, share), RxApp.TaskpoolScheduler).Select(x => (Stream)x).Subscribe(subj));
                }
                catch(Exception ex)
                {
                    subj.OnError(ex);
                }

                return disp;
            });
        }

        public IObservable<Unit> CreateRecursive(string dirPath)
        {
            return Observable.Start(() => 
            {
                using(var fs = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    string acc = "";
                    foreach(var x in dirPath.Split(Path.DirectorySeparatorChar))
                    {
                        var path = Path.Combine(acc, x);

                        if (path[path.Length - 1] == Path.VolumeSeparatorChar)
                        {
                            path += Path.DirectorySeparatorChar;
                        }


                        if (!fs.DirectoryExists(path))
                        {
                            fs.CreateDirectory(path);
                        }

                        acc = path;
                    }
                }
            }, RxApp.TaskpoolScheduler) ;
        }

        public IObservable<Unit> Delete(string path)
        {
            return Observable.Start(() =>
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
            }, RxApp.TaskpoolScheduler);
        }
    }
}
