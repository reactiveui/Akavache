using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using System.Collections.Concurrent;
using System.Net.Http;

#if SILVERLIGHT
using Akavache.Internal;
#endif

#if SILVERLIGHT
using System.Net.Browser;
#endif

namespace Akavache
{

    public class AkavacheHttpMixin : IAkavacheHttpMixin
    {
#if SILVERLIGHT
        static AkavacheHttpMixin()
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
        public IObservable<byte[]> DownloadUrl(IBlobCache This, string url, IDictionary<string, string> headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
        {
            var doFetch = new Func<IObservable<byte[]>>(() => MakeWebRequest(new Uri(url), headers).ToObservable());

            if (fetchAlways)
            {
                return This.GetAndFetchLatest(url, doFetch, absoluteExpiration: absoluteExpiration).TakeLast(1);
            }
            else
            {
                return This.GetOrFetchObject(url, doFetch, absoluteExpiration);
            }
        }

        static async Task<byte[]> MakeWebRequest(Uri uri, IDictionary<string, string> headers = null)
        {
            var client = new HttpClient();
            client.MaxResponseContentBufferSize = 1024 * 1048576;

            if (headers != null)
            {
                foreach (var kvp in headers) headers.Add(kvp);
            }

            var resp = await client.GetAsync(uri);
            var content = await resp.Content.ReadAsByteArrayAsync();

            Console.WriteLine(content.Length);
            return content;
        }
    }
}
