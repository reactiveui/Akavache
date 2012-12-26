using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reactive.Windows.Foundation;
using System.Text;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage;

namespace Akavache
{
    public class SimpleFilesystemProvider : IFilesystemProvider
    {
        public IObservable<Stream> SafeOpenFileAsync(string path, FileMode mode, FileAccess access, FileShare share, IScheduler scheduler)
        {
            return StorageFile.GetFileFromPathAsync(path).ToObservable()
                .SelectMany(x => access == FileAccess.Read ?
                    x.OpenStreamForReadAsync().ToObservable() :
                    x.OpenStreamForWriteAsync().ToObservable());
        }

        public IObservable<Unit> CreateRecursive(string path)
        {
            return createRecursiveHelper(path).ToObservable();
        }

        async Task createRecursiveHelper(string path)
        {
            var paths = path.Split('\\');
            var root = default(string);

            // Find the latest folder that exists
            for (int i = paths.Count(); i > 0; i--)
            {
                var dir = String.Join("\\", paths.Take(i));

                try
                {
                    var sf = await StorageFolder.GetFolderFromPathAsync(dir);
                    root = sf.Path;
                }
                catch (FileNotFoundException ex)
                {
                    continue;
                }
            }

            if (root == null)
            {
                throw new FileNotFoundException("Couldn't find the root directory");
            }

            var folder = await StorageFolder.GetFolderFromPathAsync(root);
            paths = path.Replace(root + "\\", "").Split('\\');

            // Create the ones that don't exist
            foreach (var pathSegment in paths)
            {
                folder = await folder.CreateFolderAsync(pathSegment);
            }
        }

        public IObservable<Unit> Delete(string path)
        {
            return StorageFile.GetFileFromPathAsync(path).ToObservable()
                .SelectMany(x => x.DeleteAsync().ToObservable());
        }
    }
}