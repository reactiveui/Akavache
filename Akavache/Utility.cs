using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using ReactiveUI;

#if WINRT
using System.Reactive.Windows.Foundation;
using Windows.Storage;
#endif

namespace Akavache
{
    static class Utility
    {
        public static string GetMd5Hash(string input)
        {
#if WINRT
            // NB: Technically, we could do this everywhere, but if we did this
            // upgrade, we may return different strings than we used to (i.e. 
            // formatting-wise), which would break old caches.
            return MD5Core.GetHashString(input, Encoding.UTF8);
#else
            using (var md5Hasher = new MD5Managed())
            {
                // Convert the input string to a byte array and compute the hash.
                var data = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sBuilder = new StringBuilder();
                foreach (var item in data)
                {
                    sBuilder.Append(item.ToString("x2"));
                }
                return sBuilder.ToString();
            }
#endif
        }

        public static IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler = null)
        {
            scheduler = scheduler ?? RxApp.TaskpoolScheduler;
            var ret = new AsyncSubject<Stream>();

            Observable.Start(() =>
            {
                try
                {
                    var createModes = new[]
                    {
                        FileMode.Create,
                        FileMode.CreateNew,
                        FileMode.OpenOrCreate,
                    };

#if !WINRT
                    // NB: We do this (even though it's incorrect!) because
                    // throwing lots of 1st chance exceptions makes debugging
                    // obnoxious, as well as a bug in VS where it detects
                    // exceptions caught by Observable.Start as Unhandled.
                    if (!createModes.Contains(mode) && !File.Exists(path))
                    {
                        ret.OnError(new FileNotFoundException());
                        return;
                    }
#endif

#if SILVERLIGHT
                    Observable.Start(() => new FileStream(path, mode, access, share, 4096), scheduler).Select(x => (Stream)x).Subscribe(ret);
#elif MONO
                    Observable.Start (() => 
                    {
                        var ufi = new Mono.Unix.UnixFileInfo (path);
                        return ufi.Open (mode, access);
                    }, scheduler).Cast<Stream>().Subscribe(ret);
#elif WINRT
                    StorageFile.GetFileFromPathAsync(path).ToObservable()
                        .SelectMany(x => x.OpenAsync(access == FileAccess.Read ? FileAccessMode.Read : FileAccessMode.ReadWrite).ToObservable())
                        .Select(x => x.AsStream())
                        .Subscribe(ret);
#else
                    Observable.Start(() => new FileStream(path, mode, access, share, 4096, true), scheduler).Cast<Stream>().Subscribe(ret);
#endif
                }
                catch (Exception ex)
                {
                    ret.OnError(ex);
                }
            }, scheduler);

            return ret;
        }

#if !WINRT
        public static void CreateRecursive(this DirectoryInfo This)
        {
            This.SplitFullPath().Aggregate((parent, dir) =>
            {
                var path = Path.Combine(parent, dir);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                return path;
            });
        }

        public static IEnumerable<string> SplitFullPath(this DirectoryInfo This)
        {
            var root = Path.GetPathRoot(This.FullName);
            var components = new List<string>();
            for (var path = This.FullName; path != root && path != null; path = Path.GetDirectoryName(path))
            {
                var filename = Path.GetFileName(path);
                if (String.IsNullOrEmpty(filename))
                    continue;
                components.Add(filename);
            }
            components.Add(root);
            components.Reverse();
            return components;
        }
#endif

        public static IObservable<T> LogErrors<T>(this IObservable<T> This, string message = null)
        {
            return Observable.Create<T>(subj =>
            {
                return This.Subscribe(subj.OnNext,
                    ex =>
                    {
                        var msg = message ?? "0x" + This.GetHashCode().ToString("x");
                        LogHost.Default.Info("{0} failed with {1}:\n{2}", msg, ex.Message, ex.ToString());
                        subj.OnError(ex);
                    }, subj.OnCompleted);
            });
        }

        public static IObservable<Unit> CopyToAsync(this Stream This, Stream destination, IScheduler scheduler = null)
        {
#if WINRT
            return This.CopyToAsync(destination).ToObservable()
                .Do(x =>
                {
                    try
                    {
                        This.Dispose();
                        destination.Dispose();
                    }
                    catch (Exception ex)
                    {
                        LogHost.Default.WarnException("CopyToAsync failed", ex);
                    }
                });
#endif

            return Observable.Start(() =>
            {
                try
                {
                    This.CopyTo(destination);
                    This.Dispose();
                    destination.Dispose();
                }
                catch(Exception ex)
                {
                    LogHost.Default.WarnException("CopyToAsync failed", ex);
                }
            }, scheduler ?? RxApp.TaskpoolScheduler);

#if FALSE
            var reader = Observable.FromAsyncPattern<byte[], int, int, int>(This.BeginRead, This.EndRead);
            var writer = Observable.FromAsyncPattern<byte[], int, int>(destination.BeginWrite, destination.EndWrite);

            //var bufs = new ThreadLocal<byte[]>(() => new byte[4096]);
            var bufs = new Lazy<byte[]>(() => new byte[4096]);

            var readStream = Observable.Defer(() => reader(bufs.Value, 0, 4096))
                .Repeat()
                .TakeWhile(x => x > 0);

            var ret = readStream
                .Select(x => writer(bufs.Value, 0, x))
                .Concat()
                .Aggregate(Unit.Default, (acc, _) => Unit.Default)
                .Finally(() => { This.Dispose(); destination.Dispose(); })
                .Multicast(new ReplaySubject<Unit>());

            ret.Connect();
            return ret;
#endif
        }

        public static void Retry(this Action block, int retries = 3)
        {
            while (true)
            {
                try
                {
                    block();
                    return;
                }
                catch (Exception)
                {
                    retries--;
                    if (retries == 0)
                    {
                        throw;
                    }
                }
            }
        }

        public static T Retry<T>(this Func<T> block, int retries = 3)
        {
            while (true)
            {
                try
                {
                    T ret = block();
                    return ret;
                }
                catch (Exception)
                {
                    retries--;
                    if (retries == 0)
                    {
                        throw;
                    }
                }
            }
        }
    }
}
