using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using ReactiveUI;

#if SILVERLIGHT
using System.Net.Browser;
#endif

namespace Akavache
{
    public static class JsonSerializationMixin
    {
        /// <summary>
        /// Insert an object into the cache, via the JSON serializer.
        /// </summary>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        public static void InsertObject<T>(this IBlobCache This, string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            This.Insert(GetTypePrefixedKey(key, typeof(T)), 
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)),
                absoluteExpiration);
        }

        /// <summary>
        /// Get an object from the cache and deserialize it via the JSON
        /// serializer.
        /// </summary>
        /// <param name="key">The key to look up in the cache.</param>
        /// <param name="noTypePrefix">Use the exact key name instead of a
        /// modified key name. If this is true, GetAllObjects will not find this object.</param>
        /// <returns>A Future result representing the object in the cache.</returns>
        public static IObservable<T> GetObjectAsync<T>(this IBlobCache This, string key, bool noTypePrefix = false)
        {
            return This.GetAsync(noTypePrefix ? key : GetTypePrefixedKey(key, typeof(T)))
                .Select(x => JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(x, 0, x.Length)));
        }

        /// <summary>
        /// Return all objects of a specific Type in the cache.
        /// </summary>
        /// <returns>A Future result representing all objects in the cache
        /// with the specified Type.</returns>
        public static IObservable<IEnumerable<T>> GetAllObjects<T>(this IBlobCache This)
        {
            // NB: This isn't exactly thread-safe, but it's Close Enough(tm)
            // We make up for the fact that the keys could get kicked out
            // from under us via the Catch below
            var matchingKeys = This.GetAllKeys()
                .Where(x => x.StartsWith(GetTypePrefixedKey("", typeof(T))))
                .ToArray();

            return matchingKeys.ToObservable()
                .SelectMany(x => This.GetObjectAsync<T>(x, true).Catch(Observable.Empty<T>()))
                .ToList();
        }

        /// <summary>
        /// Attempt to return an object from the cache. If the item doesn't
        /// exist or returns an error, call a Func to return the latest
        /// version of an object and insert the result in the cache.
        ///
        /// For most Internet applications, this method is the best method to
        /// call to fetch static data (i.e. images) from the network.
        /// </summary>
        /// <param name="key">The key to associate with the object.</param>
        /// <param name="fetchFunc">A Func which will asynchronously return
        /// the latest value for the object should the cache not contain the
        /// key. 
        ///
        /// Observable.Start is the most straightforward way (though not the
        /// most efficient!) to implement this Func.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>A Future result representing the deserialized object from
        /// the cache.</returns>
        public static IObservable<T> GetOrFetchObject<T>(this IBlobCache This, string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
        {
            return This.GetObjectAsync<T>(key).Catch<T, KeyNotFoundException>(_ =>
            {
                return fetchFunc()
                    .Multicast(new AsyncSubject<T>()).RefCount()
                    .Do(x => This.InsertObject(key, x, absoluteExpiration));
            });
        }

        /// <summary>
        /// This method attempts to returned a cached value, while
        /// simultaneously calling a Func to return the latest value. When the
        /// latest data comes back, it replaces what was previously in the
        /// cache.
        ///
        /// This method is best suited for loading dynamic data from the
        /// Internet, while still showing the user earlier data.
        ///
        /// This method returns an IObservable that may return *two* results
        /// (first the cached data, then the latest data). Therefore, it's
        /// important for UI applications that in your Subscribe method, you
        /// write the code to merge the second result when it comes in.
        /// </summary>
        /// <param name="key">The key to store the returned result under.</param>
        /// <param name="fetchFunc"></param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>An Observable stream containing either one or two
        /// results (possibly a cached version, then the latest version)</returns>
        public static IObservable<T> GetAndFetchLatest<T>(this IBlobCache This, string key, Func<IObservable<T>> fetchFunc, DateTimeOffset? absoluteExpiration = null)
        {
            var fail = Observable.Defer(fetchFunc)
                .Finally(() => This.Invalidate(key))
                .Do(x => This.InsertObject(key, x, absoluteExpiration));

            var result = This.GetObjectAsync<T>(key).Select(x => new Tuple<T, bool>(x, true))
                .Catch(Observable.Return(new Tuple<T, bool>(default(T), false)));

            return result.SelectMany(x =>
            {
                return x.Item2 ?
                    Observable.Return(x.Item1) :
                    Observable.Empty<T>();
            }).Concat(fail).Multicast(new AsyncSubject<T>()).RefCount();
        }

        static string GetTypePrefixedKey(string key, Type type)
        {
            return type.FullName + "___" + key;
        }
    }

    public static class HttpMixin
    {
#if SILVERLIGHT
        static HttpMixin()
        {
            WebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
            WebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);
        }
#endif

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. The URL itself is used as the key.
        /// </summary>
        /// <param name="url">The URL to download.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP
        /// request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public static IObservable<byte[]> DownloadUrl(this IBlobCache This, string url, Dictionary<string, string> headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
        {
            var fail = Observable.Defer(() =>
            {
                return MakeWebRequest(new Uri(url), headers)
                    .SelectMany(x => ProcessAndCacheWebResponse(x, url, This, absoluteExpiration));
            });

            return (fetchAlways ? fail : This.GetAsync(url).Catch<byte[], KeyNotFoundException>(_ => fail));
        }

        static IObservable<byte[]> ProcessAndCacheWebResponse(WebResponse wr, string url, IBlobCache cache, DateTimeOffset? absoluteExpiration)
        {
            var hwr = (HttpWebResponse) wr;
            if ((int)hwr.StatusCode >= 400)
            {
                return Observable.Throw<byte[]>(new WebException(hwr.StatusDescription));
            }

            var ms = new MemoryStream();
            hwr.GetResponseStream().CopyTo(ms);

            var ret = ms.ToArray();
            cache.Insert(url, ret, absoluteExpiration);
            return Observable.Return(ret);
        }

        static IObservable<WebResponse> MakeWebRequest(
            Uri uri, 
            Dictionary<string, string> headers = null, 
            string content = null,
            int retries = 3,
            TimeSpan? timeout = null)
        {
            var request = Observable.Defer(() =>
            {
                var hwr = WebRequest.Create(uri);
                if (headers != null)
                {
                    foreach(var x in headers)
                    {
                        hwr.Headers[x.Key] = x.Value;
                    }
                }
        
                if (content == null)
                {
                    return Observable.FromAsyncPattern<WebResponse>(hwr.BeginGetResponse, hwr.EndGetResponse)();
                }
        
                var buf = Encoding.UTF8.GetBytes(content);
                return Observable.FromAsyncPattern<Stream>(hwr.BeginGetRequestStream, hwr.EndGetRequestStream)()
                    .SelectMany(x => Observable.FromAsyncPattern<byte[], int, int>(x.BeginWrite, x.EndWrite)(buf, 0, buf.Length))
                    .SelectMany(_ => Observable.FromAsyncPattern<WebResponse>(hwr.BeginGetResponse, hwr.EndGetResponse)());
            });
        
            return request.Timeout(timeout ?? TimeSpan.FromSeconds(15)).Retry(retries);
        }
    }

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
        public static IObservable<BitmapImage> LoadImageFromUrl(this IBlobCache This, string url)
        {
            return This.DownloadUrl(url)
                .SelectMany(ThrowOnBadImageBuffer)
                .SelectMany(BytesToImage)
                .ObserveOn(RxApp.DeferredScheduler);
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

    public static class LoginMixin
    {
        /// <summary>
        /// Save a user/password combination in a secure blob cache. Note that
        /// this method only allows exactly *one* user/pass combo to be saved,
        /// calling this more than once will overwrite the previous entry.
        /// </summary>
        /// <param name="user">The user name to save.</param>
        /// <param name="password">The associated password</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        public static void SaveLogin(this ISecureBlobCache This, string user, string password, string host = "default", DateTimeOffset? absoluteExpiration = null)
        {
            This.InsertObject("login:" + host, new Tuple<string, string>(user, password), absoluteExpiration);
        }

        /// <summary>
        /// Returns the currently cached user/password. If the cache does not
        /// contain a user/password, this returns an Observable which
        /// OnError's with KeyNotFoundException.
        /// </summary>
        /// <returns>A Future result representing the user/password Tuple.</returns>
        public static IObservable<Tuple<string, string>> GetLoginAsync(this ISecureBlobCache This, string host = "default")
        {
            return This.GetObjectAsync<Tuple<string, string>>("login:" + host);
        }
    }

    public static class RelativeTimeMixin
    {
        public static void Insert(this IBlobCache This, string key, byte[] data, TimeSpan expiration)
        {
            This.Insert(key, data, This.Scheduler.Now + expiration);
        }

        public static void InsertObject<T>(this IBlobCache This, string key, T value, TimeSpan expiration)
        {
            This.InsertObject(key, value, This.Scheduler.Now + expiration);
        }

        public static IObservable<byte[]> DownloadUrl(this IBlobCache This, string url, TimeSpan expiration, Dictionary<string, string> headers = null, bool fetchAlways = false)
        {
            return This.DownloadUrl(url, headers, fetchAlways, This.Scheduler.Now + expiration);
        }

        public static void SaveLogin(this ISecureBlobCache This, string user, string password, string host, TimeSpan expiration)
        {
            This.SaveLogin(user, password, host, This.Scheduler.Now + expiration);
        }
    }
}
