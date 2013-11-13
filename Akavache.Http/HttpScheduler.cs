using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;
using System.Reactive.Disposables;
using Punchclock;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Akavache.Http
{
    public class CachingHttpScheduler : IHttpScheduler
    {
        readonly IBlobCache blobCache = null;
        readonly IHttpScheduler innerScheduler = null;
        
        public CachingHttpScheduler(IBlobCache blobCache = null, IHttpScheduler innerScheduler = null)
        {
            this.blobCache = blobCache ?? BlobCache.LocalMachine;
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

        public HttpClient Client { get; set; }

        public IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority)
        {
            throw new NotImplementedException();
        }

        static string uniqueKeyForRequest(HttpRequestMessage request)
        {
            var ret = new[] 
            {
                request.RequestUri.AbsoluteUri,
                request.Method.Method,
                request.Headers.Accept.ConcatenateAll(x => x.CharSet + x.MediaType),
                request.Headers.AcceptEncoding.ConcatenateAll(x => x.Value),
                (request.Headers.Referrer ?? new Uri("http://example")).AbsolutePath,
                request.Headers.UserAgent.ConcatenateAll(x => x.Product.ToString()),
            }.Aggregate(new StringBuilder(), (acc, x) => { acc.AppendLine(x); return acc; });

            if (request.Headers.Authorization != null) 
            {
                ret.AppendLine(request.Headers.Authorization.Parameter + request.Headers.Authorization.Scheme);
            }

            var sha1 = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(ret.ToString());
            return BitConverter.ToString(sha1.ComputeHash(bytes)).Replace("-", "");
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
        protected readonly OperationQueue opQueue;
        protected readonly int priorityBase;
        protected readonly int retryCount;

        public HttpScheduler(OperationQueue opQueue = null, int priorityBase = 100, int retryCount = 3)
        {
            this.opQueue = opQueue ?? new OperationQueue(); 
            this.priorityBase = priorityBase; 
            this.retryCount = retryCount;
        }
        
        public HttpClient Client { get; set; }

        public virtual IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority)
        {
            var rq = Observable.Defer(() => Client.SendAsyncObservable(request));
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

            return ret.PublishLast().RefCount();
        }
    }
}