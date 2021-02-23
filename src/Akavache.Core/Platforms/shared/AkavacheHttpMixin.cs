// Copyright (c) 2021 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace Akavache
{
    /// <summary>
    /// A set of methods associated with accessing HTTP resources for a blob cache.
    /// </summary>
    public class AkavacheHttpMixin : IAkavacheHttpMixin
    {
        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. The URL itself is used as the key.
        /// </summary>
        /// <param name="blobCache">The blob cache associated with the action.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP
        /// request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            return blobCache.DownloadUrl(url, url, headers, fetchAlways, absoluteExpiration);
        }

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. The URL itself is used as the key.
        /// </summary>
        /// <param name="blobCache">The blob cache associated with the action.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP
        /// request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            if (url is null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            return blobCache.DownloadUrl(url.ToString(), url, headers, fetchAlways, absoluteExpiration);
        }

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="blobCache">The blob cache associated with the action.</param>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP
        /// request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, string url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            var doFetch = MakeWebRequest(new Uri(url), headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
            var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

            IObservable<byte[]> ret;
            if (!fetchAlways)
            {
                ret = blobCache.Get(key).Catch(fetchAndCache);
            }
            else
            {
                ret = fetchAndCache;
            }

            var conn = ret.PublishLast();
            conn.Connect();
            return conn;
        }

        /// <summary>
        /// Download data from an HTTP URL and insert the result into the
        /// cache. If the data is already in the cache, this returns
        /// a cached value. An explicit key is provided rather than the URL itself.
        /// </summary>
        /// <param name="blobCache">The blob cache associated with the action.</param>
        /// <param name="key">The key to store with.</param>
        /// <param name="url">The URL to download.</param>
        /// <param name="headers">An optional Dictionary containing the HTTP
        /// request headers.</param>
        /// <param name="fetchAlways">Force a web request to always be issued, skipping the cache.</param>
        /// <param name="absoluteExpiration">An optional expiration date.</param>
        /// <returns>The data downloaded from the URL.</returns>
        public IObservable<byte[]> DownloadUrl(IBlobCache blobCache, string key, Uri url, IDictionary<string, string>? headers = null, bool fetchAlways = false, DateTimeOffset? absoluteExpiration = null)
        {
            if (blobCache is null)
            {
                throw new ArgumentNullException(nameof(blobCache));
            }

            var doFetch = MakeWebRequest(url, headers).SelectMany(x => ProcessWebResponse(x, url, absoluteExpiration));
            var fetchAndCache = doFetch.SelectMany(x => blobCache.Insert(key, x, absoluteExpiration).Select(_ => x));

            IObservable<byte[]> ret;
            if (!fetchAlways)
            {
                ret = blobCache.Get(key).Catch(fetchAndCache);
            }
            else
            {
                ret = fetchAndCache;
            }

            var conn = ret.PublishLast();
            conn.Connect();
            return conn;
        }

        private static IObservable<WebResponse> MakeWebRequest(
            Uri uri,
            IDictionary<string, string>? headers = null,
            string? content = null,
            int retries = 3,
            TimeSpan? timeout = null)
        {
            IObservable<WebResponse> request;

#if !WINDOWS_UWP
            if (ModeDetector.InUnitTestRunner())
            {
                request = Observable.Defer(() =>
                {
                    var hwr = CreateWebRequest(uri, headers);

                    if (content is null)
                    {
                        return Observable.Start(() => hwr.GetResponse(), BlobCache.TaskpoolScheduler);
                    }

                    var buf = Encoding.UTF8.GetBytes(content);
                    return Observable.Start(
                        () =>
                        {
                            hwr.GetRequestStream().Write(buf, 0, buf.Length);
                            return hwr.GetResponse();
                        },
                        BlobCache.TaskpoolScheduler);
                });
            }
            else
#endif
            {
                request = Observable.Defer(() =>
                {
                    var hwr = CreateWebRequest(uri, headers);

                    if (content is null)
                    {
                        return Observable.FromAsync(() => Task.Factory.FromAsync(hwr.BeginGetResponse, hwr.EndGetResponse, hwr));
                    }

                    var buf = Encoding.UTF8.GetBytes(content);

                    // NB: You'd think that BeginGetResponse would never block,
                    // seeing as how it's asynchronous. You'd be wrong :-/
                    var ret = new AsyncSubject<WebResponse>();
                    Observable.Start(
                        () =>
                        {
                            Observable.FromAsync(() => Task.Factory.FromAsync(hwr.BeginGetRequestStream, hwr.EndGetRequestStream, hwr))
                                .SelectMany(x => x.WriteAsyncRx(buf, 0, buf.Length))
                                .SelectMany(_ => Observable.FromAsync(() => Task.Factory.FromAsync(hwr.BeginGetResponse, hwr.EndGetResponse, hwr)))
                                .Multicast(ret).Connect();
                        },
                        BlobCache.TaskpoolScheduler);

                    return ret;
                });
            }

            return request.Timeout(timeout ?? TimeSpan.FromSeconds(15), BlobCache.TaskpoolScheduler).Retry(retries);
        }

        private static WebRequest CreateWebRequest(Uri uri, IDictionary<string, string>? headers)
        {
            var hwr = WebRequest.Create(uri);
            if (headers is not null)
            {
                foreach (var x in headers)
                {
                    hwr.Headers[x.Key] = x.Value;
                }
            }

            return hwr;
        }

        private static IObservable<byte[]> ProcessWebResponse(WebResponse wr, string url, DateTimeOffset? absoluteExpiration)
        {
            if (!(wr is HttpWebResponse hwr))
            {
                throw new ArgumentException("The Web Response is somehow null but shouldn't be: " + url + " with expiry: " + absoluteExpiration, nameof(wr));
            }

            if ((int)hwr.StatusCode >= 400)
            {
                return Observable.Throw<byte[]>(new WebException(hwr.StatusDescription));
            }

            using (var ms = new MemoryStream())
            {
                using (var responseStream = hwr.GetResponseStream())
                {
                    if (responseStream is null)
                    {
                        throw new InvalidOperationException("The response stream is somehow null: " + url + " with expiry: " + absoluteExpiration);
                    }

                    responseStream.CopyTo(ms);
                }

                var ret = ms.ToArray();
                return Observable.Return(ret);
            }
        }

        private static IObservable<byte[]> ProcessWebResponse(WebResponse wr, Uri url, DateTimeOffset? absoluteExpiration)
        {
            if (!(wr is HttpWebResponse hwr))
            {
                throw new ArgumentException("The Web Response is somehow null but shouldn't be: " + url + " with expiry: " + absoluteExpiration, nameof(wr));
            }

            if ((int)hwr.StatusCode >= 400)
            {
                return Observable.Throw<byte[]>(new WebException(hwr.StatusDescription));
            }

            using (var ms = new MemoryStream())
            {
                using (var responseStream = hwr.GetResponseStream())
                {
                    if (responseStream is null)
                    {
                        throw new InvalidOperationException("The response stream is somehow null: " + url + " with expiry: " + absoluteExpiration);
                    }

                    responseStream.CopyTo(ms);
                }

                var ret = ms.ToArray();
                return Observable.Return(ret);
            }
        }
    }
}
