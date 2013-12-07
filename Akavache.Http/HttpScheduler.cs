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
using ReactiveUI;

#if WINRT
using Windows.Security.Cryptography.Core;
using System.Runtime.InteropServices.WindowsRuntime;
#else
using System.Security.Cryptography;
#endif

namespace Akavache.Http
{
    public class CachingHttpScheduler : IHttpScheduler
    {
        readonly IBlobCache blobCache = null;
        readonly IHttpScheduler innerScheduler = null;

        readonly ConcurrentDictionary<string, IObservable<Tuple<HttpResponseMessage, byte[]>>> inflightDictionary = 
            new ConcurrentDictionary<string, IObservable<Tuple<HttpResponseMessage, byte[]>>>();

        public CachingHttpScheduler(IHttpScheduler innerScheduler = null, IBlobCache blobCache = null)
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
            //   one. (i.e. priority inversion)
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

        public IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority)
        {
            var key = uniqueKeyForRequest(request);
            var cache = default(IObservable<Tuple<HttpResponseMessage, byte[]>>);

            if (inflightDictionary.TryGetValue(key, out cache))
            {
                return cache;
            }

            var ret = innerScheduler.Schedule(request, priority)
                .Finally(() => inflightDictionary.TryRemove(key, out cache))
                .PublishLast()
                .RefCount();

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

        static string uniqueKeyForRequest(HttpRequestMessage request)
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
            return BitConverter.ToString(sha1.ComputeHash(bytes)).Replace("-", "");
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

    public class HttpScheduler : IHttpScheduler
    {
        protected readonly Subject<Unit> cancelAllSignal = new Subject<Unit>();
        protected readonly OperationQueue opQueue;
        protected readonly int priorityBase;
        protected readonly int retryCount;

        protected long? currentMax;
        protected long bytesRead;

        public HttpScheduler(int priorityBase = 100, int retryCount = 3, OperationQueue opQueue = null)
        {
            this.opQueue = opQueue ?? RxApp.DependencyResolver.GetService<OperationQueue>("Akavache.Http") ?? new OperationQueue();
            this.priorityBase = priorityBase; 
            this.retryCount = retryCount;

            ResetLimit();
        }
        
        public HttpClient Client { get; set; }

        public virtual IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority)
        {
            if (currentMax != null && bytesRead >= currentMax.Value)
            {
                return Observable.Throw<Tuple<HttpResponseMessage, byte[]>>(new SpeculationFinishedException("Ran out of bytes"));
            }

            var rq = Observable.Defer(() => Client.SendAsyncObservable(request)
                .Do(x => bytesRead += x.Item2.LongLength));

            if (retryCount > 0) 
            {
                rq = rq.Retry(retryCount);
            }

            var ret = Observable.Create<Tuple<HttpResponseMessage, byte[]>>(subj =>
            {
                var cancel = new AsyncSubject<Unit>();
                var disp = opQueue.EnqueueObservableOperation(priorityBase + priority, null, cancel, () => rq)
                    .Subscribe(subj);

                return Disposable.Create(() => 
                {
                    cancel.OnNext(Unit.Default);    
                    cancel.OnCompleted();
                    disp.Dispose();
                });
            });

            return ret.PublishLast().RefCount().TakeUntil(cancelAllSignal);
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
        }
    }

    public class SpeculationFinishedException : Exception
    {
        public SpeculationFinishedException() { }
        public SpeculationFinishedException(string message) : base(message ) { }
        public SpeculationFinishedException(string message, Exception inner) : base(message, inner) { }
    }
}