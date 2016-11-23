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
using System.Reactive.Threading.Tasks;
using System.Text;
using Splat;

namespace Akavache
{
    public static class BitmapImageMixin
    {
        /// <summary>
        /// Load an image from the blob cache.
        /// </summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <returns>A Future result representing the bitmap image. This
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public static IObservable<IBitmap> LoadImage(this IBlobCache This, string key, float? desiredWidth = null, float? desiredHeight = null)
        {
            return This.Get(key)
                .SelectMany(ThrowOnBadImageBuffer)
                .SelectMany(x => bytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image. 
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. This
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache This, string url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null)
        {
            return This.DownloadUrl(url, null, fetchAlways, absoluteExpiration)
                .SelectMany(ThrowOnBadImageBuffer)
                .SelectMany(x => bytesToImage(x, desiredWidth, desiredHeight));
        }

        /// <summary>
        /// A combination of DownloadUrl and LoadImage, this method fetches an
        /// image from a remote URL (using the cached value if possible) and
        /// returns the image. 
        /// </summary>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <returns>A Future result representing the bitmap image. This
        /// Observable is guaranteed to be returned on the UI thread.</returns>
        public static IObservable<IBitmap> LoadImageFromUrl(this IBlobCache This, string key, string url, bool fetchAlways = false, float? desiredWidth = null, float? desiredHeight = null, DateTimeOffset? absoluteExpiration = null)
        {
            return This.DownloadUrl(key, url, null, fetchAlways, absoluteExpiration)
                .SelectMany(ThrowOnBadImageBuffer)
                .SelectMany(x => bytesToImage(x, desiredWidth, desiredHeight));
        }


        /// <summary>
        /// Converts bad image buffers into an exception
        /// </summary>
        /// <returns>The byte[], or OnError if the buffer is corrupt (empty or 
        /// too small)</returns>
        public static IObservable<byte[]> ThrowOnBadImageBuffer(byte[] compressedImage)
        {
            return (compressedImage == null || compressedImage.Length < 64) ?
                Observable.Throw<byte[]>(new Exception("Invalid Image")) :
                Observable.Return(compressedImage);
        }

        static IObservable<IBitmap> bytesToImage(byte[] compressedImage, float? desiredWidth, float? desiredHeight)
        {
            return BitmapLoader.Current.Load(new MemoryStream(compressedImage), desiredWidth, desiredHeight).ToObservable();
        }
    }
}