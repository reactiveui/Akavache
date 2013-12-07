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

        public IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority)
        {
            return inner.Schedule(request, priority).TakeUntil(cancel);
        }

        public void ResetLimit(long? maxBytesToRead = null)
        {
            inner.ResetLimit(maxBytesToRead);
        }

        public HttpClient Client
        {
            get { return inner.Client; }
            set { inner.Client = value; }
        }
    }

}
