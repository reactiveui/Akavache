using System;
using System.Linq;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;
using System.Reactive.Disposables;
using Punchclock;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using Splat;
using System.Threading;

#if WINRT
using Windows.Security.Cryptography.Core;
using System.Runtime.InteropServices.WindowsRuntime;
#else
using System.Security.Cryptography;
#endif

namespace Akavache.Http
{
    class HttpCacheEntry
    {
        public string ETag { get; set; }
        public DateTimeOffset? LastModified { get; set; }
        public bool ShouldCheckResponseHeaders { get; set; }
        public HttpStatusCode Code { get; set; }

        public Dictionary<string, List<string>> Headers { get; set; }
        public Dictionary<string, List<string>> ContentHeaders { get; set; }
        public byte[] Data { get; set; }

        public bool UseCachedData(HttpResponseMessage response)
        {
            if (response.Headers.ETag != null && ETag != response.Headers.ETag.Tag)
            {
                return false;
            }

            if (response.Content.Headers.LastModified != null && LastModified != null && response.Content.Headers.LastModified > LastModified)
            {
                return false;
            }

            return true;
        }

        public static HttpCacheEntry FromResponse(HttpResponseMessage message, byte[] data)
        {
            return new HttpCacheEntry() {
                ETag = message.Headers.ETag != null ? message.Headers.ETag.Tag : null,
                LastModified = message.Content.Headers.LastModified,
                Code = message.StatusCode,
                Headers = message.Headers.ToDictionary(x => x.Key, x => x.Value.ToList()),
                ContentHeaders = message.Content.Headers.ToDictionary(x => x.Key, x => x.Value.ToList()),
                Data = data,
            };
        }

        public HttpResponseMessage ToResponse()
        {
            var ret = new HttpResponseMessage(Code)
            {
                Content = new ByteArrayContent(Data),
            };

            foreach (var kvp in Headers) { ret.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value); }
            foreach (var kvp in ContentHeaders) { ret.Content.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value); }
            return ret;
        }
    }

    public class CachingHttpScheduler : ISpeculativeHttpScheduler
    {
        readonly IBlobCache blobCache = null;
        readonly ISpeculativeHttpScheduler innerScheduler = null;

        readonly ConcurrentDictionary<Tuple<string, int>, IObservable<Tuple<HttpResponseMessage, byte[]>>> inflightDictionary = 
            new ConcurrentDictionary<Tuple<string, int>, IObservable<Tuple<HttpResponseMessage, byte[]>>>();

        public CachingHttpScheduler(ISpeculativeHttpScheduler innerScheduler = null, IBlobCache blobCache = null)
        {
            this.blobCache = blobCache;
            this.innerScheduler = innerScheduler ?? new HttpScheduler();
            
            // Things that are interesting:
            // - If-None-Match
            // - If-Modified-Since
            // - If-Range
            // - If-Unmodified-Since
            // - Last-Modified
            // - HEAD
            // - Age
            // - Cache-Control request
            // - Cache-Control response
            // - ETag
            // - ServerDate
            // - Expires
            // - Pragma: no-cache
            // - Retry-After + 503

            // Things that are tricky
            // - High prio rqs that satisfy a low prio pending request, we need
            //   to cancel / dequeue the low prio underlying one and return the high-prio
            //   one. (i.e. priority inversion) (FIXED)
        }

        public HttpClient Client
        {
            get { return innerScheduler.Client; }
            set { innerScheduler.Client = value; }
        }

        protected IBlobCache Cache
        {
            // NB: If we didn't look this up here, it would be impossible to
            // override the cache that Akavache.Http uses since it would be
            // saved off in the static ctor
            get { return blobCache ?? BlobCache.LocalMachine; }
        }

        public IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority, Func<HttpResponseMessage, bool> shouldFetchContent = null)
        {
            var key = Tuple.Create(UniqueKeyForRequest(request), priority);
            var cache = default(IObservable<Tuple<HttpResponseMessage, byte[]>>);
            shouldFetchContent = shouldFetchContent ?? (_ => true);

            if (inflightDictionary.TryGetValue(key, out cache))
            {
                return cache;
            }

            if (request.Method != HttpMethod.Get || 
                (request.Headers.CacheControl != null && request.Headers.CacheControl.NoStore))
            {
                var noCache = Observable.Defer(() => innerScheduler.Schedule(request, priority, x => shouldFetchContent(x)))
                    .Finally(() => inflightDictionary.TryRemove(key, out cache))
                    .PublishLast();

                noCache.Connect();
                return noCache;
            }

            var ret = Cache.GetObjectAsync<HttpCacheEntry>(key.Item1).Catch<HttpCacheEntry>(Observable.Return(default(HttpCacheEntry)))
                .SelectMany(async (cacheEntry, ct) =>
                {
                    var cancelSignal = new AsyncSubject<Unit>();
                    ct.Register(() => { cancelSignal.OnNext(Unit.Default); cancelSignal.OnCompleted(); });

                    if (cacheEntry != null && !cacheEntry.ShouldCheckResponseHeaders)
                    {
                        return Tuple.Create(cacheEntry.ToResponse(), cacheEntry.Data);
                    }

                    var toConcat = Observable.Empty<Tuple<HttpResponseMessage, byte[]>>();
                    var cacheIsValid = false;

                    ct.ThrowIfCancellationRequested();
                    var respWithData = await innerScheduler.Schedule(request, priority, respHeaders =>
                    {
                        if (!shouldFetchContent(respHeaders))
                        {
                            cancelSignal.OnNext(Unit.Default);
                            cancelSignal.OnCompleted();
                            return false;
                        }

                        if (cacheEntry != null && cacheEntry.UseCachedData(respHeaders))
                        {
                            toConcat = Observable.Return(Tuple.Create(cacheEntry.ToResponse(), cacheEntry.Data));
                            cacheIsValid = true;
                            return false;
                        }

                        return true;
                    }).TakeUntil(cancelSignal).Concat(Observable.Defer(() => toConcat));

                    if (cancelSignal.IsCompleted || ct.IsCancellationRequested)
                    {
                        throw new OperationCanceledException();
                    }

                    if (DefinitelyShouldntCache(respWithData.Item1) || cacheIsValid)
                    {
                        cancelSignal.OnNext(Unit.Default);
                        cancelSignal.OnCompleted();
                        return respWithData;
                    }

                    var expiryDate = CacheUntilDate(respWithData.Item1);
                    var entry = HttpCacheEntry.FromResponse(respWithData.Item1, respWithData.Item2);
                    entry.ShouldCheckResponseHeaders = (expiryDate == null);

                    await Cache.InsertObject(key.Item1, entry, expiryDate);
                    return respWithData;
                })
                .Finally(() => inflightDictionary.TryRemove(key, out cache))
                .PublishLast();

            ret.Connect();

            inflightDictionary.TryAdd(key, ret);
            return ret;
        }

        public void ResetLimit(long? maxBytesToRead)
        {
            innerScheduler.ResetLimit(maxBytesToRead);
        }

        public void CancelAll()
        {
            innerScheduler.CancelAll();
        }

        static bool DefinitelyShouldntCache(HttpResponseMessage message)
        {
            if (!message.IsSuccessStatusCode) return true;
            if (message.Headers.CacheControl != null && message.Headers.CacheControl.NoStore) return true;

            return false;
        }

        static DateTimeOffset? CacheUntilDate(HttpResponseMessage message)
        {
            if (message.Headers.CacheControl != null && message.Headers.CacheControl.MaxAge != null) 
            {
                return BlobCache.TaskpoolScheduler.Now + message.Headers.CacheControl.MaxAge;
            }

            if (message.Content.Headers.Expires != null)
            {
                return message.Content.Headers.Expires;
            }

            return null;
        }

        static string UniqueKeyForRequest(HttpRequestMessage request)
        {
            var ret = new[] 
            {
                request.RequestUri.ToString(),
                request.Method.Method,
                request.Headers.Accept.ConcatenateAll(x => x.CharSet + x.MediaType),
                request.Headers.AcceptEncoding.ConcatenateAll(x => x.Value),
                (request.Headers.Referrer ?? new Uri("http://example")).AbsoluteUri,
                request.Headers.UserAgent.ConcatenateAll(x => x.Product.ToString()),
            }.Aggregate(new StringBuilder(), (acc, x) => { acc.AppendLine(x); return acc; });

            if (request.Headers.Authorization != null) 
            {
                ret.AppendLine(request.Headers.Authorization.Parameter + request.Headers.Authorization.Scheme);
            }

#if WINRT
            var sha1 = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1);
            var ms = new MemoryStream();
            sha1.HashData(Encoding.UTF8.GetBytes(ret.ToString()).AsBuffer()).AsStream().CopyTo(ms);
            return BitConverter.ToString(ms.ToArray()).Replace("-", "");
#else
            var sha1 = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(ret.ToString());
            return "HttpSchedulerCache_" + BitConverter.ToString(sha1.ComputeHash(bytes)).Replace("-", "");
#endif
        }
    }

    internal static class ConcatenateMixin
    {
        public static string ConcatenateAll<T>(this IEnumerable<T> This, Func<T, string> selector, char separator = '|')
        {
            return This.Aggregate(new StringBuilder(), (acc, x) => 
            {
                acc.Append(selector(x));
                acc.Append(separator);
                return acc;
            }).ToString();
        }
    }

    public class HttpScheduler : ISpeculativeHttpScheduler
    {
        protected readonly Subject<Unit> cancelAllSignal = new Subject<Unit>();
        protected readonly OperationQueue opQueue;
        protected readonly int priorityBase;
        protected readonly int retryCount;

        protected long? currentMax;
        protected long bytesRead;

        public HttpScheduler(int priorityBase = 100, int retryCount = 3, OperationQueue opQueue = null)
        {
            this.opQueue = opQueue ?? Locator.Current.GetService<OperationQueue>("Akavache.Http") ?? new OperationQueue();
            this.priorityBase = priorityBase; 
            this.retryCount = retryCount;

            ResetLimit();
        }
        
        public HttpClient Client { get; set; }

        public virtual IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority, Func<HttpResponseMessage, bool> shouldFetchContent = null)
        {
            shouldFetchContent = shouldFetchContent ?? (_ => true);

            if (currentMax != null && bytesRead >= currentMax.Value) 
            {
                return Observable.Throw<Tuple<HttpResponseMessage, byte[]>>(new SpeculationFinishedException("Ran out of bytes"));
            }

            var ret = Observable.Create<Tuple<HttpResponseMessage, byte[]>>(async (subj, ct) => 
            {
                var cts = new CancellationTokenSource();
                ct.Register(() => cts.Cancel());

                var resp = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
                try 
                {
                    if (!shouldFetchContent(resp)) 
                    {
                        subj.OnNext(Tuple.Create(resp, default(byte[])));
                        subj.OnCompleted();
                        return;
                    }

                    var ms = new MemoryStream();
                    using (var stream = await resp.Content.ReadAsStreamAsync()) 
                    {
                        await stream.CopyToAsync(ms, 4096, ct);
                    }

                    subj.OnNext(Tuple.Create(resp, ms.ToArray()));
                    subj.OnCompleted();
                } 
                finally 
                {
                    resp.Content.Dispose();
                }
            });

            if (retryCount > 0) ret = ret.Retry(retryCount);

            // NB: We have to do this double-create dance because Punchclock won't
            // unsubscribe to the source if it's already in progress, if nobody is
            // listening to the enqueued observable operation, we have to explicitly
            // signal cancelation via the cancel observable. Weird. Who wrote this
            // crap??!
            return Observable.Create<Tuple<HttpResponseMessage, byte[]>>(subj =>
            {
                var cancel = new AsyncSubject<Unit>();

                return new CompositeDisposable(
                    Disposable.Create(() => { cancel.OnNext(Unit.Default); cancel.OnCompleted(); }),
                    opQueue.EnqueueObservableOperation(priority, null, cancel, () => ret).Subscribe(subj));
            }).TakeUntil(cancelAllSignal).PublishLast().RefCount();
        }

        public void ResetLimit(long? maxBytesToRead = null)
        {
            bytesRead = 0;
            currentMax = maxBytesToRead;
        }

        public void CancelAll()
        {
            // NB: This is intentionally not completed, it is Hot - the current
            // subscribers will be cleared out via the TakeUntil on the request,
            // but new subscribers can just glom on to the signal
            lock (cancelAllSignal) { cancelAllSignal.OnNext(Unit.Default); }
            Client.CancelPendingRequests();
        }
    }

    public class SpeculationFinishedException : Exception
    {
        public SpeculationFinishedException() { }
        public SpeculationFinishedException(string message) : base(message ) { }
        public SpeculationFinishedException(string message, Exception inner) : base(message, inner) { }
    }
}