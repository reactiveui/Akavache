using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using ReactiveUI;

namespace Akavache
{
    public static class JsonSerializationMixin
    {
        public static void InsertObject<T>(this IBlobCache This, string key, T value, DateTimeOffset? absoluteExpiration = null)
        {
            This.Insert(GetTypePrefixedKey(key, typeof(T)), 
                Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)),
                absoluteExpiration);
        }

        public static IObservable<T> GetObjectAsync<T>(this IBlobCache This, string key, bool noTypePrefix = false)
        {
            return This.GetAsync(noTypePrefix ? key : GetTypePrefixedKey(key, typeof(T)))
                .Select(x => JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(x, 0, x.Length)));
        }

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

        static string GetTypePrefixedKey(string key, Type type)
        {
            return type.FullName + "___" + key;
        }
    }

    public static class HttpMixin
    {
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
        public static IObservable<BitmapImage> LoadImage(this IBlobCache This, string key)
        {
            return This.GetAsync(key)
                .SelectMany(ThrowOnBadImageBuffer)
                .ObserveOn(RxApp.DeferredScheduler)
                .SelectMany(BytesToImage);
        }

        public static IObservable<BitmapImage> LoadImageFromUrl(this IBlobCache This, string url)
        {
            return This.DownloadUrl(url)
                .SelectMany(ThrowOnBadImageBuffer)
                .ObserveOn(RxApp.DeferredScheduler)
                .SelectMany(BytesToImage);
        }

        static IObservable<byte[]> ThrowOnBadImageBuffer(byte[] compressedImage)
        {
            return (compressedImage == null || compressedImage.Length < 64) ?
                Observable.Throw<byte[]>(new Exception("Invalid Image")) :
                Observable.Return(compressedImage);
        }

        static IObservable<BitmapImage> BytesToImage(byte[] compressedImage)
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
        public static void SaveLogin(this ISecureBlobCache This, string user, string password, DateTimeOffset? absoluteExpiration = null)
        {
            This.InsertObject("login", new Tuple<string, string>(user, password), absoluteExpiration);
        }

        public static IObservable<Tuple<string, string>> GetLoginAsync(this ISecureBlobCache This)
        {
            return This.GetObjectAsync<Tuple<string, string>>("login");
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

        public static void SaveLogin(this ISecureBlobCache This, string user, string password, TimeSpan expiration)
        {
            This.SaveLogin(user, password, This.Scheduler.Now + expiration);
        }
    }
}
