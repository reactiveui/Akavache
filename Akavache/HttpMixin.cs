using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using ReactiveUI;

#if SILVERLIGHT
using System.Net.Browser;
#endif

namespace Akavache
{
    public static class HttpMixin
    {
#if SILVERLIGHT
        static HttpMixin()
        {
            WebRequest.RegisterPrefix("http://", WebRequestCreator.ClientHttp);
            WebRequest.RegisterPrefix("https://", WebRequestCreator.ClientHttp);
        }
#endif

        static readonly ConcurrentDictionary<string, IObservable<byte[]>> inflightWebRequests = new ConcurrentDictionary<string, IObservable<byte[]>>();

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
            var doFetch = new Func<KeyNotFoundException, IObservable<byte[]>>(_ => inflightWebRequests.GetOrAdd(url, __ => Observable.Defer(() =>
            {
                return MakeWebRequest(new Uri(url), headers)
                    .SelectMany(x => ProcessAndCacheWebResponse(x, url, This, absoluteExpiration));
            }).Multicast(new AsyncSubject<byte[]>()).RefCount()));

            IObservable<byte[]> dontcare;
            var ret = fetchAlways ? doFetch(null) : This.GetAsync(url).Catch(doFetch);
            return ret.Finally(() => inflightWebRequests.TryRemove(url, out dontcare));
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
            IObservable<WebResponse> request;

            var hwr = WebRequest.Create(uri);
            if (headers != null)
            {
                foreach(var x in headers)
                {
                    hwr.Headers[x.Key] = x.Value;
                }
            }

#if !(SILVERLIGHT || NETFX_CORE)
            if (RxApp.InUnitTestRunner()) 
            {
                request = Observable.Defer(() => 
                {
                    if (content == null) 
                    {
                        return Observable.Start(() => hwr.GetResponse(), RxApp.TaskpoolScheduler);
                    }

                    var buf = Encoding.UTF8.GetBytes(content);
                    return Observable.Start(() => 
                    {
                        hwr.GetRequestStream().Write(buf, 0, buf.Length);
                        return hwr.GetResponse();
                    }, RxApp.TaskpoolScheduler);
                });
            }
            else 
#endif
            {
                request = Observable.Defer(() =>
                {
                    if (content == null)
                    {
                        return Observable.FromAsyncPattern<WebResponse>(hwr.BeginGetResponse, hwr.EndGetResponse)();
                    }
            
                    var buf = Encoding.UTF8.GetBytes(content);
                    
                    // NB: You'd think that BeginGetResponse would never block, 
                    // seeing as how it's asynchronous. You'd be wrong :-/
                    var ret = new AsyncSubject<WebResponse>();
                    Observable.Start(() =>
                    {
                        Observable.FromAsyncPattern<Stream>(hwr.BeginGetRequestStream, hwr.EndGetRequestStream)()
#if NETFX_CORE
                            .SelectMany(x => x.WriteAsyncRx(buf, 0, buf.Length))
#else
                            .SelectMany(x => x.WriteAsyncRx(buf, 0, buf.Length))
#endif
                            .SelectMany(_ => Observable.FromAsyncPattern<WebResponse>(hwr.BeginGetResponse, hwr.EndGetResponse)())
                            .Multicast(ret).Connect();
                    }, RxApp.TaskpoolScheduler);

                    return ret;
                });
            }
        
            return request.Timeout(timeout ?? TimeSpan.FromSeconds(15), RxApp.TaskpoolScheduler).Retry(retries);
        }
    }
}