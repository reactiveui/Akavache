using System;
using System.Net.Http;
using ReactiveUI;
using Punchclock;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive;
using System.Reactive.Disposables;

namespace Akavache.Http
{

    public class HttpScheduler : IHttpScheduler
    {
        readonly OperationQueue opQueue;
        readonly int priorityBase;
        readonly int retryCount;

        public HttpScheduler(OperationQueue opQueue = null, int priorityBase = 100, int retryCount = 3)
        {
            this.opQueue = opQueue; 
            this.priorityBase = priorityBase; 
            this.retryCount = retryCount;
        }

        public IObservable<Tuple<HttpResponseMessage, byte[]>> Schedule(HttpRequestMessage request, int priority)
        {
            var rq = Observable.Defer(() => Client.SendAsyncObservable(request));
            if (retryCount > 0) 
            {
                rq = rq.Retry(retryCount);
            }

            var ret = Observable.Create<Tuple<HttpResponseMessage, byte[]>>(subj =>
            {
                var cancel = new AsyncSubject<Unit>();
                var disp = opQueue.EnqueueObservableOperation(priorityBase + priority, null, cancel, () => rq).Subscribe(subj);

                return Disposable.Create(() => 
                {
                    cancel.OnNext(Unit.Default);    
                    cancel.OnCompleted();
                    disp.Dispose();
                });
            });

            return ret.PublishLast().RefCount();
        }

        public HttpClient Client { get; set; }
    }
    
}