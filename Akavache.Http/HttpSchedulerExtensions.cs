using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Disposables;

namespace Akavache.Http
{
    public static class HttpSchedulerExtensions
    {
        /// <summary>
        /// Sends an HTTP request to be sent with the specified priority
        /// </summary>
        /// <param name="request">The request to send</param>
        /// <param name="priority">The relative priority for the class of
        /// request. Use zero if you don't know.</param>
        /// <returns>A Future result representing the HTTP response as well as
        /// the body of the message.</returns>
        public static IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(this IHttpScheduler This, HttpRequestMessage request, int priority)
        {
            return This.Schedule(request, priority, _ => true);
        }

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
    }

    class CancellationWrapper : IHttpScheduler
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
            inner.ResetLimit(maxBytesToRead);
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
}
