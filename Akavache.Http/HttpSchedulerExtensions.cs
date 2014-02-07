using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;
using System.Threading;

namespace Akavache.Http
{
    public static class HttpSchedulerExtensions
    {
        /// <summary>
        /// This method allows you to schedule several requests in a group,
        /// and cancel them all at once if necessary.
        /// </summary>
        /// <param name="block">Use the provided IHttpScheduler to schedule
        /// requests. All requests scheduled will be canceled with the same
        /// IDisposable returned.</param>
        /// <returns>A value that when disposed, cancels all requests made in
        /// the block.</returns>
        public static IDisposable ScheduleAll(this IHttpScheduler This, Action<IHttpScheduler> block)
        {
            var cancel = new AsyncSubject<Unit>();
            block(new CancellationWrapper(This, cancel));

            return Disposable.Create(() =>
            {
                cancel.OnNext(Unit.Default);
                cancel.OnCompleted();
            });
        }

        public static Task<HttpResponseMessage> SendAsync(this IHttpScheduler This, HttpRequestMessage request, CancellationToken ct, int priority = 0)
        {
            return This.Schedule(request, priority, _ => true)
                .Select(x =>
                {
                    var ret = x.Item1;
                    var headers = ret.Content.Headers;
                    ret.Content = new ByteArrayContent(x.Item2);
                    foreach (var kvp in headers) ret.Content.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                    return ret;
                })
                .ToTask(ct);
        }
    }

    class CancellationWrapper : ISpeculativeHttpScheduler
    {
        readonly IHttpScheduler inner;
        readonly IObservable<Unit> cancel;

        public CancellationWrapper(IHttpScheduler inner, IObservable<Unit> cancel)
        {
            this.inner = inner;
            this.cancel = cancel;
        }

        public IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority, Func<HttpResponseMessage, bool> shouldFetchContent)
        {
            return inner.Schedule(request, priority, shouldFetchContent).TakeUntil(cancel);
        }

        public void ResetLimit(long? maxBytesToRead = null)
        {
            var spec = inner as ISpeculativeHttpScheduler;
            if (spec == null) return;

            spec.ResetLimit(maxBytesToRead);
        }

        public void CancelAll()
        {
            inner.CancelAll();
        }

        public HttpClient Client
        {
            get { return inner.Client; }
            set { inner.Client = value; }
        }
    }

    #region Boring HttpClient overloads
    public static class HttpClientSchedulerExtensions
    {
        public static Task<HttpResponseMessage> DeleteAsync(this IHttpScheduler This, string requestUri, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Delete, requestUri), priority);
        }

        public static Task<HttpResponseMessage> DeleteAsync(this IHttpScheduler This, string requestUri, CancellationToken cancellationToken, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Delete, requestUri), cancellationToken, priority);
        }

        public static Task<HttpResponseMessage> DeleteAsync(this IHttpScheduler This, Uri requestUri, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Delete, requestUri), priority);
        }

        public static Task<HttpResponseMessage> DeleteAsync(this IHttpScheduler This, Uri requestUri, CancellationToken cancellationToken, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Delete, requestUri), cancellationToken, priority);
        }

        public static Task<HttpResponseMessage> GetAsync(this IHttpScheduler This, string requestUri, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Get, requestUri), priority);
        }

        public static Task<HttpResponseMessage> GetAsync(this IHttpScheduler This, string requestUri, CancellationToken cancellationToken, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Get, requestUri), priority);
        }

        public static Task<HttpResponseMessage> GetAsync(this IHttpScheduler This, Uri requestUri, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Get, requestUri), priority);
        }

        public static Task<HttpResponseMessage> GetAsync(this IHttpScheduler This, Uri requestUri, CancellationToken cancellationToken, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Get, requestUri), cancellationToken, priority);
        }

        public static Task<HttpResponseMessage> PostAsync(this IHttpScheduler This, string requestUri, HttpContent content, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Post, requestUri) { Content = content }, priority);
        }

        public static Task<HttpResponseMessage> PostAsync(this IHttpScheduler This, string requestUri, HttpContent content, CancellationToken cancellationToken, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Post, requestUri) { Content = content }, cancellationToken, priority);
        }

        public static Task<HttpResponseMessage> PostAsync(this IHttpScheduler This, Uri requestUri, HttpContent content, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Post, requestUri) { Content = content }, priority);
        }

        public static Task<HttpResponseMessage> PostAsync(this IHttpScheduler This, Uri requestUri, HttpContent content, CancellationToken cancellationToken, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Post, requestUri) { Content = content }, cancellationToken, priority);
        }

        public static Task<HttpResponseMessage> PutAsync(this IHttpScheduler This, Uri requestUri, HttpContent content, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Put, requestUri) { Content = content }, priority);
        }

        public static Task<HttpResponseMessage> PutAsync(this IHttpScheduler This, Uri requestUri, HttpContent content, CancellationToken cancellationToken, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Put, requestUri) { Content = content }, cancellationToken, priority);
        }

        public static Task<HttpResponseMessage> PutAsync(this IHttpScheduler This, string requestUri, HttpContent content, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Put, requestUri) { Content = content }, priority);
        }

        public static Task<HttpResponseMessage> PutAsync(this IHttpScheduler This, string requestUri, HttpContent content, CancellationToken cancellationToken, int priority = 0)
        {
            return This.SendAsync(new HttpRequestMessage (HttpMethod.Put, requestUri) { Content = content }, cancellationToken, priority);
        }

        public static Task<HttpResponseMessage> SendAsync(this IHttpScheduler This, HttpRequestMessage request, int priority = 0)
        {
            return This.SendAsync(request, CancellationToken.None, priority);
        }
    }
#endregion
}
