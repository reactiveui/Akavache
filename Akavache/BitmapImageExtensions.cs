using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Newtonsoft.Json;
using ReactiveUI;

namespace Akavache
{

    public static class BitmapImageMixin
    {
        /// <summary>
        /// Load a XAML image from the blob cache.
        /// </summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <returns>A Future result representing the bitmap image. This
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public static IObservable<BitmapImage> LoadImage(this IBlobCache This, string key)
        {
            return This.GetAsync(key)
                .SelectMany(ThrowOnBadImageBuffer)
                .SelectMany(BytesToImage)
                .ObserveOn(RxApp.DeferredScheduler);
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the XAML image. 
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. This
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public static IObservable<BitmapImage> LoadImageFromUrl(this IBlobCache This, string url, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
        {
            return This.DownloadUrl(url, null, fetchAlways, absoluteExpiration)
                .SelectMany(ThrowOnBadImageBuffer)
                .SelectMany(BytesToImage);
        }

        public static IObservable<byte[]> ThrowOnBadImageBuffer(byte[] compressedImage)
        {
            return (compressedImage == null || compressedImage.Length < 64) ?
                Observable.Throw<byte[]>(new Exception("Invalid Image")) :
                Observable.Return(compressedImage);
        }

        public static IObservable<BitmapImage> BytesToImage(byte[] compressedImage)
        {
            try
            {
                var ret = new BitmapImage();
#if SILVERLIGHT
                ret.SetSource(new MemoryStream(compressedImage));
#else
                ret.BeginInit();
                ret.StreamSource = new MemoryStream(compressedImage);
                ret.EndInit();
                ret.Freeze();
#endif
                return Observable.Return(ret);
            }
            catch (Exception ex)
            {
                return Observable.Throw<BitmapImage>(ex);
            }
        }
    }
}